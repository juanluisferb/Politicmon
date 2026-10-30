// Iconos de 12x12 de los objetos robables.
import { Pix } from './pix.js';
import { TINTS, md } from './palette.js';

export const ICON = 12;

const DRAW = {
  sock(p) {
    p.rect(3, 1, 4, 7, 'c');
    p.rect(3, 6, 7, 3, 'c');
    p.rect(4, 9, 6, 1, 'C');
    p.rect(3, 1, 4, 2, 'w');
    p.set(9, 6, 'C');
    p.set(3, 8, 'C');
  },
  woolsock(p) {
    p.rect(3, 1, 4, 7, 'r');
    p.rect(3, 6, 7, 3, 'r');
    for (const y of [2, 4, 7]) p.hline(3, y, y === 7 ? 7 : 4, 'w');
    p.rect(4, 9, 6, 1, 'R');
    p.rect(3, 1, 4, 1, 'y');
  },
  keys(p) {
    p.ellipse(4, 4, 3, 3, 'y');
    p.ellipse(4, 4, 1.4, 1.4, null);
    p.rect(5, 6, 5, 2, 'Y');
    p.rect(8, 8, 1, 2, 'Y');
    p.rect(10, 6, 1, 3, 'Y');
    p.rect(2, 7, 2, 4, 's');
    p.set(4, 9, 's');
    p.set(4, 10, 's');
  },
  remote(p) {
    p.rect(4, 1, 4, 10, 'G');
    p.set(5, 2, 'r');
    p.rect(5, 4, 2, 1, 'w');
    p.rect(5, 6, 2, 1, 'g');
    p.rect(5, 8, 2, 1, 'g');
    p.set(6, 2, 'b');
  },
  treat(p) {
    p.ellipse(6, 6, 4.5, 3.5, 'n');
    p.set(4, 5, 'N');
    p.set(7, 7, 'N');
    p.set(8, 4, 'N');
    p.set(5, 4, 'e');
  },
  hairtie(p) {
    p.ellipse(6, 6, 4.4, 4.4, 'p');
    p.ellipse(6, 6, 2.2, 2.2, null);
    p.set(3, 4, 'P');
    p.set(8, 8, 'P');
  },
  cap(p) {
    p.ellipse(6, 6, 3.6, 3.6, 'b');
    p.ellipse(6, 6, 2, 2, 'B');
    p.set(5, 4, 'c');
  },
  pen(p) {
    for (let i = 0; i < 7; i++) p.rect(2 + i, 8 - i, 2, 2, 'b');
    p.rect(9, 1, 2, 2, 'B');
    p.set(2, 10, 'G');
  },
  spoon(p) {
    p.ellipse(4, 4, 2.5, 3, 's');
    for (let i = 0; i < 5; i++) p.set(6 + i, 6 + i, 'g');
    p.set(3, 3, 'w');
  },
  clothespin(p) {
    p.rect(5, 1, 3, 10, 'm');
    p.vline(6, 1, 10, 'M');
    p.rect(4, 5, 5, 1, 'g');
  },
  ball(p) {
    p.ellipse(6, 6, 4.5, 4.5, 'r');
    p.hline(2, 6, 9, 'y');
    p.set(4, 3, 'w');
  },
  coin(p) {
    p.ellipse(6, 6, 4.4, 4.4, 'y');
    p.ellipse(6, 6, 2.8, 2.8, 'Y');
    p.ellipse(6, 6, 1.8, 1.8, 'y');
    p.set(4, 3, 'w');
  },
  lipstick(p) {
    p.rect(4, 6, 4, 5, 'Y');
    p.hline(4, 6, 4, 'y');
    p.rect(5, 2, 2, 4, 'r');
    p.set(5, 1, 'r');
    p.set(6, 2, 'R');
  },
  earring(p) {
    p.ellipse(6, 3.5, 2, 2, 'y');
    p.ellipse(6, 3.5, 0.9, 0.9, null);
    p.ellipse(6, 8, 2.4, 2.8, 'c');
    p.set(5, 7, 'w');
    p.set(7, 9, 'b');
  },
  glove(p) {
    p.rect(3, 4, 6, 6, 'v');
    for (const x of [3, 5, 7]) p.rect(x, 1, 2, 3, 'v');
    p.rect(9, 5, 2, 3, 'v');
    p.rect(3, 9, 6, 2, 'V');
  },
  sunglasses(p) {
    p.ellipse(3.5, 6, 2.6, 2.2, 'k');
    p.ellipse(8.5, 6, 2.6, 2.2, 'k');
    p.hline(5, 5, 2, 'G');
    p.hline(0, 5, 1, 'G');
    p.hline(11, 5, 1, 'G');
    p.set(2, 5, 'c');
    p.set(7, 5, 'c');
  },
  phone(p) {
    p.rect(3, 1, 6, 10, 'k');
    p.rect(4, 2, 4, 7, 'c');
    p.set(4, 2, 'w');
    p.set(6, 10, 'G');
  },
  wallet(p) {
    p.rect(1, 3, 10, 7, 'N');
    p.rect(1, 3, 10, 3, 'n');
    p.set(9, 5, 'y');
    p.hline(2, 8, 8, 'M');
  },
  slipper(p) {
    p.ellipse(6, 7, 5, 3, 'b');
    p.ellipse(7, 7, 3, 1.6, 'B');
    p.ellipse(3.5, 5, 1.7, 1.7, 'w');
  },
};

export function itemIcon(icon, tint) {
  const p = new Pix(ICON, ICON);
  DRAW[icon](p);
  p.outline('o');
  const extra = {};
  if (tint) {
    extra.c = md(TINTS[tint][0]);
    extra.C = md(TINTS[tint][1]);
  }
  return { pix: p, extra };
}

export const ICON_NAMES = Object.keys(DRAW);
