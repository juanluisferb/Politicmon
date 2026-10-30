// IA del dueño (y del invitado): rutina por waypoints, oído, vista con cono y sospecha global.
import { TILE, F, lineOfSight, tileCenter } from './grid.js';
import { findPath, nearestFree } from './path.js';
import { ITEMS } from '../data/items.js';

export const TIER_SPEED = [1, 1.12, 1.24];
export const TIER_WAIT = [1, 0.7, 0.45];
export const TIER_RANGE = [5, 6, 7];      // alcance de la vista en casillas
export const WALK_SPEED = 40;
export const CHASE_SPEED = 76;
export const CATCH_DIST = 11;
const FOV_COS = Math.cos((80 / 2) * (Math.PI / 180));
const TURN_RATE = 7;

const angleTo = (x0, y0, x1, y1) => Math.atan2(y1 - y0, x1 - x0);
const wrap = (a) => Math.atan2(Math.sin(a), Math.cos(a));

export class Owner {
  constructor(world, def, index) {
    this.world = world;
    this.def = def;
    this.index = index;
    this.name = def.name;
    this.skin = def.skin;
    this.speedMul = def.speed ?? 1;
    this.lines = def.lines ?? {};
    this.x = tileCenter(def.start.x);
    this.y = tileCenter(def.start.y);
    this.angle = Math.PI / 2;
    this.state = 'routine';
    this.wpIdx = 0;
    this.wpPhase = 'go';
    this.waitT = 0;
    this.path = null;
    this.goal = null;
    this.detect = 0;
    this.lastSeen = null;
    this.lostT = 0;
    this.repathT = 0;
    this.pauseT = 0;
    this.lookT = 0;
    this.lookBase = 0;
    this.task = null;
    this.searchT = 0;
    this.searchC = null;
    this.hearCd = 0;
    this.act = null;       // animación actual: 'sit', 'cook', 'phone', 'wash'...
    this.bubble = null;    // '?', '!' o null
    this.moving = false;
    this.sofaCd = 25;
    this.tidyCd = 0;
  }

  // ------------------------------------------------------------ utilidades
  get tx() { return Math.floor(this.x / TILE); }
  get ty() { return Math.floor(this.y / TILE); }

  say(text) {
    if (text) this.world.emit({ type: 'say', owner: this.index, text });
  }

  line(key, fallback) {
    const l = this.lines[key] ?? fallback;
    if (Array.isArray(l)) return l[this.world.rng.int(l.length)];
    return l;
  }

  routine() {
    const r = this.def.routine;
    const t = this.world.tier;
    return (t >= 2 && r.paranoid) || (t >= 1 && r.alert) || r.calm;
  }

  goTo(tx, ty) {
    const lv = this.world.level;
    const p = findPath(lv, this.tx, this.ty, tx, ty, F.BLOCK_OWNER);
    this.goal = { x: tx, y: ty };
    this.path = p ?? [];
    return p !== null;
  }

  /** Avanza por el camino. Devuelve true al llegar. */
  follow(dt, speed) {
    this.moving = false;
    if (!this.path) return true;
    if (this.path.length === 0) return true;
    const n = this.path[0];
    const cx = tileCenter(n.x);
    const cy = tileCenter(n.y);
    const dx = cx - this.x;
    const dy = cy - this.y;
    const d = Math.hypot(dx, dy);
    const step = speed * dt;
    this.turnTo(Math.atan2(dy, dx), dt);
    this.moving = true;
    if (d <= step) {
      this.x = cx;
      this.y = cy;
      this.path.shift();
      this.world.ownerEnteredTile(this);
      return this.path.length === 0;
    }
    this.x += (dx / d) * step;
    this.y += (dy / d) * step;
    return false;
  }

  turnTo(a, dt) {
    const diff = wrap(a - this.angle);
    const m = TURN_RATE * dt;
    this.angle = wrap(this.angle + Math.max(-m, Math.min(m, diff)));
  }

