// Simulación completa de un nivel, independiente de Phaser (se usa en el juego, en los tests y en el bot).
import { TILE, F, parseMap, computeFlags, flagAtPx, moveBox, furnitureRect, tileCenter, lineOfSight } from './grid.js';
import { FURNITURE } from './furniture.js';
import { ITEMS } from '../data/items.js';
import { FERRETS } from '../data/ferrets.js';
import { Owner } from './owner.js';
import { makeRng } from './rng.js';
import { computeScore, starsFor, STEALTH_BONUS } from './score.js';

export const FERRET_HW = 5;
export const FERRET_HH = 4;
const PICK_DIST = 14;
const SEARCH_TIME = 1.2;
const RUN_DRAIN = 20;
const ENERGY_REGEN = 12;
const CAUGHT_TIME = 6;
const CAUGHT_PENALTY = 15;   // segundos de día que se pierden al ir al corralito
const TV_TIME = 12;
const SLIDE_SPEED = 210;
const TREAT_TIME = 8;
const NOISE_TILES = 9;       // radio de ruido (en casillas) con ruido = 1

export const TIER_LINES = [
  null,
  '¡Aquí pasa algo raro!',
  '¡Te voy a pillar, bicho!',
];

export class World {
  constructor(levelDef, ferretId, opts = {}) {
    this.levelDef = levelDef;
    this.rng = makeRng(opts.seed ?? 12345);
    this.events = [];
    this.time = 0;
    this.duration = levelDef.duration;
    this.quota = levelDef.quota;
    this.suspicion = 0;
    this.tier = 0;
    this.tvT = 0;
    this.timers = [];
    this.ended = false;
    this.result = null;
    this.stats = { caught: 0, chases: 0, stolen: 0, used: 0, washed: 0, recovered: 0 };
    this.seenHints = new Set();

    const m = parseMap(levelDef.map);
    this.level = { ...m, furniture: [] };
    levelDef.furniture.forEach((f, i) => {
      const def = FURNITURE[f.type];
      if (!def) throw new Error(`Mueble desconocido ${f.type}`);
      const pos = f.positions ? f.positions[0] : f;
      this.level.furniture.push({ ...f, x: pos.x, y: pos.y, uid: f.id ?? `${f.type}${i}`, posIdx: 0 });
    });
    computeFlags(this.level);

    this.sofa = this.level.furniture.find((f) => f.type === 'sofa') ?? null;
    this.burrow = this.level.furniture.find((f) => f.type === 'burrow');
    this.playpen = this.level.furniture.find((f) => f.type === 'playpen');
    this.tubes = levelDef.tubes ?? [];

    this.items = levelDef.items.map((it, i) => {
      const def = ITEMS[it.id];
      if (!def) throw new Error(`Objeto desconocido ${it.id}`);
      const base = { uid: i, id: it.id, state: 'world', stash: null, noticed: false };
      if (it.in) {
        return { ...base, container: it.in, originContainer: it.in, x: 0, y: 0, ox: 0, oy: 0 };
      }
      const x = tileCenter(it.x) + (it.dx ?? 0);
      const y = tileCenter(it.y) + (it.dy ?? 0);
      return { ...base, container: null, originContainer: null, x, y, ox: x, oy: y };
    });

    const fd = FERRETS[ferretId];
    this.ferret = {
      def: fd,
      x: tileCenter(levelDef.start.x),
      y: tileCenter(levelDef.start.y),
      fx: 0, fy: 1,         // dirección a la que mira
      mode: 'walk',          // walk | run | sneak | still
      state: 'free',         // free | caught | tube | slide | search
      carried: [],
      energy: fd.energy,
      exhaustedT: 0,
      treatT: 0,
      noise: 0,
      noiseT: 0,
      stepT: 0,
      graceT: 0,
      moving: false,
      hint: null,
    };
    this.owners = levelDef.owners.map((o, i) => new Owner(this, o, i));
  }

