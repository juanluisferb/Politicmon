// Punto de entrada: configura Phaser con resolución interna de 224 px de alto (Mega Drive) y
// ancho según la pantalla, escalado para llenarla.
import Phaser from 'phaser';
import { Boot } from './scenes/Boot.js';
import { Title } from './scenes/Title.js';
import { Select } from './scenes/Select.js';
import { Levels } from './scenes/Levels.js';
import { Game } from './scenes/Game.js';
import { Hud } from './scenes/Hud.js';
import { Pause } from './scenes/Pause.js';
import { Results } from './scenes/Results.js';
import { sound } from './audio/audio.js';
import { save } from './save.js';

const H = 224;
const aspect = window.innerWidth / Math.max(1, window.innerHeight);
const W = Math.max(320, Math.min(480, Math.round((H * aspect) / 2) * 2));

save.load();

const game = new Phaser.Game({
  type: Phaser.AUTO,
  parent: 'game',
  width: W,
  height: H,
  backgroundColor: '#101828',
  pixelArt: true,
  roundPixels: true,
  input: { gamepad: true, activePointers: 4 },
  scale: { mode: Phaser.Scale.FIT, autoCenter: Phaser.Scale.CENTER_BOTH },
  scene: [Boot, Title, Select, Levels, Game, Hud, Pause, Results],
  audio: { noAudio: true },
});

// El audio solo puede arrancar tras un gesto del usuario.
const unlock = () => sound.unlock();
window.addEventListener('pointerdown', unlock);
window.addEventListener('keydown', unlock);
window.addEventListener('touchstart', unlock, { passive: true });

document.addEventListener('visibilitychange', () => {
  if (document.hidden) {
    sound.suspend();
    game.events.emit('appHidden');
  } else {
    sound.resume();
  }
});

// Botón "atrás" de Android (lo llama MainActivity).
window.__androidBack = () => game.events.emit('androidBack');
window.__game = game;
