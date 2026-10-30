// Interfaz superpuesta: cuota, reloj, sospecha, ruido, energía, objetos, avisos y controles táctiles.
import Phaser from 'phaser';
import { text, wrap, panel, bar } from '../ui/text.js';
import { touch, isTouchDevice, Menu } from '../ui/controls.js';
import { UI } from '../gfx/palette.js';
import { ITEMS } from '../data/items.js';
import { sound } from '../audio/audio.js';

const TIER_NAMES = ['TRANQUILO', 'ALERTA', 'PARANOIA'];
const TIER_COLORS = [UI.green, UI.orange, UI.red];

export class Hud extends Phaser.Scene {
  constructor() { super('Hud'); }

  create() {
    this.gameScene = this.scene.get('Game');
    this.world = this.gameScene.world;
    const W = this.scale.width;
    const H = this.scale.height;
    const q = this.world.quota;
    this.qMax = Math.round(q[2] * 1.12);

    // Barra superior.
    const top = this.add.graphics();
    top.fillStyle(0x000000, 0.55).fillRect(0, 0, W, 22);
    top.fillStyle(UI.borderDark, 1).fillRect(0, 22, W, 1);

    // Cuota: puntos, barra y una estrella en cada umbral.
    this.scoreTxt = text(this, 4, 2, '0', { color: UI.gold });
    this.quotaBar = bar(this, 4, 14, 104, 5, UI.green);
    this.qStars = q.map((v) => {
      const x = 4 + Math.round((v / this.qMax) * 104);
      this.add.rectangle(x, 14, 1, 5, 0xffffff).setOrigin(0.5, 0);
      return this.add.image(x, 7, 'star_empty');
    });

    this.clockTxt = text(this, W / 2, 3, '08:00', { color: UI.text, align: 'center' });
    this.dayBar = bar(this, W / 2 - 30, 14, 60, 3, UI.blue);

    this.add.image(W - 104, 9, 'icon_eye');
    this.suspBar = bar(this, W - 96, 5, 50, 5, UI.green);
    this.tierTxt = text(this, W - 96, 12, TIER_NAMES[0], { color: UI.green });

    // Bloque izquierdo: objetos en la boca, energía y ruido.
    this.slots = [0, 1].map((i) => {
      const g = this.add.graphics();
      const img = this.add.image(14 + i * 20, 36, 'px').setVisible(false);
      const tag = text(this, 18 + i * 20, 39, 'B', { color: UI.blue });
      return { g, img, tag, x: 4 + i * 20, y: 26 };
    });
    this.add.image(8, 52, 'icon_bolt');
    this.energyBar = bar(this, 14, 50, 34, 4, UI.gold);
    this.add.image(8, 61, 'icon_noise');
    this.noiseBar = bar(this, 14, 59, 34, 4, UI.green);
    this.statusTxt = text(this, 4, 67, '', { color: UI.gray });

    // Texto de acción contextual.
    this.actionTxt = text(this, W / 2, H - 13, '', { color: UI.text, align: 'center' });

    // Mensajes.
    this.hintBox = this.add.container(0, 0).setVisible(false);
    this.hintQueue = [];
    this.hintT = 0;
    this.toasts = [];
    this.bannerTxt = text(this, W / 2, 84, '', { color: UI.gold, align: 'center', scale: 2 }).setVisible(false);

    // Escape del corralito.
    this.escapeBar = bar(this, W / 2 - 40, H - 34, 80, 5, UI.gold);
    this.escapeBar.g.setVisible(false);
    this.escapeTxt = text(this, W / 2, H - 46, '¡PULSA A PARA ESCAPAR!', { color: UI.gold, align: 'center' }).setVisible(false);

    this.useTouch = isTouchDevice();
    this.buildTouch();
    this.input.on('pointerdown', (p) => {
      if (p.wasTouch && !this.useTouch) {
        this.useTouch = true;
        this.touchLayer.setVisible(true);
      }
    });
  }

