using System.Collections;
using System.Linq;
using NUnit.Framework;
using Tests.PlayMode;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Netcode
{
    /// <summary>
    /// GameManager online: kun hosten skifter level og afgør game over, klienterne ser det samme.
    /// GameManager.Instance er fælles for alle maskiner i én proces, så testene bruger hver maskines egen kopi.
    /// </summary>
    public class GameManagerNetcodeTests : NetcodeTestBase
    {
        private GameObject prefab;
        private NetworkObject obj;

        protected override void OnCreatePlayerPrefab()
        {
            m_PlayerPrefab.tag = "Player";
            TestScene.SetField(m_PlayerPrefab.AddComponent<ResourceController>(), "maxHealth", 5);
        }

        protected override void OnServerAndClientsCreated()
        {
            prefab = NewPrefab("GameManager");
            prefab.AddComponent<GameManager>();
        }

        private IEnumerator SpawnGameManager()
        {
            yield return Spawn(prefab, Host, o => obj = o);
        }

        [UnityTest]
        public IEnumerator NextLevel_OnHost_ReplicatesToClients()
        {
            yield return SpawnGameManager();

            On<GameManager>(Host, obj).NextLevel();

            yield return WaitFor(() => Copies<GameManager>(obj).All(g => g.Level.Value == 2), "Level blev ikke synkroniseret");
        }

        [UnityTest]
        public IEnumerator NextLevel_OnClient_IsIgnored()
        {
            yield return SpawnGameManager();

            On<GameManager>(Client1, obj).NextLevel();
            yield return WaitTicks();

            Assert.That(Copies<GameManager>(obj).Select(g => g.Level.Value), Is.All.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator AllPlayersDead_SetsGameOverForEveryone()
        {
            yield return SpawnGameManager();

            foreach (var owner in m_NetworkManagers)
                Player<ResourceController>(Host, owner).TakeDamage(999);

            yield return WaitFor(() => Copies<GameManager>(obj).All(g => g.State.Value == GameManager.GameState.GameOver),
                "Game over nåede ikke alle");
        }

        [UnityTest]
        public IEnumerator OnePlayerDead_StaysPlaying()
        {
            yield return SpawnGameManager();

            Player<ResourceController>(Host, Client1).TakeDamage(999);
            yield return WaitFor(() => PlayerCopies<ResourceController>(Client1).All(r => r.IsDead), "Spilleren døde ikke hos alle");
            yield return WaitTicks();

            Assert.That(Copies<GameManager>(obj).Select(g => g.State.Value), Is.All.EqualTo(GameManager.GameState.Playing));
        }
    }
}
