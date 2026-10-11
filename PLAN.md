# POLITICMON — Plan de proyecto

Juego móvil (vertical) estilo Pokémon de sátira política amable. Todo ficticio.
Motor: **Godot 4.7.2 stable** con GDScript. Sin assets externos: gráficos, fuente y audio
se generan con scripts de `tools/`.

## Estructura

```
project.godot            Configuración (viewport 180x320 base, stretch viewport, nearest)
export_presets.cfg       Presets Android / iOS
assets/                  Generados por tools/ (no editar a mano)
  sprites/politicians/   NN_nombre_front.png (56x56), _back.png (32x32), _icon.png (16x16)
  sprites/trainers/      Sprites de combate de entrenadores y líderes (56x56)
  sprites/overworld/     Hojas 16x16: 4 direcciones x 2 frames
  sprites/items/         Iconos 16x16 de objetos y medalla Escaño
  tiles/                 tiles8.png (tiles 8x8 únicos) y metatiles.png (16x16)
  ui/                    Marcos de diálogo, cursor, controles táctiles
  fonts/                 Fuente bitmap 8x8 (BMFont .fnt + .png)
  audio/                 Música y efectos chiptune (.wav)
data/                    Datos del juego en JSON (ampliables sin tocar código)
  types.json moves.json politicians.json items.json parties.json
  trainers.json palettes.json tiles.json maps/*.json
src/
  core/                  Autoloads: datos, estado de partida, escalado, router de escenas
  battle/                Lógica pura de combate (testeable) y escena de combate
  overworld/             Mapas, jugador, NPCs, encuentros, eventos
  ui/                    Diálogo, menús, pausa, Politidex, equipo, objetos, título, victoria
  save/                  Guardado local JSON (user://)
  audio/                 Gestor de música/efectos
scenes/                  Escenas .tscn
tests/                   Tests automatizados (runner propio headless)
tools/                   Generadores de assets y utilidades (Python + Pillow, scripts Godot)
```

## Arquitectura

- **Presentación**: resolución lógica dinámica. `ScreenScaler` calcula un factor entero
  `s = floor(min(w/160, h/256))` y fija `content_scale_size = (w/s, h/s)` con stretch mode
  `viewport` + escala entera + filtrado nearest. Así se ven más casillas en pantallas más
  altas/anchas en lugar de añadir bandas. La UI respeta el safe area (convertido a px lógicos).
- **Datos**: `GameData` (autoload) carga todos los JSON. La tabla de tipos vive en
  `data/types.json`.
- **Estado**: `GameState` (autoload) guarda equipo, reserva, objetos, fondos, medallas,
  Politidex, banderas, posición. `SaveSystem` serializa a `user://politicmon_save.json`.
- **Combate**: `BattleLogic` (funciones estáticas puras: daño, tipos, captura, experiencia,
  estados) + `Politician` (instancia) + `BattleScene` (presentación y flujo por turnos).
- **Overworld**: movimiento por casillas de 16 px, mapas en JSON (filas de caracteres +
  leyenda por zona), TileMapLayer construido en runtime desde el atlas de metatiles,
  objetos (NPCs, entrenadores, carteles, puertas, objetos del suelo) con eventos.
- **Entrada**: acciones `up/down/left/right/a/b/start`; teclado (flechas/WASD, Z/Enter,
  X/Backspace, Esc) y controles táctiles semitransparentes (D-pad izq., A/B dcha., START).
  Los menús también aceptan toques directos sobre cada opción.

## Fases

1. [x] Proyecto y presentación a pantalla completa
2. [x] Datos y tabla de tipos
3. [ ] Gráficos y fuente
4. [ ] Combate
5. [ ] Captura
6. [ ] Overworld, mapas y encuentros
7. [ ] Entrenadores y gimnasios
8. [ ] Sonido
9. [ ] Menús y guardado
10. [ ] Equilibrado, pulido y exportación

## Registro de progreso

### Fase 1 — hecho
- Godot 4.7.2 instalado (binario oficial) + plantillas de exportación.
- `project.godot` (vertical, stretch viewport, escala entera, nearest, GL Compatibility).
- `ScreenScaler`: factor entero dinámico y resolución lógica que llena la pantalla; safe area.
- UI base: fuente bitmap 8x8 propia (BMFont), marco de diálogo GBC, `DialogBox`, `MenuList`,
  autoload `Ui` (diálogos, menús, fundidos, carteles), controles táctiles semitransparentes.
- Herramientas: `tools/screenshot.sh` (capturas reales con Xvfb), `tools/run_tests.sh`.
- Tests: escalado entero sin bandas en 1080x1920, 1170x2532, 720x1600, 1536x2048.


### Fase 2 — hecho
- `data/types.json` (10 tipos, tabla completa en datos, bonus regional x1.3),
  `moves.json` (38 movimientos + Balbuceo), `politicians.json` (25, generado desde
  `tools/data_politicians.py`), `items.json`, `parties.json`, `trainers.json` (9 rivales + 3 líderes),
  `palettes.json` (paletas de 4 colores para sprites, tiles, objetos y UI).
- `GameData` (carga JSON), `Politician` (stats, niveles, movimientos, evolución, serialización),
  `BattleLogic` (daño, tipos, STAB, críticos, regionalismo, estados, captura, experiencia, IA).
- Tests: tabla de tipos, daño, captura, experiencia/evolución (36 tests en verde).
