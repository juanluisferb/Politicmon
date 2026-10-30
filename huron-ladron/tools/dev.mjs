// Servidor de desarrollo con recarga: http://localhost:8080
import { context } from 'esbuild';
import fs from 'node:fs';

fs.mkdirSync('dist', { recursive: true });
fs.copyFileSync('public/index.html', 'dist/index.html');
const ctx = await context({
  entryPoints: ['src/main.js'], bundle: true, format: 'iife', sourcemap: true, outfile: 'dist/game.js',
});
await ctx.watch();
const { host, port } = await ctx.serve({ servedir: 'dist', port: 8080 });
console.log(`Hurón Ladrón en http://${host === '0.0.0.0' ? 'localhost' : host}:${port}`);
