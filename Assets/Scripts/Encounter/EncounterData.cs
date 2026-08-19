using System.Collections.Generic;
using UnityEngine;

namespace Daeume.Encounter
{
    public enum EncounterEnemyType
    {
        MeleeRemnant
    }

    public enum EncounterClearCondition
    {
        DefeatAll,
        Survive,
        PassWithoutAggression,
        OptionalReactive
    }

    [CreateAssetMenu(fileName = "EncounterData", menuName = "Daeume/Encounter Data")]
    public sealed class EncounterData : ScriptableObject
    {
        [SerializeField] private string encounterId = "stage01.encounter.01";
        [SerializeField] private string triggerMarkerId = "stage01.encounter.01.trigger";
        [SerializeField] private Vector2 triggerAreaSize = new(2f, 3f);
        [SerializeField] private EncounterEnemyType enemyType = EncounterEnemyType.MeleeRemnant;
        [SerializeField] private List<string> spawnMarkerIds = new();
        [SerializeField, Min(1)] private int spawnCount = 1;
        [SerializeField, Min(1)] private int waveCount = 1;
        [SerializeField] private EncounterClearCondition clearCondition = EncounterClearCondition.DefeatAll;
        [SerializeField] private bool lockExit = true;
        [SerializeField] private string exitMarkerId = "stage01.encounter.01.exit";
        [SerializeField] private List<string> terrainHazardIds = new();

        public string EncounterId => encounterId;
        public string TriggerMarkerId => triggerMarkerId;
        public Vector2 TriggerAreaSize => triggerAreaSize;
        public EncounterEnemyType EnemyType => enemyType;
        public IReadOnlyList<string> SpawnMarkerIds => spawnMarkerIds;
        public int SpawnCount => Mathf.Max(1, spawnCount);
        public int WaveCount => Mathf.Max(1, waveCount);
        public EncounterClearCondition ClearCondition => clearCondition;
        public bool LockExit => lockExit;
        public string ExitMarkerId => exitMarkerId;
        public IReadOnlyList<string> TerrainHazardIds => terrainHazardIds;

        public void Configure(
            string id,
            string triggerId,
            Vector2 areaSize,
            EncounterEnemyType type,
            IEnumerable<string> spawnIds,
            int enemiesPerWave,
            int waves,
            EncounterClearCondition condition,
            bool shouldLockExit,
            string exitId,
            IEnumerable<string> hazardIds)
        {
            encounterId = id ?? string.Empty;
            triggerMarkerId = triggerId ?? string.Empty;
            triggerAreaSize = areaSize;
            enemyType = type;
            spawnMarkerIds = spawnIds == null ? new List<string>() : new List<string>(spawnIds);
            spawnCount = Mathf.Max(1, enemiesPerWave);
            waveCount = Mathf.Max(1, waves);
            clearCondition = condition;
            lockExit = shouldLockExit;
            exitMarkerId = exitId ?? string.Empty;
            terrainHazardIds = hazardIds == null ? new List<string>() : new List<string>(hazardIds);
        }

        public bool ValidateData(out string error)
        {
            if (string.IsNullOrWhiteSpace(encounterId)) return Fail("EncounterId is required.", out error);
            if (string.IsNullOrWhiteSpace(triggerMarkerId)) return Fail("TriggerArea marker is required.", out error);
            if (spawnMarkerIds == null || spawnMarkerIds.Count == 0) return Fail("At least one SpawnPoint is required.", out error);
            if (clearCondition != EncounterClearCondition.DefeatAll) return Fail("Stage 1 supports DefeatAll only.", out error);
            if (lockExit && string.IsNullOrWhiteSpace(exitMarkerId)) return Fail("A locked encounter requires an exit marker.", out error);
            error = string.Empty;
            return true;
        }

        private static bool Fail(string message, out string error)
        {
            error = message;
            return false;
        }
    }
}
