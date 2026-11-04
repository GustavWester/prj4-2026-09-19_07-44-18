using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// Bygger level 3 (DungeonScene3) ud fra kortet: Start nederst, snoede gange, Trap Room og Boss Room med fire søjler.
/// Tools > Level 3 > Build DungeonScene3:
///   1. kopierer DungeonScene2 (kamera, lys, HUD, Grid med tilemaps) og fjerner fjender, rum og tiles,
///   2. maler gulv og vægge efter rektanglerne i Areas,
///   3. laver et rum (lys + collider + RoomVisibility) pr. rektangel,
///   4. sætter PlayerSpawn, Knight-bossen og spike-fælderne,
///   5. lægger en stige i DungeonScene2 ved GhostBoss, der fører hertil.
/// Fjender, kister, fakler og pynt placeres i hånden bagefter. Kører man værktøjet igen, bygges scenen forfra.
/// </summary>
public static class Level3Builder
{
    const string Source = "Assets/Scenes/DungeonScene2.unity";
    const string Target = "Assets/Scenes/DungeonScene3.unity";
    const string TileFolder = "Assets/Artwork/Forlorn_Crypt_Tileset/Tile Assets/";
    const string BossPrefab = "Assets/Enemies/KnightBoss/KnightBoss.prefab";
    const string SpikePrefab = "Assets/World/SpikeTrap/SpikeTrap.prefab";
    const string LadderTile = "static_objects_dungeon_2_4"; // samme stige som i DungeonScene
    const string BossRoomIcon = "Assets/UI/MinimapBoss.png";

    // Rum og gange som rektangler i tiles (x, y = nederste venstre hjørne). Overlap = forbundet.
    // Hold mindst 2 felter vandret og 3 lodret mellem områder der ikke skal hænge sammen, ellers er der ikke plads til væggene.
    static readonly (string name, RectInt rect)[] Areas =
    {
        ("Start", new RectInt(34, 10, 12, 12)),
        ("StartEast", new RectInt(45, 14, 10, 4)),
        ("SouthEast", new RectInt(52, 1, 10, 8)),
        ("EastHall", new RectInt(54, 8, 4, 31)),
        ("Funnel", new RectInt(44, 38, 24, 7)),
        ("FunnelNeck", new RectInt(51, 44, 4, 13)),
        ("Junction", new RectInt(46, 56, 14, 6)),
        ("JunctionEast", new RectInt(59, 57, 6, 4)),
        ("EastRoom", new RectInt(64, 54, 8, 14)),
        ("JunctionNorth", new RectInt(52, 61, 4, 13)),
        ("MidRoom", new RectInt(49, 73, 12, 7)),
        ("MidWest", new RectInt(35, 74, 15, 4)),
        ("CenterNorth", new RectInt(35, 77, 4, 10)),
        ("CenterSouth", new RectInt(35, 64, 4, 11)),
        ("CenterRoom", new RectInt(31, 55, 9, 10)),
        ("CenterWest", new RectInt(25, 58, 7, 4)),
        ("WestRoom", new RectInt(15, 50, 11, 11)),
        ("WestSouth", new RectInt(19, 41, 4, 10)),
        ("WestSouthEast", new RectInt(19, 40, 10, 4)),
        ("SouthWestRoom", new RectInt(28, 37, 9, 8)),
        ("FarWestHall", new RectInt(11, 68, 25, 4)),
        ("FarWestRoom", new RectInt(4, 65, 8, 10)),
        ("WestHall", new RectInt(16, 71, 4, 32)),
        ("WestNook", new RectInt(20, 85, 4, 4)),
        ("NookRoom", new RectInt(23, 83, 8, 8)),
        ("NorthWestHall", new RectInt(16, 99, 10, 4)),
        ("AnteRoom", new RectInt(24, 95, 10, 9)),
        ("CenterHall", new RectInt(38, 84, 12, 4)),
        ("AnteEastDown", new RectInt(34, 87, 4, 12)),
        ("TrapHall", new RectInt(46, 84, 4, 11)),
        ("TrapRoom", new RectInt(44, 94, 16, 12)),
        ("TrapNook", new RectInt(59, 98, 8, 4)),
        ("TrapNookRoom", new RectInt(66, 95, 6, 8)),
        ("EastHall2", new RectInt(49, 84, 10, 4)),
        ("NorthEastRoom", new RectInt(58, 79, 9, 8)),
        ("BossHall", new RectInt(26, 103, 4, 10)),
        ("Boss", new RectInt(14, 112, 22, 20)),
    };

