import Phaser from 'phaser';
import { text, panel, wrap } from '../ui/text.js';
import { Menu } from '../ui/controls.js';
import { UI } from '../gfx/palette.js';
import { LEVELS } from '../data/levels/index.js';
import { FERRETS } from '../data/ferrets.js';
import { sound } from '../audio/audio.js';
import { save } from '../save.js';

export class Levels extends Phaser.Scene {
  constructor() { super('Levels'); }

  create() {
    const W = this.scale.width;
    const H = this.scale.height;
    this.cameras.main.fadeIn(200);
    text(this, W / 2, 6, 'ELIGE NIVEL', { color: UI.gold, scale: 2, align: 'center' });
    const fid = save.data.ferret;
    this.add.sprite(W - 22, 14, `ferret_${fid}`).play(`ferret_${fid}_down_idle`);
    text(this, W - 34, 26, FERRETS[fid].name, { color: UI.gray, align: 'center' });

    const cw = Math.min(104, Math.floor((W - 16) / 3));
    const x0 = Math.round((W - cw * 3 - 8) / 2);
    this.cards = LEVELS.map((lv, i) => {
      const x = x0 + i * (cw + 4);
      const y = 40;
      const locked = lv.id > save.data.unlocked;
      const border = this.add.graphics();
      panel(this, x, y, cw, 140, { fill: locked ? UI.panelDark : UI.panel });
      text(this, x + cw / 2, y + 8, String(lv.id), { color: locked ? UI.gray : UI.gold, scale: 3, align: 'center' });
      text(this, x + cw / 2, y + 36, wrap(lv.name, 15), { color: locked ? UI.gray : UI.text, align: 'center' });
      if (locked) {
        text(this, x + cw / 2, y + 70, 'BLOQUEADO', { color: UI.red, align: 'center' });
        text(this, x + cw / 2, y + 84, wrap('Supera el nivel anterior', 15), { color: UI.gray, align: 'center' });
      } else {
        text(this, x + 6, y + 58, wrap(lv.subtitle, 15), { color: UI.gray, lineSpacing: 1 });
        const best = save.data.best[lv.id];
        for (let s = 0; s < 3; s++) this.add.image(x + cw / 2 - 12 + s * 12, y + 118, best && best.stars > s ? 'star' : 'star_empty');
        text(this, x + cw / 2, y + 127, best ? `RÉCORD ${best.score}` : 'SIN JUGAR', { color: UI.gray, align: 'center' });
      }
      return { lv, x, y, w: cw, h: 140, border, locked };
    });
    text(this, W / 2, H - 12, 'A / TOCA DOS VECES: JUGAR    B: VOLVER', { color: UI.gray, align: 'center' });

    const start = Math.min(save.data.unlocked, LEVELS.length) - 1;
    this.menu = new Menu(this, this.cards.map((c) => ({ x: c.x, y: c.y, w: c.w, h: c.h, onSelect: () => this.play(c) })), {
      layout: 'h', start, tapToFocus: true,
      sound: (s) => sound.sfx(s),
      onChange: (i) => this.focus(i),
      onBack: () => this.scene.start('Select'),
    });
    this.handleBack = () => this.scene.start('Select');
    this.game.events.on('androidBack', this.handleBack);
    this.events.once('shutdown', () => this.game.events.off('androidBack', this.handleBack));
    sound.play('title');
  }

  focus(i) {
    this.cards.forEach((c, j) => {
      c.border.clear();
      if (j === i) c.border.lineStyle(2, c.locked ? UI.gray : UI.gold, 1).strokeRect(c.x - 2, c.y - 2, c.w + 4, c.h + 4);
    });
  }

  play(card) {
    if (card.locked) {
      sound.sfx('noPower');
      this.cameras.main.shake(120, 0.004);
      return;
    }
    this.menu.enabled = false;
    this.cameras.main.fadeOut(200, 0, 0, 0);
    this.cameras.main.once('camerafadeoutcomplete', () => this.scene.start('Game', { levelId: card.lv.id, ferret: save.data.ferret }));
  }
}
