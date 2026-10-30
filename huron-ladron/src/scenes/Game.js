// Escena de juego: dibuja la simulación (World) y traduce sus eventos en sonido y efectos.
import Phaser from 'phaser';
import { World } from '../sim/world.js';
import { TILE, F, flagAt, furnitureRect, lineOfSight } from '../sim/grid.js';
import { FURNITURE } from '../sim/furniture.js';
import { ITEMS, POWER_INFO } from '../data/items.js';
import { LEVELS } from '../data/levels/index.js';
import { Controls } from '../ui/controls.js';
import { Bot } from '../sim/bot.js';
import { text, wrap } from '../ui/text.js';
import { UI } from '../gfx/palette.js';
import { sound } from '../audio/audio.js';

const HIDES = new Set(['bed', 'table', 'desk', 'chair']);
const DECALS = new Set(['burrow', 'tube']);

export class Game extends Phaser.Scene {
  constructor() { super('Game'); }

  init(data) {
    this.levelId = data.levelId ?? 1;
    this.ferretId = data.ferret ?? 'pepis';
  }

  create() {
    this.def = LEVELS.find((l) => l.id === this.levelId);
    this.world = new World(this.def, this.ferretId, { seed: (Date.now() & 0xffff) + 1 });
    this.controls = new Controls(this);
    // Modo de pruebas: ?bot=1 juega solo, ?speed=N acelera la simulación.
    const q = new URLSearchParams(window.location.search);
    this.bot = q.has('bot') ? new Bot(this.world) : null;
    this.speed = Math.max(1, Math.min(8, Number(q.get('speed')) || 1));
    this.lvW = this.world.level.w * TILE;
    this.lvH = this.world.level.h * TILE;
    this.paused = false;
    this.modal = false;
    this.endT = -1;
    this.rings = [];
    this.searchSfxT = 0;
    this.dookT = 2;
    this.toastCd = {};

    this.buildFloor();
    this.buildWalls();
    this.buildFurniture();
    this.buildItems();
    this.buildActors();
    this.cone = this.add.graphics().setDepth(1);
    this.fx = this.add.graphics().setDepth(9000);

    const cam = this.cameras.main;
    const W = this.scale.width;
    const H = this.scale.height;
    const bx = Math.min(-8, (this.lvW - W) / 2);
    const bw = Math.max(this.lvW + 16, W);
    cam.setBounds(bx, -30, bw, Math.max(this.lvH + 30 + 24, H));
    cam.startFollow(this.ferretSpr, true, 0.12, 0.12);
    cam.setRoundPixels(true);
    cam.fadeIn(300);

    this.scene.launch('Hud', { levelId: this.levelId });
    this.hud = this.scene.get('Hud');

    this.onBack = () => this.openPause();
    this.onHidden = () => this.openPause();
    this.game.events.on('androidBack', this.onBack);
    this.game.events.on('appHidden', this.onHidden);
    this.events.once('shutdown', () => {
      this.game.events.off('androidBack', this.onBack);
      this.game.events.off('appHidden', this.onHidden);
    });
    sound.play('calm');
    window.__world = this.world; // para las pruebas automáticas
  }

  // ------------------------------------------------------------------ construcción
  buildFloor() {
    const lv = this.world.level;
    const key = 'floor_level';
    if (this.textures.exists(key)) this.textures.remove(key);
    const c = document.createElement('canvas');
    c.width = this.lvW;
    c.height = this.lvH;
    const ctx = c.getContext('2d');
    ctx.fillStyle = '#100c18';
    ctx.fillRect(0, 0, c.width, c.height);
    const img = (k) => this.textures.get(k).getSourceImage();
    for (let y = 0; y < lv.h; y++) {
      for (let x = 0; x < lv.w; x++) {
        const g = lv.ground[y * lv.w + x];
        if (g.wall || g.void) continue;
        const v = (x * 7 + y * 13 + ((x * y) % 5)) % 4;
        const k = g.floor === 'tile' ? `floor_tile_${(x + y) % 2}` : g.floor === 'carpet' ? `floor_carpet_${v % 2}` : `floor_wood_${v}`;
        ctx.drawImage(img(k), x * TILE, y * TILE);
      }
    }
    // Sombra suave al pie de las paredes.
    ctx.fillStyle = 'rgba(0,0,0,0.25)';
    for (let y = 1; y < lv.h; y++) {
      for (let x = 0; x < lv.w; x++) {
        const g = lv.ground[y * lv.w + x];
        if (!g.wall && lv.ground[(y - 1) * lv.w + x].wall) ctx.fillRect(x * TILE, y * TILE, TILE, 3);
      }
    }
    this.textures.addCanvas(key, c);
    this.add.image(0, 0, key).setOrigin(0).setDepth(0);
  }

