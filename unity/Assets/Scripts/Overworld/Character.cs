using Politicmon.Animation;
using Politicmon.Core;
using Politicmon.Data;
using UnityEngine;

namespace Politicmon.Overworld
{
    /// <summary>
    /// A character on the grid (player or NPC): position in cells, facing, animated sprite and
    /// smooth 16 px steps. Sorting follows the Y position (camera uses a custom Y sort axis).
    /// </summary>
    public class Character : MonoBehaviour
    {
        public static readonly string[] Directions = { "down", "up", "left", "right" };

        public Vector2Int Cell { get; private set; }
        public string Facing => animator.Facing;
        public bool IsMoving { get; private set; }
        public NpcData Npc { get; private set; }

        CharacterAnimator animator;
        SpriteRenderer spriteRenderer;
        Transform body;

        /// <summary>Feet are drawn 4 px above the bottom of the tile, like the Game Boy games.</summary>
        static readonly Vector3 FeetOffset = new(0f, 0.25f, 0f);

        public static Character Spawn(Transform parent, string sheet, Vector2Int cell, string facing, NpcData npc = null)
        {
            var go = new GameObject(npc != null ? "NPC_" + npc.id : "Player");
            go.transform.SetParent(parent, false);
            var character = go.AddComponent<Character>();
            character.Npc = npc;
            character.body = new GameObject("Sprite").transform;
            character.body.SetParent(go.transform, false);
            character.spriteRenderer = character.body.gameObject.AddComponent<SpriteRenderer>();
            character.animator = character.body.gameObject.AddComponent<CharacterAnimator>();
            character.animator.Setup(sheet, facing);
            character.Teleport(cell);
            return character;
        }

        public static Vector2Int DirectionVector(string dir) => dir switch
        {
            "up" => new Vector2Int(0, -1),
            "left" => new Vector2Int(-1, 0),
            "right" => new Vector2Int(1, 0),
            _ => new Vector2Int(0, 1),
        };

        public static string Opposite(string dir) => dir switch
        {
            "up" => "down",
            "down" => "up",
            "left" => "right",
            _ => "left",
        };

        public Vector2Int FrontCell => Cell + DirectionVector(Facing);

        public void Face(string dir) => animator.SetState(dir, false);

        public void Teleport(Vector2Int cell)
        {
            Cell = cell;
            transform.localPosition = MapView.CellToWorld(cell) + FeetOffset;
        }

        /// <summary>Walks one tile in the current facing direction (Cell updates immediately).</summary>
        public async Awaitable StepTo(Vector2Int target, float duration)
        {
            IsMoving = true;
            var from = transform.localPosition;
            var to = MapView.CellToWorld(target) + FeetOffset;
            Cell = target;
            animator.SetState(Facing, true);
            await Async.Tween(duration, t =>
            {
                var p = Vector3.Lerp(from, to, t);
                transform.localPosition = new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, 0f);
            });
            transform.localPosition = to;
            if (!GameInputHeldTowards())
                animator.SetState(Facing, false);
            IsMoving = false;
        }

        /// <summary>Keeps the walking animation running between consecutive steps.</summary>
        bool GameInputHeldTowards() => this == OverworldScreen.Instance?.Player && GameInput.Held(Facing switch
        {
            "up" => GameButton.Up,
            "left" => GameButton.Left,
            "right" => GameButton.Right,
            _ => GameButton.Down,
        });

        public void StopWalking() => animator.SetState(Facing, false);

        /// <summary>Adds a child sprite drawn on top (tall grass covering the feet).</summary>
        public SpriteRenderer AddOverlay(Sprite sprite)
        {
            var go = new GameObject("GrassOverlay");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = -FeetOffset;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 1;
            return sr;
        }

        /// <summary>The "!" bubble shown when a trainer spots the player.</summary>
        public GameObject ShowExclamation()
        {
            var go = new GameObject("Exclamation");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = Resources.Load<Texture2D>("Sprites/UI/exclaim");
            sr.sprite = SpriteSheet.Cut(tex, 0, 0, tex.width, tex.height, new Vector2(0.5f, 0f));
            sr.sortingOrder = 10;
            return go;
        }
    }
}
