using Daeume.Core;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class SaveSystemTests
    {
        [Test]
        public void Test_Save_FirstRunStartsStageOne()
        {
            var result = new SaveSystem(new MemoryStore()).Load(3);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.FirstRun));
            Assert.That(result.Data.CurrentStageId, Is.EqualTo(1));
        }

        [Test]
        public void Test_Save_MemoryNeverDuplicates()
        {
            var data = new SaveData();
            SaveSystem.AddUnique(data.CompletedMemoryAnchors, "Memory01");
            SaveSystem.AddUnique(data.CompletedMemoryAnchors, "Memory01");
            Assert.That(data.CompletedMemoryAnchors, Has.Count.EqualTo(1));
        }

        [Test]
        public void Test_Save_ChaseDeathSkipsReplayAndKeepsVariant()
        {
            var store = new MemoryStore();
            var system = new SaveSystem(store);
            system.Save(new SaveData
            {
                CheckpointId = "Stage01_Chase",
                ContaminationVariantId = "Stage01_Intrusion"
            }, 3);
            var loaded = system.Load(3).Data;
            Assert.That(loaded.CheckpointId, Is.EqualTo("Stage01_Chase"));
            Assert.That(loaded.ContaminationVariantId, Is.EqualTo("Stage01_Intrusion"));
        }

        [Test]
        public void Test_Save_RespawnHealthUsesCheckpointPolicy()
        {
            var data = new SaveData { PlayerHealth = 1 };
            Assert.That(SaveSystem.ResolveRespawnHealth(data, 3, false, 3), Is.EqualTo(1));
            Assert.That(SaveSystem.ResolveRespawnHealth(data, 3, true, 3), Is.EqualTo(3));
        }

        [Test]
        public void Test_Save_UnclearedEncounterRestartsAllWaves()
        {
            var data = RoundTrip(new SaveData(), 3);
            Assert.That(data.DefeatedEncounterState, Is.Empty);
        }

        [Test]
        public void Test_Save_ClearedEncounterStaysCleared()
        {
            var input = new SaveData();
            input.DefeatedEncounterState.Add("Stage01_Encounter01");
            Assert.That(RoundTrip(input, 3).DefeatedEncounterState, Contains.Item("Stage01_Encounter01"));
        }

        [Test]
        public void Test_Save_GrabFailureUsesChaseCheckpoint()
        {
            var data = RoundTrip(new SaveData { CheckpointId = "Stage01_Chase" }, 3);
            Assert.That(data.CheckpointId, Is.EqualTo("Stage01_Chase"));
        }

        [Test]
        public void Test_Save_AssistSettingsSurviveNewGame()
        {
            var progress = new MemoryStore();
            var settings = new MemoryStore();
            var system = new SaveSystem(progress, settings);
            system.Save(new SaveData { AssistSettings = new AssistSettings { ChaseSpeedAssist = true } }, 3);
            system.DeleteProgress();
            var restarted = new SaveSystem(progress, settings);
            restarted.Load(3);
            Assert.That(restarted.CreateNewGame(3).AssistSettings.ChaseSpeedAssist, Is.True);
        }

        [Test]
        public void Test_Save_StoresStableIdsOnly()
        {
            var store = new MemoryStore();
            new SaveSystem(store).Save(new SaveData { PlayerPosition = Vector2.one }, 3);
            Assert.That(store.Json, Does.Not.Contain("instanceID").And.Not.Contain("fileID").And.Not.Contain("scenePath"));
        }

        [Test]
        public void Test_Save_CorruptDataReturnsExplicitRecovery()
        {
            var result = new SaveSystem(new MemoryStore("not-json")).Load(3);
            Assert.That(result.Status, Is.EqualTo(SaveLoadStatus.RecoveredCorrupt));
        }

        private static SaveData RoundTrip(SaveData data, int maxHealth)
        {
            var store = new MemoryStore();
            var system = new SaveSystem(store);
            system.Save(data, maxHealth);
            return system.Load(maxHealth).Data;
        }

        private sealed class MemoryStore : ISaveStore
        {
            public MemoryStore(string json = null) => Json = json;
            public bool Exists => Json != null;
            public string Json { get; private set; }
            public string Read() => Json;
            public void Write(string json) => Json = json;
            public void Delete() => Json = null;
        }
    }
}
