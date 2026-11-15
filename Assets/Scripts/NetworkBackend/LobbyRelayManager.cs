using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyRelayManager : MonoBehaviour
{
    private Lobby lobby;
    private bool busy; // et ekstra klik på Host/Join, mens vi venter, ignoreres (ellers laves der to lobbies)
    private bool isHost;
    private float heartbeatTimer;
    
    private string statusMessage = "";

    private async void Start()
    {
        // Ingen adgang til spillet uden login
        if (LoginScreen.Instance == null || !LoginScreen.Instance.IsVerified)
        {
            SceneManager.LoadScene("LoginScene");
            return;
        }

        await UnityServices.InitializeAsync();
        // Try again genindlæser DungeonScene, og så er vi allerede logget ind
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateGame()
    {
        if (busy || NetworkManager.Singleton.IsListening) return;
        busy = true;
        try
        {
            lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby", 4);
            isHost = true;
            Debug.Log($"Room code: {lobby.LobbyCode}");

            var allocation = await RelayService.Instance.CreateAllocationAsync(3);
            string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            // hosten forbinder til Relay med det samme, før koden lægges i lobbyen. Ellers kan en langsom lobby-server
            // forsinke det, og en klient kan finde koden, før hosten er klar
            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(new RelayServerData(allocation, "dtls"));
            NetworkManager.Singleton.StartHost();

            await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            });
        }
        finally { busy = false; }
    }

    public async void JoinGame(string code)
    {
        if (busy || NetworkManager.Singleton.IsListening) return;
        busy = true;
        try
        {
            lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(lobby.Data["RelayCode"].Value);

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAlloc, "dtls"));
            NetworkManager.Singleton.StartClient();
        }
        // alle fejl vises (fx Relay), ikke kun "Forkert lobby-id", så man kan se hvad der går galt
        catch (System.Exception e)
        {
            Debug.LogWarning($"Kunne ikke joine: {e.Message}");
            statusMessage = e is LobbyServiceException { Reason: LobbyExceptionReason.LobbyNotFound or LobbyExceptionReason.InvalidJoinCode }
                ? "Forkert lobby-id"
                : $"Join failed: {e.Message}";
        }
        finally { busy = false; }
    }

    private async void Update()
    {
        if (!isHost || lobby == null) return;
        heartbeatTimer -= Time.deltaTime;
        if (heartbeatTimer <= 0)
        {
            heartbeatTimer = 15;
            await LobbyService.Instance.SendHeartbeatPingAsync(lobby.Id);
        }
    }
    
    
    
   //---------MENU---------
private string joinCode = "";
private bool showJoin; // false = Host/Join-knapper, true = felt til lobby-id
private GUIStyle titleStyle, buttonStyle, fieldStyle, labelStyle;

private void OnGUI()
{
    if (NetworkManager.Singleton != null &&
        (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
    {
        // klienten er startet, men hosten har ikke svaret endnu (giver op efter ca. 60 sek.)
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsConnectedClient)
        {
            GUILayout.Label("Connecting to host…");
            return;
        }
        GUILayout.Label($"Connected players: {NetworkManager.Singleton.ConnectedClients.Count}");
        if (lobby != null) GUILayout.Label($"Room code: {lobby.LobbyCode}");
        return;
    }

    if (titleStyle == null) // GUI.skin findes kun inde i OnGUI, så styles laves her første gang
    {
        titleStyle  = new GUIStyle(GUI.skin.label)     { fontSize = 64, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        buttonStyle = new GUIStyle(GUI.skin.button)    { fontSize = 32 };
        fieldStyle  = new GUIStyle(GUI.skin.textField) { fontSize = 32, alignment = TextAnchor.MiddleCenter };
        labelStyle  = new GUIStyle(GUI.skin.label)     { fontSize = 24, alignment = TextAnchor.MiddleCenter };
    }

    // en kolonne midt på skærmen
    float w = 500;
    GUILayout.BeginArea(new Rect((Screen.width - w) / 2, Screen.height * 0.15f, w, Screen.height * 0.8f));
    GUILayout.Label("Dungeon Quest", titleStyle, GUILayout.Height(120));
    GUILayout.Space(40);

    if (!showJoin)
    {
        if (GUILayout.Button("Host", buttonStyle, GUILayout.Height(70))) CreateGame();
        GUILayout.Space(20);
        if (GUILayout.Button("Join", buttonStyle, GUILayout.Height(70))) showJoin = true;
    }
    else
    {
        GUILayout.Label("Lobby-id", labelStyle);
        joinCode = GUILayout.TextField(joinCode, fieldStyle, GUILayout.Height(60));
        GUILayout.Space(20);
        if (GUILayout.Button("Join", buttonStyle, GUILayout.Height(70))) JoinGame(joinCode.Trim().ToUpper());
        GUILayout.Space(10);
        if (GUILayout.Button("Tilbage", buttonStyle, GUILayout.Height(50))) { showJoin = false; statusMessage = ""; }
    }

    // statusMessage ("Forkert lobby-id") blev sat før, men aldrig vist
    if (statusMessage != "") GUILayout.Label(statusMessage, labelStyle);
    GUILayout.EndArea();
}
}