  emit(e) { this.events.push(e); }
  drainEvents() { const e = this.events; this.events = []; return e; }
  itemByUid(uid) { return this.items[uid]; }
  furnitureById(id) { return this.level.furniture.find((f) => f.uid === id); }

  itemAtOrigin(it) {
    if (it.originContainer) return it.state === 'world' && it.container === it.originContainer;
    return it.state === 'world' && !it.container && Math.abs(it.x - it.ox) < 1 && Math.abs(it.y - it.oy) < 1;
  }

  score() { return computeScore(this.items); }
  sofaItems() { return this.items.filter((i) => i.state === 'stashed' && i.stash === 'sofa'); }
  sofaCenter() {
    const r = furnitureRect(this.sofa);
    return { x: (r.x + r.w / 2) * TILE, y: (r.y + r.h / 2) * TILE };
  }
  sofaSpot() {
    const r = furnitureRect(this.sofa);
    return this.levelDef.sofaSpot ?? { x: r.x + 1, y: r.y + r.h };
  }

  mostValuableLeft() {
    let best = null;
    for (const it of this.items) {
      if (it.state !== 'world' || it.container || !this.itemAtOrigin(it)) continue;
      if (!best || ITEMS[it.id].value > ITEMS[best.id].value) best = it;
    }
    return best;
  }

  addSuspicion(n) {
    this.suspicion = Math.min(100, this.suspicion + n);
    const tier = this.suspicion >= 70 ? 2 : this.suspicion >= 35 ? 1 : 0;
    if (tier > this.tier) {
      this.tier = tier;
      this.emit({ type: 'tier', tier, text: TIER_LINES[tier] });
      if (tier >= 1) this.closeDoors();
    }
  }

  closeDoors() {
    let changed = false;
    for (const d of this.level.doors) {
      if (d.kind === 'closable' && !d.closed) {
        d.closed = true;
        changed = true;
      }
    }
    if (changed) {
      this.emit({ type: 'doors' });
      computeFlags(this.level);
    }
  }

  ownerEnteredTile(owner) {
    for (const d of this.level.doors) {
      if (!d.closed) continue;
      if (Math.abs(d.x - owner.tx) <= 1 && Math.abs(d.y - owner.ty) <= 1) {
        const wasShut = d.openTimer <= 0;
        d.openTimer = 1.6;
        if (wasShut) {
          computeFlags(this.level);
          this.emit({ type: 'door', x: d.x, y: d.y, open: true });
        }
      }
    }
  }

  routineAction(owner, action) {
    if (action === 'basket') {
      const b = this.level.furniture.find((f) => f.positions);
      if (!b) return;
      b.posIdx = (b.posIdx + 1) % b.positions.length;
      b.x = b.positions[b.posIdx].x;
      b.y = b.positions[b.posIdx].y;
      computeFlags(this.level);
      this.emit({ type: 'basket', uid: b.uid });
    }
  }

  // ------------------------------------------------------------ visibilidad del hurón
  /** 0 = invisible (escondido, en tubo...), 0.6 sigilo quieto, 1 normal. */
  ferretVisibility() {
    const f = this.ferret;
    if (f.state === 'caught' || f.state === 'tube' || f.graceT > 0) return 0;
    if (flagAtPx(this.level, f.x, f.y) & F.HIDE) return 0;
    if (f.mode === 'sneak') return f.moving ? 0.75 : 0.6;
    return 1;
  }

  emitNoise(x, y, level, kind = 'step') {
    const radius = level * NOISE_TILES * TILE;
    this.emit({ type: 'noise', x, y, r: radius, kind });
    for (const o of this.owners) o.hear(x, y, radius, kind);
  }

  onChase() {
    this.stats.chases++;
    this.addSuspicion(8);
    this.emit({ type: 'chase' });
  }

