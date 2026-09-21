using UnityEngine;

[CreateAssetMenu(fileName = "EnemyStats", menuName = "Scriptable Objects/EnemyStats")]
public class EnemyStats : ScriptableObject
{
    [Header("Identity")]
    public string enemyName;
    public EnemyClass enemyClass;

    [Header ("Core Stats")]
    public int Health = 100;
    public int attackDamage = 5;
    public float moveSpeed = 2f;

    [Header("Combat")]
    public float attackRange = 1.5f; //afstand enemy skal være indenfor for at attack
    public float attackCooldown = 1f; //sekunder mellem hvert attack
    public float detectionRange = 6f; //afstand hvor enemy opdager/aggro på spilleren
}
