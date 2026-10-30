// Jugador automático sencillo para tests y balance: va a por lo más rentable, lo esconde en la
// madriguera, va en sigilo cuando el dueño está cerca, huye bajo la cama si le persiguen.
import { TILE, F, flagAt, furnitureRect, tileCenter, lineOfSight } from './grid.js';
import { findPath } from './path.js';
import { FURNITURE } from './furniture.js';
import { ITEMS } from '../data/items.js';

export class Bot {
  constructor(world, opts = {}) {
    this.w = world;
    this.path = null;
    this.goal = null;
    this.repathT = 0;
    this.useSofa = opts.useSofa ?? false;
    this.stuckT = 0;
    this.lastPos = null;
    this.mash = false;
    this.skip = new Map();
    this.smart = opts.smart ?? true;
  }

  tile() {
    const f = this.w.ferret;
    return { x: Math.floor(f.x / TILE), y: Math.floor(f.y / TILE) };
  }

  pathTo(tx, ty) {
    const t = this.tile();
    return findPath(this.w.level, t.x, t.y, tx, ty, F.BLOCK_FERRET, 20000);
  }

  /** Elige el siguiente objetivo: {x, y, kind, ref}. */
  chooseGoal() {
    const w = this.w;
    const f = w.ferret;
    const full = f.carried.length > 0 && !w.canCarry({ size: 's' });
    if (f.carried.length && (full || w.time > w.duration - 40)) return this.stashGoal();
    const t = this.tile();
    let best = null;
    let bestScore = -Infinity;
    const consider = (x, y, value, kind, ref) => {
      if ((this.skip.get(`${kind}:${ref}`) ?? 0) > w.time) return;
      const p = findPath(w.level, t.x, t.y, x, y, F.BLOCK_FERRET, 20000);
      if (!p) return;
      const s = value / (p.length + 6);
      if (s > bestScore) { bestScore = s; best = { x, y, kind, ref }; }
    };
    for (const it of w.items) {
      if (it.state !== 'world' || it.container) continue;
      const def = ITEMS[it.id];
      if (!w.canCarry(def) && !def.instant) continue;
      consider(Math.floor(it.x / TILE), Math.floor(it.y / TILE), def.value + (def.instant ? 10 : 0) + (def.pair ? 8 : 0), 'item', it.uid);
    }
    if (w.canCarry({ size: 's' })) {
      for (const c of w.level.furniture) {
        if (!FURNITURE[c.type].container) continue;
        const inside = w.containerItems(c);
        if (!inside.length) continue;
        const spot = this.adjacentSpot(c);
        if (spot) consider(spot.x, spot.y, ITEMS[inside[0].id].value * 0.8, 'container', c.uid);
      }
    }
    if (!best && f.carried.length) return this.stashGoal();
    if (!best) return { ...this.burrowTile(), kind: 'end' };
    return best;
  }

  /** Casilla de escondite (bajo mesa/cama) más cercana por camino. */
  nearestHide() {
    const lv = this.w.level;
    const t = this.tile();
    let best = this.burrowTile();
    let bl = Infinity;
    for (let y = 0; y < lv.h; y++) {
      for (let x = 0; x < lv.w; x++) {
        const fl = flagAt(lv, x, y);
        if (!(fl & F.HIDE) || fl & F.BLOCK_FERRET) continue;
        if (Math.abs(x - t.x) + Math.abs(y - t.y) > 14) continue;
        const p = findPath(lv, t.x, t.y, x, y, F.BLOCK_FERRET, 4000);
        if (p && p.length < bl) { bl = p.length; best = { x, y }; }
      }
    }
    return best;
  }

  burrowTile() { return { x: this.w.burrow.x, y: this.w.burrow.y }; }

  stashGoal() {
    if (this.useSofa && this.w.tier === 0) {
      const s = this.w.sofaSpot();
      return { x: s.x, y: s.y - 0, kind: 'stash' };
    }
    return { ...this.burrowTile(), kind: 'stash' };
  }

  adjacentSpot(c) {
    const r = furnitureRect(c);
    const lv = this.w.level;
    const t = this.tile();
    let best = null;
    let bl = Infinity;
    for (let y = r.y - 1; y <= r.y + r.h; y++) {
      for (let x = r.x - 1; x <= r.x + r.w; x++) {
        const edge = x === r.x - 1 || x === r.x + r.w || y === r.y - 1 || y === r.y + r.h;
        const diag = (x === r.x - 1 || x === r.x + r.w) && (y === r.y - 1 || y === r.y + r.h);
        if (!edge || diag || flagAt(lv, x, y) & F.BLOCK_FERRET) continue;
        const p = findPath(lv, t.x, t.y, x, y, F.BLOCK_FERRET, 20000);
        if (p && p.length < bl) { bl = p.length; best = { x, y }; }
      }
    }
    return best;
  }

