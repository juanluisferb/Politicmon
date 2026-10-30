// Nivel 1 — Piso pequeño (tutorial).
import { carve } from './build.js';

export default {
  id: 1,
  name: 'Piso pequeño',
  subtitle: 'Tutorial: aprende a robar sin que te pillen',
  duration: 240,
  quota: [90, 160, 230],
  map: carve(26, 16, [
    [1, 1, 8, 6, '.'],    // dormitorio
    [1, 8, 8, 4, ','],    // baño
    [10, 1, 9, 14, '.'],  // salón
    [20, 1, 5, 7, ','],   // cocina
    [19, 9, 6, 3, '.'],   // rincón del salón
    [1, 13, 24, 2, '.'],  // pasillo de entrada
    [12, 6, 6, 5, '_'],   // alfombra
  ], [
    [9, 4, 'd'], [5, 7, 'D'], [9, 10, 'D'], [19, 4, 'D'], [22, 8, 'D'],
  ]),
  start: { x: 4, y: 4 },
  furniture: [
    { type: 'bed', x: 1, y: 1 },
    { type: 'burrow', x: 2, y: 2 },
    { type: 'nightstand', x: 3, y: 1, id: 'mesilla' },
    { type: 'wardrobe', x: 6, y: 1 },
    { type: 'drawer', x: 1, y: 5, id: 'comoda' },
    { type: 'bathtub', x: 1, y: 8 },
    { type: 'sink', x: 6, y: 8 },
    { type: 'toilet', x: 8, y: 8 },
    { type: 'plant', x: 10, y: 1 },
    { type: 'bookshelf', x: 16, y: 1 },
    { type: 'sofa', x: 12, y: 6 },
    { type: 'tv', x: 12, y: 10 },
    { type: 'table', x: 15, y: 12 },
    { type: 'counter', x: 20, y: 1 }, { type: 'counter', x: 21, y: 1 }, { type: 'stove', x: 22, y: 1 },
    { type: 'counter', x: 23, y: 1 }, { type: 'fridge', x: 24, y: 1 },
    { type: 'table', x: 21, y: 4 }, { type: 'chair', x: 23, y: 4 },
    { type: 'shoerack', x: 22, y: 14, id: 'zapatero' },
    { type: 'playpen', x: 2, y: 13 },
  ],
  playpenExit: { x: 4, y: 14 },
  tubes: [[{ x: 8, y: 1 }, { x: 24, y: 7 }]],
  tv: { x: 12, y: 10, watch: [{ x: 13, y: 8 }] },
  sofaSpot: { x: 13, y: 8 },
  items: [
    { id: 'sock_red', x: 5, y: 4 },
    { id: 'sock_red', in: 'comoda' },
    { id: 'sunglasses', in: 'comoda' },
    { id: 'earring', in: 'mesilla' },
    { id: 'sock_blue', x: 11, y: 12 },
    { id: 'sock_blue', x: 3, y: 10 },
    { id: 'hairtie', x: 7, y: 10 },
    { id: 'remote', x: 17, y: 8 },
    { id: 'phone', x: 11, y: 3 },
    { id: 'pen', x: 17, y: 3 },
    { id: 'spoon', x: 20, y: 3 },
    { id: 'coin', x: 23, y: 6 },
    { id: 'sock_wool', x: 21, y: 7 },
    { id: 'glove', x: 20, y: 13 },
    { id: 'slipper', in: 'zapatero' },
    { id: 'ball', x: 14, y: 14 },
    { id: 'treat', x: 7, y: 14 },
    { id: 'cap', x: 23, y: 10 },
  ],
  owners: [{
    name: 'Dueño', skin: 'owner', start: { x: 21, y: 3 },
    routine: {
      calm: [
        { x: 21, y: 3, wait: 7, act: 'cook', face: -90, say: 'A ver qué desayuno...' },
        { x: 13, y: 8, wait: 9, act: 'sit', face: 90, say: '¿Qué echan hoy en la tele?' },
        { x: 5, y: 3, wait: 4, act: 'look', face: 180 },
        { x: 4, y: 10, wait: 5, act: 'wash', face: -90, say: 'Me lavo los dientes.' },
        { x: 12, y: 13, wait: 4, act: 'phone', face: 90, say: 'Hola, mamá...' },
      ],
      alert: [
        { x: 21, y: 3, wait: 4, act: 'cook', face: -90 },
        { x: 13, y: 8, wait: 4, act: 'look', face: 90 },
        { x: 5, y: 4, wait: 3, act: 'look', face: 180, say: 'Juraría que había algo aquí...' },
        { x: 21, y: 10, wait: 3, act: 'look', face: 180 },
        { x: 6, y: 13, wait: 3, act: 'look', face: 0 },
      ],
    },
  }],
  hints: [
    { x: 1, y: 1, w: 8, h: 6, text: 'Muévete con el joystick o WASD. Coge cosas con A.' },
    { x: 1, y: 1, w: 3, h: 3, text: 'Tu madriguera está bajo la cama: deja aquí lo robado con A.' },
    { x: 10, y: 1, w: 9, h: 5, text: 'Ojo con el cono de visión del dueño. Con SIGILO (C) apenas haces ruido.' },
    { x: 11, y: 5, w: 6, h: 4, text: 'El sofá vale x1.5, pero si el dueño sospecha, lo revisará.' },
    { x: 20, y: 1, w: 5, h: 7, text: 'CORRER (Shift) hace ruido. En el azulejo el calcetín de lana desliza más.' },
    { x: 16, y: 7, w: 3, h: 3, text: 'El mando es un potenciador: B enciende la tele y distrae al dueño (pierdes sus puntos).' },
    { x: 1, y: 13, w: 9, h: 2, text: 'Si te pillan, vas al corralito. Pulsa A rápido para escapar antes.' },
  ],
};