  catchFerret(owner) {
    const f = this.ferret;
    for (const it of f.carried) this.returnToOrigin(it);
    f.carried = [];
    f.state = 'caught';
    f.caughtT = CAUGHT_TIME;
    const r = furnitureRect(this.playpen);
    f.x = (r.x + r.w / 2) * TILE;
    f.y = (r.y + r.h / 2) * TILE + 2;
    this.time += CAUGHT_PENALTY;
    this.stats.caught++;
    this.addSuspicion(8);
    for (const o of this.owners) {
      o.detect = 0;
      if (o !== owner && o.state === 'chase') o.backToRoutine();
    }
    this.emit({ type: 'caught', owner: owner.index });
  }

  returnToOrigin(it) {
    it.state = 'world';
    it.stash = null;
    if (it.originContainer) {
      it.container = it.originContainer;
    } else {
      it.container = null;
      it.x = it.ox;
      it.y = it.oy;
    }
    it.noticed = false;
  }

  tidyItem(owner, it) {
    const zone = this.levelDef.laundryZone;
    const tx = Math.floor(it.x / TILE);
    const ty = Math.floor(it.y / TILE);
    if (zone && tx >= zone.x && ty >= zone.y && tx < zone.x + zone.w && ty < zone.y + zone.h) {
      it.state = 'lost';
      this.stats.washed++;
      owner.say(`¡${ITEMS[it.id].name}! A la lavadora.`);
      this.emit({ type: 'washed', item: it.uid });
    } else {
      this.returnToOrigin(it);
      owner.say('Esto va a su sitio.');
      this.emit({ type: 'tidy', item: it.uid });
    }
    this.addSuspicion(2);
  }

  checkSofa(owner) {
    const found = this.sofaItems();
    if (found.length === 0) {
      owner.say('Nada entre los cojines.');
      return;
    }
    for (const it of found) this.returnToOrigin(it);
    this.stats.recovered += found.length;
    owner.say(`¡Aquí están mis cosas! (${found.length})`);
    this.addSuspicion(5);
    this.emit({ type: 'sofaCheck', count: found.length });
  }

  // ------------------------------------------------------------ interacción del hurón
  canCarry(def) {
    const f = this.ferret;
    if (f.def.slots === 1) return f.carried.length === 0;
    if (f.carried.some((c) => ITEMS[c.id].size === 'l')) return false;
    if (def.size === 'l') return f.carried.length === 0;
    return f.carried.length < 2;
  }

  stashZoneAt(x, y) {
    if (this.burrow) {
      const bx = tileCenter(this.burrow.x);
      const by = tileCenter(this.burrow.y);
      if (Math.hypot(x - bx, y - by) < 20) return 'burrow';
    }
    if (this.sofa) {
      const r = furnitureRect(this.sofa);
      if (x > r.x * TILE - 10 && x < (r.x + r.w) * TILE + 10 && y > r.y * TILE - 10 && y < (r.y + r.h) * TILE + 12) return 'sofa';
    }
    return null;
  }

  nearItem(x, y) {
    let best = null;
    let bd = PICK_DIST;
    for (const it of this.items) {
      if (it.state !== 'world' || it.container) continue;
      const d = Math.hypot(it.x - x, it.y - y);
      if (d < bd) { bd = d; best = it; }
    }
    return best;
  }

  nearContainer(x, y) {
    let best = null;
    let bd = Infinity;
    for (const f of this.level.furniture) {
      if (!FURNITURE[f.type].container) continue;
      const r = furnitureRect(f);
      const cx = Math.max(r.x * TILE, Math.min(x, (r.x + r.w) * TILE));
      const cy = Math.max(r.y * TILE, Math.min(y, (r.y + r.h) * TILE));
      const d = Math.hypot(cx - x, cy - y);
      if (d < 10 && d < bd) { bd = d; best = f; }
    }
    return best;
  }

  containerItems(f) {
    return this.items.filter((it) => it.state === 'world' && it.container === f.uid);
  }

