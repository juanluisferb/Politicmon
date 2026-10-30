// Los tres hurones seleccionables. Velocidades en px/s, energía en puntos.
export const FERRETS = {
  sardi: {
    id: 'sardi', name: 'Sardi', coat: 'Blanco', style: 'Rápido',
    desc: 'Corre como un rayo, pero se cansa antes.',
    walk: 74, run: 122, sneak: 38, energy: 70, noise: 1.0, slots: 1, carryPenalty: 1.0,
    stats: { speed: 5, stealth: 3, strength: 2, energy: 2 },
  },
  pepis: {
    id: 'pepis', name: 'Pepis', coat: 'Sable marrón', style: 'Sigiloso',
    desc: 'Sus patitas apenas hacen ruido.',
    walk: 66, run: 106, sneak: 36, energy: 100, noise: 0.6, slots: 1, carryPenalty: 1.0,
    stats: { speed: 3, stealth: 5, strength: 2, energy: 4 },
  },
  chonko: {
    id: 'chonko', name: 'Chonko', coat: 'Chocolate peludo', style: 'Fuerte',
    desc: 'Lleva 2 cosas pequeñas o 1 grande sin frenarse.',
    walk: 58, run: 94, sneak: 30, energy: 100, noise: 1.25, slots: 2, carryPenalty: 0.45,
    stats: { speed: 2, stealth: 2, strength: 5, energy: 4 },
  },
};
export const FERRET_ORDER = ['sardi', 'pepis', 'chonko'];
