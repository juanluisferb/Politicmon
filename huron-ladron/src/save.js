// Progreso guardado en localStorage (si no está disponible, el juego funciona igual sin guardar).
const KEY = 'huron.save.v1';

function read() {
  try {
    const raw = localStorage.getItem(KEY);
    if (raw) return JSON.parse(raw);
  } catch { /* sin almacenamiento */ }
  return null;
}

export const save = {
  data: { unlocked: 1, best: {}, ferret: 'pepis' },

  load() {
    const d = read();
    if (d && typeof d === 'object') this.data = { ...this.data, ...d, best: { ...(d.best || {}) } };
    return this.data;
  },

  write() {
    try { localStorage.setItem(KEY, JSON.stringify(this.data)); } catch { /* nada */ }
  },

  /** Registra un resultado. Devuelve true si es récord. */
  record(levelId, result, totalLevels) {
    const prev = this.data.best[levelId];
    let isBest = false;
    if (!prev || result.final > prev.score) {
      this.data.best[levelId] = { score: result.final, stars: Math.max(result.stars, prev?.stars ?? 0) };
      isBest = true;
    } else if (result.stars > prev.stars) {
      prev.stars = result.stars;
    }
    if (result.win) this.data.unlocked = Math.min(totalLevels, Math.max(this.data.unlocked, levelId + 1));
    this.write();
    return isBest;
  },
};