    static readonly RectInt[] Pillars =
    {
        new(18, 116, 3, 4), new(29, 116, 3, 4), new(18, 124, 3, 4), new(29, 124, 3, 4),
    };

    static readonly Vector2Int SpawnCell = new(40, 15);
    static readonly Vector2Int BossCell = new(25, 121);

    // det der beholdes fra DungeonScene2 (resten - fjender, kister, fakler, gamle rum - fjernes)
    static readonly string[] Keep = { "World", "Main Camera", "Global Light 2D", "EventSystem", "Canvas", "PlayerSpawn" };

    static readonly string[] FloorVariants =
    {
        "ground_dungeon_70", "ground_dungeon_71", "ground_dungeon_73", "ground_dungeon_84",
        "ground_dungeon_86", "ground_dungeon_97", "ground_dungeon_99", "ground_dungeon_58",
    };

    [MenuItem("Tools/Level 3/Build DungeonScene3")]
    static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (File.Exists(Target) && !EditorUtility.DisplayDialog("Level 3", "DungeonScene3 findes allerede. Byg den forfra? Alt i scenen bliver erstattet.", "Byg forfra", "Annuller"))
            return;

        // kopi af DungeonScene2 via Unity, så alle objekter får nye id'er i den nye scene
        var source = EditorSceneManager.OpenScene(Source);
        EditorSceneManager.SaveScene(source, Target, true);
        var scene = EditorSceneManager.OpenScene(Target);

        var template = Clean(scene);
        if (template == null) return;

        var tilemaps = Object.FindObjectsByType<Tilemap>().ToDictionary(t => t.name);
        Tilemap floor = tilemaps["Floor"], wall = tilemaps["Wall"];
        var walk = Walkable();
        Paint(floor, wall, walk);
        BuildRooms(template, floor);

        Place("PlayerSpawn", floor, SpawnCell);
        PlacePrefab(BossPrefab, floor, BossCell);
        PlaceSpikes(floor);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AddToBuildSettings();
        AddLadderToLevel2();

