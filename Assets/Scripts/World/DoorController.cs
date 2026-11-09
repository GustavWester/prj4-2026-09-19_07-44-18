using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dør (eller mimic-kiste) der åbner når spilleren står ved den og trykker E.
/// Kræver to Collider2D på døren: en solid der blokerer spilleren,
/// og en lidt større med Is Trigger slået til (området hvor man kan trykke E).
/// Animationen ligger i Door.controller (Closed -> Open via trigger "Open").
///
/// Online: kræver en NetworkObject. Hosten bestemmer om døren er åben (netOpen),
/// klienter beder hosten om at åbne den, og alle - også dem der joiner senere - ser den åben.
/// Offline (ingen NetworkObject / ikke spawnet) åbner den bare lokalt.
/// </summary>
[RequireComponent(typeof(Animator))]
public class DoorController : NetworkBehaviour
{
    [SerializeField] private Collider2D blockingCollider; // den solide collider, slås fra når døren er åben
    [SerializeField] private bool openWithE = true; // slå fra på døre der kun skal åbnes via Open() (fx når en fjende dør)
    [SerializeField] private ResourceController enemy; // valgfri: døren åbner når denne fjende dør

    private readonly NetworkVariable<bool> netOpen = new(); // hosten skriver
    private Animator animator;
    private bool playerNear;
    private bool isOpen;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (enemy != null) enemy.onDeath.AddListener(Open); //hvis goblin dør, så åbner døren
    }

    public override void OnNetworkSpawn()
    {
        netOpen.OnValueChanged += OnOpenChanged;
        if (netOpen.Value) PlayOpen(); // joinede efter døren blev åbnet
    }

    public override void OnNetworkDespawn() => netOpen.OnValueChanged -= OnOpenChanged;

    private void OnOpenChanged(bool _, bool open) { if (open) PlayOpen(); }

    private void Update()
    {
        if (!openWithE || !playerNear || !Keyboard.current.eKey.wasPressedThisFrame || Revive.AnyNearby()) return;
        if (IsSpawned && !IsServer) OpenRpc(); // klienten beder hosten
        else Open();
    }

    [Rpc(SendTo.Server)]
    private void OpenRpc() => Open();

    /// <summary>
    /// Åbner døren. Kan kobles på et UnityEvent i inspectoren, fx ResourceController.onDeath på en goblin.
    /// Online gør kun hostens kald noget. onDeath kører også hos hosten, så det passer.
    /// </summary>
    public void Open()
    {
        if (!IsSpawned) PlayOpen();
        else if (IsServer) netOpen.Value = true; // OnOpenChanged afspiller den hos alle
    }

    private void PlayOpen()
    {
        if (isOpen) return;
        isOpen = true;
        animator.SetTrigger("Open");
    }

    /// <summary>
    /// Kaldes af en Animation Event på sidste frame af DoorOpen-klippet.
    /// </summary>
    public void OnOpened()
    {
        if (blockingCollider != null) blockingCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other) { if (IsMe(other)) playerNear = true; }
    private void OnTriggerExit2D(Collider2D other) { if (IsMe(other)) playerNear = false; }

    // kun min egen wizard, så E ikke virker fordi en anden spiller står ved døren (samme som LevelExit)
    private static bool IsMe(Collider2D other) =>
        other.TryGetComponent(out PlayerMovementController p) && p.HasControl;
}
