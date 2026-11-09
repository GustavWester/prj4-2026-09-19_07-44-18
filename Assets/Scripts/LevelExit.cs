using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Stige til næste level. Når bossen er død, og min spiller står ved stigen og trykker E, skifter hele gruppen scene.
/// Kræver NetworkObject + en Collider2D med Is Trigger (området hvor man kan trykke E).
/// Scenen skal ligge i Build Settings.
/// </summary>
public class LevelExit : NetworkBehaviour
{
    [SerializeField] private string nextScene;
    [SerializeField] private ResourceController boss; // RedWizard i scenen; stigen virker først når den er død
    private bool playerNear;

    // null = bossen er despawnet efter døden (EnemyController).
    // CurrentHealth i stedet for IsDead, så det også passer for klienter der joiner efter bossen døde
    private bool Unlocked => boss == null || boss.CurrentHealth <= 0;

    private void Update()
    {
        if (playerNear && Unlocked && Keyboard.current.eKey.wasPressedThisFrame) UseRpc();
    }

    // kun serveren må skifte scene, NGO tager alle klienter med. Serveren tjekker igen, den har det sidste ord
    [Rpc(SendTo.Server)]
    private void UseRpc()
    {
        if (Unlocked) StartCoroutine(LoadNext());
    }

    // ponytail: omvej for en Unity-editor-deadlock: Destroy() af en SpriteRenderer midt i en async scene-load fryser editoren.
    // Netcode sletter loot/ghosts mv. under loadet, så de despawnes først og når at blive slettet før loadet starter.
    private System.Collections.IEnumerator LoadNext()
    {
        foreach (var obj in new System.Collections.Generic.List<NetworkObject>(NetworkManager.SpawnManager.SpawnedObjectsList))
            if (!obj.IsPlayerObject && !obj.InScenePlaced && obj.DestroyWithScene) obj.Despawn(true);
        yield return null;
        NetworkManager.SceneManager.LoadScene(nextScene, LoadSceneMode.Single);
    }

    private void OnTriggerEnter2D(Collider2D other) { if (IsMe(other)) playerNear = true; }
    private void OnTriggerExit2D(Collider2D other) { if (IsMe(other)) playerNear = false; }

    // kun min egen wizard, så E ikke virker fordi en anden spiller står ved stigen
    private static bool IsMe(Collider2D other) =>
        other.TryGetComponent(out PlayerMovementController p) && p.HasControl;
}
