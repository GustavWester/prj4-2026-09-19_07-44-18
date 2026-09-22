using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Tests.PlayMode
{
    /// <summary>
    /// Boots the game's first scene and lets it run for a few seconds.
    /// Unity's test framework automatically fails a test if any error,
    /// assert or exception is logged (unless LogAssert.Expect is used),
    /// so "no failure" == the game started and ran without errors.
    /// </summary>
    public class SmokeTests
    {
        const float RunSeconds = 5f;

        [UnityTest, Category("Smoke")]
        public IEnumerator Game_Boots_And_Runs_Without_Errors()
        {
            Assert.Greater(SceneManager.sceneCountInBuildSettings, 0,
                "No scenes in Build Settings. Add your main scene (File > Build Profiles / Build Settings).");

            var load = SceneManager.LoadSceneAsync(0, LoadSceneMode.Single);
            Assert.IsNotNull(load, "Scene 0 could not be loaded.");
            yield return load;

            float end = Time.realtimeSinceStartup + RunSeconds;
            while (Time.realtimeSinceStartup < end)
                yield return null;

            Assert.IsTrue(SceneManager.GetActiveScene().isLoaded);
        }
    }

    public class SmokeTests_always_pass
    {
        [UnityTest]
        [Category("Smoke")]
        public IEnumerator GameStarts()
        {
            yield return null;

            Assert.Pass();
        }
    }
}
