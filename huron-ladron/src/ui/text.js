// Utilidades de texto e interfaz pixel art.
import { fontText } from '../gfx/font.js';
import { UI } from '../gfx/palette.js';

/** Texto bitmap con sombra. Devuelve un contenedor con .setText(), .setTint() y .width. */
export function text(scene, x, y, str, opts = {}) {
  const { color = UI.text, shadow = 0x000000, align = 'left', scale = 1, depth = 0 } = opts;
  const s = fontText(str);
  const sh = scene.add.bitmapText(1, 1, 'font', s).setTint(shadow);
  const t = scene.add.bitmapText(0, 0, 'font', s).setTint(color);
  if (opts.lineSpacing != null) { sh.setLineSpacing(opts.lineSpacing); t.setLineSpacing(opts.lineSpacing); }
  const c = scene.add.container(x, y, shadow === null ? [t] : [sh, t]).setScale(scale).setDepth(depth);
  c.main = t;
  c.shadowText = sh;
  c.align = align;
  c.baseX = x;
  const place = () => {
    const w = t.width * scale;
    c.x = align === 'center' ? Math.round(c.baseX - w / 2) : align === 'right' ? Math.round(c.baseX - w) : c.baseX;
  };
  c.setText = (v) => {
    const n = fontText(v);
    t.setText(n);
    sh.setText(n);
    place();
    return c;
  };
  c.setTint = (col) => { t.setTint(col); return c; };
  c.setX2 = (nx) => { c.baseX = nx; place(); return c; };
  Object.defineProperty(c, 'textWidth', { get: () => t.width * scale });
  place();
  return c;
}

/** Parte un texto en líneas de como mucho n caracteres. */
export function wrap(str, n) {
  const words = String(str).split(' ');
  const lines = [];
  let cur = '';
  for (const w of words) {
    if ((cur + ' ' + w).trim().length > n) {
      if (cur) lines.push(cur);
      cur = w;
    } else {
      cur = (cur + ' ' + w).trim();
    }
  }
  if (cur) lines.push(cur);
  return lines.join('\n');
}

/** Panel con borde doble estilo 16 bits. */
export function panel(scene, x, y, w, h, opts = {}) {
  const g = scene.add.graphics();
  const fill = opts.fill ?? UI.panel;
  const border = opts.border ?? UI.border;
  g.fillStyle(0x000000, 0.5).fillRect(x + 2, y + 2, w, h);
  g.fillStyle(UI.borderDark, 1).fillRect(x, y, w, h);
  g.fillStyle(border, 1).fillRect(x + 1, y + 1, w - 2, h - 2);
  g.fillStyle(UI.borderDark, 1).fillRect(x + 2, y + 2, w - 4, h - 4);
  g.fillStyle(fill, opts.alpha ?? 1).fillRect(x + 3, y + 3, w - 6, h - 6);
  if (opts.depth != null) g.setDepth(opts.depth);
  return g;
}

/** Barra horizontal con marco. Devuelve {set(frac, color)}. */
export function bar(scene, x, y, w, h, color, depth = 0) {
  const g = scene.add.graphics().setDepth(depth);
  const api = {
    g,
    set(frac, col = color) {
      g.clear();
      g.fillStyle(0x000000, 1).fillRect(x - 1, y - 1, w + 2, h + 2);
      g.fillStyle(0x303048, 1).fillRect(x, y, w, h);
      const fw = Math.round(Math.max(0, Math.min(1, frac)) * w);
      if (fw > 0) {
        g.fillStyle(col, 1).fillRect(x, y, fw, h);
        g.fillStyle(0xffffff, 0.35).fillRect(x, y, fw, 1);
      }
      return api;
    },
  };
  return api.set(0);
}