  // ------------------------------------------------------------------ táctil
  buildTouch() {
    const W = this.scale.width;
    const H = this.scale.height;
    this.input.addPointer(3);
    const L = this.add.container(0, 0);
    this.touchLayer = L;
    this.joyHome = { x: 46, y: H - 46 };
    this.joyBase = this.add.image(this.joyHome.x, this.joyHome.y, 'touch_base').setAlpha(0.6);
    this.joyKnob = this.add.image(this.joyHome.x, this.joyHome.y, 'touch_knob').setAlpha(0.8);
    L.add([this.joyBase, this.joyKnob]);
    const mk = (key, x, y, r, label) => {
      const img = this.add.image(x, y, key).setAlpha(0.75);
      const t = text(this, x, y + r + 2, label, { color: UI.text, align: 'center' });
      L.add([img, t]);
      return { img, x, y, r: r + 6, label: t, ptr: null };
    };
    this.btns = {
      a: mk('btn_a', W - 32, H - 46, 17, 'A'),
      b: mk('btn_b', W - 74, H - 30, 13, 'B'),
      run: mk('btn_run', W - 72, H - 72, 12, 'CORRER'),
      sneak: mk('btn_sneak', W - 30, H - 88, 12, 'SIGILO'),
      pause: mk('btn_small', W - 12, 34, 9, ''),
    };
    const pz = text(this, W - 12, 30, 'II', { color: UI.text, align: 'center' });
    L.add(pz);
    this.joyPtr = null;
    this.sneakOn = false;
    L.setVisible(this.useTouch).setDepth(50);
    touch.active = true;

    const R = 26;
    this.input.on('pointerdown', (p) => {
      if (!this.touchLayer.visible || this.modalMenu) return;
      for (const [k, b] of Object.entries(this.btns)) {
        if (Math.hypot(p.x - b.x, p.y - b.y) <= b.r) {
          b.ptr = p.id;
          b.img.setAlpha(1).setScale(0.92);
          if (k === 'a') { touch.a = true; touch.aHeld = true; }
          if (k === 'b') touch.b = true;
          if (k === 'run') touch.run = true;
          if (k === 'sneak') { this.sneakOn = !this.sneakOn; touch.sneak = this.sneakOn; }
          if (k === 'pause') touch.pause = true;
          return;
        }
      }
      if (p.x < W * 0.5 && p.y > 24 && this.joyPtr === null) {
        this.joyPtr = p.id;
        this.joyBase.setPosition(p.x, p.y).setAlpha(0.9);
        this.joyKnob.setPosition(p.x, p.y);
      }
    });
    this.input.on('pointermove', (p) => {
      if (p.id !== this.joyPtr) return;
      const dx = p.x - this.joyBase.x;
      const dy = p.y - this.joyBase.y;
      const d = Math.hypot(dx, dy);
      const k = d > R ? R / d : 1;
      this.joyKnob.setPosition(this.joyBase.x + dx * k, this.joyBase.y + dy * k);
      const m = Math.min(1, d / R);
      touch.mx = d > 3 ? (dx / d) * m : 0;
      touch.my = d > 3 ? (dy / d) * m : 0;
    });
    const up = (p) => {
      if (p.id === this.joyPtr) {
        this.joyPtr = null;
        touch.mx = 0;
        touch.my = 0;
        this.joyBase.setPosition(this.joyHome.x, this.joyHome.y).setAlpha(0.6);
        this.joyKnob.setPosition(this.joyHome.x, this.joyHome.y);
      }
      for (const [k, b] of Object.entries(this.btns)) {
        if (b.ptr === p.id) {
          b.ptr = null;
          b.img.setScale(1).setAlpha(k === 'sneak' && this.sneakOn ? 1 : 0.75);
          if (k === 'a') touch.aHeld = false;
          if (k === 'run') touch.run = false;
        }
      }
    };
    this.input.on('pointerup', up);
    this.input.on('pointerupoutside', up);
    this.events.once('shutdown', () => {
      touch.active = false;
      touch.mx = 0; touch.my = 0; touch.run = false; touch.sneak = false; touch.aHeld = false;
    });
  }

  // ------------------------------------------------------------------ API para Game
  toast(msg, color = UI.text) {
    const W = this.scale.width;
    const t = text(this, W / 2, 0, wrap(msg, 46), { color, align: 'center' });
    this.toasts.push({ t, life: 2.6 });
    if (this.toasts.length > 3) this.toasts.shift().t.destroy();
  }

  banner(msg, color = UI.gold) {
    this.bannerTxt.setText(msg).setTint(color).setVisible(true).setAlpha(1).setScale(2);
    this.tweens.killTweensOf(this.bannerTxt);
    this.bannerTxt.setScale(2.6);
    this.tweens.add({ targets: this.bannerTxt, scale: 2, duration: 180, ease: 'Back.out' });
    this.tweens.add({ targets: this.bannerTxt, alpha: 0, delay: 1500, duration: 400, onComplete: () => this.bannerTxt.setVisible(false) });
  }

  hint(msg) {
    this.hintQueue.push(msg);
  }

  showHint(msg) {
    const W = this.scale.width;
    this.hintBox.removeAll(true);
    const lines = wrap(msg, 40);
    const n = lines.split('\n').length;
    const w = 252;
    const h = 10 + n * 10;
    const x = Math.round(W / 2 - w / 2);
    this.hintBox.add(panel(this, x, 26, w, h, { fill: 0x283868 }));
    this.hintBox.add(text(this, W / 2, 31, lines, { color: UI.text, align: 'center' }));
    this.hintBox.setVisible(true).setAlpha(1);
    this.hintT = 3 + n * 1.6;
  }

