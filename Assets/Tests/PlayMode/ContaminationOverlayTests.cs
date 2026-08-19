using System.Collections;
using System.Linq;
using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    // B4 integration validation for additive overlay lifecycle.
    public sealed class ContaminationOverlayTests
    {
        [UnityTest]
        public IEnumerator Test_Contamination_VariantOverlaysBaseWithoutClosingIt()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Single);
            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            Assert.That(loader, Is.Not.Null);
            yield return loader.ApplyRequest("Stage01_Overlay_Echo", true);
            Assert.That(SceneManager.GetSceneByName("Stage01_Base").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName("Stage01_Overlay_Echo").isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.GreaterThanOrEqualTo(2));
            yield return ResetLoadedScenes();
        }

        [UnityTest]
        public IEnumerator Test_Contamination_OverlayUnloadRestoresBase()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Single);
            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            var baseScene = SceneManager.GetSceneByName("Stage01_Base");
            var baseColliderCount = CountColliders(baseScene);
            yield return loader.ApplyRequest("Stage01_Overlay_Intrusion", true);
            Assert.That(CountColliders(SceneManager.GetSceneByName("Stage01_Overlay_Intrusion")), Is.GreaterThan(0));
            yield return loader.ApplyRequest("Stage01_Overlay_Intrusion", false);
            Assert.That(SceneManager.GetSceneByName("Stage01_Base").isLoaded, Is.True);
            Assert.That(CountColliders(SceneManager.GetSceneByName("Stage01_Base")), Is.EqualTo(baseColliderCount));
            Assert.That(SceneManager.GetSceneByName("Stage01_Overlay_Intrusion").isLoaded, Is.False);
            yield return ResetLoadedScenes();
        }

        [UnityTest]
        public IEnumerator Test_Contamination_DebugTriggerChangesPressureAndOverlay()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Single);
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            var memoryAdapter = Object.FindAnyObjectByType<MemoryCompletionAdapter>();
            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            Assert.That(director, Is.Not.Null);
            Assert.That(memoryAdapter, Is.Not.Null);
            memoryAdapter.TriggerDebugMemoryComplete();
            yield return loader.ApplyRequest(director.Data.IntrusionOverlayScene, true);
            Assert.That(director.Pressure, Is.EqualTo(PressureStage.Intrusion));
            Assert.That(SceneManager.GetSceneByName(director.Data.IntrusionOverlayScene).isLoaded, Is.True);
            director.SetPressure(PressureStage.Stable);
            yield return loader.ApplyRequest(director.Data.IntrusionOverlayScene, false);
            Assert.That(SceneManager.GetSceneByName("Stage01_Base").isLoaded, Is.True);
            Assert.That(SceneManager.GetSceneByName(director.Data.IntrusionOverlayScene).isLoaded, Is.False);
            yield return ResetLoadedScenes();
        }

        private static int CountColliders(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return 0;
            return scene.GetRootGameObjects().Sum(root => root.GetComponentsInChildren<Collider2D>(true).Length);
        }

        private static IEnumerator ResetLoadedScenes()
        {
            var cleanup = SceneManager.CreateScene("B4_TestCleanup");
            SceneManager.SetActiveScene(cleanup);
            for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene == cleanup) continue;
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