  walkSpeed() { return WALK_SPEED * TIER_SPEED[this.world.tier] * this.speedMul; }
  chaseSpeed() { return CHASE_SPEED * TIER_SPEED[this.world.tier] * this.speedMul; }
  range() { return TIER_RANGE[this.world.tier] * TILE; }

  inCone(px, py, range) {
    const dx = px - this.x;
    const dy = py - this.y;
    const d = Math.hypot(dx, dy);
    if (d > range) return false;
    if (d < 1) return true;
    const c = (dx * Math.cos(this.angle) + dy * Math.sin(this.angle)) / d;
    if (c < FOV_COS) return false;
    return lineOfSight(this.world.level, this.x, this.y - 6, px, py);
  }

  // ------------------------------------------------------------ percepción
  perceive(dt) {
    const w = this.world;
    const f = w.ferret;
    const vis = w.ferretVisibility();
    let seen = false;
    let d = Infinity;
    if (vis > 0) {
      d = Math.hypot(f.x - this.x, f.y - this.y);
      const range = this.range() * vis;
      const close = d < TILE * 1.4 && lineOfSight(w.level, this.x, this.y, f.x, f.y);
      seen = close || this.inCone(f.x, f.y, range);
      if (seen) {
        let rate = 0.95 * (1 + 1.5 * Math.max(0, 1 - d / Math.max(range, 1))) * (1 + 0.25 * w.tier);
        if (f.mode === 'run') rate *= 1.4;
        if (f.mode === 'sneak') rate *= 0.5;
        if (close) rate = Math.max(rate, 4);
        if (this.state === 'tv') rate *= 0.35;
        this.detect = Math.min(1, this.detect + rate * dt);
      }
    }
    if (!seen) this.detect = Math.max(0, this.detect - 0.6 * dt);

    if (this.state === 'chase') {
      if (seen) {
        this.lastSeen = { x: f.x, y: f.y };
        this.lostT = 0;
      } else {
        this.lostT += dt;
        if (this.lostT > 2.5) {
          this.say(this.line('lost', '¿Dónde te has metido?'));
          w.emit({ type: 'lost', owner: this.index });
          this.startSearch(this.lastSeen ?? { x: this.x, y: this.y }, 6);
        }
      }
      return;
    }
    if (this.detect >= 1) {
      this.startChase();
      return;
    }
    if (seen && this.detect > 0.35 && this.state !== 'suspicious' && this.state !== 'catching') {
      this.bubble = '?';
      this.startSuspicious(f.x, f.y, false);
      return;
    }
    if (this.state === 'tv') return;
    this.noticeItems();
  }

  noticeItems() {
    const w = this.world;
    const range = this.range();
    for (const it of w.items) {
      if (it.noticed || it.originContainer || w.itemAtOrigin(it)) continue;
      if (this.inCone(it.ox, it.oy, range)) {
        it.noticed = true;
        const name = ITEMS[it.id].name.toLowerCase();
        this.say(this.line('missing', `¿Dónde está mi ${name}?`).replace('{item}', name));
        w.emit({ type: 'notice', owner: this.index, item: it.uid });
        w.addSuspicion(4);
        return;
      }
    }
    if (this.task || this.tidyCd > 0 || this.state === 'search') return;
    for (const it of w.items) {
      if (it.state !== 'world' || it.container || w.itemAtOrigin(it)) continue;
      if (Math.hypot(it.x - this.x, it.y - this.y) > 6 * TILE) continue;
      if (this.inCone(it.x, it.y, range)) {
        this.startTidy(it);
        return;
      }
    }
  }

