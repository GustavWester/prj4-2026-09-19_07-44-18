using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Holdets fælles tilstand: game state, map level og item pool.
/// Hosten skriver, alle andre læser. Nye spillere får værdierne automatisk.
/// Placeres i scenen på et GameObject med NetworkObject.
/// </summary>
[DisallowMultipleComponent]
public class GameManager : NetworkBehaviour
{
    public enum GameState { Playing, GameOver }

    public static GameManager Instance { get; private set; }

    [Header("Item pool")]
    [Tooltip("Alle items der kan uddeles. Rækkefølgen må ikke ændres under spillet: index er det der sendes over netværket.")]
    [SerializeField] private List<ItemSO> itemPool = new();

    // Hosten skriver, alle læser. Lyt på OnValueChanged for at vise game over-skærm.
    public readonly NetworkVariable<GameState> State = new(GameState.Playing);
    public readonly NetworkVariable<int> Level = new(1);

    // Index i itemPool for de items der allerede er uddelt. Kun hosten bruger den.
    private readonly HashSet<int> usedItems = new();

    private bool HasAuthority => !IsSpawned || IsServer;

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Update()
    {
        if (!HasAuthority || State.Value != GameState.Playing) return;
        if (AllPlayersDead()) State.Value = GameState.GameOver;
    }

    private bool AllPlayersDead()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players.Length == 0) return false;

        foreach (GameObject p in players)
            if (!p.GetComponent<ResourceController>().IsDead) return false;
        return true;
    }

    // ---------------------------------------------------------------
    // Level og ny runde
    // ---------------------------------------------------------------
    public void NextLevel()
    {
        if (HasAuthority) Level.Value++;
    }

    public void NewRun()
    {
        if (!HasAuthority) return;
        usedItems.Clear();
        Level.Value = 1;
        State.Value = GameState.Playing;
    }

    // ---------------------------------------------------------------
    // Item pool
    // ---------------------------------------------------------------
    /// <summary>
    /// Trækker et tilfældigt item der ikke er uddelt endnu og markerer det som brugt.
    /// Kun hosten. Returnerer false hvis poolen er tom. index er det der sendes til klienterne.
    /// </summary>
    public bool TryDrawItem(out int index)
    {
        index = -1;
        if (!HasAuthority) return false;

        var free = new List<int>();
        for (int i = 0; i < itemPool.Count; i++)
            if (!usedItems.Contains(i)) free.Add(i);

        if (free.Count == 0) return false;

        index = free[Random.Range(0, free.Count)];
        usedItems.Add(index);
        return true;
    }

    /// <summary>Slår et index op i poolen. Virker på alle maskiner, da alle har samme liste.</summary>
    public ItemSO GetItem(int index) => itemPool[index];
}