  nearTube(x, y) {
    for (const [a, b] of this.tubes) {
      if (Math.hypot(tileCenter(a.x) - x, tileCenter(a.y) - y) < 12) return { from: a, to: b };
      if (Math.hypot(tileCenter(b.x) - x, tileCenter(b.y) - y) < 12) return { from: b, to: a };
    }
    return null;
  }

  /** Qué haría el botón A ahora mismo (para el texto de ayuda del HUD y para la acción). */
  actionHint() {
    const f = this.ferret;
    if (f.state === 'caught') return { kind: 'mash', label: '¡Pulsa A para escapar!' };
    if (f.state !== 'free') return null;
    const zone = this.stashZoneAt(f.x, f.y);
    if (f.carried.length && zone) return { kind: 'deposit', zone, label: zone === 'sofa' ? 'Esconder en el sofá (x1.5)' : 'Esconder en la madriguera' };
    const it = this.nearItem(f.x, f.y);
    if (it && (this.canCarry(ITEMS[it.id]) || ITEMS[it.id].instant)) return { kind: 'pickup', item: it, label: `Coger: ${ITEMS[it.id].name}` };
    if (it && f.carried.length) return { kind: 'swap', item: it, label: `Cambiar por: ${ITEMS[it.id].name}` };
    if (zone === 'burrow' && !f.carried.length && this.score().total >= this.quota[0]) return { kind: 'end', label: 'Dormir y terminar el día' };
    const c = this.nearContainer(f.x, f.y);
    if (c && this.canCarry({ size: 's' })) return { kind: 'search', container: c, label: `Rebuscar: ${FURNITURE[c.type].label}` };
    const t = this.nearTube(f.x, f.y);
    if (t) return { kind: 'tube', tube: t, label: 'Meterse por el tubo' };
    if (f.carried.length) return { kind: 'drop', label: 'Soltar' };
    return null;
  }

  pickup(it) {
    const f = this.ferret;
    const def = ITEMS[it.id];
    if (def.instant) {
      it.state = 'used';
      f.treatT = TREAT_TIME;
      this.stats.used++;
      this.emit({ type: 'power', power: def.power, item: it.uid });
      return;
    }
    it.state = 'carried';
    it.container = null;
    f.carried.push(it);
    this.stats.stolen++;
    this.emit({ type: 'pickup', item: it.uid });
  }

  deposit(zone) {
    const f = this.ferret;
    const before = this.score().total;
    const colorsBefore = this.score().pairCount;
    for (const it of f.carried) {
      it.state = 'stashed';
      it.stash = zone;
    }
    const items = f.carried.map((i) => i.uid);
    f.carried = [];
    const sc = this.score();
    this.emit({ type: 'deposit', zone, items, gained: sc.total - before });
    if (sc.pairCount > colorsBefore) this.emit({ type: 'pair' });
    this.emitNoise(f.x, f.y, 0.15 * f.def.noise, 'deposit');
    if (zone === 'sofa') {
      for (const o of this.owners) {
        if (o.inCone(f.x, f.y, o.range())) {
          o.sofaCd = 0;
          this.addSuspicion(4);
        }
      }
    }
  }

  usePower() {
    const f = this.ferret;
    const it = f.carried.find((c) => ITEMS[c.id].power);
    if (!it) return false;
    const power = ITEMS[it.id].power;
    f.carried = f.carried.filter((c) => c !== it);
    it.state = 'used';
    this.stats.used++;
    this.emit({ type: 'power', power, item: it.uid });
    if (power === 'slide') {
      f.state = 'slide';
      f.slideT = flagAtPx(this.level, f.x, f.y) & F.SLIPPERY ? 1.5 : 1.0;
      f.noiseT = 0;
    } else if (power === 'keys') {
      // Vuelan hasta 6 casillas en la dirección a la que mira, hasta chocar.
      let tx = f.x;
      let ty = f.y;
      for (let i = 0; i < 6 * 4; i++) {
        const nx = tx + f.fx * 4;
        const ny = ty + f.fy * 4;
        if (flagAtPx(this.level, nx, ny) & (F.WALL | F.BLOCK_VISION)) break;
        tx = nx;
        ty = ny;
      }
      this.emit({ type: 'keysThrow', x0: f.x, y0: f.y, x: tx, y: ty });
      this.timers.push({ t: 0.45, fn: () => {
        this.emit({ type: 'keysLand', x: tx, y: ty });
        this.emitNoise(tx, ty, 1.25, 'keys');
      } });
    } else if (power === 'remote') {
      this.tvT = TV_TIME;
      this.emit({ type: 'tv', on: true });
      for (const o of this.owners) o.startTv();
    }
    return true;
  }