  hear(x, y, radius, kind) {
    if (this.state === 'catching' || this.pauseT > 0) return;
    const d = Math.hypot(x - this.x, y - this.y);
    const los = lineOfSight(this.world.level, this.x, this.y, x, y);
    if (d > radius * (los ? 1 : 0.55)) return;
    if (this.state === 'chase') {
      if (kind !== 'keys') this.lastSeen = { x, y };
      return;
    }
    if (kind === 'keys') {
      this.say(this.line('keys', '¿Y ese tintineo? ¿Mis llaves?'));
      this.startSuspicious(x, y, true);
      this.world.addSuspicion(3);
      return;
    }
    if (this.state === 'tv' && d > 3 * TILE) return;
    if (this.hearCd <= 0) {
      this.world.addSuspicion(1.5);
      this.hearCd = 3;
      if (this.state !== 'suspicious') this.say(this.line('hear', ['¿Qué ha sido eso?', '¿Hola?', '¿Eh?']));
    }
    this.startSuspicious(x, y, false);
  }

  // ------------------------------------------------------------ transiciones
  startChase() {
    const w = this.world;
    this.state = 'chase';
    this.task = null;
    this.bubble = '!';
    this.lostT = 0;
    this.repathT = 0;
    this.lastSeen = { x: w.ferret.x, y: w.ferret.y };
    this.say(this.line('chase', ['¡TE PILLÉ!', '¡QUIETO AHÍ!', '¡OTRA VEZ NO!']));
    w.onChase(this);
  }

  startSuspicious(x, y, lure) {
    this.state = 'suspicious';
    this.task = null;
    this.bubble = '?';
    this.lure = lure;
    this.lookT = 0;
    const t = nearestFree(this.world.level, Math.floor(x / TILE), Math.floor(y / TILE), F.BLOCK_OWNER) ?? { x: this.tx, y: this.ty };
    this.goTo(t.x, t.y);
    this.lookTarget = { x, y };
  }

  startSearch(c, secs) {
    this.state = 'search';
    this.task = null;
    this.bubble = '?';
    this.searchC = { x: Math.floor(c.x / TILE), y: Math.floor(c.y / TILE) };
    this.searchT = secs;
    this.path = [];
    this.waitT = 0.4;
  }

  backToRoutine() {
    this.state = 'routine';
    this.bubble = null;
    this.task = null;
    this.act = null;
    // Retoma el waypoint más cercano de la rutina actual.
    const r = this.routine();
    let best = 0;
    let bd = Infinity;
    r.forEach((wp, i) => {
      const d = (wp.x - this.tx) ** 2 + (wp.y - this.ty) ** 2;
      if (d < bd) { bd = d; best = i; }
    });
    this.wpIdx = best;
    this.wpPhase = 'go';
    this.path = null;
  }

  startTidy(item) {
    this.task = { kind: 'tidy', item: item.uid, phase: 'go' };
    this.state = 'task';
    const t = nearestFree(this.world.level, Math.floor(item.x / TILE), Math.floor(item.y / TILE), F.BLOCK_OWNER);
    if (t) this.goTo(t.x, t.y);
    this.say(this.line('tidy', '¿Qué hace esto en el suelo?'));
  }

  startSofaCheck() {
    const s = this.world.sofa;
    if (!s) return;
    const spot = this.world.sofaSpot();
    this.task = { kind: 'sofa', phase: 'go' };
    this.state = 'task';
    this.goTo(spot.x, spot.y);
    this.say(this.line('sofa', 'Voy a mirar entre los cojines...'));
  }

  startTv() {
    const tv = this.world.levelDef.tv;
    if (!tv || this.state === 'chase' || this.state === 'catching') return;
    this.task = { kind: 'tv', phase: 'go' };
    this.state = 'task';
    const spot = tv.watch[this.index % tv.watch.length];
    this.goTo(spot.x, spot.y);
    this.say(this.line('tv', '¡Anda, la tele! Mi serie...'));
  }

