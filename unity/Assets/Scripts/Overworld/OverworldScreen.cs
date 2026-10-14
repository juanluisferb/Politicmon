using System.Collections.Generic;
using Politicmon.Animation;
using Politicmon.Audio;
using Politicmon.Core;
using Politicmon.Data;
using Politicmon.UI;
using UnityEngine;

namespace Politicmon.Overworld
{
    /// <summary>
    /// The explorable world: loads a map, spawns the player, NPCs and items, moves the player on
    /// the grid, follows it with the camera and triggers warps, encounters and events.
    /// </summary>
    public class OverworldScreen : MonoBehaviour
    {
        const float WalkTime = 0.2f;
        const float RunTime = 0.11f;
        const float TurnDelay = 0.09f;

        public static OverworldScreen Instance { get; private set; }

        public MapView Map { get; private set; }
        public Character Player { get; private set; }
        public string MapId => Map.Data.id;
        public bool Busy { get; set; }

        public readonly Dictionary<string, Character> Npcs = new();
        public readonly Dictionary<Vector2Int, (GameObject node, MapItemData data)> Items = new();
        readonly Dictionary<Vector2Int, SignData> signs = new();
        readonly Dictionary<Vector2Int, WarpData> warps = new();

        Transform entities;
        SpriteRenderer grassOverlay;
        OverworldEvents events;
        string currentArea = "";
        float turnTimer;
        float bumpCooldown;

        public static OverworldScreen Create(string mapId, Vector2Int cell, string facing)
        {
            var go = new GameObject("Overworld");
            var screen = go.AddComponent<OverworldScreen>();
            screen.Map = new GameObject("Map").AddComponent<MapView>();
            screen.Map.transform.SetParent(go.transform, false);
            screen.entities = new GameObject("Entities").transform;
            screen.entities.SetParent(go.transform, false);
            screen.events = new OverworldEvents(screen);
            screen.LoadMap(mapId, cell, facing);
            return screen;
        }

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        async void Start()
        {
            await UIManager.FadeIn(0.3f);
            CheckArea();
            await events.OnMapLoaded();
        }

        // ---------------------------------------------------------------- loading

        public void LoadMap(string mapId, Vector2Int cell, string facing)
        {
            foreach (Transform child in entities)
                Destroy(child.gameObject);
            Npcs.Clear();
            Items.Clear();
            signs.Clear();
            warps.Clear();

            var data = GameData.Map(mapId);
            Map.Build(data);
            foreach (var w in data.warps)
                warps[new Vector2Int(w.x, w.y)] = w;
            foreach (var s in data.signs)
                signs[new Vector2Int(s.x, s.y)] = s;
            foreach (var item in data.items)
            {
                if (GameState.HasFlag("item:" + item.id))
                    continue;
                var go = new GameObject("Item_" + item.id);
                go.transform.SetParent(entities, false);
                var cellPos = new Vector2Int(item.x, item.y);
                go.transform.localPosition = MapView.CellToWorld(cellPos);
                var sr = go.AddComponent<SpriteRenderer>();
                var tex = Resources.Load<Texture2D>("Sprites/Items/" + GameData.Item(item.item).icon);
                sr.sprite = SpriteSheet.Cut(tex, 0, 0, 16, 16, new Vector2(0.5f, 0f));
                Items[cellPos] = (go, item);
            }
            foreach (var npc in data.npcs)
            {
                if (GameState.CheckCondition(npc.hideIf))
                    continue;
                Npcs[npc.id] = Character.Spawn(entities, npc.sprite, new Vector2Int(npc.x, npc.y), npc.facing, npc);
            }

            Player = Character.Spawn(entities, "player", cell, facing);
            grassOverlay = Player.AddOverlay(MapView.GrassOverlaySprite(Map.EncounterTileName()));
            GameState.SetPosition(mapId, cell, facing);
            currentArea = "";
            UpdateGrassOverlay();
            AudioManager.PlayMusic(data.music);
            SnapCamera();
        }

        // ---------------------------------------------------------------- queries

        public Character NpcAt(Vector2Int cell)
        {
            foreach (var npc in Npcs.Values)
                if (npc.Cell == cell)
                    return npc;
            return null;
        }

        public bool IsWalkable(Vector2Int cell)
        {
            var tile = Map.TileAt(cell);
            if (tile == null)
                return false;
            if (tile.door)
                return warps.ContainsKey(cell);
            if (tile.solid || tile.water)
                return false;
            return NpcAt(cell) == null && !Items.ContainsKey(cell) && cell != Player.Cell;
        }

        static bool WarpAllowed(WarpData w) =>
            GameState.BadgeCount >= w.requiredBadges && (string.IsNullOrEmpty(w.requiredFlag) || GameState.HasFlag(w.requiredFlag));

        // ---------------------------------------------------------------- per frame