  // ------------------------------------------------------------ actualización
  /**
   * input: { mx, my (-1..1), run, sneak, a (pulsado este frame), b (pulsado este frame) }
   */
  update(dt, input = {}) {
    if (this.ended) return;
    dt = Math.min(dt, 0.05);
    this.time += dt;
    for (const t of this.timers) t.t -= dt;
    const due = this.timers.filter((t) => t.t <= 0);
    this.timers = this.timers.filter((t) => t.t > 0);
    due.forEach((t) => t.fn());

    if (this.tvT > 0) {
      this.tvT -= dt;
      if (this.tvT <= 0) this.emit({ type: 'tv', on: false });
    }
    this.updateDoors(dt);
    this.updateFerret(dt, input);
    for (const o of this.owners) o.update(dt);
    this.updateHints();
    this.ferret.hint = this.actionHint();
    if (this.time >= this.duration) this.finish('time');
  }

  updateDoors(dt) {
    let changed = false;
    const f = this.ferret;
    for (const d of this.level.doors) {
      if (!d.closed || d.openTimer <= 0) continue;
      d.openTimer -= dt;
      if (d.openTimer <= 0) {
        // No cierra encima de nadie.
        const busy = (Math.floor(f.x / TILE) === d.x && Math.floor(f.y / TILE) === d.y)
          || this.owners.some((o) => Math.abs(o.tx - d.x) <= 1 && Math.abs(o.ty - d.y) <= 1);
        if (busy) d.openTimer = 0.3;
        else changed = true;
      }
    }
    if (changed) {
      computeFlags(this.level);
      this.emit({ type: 'doors' });
    }
  }

  updateHints() {
    const hints = this.levelDef.hints;
    if (!hints) return;
    const tx = Math.floor(this.ferret.x / TILE);
    const ty = Math.floor(this.ferret.y / TILE);
    hints.forEach((h, i) => {
      if (this.seenHints.has(i)) return;
      if (tx >= h.x && ty >= h.y && tx < h.x + h.w && ty < h.y + h.h) {
        this.seenHints.add(i);
        this.emit({ type: 'hint', text: h.text });
      }
    });
  }

