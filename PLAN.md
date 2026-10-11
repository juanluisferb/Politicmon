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
3. [x] Gráficos y fuente
4. [x] Combate
5. [x] Captura
6. [x] Overworld, mapas y encuentros
7. [x] Entrenadores y gimnasios
8. [x] Sonido
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

### Fase 3 — hecho
- Librería de pixel art propia (`tools/art/canvas.py`, `chibi.py`): primitivas sin antialiasing
  sobre índices de paleta + plantillas de caracteres.
- 25 políticos: frontal 56x56 dibujado por personaje (`politicians_art.py`), espalda 32x32 e
  icono 16x16 (`small_art.py`). Paleta de 4 colores (1 transparente) por tipo.
- Entrenadores: 4 modelos de rival (uno por partido), 3 líderes, jugador, mentora, espalda del jugador.
- Overworld 16x16: 17 personajes x 4 direcciones x 2 frames desde plantillas dibujadas a mano.
- Iconos de objetos (6) + medalla Escaño (genérica y una por zona).
- Tileset: 95 metatiles 16x16 compuestos de 294 tiles 8x8 únicos, paleta de 4 colores por tile
  (`gen_tiles.py` -> `assets/tiles/*.png` + `data/tiles.json`).
- Fuente bitmap 8x8 proporcional con acentos/ñ/¿¡ (`gen_font.py`), marcos GBC y controles táctiles.
- Hojas de contacto en `docs/contact_sheets/`; `tools/check_assets.py` valida tamaños y paletas.
- `tools/build_assets.sh` regenera todo.

### Fase 4 — hecho
- `BattleScene`: combate 1 vs 1 por turnos a pantalla completa (fondo, plataformas, HUD con PS/EXP,
  cuadro de texto de ancho completo), intro con fundido, menú LUCHAR/EQUIPO/OBJETOS/HUIR,
  menú de movimientos con tipo/PP, orden por Agilidad, precisión, críticos, STAB, eficacia,
  bonus regional, estados (Escándalo, Imputado, En Campaña, Dimisión), cambios de estadísticas,
  drenaje/retroceso, IA (aleatoria en salvajes, táctica en entrenadores), EXP compartida (50%),
  subida de nivel, aprendizaje de movimientos con sustitución, evolución tras el combate,
  derrota (pierdes la mitad de fondos y vuelves a la Sede).
- `PartyScreen`, `SummaryScreen`, `HpBar`, `BattleHud`, `ItemEffects`.
- Escenarios de depuración + piloto automático (`--scenario=battle_auto --autopress=a`).

### Fase 5 — hecho
- Afiliación con Carnet Normal / Premium / Puesto en la Lista: probabilidad por PS restantes,
  estado y tipo de carnet; 4 comprobaciones (0-3 sacudidas) con animación de lanzamiento.
- Equipo máximo de 6; el resto va a la reserva de la Sede. Registro automático en la Politidex.
- No se puede afiliar a militantes de otros partidos (combates contra entrenadores).
- Tests de captura (probabilidad empírica, bonus de estado, legendario) y de objetos/inventario.

### Fase 6 — hecho
- `tools/gen_maps.py`: 11 mapas diseñados por código (plantillas de edificios) -> `data/maps/*.json`
  + vistas previas en `docs/maps/`. Madrid (Ministerios, Gran Vía, Barrio de Tertulias),
  Andalucía (Real de la Feria, Cortijo, Plaza del Ayuntamiento), Galicia (Pazo, Aldea con niebla,
  Puerto), garaje inicial, 3 Sedes, 3 Parlamentos y el Hemiciclo Nacional.
- `Overworld`: TileMapLayer construido en runtime desde el atlas, borde infinito, cámara centrada,
  movimiento por casillas (correr con B), giro con toque corto, colisiones, puertas/warps con
  condiciones (Escaños, banderas), transiciones entre zonas, nombres de área, hierba que tapa los pies.
- Encuentros salvajes por zona (tablas con pesos y rangos de nivel), objetos en el suelo,
  carteles, NPCs con diálogo, guardias que bloquean el paso hasta tener el Escaño necesario.
- Eventos: intro + elección de inicial, Sede (curar, tienda, Secretaría/reserva), entrenadores con
  línea de visión, líderes con Escaño y regalo, legendario.
- Menú de pausa (Equipo, Politidex, Objetos, Ficha, Guardar, Opciones).
- Guion de entradas para pruebas headless (`--inputs=up:3,a,...`).
- Tests de mapas: formato, warps, objetos en casillas válidas, 3 rivales + 1 líder por zona,
  tablas de encuentros por zona y alcanzabilidad (BFS).

### Fase 7 — hecho
- 9 rivales (3 por zona) de 4 partidos ficticios (PPI, ASC, MYT, PAS) con colores propios,
  frase de presentación, derrota y posterior; equipos de 1-3 con nivel creciente.
- 3 Parlamentos (Asamblea de Madrid / Mediático, Parlamento del Sur / Barón,
  Parlamento del Noroeste / Regionalista) con un rival de guardia y su líder.
- Escaño + regalo por líder; guardias que abren Andalucía (Escaño de Madrid) y Galicia
  (Escaño del Sur); el Hemiciclo Nacional se abre con 3 Escaños (legendario).
- Tests de progresión de niveles, partidos, sprites y especialidad de los líderes.

### Fase 8 — hecho
- `tools/gen_audio.py`: sintetizador chiptune (cuadradas 12,5/25/50 %, ruido LFSR, bombo) y
  partituras en notación compacta. 14 músicas en bucle (título, Madrid, Andalucía con aire
  flamenco, Galicia en 6/8 tipo muiñeira, combate, gimnasio, legendario, victoria, encuentro,
  medalla, evolución, Sede, garaje, final) y 20 efectos (golpe, captura, subida de nivel, curar...).
- `Audio` reproduce bucles (también con WAV comprimido) y efectos con varios canales.
- Tests: todo efecto/música referenciado en código o datos existe; los bucles cargan y duran <30 s.