  // ------------------------------------------------------------ actualización
  update(dt) {
    const w = this.world;
    this.hearCd -= dt;
    this.sofaCd -= dt;
    this.tidyCd -= dt;
    if (this.pauseT > 0) {
      this.pauseT -= dt;
      this.moving = false;
      if (this.pauseT <= 0 && this.state === 'catching') this.backToRoutine();
      return;
    }
    this.perceive(dt);
    switch (this.state) {
      case 'routine': this.updateRoutine(dt); break;
      case 'suspicious': this.updateSuspicious(dt); break;
      case 'search': this.updateSearch(dt); break;
      case 'chase': this.updateChase(dt); break;
      case 'task': this.updateTask(dt); break;
      case 'tv':
        this.moving = false;
        this.act = 'sit';
        this.turnTo(this.tvAngle ?? -Math.PI / 2, dt);
        if (w.tvT <= 0) {
          this.say(this.line('tvEnd', 'Vaya, se ha apagado...'));
          this.backToRoutine();
        }
        break;
      default: break;
    }
  }

  updateRoutine(dt) {
    const w = this.world;
    const r = this.routine();
    const wp = r[this.wpIdx % r.length];
    if (this.wpPhase === 'go') {
      this.act = null;
      if (!this.path) {
        // Antes de ir al siguiente punto, puede decidir revisar el sofá o vigilar algo valioso.
        if (w.tier >= 1 && this.sofaCd <= 0 && w.sofaItems().length > 0) {
          this.sofaCd = w.tier >= 2 ? 30 : 45;
          this.startSofaCheck();
          return;
        }
        if (w.tier >= 2 && !this.guarding && w.rng() < 0.35) {
          const target = w.mostValuableLeft();
          if (target) {
            const t = nearestFree(w.level, Math.floor(target.x / TILE), Math.floor(target.y / TILE), F.BLOCK_OWNER);
            if (t) {
              this.guarding = true;
              this.goTo(t.x, t.y);
              this.guardWp = { x: t.x, y: t.y, wait: 3, act: 'look', say: this.line('guard', 'Esto lo vigilo yo.') };
              return;
            }
          }
        }
        this.guarding = false;
        this.guardWp = null;
        this.goTo(wp.x, wp.y);
      }
      if (this.follow(dt, this.walkSpeed())) {
        const cur = this.guardWp ?? wp;
        this.wpPhase = 'wait';
        this.waitT = (cur.wait ?? 1) * TIER_WAIT[w.tier];
        this.act = cur.act ?? null;
        this.lookBase = cur.face != null ? (cur.face * Math.PI) / 180 : this.angle;
        this.lookT = 0;
        if (cur.say && w.rng() < (cur.sayChance ?? 0.7)) this.say(cur.say);
        if (cur.do) w.routineAction(this, cur.do);
      }
      return;
    }
    // Esperando en el waypoint.
    this.moving = false;
    this.waitT -= dt;
    this.lookT += dt;
    const sweep = this.act === 'look' ? Math.sin(this.lookT * 2.2) * 1.1 : 0;
    this.turnTo(this.lookBase + sweep, dt);
    if (this.waitT <= 0) {
      if (this.guardWp) {
        this.guardWp = null;
      } else {
        this.wpIdx = (this.wpIdx + 1) % r.length;
      }
      this.wpPhase = 'go';
      this.path = null;
      this.act = null;
    }
  }

  updateSuspicious(dt) {
    this.act = null;
    if (this.path && this.path.length) {
      this.follow(dt, this.walkSpeed() * 1.15);
      return;
    }
    this.moving = false;
    this.lookT += dt;
    const base = angleTo(this.x, this.y, this.lookTarget.x, this.lookTarget.y);
    this.turnTo(base + Math.sin(this.lookT * 3) * 1.2, dt);
    if (this.lookT > (this.lure ? 3 : 1.6)) this.startSearch({ x: this.x, y: this.y }, this.lure ? 4 : 5);
  }

