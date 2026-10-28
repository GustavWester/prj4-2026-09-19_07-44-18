using UnityEngine;

/// <summary>
/// Data-driven definition of a single bullet pattern.
/// Create instances via Assets > Create > BulletHell > BulletPattern
/// so designers can tweak patterns without touching code.
/// </summary>
[CreateAssetMenu(fileName = "NewBulletPattern", menuName = "BulletHell/BulletPattern")]
public class BulletPatternSO : ScriptableObject
{
    public enum PatternType { RadialBurst, Spiral, AimedSpread, Curtain }

    [Header("Pattern")]
    public PatternType type = PatternType.RadialBurst;
    public GameObject bulletPrefab;

    [Header("Shape")]
    public int bulletCount = 16;
    [Tooltip("Total angle the pattern covers. 360 = full circle.")]
    public float spreadAngle = 360f;
    public float bulletSpeed = 5f;

    [Header("Timing")]
    [Tooltip("How many times the pattern 'fires' before it's considered done.")]
    public int volleys = 1;
    public float delayBetweenVolleys = 0.15f;

    [Header("Spiral-only")]
    public float spiralRotationSpeedDeg = 20f;

    [Header("Behaviour")]
    public bool aimedAtPlayer = false;
}