  danger() {
    const w = this.w;
    const f = w.ferret;
    let near = false;
    let chased = false;
    let watched = false;
    for (const o of w.owners) {
      if (o.state === 'chase') chased = true;
      const d = Math.hypot(o.x - f.x, o.y - f.y);
      if (d < 6 * TILE && lineOfSight(w.level, o.x, o.y, f.x, f.y)) {
        near = true;
        const c = ((f.x - o.x) * Math.cos(o.angle) + (f.y - o.y) * Math.sin(o.angle)) / Math.max(d, 1);
        if (c > 0.5 && d > 2 * TILE && o.detect > 0.15) watched = true;
      }
    }
    return { near, chased, watched };
  }

  input(dt) {
    const w = this.w;
    const f = w.ferret;
    const inp = { mx: 0, my: 0, run: false, sneak: false, a: false, b: false };
    if (f.state === 'caught') {
      this.mash = !this.mash;
      inp.a = this.mash;
      this.path = null;
      return inp;
    }
    if (f.state !== 'free') return inp;
    const { near, chased, watched } = this.danger();
    // Si le están mirando, se queda quieto en sigilo (como haría un jugador prudente).
    if (this.smart && watched && !chased && (this.freezeT = (this.freezeT ?? 0) + dt) < 3) {
      inp.sneak = true;
      return inp;
    }
    if (!watched) this.freezeT = 0;

    if (chased && this.goal?.kind !== 'flee') {
      this.goal = { ...this.nearestHide(), kind: 'flee' };
      this.path = this.pathTo(this.goal.x, this.goal.y);
    }
    if (!chased && this.goal?.kind === 'flee') this.goal = null;

    this.repathT -= dt;
    if (!this.goal || !this.path || this.repathT <= 0) {
      if (!this.goal || this.goal.kind !== 'flee') this.goal = this.chooseGoal();
      this.path = this.goal ? this.pathTo(this.goal.x, this.goal.y) : null;
      this.repathT = 1.5;
    }

    // ¿Ha llegado? Intenta la acción correspondiente.
    const h = f.hint;
    if (this.goal) {
      const t = this.tile();
      const atGoal = this.path && this.path.length === 0 && t.x === this.goal.x && t.y === this.goal.y;
      if (h && (h.kind === 'deposit' && this.goal.kind !== 'flee'
        || (h.kind === 'pickup' && (this.goal.kind === 'item' || ITEMS[h.item.id].instant))
        || (h.kind === 'search' && this.goal.kind === 'container' && h.container.uid === this.goal.ref))) {
        inp.a = true;
        this.goal = null;
        this.path = null;
        return inp;
      }
      if (h && h.kind === 'end' && this.goal.kind === 'end') {
        inp.a = true;
        return inp;
      }
      if (atGoal && this.goal.kind !== 'flee' && this.goal.kind !== 'end') {
        // Llegó pero no hay acción posible: descarta este objetivo un rato.
        this.skip.set(`${this.goal.kind}:${this.goal.ref}`, w.time + 20);
        this.goal = null;
        this.path = null;
        return inp;
      }
    }

    // Seguir el camino.
    if (this.path && this.path.length) {
      const n = this.path[0];
      const cx = tileCenter(n.x);
      const cy = tileCenter(n.y);
      const dx = cx - f.x;
      const dy = cy - f.y;
      const d = Math.hypot(dx, dy);
      if (d < 3) {
        this.path.shift();
      } else {
        inp.mx = dx / d;
        inp.my = dy / d;
      }
    } else if (this.goal) {
      const dx = tileCenter(this.goal.x) - f.x;
      const dy = tileCenter(this.goal.y) - f.y;
      const d = Math.hypot(dx, dy);
      if (d > 2) { inp.mx = dx / d; inp.my = dy / d; }
    }
    if (chased) inp.run = f.energy > 5;
    else if (near) inp.sneak = true;

    // Atascos: si no avanza en 2 s, recalcula.
    if (this.lastPos && Math.hypot(this.lastPos.x - f.x, this.lastPos.y - f.y) < 0.2 && (inp.mx || inp.my)) {
      this.stuckT += dt;
      if (this.stuckT > 2) {
        this.stuckT = 0;
        if (this.goal) this.skip.set(`${this.goal.kind}:${this.goal.ref}`, w.time + 15);
        this.goal = null;
        this.path = null;
      }
    } else {
      this.stuckT = 0;
    }
    this.lastPos = { x: f.x, y: f.y };
    return inp;
  }
}

/** Juega un nivel completo con el bot. Devuelve el resultado del mundo. */
export function playLevel(World, levelDef, ferretId, opts = {}) {
  const w = new World(levelDef, ferretId, { seed: opts.seed ?? 7 });
  const bot = new Bot(w, opts);
  const dt = 1 / 30;
  let guard = 0;
  while (!w.ended && guard++ < 30 * 60 * 20) {
    const inp = bot.input(dt);
    w.update(dt, inp);
    for (const e of w.drainEvents()) {
      if (e.type === 'askEnd') w.finish('sleep');
    }
  }
  return w.result;
}
