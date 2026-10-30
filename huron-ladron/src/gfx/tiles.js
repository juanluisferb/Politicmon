// Suelos, paredes y puertas (16x16; las paredes miden 16x24 porque asoman 8 px hacia arriba).
import { Pix } from './pix.js';
import { mdPal } from './palette.js';

export const WALLPAPERS = {
  1: mdPal({ wp: '#e8c898', wq: '#d0a878' }),
  2: mdPal({ wp: '#a8d8b8', wq: '#88b898' }),
  3: mdPal({ wp: '#c8b0e0', wq: '#a890c8' }),
};

export const TILE_PAL = mdPal({
  m: '#c08850', M: '#8a5a30', n: '#a87040', i: '#d8a060',
  a: '#e8e8e0', A: '#a8a8b0', w: '#ffffff', b: '#a8c8e8',
  f: '#c04040', F: '#902c2c', y: '#e8b040',
  z: '#3a2c48', Z: '#5a4870', k: '#1c1420', q: '#5c3818', s: '#e0d8c0',
});

export function woodTile(v) {
  const p = new Pix(16, 16);
  p.rect(0, 0, 16, 16, v % 2 ? 'n' : 'm');
  for (let y = 0; y < 16; y += 4) {
    p.hline(0, y + 3, 16, 'M');
    const j = (y / 4 + v) % 2 ? 5 : 11;
    p.vline(j, y, 3, 'M');
    p.set((j + 4 + v * 3) % 16, y + 1, 'i');
  }
  return p;
}

export function tileTile(v) {
  const p = new Pix(16, 16);
  p.rect(0, 0, 8, 8, 'a');
  p.rect(8, 8, 8, 8, 'a');
  p.rect(8, 0, 8, 8, v ? 'b' : 'w');
  p.rect(0, 8, 8, 8, v ? 'b' : 'w');
  p.hline(0, 15, 16, 'A');
  p.vline(15, 0, 16, 'A');
  p.hline(0, 7, 16, 'A');
  p.vline(7, 0, 16, 'A');
  return p;
}

export function carpetTile(v) {
  const p = new Pix(16, 16);
  p.rect(0, 0, 16, 16, 'f');
  for (let i = 0; i < 8; i++) {
    p.set(8 + i - 4, 4 + Math.abs(i - 4) - 0, 'F');
    p.set(8 + i - 4, 12 - Math.abs(i - 4), 'F');
  }
  p.set(8, 8, 'y');
  if (v) { p.set(0, 0, 'y'); p.set(15, 15, 'y'); }
  return p;
}

/** Pared de 16x24. front = la casilla de abajo no es pared (se ve la cara con papel pintado). */
export function wallSprite(front) {
  const p = new Pix(16, 24);
  if (!front) {
    p.rect(0, 0, 16, 24, 'z');
    p.hline(0, 0, 16, 'Z');
    return p;
  }
  p.rect(0, 0, 16, 8, 'z');
  p.hline(0, 0, 16, 'Z');
  p.hline(0, 7, 16, 'k');
  p.rect(0, 8, 16, 16, 'wp');
  for (let x = 1; x < 16; x += 4) p.vline(x, 8, 13, 'wq');
  p.rect(0, 21, 16, 3, 'q');
  p.hline(0, 21, 16, 's');
  return p;
}

/** Puerta: abierta (solo dintel) o cerrada (hoja de madera). */
export function doorSprite(closed) {
  const p = new Pix(16, 24);
  p.rect(0, 0, 16, 8, 'z');
  p.hline(0, 0, 16, 'Z');
  p.hline(0, 7, 16, 'k');
  if (closed) {
    p.rect(1, 8, 14, 16, 'n');
    p.box(3, 10, 10, 5, 'm', 'M');
    p.box(3, 16, 10, 6, 'm', 'M');
    p.set(12, 15, 'y');
    p.vline(0, 8, 16, 'q');
    p.vline(15, 8, 16, 'q');
  } else {
    p.vline(0, 8, 16, 'q');
    p.vline(15, 8, 16, 'q');
  }
  return p;
}