  buildWalls() {
    const lv = this.world.level;
    this.walls = [];
    for (let y = 0; y < lv.h; y++) {
      for (let x = 0; x < lv.w; x++) {
        const g = lv.ground[y * lv.w + x];
        if (!g.wall) continue;
        const below = y + 1 < lv.h ? lv.ground[(y + 1) * lv.w + x] : null;
        const front = below && !below.wall && !below.void;
        const s = this.add.image(x * TILE, (y + 1) * TILE, front ? `wall_front_${this.levelId}` : 'wall_top')
          .setOrigin(0, 1).setDepth((y + 1) * TILE);
        this.walls.push(s);
      }
    }
    this.doorSprites = lv.doors.map((d) => this.add.image(d.x * TILE, (d.y + 1) * TILE, 'door_open').setOrigin(0, 1).setDepth((d.y + 1) * TILE));
  }

  buildFurniture() {
    this.furn = this.world.level.furniture.map((f) => {
      const def = FURNITURE[f.type];
      const r = furnitureRect(f);
      const s = this.add.image(r.x * TILE - 1, (r.y + r.h) * TILE + 1, `furn_${f.type}`).setOrigin(0, 1);
      s.setDepth(DECALS.has(f.type) ? 0.5 : (r.y + r.h) * TILE - 0.5);
      let spark = null;
      if (def.container) {
        spark = this.add.image(0, 0, 'sparkle').setDepth(8000);
        this.tweens.add({ targets: spark, alpha: 0.2, duration: 500, yoyo: true, repeat: -1 });
      }
      return { f, def, s, spark };
    });
    // Marcadores: madriguera (siempre) y sofá (cuando llevas algo).
    const b = this.world.burrow;
    this.homeIcon = this.add.image(b.x * TILE + 8, b.y * TILE - 10, 'icon_home').setDepth(8500);
    this.tweens.add({ targets: this.homeIcon, y: this.homeIcon.y - 3, duration: 600, yoyo: true, repeat: -1, ease: 'Sine.inOut' });
    const sc = this.world.sofaCenter();
    this.sofaIcon = text(this, sc.x, sc.y - 30, 'x1.5', { color: UI.gold, align: 'center', depth: 8500 });
    this.tweens.add({ targets: this.sofaIcon, y: sc.y - 33, duration: 500, yoyo: true, repeat: -1 });
  }

  buildItems() {
    this.itemSprites = this.world.items.map((it) => {
      const s = this.add.image(it.x, it.y, `item_${it.id}`);
      if (ITEMS[it.id].power) {
        s.glow = this.add.image(it.x, it.y, 'sparkle').setDepth(8000).setAlpha(0.8);
        this.tweens.add({ targets: s.glow, alpha: 0.1, duration: 400, yoyo: true, repeat: -1 });
      }
      return s;
    });
  }

  buildActors() {
    const f = this.world.ferret;
    this.ferretSpr = this.add.sprite(f.x, f.y + 4, `ferret_${this.ferretId}`).setOrigin(0.5, 0.875);
    this.carrySpr = [this.add.image(0, 0, 'px').setVisible(false), this.add.image(0, 0, 'px').setVisible(false)];
    this.ownerSpr = this.world.owners.map((o) => {
      const s = this.add.sprite(o.x, o.y + 6, `human_${o.skin}`).setOrigin(0.5, 1);
      const mark = text(this, 0, 0, '', { color: UI.orange, align: 'center', depth: 9500, scale: 2 });
      const meter = this.add.graphics().setDepth(9500);
      const bubble = this.add.container(0, 0).setDepth(9600).setVisible(false);
      return { s, mark, meter, bubble, bubbleT: 0 };
    });
  }