  updateFerret(dt, input) {
    const f = this.ferret;
    const fd = f.def;
    f.graceT = Math.max(0, f.graceT - dt);
    f.treatT = Math.max(0, f.treatT - dt);
    f.moving = false;

    if (f.state === 'caught') {
      f.mode = 'still';
      if (input.a) f.caughtT -= 0.35;
      f.caughtT -= dt;
      if (f.caughtT <= 0) {
        const r = furnitureRect(this.playpen);
        const exit = this.levelDef.playpenExit ?? { x: r.x + r.w, y: r.y + r.h - 1 };
        f.x = tileCenter(exit.x);
        f.y = tileCenter(exit.y);
        f.state = 'free';
        f.graceT = 2.5;
        this.emit({ type: 'released' });
      }
      return;
    }
    if (f.state === 'tube') {
      f.tubeT -= dt;
      if (f.tubeT <= 0) {
        f.x = tileCenter(f.tubeTo.x);
        f.y = tileCenter(f.tubeTo.y);
        f.state = 'free';
        this.emit({ type: 'tubeOut' });
      }
      return;
    }
    if (f.state === 'search') {
      const moving = Math.hypot(input.mx ?? 0, input.my ?? 0) > 0.3;
      if (moving) {
        f.state = 'free';
        this.emit({ type: 'searchCancel' });
      } else {
        f.searchT -= dt;
        f.noiseT -= dt;
        if (f.noiseT <= 0) {
          f.noiseT = 0.5;
          this.emitNoise(f.x, f.y, 0.4 * fd.noise, 'search');
        }
        f.noise = 0.4 * fd.noise;
        if (f.searchT <= 0) {
          f.state = 'free';
          const c = this.furnitureById(f.searchIn);
          const inside = c ? this.containerItems(c) : [];
          if (inside.length) {
            const it = inside[0];
            const fits = this.canCarry(ITEMS[it.id]);
            if (fits) {
              this.pickup(it);
            } else {
              // Grande y no cabe en la boca: cae a los pies.
              it.container = null;
              it.x = f.x;
              it.y = f.y + 4;
            }
            this.emit({ type: 'searchDone', item: it.uid, dropped: !fits, left: inside.length - 1 });
          } else {
            this.emit({ type: 'searchDone', item: null });
          }
        }
        return;
      }
    }

    // --- Movimiento
    let mx = input.mx ?? 0;
    let my = input.my ?? 0;
    const mag = Math.hypot(mx, my);
    if (mag > 1) { mx /= mag; my /= mag; }
    let speed;
    let noise = 0;
    if (f.state === 'slide') {
      f.slideT -= dt;
      const r = moveBox(this.level, f.x, f.y, FERRET_HW, FERRET_HH, f.fx * SLIDE_SPEED * dt, f.fy * SLIDE_SPEED * dt, F.BLOCK_FERRET);
      f.x = r.x;
      f.y = r.y;
      f.moving = true;
      f.mode = 'slide';
      if (f.slideT <= 0 || r.hitX || r.hitY) f.state = 'free';
      f.noise = 0.3 * fd.noise;
      f.noiseT -= dt;
      if (f.noiseT <= 0) {
        f.noiseT = 0.3;
        this.emitNoise(f.x, f.y, f.noise, 'slide');
      }
      return;
    }

    if (f.exhaustedT > 0) f.exhaustedT -= dt;
    const wantRun = input.run && f.exhaustedT <= 0 && f.energy > 0;
    const moving = Math.hypot(mx, my) > 0.15;
    if (!moving) f.mode = 'still';
    else if (input.sneak) f.mode = 'sneak';
    else if (wantRun) f.mode = 'run';
    else f.mode = 'walk';

    speed = f.mode === 'run' ? fd.run : f.mode === 'sneak' ? fd.sneak : fd.walk;
    if (f.exhaustedT > 0) speed *= 0.7;
    const weight = f.carried.reduce((s, c) => s + ITEMS[c.id].weight, 0);
    speed *= Math.max(0.5, 1 - weight * fd.carryPenalty * 1.6);
    if (f.treatT > 0) speed *= 1.3;

    if (f.mode === 'run') {
      f.energy = Math.max(0, f.energy - RUN_DRAIN * dt);
      if (f.energy <= 0) {
        f.exhaustedT = 2.5;
        this.emit({ type: 'exhausted' });
      }
    } else {
      f.energy = Math.min(fd.energy, f.energy + ENERGY_REGEN * dt * (f.mode === 'still' ? 1.4 : 1));
    }

    if (moving) {
      const r = moveBox(this.level, f.x, f.y, FERRET_HW, FERRET_HH, mx * speed * dt, my * speed * dt, F.BLOCK_FERRET);
      f.moving = Math.abs(r.x - f.x) + Math.abs(r.y - f.y) > 0.01;
      f.x = r.x;
      f.y = r.y;
      // Dirección dominante a 8 vías (para lanzar llaves / deslizarse).
      const a = Math.atan2(my, mx);
      const oct = Math.round(a / (Math.PI / 4)) * (Math.PI / 4);
      f.fx = Math.round(Math.cos(oct) * 1000) / 1000;
      f.fy = Math.round(Math.sin(oct) * 1000) / 1000;
      const flags = flagAtPx(this.level, f.x, f.y);
      noise = f.mode === 'run' ? 0.7 : f.mode === 'sneak' ? 0.05 : 0.22;
      if (flags & F.CARPET) noise *= 0.6;
      if (flags & F.SLIPPERY) noise *= 1.15;
      noise += f.carried.reduce((s, c) => s + ITEMS[c.id].noise, 0) * (f.mode === 'sneak' ? 0.3 : 1);
      noise *= fd.noise;
      if (f.treatT > 0) noise *= 0.5;
      f.stepT -= dt;
      if (f.stepT <= 0) {
        f.stepT = f.mode === 'run' ? 0.16 : 0.28;
        this.emit({ type: 'step', mode: f.mode });
      }
    }
    f.noise = Math.min(1, noise);
    f.noiseT -= dt;
    if (f.noiseT <= 0 && f.noise > 0.12) {
      f.noiseT = 0.3;
      this.emitNoise(f.x, f.y, f.noise, 'step');
    }

    // --- Acciones
    if (input.b) {
      if (!this.usePower()) this.emit({ type: 'noPower' });
    }
    if (input.a) this.doAction();
  }

