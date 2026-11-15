using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Game over-skærm for min egen wizard.
/// Lever en anden spiller endnu, kan man trykke Wait for revive: sedlen forsvinder, og man ser kun rummet hvor man døde,
/// indtil medspilleren revive'r en (Revive). Dør medspilleren også, kommer sedlen igen. Er alle døde (eller solo), kan man trykke
/// Try again: hosten sender alle tilbage til første level (DungeonScene) uden items, powerups eller nyt spell.
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
    private bool waitingForRevive;

    private void Awake()
    {
        health = GetComponent<ResourceController>();
        if (uiPrefab == null) Debug.LogError($"{name}: DeathScreen mangler uiPrefab. Kør Tools > Create Death Screen UI.", this);
    }

    public override void OnDestroy()
    {
        if (ui != null) Destroy(ui.gameObject); // fx når spilleren forlader spillet
        if (victoryUi != null) Destroy(victoryUi.gameObject);
        base.OnDestroy();
    }

    private const string FirstScene = "DungeonScene";

    // checkpoint = tilstanden da wizarden spawnede (ingen items). Gemmes på alle maskiner for alle wizards,
    // fordi powerups og spell findes på alle maskiner (inventory kun hos ejeren)
    private void Start() => SaveCheckpoint();

    private void SaveCheckpoint()
    {
        GetComponent<Inventory>().SaveCheckpoint();
        GetComponent<PlayerManager>().SaveCheckpoint();
        GetComponent<PlayerAttackController>().SaveCheckpoint();
    }

    [Rpc(SendTo.Everyone)]
    private void LoadCheckpointRpc()
    {
        gameWon = false; // nyt run
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

    // Rummets lys gik ud, da wizarden døde (Rigidbody2D.simulated = false fyrer OnTriggerExit2D), så det tændes igen:
    // man ser kun rummet hvor man døde. Kameraet følger stadig min døde wizard
    private void WaitForRevive()
    {
        waitingForRevive = true;
        RoomVisibility.LightAt(transform.position);
    }

    private void TryAgain()
    {
        if (IsSpawned) TryAgainRpc();
        else SceneManager.LoadScene(FirstScene); // offline
    }

    // kun serveren må skifte scene (som LevelExit). Serveren tjekker igen, så et dobbelt-klik ikke genstarter to gange
    [Rpc(SendTo.Server)]
    private void TryAgainRpc()
    {
        if (AnyoneAlive()) return;
        Restart();
    }

    // hosten: alle tilbage til første level uden items, powerups eller nyt spell
    private void Restart()
    {
        foreach (GameObject p in GameObject.FindGameObjectsWithTag("Player"))
        {
            p.GetComponent<DeathScreen>().LoadCheckpointRpc();
            p.GetComponent<ResourceController>().ResetHealth(); // wizards overlever scene-skift, så de skal have livet igen
        }
        NetworkManager.SceneManager.LoadScene(FirstScene, LoadSceneMode.Single);
    }

    //---------VICTORY-SKÆRM---------
    // Samme seddel som game over, med andre tekster. Statisk, fordi den gælder hele spillet på denne maskine
    // (hosten bruger den også til at afvise et dobbelt-klik på Play again)
    private static bool gameWon;
    private DeathScreenUI victoryUi;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => gameWon = false;

    /// <summary>Kaldes når den sidste boss dør (KnightBossPhases). onDeath kører på alle maskiner, så alle får skærmen.</summary>
    public static void ShowVictory()
    {
        if (gameWon) return;
        gameWon = true;
        if (LocalPlayer.Current != null) LocalPlayer.Current.GetComponent<DeathScreen>().ShowVictoryUI();
    }

    private void ShowVictoryUI()
    {
        if (uiPrefab == null) return;
        if (ui != null) ui.gameObject.SetActive(false); // game over-sedlen væk, hvis man lå død og ventede på revive
        victoryUi = Instantiate(uiPrefab);
        victoryUi.titleText.text = "VICTORY";
        victoryUi.messageText.text = "Sir John has fallen. The crypt is free at last.";
        victoryUi.killedByText.text = "Thanks for playing Dungeon Quest!";
        SetButton(victoryUi.tryAgainButton, "Play again", PlayAgain);
        SetButton(victoryUi.waitForReviveButton, "Quit", Application.Quit);
    }

    private static void SetButton(UnityEngine.UI.Button button, string label, UnityEngine.Events.UnityAction onClick)
    {
        button.GetComponentInChildren<TMPro.TMP_Text>().text = label;
        button.interactable = true;
        button.onClick.AddListener(onClick);
    }

    private void PlayAgain()
    {
        if (IsSpawned) PlayAgainRpc();
        else { gameWon = false; SceneManager.LoadScene(FirstScene); } // offline
    }

    [Rpc(SendTo.Server)]
    private void PlayAgainRpc()
    {
        if (gameWon) Restart(); // Restart sætter gameWon = false hos alle, så et klik mere gør ingenting
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
            waitingForRevive = false;
        }
        wasDead = health.IsDead;

        // venter man på revive, er sedlen væk, indtil medspilleren også dør
        bool show = !gameWon && health.IsDead && (!IsSpawned || IsOwner) && !(waitingForRevive && AnyoneAlive());
        if (show && ui == null && uiPrefab != null)
        {
            ui = Instantiate(uiPrefab);
            ui.tryAgainButton.onClick.AddListener(TryAgain);
            ui.waitForReviveButton.onClick.AddListener(WaitForRevive);
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
