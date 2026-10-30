import Phaser from 'phaser';
import { text, panel, wrap } from '../ui/text.js';
import { Menu } from '../ui/controls.js';
import { UI } from '../gfx/palette.js';
import { FERRETS, FERRET_ORDER } from '../data/ferrets.js';
import { sound } from '../audio/audio.js';
import { save } from '../save.js';

const STAT_LABELS = [['speed', 'VELOCIDAD'], ['stealth', 'SIGILO'], ['strength', 'FUERZA'], ['energy', 'AGUANTE']];

export class Select extends Phaser.Scene {
  constructor() { super('Select'); }

  create() {
    const W = this.scale.width;
    const H = this.scale.height;
    this.cameras.main.fadeIn(200);
    text(this, W / 2, 6, 'ELIGE TU HURÓN', { color: UI.gold, scale: 2, align: 'center' });

    const cw = Math.min(104, Math.floor((W - 16) / 3));
    const x0 = Math.round((W - cw * 3 - 8) / 2);
    this.cards = FERRET_ORDER.map((id, i) => {
      const f = FERRETS[id];
      const x = x0 + i * (cw + 4);
      const y = 28;
      const border = this.add.graphics();
      panel(this, x, y, cw, 150, { fill: UI.panelDark });
      const spr = this.add.sprite(x + cw / 2, y + 28, `ferret_${id}`).setScale(2).play(`ferret_${id}_side`);
      spr.anims.timeScale = 0.6;
      text(this, x + cw / 2, y + 52, f.name, { color: UI.gold, align: 'center', scale: 1 });
      text(this, x + cw / 2, y + 62, f.coat, { color: UI.gray, align: 'center' });
      text(this, x + cw / 2, y + 72, f.style, { color: UI.orange, align: 'center' });
      STAT_LABELS.forEach(([k, label], j) => {
        const sy = y + 86 + j * 15;
        text(this, x + 6, sy, label, { color: UI.text });
        const pg = this.add.graphics();
        for (let n = 0; n < 5; n++) {
          pg.fillStyle(0x000000, 1).fillRect(x + 6 + n * 9, sy + 8, 8, 4);
          pg.fillStyle(n < f.stats[k] ? UI.green : 0x404058, 1).fillRect(x + 7 + n * 9, sy + 9, 6, 2);
        }
      });
      return { id, x, y, w: cw, h: 150, border, spr };
    });
    this.desc = text(this, W / 2, 184, '', { align: 'center' });
    text(this, W / 2, H - 12, 'A / TOCA DOS VECES: ELEGIR    B: VOLVER', { color: UI.gray, align: 'center' });

    const start = Math.max(0, FERRET_ORDER.indexOf(save.data.ferret));
    this.menu = new Menu(this, this.cards.map((c) => ({ x: c.x, y: c.y, w: c.w, h: c.h, onSelect: () => this.pick(c.id) })), {
      layout: 'h', start, tapToFocus: true,
      sound: (s) => sound.sfx(s),
      onChange: (i) => this.focus(i),
      onBack: () => this.scene.start('Title'),
    });
    this.handleBack = () => this.scene.start('Title');
    this.game.events.on('androidBack', this.handleBack);
    this.events.once('shutdown', () => this.game.events.off('androidBack', this.handleBack));
    sound.play('title');
  }

  focus(i) {
    this.cards.forEach((c, j) => {
      c.border.clear();
      if (j === i) {
        c.border.lineStyle(2, UI.gold, 1).strokeRect(c.x - 2, c.y - 2, c.w + 4, c.h + 4);
        c.spr.anims.timeScale = 1.2;
      } else {
        c.spr.anims.timeScale = 0.4;
      }
    });
    this.desc.setText(wrap(FERRETS[this.cards[i].id].desc, 60));
  }

  pick(id) {
    save.data.ferret = id;
    save.write();
    sound.sfx('dook');
    this.menu.enabled = false;
    this.cameras.main.fadeOut(200, 0, 0, 0);
    this.cameras.main.once('camerafadeoutcomplete', () => this.scene.start('Levels'));
  }
}
