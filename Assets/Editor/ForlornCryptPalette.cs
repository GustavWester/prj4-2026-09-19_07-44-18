using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Laver Forlorn Crypt-spritesene om til tiles og en Tile Palette ("Forlorn Crypt").
/// Kør via Tools > Create Forlorn Crypt Palette. Kan køres igen, paletten bygges bare forfra.
/// </summary>
public static class ForlornCryptPalette
{
    const string Root = "Assets/Artwork/Forlorn_Crypt_Tileset";
    const string TileFolder = Root + "/Tile Assets";
    const string PaletteFolder = "Assets/World/Dungeon/Tile Palettes";
    const string PaletteName = "Forlorn Crypt";
    const int Cell = 16; // pixels pr. tile = PPU, samme som resten af dungeonen

    [MenuItem("Tools/Create Forlorn Crypt Palette")]
    static void Create()
    {
        Directory.CreateDirectory(TileFolder);
        string palettePath = $"{PaletteFolder}/{PaletteName}.prefab";
        AssetDatabase.DeleteAsset(palettePath);
        var prefab = GridPaletteUtility.CreateNewPalette(PaletteFolder, PaletteName, GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Manual, Vector3.one, GridLayout.CellSwizzle.XYZ);
        var palette = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        var tilemap = palette.GetComponentInChildren<Tilemap>();

        // én række pr. mappe: Objects nederst, Tiles øverst. Arkene ligger side om side som i png'en
        int y = 0;
        foreach (var folder in new[] { "Objects", "Tiles" })
        {
            int x = 0, rowHeight = 0;
            foreach (var png in Directory.GetFiles($"{Root}/{folder}", "*.png").OrderBy(p => p))
            {
                Vector2Int size = Import(png);
                foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(png).OfType<Sprite>())
                    tilemap.SetTile(new Vector3Int(x + (int)sprite.rect.x / Cell, y + (int)sprite.rect.y / Cell, 0), GetTile(sprite));
                x += size.x / Cell + 1;
                rowHeight = Mathf.Max(rowHeight, size.y / Cell);
            }
            y += rowHeight + 1;
        }

        PrefabUtility.SaveAsPrefabAssetAndConnect(palette, palettePath, InteractionMode.AutomatedAction);
        Object.DestroyImmediate(palette);
        AssetDatabase.SaveAssets();
        Debug.Log($"Tile Palette oprettet: {palettePath}");
    }

    // Pixel art-indstillinger + 16x16-slicing. Ark med håndlavet slicing (fx forskellige størrelser) røres ikke.
    // Eksisterende 16x16-sprites beholder deres ID, så tiles der allerede er malet, stadig virker efter man har tegnet videre i Aseprite.
    static Vector2Int Import(string png)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(png);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = Cell;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var tex = new Texture2D(2, 2);
        tex.LoadImage(File.ReadAllBytes(png)); // læser filen direkte, så teksturen ikke skal være Read/Write
        var size = new Vector2Int(tex.width, tex.height);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(importer);
        dp.InitSpriteEditorDataProvider();
        var existing = dp.GetSpriteRects();
        bool gridSliced = existing.All(r => r.rect.width == Cell && r.rect.height == Cell && r.rect.x % Cell == 0 && r.rect.y % Cell == 0);
        if (existing.Length <= 1 || gridSliced) // ikke skåret op endnu, eller skåret i 16x16
        {
            string name = Path.GetFileNameWithoutExtension(png);
            var rects = new List<SpriteRect>();
            for (int cy = size.y - Cell; cy >= 0; cy -= Cell)
                for (int cx = 0; cx + Cell <= size.x; cx += Cell)
                {
                    var rect = new Rect(cx, cy, Cell, Cell);
                    var old = existing.FirstOrDefault(r => r.rect == rect);
                    // tomme felter springes over, men en sprite der fandtes før beholdes
                    if (old == null && !tex.GetPixels(cx, cy, Cell, Cell).Any(p => p.a > 0)) continue;
                    rects.Add(new SpriteRect
                    {
                        name = old?.name ?? $"{name}_{cx / Cell}_{cy / Cell}",
                        rect = rect,
                        alignment = SpriteAlignment.Center,
                        pivot = new Vector2(0.5f, 0.5f),
                        spriteID = old?.spriteID ?? GUID.Generate(),
                    });
                }
            dp.SetSpriteRects(rects.ToArray());
            dp.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            dp.Apply();
        }
        Object.DestroyImmediate(tex);
        importer.SaveAndReimport();
        return size;
    }

    static Tile GetTile(Sprite sprite)
    {
        string path = $"{TileFolder}/{sprite.name}.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        EditorUtility.SetDirty(tile);
        return tile;
    }
}
