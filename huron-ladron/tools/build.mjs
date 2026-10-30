// Empaqueta el juego en dist/ (un único script clásico: funciona desde file:// dentro del WebView de Android).
import { build } from 'esbuild';
import fs from 'node:fs';

fs.rmSync('dist', { recursive: true, force: true });
fs.mkdirSync('dist', { recursive: true });
await build({
  entryPoints: ['src/main.js'],
  bundle: true,
  minify: true,
  format: 'iife',
  target: ['chrome80'],
  outfile: 'dist/game.js',
  legalComments: 'none',
});
fs.copyFileSync('public/index.html', 'dist/index.html');
const kb = (fs.statSync('dist/game.js').size / 1024).toFixed(0);
console.log(`dist/ listo (game.js ${kb} KB)`);
