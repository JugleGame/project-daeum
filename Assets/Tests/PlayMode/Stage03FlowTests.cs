using System.Collections;
using System.Linq;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Flow;
using Daeume.Memory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    /// <summary>Issue #13: Stage 03 진입 → 전투 → 회상 → 추격 흐름을 검증한다.</summary>
    [Ignore("3스테이지 재구성(#56)으로 Stage03_Base를 빌드에서 내렸다. 씬을 이름으로 로드할 수 없다.")]
    public sealed class Stage03FlowTests
    {
        [UnityTest]
        public IEnumerator Test_Progression_Stage02ClearAdvancesToStage03AndRaisesCurrentStageId()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.That(flow, Is.Not.Null);

            flow.CurrentData.CurrentStageId = 2;
            GameManager.Instance.ResetStage(StageState.Explore);
            GameManager.Instance.SetStageState(StageState.Memory);
            GameManager.Instance.SetStageState(StageState.Chase);
            Assert.That(flow.CompleteStageOne(), Is.True);

            yield return WaitForScene("Stage03_Base");
            Assert.That(flow.CurrentData.CurrentStageId, Is.EqualTo(3));
            Assert.That(flow.CurrentData.CheckpointId, Is.Empty);
            Assert.That(flow.CurrentData.ContaminationVariantId, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Test_Stage03_DashTutorialThenMemoryStartsTheChase()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage03_Base", LoadSceneMode.Additive);
            yield return null;

            var tutorial = Object.FindObjectsByType<Stage03EncounterController>(FindObjectsSortMode.None)
                .Single(controller => controller.Data.EncounterId == "stage03.encounter.01");
            Assert.That(tutorial.TryActivate(), Is.True);
            Assert.That(tutorial.State, Is.EqualTo(EncounterState.Active));
            Assert.That(tutorial.ActiveDashEnemies.Count, Is.EqualTo(1));
            Assert.That(tutorial.ActiveMeleeEnemies, Is.Empty);
            tutorial.ActiveDashEnemies.Single().ApplyDamage(new DamageRequest(999));
            yield return null;
            Assert.That(tutorial.State, Is.EqualTo(EncounterState.Cleared));

            var anchor = Object.FindAnyObjectByType<MemoryAnchor>();
            Assert.That(anchor, Is.Not.Null);
            Assert.That(anchor.StableId, Is.EqualTo("memory-stage03-fragment-01"));

            var memoryAdapter = Object.FindAnyObjectByType<MemoryCompletionAdapter>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            memoryAdapter.TriggerDebugMemoryComplete();
            yield return null;

            Assert.That(GameManager.Instance.StageState, Is.EqualTo(StageState.Chase));
            Assert.That(director.ChaseActive, Is.True);
            Assert.That(director.VariantId, Is.EqualTo("Stage03_Overlay_Intrusion"));
        }

        [UnityTest]
        public IEnumerator Test_Stage03_MixedEncountersSpawnDeclaredCompositions()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage03_Base", LoadSceneMode.Additive);
            yield return null;

            var controllers = Object.FindObjectsByType<Stage03EncounterController>(FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .OrderBy(controller => controller.Data.EncounterId).ToArray();
            Assert.That(controllers, Has.Length.EqualTo(3));

            Assert.That(controllers[1].TryActivate(), Is.True);
            Assert.That(controllers[1].ActiveMeleeEnemies, Has.Count.EqualTo(1));
            Assert.That(controllers[1].ActiveDashEnemies, Has.Count.EqualTo(1));

            Assert.That(controllers[2].TryActivate(), Is.True);
            Assert.That(controllers[2].ActiveMeleeEnemies, Has.Count.EqualTo(1));
            Assert.That(controllers[2].ActiveDashEnemies, Has.Count.EqualTo(2));
            Assert.That(controllers[2].CurrentWaveNumber, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Test_Contamination_Stage03VariantOverlaysBaseWithoutClosingIt()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage03_Base", LoadSceneMode.Additive);
            yield return null;

            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            var sceneCountBefore = SceneManager.sceneCount;
            var echo = OverlaySceneLoader.FindOverlayRoot(director.Data.EchoOverlayName);
            Assert.That(echo, Is.Not.Null);
            Assert.That(echo.activeSelf, Is.False);

            loader.ApplyRequest(director.Data.EchoOverlayName, true);
            Assert.That(echo.activeSelf, Is.True);
            Assert.That(SceneManager.GetSceneByName("Stage03_Base").isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore));

            loader.ApplyRequest(director.Data.EchoOverlayName, true);
            Assert.That(echo.activeSelf, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore));

            loader.ApplyRequest(director.Data.EchoOverlayName, false);
            Assert.That(echo.activeSelf, Is.False);
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            for (var frame = 0; frame < 600; frame++)
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid() && scene.isLoaded) yield break;
                yield return null;
            }

            Assert.Fail($"{sceneName} did not load within 600 frames.");
        }
    }
}
