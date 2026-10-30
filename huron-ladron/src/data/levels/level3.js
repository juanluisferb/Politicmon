// Nivel 3 — Visita en casa: dos humanos, y la invitada trae bolso y abrigo llenos de cosas.
import { carve } from './build.js';

export default {
  id: 3,
  name: 'Visita en casa',
  subtitle: 'Dos humanos vigilando. El bolso de la invitada es un tesoro.',
  duration: 300,
  quota: [170, 290, 410],
  map: carve(34, 18, [
    [1, 1, 9, 6, '.'],     // dormitorio
    [11, 1, 7, 6, '.'],    // despacho
    [19, 1, 14, 6, ','],   // cocina-comedor
    [1, 8, 9, 5, ','],     // baño
    [11, 8, 15, 5, '.'],   // salón
    [27, 8, 6, 9, '.'],    // recibidor
    [1, 14, 25, 3, '.'],   // pasillo
    [13, 9, 7, 3, '_'],    // alfombra
  ], [
    [10, 3, 'd'], [18, 3, 'D'], [5, 7, 'D'], [5, 13, 'D'], [14, 7, 'd'], [21, 7, 'D'],
    [14, 13, 'D'], [22, 13, 'd'], [26, 10, 'D'], [26, 15, 'D'],
  ]),
  start: { x: 4, y: 4 },
  furniture: [
    { type: 'bed', x: 1, y: 1 },
    { type: 'burrow', x: 2, y: 2 },
    { type: 'nightstand', x: 3, y: 1, id: 'mesilla' },
    { type: 'wardrobe', x: 7, y: 1 },
    { type: 'drawer', x: 1, y: 5, id: 'comoda' },
    { type: 'desk', x: 12, y: 1 },
    { type: 'chair', x: 12, y: 2 },
    { type: 'bookshelf', x: 15, y: 1 },
    { type: 'drawer', x: 15, y: 5, id: 'cajonera' },
    { type: 'counter', x: 19, y: 1 }, { type: 'counter', x: 20, y: 1 }, { type: 'counter', x: 21, y: 1 },
    { type: 'stove', x: 22, y: 1 }, { type: 'counter', x: 23, y: 1 }, { type: 'fridge', x: 32, y: 1 },
    { type: 'table', x: 26, y: 3 }, { type: 'chair', x: 25, y: 3 }, { type: 'chair', x: 28, y: 4 },
    { type: 'bathtub', x: 1, y: 8 }, { type: 'sink', x: 6, y: 8 }, { type: 'toilet', x: 9, y: 8 },
    { type: 'plant', x: 11, y: 8 },
    { type: 'sofa', x: 14, y: 8 },
    { type: 'tv', x: 14, y: 12 },
    { type: 'bookshelf', x: 23, y: 8 },
    { type: 'table', x: 21, y: 10 },
    { type: 'coatrack', x: 32, y: 8, id: 'abrigo' },
    { type: 'bag', x: 28, y: 11, id: 'bolso' },
    { type: 'shoerack', x: 30, y: 16, id: 'zapatero' },
    { type: 'plant', x: 27, y: 16 },
    { type: 'playpen', x: 2, y: 15 },
  ],
  playpenExit: { x: 4, y: 16 },
  tubes: [[{ x: 17, y: 6 }, { x: 32, y: 14 }], [{ x: 9, y: 12 }, { x: 31, y: 6 }]],
  tv: { x: 14, y: 12, watch: [{ x: 15, y: 10 }, { x: 17, y: 10 }] },
  sofaSpot: { x: 16, y: 10 },
  items: [
    { id: 'earring', in: 'mesilla' },
    { id: 'sock_red', in: 'comoda' }, { id: 'sock_red', x: 6, y: 4 },
    { id: 'pen', x: 13, y: 4 }, { id: 'coin', in: 'cajonera' }, { id: 'glove', x: 16, y: 3 },
    { id: 'spoon', x: 24, y: 5 }, { id: 'spoon', x: 29, y: 2 }, { id: 'cap', x: 20, y: 4 },
    { id: 'sock_wool', x: 31, y: 4 },
    { id: 'hairtie', x: 3, y: 11 }, { id: 'lipstick', x: 8, y: 11 },
    { id: 'remote', x: 19, y: 10 },
    { id: 'phone', x: 12, y: 11 },
    { id: 'sock_blue', x: 24, y: 11 }, { id: 'sock_blue', x: 8, y: 15 },
    { id: 'keys', x: 29, y: 9 },
    { id: 'wallet', in: 'abrigo' }, { id: 'glove', in: 'abrigo' },
    { id: 'sunglasses', in: 'bolso' }, { id: 'lipstick', in: 'bolso' }, { id: 'phone', in: 'bolso' },
    { id: 'slipper', in: 'zapatero' },
    { id: 'treat', x: 18, y: 15 },
    { id: 'ball', x: 11, y: 15 },
    { id: 'clothespin', x: 24, y: 16 },
  ],
  owners: [
    {
      name: 'Dueño', skin: 'owner', start: { x: 21, y: 3 },
      routine: {
        calm: [
          { x: 21, y: 3, wait: 7, act: 'cook', face: -90, say: '¡La paella está casi lista!' },
          { x: 29, y: 3, wait: 4, act: 'look', face: 180 },
          { x: 13, y: 3, wait: 5, act: 'phone', face: -90, say: 'Un correo del trabajo...' },
          { x: 5, y: 4, wait: 3, act: 'look', face: 180 },
          { x: 18, y: 11, wait: 6, act: 'sit', face: 90, say: '¿Qué tal el viaje?' },
          { x: 20, y: 15, wait: 3, act: 'look', face: 180 },
        ],
        alert: [
          { x: 21, y: 4, wait: 3, act: 'look', face: 180 },
          { x: 13, y: 4, wait: 3, act: 'look', face: 0 },
          { x: 5, y: 5, wait: 3, act: 'look', face: 180, say: 'El bicho anda suelto...' },
          { x: 7, y: 15, wait: 3, act: 'look', face: 0 },
          { x: 24, y: 9, wait: 3, act: 'look', face: 180 },
        ],
        paranoid: [
          { x: 5, y: 4, wait: 2, act: 'look', face: 180 },
          { x: 13, y: 5, wait: 2, act: 'look', face: 0 },
          { x: 24, y: 4, wait: 2, act: 'look', face: 0 },
          { x: 29, y: 13, wait: 2, act: 'look', face: 180 },
          { x: 12, y: 15, wait: 2, act: 'look', face: 0 },
        ],
      },
    },
    {
      name: 'Invitada', skin: 'guest', start: { x: 29, y: 13 }, speed: 0.85,
      lines: {
        missing: '¿Alguien ha visto mi {item}?',
        hear: ['¿Habéis oído eso?', '¡Ay! ¿Qué ha sido?'],
        chase: ['¡Ahí va el hurón!', '¡Que se lleva mis cosas!'],
        caught: '¡Te pillé, ladronzuelo!',
        tv: '¡Uy, mi programa favorito!',
        giveUp: 'Bueno, da igual.',
      },
      routine: {
        calm: [
          { x: 29, y: 13, wait: 5, act: 'look', face: -90, say: '¡Qué casa más bonita!' },
          { x: 16, y: 10, wait: 9, act: 'sit', face: 90, say: '¡Qué sofá más cómodo!' },
          { x: 27, y: 5, wait: 5, act: 'look', face: -90, say: '¡Qué bien huele!' },
          { x: 4, y: 10, wait: 5, act: 'wash', face: -90 },
          { x: 29, y: 10, wait: 3, act: 'look', face: 90, say: 'Voy a mirar el móvil...' },
        ],
        alert: [
          { x: 29, y: 12, wait: 4, act: 'look', face: 90, say: 'Vigilo mi bolso, por si acaso.' },
          { x: 16, y: 10, wait: 4, act: 'look', face: 90 },
          { x: 30, y: 9, wait: 3, act: 'look', face: 0 },
          { x: 22, y: 15, wait: 3, act: 'look', face: 180 },
        ],
      },
    },
  ],
  hints: [
    { x: 1, y: 1, w: 9, h: 6, text: 'Hay visita: dos humanos vigilando. La invitada dejó bolso y abrigo en el recibidor.' },
    { x: 27, y: 8, w: 6, h: 9, text: 'El bolso y el abrigo esconden lo más valioso. Rebuscar hace ruido.' },
  ],
};
