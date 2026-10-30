using UnityEngine;

/// <summary>
/// Kobler en ResourceController til to SegmentedIconBars (hjerter og mana).
/// 1 helt hjerte = 2 HP, 1 hel mana-kugle = 10 mana (sættes på barerne).
///
/// Brug:
///  - Singleplayer/test: træk spillerens ResourceController ind i "target".
///  - Netcode: lad ejeren kalde hud.Bind(resourceController) når spilleren spawner,
///    fx i PlayerManager.OnNetworkSpawn() hvis IsOwner.
/// </summary>
public class ResourceHUD : MonoBehaviour
{
    [SerializeField] private HUDBar heartBar;
    [SerializeField] private HUDBar manaBar;
    [Tooltip("Valgfri. Kan også sættes fra kode med Bind().")]
    [SerializeField] private ResourceController target;
    
    [Header("Fælles layout")]
    [SerializeField] private Vector2 iconSize = new Vector2(32, 32);
    [SerializeField] private float iconSpacing = 4f;

    private ResourceController bound;

    
    
    private void Awake()
    {
        heartBar.Configure(iconSize, iconSpacing);
        manaBar.Configure(iconSize, iconSpacing);
    }
    private void OnEnable()
    {
        LocalPlayer.Changed += OnLocalPlayerChanged;
        // Hvis spilleren allerede er spawnet, før HUD'en blev aktiv
        if (LocalPlayer.Current != null) OnLocalPlayerChanged(LocalPlayer.Current);
    }

    private void OnDisable()
    {
        LocalPlayer.Changed -= OnLocalPlayerChanged;
        Unbind();
    }

    private void OnLocalPlayerChanged(GameObject player)
    {
        if (player == null)
        {
            Unbind();
            return;
        }
        Bind(player.GetComponent<ResourceController>());
    }

    public void Bind(ResourceController controller)
    {
        Unbind();
        if (controller == null) return;

        bound = controller;
        bound.onHealthPercentChanged.AddListener(OnHealthChanged);
        bound.onManaChanged.AddListener(OnManaChanged);

        // Vis nuværende værdier med det samme
        RefreshHealth();
        RefreshMana();
    }

    private void Unbind()
    {
        if (bound == null) return;
        bound.onHealthPercentChanged.RemoveListener(OnHealthChanged);
        bound.onManaChanged.RemoveListener(OnManaChanged);
        bound = null;
    }

    // onHealthPercentChanged fyrer også når max-liv ændres, så den dækker begge dele
    private void OnHealthChanged(float _) => RefreshHealth();
    private void OnManaChanged(int current, int max) => manaBar.SetValue(current, max);

    private void RefreshHealth()
    {
        if (bound != null && heartBar != null)
            heartBar.SetValue(bound.CurrentHealth, bound.MaxHealth);
    }

    private void RefreshMana()
    {
        if (bound != null && manaBar != null)
            manaBar.SetValue(bound.CurrentMana, bound.MaxMana);
    }
    
    
}