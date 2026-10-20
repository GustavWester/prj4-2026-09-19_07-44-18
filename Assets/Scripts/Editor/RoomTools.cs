using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

/// <summary>
/// Værktøjer til hele rum: tiles på alle tilemaps, rum-objektet (lys + collider) og alt der står i rummet
/// (kister, fakler, fjender, døre ...). Rummets område er dets Light 2D-form, som også dækker væggene.
/// Vælg rummet (fx Room3) og brug Tools-menuen. Alt kan fortrydes med Cmd+Z.
/// </summary>
public static class RoomTools
{
    const int Gap = 3; // tomme felter mellem banen og en kopi

    [MenuItem("Tools/Duplicate Selected Room")]
    static void Duplicate()
    {
        var room = SelectedRoom();
        if (room == null) return;
        Rect area = RoomArea(room);
        var tilemaps = Object.FindObjectsByType<Tilemap>();

        // læg kopien under det laveste tile i hele banen
        float mapBottom = tilemaps.SelectMany(tm => UsedCells(tm).Select(c => tm.CellToWorld(c).y)).Min();
        var offset = new Vector3Int(0, Mathf.FloorToInt(mapBottom - Gap - area.yMax), 0);

        foreach (var tm in tilemaps)
        {
            Undo.RegisterCompleteObjectUndo(tm, "Duplicate Room");
            foreach (var cell in UsedCells(tm).Where(c => Overlaps(tm, c, area)).ToList())
                tm.SetTile(cell + offset, tm.GetTile(cell));
        }

        Object[] things = ThingsIn(area).Append(room.gameObject).ToArray();
        Selection.objects = things;
        Unsupported.DuplicateGameObjectsUsingPasteboard(); // som Cmd+D: beholder prefab-links og giver nye netværks-id'er
        if (Selection.gameObjects.Cast<Object>().Intersect(things).Any()) // skal være kopierne der er valgt, ellers flytter vi originalerne
        {
            Debug.LogError("Kunne ikke kopiere objekterne. Tiles er kopieret, brug Cmd+Z for at fortryde.");
            return;
        }
        foreach (var go in Selection.gameObjects)
        {
            Undo.RecordObject(go.transform, "Duplicate Room");
            go.transform.position += offset;
            if (go.GetComponent<RoomVisibility>() != null)
            {
                go.name = "Room" + Object.FindObjectsByType<RoomVisibility>().Length;
                Selection.activeGameObject = go; // så man kan flytte kopien med det samme
            }
        }

        EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
        SceneView.lastActiveSceneView?.Frame(new Bounds(area.center + (Vector2)(Vector3)offset, area.size), false);
        Debug.Log($"Kopierede {room.name} med {things.Length - 1} objekter, flyttet {offset.y} felter ned. Husk at gemme scenen.");
    }

    // Shift+Alt+I/J/K/L flytter rummet ét felt (som en piletast-klynge). Piletasterne selv bruges af
    // Scene view (kamera) og Hierarchy (fold ud/ind), så de når aldrig frem til menuen
    [MenuItem("Tools/Move Room/Left #&j")] static void Left() => Move(Vector3Int.left);
    [MenuItem("Tools/Move Room/Right #&l")] static void Right() => Move(Vector3Int.right);
    [MenuItem("Tools/Move Room/Up #&i")] static void Up() => Move(Vector3Int.up);
    [MenuItem("Tools/Move Room/Down #&k")] static void Down() => Move(Vector3Int.down);

    static void Move(Vector3Int offset)
    {
        var room = SelectedRoom();
        if (room == null) return;
        Rect area = RoomArea(room);

        foreach (var tm in Object.FindObjectsByType<Tilemap>())
        {
            Undo.RegisterCompleteObjectUndo(tm, "Move Room");
            // læs alt først og slet derefter, så felter der overlapper sig selv ikke går tabt
            var cells = UsedCells(tm).Where(c => Overlaps(tm, c, area)).ToList();
            var tiles = cells.Select(c => tm.GetTile(c)).ToList();
            foreach (var c in cells) tm.SetTile(c, null);
            for (int i = 0; i < cells.Count; i++) tm.SetTile(cells[i] + offset, tiles[i]);
        }

        foreach (var go in ThingsIn(area).Append(room.gameObject))
        {
            Undo.RecordObject(go.transform, "Move Room");
            go.transform.position += offset;
        }
        EditorSceneManager.MarkSceneDirty(room.gameObject.scene);
    }

    static RoomVisibility SelectedRoom()
    {
        var room = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<RoomVisibility>() : null;
        if (room == null) Debug.LogWarning("Vælg et rum-objekt (med RoomVisibility) i Hierarchy først.");
        return room;
    }

    // lysets form (dækker væggene) + gulv-collideren
    static Rect RoomArea(RoomVisibility room)
    {
        Bounds b = room.GetComponent<Collider2D>().bounds;
        foreach (var p in room.GetComponent<Light2D>().shapePath)
            b.Encapsulate(room.transform.TransformPoint(p));
        return new Rect(b.min, b.size);
    }

    // ting i rummet: objekter i roden med en renderer (ikke selve tilemap-griddet)
    static IEnumerable<GameObject> ThingsIn(Rect area) =>
        Object.FindObjectsByType<Transform>()
            .Where(t => t.parent == null && area.Contains(t.position)
                        && t.GetComponentInChildren<Renderer>() != null
                        && t.GetComponentInChildren<TilemapRenderer>() == null)
            .Select(t => t.gameObject);

    static IEnumerable<Vector3Int> UsedCells(Tilemap tm)
    {
        foreach (var cell in tm.cellBounds.allPositionsWithin)
            if (tm.HasTile(cell)) yield return cell;
    }

    // feltet (1x1) overlapper rummets område
    static bool Overlaps(Tilemap tm, Vector3Int cell, Rect area)
    {
        Vector3 min = tm.CellToWorld(cell);
        return min.x < area.xMax && min.x + 1 > area.xMin && min.y < area.yMax && min.y + 1 > area.yMin;
    }
}
