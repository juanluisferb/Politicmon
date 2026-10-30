// Muebles en vista 3/4: superficie superior + cara frontal. El sprite mide (w*16) x (h*16 + tall).
import { Pix } from './pix.js';
import { FURNITURE } from '../sim/furniture.js';

const T = 16;

function base(type, w, h) {
  const def = FURNITURE[type];
  const W = w * T;
  const H = h * T + def.tall;
  return { p: new Pix(W, H), W, H, tall: def.tall, foot: h * T };
}

const DRAW = {
  sofa({ p, W, H }) {
    p.rect(0, 0, W, 16, 'F');            // respaldo
    p.hline(1, 1, W - 2, 'f');
    p.rect(4, 12, W - 8, 18, 'f');        // asiento
    for (let x = 4 + 13; x < W - 8; x += 13) p.vline(x, 13, 16, 'F');
    p.rect(0, 8, 6, H - 8, 'F');          // brazos
    p.rect(W - 6, 8, 6, H - 8, 'F');
    p.hline(1, 9, 4, 'f');
    p.hline(W - 5, 9, 4, 'f');
    p.rect(6, 30, W - 12, H - 30, 'x');    // frontal
    p.hline(6, 30, W - 12, 'F');
  },
  bed({ p, W, H }) {
    p.rect(0, 0, W, H, 'M');
    p.rect(1, 1, W - 2, 6, 'q');           // cabecero
    p.rect(3, 6, W - 6, 8, 'w');           // almohada
    p.hline(4, 13, W - 8, 'W');
    p.rect(1, 15, W - 2, H - 22, 'b');     // manta
    for (let y = 18; y < H - 8; y += 6) p.hline(2, y, W - 4, 'B');
    p.rect(1, 15, W - 2, 2, 'l');
    p.rect(0, H - 6, W, 6, 'q');
  },
  table({ p, W, H, foot }) {
    const top = foot - 4;
    p.rect(0, 0, W, top, 'm');
    p.hline(1, 1, W - 2, 'i');
    p.rect(0, top, W, 3, 'M');
    p.rect(1, top + 3, 3, H - top - 3, 'q');
    p.rect(W - 4, top + 3, 3, H - top - 3, 'q');
  },
  desk({ p, W, H }) {
    p.rect(0, 0, W, 12, 'm');
    p.hline(1, 1, W - 2, 'i');
    p.rect(0, 12, W, 3, 'M');
    p.rect(1, 15, 3, H - 15, 'q');
    p.rect(W - 12, 12, 11, H - 14, 'M');
    p.hline(W - 11, 18, 9, 'q');
    p.set(W - 7, 15, 'y');
    p.set(W - 7, 21, 'y');
    p.rect(8, 2, 10, 7, 'G');               // portátil
    p.rect(9, 3, 8, 5, 'c');
  },
  chair({ p, W, H }) {
    p.rect(3, 0, W - 6, 10, 'M');
    p.rect(4, 1, W - 8, 8, 'm');
    p.rect(2, 10, W - 4, 6, 'm');
    p.rect(2, 16, 2, H - 16, 'q');
    p.rect(W - 4, 16, 2, H - 16, 'q');
  },
  counter({ p, W, H }) {
    p.rect(0, 0, W, 14, 'a');
    p.hline(0, 0, W, 'w');
    p.rect(0, 14, W, H - 14, 'm');
    p.box(2, 16, W - 4, H - 18, 'm', 'M');
    p.set(W - 5, 21, 'y');
  },
  stove({ p, W, H }) {
    DRAW.counter({ p, W, H });
    p.rect(1, 1, W - 2, 12, 'G');
    p.ellipse(5, 5, 2.4, 2, 'k');
    p.ellipse(11, 9, 2.4, 2, 'k');
    p.set(5, 5, 'r');
    p.box(2, 16, W - 4, H - 18, 'G', 'k');
  },
  fridge({ p, W, H }) {
    p.rect(0, 0, W, H, 'w');
    p.rect(0, 0, W, 10, 'a');
    p.hline(0, 22, W, 'A');
    p.vline(W - 3, 12, 8, 'g');
    p.vline(W - 3, 25, 10, 'g');
    p.rect(3, 13, 4, 3, 'r');               // imán
  },
  wardrobe({ p, W, H }) {
    p.rect(0, 0, W, H, 'M');
    p.rect(0, 0, W, 10, 'm');
    p.box(1, 11, W / 2 - 1, H - 12, 'm', 'q');
    p.box(W / 2, 11, W / 2 - 1, H - 12, 'm', 'q');
    p.set(W / 2 - 3, 24, 'y');
    p.set(W / 2 + 2, 24, 'y');
  },
  bookshelf({ p, W, H }) {
    p.rect(0, 0, W, H, 'q');
    p.rect(0, 0, W, 10, 'M');
    const cols = ['r', 'b', 'y', 'v', 'u', 't', 'c', 'p'];
    let k = 0;
    for (let y = 12; y + 8 <= H - 2; y += 9) {
      p.hline(1, y + 8, W - 2, 'M');
      for (let x = 2; x < W - 3; x += 3) {
        const hgt = 5 + ((x * 7 + y) % 3);
        p.rect(x, y + 8 - hgt, 2, hgt, cols[k++ % cols.length]);
      }
    }
  },
  tv({ p, W, H }) {
    p.rect(0, 16, W, H - 16, 'M');          // mueble
    p.rect(0, 16, W, 3, 'm');
    p.hline(2, 24, W - 4, 'q');
    p.rect(3, 0, W - 6, 15, 'k');           // pantalla
    p.rect(5, 2, W - 10, 11, 'z');
    p.rect(W / 2 - 2, 15, 4, 2, 'G');
  },
  plant({ p }) {
    p.rect(4, 18, 8, 11, 't');
    p.hline(3, 18, 10, 'T');
    p.ellipse(8, 10, 6, 6, 'v');
    p.ellipse(5, 7, 3, 4, 'V');
    p.ellipse(11, 12, 3, 3, 'V');
    p.ellipse(9, 5, 2.5, 3, 'v');
  },
  drawer({ p, W, H }) {
    p.rect(0, 0, W, 12, 'm');
    p.hline(1, 1, W - 2, 'i');
    p.rect(0, 12, W, H - 12, 'M');
    p.box(2, 14, W - 4, 6, 'm', 'q');
    p.box(2, 20, W - 4, 6, 'm', 'q');
    p.set(W / 2, 16, 'y');
    p.set(W / 2, 22, 'y');
    p.rect(4, 3, 6, 6, 'w');                // portarretratos
    p.rect(5, 4, 4, 4, 'l');
  },
  nightstand({ p, W, H }) {
    p.rect(0, 0, W, 12, 'm');
    p.rect(0, 12, W, H - 12, 'M');
    p.box(2, 14, W - 4, 6, 'm', 'q');
    p.set(W / 2, 16, 'y');
    p.ellipse(8, 5, 4, 3, 'y');             // lámpara
    p.rect(7, 7, 2, 3, 'G');
  },
  basket({ p, W, H }) {
    p.rect(1, 4, W - 2, H - 4, 'E');
    for (let x = 2; x < W - 2; x += 3) p.vline(x, 8, H - 9, 'e');
    p.hline(1, H - 1, W - 2, 'I');
    p.ellipse(5, 5, 3.4, 3, 'r');
    p.ellipse(10, 4, 3.4, 3, 'b');
    p.ellipse(8, 7, 3.4, 2.2, 'y');
    p.ellipse(12, 7, 2.4, 2, 'v');
  },
  coatrack({ p, H }) {
    p.vline(7, 2, H - 4, 'q');
    p.vline(8, 2, H - 4, 'M');
    p.rect(3, H - 3, 10, 3, 'q');
    p.rect(3, 8, 10, 18, 'u');              // abrigo colgado
    p.rect(3, 8, 10, 3, 'U');
    p.vline(8, 11, 15, 'U');
    p.set(6, 16, 'y');
    p.set(6, 20, 'y');
    p.ellipse(8, 4, 5, 2.5, 'n');           // sombrero
    p.hline(5, 2, 6, 'N');
  },
  bag({ p, W, H }) {
    p.ellipse(8, 6, 4.5, 4.5, 'N');
    p.ellipse(8, 6, 3, 3, null);
    p.rect(2, 7, W - 4, H - 7, 'P');
    p.rect(2, 7, W - 4, 4, 'p');
    p.set(8, 12, 'y');
  },
  washer({ p, W, H }) {
    p.rect(0, 0, W, H, 'w');
    p.rect(0, 0, W, 12, 'a');
    p.rect(2, 2, 5, 2, 'g');
    p.set(11, 3, 'r');
    p.ellipse(8, 19, 5, 5, 'A');
    p.ellipse(8, 19, 3.6, 3.6, 'L');
    p.ellipse(7, 18, 1.6, 1.6, 'l');
  },
  bathtub({ p, W, H }) {
    p.rect(0, 0, W, H, 'w');
    p.rect(3, 3, W - 6, 10, 'l');
    p.hline(3, 3, W - 6, 'L');
    p.rect(0, 16, W, H - 16, 'W');
    p.rect(W - 8, 1, 3, 2, 's');
  },
  toilet({ p, H }) {
    p.rect(3, 0, 10, 7, 'w');
    p.hline(3, 6, 10, 'W');
    p.ellipse(8, 12, 5, 4.4, 'w');
    p.ellipse(8, 11.5, 3, 2.6, 'l');
    p.rect(5, 15, 6, H - 15, 'W');
  },
  sink({ p, W, H }) {
    p.rect(0, 0, W, 14, 'w');
    p.ellipse(8, 7, 5, 4, 'l');
    p.rect(7, 1, 2, 3, 's');
    p.rect(1, 14, W - 2, H - 14, 'm');
    p.vline(8, 15, H - 16, 'M');
  },
  shoerack({ p, W, H }) {
    p.rect(0, 0, W, H, 'M');
    p.rect(0, 0, W, 10, 'm');
    p.ellipse(7, 6, 5, 3, 'b');
    p.ellipse(20, 6, 5, 3, 'r');
    p.ellipse(27, 5, 3, 2, 'w');
    p.hline(1, 13, W - 2, 'q');
  },
  playpen({ p, W, H }) {
    p.rect(1, 6, W - 2, H - 8, 'l');        // mantita
    for (let y = 9; y < H - 3; y += 4) p.hline(2, y, W - 4, 'L');
    p.rect(0, 0, W, 2, 'm');
    p.rect(0, H - 9, W, 2, 'm');
    p.rect(0, H - 2, W, 2, 'M');
    for (let x = 0; x < W; x += 4) p.vline(x, H - 9, 9, 'm');
    p.vline(0, 0, H, 'M');
    p.vline(W - 1, 0, H, 'M');
  },
  burrow({ p }) {
    p.ellipse(8, 9, 7.5, 5.5, 'E');
    p.ellipse(8, 9, 5.5, 3.6, 'u');
    p.ellipse(7, 8, 3, 1.6, 'U');
  },
  tube({ p }) {
    p.ellipse(8, 9, 7, 5.5, 'g');
    p.ellipse(8, 9, 5, 3.6, 'k');
    p.hline(3, 5, 10, 's');
  },
};

/** Devuelve {pix, tall} del mueble; para la tele hay variante encendida. */
export function furnitureSprite(type, w, h, variant) {
  const b = base(type, w, h);
  DRAW[type](b);
  if (type === 'tv' && variant === 'on') {
    b.p.rect(5, 2, b.W - 10, 11, 'c');
    b.p.rect(7, 4, 6, 4, 'w');
    b.p.rect(15, 6, 8, 5, 'y');
    b.p.hline(5, 12, b.W - 10, 'b');
  }
  // Margen de 1 px para que el contorno también rodee los bordes del sprite.
  const pad = new Pix(b.W + 2, b.H + 2);
  pad.blit(b.p, 1, 1);
  if (type !== 'burrow' && type !== 'tube') pad.outline('o');
  return { ...b, pix: pad };
}
