// Genera todas las texturas del juego en el arranque (no hay ni un PNG en el proyecto).
import Phaser from 'phaser';
import { ferretFrames, FERRET_PALETTES, FERRET_FRAME, humanFrames, HUMAN_PALETTES, HUMAN_W, HUMAN_H } from './characters.js';
import { itemIcon } from './items.js';
import { furnitureSprite } from './furniture.js';
import { woodTile, tileTile, carpetTile, wallSprite, doorSprite, TILE_PAL, WALLPAPERS } from './tiles.js';
import { fontCanvas, FONT_CHARS, FONT_W, FONT_H } from './font.js';
import { PAL, md } from './palette.js';
import { Pix, makeCanvas } from './pix.js';
import { ITEMS } from '../data/items.js';
import { FURNITURE } from '../sim/furniture.js';
import { FERRET_ORDER } from '../data/ferrets.js';

function addSheet(scene, key, frames, pal) {
  const fw = frames[0].w;
  const fh = frames[0].h;
  const c = makeCanvas(fw * frames.length, fh);
  const ctx = c.getContext('2d');
  frames.forEach((f, i) => f.draw(ctx, pal, i * fw, 0));
  const tex = scene.textures.addCanvas(key, c);
  frames.forEach((_, i) => tex.add(i, 0, i * fw, 0, fw, fh));
  return tex;
}

function addPix(scene, key, pix, pal) {
  const c = makeCanvas(pix.w, pix.h);
  pix.draw(c.getContext('2d'), pal);
  scene.textures.addCanvas(key, c);
}

function circle(scene, key, r, fill, border, inner) {
  const p = new Pix(r * 2 + 2, r * 2 + 2);
  p.ellipse(r + 1, r + 1, r + 0.5, r + 0.5, 'b');
  p.ellipse(r + 1, r + 1, r - 0.5, r - 0.5, 'f');
  if (inner) p.ellipse(r + 1, r - 1, r * 0.6, r * 0.45, 'h');
  addPix(scene, key, p, { b: border, f: fill, h: inner });
}

