using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>"Juice"-effekterne: HitStop og CameraShake skal altid vende tilbage til normal tilstand.</summary>
    public class EffectsTests
    {
        private TestScene scene;

        [SetUp]
        public void SetUp() => scene = new TestScene();

        [TearDown]
        public void TearDown()
        {
            scene.Cleanup();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator HitStop_FreezesTimeThenRestoresIt()
        {
            var hitStop = scene.Active("HitStop").AddComponent<HitStop>();
            Assert.AreSame(hitStop, HitStop.Instance);

            hitStop.Stop(0.1f);
            yield return null;
            Assert.AreEqual(0f, Time.timeScale, "Tiden skal stå stille under hit stop");

            yield return new WaitForSecondsRealtime(0.3f);
            Assert.AreEqual(1f, Time.timeScale, "Tiden skal køre normalt igen bagefter");
        }

        [UnityTest]
        public IEnumerator HitStop_TwoHitsInARow_DoesNotLeaveTimeFrozen()
        {
            // Hvis hit nr. 2 kommer mens tiden står stille, må den ikke gemme timeScale 0 som "originalen".
            var hitStop = scene.Active("HitStop").AddComponent<HitStop>();

            hitStop.Stop(0.1f);
            yield return null;
            hitStop.Stop(0.1f);

            yield return new WaitForSecondsRealtime(0.4f);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void HitStop_Destroyed_ClearsInstance()
        {
            var go = scene.Active("HitStop");
            go.AddComponent<HitStop>();

            Object.DestroyImmediate(go);

            Assert.IsTrue(ReferenceEquals(HitStop.Instance, null),
                "Instance skal nulstilles, ellers fejler HitStop.Instance?.Stop() efter et sceneskift");
        }

        [UnityTest]
        public IEnumerator CameraShake_MovesCameraThenReturnsToStart()
        {
            var cam = scene.Active("Camera");
            cam.transform.localPosition = new Vector3(1f, 2f, -10f);
            var shake = cam.AddComponent<CameraShake>();
            var start = cam.transform.localPosition;

            shake.Shake(0.1f, 0.5f);
            bool moved = false;
            for (int i = 0; i < 5; i++)
            {
                yield return null;
                moved |= cam.transform.localPosition != start;
            }
            Assert.IsTrue(moved, "Kameraet skal ryste");

            yield return new WaitForSeconds(0.2f);
            Assert.AreEqual(start, cam.transform.localPosition);
        }

        [Test]
        public void CameraShake_Destroyed_ClearsInstance()
        {
            var go = scene.Active("Camera");
            go.AddComponent<CameraShake>();

            Object.DestroyImmediate(go);

            Assert.IsTrue(ReferenceEquals(CameraShake.Instance, null));
        }
    }
}
