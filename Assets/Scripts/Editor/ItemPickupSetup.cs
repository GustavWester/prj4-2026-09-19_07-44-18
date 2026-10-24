using Unity.Netcode;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Laver ItemPickup.prefab (SpriteRenderer, trigger, NetworkObject, ItemPickup), lægger den i DefaultNetworkPrefabs
/// og sætter 3 Health Potions i den åbne scene, der hvor Scene-viewet kigger. Kan fortrydes med Cmd+Z.
/// </summary>
public static class ItemPickupSetup
{
    const string PrefabPath = "Assets/World/ItemPickup/ItemPickup.prefab";
    const string PotionPath = "Assets/Items/Resources/Items/HealthPotion.asset";

    [MenuItem("Tools/Items/Create Pickup Prefab + Place 3 Health Potions")]
    static void Setup()
    {
        var potion = AssetDatabase.LoadAssetAtPath<ItemSO>(PotionPath);
        if (potion == null)
        {
            Debug.LogError($"Fandt ikke {PotionPath}");
            return;
        }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            var go = new GameObject("ItemPickup");
            var sprite = go.AddComponent<SpriteRenderer>();
            sprite.sprite = potion.icon;
            sprite.sortingOrder = 3; // samme som kisten
            var trigger = go.AddComponent<CircleCollider2D>();
            trigger.isTrigger = true;
            trigger.radius = 0.6f;
            go.AddComponent<NetworkObject>();
            go.AddComponent<ItemPickup>();
            prefab = PrefabUtility.SaveAsPrefabAsset(go, PrefabPath);
            Object.DestroyImmediate(go);
        }

        var list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>("Assets/DefaultNetworkPrefabs.asset");
        if (!list.Contains(prefab))
        {
            list.Add(new NetworkPrefab { Prefab = prefab });
            EditorUtility.SetDirty(list);
            AssetDatabase.SaveAssets();
        }

        // 3 potions på en række, der hvor Scene-viewet kigger
        var view = SceneView.lastActiveSceneView;
        Vector3 center = view != null ? view.pivot : Vector3.zero;
        center.z = 0;
        for (int i = -1; i <= 1; i++)
        {
            var pickup = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            pickup.transform.position = center + new Vector3(i, 0, 0);
            var so = new SerializedObject(pickup.GetComponent<ItemPickup>());
            so.FindProperty("item").objectReferenceValue = potion;
            so.ApplyModifiedPropertiesWithoutUndo();
            Undo.RegisterCreatedObjectUndo(pickup, "Place Health Potion");
        }
        Debug.Log($"{PrefabPath} er klar, og 3 Health Potions er sat ind. Gem scenen.");
    }
}
