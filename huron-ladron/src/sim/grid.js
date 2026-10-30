// Rejilla del nivel: tipos de suelo, paredes, puertas y muebles convertidos en banderas por casilla.
import { FURNITURE } from './furniture.js';

export const TILE = 16;

export const F = {
  BLOCK_FERRET: 1,
  BLOCK_OWNER: 2,
  BLOCK_VISION: 4,
  HIDE: 8,        // bajo una mesa/cama: el hurón no se ve
  SLIPPERY: 16,   // azulejo: el calcetín desliza más
  CARPET: 32,     // alfombra: pasos más silenciosos
  WALL: 64,
  DOOR: 128,
};

// Leyenda del mapa ASCII.
export const GROUND = {
  '#': { wall: true },
  '.': { floor: 'wood' },
  ',': { floor: 'tile' },
  '_': { floor: 'carpet' },
  'D': { floor: 'wood', door: 'open' },     // puerta siempre abierta
  'd': { floor: 'wood', door: 'closable' }, // se cierra cuando el dueño se alerta
  ' ': { void: true },
};

export function parseMap(rows) {
  const h = rows.length;
  const w = Math.max(...rows.map((r) => r.length));
  const ground = [];
  const doors = [];
  for (let y = 0; y < h; y++) {
    for (let x = 0; x < w; x++) {
      const ch = rows[y][x] ?? ' ';
      const g = GROUND[ch];
      if (!g) throw new Error(`Carácter de mapa desconocido '${ch}' en ${x},${y}`);
      ground.push(g);
      if (g.door) doors.push({ x, y, kind: g.door, closed: false, openTimer: 0 });
    }
  }
  return { w, h, ground, doors };
}

export function furnitureRect(f) {
  const def = FURNITURE[f.type];
  return { x: f.x, y: f.y, w: f.w ?? def.w, h: f.h ?? def.h };
}

/** Recalcula las banderas de todas las casillas (al mover muebles o cerrar puertas). */
export function computeFlags(level) {
  const { w, h, ground } = level;
  const flags = new Uint16Array(w * h);
  for (let i = 0; i < w * h; i++) {
    const g = ground[i];
    if (g.wall || g.void) flags[i] = F.BLOCK_FERRET | F.BLOCK_OWNER | F.BLOCK_VISION | F.WALL;
    else if (g.floor === 'tile') flags[i] = F.SLIPPERY;
    else if (g.floor === 'carpet') flags[i] = F.CARPET;
    if (g.door) flags[i] |= F.DOOR;
  }
  for (const d of level.doors) {
    if (d.closed && d.openTimer <= 0) flags[d.y * w + d.x] |= F.BLOCK_FERRET | F.BLOCK_VISION;
  }
  for (const f of level.furniture) {
    const def = FURNITURE[f.type];
    const r = furnitureRect(f);
    let bits = 0;
    if (def.blocksFerret) bits |= F.BLOCK_FERRET;
    if (def.blocksOwner) bits |= F.BLOCK_OWNER;
    if (def.blocksVision) bits |= F.BLOCK_VISION;
    if (def.hides) bits |= F.HIDE;
    for (let y = r.y; y < r.y + r.h; y++) {
      for (let x = r.x; x < r.x + r.w; x++) {
        if (x >= 0 && y >= 0 && x < w && y < h) flags[y * w + x] |= bits;
      }
    }
  }
  level.flags = flags;
  return flags;
}

export function flagAt(level, tx, ty) {
  if (tx < 0 || ty < 0 || tx >= level.w || ty >= level.h) return F.BLOCK_FERRET | F.BLOCK_OWNER | F.BLOCK_VISION | F.WALL;
  return level.flags[ty * level.w + tx];
}

export function flagAtPx(level, px, py) {
  return flagAt(level, Math.floor(px / TILE), Math.floor(py / TILE));
}

/** Línea de visión entre dos puntos en píxeles (muestreo cada 4 px). */
export function lineOfSight(level, x0, y0, x1, y1) {
  const dx = x1 - x0;
  const dy = y1 - y0;
  const dist = Math.hypot(dx, dy);
  const steps = Math.max(1, Math.ceil(dist / 4));
  const t0 = Math.floor(x1 / TILE) + Math.floor(y1 / TILE) * level.w;
  for (let i = 1; i < steps; i++) {
    const px = x0 + (dx * i) / steps;
    const py = y0 + (dy * i) / steps;
    const tx = Math.floor(px / TILE);
    const ty = Math.floor(py / TILE);
    if (tx + ty * level.w === t0) continue;
    if (flagAt(level, tx, ty) & F.BLOCK_VISION) return false;
  }
  return true;
}

/** Mueve una caja (centro x,y; semitamaño hw,hh) resolviendo colisiones por ejes. */
export function moveBox(level, x, y, hw, hh, dx, dy, blockMask) {
  const blocked = (cx, cy) => {
    const x0 = Math.floor((cx - hw) / TILE);
    const x1 = Math.floor((cx + hw - 0.001) / TILE);
    const y0 = Math.floor((cy - hh) / TILE);
    const y1 = Math.floor((cy + hh - 0.001) / TILE);
    for (let ty = y0; ty <= y1; ty++) {
      for (let tx = x0; tx <= x1; tx++) if (flagAt(level, tx, ty) & blockMask) return true;
    }
    return false;
  };
  let hitX = false;
  let hitY = false;
  // Si choca de lado contra una esquina y no hay entrada perpendicular, empuja hacia el hueco
  // (hasta 5 px) para no quedarse enganchado al entrar por puertas.
  const nudge = (tryMove, step) => {
    for (let n = 1; n <= 5; n++) {
      for (const s of [1, -1]) if (tryMove(s * n)) return s * Math.min(n, step);
    }
    return 0;
  };
  let nx = x + dx;
  if (dx !== 0 && blocked(nx, y)) {
    hitX = true;
    nx = x;
    if (dy === 0) y += nudge((o) => !blocked(x + dx, y + o) && !blocked(x, y + Math.sign(o)), Math.abs(dx));
  }
  let ny = y + dy;
  if (dy !== 0 && blocked(nx, ny)) {
    hitY = true;
    ny = y;
    if (dx === 0) nx += nudge((o) => !blocked(nx + o, y + dy) && !blocked(nx + Math.sign(o), y), Math.abs(dy));
  }
  return { x: nx, y: ny, hitX, hitY };
}

export const tileCenter = (t) => t * TILE + TILE / 2;
