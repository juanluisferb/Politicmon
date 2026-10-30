import test from 'node:test';
import assert from 'node:assert/strict';
import { World } from '../src/sim/world.js';
import { TILE, F, parseMap, computeFlags, lineOfSight, moveBox, tileCenter } from '../src/sim/grid.js';
import { findPath } from '../src/sim/path.js';
import { computeScore, starsFor } from '../src/sim/score.js';
import { playLevel } from '../src/sim/bot.js';
import { LEVELS } from '../src/data/levels/index.js';
import { FERRET_ORDER } from '../src/data/ferrets.js';
import { carve } from '../src/data/levels/build.js';

const L1 = LEVELS[0];
const tick = (w, secs, input = {}) => {
  const n = Math.round(secs * 30);
  for (let i = 0; i < n; i++) w.update(1 / 30, input);
};
/** Mundo de prueba: una habitación vacía con el dueño muy lejos y quieto. */
function sandbox(extra = {}) {
  const def = {
    id: 99, name: 'Prueba', duration: 300, quota: [10, 20, 30],
    map: carve(20, 10, [[1, 1, 18, 8, '.']]),
    start: { x: 5, y: 5 },
    furniture: [
      { type: 'burrow', x: 2, y: 2 },
      { type: 'sofa', x: 10, y: 1 },
      { type: 'playpen', x: 16, y: 6 },
      { type: 'drawer', x: 2, y: 7, id: 'cajon' },
      ...(extra.furniture ?? []),
    ],
    playpenExit: { x: 15, y: 7 },
    items: extra.items ?? [],
    owners: [{ name: 'Dueño', skin: 'owner', start: { x: 18, y: 1 }, routine: { calm: [{ x: 18, y: 1, wait: 999 }] } }],
    tv: { x: 10, y: 1, watch: [{ x: 11, y: 4 }] },
  };
  const w = new World(def, extra.ferret ?? 'pepis', { seed: 1 });
  w.owners[0].angle = -Math.PI / 2; // mira a la pared
  return w;
}
const place = (w, tx, ty) => { w.ferret.x = tileCenter(tx); w.ferret.y = tileCenter(ty); };

test('A* encuentra caminos y respeta paredes', () => {
  const lv = { ...parseMap(carve(10, 5, [[1, 1, 3, 3, '.'], [6, 1, 3, 3, '.']], [[4, 2, 'D'], [5, 2, 'D']])), furniture: [] };
  computeFlags(lv);
  const p = findPath(lv, 1, 1, 8, 3, F.BLOCK_OWNER);
  assert.ok(p && p.length > 0);
  assert.ok(p.some((n) => n.x === 4 && n.y === 2), 'pasa por la puerta');
  assert.equal(findPath(lv, 1, 1, 0, 0, F.BLOCK_OWNER)?.length ?? 0, 0, 'meta en pared → casilla libre más cercana');
});

test('línea de visión bloqueada por paredes', () => {
  const lv = { ...parseMap(carve(10, 5, [[1, 1, 3, 3, '.'], [6, 1, 3, 3, '.']])), furniture: [] };
  computeFlags(lv);
  assert.equal(lineOfSight(lv, tileCenter(1), tileCenter(2), tileCenter(3), tileCenter(2)), true);
  assert.equal(lineOfSight(lv, tileCenter(1), tileCenter(2), tileCenter(7), tileCenter(2)), false);
});

test('el hurón choca con las paredes', () => {
  const lv = { ...parseMap(carve(6, 6, [[1, 1, 4, 4, '.']])), furniture: [] };
  computeFlags(lv);
  let x = tileCenter(2);
  let y = tileCenter(2);
  for (let i = 0; i < 100; i++) ({ x, y } = moveBox(lv, x, y, 5, 4, -2, 0, F.BLOCK_FERRET));
  assert.ok(x - 5 >= TILE - 0.01, `no atraviesa la pared (x=${x})`);
});

