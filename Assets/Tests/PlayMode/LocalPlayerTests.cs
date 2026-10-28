using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>LocalPlayer offline: wizarden i scenen bliver automatisk "min" spiller.</summary>
    public class LocalPlayerTests
    {
        private TestScene scene;

        [SetUp]
        public void SetUp() => scene = new TestScene();

        [TearDown]
        public void TearDown() => scene.Cleanup();

        [UnityTest]
        public IEnumerator Offline_FirstWizardBecomesLocalPlayer()
        {
            GameObject announced = null;
            System.Action<GameObject> onChanged = p => announced = p;
            LocalPlayer.Changed += onChanged;
            try
            {
                var wizard = scene.Active("Wizard");
                wizard.AddComponent<LocalPlayer>();

                yield return null; // Start

                Assert.AreSame(wizard, LocalPlayer.Current);
                Assert.AreSame(wizard, announced);
            }
            finally
            {
                LocalPlayer.Changed -= onChanged;
            }
        }

        [UnityTest]
        public IEnumerator Offline_SecondWizardDoesNotTakeOver()
        {
            var first = scene.Active("Wizard1");
            first.AddComponent<LocalPlayer>();
            yield return null;

            var second = scene.Active("Wizard2");
            second.AddComponent<LocalPlayer>();
            yield return null;

            Assert.AreSame(first, LocalPlayer.Current);
        }

        [UnityTest]
        public IEnumerator Destroyed_ClearsLocalPlayer()
        {
            var wizard = scene.Active("Wizard");
            wizard.AddComponent<LocalPlayer>();
            yield return null;

            Object.Destroy(wizard);
            yield return null;

            Assert.IsTrue(LocalPlayer.Current == null);
        }
    }
}
