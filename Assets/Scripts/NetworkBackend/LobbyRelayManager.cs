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

public class LobbyRelayManager : MonoBehaviour
{
    private Lobby lobby;
    private bool isHost;
    private float heartbeatTimer;
    
    private string statusMessage = "";

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void CreateGame()
    {
        lobby = await LobbyService.Instance.CreateLobbyAsync("Lobby", 4);
        isHost = true;
        Debug.Log($"Room code: {lobby.LobbyCode}");

        var allocation = await RelayService.Instance.CreateAllocationAsync(3);
        string relayCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        await LobbyService.Instance.UpdateLobbyAsync(lobby.Id, new UpdateLobbyOptions
        {
            Data = new Dictionary<string, DataObject>
            {
                { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
            }
        });

        NetworkManager.Singleton.GetComponent<UnityTransport>()
            .SetRelayServerData(new RelayServerData(allocation, "dtls"));
        NetworkManager.Singleton.StartHost();
    }

    public async void JoinGame(string code)
    {

        try
        {
            lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(code);
            var joinAlloc = await RelayService.Instance.JoinAllocationAsync(lobby.Data["RelayCode"].Value);

            NetworkManager.Singleton.GetComponent<UnityTransport>()
                .SetRelayServerData(new RelayServerData(joinAlloc, "dtls"));
            NetworkManager.Singleton.StartClient();
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"Kunne ikke joine: {e.Message}");
            statusMessage = "Forkert lobby-id";
        }
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
    
    
    
    //---------TEST---------
    private string joinCode = "";

    private void OnGUI()
    {
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            GUILayout.Label($"Connected players: {NetworkManager.Singleton.ConnectedClients.Count}");
            if (lobby != null) GUILayout.Label($"Room code: {lobby.LobbyCode}");
            return;
        }

        if (GUILayout.Button("Host", GUILayout.Width(150), GUILayout.Height(40)))
            CreateGame();

        joinCode = GUILayout.TextField(joinCode, GUILayout.Width(150));
        if (GUILayout.Button("Join", GUILayout.Width(150), GUILayout.Height(40)))
            JoinGame(joinCode.Trim().ToUpper());
    }
}