// A* sobre la rejilla de casillas, 8 direcciones sin cortar esquinas.
import { flagAt } from './grid.js';

const DIRS = [
  [1, 0, 1], [-1, 0, 1], [0, 1, 1], [0, -1, 1],
  [1, 1, Math.SQRT2], [1, -1, Math.SQRT2], [-1, 1, Math.SQRT2], [-1, -1, Math.SQRT2],
];

class Heap {
  constructor() { this.a = []; }
  get size() { return this.a.length; }
  push(n) {
    const a = this.a;
    a.push(n);
    let i = a.length - 1;
    while (i > 0) {
      const p = (i - 1) >> 1;
      if (a[p].f <= a[i].f) break;
      [a[p], a[i]] = [a[i], a[p]];
      i = p;
    }
  }
  pop() {
    const a = this.a;
    const top = a[0];
    const last = a.pop();
    if (a.length) {
      a[0] = last;
      let i = 0;
      for (;;) {
        const l = 2 * i + 1;
        const r = l + 1;
        let m = i;
        if (l < a.length && a[l].f < a[m].f) m = l;
        if (r < a.length && a[r].f < a[m].f) m = r;
        if (m === i) break;
        [a[m], a[i]] = [a[i], a[m]];
        i = m;
      }
    }
    return top;
  }
}

/**
 * Devuelve la lista de casillas [{x,y}] desde start (excluida) hasta goal, o null.
 * blockMask: banderas que impiden el paso. Si la meta está bloqueada, busca la casilla libre más cercana.
 */
export function findPath(level, sx, sy, gx, gy, blockMask, maxNodes = 4000) {
  const { w, h } = level;
  const free = (x, y) => x >= 0 && y >= 0 && x < w && y < h && !(flagAt(level, x, y) & blockMask);
  if (!free(gx, gy)) {
    const alt = nearestFree(level, gx, gy, blockMask);
    if (!alt) return null;
    gx = alt.x;
    gy = alt.y;
  }
  if (sx === gx && sy === gy) return [];
  const key = (x, y) => y * w + x;
  const g = new Map();
  const came = new Map();
  const open = new Heap();
  const hfn = (x, y) => {
    const dx = Math.abs(x - gx);
    const dy = Math.abs(y - gy);
    return dx + dy + (Math.SQRT2 - 2) * Math.min(dx, dy);
  };
  g.set(key(sx, sy), 0);
  open.push({ x: sx, y: sy, f: hfn(sx, sy) });
  let expanded = 0;
  while (open.size) {
    const cur = open.pop();
    const ck = key(cur.x, cur.y);
    if (cur.x === gx && cur.y === gy) {
      const path = [];
      let k = ck;
      while (k !== key(sx, sy)) {
        path.push({ x: k % w, y: Math.floor(k / w) });
        k = came.get(k);
      }
      return path.reverse();
    }
    if (++expanded > maxNodes) return null;
    const cg = g.get(ck);
    for (const [dx, dy, cost] of DIRS) {
      const nx = cur.x + dx;
      const ny = cur.y + dy;
      if (!free(nx, ny)) continue;
      if (dx && dy && (!free(cur.x + dx, cur.y) || !free(cur.x, cur.y + dy))) continue;
      const nk = key(nx, ny);
      const ng = cg + cost;
      if (ng < (g.get(nk) ?? Infinity)) {
        g.set(nk, ng);
        came.set(nk, ck);
        open.push({ x: nx, y: ny, f: ng + hfn(nx, ny) });
      }
    }
  }
  return null;
}

export function nearestFree(level, tx, ty, blockMask, maxR = 6) {
  for (let r = 0; r <= maxR; r++) {
    let best = null;
    let bd = Infinity;
    for (let y = ty - r; y <= ty + r; y++) {
      for (let x = tx - r; x <= tx + r; x++) {
        if (Math.max(Math.abs(x - tx), Math.abs(y - ty)) !== r) continue;
        if (x < 0 || y < 0 || x >= level.w || y >= level.h) continue;
        if (flagAt(level, x, y) & blockMask) continue;
        const d = (x - tx) ** 2 + (y - ty) ** 2;
        if (d < bd) { bd = d; best = { x, y }; }
      }
    }
    if (best) return best;
  }
  return null;
}
