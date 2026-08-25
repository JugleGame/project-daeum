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
    [Ignore("3스테이지 재구성(#56)으로 Stage04_Base를 빌드에서 내렸다. 씬을 이름으로 로드할 수 없다.")]
    public sealed class Stage04FlowTests
    {
        [UnityTest]
        public IEnumerator Test_Stage04_EntryCombatMemoryChaseEscapeCompletesOnce()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage04_Base", LoadSceneMode.Additive);
            yield return null;
            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            flow.CurrentData.CurrentStageId = 4;
            var encounters = Object.FindObjectsByType<Stage03EncounterController>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(controller => controller.Data.EncounterId).ToArray();
            Assert.That(encounters, Has.Length.EqualTo(3));
            foreach (var encounter in encounters)
            {
                Assert.That(encounter.TryActivate(), Is.True, encounter.Data.EncounterId);
                foreach (var enemy in encounter.ActiveMeleeEnemies.ToArray()) enemy.ApplyDamage(new DamageRequest(999));
                foreach (var enemy in encounter.ActiveDashEnemies.ToArray()) enemy.ApplyDamage(new DamageRequest(999));
                yield return null;
                Assert.That(encounter.State, Is.EqualTo(EncounterState.Cleared), encounter.Data.EncounterId);
            }
            var anchor = Object.FindAnyObjectByType<MemoryAnchor>();
            Assert.That(anchor.StableId, Is.EqualTo("memory-stage04-fragment-01"));
            var adapter = Object.FindAnyObjectByType<MemoryCompletionAdapter>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            adapter.TriggerDebugMemoryComplete();
            yield return null;
            Assert.That(GameManager.Instance.StageState, Is.EqualTo(StageState.Chase));
            Assert.That(director.ChaseActive, Is.True);
            Assert.That(director.VariantId, Is.EqualTo("Stage04_Overlay_Intrusion"));
            var chase = Object.FindAnyObjectByType<StageOneChaseController>();
            Assert.That(chase.CompleteAtEscape(), Is.True);
            Assert.That(chase.CompleteAtEscape(), Is.False, "escape transition must start only once");
            yield return WaitForScene("Title");
            Assert.That(flow.CurrentData.CurrentStageId, Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator Test_Contamination_Stage04RetryUsesSameVariantAndBaseScene()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage04_Base", LoadSceneMode.Additive);
            yield return null;
            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            var count = SceneManager.sceneCount;
            var intrusion = OverlaySceneLoader.FindOverlayRoot(director.Data.IntrusionOverlayName);
            loader.ApplyRequest(director.Data.IntrusionOverlayName, true);
            loader.ApplyRequest(director.Data.IntrusionOverlayName, true);
            Assert.That(intrusion.activeSelf, Is.True);
            Assert.That(SceneManager.GetSceneByName("Stage04_Base").isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(count));
            Assert.That(director.VariantId, Is.EqualTo("Stage04_Overlay_Intrusion"));
        }

        private static IEnumerator WaitForScene(string name)
        {
            for (var frame = 0; frame < 600; frame++)
            {
                if (SceneManager.GetSceneByName(name).isLoaded) yield break;
                yield return null;
            }
            Assert.Fail($"{name} did not load within 600 frames.");
        }
    }
}
