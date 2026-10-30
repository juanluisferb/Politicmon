// Simula partidas con el bot (3 hurones x 3 niveles x varias semillas) e imprime el resultado medio.
import { World } from '../src/sim/world.js';
import { playLevel } from '../src/sim/bot.js';
import { LEVELS } from '../src/data/levels/index.js';
import { FERRET_ORDER } from '../src/data/ferrets.js';

const seeds = Number(process.argv[2] ?? 4);
for (const lv of LEVELS) {
  console.log(`\n== Nivel ${lv.id}: ${lv.name}  cuota ${lv.quota.join(' / ')}  (${lv.duration}s)`);
  for (const fid of FERRET_ORDER) {
    const rs = [];
    for (let s = 1; s <= seeds; s++) rs.push(playLevel(World, lv, fid, { seed: s * 101 }));
    const avg = (k) => (rs.reduce((a, r) => a + r[k], 0) / rs.length).toFixed(0);
    const avgS = (k) => (rs.reduce((a, r) => a + r.stats[k], 0) / rs.length).toFixed(1);
    console.log(`${fid.padEnd(7)} puntos ${avg('final').padStart(4)}  estrellas [${rs.map((r) => r.stars).join(',')}]`
      + `  pillado ${avgS('caught')}  persecuciones ${avgS('chases')}  escondidos ${avg('stashed')}  fin:${rs.map((r) => r.reason[0]).join('')}`);
  }
}
