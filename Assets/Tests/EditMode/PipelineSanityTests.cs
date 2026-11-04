using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Project-independent tests that only prove the CI pipeline can
    /// compile, run and report Unity tests.
    /// </summary>
    public class PipelineSanityTests
    {
        [Test]
        public void TestRunner_Executes()
        {
            Assert.AreEqual(2, 1 + 1);
        }

        [Test]
        public void UnityApi_IsAvailable()
        {
            Assert.IsFalse(string.IsNullOrEmpty(Application.unityVersion));
            var go = new GameObject("PipelineCheck");
            Assert.IsNotNull(go.transform);
            Object.DestroyImmediate(go);
        }

        // Excluded from normal runs. Executed only by the "verify failure"
        // path in the workflow to prove a failing test turns the pipeline red.
        [Test, Category("ShouldFail")]
        public void Deliberate_Failure()
        {
            Assert.Fail("Intentional failure to verify the pipeline reports failures.");
        }
    }
}
