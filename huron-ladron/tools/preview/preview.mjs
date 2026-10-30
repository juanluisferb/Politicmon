// Renderiza una hoja de previsualización de los gráficos generados (para revisar el pixel art).
// Uso: node tools/preview/preview.mjs salida.png [modulo]
import { build } from 'esbuild';
import { createRequire } from 'node:module';
const { chromium } = createRequire(import.meta.url)('playwright');
import path from 'node:path';
import fs from 'node:fs';

const out = process.argv[2] ?? 'preview.png';
const entry = path.resolve('tools/preview/sheet.js');
const res = await build({ entryPoints: [entry], bundle: true, write: false, format: 'iife' });
const js = res.outputFiles[0].text;
const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1200, height: 900 } });
page.on('console', (m) => console.log('[page]', m.text()));
page.on('pageerror', (e) => console.log('[error]', e.message));
await page.setContent('<html><body style="margin:0;background:#334"></body></html>');
await page.addScriptTag({ content: js });
await page.waitForTimeout(300);
const el = await page.$('canvas');
fs.mkdirSync(path.dirname(path.resolve(out)), { recursive: true });
await el.screenshot({ path: out });
await browser.close();
console.log('ok', out);
