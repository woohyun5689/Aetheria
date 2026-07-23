using System;
using System.Collections.Generic;
using UnityEngine;

public sealed partial class AetheriaGame
{
    private const string DungeonRegionAtlasPath = "UI/VisualRefresh/Regions/region_atlas";

    private readonly Dictionary<string, Sprite> dungeonRegionSpriteCache =
        new Dictionary<string, Sprite>(StringComparer.Ordinal);
    private Texture2D dungeonRegionAtlasTexture;

    private Sprite DungeonRegionVisualSprite(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return null;
        }

        if (dungeonRegionSpriteCache.TryGetValue(key, out var cached) && cached != null)
        {
            return cached;
        }

        // Pink and violet artwork loses important detail when it shares the
        // atlas' magenta key colour. Keep those two regions as clean,
        // independently keyed sprites and use the atlas for the other seven.
        if (key == "elesia" || key == "shadow")
        {
            var standalone = LoadGeneratedSprite(
                "UI/VisualRefresh/Regions/" + key,
                Vector4.zero);
            if (standalone != null)
            {
                return standalone;
            }
        }

        var index = DungeonRegionVisualIndex(key);
        if (index < 0)
        {
            return null;
        }

        if (dungeonRegionAtlasTexture == null)
        {
            dungeonRegionAtlasTexture = Resources.Load<Texture2D>(DungeonRegionAtlasPath);
        }
        if (dungeonRegionAtlasTexture == null)
        {
            return null;
        }

        var cellWidth = dungeonRegionAtlasTexture.width / 3f;
        var cellHeight = dungeonRegionAtlasTexture.height / 3f;
        var column = index % 3;
        var topRow = index / 3;
        var rect = new Rect(
            column * cellWidth,
            (2 - topRow) * cellHeight,
            cellWidth,
            cellHeight);
        var sprite = Sprite.Create(
            dungeonRegionAtlasTexture,
            rect,
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
        sprite.name = "Dungeon Region " + key;
        sprite.hideFlags = HideFlags.DontSave;
        dungeonRegionSpriteCache[key] = sprite;
        return sprite;
    }

    private static int DungeonRegionVisualIndex(string key)
    {
        switch (key)
        {
            case "frost": return 0;
            case "green": return 1;
            case "lava": return 2;
            case "elesia": return 3;
            case "arcadia": return 4;
            case "desert": return 5;
            case "shadow": return 6;
            case "isles": return 7;
            case "wind": return 8;
            default: return -1;
        }
    }

    private void ReleaseDungeonRegionVisuals()
    {
        foreach (var entry in dungeonRegionSpriteCache)
        {
            if (entry.Value != null)
            {
                Destroy(entry.Value);
            }
        }
        dungeonRegionSpriteCache.Clear();
        dungeonRegionAtlasTexture = null;
    }
}
