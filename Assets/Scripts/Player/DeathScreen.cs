using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game over-skærm for min egen wizard.
/// Lever en anden spiller endnu, venter man (revive kommer senere). Er alle døde (eller solo), kan man trykke
/// Try again: hosten genstarter den nuværende level for alle, med inventory, powerups og spell som da levelen startede.
/// Sættes på wizard-prefabben.
/// </summary>
[RequireComponent(typeof(ResourceController), typeof(Inventory), typeof(PlayerAttackController))]
public class DeathScreen : NetworkBehaviour
{
    [Tooltip("UI/Prefabs/DeathScreenUI. Sættes af Tools > Create Death Screen UI.")]
    [SerializeField] private DeathScreenUI uiPrefab;

    private ResourceController health;
    private DeathScreenUI ui;
    private bool wasDead;

    private void Awake()
    {
        health = GetComponent<ResourceController>();
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (uiPrefab == null) Debug.LogError($"{name}: DeathScreen mangler uiPrefab. Kør Tools > Create Death Screen UI.", this);
    }

    public override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (ui != null) Destroy(ui.gameObject); // fx når spilleren forlader spillet
        base.OnDestroy();
    }

    // checkpoint = tilstanden da levelen startede. Gemmes på alle maskiner for alle wizards,
    // fordi powerups og spell findes på alle maskiner (inventory kun hos ejeren)
    private void Start() => SaveCheckpoint(); // første level
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => SaveCheckpoint(); // hver ny level, også hos klienter

    private void SaveCheckpoint()
    {
        GetComponent<Inventory>().SaveCheckpoint();
        GetComponent<PlayerManager>().SaveCheckpoint();
        GetComponent<PlayerAttackController>().SaveCheckpoint();
    }

    [Rpc(SendTo.Everyone)]
    private void LoadCheckpointRpc()
    {
        GetComponent<Inventory>().LoadCheckpoint();
        GetComponent<PlayerManager>().LoadCheckpoint();
        GetComponent<PlayerAttackController>().LoadCheckpoint();
    }

    // min egen wizard er død, så den tæller aldrig med
    private static bool AnyoneAlive()
    {
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
            if (!p.GetComponent<ResourceController>().IsDead) return true;
        return false;
    }

    private void TryAgain()
    {
        if (IsSpawned) TryAgainRpc();
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name); // offline
    }

    // kun serveren må skifte scene (som LevelExit). Serveren tjekker igen, så et dobbelt-klik ikke genstarter to gange
    [Rpc(SendTo.Server)]
    private void TryAgainRpc()
    {
        if (AnyoneAlive()) return;
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            // checkpoint før scene-skiftet, ellers gemmer den nye scene den gamle tilstand oven i
            p.GetComponent<DeathScreen>().LoadCheckpointRpc();
            p.GetComponent<ResourceController>().ResetHealth(); // wizards overlever scene-skift, så de skal have livet igen
        }
        NetworkManager.SceneManager.LoadScene(SceneManager.GetActiveScene().name, LoadSceneMode.Single);
    }

    //---------GAME OVER-SKÆRM---------
    // Sedlen ligger i den aktive scene, så den forsvinder ved Try again og laves igen næste gang man dør
    private void Update()
    {
        // Efter Try again rulles alt det tilbage, som Wizard2-prefab'ens onDeath gør: SetTrigger("Die"),
        // movement og attack slås fra, og Rigidbody2D.simulated = false. Tilføjes der mere til onDeath, skal det også rulles tilbage her.
        // Death-staten i animatoren har ingen vej ud, så Idle sættes direkte. Kører på alle maskiner, ligesom onDeath
        if (wasDead && !health.IsDead)
        {
            GetComponent<Animator>().Play("Idle", 0, 0f);
            GetComponent<PlayerMovementController>().enabled = true;
            GetComponent<PlayerAttackController>().enabled = true;
            GetComponent<Rigidbody2D>().simulated = true;
        }
        wasDead = health.IsDead;

        bool show = health.IsDead && (!IsSpawned || IsOwner);
        if (show && ui == null && uiPrefab != null)
        {
            ui = Instantiate(uiPrefab);
            ui.tryAgainButton.onClick.AddListener(TryAgain);
            // waitForReviveButton gør ikke noget endnu - revive kommer senere
        }
        if (ui == null) return;

        if (ui.gameObject.activeSelf != show) ui.gameObject.SetActive(show);
        if (!show) return;

        string killer = health.KilledBy;
        ui.killedByText.text = string.IsNullOrEmpty(killer) ? "" : $"Killed by: {killer}";

        // lever en anden, venter man på revive. Er alle døde, kan man prøve igen
        bool mateAlive = AnyoneAlive();
        ui.tryAgainButton.interactable = !mateAlive;
        ui.waitForReviveButton.interactable = mateAlive;
    }
}
