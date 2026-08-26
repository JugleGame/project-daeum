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

        // 복귀 직후 "플레이어가 이미 구간 안에 서 있는가"를 다시 볼지 표시한다.
        // 이벤트 시점에는 PlayerController가 아직 안 옮겨졌을 수 있어 한 물리 프레임 미룬다.
        private bool recheckPlayerInsideQueued;
        private Collider2D triggerCollider;
        private readonly List<Collider2D> overlapBuffer = new();

        private void Awake()
        {
            // 진입 감지는 "통과 가능한 감지 영역"이어야 한다. 실수로 벽이 되지 않도록 코드로 강제한다.
            triggerCollider = GetComponent<Collider2D>();
            triggerCollider.isTrigger = true;
            exitLock?.SetLocked(false);
        }

        private void OnEnable()
        {
            GameManager.Instance?.Events.Subscribe<PlayerRestoreRequested>(HandlePlayerRestore);
        }

        private void OnDisable()
        {
            GameManager.Instance?.Events.Unsubscribe<PlayerRestoreRequested>(HandlePlayerRestore);
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

            // 스폰 지점 참조가 전부 끊겨 있으면 시작하지 않는다.
            // 그냥 진행하면 State는 Active가 되고 출구는 잠기는데 적은 하나도 안 나와서 길이 막힌다.
            if (!HasUsableSpawnPoint())
            {
                Debug.LogWarning(
                    $"[{nameof(EncounterController)}] '{data.EncounterId}'의 스폰 지점이 모두 비어 있어 전투를 시작하지 않는다. "
                    + $"{name}의 Spawn Points에 마커를 다시 연결해야 한다.", this);
                return false;
            }

            State = EncounterState.Active;
            if (data.LockExit) exitLock?.SetLocked(true);
            PublishState();
            StartWave(1);
            return true;
        }

        /// <summary>
        /// 전투 구간을 미진입 상태로 되돌린다. 살아남은 적을 지우고 Wave 진행과 출구 잠금도 푼다.
        /// </summary>
        /// <remarks>
        /// 세이브 포인트 복귀는 씬을 다시 올리지 않는 경로가 있다(낙사 복귀 등). 그때 적을 그대로 두면
        /// 플레이어만 뒤로 돌아가고 적은 쫓아오던 자리에 남아, 복귀 지점이 이미 포위된 상태가 된다.
        /// 남은 적을 지우고 Inactive로 되돌려야 다시 진입할 때 스폰 지점에서 처음부터 나온다.
        ///
        /// 사망 후 재시도처럼 씬을 통째로 다시 올리는 경로에서는 이 구간이 이미 새것이라 아무 일도 하지 않는다.
        /// </remarks>
        public void ResetToInactive()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy == null) continue;

                // 구독을 먼저 끊는다. Destroy가 Died를 발행하면 이미 비운 목록을 다시 건드린다.
                enemy.Died -= HandleEnemyDied;
                Destroy(enemy.gameObject);
            }

            activeEnemies.Clear();
            State = EncounterState.Inactive;
            CurrentWaveNumber = 0;
            TotalSpawnCount = 0;
            exitLock?.SetLocked(false);
            PublishState();
        }

        private void HandlePlayerRestore(PlayerRestoreRequested _)
        {
            ResetToInactive();

            // 복귀 지점이 이 구간 안일 수 있다. 그때는 OnTriggerEnter2D가 다시 오지 않으므로
            // (이미 겹친 채로 순간이동해 들어오는 경우가 있다) 직접 확인해서 다시 시작한다.
            recheckPlayerInsideQueued = true;
        }

        private void FixedUpdate()
        {
            if (!recheckPlayerInsideQueued) return;

            recheckPlayerInsideQueued = false;
            if (State == EncounterState.Inactive && PlayerIsInsideTrigger()) TryActivate();
        }

        private bool PlayerIsInsideTrigger()
        {
            if (triggerCollider == null) return false;

            var filter = new ContactFilter2D();
            filter.NoFilter();
            filter.useTriggers = true;   // NoFilter가 useTriggers를 꺼 버리므로 뒤에서 다시 켠다.

            overlapBuffer.Clear();
            triggerCollider.Overlap(filter, overlapBuffer);
            foreach (var other in overlapBuffer)
            {
                if (FindDamageable(other.transform)?.TargetKind == DamageTargetKind.Player) return true;
            }

            return false;
        }

        private void StartWave(int waveNumber)
        {
            CurrentWaveNumber = waveNumber;
            for (var i = 0; i < data.SpawnCount; i++)
            {
                // 스폰 지점을 번갈아 사용한다. TotalSpawnCount를 더해 나머지 연산을 하므로
                // 두 번째 Wave는 첫 Wave와 다른 지점에서 시작해 단조로움을 줄인다.
                // 참조가 끊긴 칸은 건너뛴다. 그냥 쓰면 UnityEngine.Transform.position에서 예외가 나
                // Wave 도중에 스폰이 끊기고 출구가 잠긴 채로 남는다.
                var point = ResolveSpawnPoint(TotalSpawnCount + i);
                if (point == null) continue;

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

            // spec-003: PlayerCombat이 이 신호를 구독해 스스로 선공 여부를 초기화한다.
            // EncounterStateChanged가 아니라 별도 이벤트인 이유는 Daeume.Core.EncounterCleared 문서 참고(asmdef 순환 참조 회피).
            GameManager.Instance?.Events.Publish(new EncounterCleared(data.EncounterId));
        }

        private bool HasUsableSpawnPoint()
        {
            foreach (var point in spawnPoints)
            {
                if (point != null) return true;
            }

            return false;
        }

        /// <summary>index번째 스폰 지점을 고르되, 비어 있으면 뒤쪽으로 한 바퀴 돌며 살아 있는 칸을 찾는다.</summary>
        private Transform ResolveSpawnPoint(int index)
        {
            for (var offset = 0; offset < spawnPoints.Length; offset++)
            {
                var point = spawnPoints[(index + offset) % spawnPoints.Length];
                if (point != null) return point;
            }

            return null;
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
