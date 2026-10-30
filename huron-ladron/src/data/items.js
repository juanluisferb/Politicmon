// Objetos robables. value = puntos de cuota, weight = cuánto frena (0..1),
// noise = ruido extra al moverse con él en la boca, size 's' pequeño / 'l' grande.
// power: potenciador que se puede gastar con B (se pierden sus puntos).
export const ITEMS = {
  sock_red:    { name: 'Calcetín rojo',     value: 8,  weight: 0.04, noise: 0,    size: 's', icon: 'sock', tint: 'red',    pair: 'red' },
  sock_blue:   { name: 'Calcetín azul',     value: 8,  weight: 0.04, noise: 0,    size: 's', icon: 'sock', tint: 'blue',   pair: 'blue' },
  sock_green:  { name: 'Calcetín verde',    value: 8,  weight: 0.04, noise: 0,    size: 's', icon: 'sock', tint: 'green',  pair: 'green' },
  sock_yellow: { name: 'Calcetín amarillo', value: 8,  weight: 0.04, noise: 0,    size: 's', icon: 'sock', tint: 'yellow', pair: 'yellow' },
  sock_wool:   { name: 'Calcetín de lana',  value: 12, weight: 0.05, noise: 0,    size: 's', icon: 'woolsock', power: 'slide' },
  keys:        { name: 'Llaves',            value: 25, weight: 0.12, noise: 0.35, size: 's', icon: 'keys', power: 'keys' },
  remote:      { name: 'Mando de la tele',  value: 20, weight: 0.12, noise: 0.05, size: 's', icon: 'remote', power: 'remote' },
  treat:       { name: 'Golosina',          value: 0,  weight: 0,    noise: 0,    size: 's', icon: 'treat', power: 'treat', instant: true },
  hairtie:     { name: 'Coletero',          value: 5,  weight: 0.02, noise: 0,    size: 's', icon: 'hairtie' },
  cap:         { name: 'Tapón',             value: 4,  weight: 0.02, noise: 0.05, size: 's', icon: 'cap' },
  pen:         { name: 'Bolígrafo',         value: 7,  weight: 0.04, noise: 0.05, size: 's', icon: 'pen' },
  spoon:       { name: 'Cuchara',           value: 10, weight: 0.08, noise: 0.25, size: 's', icon: 'spoon' },
  clothespin:  { name: 'Pinza',             value: 5,  weight: 0.02, noise: 0.05, size: 's', icon: 'clothespin' },
  ball:        { name: 'Pelota',            value: 6,  weight: 0.05, noise: 0.1,  size: 's', icon: 'ball' },
  coin:        { name: 'Moneda',            value: 15, weight: 0.03, noise: 0.2,  size: 's', icon: 'coin' },
  lipstick:    { name: 'Pintalabios',       value: 18, weight: 0.04, noise: 0.05, size: 's', icon: 'lipstick' },
  earring:     { name: 'Pendiente',         value: 35, weight: 0.01, noise: 0.05, size: 's', icon: 'earring' },
  glove:       { name: 'Guante',            value: 12, weight: 0.08, noise: 0,    size: 's', icon: 'glove' },
  sunglasses:  { name: 'Gafas de sol',      value: 30, weight: 0.12, noise: 0.15, size: 's', icon: 'sunglasses' },
  phone:       { name: 'Móvil',             value: 45, weight: 0.22, noise: 0.15, size: 'l', icon: 'phone' },
  wallet:      { name: 'Cartera',           value: 60, weight: 0.35, noise: 0.3,  size: 'l', icon: 'wallet' },
  slipper:     { name: 'Zapatilla',         value: 20, weight: 0.3,  noise: 0.1,  size: 'l', icon: 'slipper' },
};

export const POWER_INFO = {
  slide:  'B: ¡deslizarse a toda velocidad!',
  keys:   'B: lanzar las llaves para distraer',
  remote: 'B: encender la tele',
  treat:  '¡Subidón de golosina!',
};
