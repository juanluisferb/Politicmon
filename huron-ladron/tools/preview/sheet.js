import { ferretFrames, FERRET_PALETTES, humanFrames, HUMAN_PALETTES } from '../../src/gfx/characters.js';
import { itemIcon, ICON_NAMES } from '../../src/gfx/items.js';
import { furnitureSprite } from '../../src/gfx/furniture.js';
import { FURNITURE } from '../../src/sim/furniture.js';
import { PAL } from '../../src/gfx/palette.js';
import { woodTile, tileTile, carpetTile, wallSprite, doorSprite, TILE_PAL, WALLPAPERS } from '../../src/gfx/tiles.js';
import { fontCanvas } from '../../src/gfx/font.js';

const S = 3;
const c = document.createElement('canvas');
c.width = 400; c.height = 300;
const ctx = c.getContext('2d');
ctx.fillStyle = '#8a6a48'; ctx.fillRect(0, 0, c.width, c.height);
let y = 2;
for (const id of ['sardi', 'pepis', 'chonko']) {
  const fr = ferretFrames(id);
  let x = 2;
  for (const dir of ['down', 'up', 'side']) for (const f of fr[dir].slice(0, 2)) { f.draw(ctx, FERRET_PALETTES[id], x, y); x += 22; }
  y += 22;
}
let x = 150; y = 2;
for (const name of ICON_NAMES) {
  const { pix, extra } = itemIcon(name, name === 'sock' ? 'green' : null);
  pix.draw(ctx, { ...PAL, ...extra }, x, y);
  x += 14; if (x > 390) { x = 150; y += 14; }
}
x = 2; y = 72;
let rowH = 0;
for (const type of Object.keys(FURNITURE)) {
  const d = FURNITURE[type];
  const sp = furnitureSprite(type, d.w, d.h);
  if (x + sp.pix.w > 398) { x = 2; y += rowH + 3; rowH = 0; }
  sp.pix.draw(ctx, PAL, x, y);
  x += sp.pix.w + 3; rowH = Math.max(rowH, sp.pix.h);
}
y += rowH + 4; x = 2;
const tp = { ...TILE_PAL, ...WALLPAPERS[1] };
for (const t of [woodTile(0), woodTile(1), tileTile(0), tileTile(1), carpetTile(0), carpetTile(1)]) { t.draw(ctx, tp, x, y + 8); x += 17; }
for (const t of [wallSprite(true), wallSprite(false), doorSprite(false), doorSprite(true)]) { t.draw(ctx, tp, x, y); x += 17; }
y += 28;
ctx.drawImage(fontCanvas(), 2, y);
const big = document.createElement('canvas');
big.width = c.width * S; big.height = c.height * S;
const b = big.getContext('2d');
b.imageSmoothingEnabled = false;
b.drawImage(c, 0, 0, big.width, big.height);
document.body.appendChild(big);