  // ------------------------------------------------------------------ bucle
  update(time, delta) {
    if (this.paused) return;
    const dt = Math.min(delta, 50) / 1000;
    if (this.endT >= 0) {
      this.endT -= dt;
      this.syncAll(dt);
      if (this.endT < 0) this.toResults();
      return;
    }
    const inp = this.controls.read();
    if (inp.pause) {
      this.openPause();
      return;
    }
    if (this.modal) {
      this.syncAll(0);
      return;
    }
    for (let i = 0; i < this.speed && !this.world.ended && !this.modal; i++) {
      const step = this.bot ? this.bot.input(dt) : inp;
      this.world.update(dt, step);
      const evs = this.world.drainEvents();
      if (this.bot && evs.some((e) => e.type === 'askEnd')) {
        this.world.finish('sleep');
        evs.push(...this.world.drainEvents());
      }
      this.handleEvents(evs.filter((e) => !(this.bot && e.type === 'askEnd')));
    }
    this.syncAll(dt);
    this.updateMusic();
  }

  updateMusic() {
    const w = this.world;
    if (w.owners.some((o) => o.state === 'chase')) sound.play('chase');
    else if (w.tier >= 1) sound.play('alert');
    else sound.play('calm');
  }

  syncAll(dt) {
    this.syncItems();
    this.syncFurniture();
    this.syncFerret(dt);
    this.syncOwners(dt);
    this.drawCones();
    this.drawFx(dt);
    this.fadeOccluders();
  }

  syncItems() {
    const w = this.world;
    w.items.forEach((it, i) => {
      const s = this.itemSprites[i];
      const vis = it.state === 'world' && !it.container;
      s.setVisible(vis);
      if (vis) {
        s.setPosition(Math.round(it.x), Math.round(it.y - 2)).setDepth(it.y);
      }
      if (s.glow) {
        s.glow.setVisible(vis);
        if (vis) s.glow.setPosition(Math.round(it.x + 5), Math.round(it.y - 8));
      }
    });
  }

  syncFurniture() {
    const w = this.world;
    for (const fu of this.furn) {
      if (fu.spark) {
        const has = w.containerItems(fu.f).length > 0;
        const r = furnitureRect(fu.f);
        fu.spark.setVisible(has).setPosition(r.x * TILE + r.w * 8, r.y * TILE - fu.def.tall - 2);
      }
    }
    this.sofaIcon.setVisible(w.ferret.carried.length > 0);
  }

  syncFerret(dt) {
    const f = this.world.ferret;
    const s = this.ferretSpr;
    const id = this.ferretId;
    s.setPosition(Math.round(f.x), Math.round(f.y + 4)).setDepth(f.y + 4);
    let dir = 'down';
    let flip = false;
    if (Math.abs(f.fx) > Math.abs(f.fy) + 0.01) { dir = 'side'; flip = f.fx < 0; } else if (f.fy < 0) dir = 'up';
    s.setFlipX(flip);
    s.setVisible(f.state !== 'tube');
    const moving = f.moving || f.state === 'slide';
    const key = moving ? `ferret_${id}_${dir}` : `ferret_${id}_${dir}_idle`;
    if (s.anims.currentAnim?.key !== key) s.play(key, true);
    s.anims.timeScale = f.mode === 'run' ? 1.7 : f.mode === 'sneak' ? 0.55 : f.state === 'slide' ? 0.2 : 1;
    s.setScale(f.state === 'slide' ? 1.15 : 1, f.mode === 'sneak' ? 0.85 : 1);
    const hidden = this.world.ferretVisibility() === 0 && f.state === 'free';
    let alpha = hidden ? 0.6 : 1;
    if (f.graceT > 0) alpha = Math.floor(f.graceT * 10) % 2 ? 0.3 : 0.9;
    s.setAlpha(alpha);
    if (f.state === 'caught') s.x += Math.sin(this.time.now / 40) * 1.2;

    // Objetos en la boca.
    const offs = dir === 'side' ? [[flip ? -10 : 10, -6], [flip ? -6 : 6, -9]] : dir === 'down' ? [[0, -3], [4, -6]] : [[0, -16], [4, -18]];
    this.carrySpr.forEach((cs, i) => {
      const it = f.carried[i];
      if (!it || f.state === 'tube') { cs.setVisible(false); return; }
      cs.setTexture(`item_${it.id}`).setVisible(true).setAlpha(alpha)
        .setPosition(Math.round(s.x + offs[i][0]), Math.round(s.y + offs[i][1]))
        .setDepth(s.depth + (dir === 'up' ? -0.1 : 0.1));
    });

    // Sonidos ambientales del hurón.
    if (f.state === 'search') {
      this.searchSfxT -= dt;
      if (this.searchSfxT <= 0) { this.searchSfxT = 0.3; sound.sfx('search'); }
    }
    if (f.mode === 'run') {
      this.dookT -= dt;
      if (this.dookT <= 0) { this.dookT = 2.5 + Math.random() * 2; sound.sfx('dook'); }
    }
  }

