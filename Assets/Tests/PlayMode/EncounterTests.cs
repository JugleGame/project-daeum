using System.Collections;
using System.Linq;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Enemy;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class EncounterTests
    {
        private GameObject root;
        private EncounterData data;
        private EncounterController controller;
        private EncounterExitLock exitLock;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            root = new GameObject("EncounterTestRoot");
            data = ScriptableObject.CreateInstance<EncounterData>();
            data.Configure(
                "stage01.encounter.01", "stage01.encounter.01.trigger", new Vector2(2f, 3f),
                EncounterEnemyType.MeleeRemnant,
                new[] { "stage01.remnant.spawn.01", "stage01.remnant.spawn.02" },
                1, 2, EncounterClearCondition.DefeatAll, true, "stage01.encounter.01.exit",
                new[] { "hazard-stage01-warning-pulse" });

            var lockObject = new GameObject("ExitLock");
            lockObject.transform.SetParent(root.transform);
            lockObject.AddComponent<BoxCollider2D>();
            exitLock = lockObject.AddComponent<EncounterExitLock>();

            var prefabObject = new GameObject("MeleeRemnantPrefab");
            prefabObject.transform.SetParent(root.transform);
            prefabObject.transform.position = new Vector3(1000f, 1000f);
            prefabObject.AddComponent<BoxCollider2D>();
            var prefab = prefabObject.AddComponent<MeleeRemnant>();

            var pointA = new GameObject("SpawnA").transform;
            pointA.SetParent(root.transform);
            var pointB = new GameObject("SpawnB").transform;
            pointB.SetParent(root.transform);
            pointB.position = Vector3.right;

            var controllerObject = new GameObject("EncounterController");
            controllerObject.transform.SetParent(root.transform);
            controllerObject.AddComponent<BoxCollider2D>();
            controller = controllerObject.AddComponent<EncounterController>();
            controller.Configure(data, prefab, new[] { pointA, pointB }, exitLock);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Object.Destroy(root);
            Object.Destroy(data);
            yield return null;
        }

        [Test]
        public void Test_Encounter_EntryStartsFirstWaveAndLocksExit()
        {
            Assert.That(controller.TryActivate(), Is.True);
            Assert.That(controller.State, Is.EqualTo(EncounterState.Active));
            Assert.That(controller.CurrentWaveNumber, Is.EqualTo(1));
            Assert.That(controller.ActiveEnemies, Has.Count.EqualTo(1));
            Assert.That(exitLock.IsLocked, Is.True);
        }

        [Test]
        public void Test_Encounter_NextWaveAfterElimination()
        {
            controller.TryActivate();
            KillCurrentWave();
            Assert.That(controller.CurrentWaveNumber, Is.EqualTo(2));
            Assert.That(controller.TotalSpawnCount, Is.EqualTo(2));
            Assert.That(controller.ActiveEnemies, Has.Count.EqualTo(1));
        }

        [Test]
        public void Test_Encounter_ClearUnlocksExit()
        {
            controller.TryActivate();
            KillCurrentWave();
            KillCurrentWave();
            Assert.That(controller.State, Is.EqualTo(EncounterState.Cleared));
            Assert.That(exitLock.IsLocked, Is.False);
        }

        [Test]
        public void Test_Encounter_ClearedDoesNotReactivate()
        {
            controller.TryActivate();
            KillCurrentWave();
            KillCurrentWave();
            var count = controller.TotalSpawnCount;
            Assert.That(controller.TryActivate(), Is.False);
            Assert.That(controller.TotalSpawnCount, Is.EqualTo(count));
            Assert.That(controller.ActiveEnemies, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Test_WarningPulse_AffectsPlayerAndRemnantWithWarningAndCannotKill()
        {
            var hazardObject = new GameObject("WarningPulse");
            hazardObject.transform.SetParent(root.transform);
            hazardObject.AddComponent<BoxCollider2D>();
            hazardObject.AddComponent<AudioSource>();
            var hazard = hazardObject.AddComponent<WarningPulseHazard>();

            var playerObject = new GameObject("Player");
            playerObject.transform.SetParent(root.transform);
            var player = playerObject.AddComponent<PlayerHealth>();
            var remnantObject = new GameObject("Remnant");
            remnantObject.transform.SetParent(root.transform);
            remnantObject.AddComponent<BoxCollider2D>();
            var remnant = remnantObject.AddComponent<MeleeRemnant>();
            yield return null;

            var playerStart = player.CurrentHealth;
            var remnantStart = remnant.CurrentHealth;
            hazard.BeginWarningForTests(player);
            hazard.BeginWarningForTests(remnant);
            Assert.That(hazard.IsWarningActive, Is.True);
            Assert.That(hazard.AudioWarningAvailable, Is.True);
            hazard.ResolvePulse();
            Assert.That(player.CurrentHealth, Is.EqualTo(Mathf.Max(1, playerStart - 1)));
            Assert.That(remnant.CurrentHealth, Is.EqualTo(Mathf.Max(1, remnantStart - 1)));

            player.ApplyDamageAt(new DamageRequest(player.CurrentHealth - 1), Time.time + 1f);
            remnant.ApplyDamage(new DamageRequest(remnant.CurrentHealth - 1));
            Assert.That(player.CurrentHealth, Is.EqualTo(1));
            Assert.That(remnant.CurrentHealth, Is.EqualTo(1));
            hazard.BeginWarningForTests(player);
            hazard.BeginWarningForTests(remnant);
            hazard.ResolvePulse();
            Assert.That(player.CurrentHealth, Is.EqualTo(1));
            Assert.That(remnant.CurrentHealth, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Test_Stage01_EncounterFlowIsAuthored()
        {
            Object.Destroy(root);
            Object.Destroy(data);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Single);
            var authored = Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None);
            var hazards = Object.FindObjectsByType<WarningPulseHazard>(FindObjectsSortMode.None);
            Assert.That(authored, Has.Length.EqualTo(1));
            Assert.That(authored[0].Data, Is.Not.Null);
            Assert.That(authored[0].Data.ValidateData(out var error), Is.True, error);
            Assert.That(hazards, Has.Length.GreaterThanOrEqualTo(1));

            var cleanup = SceneManager.CreateScene("EncounterTestCleanup");
            SceneManager.SetActiveScene(cleanup);
            for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene == cleanup) continue;
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }

        private void KillCurrentWave()
        {
            foreach (var enemy in controller.ActiveEnemies.ToArray())
            {
                enemy.ApplyDamage(new DamageRequest(999));
            }
        }
    }
}
