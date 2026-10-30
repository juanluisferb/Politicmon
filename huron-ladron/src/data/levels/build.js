// Construye el mapa ASCII de un nivel tallando habitaciones en un bloque de pared.
// rooms: [x, y, w, h, suelo] (suelo '.' madera, ',' azulejo, '_' alfombra); doors: [x, y, 'D' | 'd'].
export function carve(w, h, rooms, doors = []) {
  const g = Array.from({ length: h }, () => Array(w).fill('#'));
  for (const [x, y, rw, rh, c] of rooms) {
    for (let j = y; j < y + rh; j++) for (let i = x; i < x + rw; i++) g[j][i] = c;
  }
  for (const [x, y, c] of doors) g[y][x] = c;
  return g.map((r) => r.join(''));
}
