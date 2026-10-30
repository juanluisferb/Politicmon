// Sprites de hurones y humanos, dibujados por código con elipses y rectángulos sobre un Pix.
import { Pix } from './pix.js';
import { mdPal } from './palette.js';

export const FERRET_PALETTES = {
  sardi: mdPal({ o: '#4a3a3a', b: '#f4f0e8', B: '#d0c8b8', l: '#ffffff', m: '#e4d8cc', e: '#a01830', n: '#f0a0b0', p: '#dcd2c2', t: '#e8e0d2' }),
  pepis: mdPal({ o: '#2a160a', b: '#b08050', B: '#7a5030', l: '#f4e0b8', m: '#4a2a14', e: '#100808', n: '#d88888', p: '#3a2010', t: '#3a2010' }),
  chonko: mdPal({ o: '#1e0e06', b: '#704226', B: '#522c14', l: '#c09468', m: '#3e220e', e: '#0a0606', n: '#b46868', p: '#3a1e0c', t: '#4a2810' }),
};

export const FERRET_FRAME = 24;
const LEG_F = [1, 0, -1, 0];
const BOB = [0, -1, 0, -1];

function ferretSide(i, big) {
  const p = new Pix(FERRET_FRAME, FERRET_FRAME);
  const bob = BOB[i];
  const fo = LEG_F[i];
  const s = big ? 1 : 0;
  // Patas lejanas (oscuras) detrás del cuerpo: cortitas, como buen hurón.
  p.rect(16 - fo, 17 + bob, 2, 4 - bob, 'B');
  p.rect(7 + fo, 17 + bob, 2, 4 - bob, 'B');
  // Cola larga, cuerpo salchicha y cabeza puntiaguda.
  p.ellipse(3.6, 15.2 + bob, 3.4, 1.3 + s * 0.4, 't');
  p.ellipse(11, 15.5 + bob, 8.4 + s * 0.5, 2.9 + s * 0.8, null, (x, y, dx, dy) => (dy > 0.4 ? 'B' : 'b'));
  p.ellipse(18.6, 14 + bob, 3.3 + s * 0.3, 2.7 + s * 0.3, 'b');
  p.ellipse(21.4, 15 + bob, 2.1, 1.3, 'l');
  p.ellipse(19.2, 13.6 + bob, 2.2, 1, 'm');
  p.rect(16, 10 + bob, 2, 2, 'b');
  p.set(16, 11 + bob, 'l');
  p.set(20, 13 + bob, 'e');
  p.set(23, 15 + bob, 'n');
  // Patas cercanas.
  p.rect(17 + fo, 18 + bob, 2, 3 - bob, 'p');
  p.rect(5 - fo, 18 + bob, 2, 3 - bob, 'p');
  p.outline('o');
  if (big) p.fluff('o', 7 + i, 0.4);
  return p;
}

function ferretDown(i, big) {
  const p = new Pix(FERRET_FRAME, FERRET_FRAME);
  const bob = BOB[i];
  const s = big ? 0.8 : 0;
  const a = i === 0 ? 1 : i === 2 ? -1 : 0;
  // Lomo detrás de la cabeza (en escorzo), patas traseras asomando.
  p.ellipse(12, 9.5 + bob, 4.4 + s, 5.2 + s * 0.5, null, (x, y, dx) => (Math.abs(dx) > 0.6 ? 'B' : 'b'));
  p.rect(7 - s, 12 + bob + a, 2, 2, 'p');
  p.rect(15 + s, 12 + bob - a, 2, 2, 'p');
  // Cabeza: pelaje del color base, antifaz sobre los ojos y hocico claro pequeño.
  p.ellipse(8, 13 + bob, 1.4, 1.4, 'b');
  p.ellipse(16, 13 + bob, 1.4, 1.4, 'b');
  p.set(8, 13 + bob, 'l');
  p.set(16, 13 + bob, 'l');
  p.ellipse(12, 16 + bob, 4.3 + s * 0.4, 3.4, 'b');
  p.ellipse(12, 15.6 + bob, 4.2, 1.2, 'm');
  p.set(10, 14 + bob, 'l');
  p.set(14, 14 + bob, 'l');
  p.ellipse(12, 18.2 + bob, 2.2, 1.4, 'l');
  p.set(10, 16 + bob, 'e');
  p.set(14, 16 + bob, 'e');
  p.set(12, 18 + bob, 'n');
  p.rect(9, 19 + bob - a, 2, 2, 'p');
  p.rect(13, 19 + bob + a, 2, 2, 'p');
  p.outline('o');
  if (big) p.fluff('o', 11 + i, 0.4);
  return p;
}

