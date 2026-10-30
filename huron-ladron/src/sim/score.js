// Puntuación: valor de lo escondido (x1.5 en el sofá) + bonus por parejas de calcetines.
import { ITEMS } from '../data/items.js';

export const SOFA_MULT = 1.5;
export const PAIR_MULT = 3;           // una pareja vale el triple
export const STEALTH_BONUS = 0.15;    // +15 % si nunca te persiguen

export function computeScore(items) {
  let base = 0;
  const pairs = {};
  for (const it of items) {
    if (it.state !== 'stashed') continue;
    const def = ITEMS[it.id];
    base += Math.round(def.value * (it.stash === 'sofa' ? SOFA_MULT : 1));
    if (def.pair) (pairs[def.pair] ??= []).push(def.value);
  }
  let pairBonus = 0;
  let pairCount = 0;
  for (const vals of Object.values(pairs)) {
    vals.sort((a, b) => b - a);
    for (let i = 0; i + 1 < vals.length; i += 2) {
      pairCount++;
      pairBonus += (PAIR_MULT - 1) * (vals[i] + vals[i + 1]);
    }
  }
  return { base, pairBonus, pairCount, total: base + pairBonus };
}

export function starsFor(score, quota) {
  let s = 0;
  for (const q of quota) if (score >= q) s++;
  return s;
}