  syncOwners(dt) {
    this.world.owners.forEach((o, i) => {
      const v = this.ownerSpr[i];
      const s = v.s;
      s.setPosition(Math.round(o.x), Math.round(o.y + 6)).setDepth(o.y + 6);
      const c = Math.cos(o.angle);
      const sn = Math.sin(o.angle);
      let dir = 'down';
      if (Math.abs(c) > Math.abs(sn)) dir = 'side'; else if (sn < 0) dir = 'up';
      s.setFlipX(dir === 'side' && c < 0);
      if (o.moving) {
        const key = `human_${o.skin}_${dir}`;
        if (s.anims.currentAnim?.key !== key || !s.anims.isPlaying) s.play(key, true);
        s.anims.timeScale = o.state === 'chase' ? 1.8 : 1;
      } else {
        s.anims.stop();
        const base = { down: 1, up: 5, side: 9 }[dir];
        s.setFrame(o.act === 'sit' ? 12 : o.act === 'phone' ? 13 : base);
        if (o.act === 'sit' || o.act === 'phone') s.setFlipX(false);
      }
      // Signo sobre la cabeza.
      const mark = o.state === 'chase' ? '!' : o.bubble === '?' || o.state === 'suspicious' || o.state === 'search' ? '?' : '';
      v.mark.setText(mark).setTint(mark === '!' ? UI.red : UI.orange);
      v.mark.setX2(Math.round(o.x));
      v.mark.y = Math.round(o.y - 44 + Math.sin(this.time.now / 120) * 1.5);
      v.meter.clear();
      if (o.detect > 0.02 && o.state !== 'chase') {
        v.meter.fillStyle(0x000000, 1).fillRect(o.x - 9, o.y - 30, 18, 4);
        v.meter.fillStyle(o.detect > 0.6 ? UI.red : UI.orange, 1).fillRect(o.x - 8, o.y - 29, Math.round(16 * o.detect), 2);
      }
      if (v.bubbleT > 0) {
        v.bubbleT -= dt;
        v.bubble.setPosition(Math.round(o.x), Math.round(o.y - 38));
        if (v.bubbleT <= 0) v.bubble.setVisible(false);
      }
    });
  }

  say(i, str) {
    const v = this.ownerSpr[i];
    const o = this.world.owners[i];
    v.bubble.removeAll(true);
    const t = text(this, 0, 0, wrap(str, 22), { color: 0x101018, shadow: null, lineSpacing: 1 });
    const w = Math.ceil(t.main.width) + 8;
    const h = Math.ceil(t.main.height) + 6;
    const g = this.add.graphics();
    g.fillStyle(0x101018, 1).fillRect(-w / 2 - 1, -h - 1, w + 2, h + 2);
    g.fillStyle(0xffffff, 1).fillRect(-w / 2, -h, w, h);
    g.fillStyle(0xffffff, 1).fillTriangle(-3, -1, 3, -1, 0, 4);
    t.setPosition(Math.round(-w / 2 + 4), -h + 3);
    v.bubble.add([g, t]).setVisible(true).setPosition(Math.round(o.x), Math.round(o.y - 38));
    v.bubbleT = 2.8;
  }

  drawCones() {
    const g = this.cone;
    const lv = this.world.level;
    g.clear();
    for (const o of this.world.owners) {
      if (o.state === 'catching') continue;
      const range = o.range();
      const half = Phaser.Math.DegToRad(40);
      const pts = [{ x: o.x, y: o.y }];
      const rays = 16;
      for (let r = 0; r <= rays; r++) {
        const a = o.angle - half + (2 * half * r) / rays;
        const ca = Math.cos(a);
        const sa = Math.sin(a);
        let d = 6;
        while (d < range) {
          const px = o.x + ca * d;
          const py = o.y + sa * d;
          if (flagAt(lv, Math.floor(px / TILE), Math.floor(py / TILE)) & F.BLOCK_VISION) break;
          d += 4;
        }
        pts.push({ x: o.x + ca * d, y: o.y + sa * d });
      }
      const col = o.state === 'chase' ? UI.red : o.detect > 0.05 || o.state === 'suspicious' ? UI.orange : 0xffd84a;
      g.fillStyle(col, o.state === 'chase' ? 0.34 : 0.28);
      g.fillPoints(pts, true);
      g.lineStyle(1, col, 0.7);
      g.strokePoints(pts, true);
    }
  }

