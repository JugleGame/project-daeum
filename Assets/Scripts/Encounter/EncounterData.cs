using System.Collections.Generic;
using UnityEngine;

namespace Daeume.Encounter
{
    /// <summary>등장 적 유형. 8일 슬라이스에서는 근접형 1종만 만든다(spec-004 Build scope).</summary>
    public enum EncounterEnemyType
    {
        MeleeRemnant
    }

    /// <summary>
    /// 전투 완료 조건. (spec-012)
    /// 슬라이스에서는 DefeatAll만 구현하고 나머지는 데이터 형식만 미리 열어 둔다.
    /// 스키마를 지금 확정해 두면 Stage 2~13을 추가할 때 저장 형식을 바꾸지 않아도 된다.
    /// </summary>
    public enum EncounterClearCondition
    {
        DefeatAll,              // 전멸
        Survive,                // 버티기(미구현)
        PassWithoutAggression,  // 선공하지 않고 통과(미구현, Stage 11용)
        OptionalReactive        // 비선공 잔재 구간(미구현, Stage 11용)
    }

    /// <summary>
    /// 전투 구간 하나의 설정을 담는 데이터 에셋. (spec-012)
    ///
    /// 좌표를 직접 담지 않고 marker ID(문자열)로 위치를 가리키는 점이 중요하다.
    /// 레벨 담당이 지형을 옮겨도 데이터는 그대로 유효하다.
    /// </summary>
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

        /// <summary>
        /// 데이터가 이 빌드에서 실제로 동작 가능한지 검사한다.
        /// </summary>
        /// <remarks>
        /// clearCondition을 DefeatAll로 제한하는 검사가 들어 있다.
        /// 미구현 조건을 기획이 실수로 선택했을 때 "적을 다 죽여도 안 끝나는" 상태로 빠지는 대신
        /// EditMode 테스트에서 즉시 걸리게 하려는 의도다. 슬라이스 범위와 일치하며 적합하다.
        /// Stage 2~13에서 나머지 조건을 구현하면 이 검사를 함께 풀어야 한다.
        /// </remarks>
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
