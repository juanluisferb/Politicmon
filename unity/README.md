# Politicmon — versión Unity 6

Port completo del juego a **Unity 6 (6000.0 LTS)** con scripts en **C#**. Mismo contenido que la
versión Godot: 3 zonas, 25 políticos, 38 movimientos, 9 rivales, 3 Parlamentos, el Eterno Candidato,
captura, equipo de 6, Politidex, guardado, música y efectos chiptune, pixel art GBC.

## Abrir el proyecto

1. Instala **Unity 6** (cualquier 6000.0.x) con los módulos *Android Build Support* y/o *iOS Build Support*.
2. En Unity Hub: **Add → Add project from disk** y elige la carpeta `unity/` de este repositorio.
3. La primera vez Unity importa los assets (1-2 minutos). Si pregunta por el nuevo *Input System*,
   acepta (el juego funciona tanto con el Input System como con el Input Manager clásico).
4. Abre `Assets/Scenes/Main.unity` y pulsa **Play**. En la ventana *Game* elige una resolución vertical
   (por ejemplo 1080x1920).

Al abrirse por primera vez, el menú **Politicmon** configura solo los *Player Settings* (orientación
vertical, identificadores Android/iOS, icono y escena de build). Puedes repetirlo con
`Politicmon → Configurar proyecto (Player Settings)`.

### Controles
| Acción | Teclado | Móvil |
|---|---|---|
| Mover | Flechas / WASD | D-pad en pantalla |
| A (aceptar, hablar) | Z, Intro, Espacio | botón A / tocar la opción |
| B (cancelar, correr) | X, Retroceso, Mayús | botón B / tocar fuera del menú |
| START (menú) | Esc, M, Tab | botón START |

## Estructura de carpetas

```
Assets/
  Scenes/Main.unity            Escena única: contiene GameBootstrap
  Scripts/                     Código del juego (ensamblado Politicmon.Runtime)
    Core/                      Arranque, flujo entre pantallas, datos, estado, escalado, entrada
    Data/                      Clases [Serializable] que reflejan los JSON
    Battle/                    Politician, fórmulas (BattleLogic), pantalla de combate, HUD, objetos
    Overworld/                 Mapa (Tilemap), personajes, movimiento por casillas, eventos
    UI/                        Fuente bitmap, diálogos, menús, pantallas (equipo, Politidex, título...)
    Audio/                     AudioManager (música en bucle y efectos)
    Save/                      SaveSystem (JSON en persistentDataPath)
    Animation/                 Spritesheets y animación de personajes
  Editor/                      Importador pixel-art, menú Politicmon, builds Android/iOS
  Tests/EditMode/              Tests NUnit (Test Runner de Unity)
  Resources/
    Data/                      types, moves, politicians, items, parties, trainers, tiles, font (JSON)
    Data/Maps/                 11 mapas (filas de caracteres + leyenda, NPCs, warps, encuentros...)
    Sprites/Politicians/       NN_nombre_front.png (56x56), _back.png (32x32), _icon.png (16x16)
    Sprites/Characters/        Spritesheets 32x64 de overworld (4 direcciones x 2 frames), troceados
    Sprites/Trainers/          Rivales, líderes, jugador, mentora (56x56)
    Sprites/Items/             Iconos de objetos y medallas Escaño
    Sprites/Tiles/             metatiles.png (atlas 16x16) y tiles8.png (tiles 8x8)
    Sprites/UI/                Marco de diálogo, cursor, controles táctiles, icono
    Fonts/                     pixel8.png (fuente bitmap 8x8 con acentos, ñ, ¿ ¡)
    Audio/Music/, Audio/Sfx/   14 músicas en bucle y 20 efectos (WAV)
    Animations/Characters/     136 AnimationClips (.anim): walk_/idle_ x 4 direcciones x 17 personajes
  Art/ContactSheets/           Hojas de contacto para revisar todo el arte de un vistazo
Packages/manifest.json         uGUI, Tilemap, 2D Sprite, Input System, Test Framework
ProjectSettings/               Versión del editor y escena de build
Tools~/CompileCheck/           Compilación + tests sin Unity (.NET 8), ignorado por Unity
```

## Cómo está hecho (para orientarte rápido)

- **`GameBootstrap`** (en la escena) crea la cámara, `AudioManager`, `UIManager` y `TouchControls`,
  carga `GameData` y muestra el título. Si le das a Play en otra escena, se crea solo.
- **`GameFlow`** cambia entre pantallas: título → overworld → combate → evolución → final.
- **Flujo con `async/await`**: diálogos, menús y combates usan `Awaitable` de Unity 6, así que la lógica
  se lee de arriba abajo:
  ```csharp
  await UIManager.Say("¡Hola!");
  if (await UIManager.YesNo("¿Quieres guardar?"))
      SaveSystem.Save();
  ```
- **Pixel perfect**: `ScreenScaler` calcula un factor entero (mínimo 160x256 píxeles lógicos) y amplía el
  área visible al aspecto de cada móvil (más casillas, sin bandas). `PixelCamera` ajusta la cámara
  (1 unidad = 16 px) y los Canvas usan `ConstantPixelSize` con ese factor. Se respeta el *safe area*.
- **UI**: construida por código con `UIFactory` (paneles 9-slice, imágenes, texto). El texto se dibuja
  con `PixelText`, un `Graphic` de uGUI que usa la fuente bitmap propia (sin TextMeshPro).
  `UIManager` mantiene una pila de `Modal`: solo el de arriba lee la entrada.
- **Datos**: todo el contenido está en `Resources/Data/*.json` (tabla de tipos incluida). Para añadir un
  político, un movimiento o cambiar un mapa no hay que tocar código.
- **Combate**: `BattleLogic` contiene solo fórmulas (daño, tipos, STAB, críticos, bonus regional,
  estados, captura, experiencia, IA) y está cubierto por tests; `BattleScreen` es la presentación.
- **Overworld**: `MapView` construye un `Tilemap` en tiempo de ejecución desde el atlas de metatiles;
  `Character` mueve personajes casilla a casilla; `OverworldEvents` contiene la historia (inicial, Sede,
  tienda, entrenadores con línea de visión, líderes, legendario).
- **Animaciones**: `CharacterAnimator` reproduce los `.anim` de `Resources/Animations/Characters` con
  la API de Playables. Si faltan, cambia los frames por código, así que el juego siempre funciona. Se
  pueden regenerar con `Politicmon → Regenerar spritesheets y animaciones de personajes`.

## Builds

- **Android**: `Politicmon → Build → Android APK` (sale en `Builds/Android/politicmon.apk`), o desde
  línea de comandos:
  ```bash
  Unity -batchmode -quit -projectPath unity -executeMethod Politicmon.Editor.PolitikmonMenu.BuildAndroid
  ```
- **iOS**: `Politicmon → Build → iOS (proyecto Xcode)`, y después compílalo en Xcode (macOS).

## Tests

- En Unity: *Window → General → Test Runner → EditMode → Run All* (43 tests: tabla de tipos, daño,
  captura, experiencia/evolución, objetos, guardado y validación de todos los mapas).
- Sin Unity (por ejemplo en CI): `dotnet test "unity/Tools~/CompileCheck"` compila todos los scripts
  contra stubs de la API de Unity y ejecuta los mismos tests.

## Regenerar los assets

Todo el arte, el audio y los datos se generan con los scripts Python de `tools/` del repositorio:

```bash
tools/build_assets.sh            # regenera gráficos, fuente, audio, mapas y datos
python3 tools/export_unity.py    # los copia/convierte a unity/Assets (JSON, .meta, .anim)
```
