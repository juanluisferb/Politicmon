// Nivel 2 — Día de colada: muchos calcetines, la cesta va y viene y lo que dejes en el lavadero acaba en la lavadora.
import { carve } from './build.js';

export default {
  id: 2,
  name: 'Día de colada',
  subtitle: 'Calcetines por todas partes... y una lavadora hambrienta',
  duration: 270,
  quota: [150, 240, 330],
  map: carve(30, 17, [
    [1, 1, 9, 6, '.'],     // dormitorio
    [11, 1, 8, 15, '.'],   // salón
    [20, 1, 9, 5, ','],    // cocina
    [20, 7, 9, 5, ','],    // lavadero
    [1, 8, 9, 4, ','],     // baño
    [1, 13, 28, 3, '.'],   // pasillo
    [12, 4, 6, 6, '_'],    // alfombra
  ], [
    [10, 4, 'd'], [5, 7, 'D'], [5, 12, 'D'], [19, 3, 'D'], [24, 6, 'd'], [23, 12, 'D'],
  ]),
  start: { x: 4, y: 4 },
  furniture: [
    { type: 'bed', x: 1, y: 1 },
    { type: 'burrow', x: 2, y: 2 },
    { type: 'nightstand', x: 3, y: 1, id: 'mesilla' },
    { type: 'wardrobe', x: 7, y: 1 },
    { type: 'drawer', x: 1, y: 5, id: 'comoda' },
    { type: 'basket', id: 'cesta', positions: [{ x: 22, y: 8 }, { x: 8, y: 5 }] },
    { type: 'bathtub', x: 1, y: 8 },
    { type: 'sink', x: 6, y: 8 },
    { type: 'toilet', x: 9, y: 8 },
    { type: 'plant', x: 11, y: 1 },
    { type: 'bookshelf', x: 16, y: 1 },
    { type: 'sofa', x: 12, y: 6 },
    { type: 'tv', x: 12, y: 10 },
    { type: 'desk', x: 16, y: 12 },
    { type: 'counter', x: 20, y: 1 }, { type: 'counter', x: 21, y: 1 }, { type: 'counter', x: 22, y: 1 },
    { type: 'stove', x: 23, y: 1 }, { type: 'fridge', x: 28, y: 1 },
    { type: 'table', x: 24, y: 3 }, { type: 'chair', x: 26, y: 3 },
    { type: 'washer', x: 20, y: 8 }, { type: 'washer', x: 20, y: 9 },
    { type: 'coatrack', x: 28, y: 13, id: 'abrigo' },
    { type: 'shoerack', x: 19, y: 15, id: 'zapatero' },
    { type: 'plant', x: 28, y: 15 },
    { type: 'playpen', x: 2, y: 14 },
  ],
  playpenExit: { x: 4, y: 15 },
  laundryZone: { x: 20, y: 7, w: 9, h: 5 },
  tubes: [[{ x: 9, y: 1 }, { x: 28, y: 11 }]],
  tv: { x: 12, y: 10, watch: [{ x: 13, y: 8 }] },
  sofaSpot: { x: 13, y: 8 },
  items: [
    { id: 'sock_green', in: 'cesta' }, { id: 'sock_yellow', in: 'cesta' }, { id: 'sock_yellow', in: 'cesta' },
    { id: 'sock_red', in: 'cesta' }, { id: 'sock_blue', in: 'cesta' },
    { id: 'sock_green', x: 14, y: 12 },
    { id: 'sock_red', x: 26, y: 9 },
    { id: 'sock_blue', x: 5, y: 4 },
    { id: 'earring', in: 'mesilla' },
    { id: 'sunglasses', in: 'comoda' },
    { id: 'sock_wool', x: 22, y: 10 },
    { id: 'keys', x: 25, y: 14 },
    { id: 'remote', x: 17, y: 8 },
    { id: 'treat', x: 12, y: 14 },
    { id: 'clothespin', x: 24, y: 10 },
    { id: 'coin', x: 22, y: 5 },
    { id: 'spoon', x: 21, y: 3 },
    { id: 'wallet', in: 'abrigo' },
    { id: 'slipper', in: 'zapatero' },
    { id: 'phone', x: 15, y: 2 },
    { id: 'lipstick', x: 8, y: 10 },
    { id: 'ball', x: 18, y: 14 },
    { id: 'hairtie', x: 3, y: 9 },
  ],
  owners: [{
    name: 'Dueño', skin: 'owner', start: { x: 23, y: 9 },
    routine: {
      calm: [
        { x: 23, y: 9, wait: 6, act: 'wash', face: 180, say: 'Toca tender la ropa.', do: 'basket' },
        { x: 7, y: 4, wait: 5, act: 'wash', face: 0, say: 'Ropa limpia al cuarto.', do: 'basket' },
        { x: 22, y: 2, wait: 6, act: 'cook', face: -90, say: 'Un cafetito...' },
        { x: 13, y: 8, wait: 7, act: 'sit', face: 90 },
        { x: 5, y: 10, wait: 4, act: 'wash', face: -90 },
        { x: 15, y: 14, wait: 3, act: 'phone', face: 90, say: '¿Sí? Dime...' },
      ],
      alert: [
        { x: 23, y: 9, wait: 4, act: 'wash', face: 180, do: 'basket' },
        { x: 7, y: 4, wait: 3, act: 'look', face: 0, do: 'basket' },
        { x: 13, y: 3, wait: 3, act: 'look', face: 90 },
        { x: 25, y: 14, wait: 3, act: 'look', face: 180, say: '¿Dónde dejé las cosas?' },
        { x: 5, y: 14, wait: 3, act: 'look', face: 0 },
      ],
      paranoid: [
        { x: 23, y: 10, wait: 2, act: 'look', face: 180, do: 'basket' },
        { x: 7, y: 4, wait: 2, act: 'look', face: 180, do: 'basket' },
        { x: 13, y: 12, wait: 2, act: 'look', face: -90 },
        { x: 27, y: 4, wait: 2, act: 'look', face: 90 },
        { x: 10, y: 14, wait: 2, act: 'look', face: 0 },
      ],
    },
  }],
  hints: [
    { x: 1, y: 1, w: 9, h: 6, text: 'Día de colada: junta parejas de calcetines del mismo color (valen el triple).' },
    { x: 20, y: 7, w: 9, h: 5, text: 'Lavadero: lo que sueltes aquí, el dueño lo meterá en la lavadora.' },
    { x: 22, y: 13, w: 6, h: 3, text: 'Llaves: B para lanzarlas. El tintineo atrae al dueño.' },
  ],
};