  updateSearch(dt) {
    const w = this.world;
    this.searchT -= dt;
    if (this.searchT <= 0) {
      this.say(this.line('giveUp', ['Bah, serán imaginaciones mías.', 'Nada... Qué raro.']));
      this.backToRoutine();
      return;
    }
    if (this.path && this.path.length) {
      this.follow(dt, this.walkSpeed() * 1.1);
      return;
    }
    this.moving = false;
    this.waitT -= dt;
    this.lookT += dt;
    this.turnTo(this.angle + Math.sin(this.lookT * 4) * 0.8, dt);
    if (this.waitT <= 0) {
      for (let i = 0; i < 8; i++) {
        const tx = this.searchC.x + w.rng.int(9) - 4;
        const ty = this.searchC.y + w.rng.int(9) - 4;
        if (tx < 0 || ty < 0 || tx >= w.level.w || ty >= w.level.h) continue;
        if (w.level.flags[ty * w.level.w + tx] & F.BLOCK_OWNER) continue;
        if (this.goTo(tx, ty) && this.path.length < 10) break;
      }
      this.waitT = 0.7;
    }
  }

  updateChase(dt) {
    const w = this.world;
    const f = w.ferret;
    this.act = null;
    this.repathT -= dt;
    const target = this.lostT > 0 ? this.lastSeen : { x: f.x, y: f.y };
    if (this.repathT <= 0 || !this.path || this.path.length === 0) {
      this.repathT = 0.25;
      const t = nearestFree(w.level, Math.floor(target.x / TILE), Math.floor(target.y / TILE), F.BLOCK_OWNER) ?? { x: this.tx, y: this.ty };
      this.goTo(t.x, t.y);
    }
    const d = Math.hypot(f.x - this.x, f.y - this.y);
    if (this.path.length === 0 || (d < TILE * 1.2 && this.lostT === 0)) {
      // Último tramo directo hacia el hurón.
      if (this.lostT === 0 && w.ferretVisibility() > 0 && d > 2) {
        const sp = this.chaseSpeed() * dt;
        const nx = this.x + ((f.x - this.x) / d) * sp;
        const ny = this.y + ((f.y - this.y) / d) * sp;
        if (!(w.level.flags[Math.floor(ny / TILE) * w.level.w + Math.floor(nx / TILE)] & F.BLOCK_OWNER)) {
          this.x = nx;
          this.y = ny;
          this.moving = true;
        }
        this.turnTo(angleTo(this.x, this.y, f.x, f.y), dt);
      } else {
        this.moving = false;
      }
    } else {
      this.follow(dt, this.chaseSpeed());
    }
    if (d < CATCH_DIST && w.ferretVisibility() > 0) {
      this.state = 'catching';
      this.bubble = null;
      this.pauseT = 1.4;
      this.path = null;
      this.moving = false;
      this.say(this.line('caught', ['¡Al corralito!', '¡Te tengo, bicho!']));
      w.catchFerret(this);
    }
  }

  updateTask(dt) {
    const w = this.world;
    const t = this.task;
    if (!t) return this.backToRoutine();
    if (t.phase === 'go') {
      if (t.kind === 'tidy') {
        const it = w.itemByUid(t.item);
        if (!it || it.state !== 'world' || it.container) return this.backToRoutine();
      }
      if (this.follow(dt, this.walkSpeed())) {
        t.phase = 'act';
        this.waitT = t.kind === 'sofa' ? 2.2 : t.kind === 'tv' ? 0 : 1;
        this.act = t.kind === 'sofa' ? 'search' : 'pickup';
      }
      return;
    }
    this.moving = false;
    this.waitT -= dt;
    if (t.kind === 'tv') {
      this.state = 'tv';
      this.act = 'sit';
      const tv = w.levelDef.tv;
      this.tvAngle = angleTo(this.x, this.y, tileCenter(tv.x), tileCenter(tv.y));
      return;
    }
    if (t.kind === 'sofa') this.turnTo(angleTo(this.x, this.y, w.sofaCenter().x, w.sofaCenter().y), dt);
    if (this.waitT > 0) return;
    if (t.kind === 'tidy') {
      const it = w.itemByUid(t.item);
      if (it && it.state === 'world' && !it.container) w.tidyItem(this, it);
      this.tidyCd = 3;
    } else if (t.kind === 'sofa') {
      w.checkSofa(this);
    }
    this.backToRoutine();
  }
}