test('puntuación: sofá x1.5 y parejas x3', () => {
  const items = [
    { id: 'wallet', state: 'stashed', stash: 'burrow' },
    { id: 'keys', state: 'stashed', stash: 'sofa' },
    { id: 'sock_red', state: 'stashed', stash: 'burrow' },
    { id: 'sock_red', state: 'stashed', stash: 'burrow' },
    { id: 'phone', state: 'carried' },
  ];
  const s = computeScore(items);
  assert.equal(s.base, 60 + Math.round(25 * 1.5) + 8 + 8);
  assert.equal(s.pairCount, 1);
  assert.equal(s.pairBonus, 32);
  assert.equal(starsFor(100, [50, 100, 150]), 2);
});

test('coger, llevar y esconder en la madriguera suma a la cuota', () => {
  const w = sandbox({ items: [{ id: 'coin', x: 5, y: 5 }] });
  w.update(1 / 30, { a: true });
  assert.equal(w.ferret.carried.length, 1, 'lo lleva en la boca');
  assert.equal(w.score().total, 0, 'lo que llevas no cuenta');
  place(w, 2, 2);
  w.update(1 / 30, { a: true });
  assert.equal(w.ferret.carried.length, 0);
  assert.equal(w.score().total, 15);
});

test('capacidad: uno para Sardi/Pepis, dos pequeños o uno grande para Chonko', () => {
  const items = [{ id: 'coin', x: 5, y: 5 }, { id: 'pen', x: 5, y: 5 }, { id: 'wallet', x: 5, y: 5 }];
  const p = sandbox({ items, ferret: 'pepis' });
  p.update(1 / 30, { a: true });
  p.update(1 / 30, { a: true });
  assert.equal(p.ferret.carried.length, 1, 'con la boca llena, A cambia un objeto por otro');
  assert.equal(p.items.filter((i) => i.state === 'world').length, 2);
  const c = sandbox({ items, ferret: 'chonko' });
  c.update(1 / 30, { a: true });
  c.update(1 / 30, { a: true });
  assert.equal(c.ferret.carried.length, 2);
  assert.equal(c.canCarry({ size: 'l' }), false, 'con dos cosas no cabe una grande');
});

test('rebuscar en un contenedor saca el objeto tras un rato', () => {
  const w = sandbox({ items: [{ id: 'earring', in: 'cajon' }] });
  place(w, 3, 6);
  assert.equal(w.actionHint().kind, 'search');
  w.update(1 / 30, { a: true });
  assert.equal(w.ferret.state, 'search');
  tick(w, 1.5);
  assert.equal(w.ferret.carried[0]?.id, 'earring');
});

test('potenciador: usar el mando enciende la tele, distrae al dueño y gasta el objeto', () => {
  const w = sandbox({ items: [{ id: 'remote', x: 5, y: 5 }], furniture: [{ type: 'tv', x: 10, y: 8 }] });
  w.update(1 / 30, { a: true });
  w.update(1 / 30, { b: true });
  assert.ok(w.tvT > 0);
  assert.equal(w.items[0].state, 'used');
  assert.equal(w.owners[0].task?.kind, 'tv');
  tick(w, 8);
  assert.equal(w.owners[0].state, 'tv', 'se sienta a ver la tele');
});

test('potenciador: las llaves lanzadas atraen al dueño al punto de caída', () => {
  const w = sandbox({ items: [{ id: 'keys', x: 5, y: 5 }] });
  w.update(1 / 30, { a: true });
  w.ferret.fx = 1; w.ferret.fy = 0;
  w.update(1 / 30, { b: true });
  tick(w, 0.6);
  assert.equal(w.owners[0].state, 'suspicious');
  assert.ok(w.owners[0].lure);
});

test('potenciador: el calcetín de lana desliza más rápido que correr', () => {
  const w = sandbox({ items: [{ id: 'sock_wool', x: 3, y: 5 }] });
  place(w, 3, 5);
  w.update(1 / 30, { a: true });
  w.ferret.fx = 1; w.ferret.fy = 0;
  const x0 = w.ferret.x;
  w.update(1 / 30, { b: true });
  tick(w, 0.5);
  assert.ok(w.ferret.x - x0 > 90, `deslizó ${w.ferret.x - x0}px`);
});

test('el ruido alerta al dueño y va a investigar', () => {
  const w = sandbox();
  place(w, 15, 2);
  const o = w.owners[0];
  tick(w, 0.5, { mx: -1, run: true });
  assert.ok(['suspicious', 'chase'].includes(o.state), `estado ${o.state}`);
});