  confirm(msg, cb) {
    const W = this.scale.width;
    const H = this.scale.height;
    const c = this.add.container(0, 0).setDepth(100);
    const w = 200;
    const x = W / 2 - w / 2;
    const y = H / 2 - 30;
    c.add(this.add.rectangle(0, 0, W, H, 0x000000, 0.45).setOrigin(0));
    c.add(panel(this, x, y, w, 56));
    c.add(text(this, W / 2, y + 10, msg, { align: 'center' }));
    const yes = text(this, W / 2 - 34, y + 32, 'SÍ', { align: 'center', scale: 2 });
    const no = text(this, W / 2 + 34, y + 32, 'NO', { align: 'center', scale: 2 });
    c.add([yes, no]);
    this.modalMenu = true;
    const done = (v) => {
      menu.enabled = false;
      menu.items.forEach((it) => it.zone.destroy());
      c.destroy();
      this.modalMenu = false;
      cb(v);
    };
    const menu = new Menu(this, [
      { x: W / 2 - 60, y: y + 26, w: 52, h: 24, onSelect: () => done(true) },
      { x: W / 2 + 8, y: y + 26, w: 52, h: 24, onSelect: () => done(false) },
    ], {
      layout: 'h', sound: (s) => sound.sfx(s), onBack: () => done(false),
      onChange: (i) => { yes.setTint(i === 0 ? UI.gold : UI.text); no.setTint(i === 1 ? UI.gold : UI.text); },
    });
  }

  // ------------------------------------------------------------------ actualización
  update(time, delta) {
    const dt = delta / 1000;
    const w = this.world;
    const f = w.ferret;
    const sc = w.score();
    this.scoreTxt.setText(`${sc.total}`);
    this.quotaBar.set(sc.total / this.qMax, sc.total >= w.quota[0] ? UI.green : UI.orange);
    this.qStars.forEach((s, i) => {
      const on = sc.total >= w.quota[i];
      if (on && s.texture.key !== 'star') { s.setTexture('star'); sound.sfx('star'); this.tweens.add({ targets: s, scale: 1.6, duration: 150, yoyo: true }); }
      if (!on && s.texture.key !== 'star_empty') s.setTexture('star_empty');
    });
    const ck = w.clock();
    this.clockTxt.setText(ck.text);
    this.dayBar.set(ck.t, ck.t > 0.85 ? UI.red : UI.blue);
    this.suspBar.set(w.suspicion / 100, TIER_COLORS[w.tier]);
    this.tierTxt.setText(TIER_NAMES[w.tier]).setTint(TIER_COLORS[w.tier]);

    // Ranuras de objetos.
    const nSlots = f.def.slots;
    this.slots.forEach((s, i) => {
      s.g.clear();
      const show = i < nSlots;
      s.img.setVisible(false);
      s.tag.setVisible(false);
      if (!show) return;
      s.g.fillStyle(0x000000, 0.6).fillRect(s.x, s.y, 18, 18);
      s.g.lineStyle(1, UI.border, 1).strokeRect(s.x + 0.5, s.y + 0.5, 17, 17);
      const it = f.carried[i];
      if (it) {
        s.img.setTexture(`item_${it.id}`).setVisible(true).setPosition(s.x + 9, s.y + 9);
        s.tag.setVisible(!!ITEMS[it.id].power);
      }
    });
    this.energyBar.set(f.energy / f.def.energy, f.exhaustedT > 0 ? UI.red : UI.gold);
    this.noiseBar.set(f.noise, f.noise > 0.55 ? UI.red : f.noise > 0.25 ? UI.orange : UI.green);
    let status = '';
    if (f.treatT > 0) status = '¡SUBIDÓN!';
    else if (w.ferretVisibility() === 0 && f.state === 'free') status = 'ESCONDIDO';
    else if (f.mode === 'sneak') status = 'SIGILO';
    this.statusTxt.setText(status).setTint(f.treatT > 0 ? UI.green : UI.blue);

    // Texto de acción.
    const h = f.hint;
    this.actionTxt.setText(h && h.kind !== 'mash' ? `A: ${h.label}` : '');
    const caught = f.state === 'caught';
    this.escapeTxt.setVisible(caught);
    this.escapeBar.g.setVisible(caught);
    if (caught) {
      this.escapeBar.set(1 - f.caughtT / 6, UI.gold);
      this.escapeTxt.setAlpha(Math.floor(time / 250) % 2 ? 1 : 0.5);
    }

    // Pistas del tutorial.
    if (this.hintT > 0) {
      this.hintT -= dt;
      if (this.hintT < 0.4) this.hintBox.setAlpha(Math.max(0, this.hintT / 0.4));
      if (this.hintT <= 0) this.hintBox.setVisible(false);
    } else if (this.hintQueue.length) {
      this.showHint(this.hintQueue.shift());
    }
    // Avisos apilados.
    const H = this.scale.height;
    let y = H - 62;
    for (let i = this.toasts.length - 1; i >= 0; i--) {
      const t = this.toasts[i];
      t.life -= dt;
      t.t.y = y;
      t.t.setAlpha(Math.min(1, t.life / 0.5));
      y -= 10 * (t.t.main.text.split('\n').length);
      if (t.life <= 0) { t.t.destroy(); this.toasts.splice(i, 1); }
    }
  }
}