export function generateTextures(scene) {
  // Personajes ------------------------------------------------------------
  for (const id of FERRET_ORDER) {
    const fr = ferretFrames(id);
    addSheet(scene, `ferret_${id}`, [...fr.down, ...fr.up, ...fr.side], FERRET_PALETTES[id]);
  }
  for (const skin of ['owner', 'guest']) {
    const fr = humanFrames(skin);
    addSheet(scene, `human_${skin}`, [...fr.down, ...fr.up, ...fr.side, fr.sit, fr.phone], HUMAN_PALETTES[skin]);
  }
  // Objetos -----------------------------------------------------------------
  for (const [id, def] of Object.entries(ITEMS)) {
    const { pix, extra } = itemIcon(def.icon, def.tint);
    addPix(scene, `item_${id}`, pix, { ...PAL, ...extra });
  }
  // Muebles -----------------------------------------------------------------
  for (const [type, def] of Object.entries(FURNITURE)) {
    addPix(scene, `furn_${type}`, furnitureSprite(type, def.w, def.h).pix, PAL);
  }
  addPix(scene, 'furn_tv_on', furnitureSprite('tv', 2, 1, 'on').pix, PAL);
  // Suelos y paredes ---------------------------------------------------------
  for (let v = 0; v < 4; v++) addPix(scene, `floor_wood_${v}`, woodTile(v), TILE_PAL);
  for (let v = 0; v < 2; v++) addPix(scene, `floor_tile_${v}`, tileTile(v), TILE_PAL);
  for (let v = 0; v < 2; v++) addPix(scene, `floor_carpet_${v}`, carpetTile(v), TILE_PAL);
  for (const lvl of Object.keys(WALLPAPERS)) {
    const pal = { ...TILE_PAL, ...WALLPAPERS[lvl] };
    addPix(scene, `wall_front_${lvl}`, wallSprite(true), pal);
  }
  addPix(scene, 'wall_top', wallSprite(false), TILE_PAL);
  addPix(scene, 'door_open', doorSprite(false), TILE_PAL);
  addPix(scene, 'door_closed', doorSprite(true), TILE_PAL);

  // Interfaz ------------------------------------------------------------------
  const px = makeCanvas(1, 1);
  const pctx = px.getContext('2d');
  pctx.fillStyle = '#fff';
  pctx.fillRect(0, 0, 1, 1);
  scene.textures.addCanvas('px', px);

  const star = new Pix(9, 9).art([
    '....o....', '...oyo...', 'oooyyyooo', 'oyyyyyyyo', '.oyyyyyo.', '..oyyyo..', '.oyyoyyo.', '.oyo.oyo.', '.oo...oo.',
  ]);
  addPix(scene, 'star', star, { o: md('#3a2408'), y: md('#f8d030') });
  addPix(scene, 'star_empty', star, { o: md('#202030'), y: md('#505068') });

  const home = new Pix(11, 10).art([
    '.....o.....', '....oro....', '...orrro...', '..orrrrro..', '.orrrrrrro.', 'ooowwwwwooo', '..ow.oowo..', '..ow.oowo..', '..owwoowo..', '..ooooooo..',
  ]);
  addPix(scene, 'icon_home', home, { o: md('#20141c'), r: md('#e04030'), w: md('#f8e8b0') });
  const eye = new Pix(11, 7).art([
    '...ooooo...', '.oowwwwwoo.', 'owwwkkkwwwo', 'owwkkhkkwwo', 'owwwkkkwwwo', '.oowwwwwoo.', '...ooooo...',
  ]);
  addPix(scene, 'icon_eye', eye, { o: md('#20141c'), w: md('#f8f8f8'), k: md('#2050a0'), h: md('#ffffff') });
  const bolt = new Pix(7, 9).art(['...ooo.', '..oyyo.', '.oyyo..', 'oyyyyyo', 'ooyyyo.', '..oyo..', '.oyo...', '.oo....', 'o......']);
  addPix(scene, 'icon_bolt', bolt, { o: md('#20141c'), y: md('#f8d030') });
  const note = new Pix(8, 8).art(['....ooo.', '...owwo.', '...oo.o.', '...o..o.', '.ooo..o.', 'owwo.oo.', 'owwo.o..', '.oo.....']);
  addPix(scene, 'icon_noise', note, { o: md('#20141c'), w: md('#f8f8f8') });
  const zzz = new Pix(9, 9).art(['....ooooo', '.......o.', '......o..', '.....ooooo', 'oooo.....', '..o......', '.o.......', 'oooo.....', '.........']);
  addPix(scene, 'icon_zzz', zzz, { o: md('#f8f8f8') });
  const sparkle = new Pix(7, 7).art(['...w...', '...w...', '..wyw..', 'wwyyyww', '..wyw..', '...w...', '...w...']);
  addPix(scene, 'sparkle', sparkle, { w: md('#ffffff'), y: md('#f8f0a0') });
  const arrow = new Pix(9, 6).art(['ooooooooo', 'oyyyyyyyo', '.oyyyyyo.', '..oyyyo..', '...oyo...', '....o....']);
  addPix(scene, 'icon_arrow', arrow, { o: md('#20141c'), y: md('#f8d030') });

  // Controles táctiles.
  circle(scene, 'touch_base', 30, 'rgba(255,255,255,0.10)', 'rgba(255,255,255,0.45)');
  circle(scene, 'touch_knob', 13, 'rgba(255,255,255,0.45)', 'rgba(255,255,255,0.85)');
  circle(scene, 'btn_a', 17, md('#d83838'), md('#601010'), md('#f07070'));
  circle(scene, 'btn_b', 13, md('#3868d8'), md('#102060'), md('#78a0f0'));
  circle(scene, 'btn_run', 12, md('#d89030'), md('#603810'), md('#f0c070'));
  circle(scene, 'btn_sneak', 12, md('#40a050'), md('#104018'), md('#80d088'));
  circle(scene, 'btn_small', 9, md('#505068'), md('#181828'), md('#8888a0'));

  // Fuente bitmap.
  scene.textures.addCanvas('font_src', fontCanvas());
  const cfg = {
    image: 'font_src', width: FONT_W, height: FONT_H, chars: FONT_CHARS,
    charsPerRow: FONT_CHARS.length, spacing: { x: 0, y: 0 }, offset: { x: 0, y: 0 }, lineSpacing: 2,
  };
  scene.cache.bitmapFont.add('font', Phaser.GameObjects.RetroFont.Parse(scene, cfg));
}

export { FERRET_FRAME, HUMAN_W, HUMAN_H };
