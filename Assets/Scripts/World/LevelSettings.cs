using UnityEngine;

/// <summary>
/// Ét pr. scene (et tomt objekt med dette script). Ganger alle fjenders liv, også bossernes,
/// så de samme fjende-prefabs bliver sværere i de senere baner. Ingen LevelSettings i scenen = normalt liv.
/// </summary>
public class LevelSettings : MonoBehaviour
{
    [Tooltip("Fx bane 1 = 1, bane 2 = 1.5, bane 3 = 2.")]
    [SerializeField, Min(0.1f)] private float enemyHealthMultiplier = 1f;

    // slås op hver gang i stedet for en static sat i Awake, så det ikke afhænger af hvem der vågner først
    public static float EnemyHealthMultiplier =>
        FindAnyObjectByType<LevelSettings>() is { } s ? s.enemyHealthMultiplier : 1f;
}
