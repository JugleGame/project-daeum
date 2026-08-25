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
    [Ignore("3스테이지 재구성(#56)으로 Stage06_Base를 빌드에서 내렸다. 씬을 이름으로 로드할 수 없다.")]
    public sealed class Stage06FlowTests
    {
        [UnityTest]
        public IEnumerator Test_Stage06_EntryCombatMemoryChaseEscapeCompletesOnce()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage06_Base", LoadSceneMode.Additive);
            yield return null;

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            flow.CurrentData.CurrentStageId = 6;
            var encounters = Object.FindObjectsByType<Stage03EncounterController>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(controller => controller.Data.EncounterId)
                .ToArray();
            Assert.That(encounters, Has.Length.EqualTo(3));

            foreach (var encounter in encounters)
            {
                Assert.That(encounter.TryActivate(), Is.True, encounter.Data.EncounterId);
                Assert.That(encounter.TotalSpawnCount, Is.EqualTo(encounter.Data.SpawnCount));
                DefeatActiveEnemies(encounter);
                yield return null;
                Assert.That(encounter.State, Is.EqualTo(EncounterState.Cleared), encounter.Data.EncounterId);
                Assert.That(encounter.ExitLock.IsLocked, Is.False, encounter.Data.EncounterId);
            }

            Assert.That(encounters[0].TotalSpawnCount, Is.EqualTo(1));
            Assert.That(encounters[1].TotalSpawnCount, Is.EqualTo(2));
            Assert.That(encounters[2].TotalSpawnCount, Is.EqualTo(4));
            Assert.That(encounters[2].ActiveMeleeEnemies.Count +
                        encounters[2].ActiveDashEnemies.Count +
                        encounters[2].ActiveRangedEnemies.Count, Is.Zero,
                "Zone C must count Melee, Dash, and Ranged deaths before completion.");

            var anchor = Object.FindAnyObjectByType<MemoryAnchor>();
            Assert.That(anchor, Is.Not.Null);
            Assert.That(anchor.StableId, Is.EqualTo("memory-stage06-fragment-01"));

            var adapter = Object.FindAnyObjectByType<MemoryCompletionAdapter>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            adapter.TriggerDebugMemoryComplete();
            yield return null;
            Assert.That(GameManager.Instance.StageState, Is.EqualTo(StageState.Chase));
            Assert.That(director.ChaseActive, Is.True);
            Assert.That(director.VariantId, Is.EqualTo("Stage06_Overlay_Intrusion"));

            var chase = Object.FindAnyObjectByType<StageOneChaseController>();
            Assert.That(chase.CompleteAtEscape(), Is.True);
            Assert.That(chase.CompleteAtEscape(), Is.False, "escape transition must start only once");
            yield return WaitForScene("Title");
            Assert.That(flow.CurrentData.CurrentStageId, Is.EqualTo(6),
                "Stage07 is not implemented, so invalid Stage07 progress must not be saved.");
        }

        [UnityTest]
        public IEnumerator Test_Contamination_Stage06RetryUsesSameVariantAndBaseScene()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage06_Base", LoadSceneMode.Additive);
            yield return null;

            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            var sceneCount = SceneManager.sceneCount;
            var intrusion = OverlaySceneLoader.FindOverlayRoot(director.Data.IntrusionOverlayName);
            loader.ApplyRequest(director.Data.IntrusionOverlayName, true);
            loader.ApplyRequest(director.Data.IntrusionOverlayName, true);

            Assert.That(intrusion.activeSelf, Is.True);
            Assert.That(SceneManager.GetSceneByName("Stage06_Base").isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            Assert.That(director.VariantId, Is.EqualTo("Stage06_Overlay_Intrusion"));
        }

        private static void DefeatActiveEnemies(Stage03EncounterController encounter)
        {
            foreach (var enemy in encounter.ActiveMeleeEnemies.ToArray())
                enemy.ApplyDamage(new DamageRequest(999));
            foreach (var enemy in encounter.ActiveDashEnemies.ToArray())
                enemy.ApplyDamage(new DamageRequest(999));
            foreach (var enemy in encounter.ActiveRangedEnemies.ToArray())
                enemy.ApplyDamage(new DamageRequest(999));
        }

        private static IEnumerator WaitForScene(string name)
        {
            for (var frame = 0; frame < 600; frame++)
            {
                if (SceneManager.GetSceneByName(name).isLoaded) yield break;
                yield return null;
            }

            Assert.Fail(name + " did not load within 600 frames.");
        }
    }
}