  drawFx(dt) {
    const g = this.fx;
    g.clear();
    this.rings = this.rings.filter((r) => (r.t += dt) < r.life);
    for (const r of this.rings) {
      const k = r.t / r.life;
      g.lineStyle(1, r.color, (1 - k) * 0.6);
      g.strokeCircle(r.x, r.y, 4 + (r.r - 4) * k);
    }
  }

  fadeOccluders() {
    const f = this.world.ferret;
    const fs = this.ferretSpr;
    const fx0 = fs.x - 10;
    const fx1 = fs.x + 10;
    const fy0 = fs.y - 18;
    const fy1 = fs.y;
    const depth = fs.depth;
    const hit = (b) => b.right > fx0 && b.x < fx1 && b.bottom > fy0 && b.y < fy1;
    for (const w of this.walls) {
      if (Math.abs(w.x + 8 - fs.x) > 40 || Math.abs(w.y - fs.y) > 40) { if (w.alpha !== 1) w.setAlpha(1); continue; }
      w.setAlpha(w.depth > depth && hit(w.getBounds()) ? 0.45 : 1);
    }
    for (const d of this.doorSprites) d.setAlpha(d.depth > depth && hit(d.getBounds()) ? 0.45 : 1);
    const tx = Math.floor(f.x / TILE);
    const ty = Math.floor(f.y / TILE);
    for (const fu of this.furn) {
      if (DECALS.has(fu.f.type)) continue;
      const r = furnitureRect(fu.f);
      const inside = tx >= r.x && tx < r.x + r.w && ty >= r.y && ty < r.y + r.h;
      if (inside && HIDES.has(fu.f.type)) fu.s.setAlpha(0.5);
      else fu.s.setAlpha(fu.s.depth > depth && hit(fu.s.getBounds()) ? 0.5 : 1);
    }
  }

  // ------------------------------------------------------------------ eventos
  toast(msg, color, key) {
    if (key) {
      const now = this.time.now;
      if ((this.toastCd[key] ?? 0) > now) return;
      this.toastCd[key] = now + 2500;
    }
    this.hud?.toast(msg, color);
  }

  floatText(x, y, str, color) {
    const t = text(this, x, y, str, { color, align: 'center', depth: 9700 });
    this.tweens.add({ targets: t, y: y - 18, alpha: 0, duration: 1100, onComplete: () => t.destroy() });
  }

