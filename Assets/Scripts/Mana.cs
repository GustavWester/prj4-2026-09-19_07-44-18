using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Mana-pulje til spilleren. Virker som Health: abilities kalder TrySpend(cost),
/// og UI (fx CharacterSheetUI) lytter på onManaChanged.
/// </summary>
[DisallowMultipleComponent]
public class Mana : MonoBehaviour
{
    [Header("Mana")]
    public int maxMana = 50;

    [Tooltip("Mana pr. sekund der regenereres. 0 = ingen regen.")]
    public float regenPerSecond = 2f;

    [Header("Events")]
    [Tooltip("Fires with (current, max) whenever mana changes.")]
    public UnityEvent<int, int> onManaChanged;

    public int CurrentMana { get; private set; }
    public float ManaPercent => maxMana > 0 ? (float)CurrentMana / maxMana : 0f;

    private float regenBuffer; // samler brøkdele op, så regen virker selvom regenPerSecond * deltaTime < 1

    private void Awake()
    {
        CurrentMana = maxMana;
    }

    private void Update()
    {
        if (regenPerSecond <= 0f || CurrentMana >= maxMana) return;

        regenBuffer += regenPerSecond * Time.deltaTime;
        if (regenBuffer >= 1f)
        {
            int whole = Mathf.FloorToInt(regenBuffer);
            regenBuffer -= whole;
            Restore(whole);
        }
    }

    /// <summary>Trækker mana hvis der er nok. Returnerer false (og trækker intet) hvis ikke.</summary>
    public bool TrySpend(int amount)
    {
        if (amount < 0 || CurrentMana < amount) return false;

        CurrentMana -= amount;
        onManaChanged?.Invoke(CurrentMana, maxMana);
        return true;
    }

    public void Restore(int amount)
    {
        if (amount <= 0) return;

        CurrentMana = Mathf.Min(maxMana, CurrentMana + amount);
        onManaChanged?.Invoke(CurrentMana, maxMana);
    }
}