        void Update()
        {
            bumpCooldown = Mathf.Max(0f, bumpCooldown - Time.deltaTime);
            if (Busy || UIManager.IsBusy || Player == null || Player.IsMoving || GameFlow.InBattle)
                return;

            if (GameInput.Pressed(GameButton.Start))
            {
                Async.Run(OpenMenu);
                return;
            }
            if (GameInput.Pressed(GameButton.A))
            {
                Async.Run(Interact);
                return;
            }

            string dir = HeldDirection();
            if (dir == null)
            {
                turnTimer = 0f;
                return;
            }
            if (dir != Player.Facing)
            {
                Player.Face(dir);
                GameState.Progress.facing = dir;
                turnTimer = TurnDelay;
                return;
            }
            if (turnTimer > 0f)
            {
                turnTimer -= Time.deltaTime;
                return;
            }
            Async.Run(() => TryMove(dir));
        }

        void LateUpdate() => SnapCamera();

        void SnapCamera()
        {
            var cam = Camera.main;
            if (cam == null || Player == null)
                return;
            var p = Player.transform.position + new Vector3(0f, 0.25f, 0f);
            cam.transform.position = new Vector3(Mathf.Round(p.x * 16f) / 16f, Mathf.Round(p.y * 16f) / 16f, -10f);
        }

        static string HeldDirection()
        {
            if (GameInput.Held(GameButton.Up)) return "up";
            if (GameInput.Held(GameButton.Down)) return "down";
            if (GameInput.Held(GameButton.Left)) return "left";
            if (GameInput.Held(GameButton.Right)) return "right";
            return null;
        }

        // ---------------------------------------------------------------- movement

        async Awaitable TryMove(string dir)
        {
            var target = Player.Cell + Character.DirectionVector(dir);
            if (warps.TryGetValue(target, out var lockedWarp) && !WarpAllowed(lockedWarp))
            {
                Busy = true;
                await UIManager.Say(string.IsNullOrEmpty(lockedWarp.deniedText) ? "No puedes pasar." : lockedWarp.deniedText);
                Busy = false;
                return;
            }
            if (!IsWalkable(target))
            {
                Player.StopWalking();
                if (bumpCooldown <= 0f)
                {
                    AudioManager.Sfx("bump");
                    bumpCooldown = 0.3f;
                }
                return;
            }
            Busy = true;
            bool running = GameInput.Held(GameButton.B);
            await Player.StepTo(target, running ? RunTime : WalkTime);
            GameState.SetPosition(MapId, target, Player.Facing);
            UpdateGrassOverlay();
            await AfterStep();
            Busy = false;
        }

        void UpdateGrassOverlay() => grassOverlay.enabled = Map.IsEncounterTile(Player.Cell);

        async Awaitable AfterStep()
        {
            if (warps.TryGetValue(Player.Cell, out var warp))
            {
                Player.StopWalking();
                await DoWarp(warp);
                return;
            }
            CheckArea();
            if (await events.CheckTrainers())
                return;
            var data = Map.Data;
            if (Map.IsEncounterTile(Player.Cell) && data.encounters.Count > 0 && GameState.Rng.NextDouble() < data.encounterRate)
            {
                Player.StopWalking();
                await events.WildBattle(data);
            }
        }

        public async Awaitable DoWarp(WarpData warp)
        {
            AudioManager.Sfx("door");
            await UIManager.FadeOut(0.2f);
            LoadMap(warp.toMap, new Vector2Int(warp.toX, warp.toY), warp.facing);
            await UIManager.FadeIn(0.2f);
            CheckArea();
            await events.OnMapLoaded();
        }

        void CheckArea()
        {
            string name = "";
            foreach (var area in Map.Data.areas)
            {
                if (area.Contains(Player.Cell.x, Player.Cell.y))
                {
                    name = area.name;
                    break;
                }
            }
            if (name == "" && currentArea == "")
                name = Map.Data.name;
            if (name != "" && name != currentArea)
            {
                currentArea = name;
                UIManager.Banner(name);
            }
        }

        // ---------------------------------------------------------------- interaction

        async Awaitable Interact()
        {
            var front = Player.FrontCell;
            var npc = NpcAt(front);
            if (npc == null && Map.TileName(front) == "counter")
                npc = NpcAt(front + Character.DirectionVector(Player.Facing));
            Busy = true;
            if (npc != null)
                await events.TalkTo(npc);
            else if (Items.ContainsKey(front))
                await events.PickUp(front);
            else if (signs.TryGetValue(front, out var sign))
            {
                if (sign.text == "PC")
                    await events.PartyStorage();
                else
                    await UIManager.Say(sign.text);
            }
            Busy = false;
        }

        async Awaitable OpenMenu()
        {
            Busy = true;
            AudioManager.Sfx("menu");
            await PauseMenu.Open();
            Busy = false;
        }

        public void RemoveNpc(string id)
        {
            if (Npcs.TryGetValue(id, out var npc))
            {
                Destroy(npc.gameObject);
                Npcs.Remove(id);
            }
        }

        public void RemoveItem(Vector2Int cell)
        {
            if (Items.TryGetValue(cell, out var entry))
            {
                Destroy(entry.node);
                Items.Remove(cell);
            }
        }
    }
}
