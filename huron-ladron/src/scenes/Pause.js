import Phaser from 'phaser';
import { text, panel } from '../ui/text.js';
import { Menu } from '../ui/controls.js';
import { UI } from '../gfx/palette.js';
import { sound } from '../audio/audio.js';
import { LEVELS } from '../data/levels/index.js';

export class Pause extends Phaser.Scene {
  constructor() { super('Pause'); }

  init(data) { this.data2 = data; }

  create() {
    const W = this.scale.width;
    const H = this.scale.height;
    const lv = LEVELS.find((l) => l.id === this.data2.levelId);
    this.add.rectangle(0, 0, W, H, 0x000000, 0.6).setOrigin(0);
    const pw = 220;
    const px = W / 2 - pw / 2;
    panel(this, px, 14, pw, 196);
    text(this, W / 2, 22, 'PAUSA', { color: UI.gold, scale: 2, align: 'center' });
    text(this, W / 2, 42, `${lv.id}. ${lv.name}`, { color: UI.gray, align: 'center' });
    text(this, W / 2, 52, `CUOTA: ${lv.quota[0]} · ${lv.quota[1]} · ${lv.quota[2]}`, { color: UI.gray, align: 'center' });

    const opts = [
      { label: () => 'CONTINUAR', act: () => this.resume() },
      { label: () => 'REINICIAR NIVEL', act: () => this.restart() },
      { label: () => `MÚSICA: ${sound.musicOn ? 'SÍ' : 'NO'}`, act: () => { sound.toggleMusic(); this.refresh(); } },
      { label: () => `EFECTOS: ${sound.sfxOn ? 'SÍ' : 'NO'}`, act: () => { sound.toggleSfx(); this.refresh(); } },
      { label: () => 'SALIR AL MENÚ', act: () => this.quit() },
    ];
    this.opts = opts;
    this.labels = opts.map((o, i) => text(this, W / 2, 66 + i * 13, o.label(), { align: 'center' }));
    this.menu = new Menu(this, opts.map((o, i) => ({ x: px + 10, y: 63 + i * 13, w: pw - 20, h: 13, onSelect: o.act })), {
      sound: (s) => sound.sfx(s),
      onChange: (i) => this.labels.forEach((l, j) => l.setTint(j === i ? UI.gold : UI.text)),
      onBack: () => this.resume(),
    });
    const help = [
      'TECLADO: FLECHAS/WASD MOVER · Z COGER',
      'X POTENCIADOR · SHIFT CORRER · C SIGILO',
      'MANDO: A COGER · B USAR · RT CORRER · LT SIGILO',
    ];
    help.forEach((h, i) => text(this, W / 2, 140 + i * 10, h, { color: UI.gray, align: 'center' }));
    text(this, W / 2, 176, 'Lleva lo robado a la madriguera (casita) o al', { color: UI.text, align: 'center' });
    text(this, W / 2, 186, 'sofá (x1.5). Llega a la cuota antes de la noche.', { color: UI.text, align: 'center' });

    this.onBack = () => this.resume();
    this.game.events.on('androidBack', this.onBack);
    this.events.once('shutdown', () => this.game.events.off('androidBack', this.onBack));
    sound.suspend();
  }

  refresh() {
    this.labels.forEach((l, i) => l.setText(this.opts[i].label()));
    this.labels.forEach((l, j) => l.setTint(j === this.menu.index ? UI.gold : UI.text));
  }

  resume() {
    sound.resume();
    this.scene.stop();
    this.scene.get('Game').resumeFromPause();
  }

  restart() {
    sound.resume();
    this.scene.stop('Hud');
    this.scene.stop();
    this.scene.stop('Game');
    this.scene.start('Game', { levelId: this.data2.levelId, ferret: this.data2.ferret });
  }

  quit() {
    sound.resume();
    sound.stop();
    this.scene.stop('Hud');
    this.scene.stop('Game');
    this.scene.start('Levels');
  }
}
