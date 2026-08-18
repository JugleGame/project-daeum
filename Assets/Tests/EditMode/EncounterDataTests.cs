using Daeume.Encounter;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class EncounterDataTests
    {
        [Test]
        public void Test_EncounterData_Stage01ContractIsValid()
        {
            var data = ScriptableObject.CreateInstance<EncounterData>();
            data.Configure(
                "stage01.encounter.01",
                "stage01.encounter.01.trigger",
                new Vector2(2f, 3f),
                EncounterEnemyType.MeleeRemnant,
                new[] { "stage01.remnant.spawn.01", "stage01.remnant.spawn.02" },
                1,
                2,
                EncounterClearCondition.DefeatAll,
                true,
                "stage01.encounter.01.exit",
                new[] { "hazard-stage01-warning-pulse" });

            Assert.That(data.ValidateData(out var error), Is.True, error);
            Assert.That(data.WaveCount, Is.EqualTo(2));
            Assert.That(data.SpawnMarkerIds, Has.Count.EqualTo(2));
            Assert.That(data.TerrainHazardIds, Has.Count.EqualTo(1));
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Test_EncounterData_RejectsUnsupportedClearCondition()
        {
            var data = ScriptableObject.CreateInstance<EncounterData>();
            data.Configure(
                "stage01.encounter.01", "trigger", Vector2.one, EncounterEnemyType.MeleeRemnant,
                new[] { "spawn" }, 1, 1, EncounterClearCondition.Survive, false, string.Empty, null);

            Assert.That(data.ValidateData(out var error), Is.False);
            StringAssert.Contains("DefeatAll", error);
            Object.DestroyImmediate(data);
        }
    }
}
