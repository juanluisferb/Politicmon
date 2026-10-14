using System.Collections.Generic;
using Politicmon.Core;
using Politicmon.Data;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Politicmon.Overworld
{
    /// <summary>
    /// Draws a map with a Tilemap built at runtime from the metatile atlas (Sprites/Tiles/metatiles).
    /// Map coordinates: x to the right, y downwards (as in the JSON rows). Cell (x, y) is drawn at
    /// Tilemap position (x, -y - 1). A border of trees/walls fills everything around the map.
    /// </summary>
    public class MapView : MonoBehaviour
    {
        const int Border = 16;

        static Tile[] tileCache;

        public MapData Data { get; private set; }
        public int Width => Data.width;
        public int Height => Data.height;

        string[,] tileNames;
        Tilemap tilemap;

        public static Vector3 CellToWorld(Vector2Int cell) => new(cell.x + 0.5f, -cell.y - 1f, 0f);

        static Tile[] Tiles()
        {
            if (tileCache != null)
                return tileCache;
            var atlas = Resources.Load<Texture2D>("Sprites/Tiles/metatiles");
            atlas.filterMode = FilterMode.Point;
            int columns = GameData.TileColumns;
            tileCache = new Tile[GameData.TileCount];
            for (int i = 0; i < tileCache.Length; i++)
            {
                var tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = Animation.SpriteSheet.Cut(atlas, i % columns * 16, i / columns * 16, 16, 16, new Vector2(0.5f, 0.5f));
                tile.name = "metatile_" + i;
                tileCache[i] = tile;
            }
            return tileCache;
        }

        public void Build(MapData data)
        {
            Data = data;
            var legend = new Dictionary<char, string>();
            foreach (var entry in data.legend)
                legend[entry.symbol[0]] = entry.tile;

            tileNames = new string[data.width, data.height];
            for (int y = 0; y < data.height; y++)
            for (int x = 0; x < data.width; x++)
                tileNames[x, y] = legend[data.rows[y][x]];

            if (tilemap == null)
            {
                gameObject.AddComponent<Grid>();
                var layer = new GameObject("Ground");
                layer.transform.SetParent(transform, false);
                tilemap = layer.AddComponent<Tilemap>();
                var renderer = layer.AddComponent<TilemapRenderer>();
                renderer.sortingOrder = -100;
            }
            tilemap.ClearAllTiles();

            var tiles = Tiles();
            string borderTile = legend.TryGetValue(data.border[0], out var b) ? b : "void";
            int w = data.width + Border * 2;
            int h = data.height + Border * 2;
            var positions = new Vector3Int[w * h];
            var placed = new TileBase[w * h];
            int n = 0;
            for (int y = -Border; y < data.height + Border; y++)
            for (int x = -Border; x < data.width + Border; x++)
            {
                bool inside = x >= 0 && y >= 0 && x < data.width && y < data.height;
                var info = GameData.Tile(inside ? tileNames[x, y] : borderTile);
                positions[n] = new Vector3Int(x, -y - 1, 0);
                placed[n] = tiles[info?.index ?? 0];
                n++;
            }
            tilemap.SetTiles(positions, placed);
        }

        public string TileName(Vector2Int c) =>
            c.x < 0 || c.y < 0 || c.x >= Data.width || c.y >= Data.height ? null : tileNames[c.x, c.y];

        public TileData TileAt(Vector2Int c) => GameData.Tile(TileName(c));

        public bool IsEncounterTile(Vector2Int c) => TileAt(c)?.encounter ?? false;

        /// <summary>Name of the first encounter (tall grass/fog) tile used in this map, for the grass overlay.</summary>
        public string EncounterTileName()
        {
            foreach (var entry in Data.legend)
                if (GameData.Tile(entry.tile)?.encounter == true)
                    return entry.tile;
            return "tall_grass";
        }

        public static Sprite GrassOverlaySprite(string tileName)
        {
            int index = GameData.Tile(tileName)?.index ?? 0;
            var atlas = Resources.Load<Texture2D>("Sprites/Tiles/metatiles");
            int columns = GameData.TileColumns;
            return Animation.SpriteSheet.Cut(atlas, index % columns * 16, index / columns * 16 + 10, 16, 6, new Vector2(0.5f, 0f));
        }
    }
}
