// Colores cuantizados a 3 bits por canal, como el VDP de la Mega Drive (512 colores posibles).
export function md(hex) {
  const n = parseInt(hex.slice(1), 16);
  const q = (v) => Math.round((Math.round((v / 255) * 7) * 255) / 7);
  const r = q((n >> 16) & 255);
  const g = q((n >> 8) & 255);
  const b = q(n & 255);
  return `#${((1 << 24) | (r << 16) | (g << 8) | b).toString(16).slice(1)}`;
}

export function mdPal(obj) {
  const out = {};
  for (const [k, v] of Object.entries(obj)) out[k] = v === 'none' ? v : md(v);
  return out;
}

export const hexToInt = (hex) => parseInt(md(hex).slice(1), 16);

// Paleta común para objetos, muebles e interfaz.
export const PAL = mdPal({
  o: '#1c1420', k: '#101018', w: '#f8f8f8', W: '#c8c8d0', g: '#909098', G: '#505058', s: '#d0d8e0',
  r: '#e03030', R: '#902020', y: '#f8d030', Y: '#b08818', b: '#3070e0', B: '#1c3c90', c: '#40c8e8',
  v: '#40b048', V: '#206828', p: '#f088b0', P: '#b04878', n: '#a86838', N: '#6a3c1c', u: '#9050c8', U: '#583080',
  t: '#d87040', T: '#984020', e: '#f0e0b0', E: '#c8a868', h: '#ffffff', z: '#302838',
  // madera, tela, piedra
  m: '#c08850', M: '#8a5a30', q: '#5c3818', a: '#e0e0d8', A: '#a8a8a0', l: '#b8d8f8', L: '#78a0d0',
  f: '#d85040', F: '#a03028', x: '#702018', i: '#e8c8a0', I: '#b89070',
});

export const TINTS = {
  red: ['#e03838', '#982020'],
  blue: ['#3878e8', '#1c3c98'],
  green: ['#48c050', '#207028'],
  yellow: ['#f8d038', '#b08818'],
};

export const UI = {
  bg: hexToInt('#101828'),
  panel: hexToInt('#1c2848'),
  panelDark: hexToInt('#0c1020'),
  border: hexToInt('#f8e8b0'),
  borderDark: hexToInt('#806838'),
  text: hexToInt('#ffffff'),
  gold: hexToInt('#f8d030'),
  red: hexToInt('#e83838'),
  green: hexToInt('#48d050'),
  blue: hexToInt('#48a0f8'),
  orange: hexToInt('#f89838'),
  gray: hexToInt('#8888a0'),
};

export const CSS = {
  text: md('#ffffff'),
  gold: md('#f8d030'),
  red: md('#e83838'),
  green: md('#48d050'),
  blue: md('#78b8f8'),
  orange: md('#f89838'),
  gray: md('#a0a0b8'),
  dark: md('#101828'),
  cream: md('#f8e8b0'),
};