test('en sigilo apenas se hace ruido', () => {
  const w = sandbox();
  place(w, 15, 2);
  tick(w, 0.5, { mx: -1, sneak: true });
  assert.equal(w.owners[0].state, 'routine');
  assert.ok(w.ferret.noise < 0.12);
});

test('si te ve, te persigue, te atrapa y vas al corralito perdiendo lo que llevabas', () => {
  const w = sandbox({ items: [{ id: 'coin', x: 17, y: 3 }] });
  place(w, 17, 3);
  w.update(1 / 30, { a: true });
  const o = w.owners[0];
  o.angle = Math.PI / 2; // mira hacia el hurón
  o.lookBase = Math.PI / 2;
  let caught = false;
  for (let i = 0; i < 30 * 6 && !caught; i++) {
    w.update(1 / 30, {});
    caught = w.ferret.state === 'caught';
  }
  assert.ok(caught, 'atrapado');
  assert.equal(w.items[0].state, 'world', 'el objeto vuelve a su sitio');
  assert.equal(w.stats.caught, 1);
  const t0 = w.time;
  let released = false;
  for (let i = 0; i < 30 * 8 && !released; i++) { w.update(1 / 30, { a: i % 2 === 0 }); released = w.ferret.state === 'free'; }
  assert.ok(released && w.time - t0 < 4, 'pulsar A acorta el corralito');
});

test('escondido bajo la mesa no te ven', () => {
  const w = sandbox({ furniture: [{ type: 'table', x: 16, y: 3 }] });
  place(w, 16, 3);
  const o = w.owners[0];
  o.angle = Math.PI / 2;
  o.lookBase = Math.PI / 2;
  tick(w, 2);
  assert.equal(w.ferretVisibility(), 0);
  assert.equal(o.state, 'routine');
});

test('la sospecha cierra puertas y el dueño revisa el sofá', () => {
  const w = new World(L1, 'pepis', { seed: 3 });
  const sock = w.items.find((i) => i.id === 'sock_blue');
  sock.state = 'stashed';
  sock.stash = 'sofa';
  w.addSuspicion(40);
  assert.equal(w.tier, 1);
  assert.ok(w.level.doors.filter((d) => d.kind === 'closable').every((d) => d.closed));
  const o = w.owners[0];
  o.sofaCd = 0;
  o.backToRoutine();
  o.path = null;
  tick(w, 30);
  assert.equal(sock.state, 'world', 'calcetín recuperado del sofá');
});

test('nivel 2: lo que sueltas en el lavadero acaba en la lavadora', () => {
  const w = new World(LEVELS[1], 'pepis', { seed: 3 });
  const it = w.items.find((i) => i.id === 'clothespin');
  it.x = tileCenter(25);
  it.y = tileCenter(9); // fuera de su sitio, dentro del lavadero
  w.tidyItem(w.owners[0], it);
  assert.equal(it.state, 'lost');
});

test('dormir en la madriguera solo se ofrece al llegar a la cuota', () => {
  const w = sandbox({ items: [{ id: 'coin', x: 2, y: 2 }] });
  place(w, 2, 2);
  w.update(1 / 30, { a: true });
  w.update(1 / 30, { a: true });
  assert.ok(w.score().total >= 10);
  assert.equal(w.actionHint().kind, 'end');
  w.update(1 / 30, { a: true });
  assert.ok(w.drainEvents().some((e) => e.type === 'askEnd'));
  w.finish('sleep');
  assert.equal(w.result.win, true);
  assert.equal(w.result.stealthBonus, Math.round(15 * 0.15));
});

test('el día termina al acabarse el tiempo', () => {
  const w = sandbox();
  tick(w, 301);
  assert.ok(w.ended);
  assert.equal(w.result.win, false);
});

test('el bot completa los tres niveles con al menos un hurón', () => {
  for (const lv of LEVELS) {
    const wins = FERRET_ORDER.filter((fid) => [11, 22].some((seed) => playLevel(World, lv, fid, { seed }).win));
    assert.ok(wins.length >= 1, `nivel ${lv.id}: ningún hurón supera la cuota`);
  }
});
