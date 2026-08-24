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
    /// <summary>
    /// B4 오버레이 수명주기 검증.
    ///
    /// 오버레이는 Stage01_Base 안의 루트 오브젝트다(#38). 그래서 검사 대상이 "씬이 올라왔는가"에서
    /// "루트가 켜졌는가 + 씬 수가 그대로인가"로 바뀌었다. 씬 수 불변이 곧 "기저 레벨을 닫지 않는다"의 증거다.
    /// </summary>
    public sealed class ContaminationOverlayTests
    {
        [UnityTest]
        public IEnumerator Test_Contamination_VariantOverlaysBaseWithoutClosingIt()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Single);
            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            Assert.That(loader, Is.Not.Null);

            var sceneCountBefore = SceneManager.sceneCount;
            var echo = OverlaySceneLoader.FindOverlayRoot("Stage01_Overlay_Echo");
            Assert.That(echo, Is.Not.Null, "Echo 오버레이 루트가 Stage01_Base 안에 없다.");
            Assert.That(echo.activeSelf, Is.False, "탐색 중에 켜져 있으면 오염 지형이 미리 보인다.");

            loader.ApplyRequest("Stage01_Overlay_Echo", true);
            Assert.That(echo.activeSelf, Is.True);
            Assert.That(SceneManager.GetSceneByName("Stage01_Base").isLoaded, Is.True, "기저 공간은 닫히지 않는다.");
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore), "오버레이는 씬을 추가하지 않는다.");

            // 재시도(같은 Variant로 다시 진입)해도 결과가 같아야 한다.
            loader.ApplyRequest("Stage01_Overlay_Echo", true);
            Assert.That(echo.activeSelf, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore));
        }

        [UnityTest]
        public IEnumerator Test_Contamination_OverlayUnloadRestoresBase()
        {
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Single);
            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            var baseScene = SceneManager.GetSceneByName("Stage01_Base");
            var sceneCountBefore = SceneManager.sceneCount;
            var exploreColliders = CountActiveColliders(baseScene);

            loader.ApplyRequest("Stage01_Overlay_Intrusion", true);
            Assert.That(CountActiveColliders(baseScene), Is.GreaterThan(exploreColliders),
                "오버레이는 기저 공간에 지형을 더하기만 한다 — 실제로 콜라이더가 늘어야 의미가 있다.");

            loader.ApplyRequest("Stage01_Overlay_Intrusion", false);
            Assert.That(baseScene.isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore));
            Assert.That(CountActiveColliders(baseScene), Is.EqualTo(exploreColliders),
                "오버레이를 걷으면 기저 지형만 남아야 한다.");
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

            var sceneCountBefore = SceneManager.sceneCount;
            var intrusion = OverlaySceneLoader.FindOverlayRoot(director.Data.IntrusionOverlayName);
            Assert.That(intrusion, Is.Not.Null, "Intrusion 오버레이 루트가 Stage01_Base 안에 없다.");

            memoryAdapter.TriggerDebugMemoryComplete();
            loader.ApplyRequest(director.Data.IntrusionOverlayName, true);
            Assert.That(director.Pressure, Is.EqualTo(PressureStage.Intrusion));
            Assert.That(intrusion.activeSelf, Is.True);

            director.SetPressure(PressureStage.Stable);
            loader.ApplyRequest(director.Data.IntrusionOverlayName, false);
            Assert.That(intrusion.activeSelf, Is.False);
            Assert.That(SceneManager.GetSceneByName("Stage01_Base").isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore));
        }

        private static int CountActiveColliders(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return 0;
            return scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Collider2D>(true))
                .Count(collider => collider.gameObject.activeInHierarchy);
        }
    }
}