        EditorSceneManager.OpenScene(Target);
        Decorate();
        Debug.Log($"DungeonScene3 bygget: {Areas.Length} rum, {walk.Count} gulv-felter. Placér fjender og kister, og gem scenen.");
    }

    // ---------------------------------------------------------------
    // Pynt: fakler, bannere, tønder, krukker, rustninger, skeletter, fyrfade
    // ---------------------------------------------------------------
    const string TorchPrefab = "Assets/World/Torch/Torch.prefab";
    const string BrazierPrefab = "Assets/World/Brazier/Brazier.prefab";
    const string DecorRoot = "Decor";

    // kun rigtige rum får pynt, ikke gangene
    static readonly string[] DecoratedRooms =
    {
        "Start", "SouthEast", "Funnel", "Junction", "EastRoom", "MidRoom", "CenterRoom", "WestRoom", "SouthWestRoom",
        "FarWestRoom", "NookRoom", "AnteRoom", "TrapRoom", "TrapNookRoom", "NorthEastRoom", "Boss",
    };
    static readonly string[][] Banners = { new[] { "0_3", "0_2" }, new[] { "3_3", "3_2" }, new[] { "1_3", "1_2" }, new[] { "4_3", "4_2" } };
    static readonly string[] Clutter = { "0_6", "1_6", "2_6", "3_6", "8", "1_0", "2_0", "14", "4_0", "5_0" }; // tønder og krukker

    // fast "tilfældighed", så pynten bliver den samme hver gang
    static int H(int x, int y) => unchecked((x * 73856093) ^ (y * 19349663)) & 0x7fffffff;

    /// <summary>
    /// Pynter DungeonScene3. Kan køres igen: rydder Decorations-tilemappet og Decor-objektet først,
    /// så alt man selv har lagt der, forsvinder.
    /// </summary>
    [MenuItem("Tools/Level 3/Decorate DungeonScene3")]
    static void Decorate()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.path != Target)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            scene = EditorSceneManager.OpenScene(Target);
        }

        var tilemaps = Object.FindObjectsByType<Tilemap>().ToDictionary(t => t.name);
        Tilemap floor = tilemaps["Floor"], deco = tilemaps["Decorations"];
        deco.ClearAllTiles();
        var old = GameObject.Find(DecorRoot);
        if (old != null) Object.DestroyImmediate(old);
        var root = new GameObject(DecorRoot).transform;

        var walk = Walkable();
        var tiles = new Dictionary<Vector2Int, string>();
        var torches = new List<Vector2Int>();
        var braziers = new List<Vector2Int>();
        bool Put(Vector2Int c, string id) => tiles.TryAdd(c, "static_objects_dungeon_" + id);

        for (int i = 0; i < DecoratedRooms.Length; i++)
        {
            string name = DecoratedRooms[i];
            var r = Areas.First(a => a.name == name).rect;
            int x0 = r.xMin, y0 = r.yMin, w = r.width, h = r.height;

            // hold 2 felter fri omkring åbningerne, så ingen gang bliver spærret
            var openings = walk.Where(c => !r.Contains(c) && Neighbours8(c).Any(r.Contains));
            var near = new HashSet<Vector2Int>(openings.SelectMany(c =>
                from dx in Enumerable.Range(-2, 5) from dy in Enumerable.Range(-2, 5) select c + new Vector2Int(dx, dy)));
            bool Free(Vector2Int c) => walk.Contains(c) && !near.Contains(c) && !Areas.Any(a => a.name != name && a.rect.Contains(c));

            // 1) fakler og bannere på murfronten over rummet
            var style = name == "Boss" ? new[] { "2_1", "5_1" } : name == "TrapRoom" ? new[] { "2_3", "5_3" } : Banners[i % Banners.Length];
            int k = 0;
            for (int x = x0; x < x0 + w; x++)
            {
                var c = new Vector2Int(x, y0 + h);
                if (walk.Contains(c) || !walk.Contains(c + Vector2Int.down)) continue;
                if (k % 6 == 1) torches.Add(c);
                else if (k % 6 == 4) Put(c, style[(k / 6) % 2]);
                k++;
            }

            // 2) ting langs væggene
            var wallside = (from x in Enumerable.Range(x0, w) from y in Enumerable.Range(y0, h)
                            let c = new Vector2Int(x, y)
                            where Free(c) && Neighbours4(c).Any(n => !walk.Contains(n))
                            select c).ToList();

            if (name == "Boss")
            {
                // rustninger (2 felter høje) langs siderne og fyrfade i hjørnerne
                foreach (int y in new[] { y0 + 6, y0 + 10, y0 + 14 })
                    foreach (int x in new[] { x0, x0 + w - 1 })
                    {
                        var c = new Vector2Int(x, y);
                        if (Free(c) && Free(c + Vector2Int.up) && Put(c, "4_5")) Put(c + Vector2Int.up, "4_6");
                    }
                braziers.AddRange(new Vector2Int[] { new(x0 + 2, y0 + 2), new(x0 + w - 3, y0 + 2), new(x0 + 2, y0 + h - 4), new(x0 + w - 3, y0 + h - 4) });
                continue;
            }
            if (name == "TrapRoom")
            {
                foreach (var c in wallside)
                    if (H(c.x, c.y) % 100 < 18) Put(c, "2_5"); // skeletter fra dem der ikke klarede den
                continue;
            }

            foreach (var (cx, cy) in new[] { (x0, y0), (x0 + w - 1, y0), (x0, y0 + h - 1), (x0 + w - 1, y0 + h - 1) })
            {
                int sx = cx == x0 ? 1 : -1, sy = cy == y0 ? 1 : -1;
                foreach (var c in new Vector2Int[] { new(cx, cy), new(cx + sx, cy), new(cx, cy + sy) })
                    if (wallside.Contains(c) && H(c.x, c.y) % 100 < 75) Put(c, Clutter[H(c.x, c.y) % Clutter.Length]);
            }
            foreach (var c in wallside)
                if (H(c.y, c.x) % 100 < 7) Put(c, Clutter[H(c.x, c.y) % Clutter.Length]);

            if (w >= 10 && h >= 9)
            {
                int mx = x0 + w / 2, my = y0 + h / 2;
                braziers.Add(new Vector2Int(mx - 3, my));
                braziers.Add(new Vector2Int(mx + 2, my));
            }
        }

        var cache = new Dictionary<string, TileBase>();
        foreach (var (c, id) in tiles)
        {
            if (!cache.TryGetValue(id, out var tile)) cache[id] = tile = LoadTile(id);
            deco.SetTile((Vector3Int)c, tile);
        }
        foreach (var c in torches)
            PlacePrefab(TorchPrefab, floor, c, root).transform.position = floor.CellToWorld((Vector3Int)c) + new Vector3(0.49f, 0.59f); // som i DungeonScene2
        foreach (var c in braziers)
            PlacePrefab(BrazierPrefab, floor, c, root);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"DungeonScene3 pyntet: {tiles.Count} ting, {torches.Count} fakler, {braziers.Count} fyrfade.");
    }

    static IEnumerable<Vector2Int> Neighbours4(Vector2Int c) =>
        new[] { c + Vector2Int.right, c + Vector2Int.left, c + Vector2Int.up, c + Vector2Int.down };

    static IEnumerable<Vector2Int> Neighbours8(Vector2Int c) =>
        from dx in Enumerable.Range(-1, 3) from dy in Enumerable.Range(-1, 3) select c + new Vector2Int(dx, dy);

    // ---------------------------------------------------------------
    // Ryd kopien
    // ---------------------------------------------------------------
    static RoomVisibility Clean(UnityEngine.SceneManagement.Scene scene)
    {
        var rooms = Object.FindObjectsByType<RoomVisibility>();
        if (rooms.Length == 0)
        {
            Debug.LogError("DungeonScene2 har ingen rum (RoomVisibility) at bruge som skabelon.");
            return null;
        }
        var template = rooms[0];
        foreach (var root in scene.GetRootGameObjects())
            if (root != template.gameObject && !Keep.Contains(root.name))
                Object.DestroyImmediate(root);
        foreach (var tm in Object.FindObjectsByType<Tilemap>())
            tm.ClearAllTiles();
        return template;
    }

    // ---------------------------------------------------------------
    // Tiles
    // ---------------------------------------------------------------
    static HashSet<Vector2Int> Walkable()
    {
        var walk = new HashSet<Vector2Int>();
        foreach (var (_, r) in Areas)
            foreach (var c in r.allPositionsWithin) walk.Add(c);
        foreach (var p in Pillars)
            foreach (var c in p.allPositionsWithin) walk.Remove(c);
        return walk;
    }

    static void Paint(Tilemap floor, Tilemap wall, HashSet<Vector2Int> walk)
    {
        var rnd = new System.Random(3); // samme mønster hver gang
        var basic = LoadTile("ground_dungeon_61");
        var variants = FloorVariants.Select(LoadTile).ToArray();
        foreach (var c in walk)
            floor.SetTile((Vector3Int)c, rnd.NextDouble() < 0.12 ? variants[rnd.Next(variants.Length)] : basic);

        var walls = new Dictionary<int, TileBase>();
        int minX = walk.Min(c => c.x) - 3, maxX = walk.Max(c => c.x) + 3;
        int minY = walk.Min(c => c.y) - 3, maxY = walk.Max(c => c.y) + 3;
        for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            {
                int t = WallTile(walk, x, y);
                if (t < 0) continue;
                if (!walls.TryGetValue(t, out var tile)) walls[t] = tile = LoadTile("walls_dungeon_" + t);
                wall.SetTile(new Vector3Int(x, y, 0), tile);
            }
    }

    /// <summary>
    /// Hvilken væg-tile (walls_dungeon_N) et felt skal have, ud fra hvor gulvet er omkring det. -1 = ingen væg.
    /// Reglerne er aflæst fra DungeonScene2 og giver præcis de samme vægge som dem, der er malet i hånden der.
    /// Væggen er to felter høj over gulvet (front + topkant) og ét felt på siderne og i bunden.
    /// </summary>
    static int WallTile(HashSet<Vector2Int> walk, int x, int y)
    {
        bool W(int dx, int dy) => walk.Contains(new Vector2Int(x + dx, y + dy));
        if (W(0, 0)) return -1;
        if (W(0, -1)) return 16;                                   // murfront lige over gulvet
        if (W(0, 1)) return W(-1, 0) ? 0 : W(1, 0) ? 2 : 1;        // bundkant med gulv over
        if (W(-1, 0)) return W(0, -2) ? 10 : 5;                    // højre side
        if (W(1, 0)) return W(0, -2) ? 12 : 7;                     // venstre side
        if (W(0, -2)) return W(-1, -1) ? 10 : W(1, -1) ? 12 : 11;  // topkant over murfronten
        if (W(-1, -1)) return 5;
        if (W(1, -1)) return 7;
        if (W(-1, 1)) return 9;                                    // ydre hjørner
        if (W(1, 1)) return 8;
        if (W(-1, -2)) return 4;
        if (W(1, -2)) return 3;
        return -1;
    }

    static TileBase LoadTile(string name)
    {
        var tile = AssetDatabase.LoadAssetAtPath<TileBase>(TileFolder + name + ".asset");
        if (tile == null) Debug.LogError("Mangler tile: " + name);
        return tile;
    }

    // ---------------------------------------------------------------
    // Rum
    // ---------------------------------------------------------------
    static void BuildRooms(RoomVisibility template, Tilemap floor)
    {
        var bossIcon = AssetDatabase.LoadAssetAtPath<Sprite>(BossRoomIcon); // kraniet på minimappet

        for (int i = 0; i < Areas.Length; i++)
        {
            var (name, r) = Areas[i];
            var room = Object.Instantiate(template.gameObject);
            room.name = "Room" + (i + 1);
            Vector3 min = floor.CellToWorld(new Vector3Int(r.xMin, r.yMin, 0));
            Vector3 max = floor.CellToWorld(new Vector3Int(r.xMax, r.yMax, 0));
            room.transform.SetPositionAndRotation((min + max) / 2f, Quaternion.identity);
            room.transform.localScale = Vector3.one;

            var box = room.GetComponent<BoxCollider2D>();
            box.offset = Vector2.zero;
            box.size = max - min; // gulvet

            // lyset dækker også væggene: 1 felt til siderne og nedad, 2 opad (murfront + topkant)
            Vector2 h = (max - min) / 2f;
            room.GetComponent<Light2D>().SetShapePath(new Vector3[]
            {
                new(-h.x - 1, -h.y - 1), new(-h.x - 1, h.y + 2), new(h.x + 1, h.y + 2), new(h.x + 1, -h.y - 1),
            });

            var so = new SerializedObject(room.GetComponent<RoomVisibility>());
            so.FindProperty("minimapIcon").objectReferenceValue = name == "Boss" ? bossIcon : null;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Object.DestroyImmediate(template.gameObject);
    }

    // ---------------------------------------------------------------
    // Ting i banen
    // ---------------------------------------------------------------
    static Vector3 CellCenter(Tilemap floor, Vector2Int c) => floor.GetCellCenterWorld((Vector3Int)c);

    static void Place(string rootName, Tilemap floor, Vector2Int cell)
    {
        var go = GameObject.Find(rootName);
        if (go != null) go.transform.position = CellCenter(floor, cell);
        else Debug.LogError($"Fandt ikke {rootName} i scenen.");
    }

    static GameObject PlacePrefab(string path, Tilemap floor, Vector2Int cell, Transform parent = null)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null) { Debug.LogError("Mangler prefab: " + path); return null; }
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); // beholder prefab-linket
        go.transform.position = CellCenter(floor, cell);
        return go;
    }

    // tre rækker spidser på tværs af Trap Room, forskudt så de bølger fra venstre mod højre
    static void PlaceSpikes(Tilemap floor)
    {
        var room = Areas.First(a => a.name == "TrapRoom").rect;
        int col = 0;
        for (int x = room.xMin + 2; x < room.xMax - 1; x += 2, col++)
            foreach (int y in new[] { room.yMin + 2, room.yMin + 5, room.yMin + 8 })
            {
                var spike = PlacePrefab(SpikePrefab, floor, new Vector2Int(x, y));
                if (spike == null) return;
                var so = new SerializedObject(spike.GetComponent<SpikeTrap>());
                so.FindProperty("offset").floatValue = col * 0.3f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
    }

    static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        if (scenes.Any(s => s.path == Target)) return;
        scenes.Add(new EditorBuildSettingsScene(Target, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // stigen i DungeonScene2 åbner, når GhostBoss er død, og fører til DungeonScene3
    static void AddLadderToLevel2()
    {
        var scene = EditorSceneManager.OpenScene(Source);
        if (Object.FindObjectsByType<LevelExit>().Length > 0) return; // er der allerede

        var boss = Object.FindObjectsByType<EnemyController>()
            .FirstOrDefault(e => e.stats != null && !string.IsNullOrEmpty(e.stats.bossTitle));
        if (boss == null) { Debug.LogError("Fandt ingen boss i DungeonScene2 til stigen."); return; }

        var decorations = Object.FindObjectsByType<Tilemap>().First(t => t.name == "Decorations");
        var cell = decorations.WorldToCell(boss.transform.position);
        decorations.SetTile(cell, LoadTile(LadderTile));

        var ladder = new GameObject("Ladder");
        ladder.transform.position = decorations.GetCellCenterWorld(cell);
        ladder.AddComponent<NetworkObject>();
        var area = ladder.AddComponent<BoxCollider2D>();
        area.isTrigger = true;
        area.size = new Vector2(2f, 3f); // området hvor man kan trykke E
        var exit = new SerializedObject(ladder.AddComponent<LevelExit>());
        exit.FindProperty("nextScene").stringValue = "DungeonScene3";
        exit.FindProperty("boss").objectReferenceValue = boss.GetComponent<ResourceController>();
        exit.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
