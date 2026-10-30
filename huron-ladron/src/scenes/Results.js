import Phaser from 'phaser';
import { text, panel } from '../ui/text.js';
import { Menu } from '../ui/controls.js';
import { UI } from '../gfx/palette.js';
import { sound } from '../audio/audio.js';
import { save } from '../save.js';
import { LEVELS } from '../data/levels/index.js';

export class Results extends Phaser.Scene {
  constructor() { super('Results'); }

  init(data) {
    this.levelId = data.levelId;
    this.ferret = data.ferret;
    this.result = data.result;
  }

  create() {
    const W = this.scale.width;
    const H = this.scale.height;
    const r = this.result;
    const lv = LEVELS.find((l) => l.id === this.levelId);
    const isBest = save.record(this.levelId, r, LEVELS.length);
    this.cameras.main.fadeIn(250);
    panel(this, 8, 6, W - 16, H - 12);
    text(this, W / 2, 14, r.win ? '¡NIVEL SUPERADO!' : '¡NO LLEGASTE A LA CUOTA!', { color: r.win ? UI.gold : UI.red, scale: 2, align: 'center' });
    text(this, W / 2, 34, `${lv.id}. ${lv.name}`, { color: UI.gray, align: 'center' });

    const spr = this.add.sprite(46, 74, `ferret_${this.ferret}`).setScale(2);
    spr.play(r.win ? `ferret_${this.ferret}_side` : `ferret_${this.ferret}_down_idle`);
    if (!r.win) this.add.image(62, 56, 'icon_zzz');

    const lines = [
      ['Objetos escondidos', r.stashed],
      ['Valor del botín', r.base],
      [`Parejas de calcetines (${r.pairCount})`, `+${r.pairBonus}`],
      ['Bonus sin ser visto', `+${r.stealthBonus}`],
      ['Veces en el corralito', r.stats.caught],
      ['Persecuciones', r.stats.chases],
    ];
    const lx = 84;
    const rx = W - 24;
    lines.forEach(([k, v], i) => {
      const y = 50 + i * 11;
      const a = text(this, lx, y, k, { color: UI.text });
      const b = text(this, rx, y, String(v), { color: UI.gold, align: 'right' });
      a.setAlpha(0); b.setAlpha(0);
      this.tweens.add({ targets: [a, b], alpha: 1, delay: 150 + i * 140, duration: 150, onStart: () => sound.sfx('tick') });
    });
    text(this, lx, 120, 'TOTAL', { color: UI.text, scale: 2 });
    const tot = text(this, rx, 120, '0', { color: UI.gold, scale: 2, align: 'right' });
    const counter = { v: 0 };
    this.tweens.add({ targets: counter, v: r.final, delay: 1000, duration: 700, onUpdate: () => tot.setText(String(Math.round(counter.v))) });
    text(this, lx, 138, `CUOTA: ${lv.quota.join(' / ')}`, { color: UI.gray });
    if (isBest && r.final > 0) {
      const nb = text(this, W / 2, 150, '¡NUEVO RÉCORD!', { color: UI.green, align: 'center' }).setAlpha(0);
      this.tweens.add({ targets: nb, alpha: 1, delay: 1900, duration: 200 });
    }
    for (let s = 0; s < 3; s++) {
      const st = this.add.image(46 - 18 + s * 18, 104, 'star_empty').setScale(1.6);
      if (s < r.stars) {
        this.time.delayedCall(1800 + s * 350, () => {
          st.setTexture('star');
          sound.sfx('star');
          this.tweens.add({ targets: st, scale: 2.4, duration: 140, yoyo: true });
        });
      }
    }

    const next = LEVELS.find((l) => l.id === this.levelId + 1);
    const opts = [];
    if (r.win && next) opts.push({ label: 'SIGUIENTE NIVEL', act: () => this.go('Game', { levelId: next.id, ferret: this.ferret }) });
    opts.push({ label: 'REPETIR', act: () => this.go('Game', { levelId: this.levelId, ferret: this.ferret }) });
    opts.push({ label: 'CAMBIAR DE HURÓN', act: () => this.go('Select') });
    opts.push({ label: 'MENÚ DE NIVELES', act: () => this.go('Levels') });
    if (r.win && !next) text(this, W / 2, 160, '¡Has completado todos los niveles! Eres el rey del calcetín.', { color: UI.green, align: 'center' });
    this.labels = opts.map((o, i) => text(this, W / 2, 172 + i * 11, o.label, { align: 'center' }));
    this.menu = new Menu(this, opts.map((o, i) => ({ x: W / 2 - 70, y: 169 + i * 11, w: 140, h: 11, onSelect: o.act })), {
      sound: (s) => sound.sfx(s),
      onChange: (i) => this.labels.forEach((l, j) => l.setTint(j === i ? UI.gold : UI.text)),
      onBack: () => this.go('Levels'),
    });
    this.onBack = () => this.go('Levels');
    this.game.events.on('androidBack', this.onBack);
    this.events.once('shutdown', () => this.game.events.off('androidBack', this.onBack));
  }

  go(scene, data) {
    this.menu.enabled = false;
    this.cameras.main.fadeOut(200, 0, 0, 0);
    this.cameras.main.once('camerafadeoutcomplete', () => this.scene.start(scene, data));
  }
}