function ferretUp(i, big) {
  const p = new Pix(FERRET_FRAME, FERRET_FRAME);
  const bob = BOB[i];
  const s = big ? 0.8 : 0;
  const a = i === 0 ? 1 : i === 2 ? -1 : 0;
  p.rect(8, 9 + bob - a, 2, 2, 'p');
  p.rect(14, 9 + bob + a, 2, 2, 'p');
  p.ellipse(12, 19.5, 1.5 + s * 0.5, 2.8, 't');
  p.ellipse(12, 13.5 + bob, 4.4 + s, 5.2 + s * 0.5, null, (x, y, dx) => (Math.abs(dx) > 0.6 ? 'B' : 'b'));
  p.rect(7 - s, 16 + bob + a, 2, 2, 'p');
  p.rect(15 + s, 16 + bob - a, 2, 2, 'p');
  p.ellipse(12, 8 + bob, 3.8 + s * 0.4, 3.1, 'b');
  p.ellipse(8.8, 5.8 + bob, 1.3, 1.3, 'b');
  p.ellipse(15.2, 5.8 + bob, 1.3, 1.3, 'b');
  p.set(9, 6 + bob, 'l');
  p.set(15, 6 + bob, 'l');
  p.outline('o');
  if (big) p.fluff('o', 17 + i, 0.4);
  return p;
}

/** Devuelve {down:[4], up:[4], side:[4]} fotogramas Pix de un hurón. */
export function ferretFrames(id) {
  const big = id === 'chonko';
  const out = { down: [], up: [], side: [] };
  for (let i = 0; i < 4; i++) {
    out.down.push(ferretDown(i, big));
    out.up.push(ferretUp(i, big));
    out.side.push(ferretSide(i, big));
  }
  return out;
}

// Humanos ------------------------------------------------------------------
export const HUMAN_W = 20;
export const HUMAN_H = 32;

export const HUMAN_PALETTES = {
  owner: mdPal({ o: '#20141c', k: '#f0b890', K: '#c88860', h: '#5a3418', H: '#3a2010', S: '#3a68c8', T: '#24448a', P: '#283048', Q: '#181c30', s: '#181010', e: '#181018', a: '#f0f0f0' }),
  guest: mdPal({ o: '#20141c', k: '#e8a878', K: '#b87850', h: '#d04018', H: '#902810', S: '#a040b0', T: '#682878', P: '#40284a', Q: '#28182e', s: '#801c1c', e: '#181018', a: '#f8d030' }),
};

