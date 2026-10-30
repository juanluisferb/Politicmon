import test from 'node:test';
import assert from 'node:assert/strict';
import { LEVELS } from '../src/data/levels/index.js';
import { World } from '../src/sim/world.js';
import { F, flagAt, furnitureRect, TILE } from '../src/sim/grid.js';
import { findPath } from '../src/sim/path.js';
import { FURNITURE } from '../src/sim/furniture.js';

for (const def of LEVELS) {
  test(`nivel ${def.id} (${def.name}): datos coherentes`, () => {
    const w = new World(def, 'sardi');
    const lv = w.level;
    assert.ok(def.map.every((r) => r.length === lv.w), 'todas las filas del mapa miden lo mismo');
    assert.equal(def.quota.length, 3);
    assert.ok(def.quota[0] < def.quota[1] && def.quota[1] < def.quota[2]);
    assert.ok(w.burrow && w.playpen && w.sofa, 'madriguera, corralito y sofá');

    // Los muebles están dentro y sobre suelo (no sobre paredes).
    for (const f of lv.furniture) {
      const r = furnitureRect(f);
      for (let y = r.y; y < r.y + r.h; y++) for (let x = r.x; x < r.x + r.w; x++) {
        assert.ok(!(flagAt(lv, x, y) & F.WALL), `${f.type} en ${x},${y} pisa una pared`);
      }
    }
    const ferretFree = (x, y) => !(flagAt(lv, x, y) & F.BLOCK_FERRET);
    const start = def.start;
    assert.ok(ferretFree(start.x, start.y), 'inicio libre');
    const reach = (x, y) => findPath(lv, start.x, start.y, x, y, F.BLOCK_FERRET, 20000) !== null;

    // Cada objeto del suelo es alcanzable; cada contenedor tiene una casilla adyacente alcanzable.
    for (const it of def.items) {
      if (it.in) {
        const c = lv.furniture.find((f) => f.uid === it.in);
        assert.ok(c && FURNITURE[c.type].container, `contenedor ${it.in}`);
      } else {
        assert.ok(ferretFree(it.x, it.y), `objeto ${it.id} en ${it.x},${it.y} sobre casilla bloqueada`);
        assert.ok(reach(it.x, it.y), `objeto ${it.id} inalcanzable`);
      }
    }
    for (const c of lv.furniture.filter((f) => FURNITURE[f.type].container)) {
      const r = furnitureRect(c);
      let ok = false;
      for (let y = r.y - 1; y <= r.y + r.h && !ok; y++) for (let x = r.x - 1; x <= r.x + r.w && !ok; x++) {
        if (ferretFree(x, y) && reach(x, y)) ok = true;
      }
      assert.ok(ok, `contenedor ${c.uid} sin acceso`);
    }
    assert.ok(reach(w.burrow.x, w.burrow.y), 'madriguera alcanzable');
    assert.ok(ferretFree(def.playpenExit.x, def.playpenExit.y), 'salida del corralito libre');
    for (const [a, b] of def.tubes ?? []) {
      assert.ok(ferretFree(a.x, a.y) && ferretFree(b.x, b.y), 'bocas de tubo libres');
    }

    // Los waypoints del dueño son pisables y alcanzables por él.
    for (const o of def.owners) {
      const all = [o.start, ...Object.values(o.routine).flat(), ...(def.tv?.watch ?? []), def.sofaSpot];
      for (const p of all) {
        assert.ok(!(flagAt(lv, p.x, p.y) & F.BLOCK_OWNER), `${o.name}: punto ${p.x},${p.y} bloqueado`);
        assert.ok(findPath(lv, o.start.x, o.start.y, p.x, p.y, F.BLOCK_OWNER, 20000) !== null, `${o.name}: ${p.x},${p.y} inalcanzable`);
      }
    }
    // Con todo robado y escondido se pueden conseguir las 3 estrellas.
    for (const it of w.items) if (it.id !== 'treat') { it.state = 'stashed'; it.stash = 'burrow'; }
    assert.ok(w.score().total >= def.quota[2], `máximo ${w.score().total} < ${def.quota[2]}`);
    void TILE;
  });
}
