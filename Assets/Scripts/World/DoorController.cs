using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Dør der åbner når spilleren står ved den og trykker E.
/// Kræver to Collider2D på døren: en solid der blokerer spilleren,
/// og en lidt større med Is Trigger slået til (området hvor man kan trykke E).
/// Animationen ligger i Door.controller (Closed -> Open via trigger "Open").
/// </summary>
[RequireComponent(typeof(Animator))]
public class DoorController : MonoBehaviour
{
    [SerializeField] private Collider2D blockingCollider; // den solide collider, slås fra når døren er åben
    [SerializeField] private bool openWithE = true; // slå fra på døre der kun skal åbnes via Open() (fx når en fjende dør)
    [SerializeField] private ResourceController enemy; // valgfri: døren åbner når denne fjende dør

    private Animator animator;
    private bool playerNear;
    private bool isOpen;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        if (enemy != null) enemy.onDeath.AddListener(Open); //hvis goblin dør, så åbner døren 
    }

    private void Update()
    {
        if (openWithE && playerNear && Keyboard.current.eKey.wasPressedThisFrame)
            Open();
    }

    /// <summary>
    /// Åbner døren. Kan kobles på et UnityEvent i inspectoren, fx ResourceController.onDeath på en goblin.
    /// </summary>
    public void Open()
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
        blockingCollider.enabled = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNear = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player")) playerNear = false;
    }
}
