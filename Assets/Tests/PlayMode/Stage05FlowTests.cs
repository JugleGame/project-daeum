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
    public sealed class Stage05FlowTests
    {
        [UnityTest]
        public IEnumerator Test_Stage04_ClearEntersStage05AndResetsTransientProgress()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage04_Base", LoadSceneMode.Additive);
            yield return null;
            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            flow.CurrentData.CurrentStageId = 4;
            flow.CurrentData.CheckpointId = "stage04.checkpoint.chase";
            flow.CurrentData.ContaminationVariantId = "Stage04_Overlay_Intrusion";
            Assert.That(Object.FindAnyObjectByType<StageOneChaseController>().CompleteAtEscape(), Is.True);
            yield return WaitForScene("Stage05_Base");
            Assert.That(flow.CurrentData.CurrentStageId, Is.EqualTo(5));
            Assert.That(flow.CurrentData.CheckpointId, Is.Empty);
            Assert.That(flow.CurrentData.ContaminationVariantId, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Test_Stage05_EntryCombatMemoryChaseEscapeCompletesOnce()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage05_Base", LoadSceneMode.Additive);
            yield return null;
            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            flow.CurrentData.CurrentStageId = 5;
            var encounters = Object.FindObjectsByType<Stage03EncounterController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OrderBy(controller => controller.Data.EncounterId).ToArray();
            Assert.That(encounters, Has.Length.EqualTo(3));
            foreach (var encounter in encounters)
            {
                Assert.That(encounter.TryActivate(), Is.True, encounter.Data.EncounterId);
                for (var expectedWave = 1; expectedWave <= encounter.Data.WaveCount; expectedWave++)
                {
                    Assert.That(encounter.CurrentWaveNumber, Is.EqualTo(expectedWave));
                    DefeatActiveEnemies(encounter);
                    yield return null;
                }
                Assert.That(encounter.State, Is.EqualTo(EncounterState.Cleared), encounter.Data.EncounterId);
                Assert.That(encounter.ExitLock.IsLocked, Is.False, encounter.Data.EncounterId);
            }

            var anchor = Object.FindAnyObjectByType<MemoryAnchor>();
            Assert.That(anchor.StableId, Is.EqualTo("memory-stage05-fragment-01"));
            var adapter = Object.FindAnyObjectByType<MemoryCompletionAdapter>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            adapter.TriggerDebugMemoryComplete();
            yield return null;
            Assert.That(GameManager.Instance.StageState, Is.EqualTo(StageState.Chase));
            Assert.That(director.ChaseActive, Is.True);
            Assert.That(director.VariantId, Is.EqualTo("Stage05_Overlay_Intrusion"));
            var chase = Object.FindAnyObjectByType<StageOneChaseController>();
            Assert.That(chase.CompleteAtEscape(), Is.True);
            Assert.That(chase.CompleteAtEscape(), Is.False, "escape transition must start only once");
            yield return WaitForScene("Title");
            Assert.That(flow.CurrentData.CurrentStageId, Is.EqualTo(5), "Stage06 is not implemented, so progress must remain at Stage05.");
        }

        [UnityTest]
        public IEnumerator Test_Stage05_ZoneCSpawnsSecondWaveBeforeUnlockingExit()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage05_Base", LoadSceneMode.Additive);
            yield return null;
            var encounter = Object.FindObjectsByType<Stage03EncounterController>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Single(value => value.Data.EncounterId == "stage05.encounter.03");
            Assert.That(encounter.TryActivate(), Is.True);
            Assert.That(encounter.ActiveMeleeEnemies.Count + encounter.ActiveDashEnemies.Count, Is.EqualTo(5));
            DefeatActiveEnemies(encounter);
            yield return null;
            Assert.That(encounter.CurrentWaveNumber, Is.EqualTo(2));
            Assert.That(encounter.State, Is.EqualTo(EncounterState.Active));
            Assert.That(encounter.ExitLock.IsLocked, Is.True);
            Assert.That(encounter.ActiveMeleeEnemies.Count + encounter.ActiveDashEnemies.Count, Is.EqualTo(5));
            DefeatActiveEnemies(encounter);
            yield return null;
            Assert.That(encounter.State, Is.EqualTo(EncounterState.Cleared));
            Assert.That(encounter.TotalSpawnCount, Is.EqualTo(10));
            Assert.That(encounter.ExitLock.IsLocked, Is.False);
        }

        [UnityTest]
        public IEnumerator Test_Contamination_Stage05RetryUsesSameVariantAndBaseScene()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage05_Base", LoadSceneMode.Additive);
            yield return null;
            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            var sceneCount = SceneManager.sceneCount;
            var intrusion = OverlaySceneLoader.FindOverlayRoot(director.Data.IntrusionOverlayName);
            loader.ApplyRequest(director.Data.IntrusionOverlayName, true);
            loader.ApplyRequest(director.Data.IntrusionOverlayName, true);
            Assert.That(intrusion.activeSelf, Is.True);
            Assert.That(SceneManager.GetSceneByName("Stage05_Base").isLoaded, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCount));
            Assert.That(director.VariantId, Is.EqualTo("Stage05_Overlay_Intrusion"));
        }

        private static void DefeatActiveEnemies(Stage03EncounterController encounter)
        {
            foreach (var enemy in encounter.ActiveMeleeEnemies.ToArray()) enemy.ApplyDamage(new DamageRequest(999));
            foreach (var enemy in encounter.ActiveDashEnemies.ToArray()) enemy.ApplyDamage(new DamageRequest(999));
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