function humanDown(i, opt) {
  const p = new Pix(HUMAN_W, HUMAN_H);
  const step = [1, 0, -1, 0][i];
  const sit = opt.sit;
  const dy = sit ? 4 : 0;
  if (!sit) {
    p.rect(6, 22, 3, 7 - Math.max(0, step), 'P');
    p.rect(11, 22, 3, 7 - Math.max(0, -step), 'P');
    p.rect(6, 29 - Math.max(0, step), 3, 2, 's');
    p.rect(11, 29 - Math.max(0, -step), 3, 2, 's');
  } else {
    p.rect(5, 24, 10, 4, 'P');
    p.rect(6, 28, 3, 2, 's');
    p.rect(11, 28, 3, 2, 's');
  }
  p.rect(5, 12 + dy, 10, 11 - (sit ? 2 : 0), 'S');
  p.rect(5, 12 + dy, 1, 10 - (sit ? 2 : 0), 'T');
  p.rect(14, 12 + dy, 1, 10 - (sit ? 2 : 0), 'T');
  if (opt.long) p.rect(5, 20 + dy, 10, 3, 'T');
  // Brazos (balanceo al andar). En "phone" el derecho sube a la oreja.
  p.rect(3, 13 + dy + step, 2, 8, 'S');
  p.rect(3, 21 + dy + step, 2, 2, 'k');
  if (opt.phone) {
    p.rect(15, 9 + dy, 2, 6, 'S');
    p.rect(14, 7 + dy, 2, 3, 'k');
    p.rect(13, 6 + dy, 2, 4, 'e');
  } else {
    p.rect(15, 13 + dy - step, 2, 8, 'S');
    p.rect(15, 21 + dy - step, 2, 2, 'k');
  }
  p.rect(8, 10 + dy, 4, 2, 'k');
  p.ellipse(10, 6.5 + dy, 4.6, 5, null, (x, y, ex, ey) => (ey < -0.15 ? 'h' : 'k'));
  if (opt.long) {
    p.rect(5, 4 + dy, 2, 10, 'h');
    p.rect(13, 4 + dy, 2, 10, 'h');
  }
  p.set(8, 7 + dy, 'e');
  p.set(12, 7 + dy, 'e');
  p.rect(9, 9 + dy, 2, 1, 'K');
  p.outline('o');
  return p;
}

function humanUp(i, opt) {
  const p = new Pix(HUMAN_W, HUMAN_H);
  const step = [1, 0, -1, 0][i];
  p.rect(6, 22, 3, 7 - Math.max(0, -step), 'P');
  p.rect(11, 22, 3, 7 - Math.max(0, step), 'P');
  p.rect(6, 29 - Math.max(0, -step), 3, 2, 's');
  p.rect(11, 29 - Math.max(0, step), 3, 2, 's');
  p.rect(5, 12, 10, 11, 'S');
  p.rect(9, 13, 2, 9, 'T');
  if (opt.long) p.rect(5, 20, 10, 3, 'T');
  p.rect(3, 13 - step, 2, 8, 'S');
  p.rect(3, 21 - step, 2, 2, 'k');
  p.rect(15, 13 + step, 2, 8, 'S');
  p.rect(15, 21 + step, 2, 2, 'k');
  p.rect(8, 10, 4, 2, 'k');
  p.ellipse(10, 6.5, 4.6, 5, null, (x, y, ex, ey) => (ey > 0.75 ? 'H' : 'h'));
  if (opt.long) p.rect(6, 8, 8, 7, 'h');
  p.outline('o');
  return p;
}

function humanSide(i, opt) {
  const p = new Pix(HUMAN_W, HUMAN_H);
  const step = [2, 0, -2, 0][i];
  // Pierna lejana, luego cercana.
  p.rect(9 - step, 22, 3, 7, 'Q');
  p.rect(9 - step, 29, 4, 2, 's');
  p.rect(9 + step, 22, 3, 7, 'P');
  p.rect(9 + step, 29, 4, 2, 's');
  p.rect(7, 12, 6, 11, 'S');
  if (opt.long) p.rect(6, 20, 8, 3, 'T');
  p.rect(9 + Math.round(step / 2), 13, 2, 8, 'T');
  p.rect(9 + Math.round(step / 2), 21, 2, 2, 'k');
  p.rect(9, 10, 3, 2, 'k');
  p.ellipse(10.5, 6.5, 4.4, 5, null, (x, y, ex, ey) => (ey < -0.15 || ex < -0.35 ? 'h' : 'k'));
  if (opt.long) p.rect(6, 5, 3, 10, 'h');
  p.set(13, 7, 'e');
  p.set(14, 9, 'K');
  p.outline('o');
  return p;
}

/** {down:[4], up:[4], side:[4], sit, phone} */
export function humanFrames(skin) {
  const opt = { long: skin === 'guest' };
  const out = { down: [], up: [], side: [] };
  for (let i = 0; i < 4; i++) {
    out.down.push(humanDown(i, opt));
    out.up.push(humanUp(i, opt));
    out.side.push(humanSide(i, opt));
  }
  out.sit = humanDown(1, { ...opt, sit: true });
  out.phone = humanDown(1, { ...opt, phone: true });
  return out;
}