  handleEvents(evs) {
    const w = this.world;
    for (const e of evs) {
      switch (e.type) {
        case 'step': sound.sfx(e.mode === 'run' ? 'stepRun' : e.mode === 'sneak' ? '' : 'step'); break;
        case 'noise':
          if (e.kind !== 'step' || e.r > TILE * 3) this.rings.push({ x: e.x, y: e.y, r: e.r, t: 0, life: 0.45, color: e.kind === 'keys' ? UI.gold : 0xffffff });
          break;
        case 'pickup': {
          const def = ITEMS[w.items[e.item].id];
          sound.sfx('pickup');
          this.floatText(w.ferret.x, w.ferret.y - 22, def.name, UI.text);
          if (def.power) this.toast(POWER_INFO[def.power], UI.blue);
          break;
        }
        case 'deposit':
          sound.sfx('deposit');
          this.floatText(w.ferret.x, w.ferret.y - 24, `+${e.gained}`, UI.gold);
          break;
        case 'pair':
          sound.sfx('pair');
          this.hud?.banner('¡PAREJA! x3', UI.gold);
          break;
        case 'drop': sound.sfx('drop'); break;
        case 'searchDone':
          if (e.item == null) { sound.sfx('empty'); this.floatText(w.ferret.x, w.ferret.y - 22, 'Vacío...', UI.gray); } else sound.sfx('found');
          if (e.dropped) this.toast('¡No te cabe en la boca! Se ha caído al suelo.', UI.orange);
          break;
        case 'power':
          if (e.power === 'slide') sound.sfx('slide');
          if (e.power === 'treat') { sound.sfx('treat'); this.toast(POWER_INFO.treat, UI.green); }
          if (e.power === 'keys') sound.sfx('power');
          break;
        case 'keysThrow': this.throwKeys(e); break;
        case 'keysLand': sound.sfx('keys'); break;
        case 'tv': {
          const tv = this.furn.find((fu) => fu.f.type === 'tv');
          if (tv) tv.s.setTexture(e.on ? 'furn_tv_on' : 'furn_tv');
          if (e.on) sound.sfx('tv');
          break;
        }
        case 'say': this.say(e.owner, e.text); break;
        case 'chase':
          sound.sfx('alarm');
          this.cameras.main.shake(150, 0.003);
          break;
        case 'caught':
          sound.sfx('caught');
          this.cameras.main.flash(200, 255, 80, 80);
          this.hud?.banner('¡AL CORRALITO!', UI.red);
          break;
        case 'released': sound.sfx('released'); break;
        case 'tier':
          sound.sfx('tier');
          this.hud?.banner(e.text.toUpperCase(), UI.red);
          break;
        case 'tidy': sound.sfx('tidy'); break;
        case 'washed':
          sound.sfx('washed');
          this.toast(`¡${ITEMS[w.items[e.item].id].name} a la lavadora! Perdido.`, UI.red);
          break;
        case 'sofaCheck':
          this.toast(`El dueño ha recuperado ${e.count} ${e.count === 1 ? 'objeto' : 'objetos'} del sofá.`, UI.red);
          break;
        case 'doors':
        case 'door':
          this.syncDoors();
          sound.sfx('door');
          break;
        case 'basket': {
          const fu = this.furn.find((x) => x.f.uid === e.uid);
          const r = furnitureRect(fu.f);
          fu.s.setPosition(r.x * TILE - 1, (r.y + r.h) * TILE + 1).setDepth((r.y + r.h) * TILE - 0.5);
          break;
        }
        case 'hint': this.hud?.hint(e.text); break;
        case 'askEnd': this.askEnd(); break;
        case 'exhausted':
          sound.sfx('exhausted');
          this.floatText(w.ferret.x, w.ferret.y - 22, '¡Agotado!', UI.orange);
          break;
        case 'noPower': this.toast('No llevas ningún potenciador.', UI.gray, 'noPower'); sound.sfx('noPower'); break;
        case 'tubeIn': case 'tubeOut': sound.sfx('tube'); break;
        case 'end': this.onEnd(e.result); break;
        default: break;
      }
    }
  }

  syncDoors() {
    this.world.level.doors.forEach((d, i) => {
      this.doorSprites[i].setTexture(d.closed && d.openTimer <= 0 ? 'door_closed' : 'door_open');
    });
  }

  throwKeys(e) {
    const img = this.add.image(e.x0, e.y0, 'item_keys').setDepth(9000);
    const o = { t: 0 };
    this.tweens.add({
      targets: o, t: 1, duration: 450,
      onUpdate: () => img.setPosition(e.x0 + (e.x - e.x0) * o.t, e.y0 + (e.y - e.y0) * o.t - Math.sin(o.t * Math.PI) * 18).setAngle(o.t * 720),
      onComplete: () => this.tweens.add({ targets: img, alpha: 0, delay: 800, duration: 300, onComplete: () => img.destroy() }),
    });
  }

  askEnd() {
    this.modal = true;
    this.hud?.confirm('¿Dormir y terminar el día?', (yes) => {
      this.modal = false;
      if (yes) this.world.finish('sleep');
      this.handleEvents(this.world.drainEvents());
    });
  }

  onEnd(result) {
    this.result = result;
    this.endT = 1.6;
    sound.stop();
    sound.sfx(result.win ? 'win' : 'fail');
    this.hud?.banner(result.reason === 'sleep' ? '¡A DORMIR!' : '¡FIN DEL DÍA!', result.win ? UI.gold : UI.red);
  }

  toResults() {
    this.scene.stop('Hud');
    this.scene.start('Results', { levelId: this.levelId, ferret: this.ferretId, result: this.result });
  }

  openPause() {
    if (this.paused || this.endT >= 0) return;
    this.paused = true;
    this.scene.pause('Hud');
    this.scene.launch('Pause', { levelId: this.levelId, ferret: this.ferretId });
    this.scene.pause();
  }

  resumeFromPause() {
    this.paused = false;
    this.scene.resume('Hud');
    this.scene.resume();
  }
}

export { lineOfSight };
