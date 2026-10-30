import Phaser from 'phaser';
import { generateTextures } from '../gfx/textures.js';
import { FERRET_ORDER } from '../data/ferrets.js';

export class Boot extends Phaser.Scene {
  constructor() { super('Boot'); }

  create() {
    generateTextures(this);
    // Animaciones: 0-3 abajo, 4-7 arriba, 8-11 lado.
    const dirs = { down: 0, up: 4, side: 8 };
    for (const id of FERRET_ORDER) {
      for (const [d, o] of Object.entries(dirs)) {
        this.anims.create({ key: `ferret_${id}_${d}`, frames: this.anims.generateFrameNumbers(`ferret_${id}`, { frames: [o, o + 1, o + 2, o + 3] }), frameRate: 10, repeat: -1 });
        this.anims.create({ key: `ferret_${id}_${d}_idle`, frames: this.anims.generateFrameNumbers(`ferret_${id}`, { frames: [o + 1, o + 1, o + 3] }), frameRate: 3, repeat: -1 });
      }
    }
    for (const skin of ['owner', 'guest']) {
      for (const [d, o] of Object.entries(dirs)) {
        this.anims.create({ key: `human_${skin}_${d}`, frames: this.anims.generateFrameNumbers(`human_${skin}`, { frames: [o, o + 1, o + 2, o + 3] }), frameRate: 7, repeat: -1 });
      }
    }
    this.scene.start('Title');
  }
}
