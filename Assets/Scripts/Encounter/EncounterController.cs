using System.Collections.Generic;
using Daeume.Core;
using Daeume.Enemy;
using UnityEngine;

namespace Daeume.Encounter
{
    /// <summary>
    /// 전투 구간 하나를 관리한다: 진입 감지 → 출구 잠금 → Wave 스폰 → 전멸 확인 → 해제. (spec-012)
    ///
    /// 스폰 수·Wave 수·출구 잠금 여부는 모두 EncounterData(에셋)가 선언한다.
    /// 코드에는 "규칙"만 있고 "수치"는 없다 — 기획이 코드 없이 전투를 조정할 수 있게 하는 구조다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class EncounterController : MonoBehaviour
    {
        [SerializeField] private EncounterData data;
        [SerializeField] private MeleeRemnant enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private EncounterExitLock exitLock;

        // 지금 살아 있는 적 목록. 이 목록이 비면 Wave가 끝난 것으로 판단한다.
        private readonly List<MeleeRemnant> activeEnemies = new();

        public EncounterState State { get; private set; } = EncounterState.Inactive;
        public int CurrentWaveNumber { get; private set; }
        public int TotalSpawnCount { get; private set; }
        public IReadOnlyList<MeleeRemnant> ActiveEnemies => activeEnemies;
        public EncounterData Data => data;

        private void Awake()
        {
            // 진입 감지는 "통과 가능한 감지 영역"이어야 한다. 실수로 벽이 되지 않도록 코드로 강제한다.
            var trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            exitLock?.SetLocked(false);
        }

        private void OnDestroy()
        {
            // 이벤트 구독(Died)을 풀지 않고 파괴되면, 이미 사라진 이 객체의 함수가 호출돼 오류가 난다.
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null) enemy.Died -= HandleEnemyDied;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // "플레이어인가"를 이름이나 태그가 아니라 IDamageable의 종류로 판단한다.
            // 이름 규칙이 바뀌어도 깨지지 않는 방식이라 더 안전하다.
            var damageable = FindDamageable(other.transform);
            if (damageable?.TargetKind == DamageTargetKind.Player) TryActivate();
        }

        /// <summary>인스펙터 대신 코드로 구성한다(테스트나 런타임 배치용).</summary>
        public void Configure(EncounterData encounterData, MeleeRemnant prefab, Transform[] points, EncounterExitLock encounterExitLock)
        {
            data = encounterData;
            enemyPrefab = prefab;
            spawnPoints = points;
            exitLock = encounterExitLock;
            exitLock?.SetLocked(false);
        }

        /// <summary>전투를 시작한다. 이미 진행 중이거나 완료됐으면 아무 일도 하지 않는다.</summary>
        public bool TryActivate()
        {
            // State != Inactive 조건이 spec-012의 "완료 Encounter 재진입은 Spawn 0회"를 보장한다.
            // 전투를 끝낸 구간을 되돌아 지나갈 때 적이 다시 나오면 진행이 무의미해진다.
            if (State != EncounterState.Inactive || data == null || enemyPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            {
                return false;
            }

            State = EncounterState.Active;
            if (data.LockExit) exitLock?.SetLocked(true);
            PublishState();
            StartWave(1);
            return true;
        }

        private void StartWave(int waveNumber)
        {
            CurrentWaveNumber = waveNumber;
            for (var i = 0; i < data.SpawnCount; i++)
            {
                // 스폰 지점을 번갈아 사용한다. TotalSpawnCount를 더해 나머지 연산을 하므로
                // 두 번째 Wave는 첫 Wave와 다른 지점에서 시작해 단조로움을 줄인다.
                var point = spawnPoints[(TotalSpawnCount + i) % spawnPoints.Length];
                var enemy = Instantiate(enemyPrefab, point.position, point.rotation, transform);
                enemy.name = $"{enemyPrefab.name}_Wave{waveNumber}_{i + 1}";

                // 적이 죽으면 알림을 받도록 구독한다. 이 신호로 Wave 종료를 판단한다.
                enemy.Died += HandleEnemyDied;
                activeEnemies.Add(enemy);
            }

            TotalSpawnCount += data.SpawnCount;
            GameManager.Instance?.Events.Publish(new EncounterWaveStarted(data.EncounterId, waveNumber, data.SpawnCount));
            PublishState();
        }

        private void HandleEnemyDied(MeleeRemnant enemy)
        {
            // 구독을 먼저 해제한다. 같은 적이 두 번 죽음을 알리는 경우를 원천 차단한다.
            enemy.Died -= HandleEnemyDied;
            activeEnemies.Remove(enemy);

            if (State != EncounterState.Active || activeEnemies.Count > 0) return;

            if (CurrentWaveNumber < data.WaveCount)
            {
                StartWave(CurrentWaveNumber + 1);
                return;
            }

            State = EncounterState.Cleared;
            exitLock?.SetLocked(false);
            PublishState();

            // 검토 메모(미구현): spec-003은 Encounter가 Cleared되면 PlayerAggression을 초기화하라고 한다.
            // 지금은 초기화 호출부가 없다. Encounter 모듈이 Player 모듈을 참조하지 않기 때문인데,
            // 슬라이스에서는 Stage 11의 비선공 통과가 범위 밖이라 실피해는 없다.
            // 해결하려면 "EncounterCleared" 이벤트를 PlayerCombat이 구독해 스스로 초기화하는 방식이 적절하다.
        }

        private void PublishState()
        {
            GameManager.Instance?.Events.Publish(new EncounterStateChanged(data.EncounterId, State, CurrentWaveNumber));
        }

        private static IDamageable FindDamageable(Transform value)
        {
            if (value == null) return null;
            foreach (var behaviour in value.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IDamageable damageable) return damageable;
            }

            return null;
        }
    }
}