  doAction() {
    const f = this.ferret;
    const h = this.actionHint();
    if (!h) return;
    switch (h.kind) {
      case 'deposit': this.deposit(h.zone); break;
      case 'pickup': this.pickup(h.item); break;
      case 'swap': {
        // Suelta lo último que llevaba y coge el nuevo (si aun así no cabe, suelta también el resto).
        while (f.carried.length && !this.canCarry(ITEMS[h.item.id])) {
          const old = f.carried.pop();
          old.state = 'world';
          old.x = f.x;
          old.y = f.y + 3;
          this.emit({ type: 'drop', item: old.uid });
        }
        this.pickup(h.item);
        break;
      }
      case 'end': this.emit({ type: 'askEnd' }); break;
      case 'search':
        f.state = 'search';
        f.searchT = SEARCH_TIME;
        f.searchIn = h.container.uid;
        f.noiseT = 0;
        this.emit({ type: 'searchStart', container: h.container.uid });
        break;
      case 'tube':
        f.state = 'tube';
        f.tubeT = 1.0;
        f.tubeTo = h.tube.to;
        this.emit({ type: 'tubeIn' });
        break;
      case 'drop': {
        const it = f.carried.pop();
        it.state = 'world';
        it.container = null;
        it.x = f.x;
        it.y = f.y + 3;
        this.emit({ type: 'drop', item: it.uid });
        break;
      }
      default: break;
    }
  }

  finish(reason) {
    if (this.ended) return;
    this.ended = true;
    const sc = this.score();
    const stealth = this.stats.chases === 0 && this.stats.caught === 0 && sc.total > 0;
    const stealthBonus = stealth ? Math.round(sc.total * STEALTH_BONUS) : 0;
    const total = sc.total + stealthBonus;
    const stars = starsFor(total, this.quota);
    this.result = {
      reason, ...sc, stealthBonus, final: total, stars, win: stars >= 1,
      stats: { ...this.stats }, stashed: this.items.filter((i) => i.state === 'stashed').length,
    };
    this.emit({ type: 'end', result: this.result });
  }

  /** Para la presentación: fracción del día (0..1) y reloj "HH:MM" de 8:00 a 20:00. */
  clock() {
    const t = Math.min(1, this.time / this.duration);
    const mins = 8 * 60 + Math.floor(t * 12 * 60);
    return { t, text: `${String(Math.floor(mins / 60)).padStart(2, '0')}:${String(mins % 60).padStart(2, '0')}` };
  }
}

export { TILE, F, lineOfSight };
