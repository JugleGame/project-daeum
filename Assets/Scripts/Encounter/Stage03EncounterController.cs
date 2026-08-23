using System.Collections.Generic;
using Daeume.Core;
using Daeume.Enemy;
using UnityEngine;

namespace Daeume.Encounter
{
    /// <summary>
    /// Stage 03 전용 혼합 조우 컨트롤러. 기존 EncounterController를 바꾸지 않고
    /// 처음 도입되는 DashRemnant와 MeleeRemnant를 한 출구 잠금 아래에서 관리한다(Issue #13).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Stage03EncounterController : MonoBehaviour
    {
        [SerializeField] private EncounterData data;
        [SerializeField] private MeleeRemnant meleePrefab;
        [SerializeField] private DashRemnant dashPrefab;
        [SerializeField] private DashRemnantData dashData;
        [SerializeField] private Transform[] meleeSpawnPoints;
        [SerializeField] private Transform[] dashSpawnPoints;
        [SerializeField] private EncounterExitLock exitLock;

        private readonly List<MeleeRemnant> activeMeleeEnemies = new();
        private readonly List<DashRemnant> activeDashEnemies = new();

        public EncounterData Data => data;
        public EncounterExitLock ExitLock => exitLock;
        public IReadOnlyList<Transform> MeleeSpawnPoints => meleeSpawnPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<Transform> DashSpawnPoints => dashSpawnPoints ?? System.Array.Empty<Transform>();
        public IReadOnlyList<MeleeRemnant> ActiveMeleeEnemies => activeMeleeEnemies;
        public IReadOnlyList<DashRemnant> ActiveDashEnemies => activeDashEnemies;
        public EncounterState State { get; private set; } = EncounterState.Inactive;
        public int CurrentWaveNumber { get; private set; }
        public int TotalSpawnCount { get; private set; }

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            exitLock?.SetLocked(false);
        }

        private void OnDestroy()
        {
            foreach (var enemy in activeMeleeEnemies)
            {
                if (enemy != null) enemy.Died -= HandleMeleeDied;
            }

            foreach (var enemy in activeDashEnemies)
            {
                if (enemy != null) enemy.Died -= HandleDashDied;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var damageable = FindDamageable(other.transform);
            if (damageable?.TargetKind == DamageTargetKind.Player) TryActivate();
        }

        public void Configure(
            EncounterData encounterData,
            MeleeRemnant melee,
            DashRemnant dash,
            DashRemnantData authoredDashData,
            Transform[] meleePoints,
            Transform[] dashPoints,
            EncounterExitLock encounterExitLock)
        {
            data = encounterData;
            meleePrefab = melee;
            dashPrefab = dash;
            dashData = authoredDashData;
            meleeSpawnPoints = meleePoints ?? System.Array.Empty<Transform>();
            dashSpawnPoints = dashPoints ?? System.Array.Empty<Transform>();
            exitLock = encounterExitLock;
            exitLock?.SetLocked(false);
        }

        public bool TryActivate()
        {
            if (State != EncounterState.Inactive || data == null ||
                (MeleeSpawnPoints.Count == 0 && DashSpawnPoints.Count == 0) ||
                (MeleeSpawnPoints.Count > 0 && meleePrefab == null) ||
                (DashSpawnPoints.Count > 0 && (dashPrefab == null || dashData == null)))
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
            SpawnMelee(waveNumber);
            SpawnDash(waveNumber);
            var waveSpawnCount = MeleeSpawnPoints.Count + DashSpawnPoints.Count;
            TotalSpawnCount += waveSpawnCount;
            GameManager.Instance?.Events.Publish(new EncounterWaveStarted(data.EncounterId, waveNumber, waveSpawnCount));
            PublishState();
        }

        private void SpawnMelee(int waveNumber)
        {
            for (var index = 0; index < MeleeSpawnPoints.Count; index++)
            {
                var point = MeleeSpawnPoints[index];
                var enemy = Instantiate(meleePrefab, point.position, point.rotation, transform);
                enemy.name = $"{meleePrefab.name}_Wave{waveNumber}_{index + 1}";
                enemy.Died += HandleMeleeDied;
                activeMeleeEnemies.Add(enemy);
            }
        }

        private void SpawnDash(int waveNumber)
        {
            for (var index = 0; index < DashSpawnPoints.Count; index++)
            {
                var point = DashSpawnPoints[index];
                var enemy = Instantiate(dashPrefab, point.position, point.rotation, transform);
                enemy.name = $"{dashPrefab.name}_Wave{waveNumber}_{index + 1}";
                enemy.SetData(dashData);
                enemy.Died += HandleDashDied;
                activeDashEnemies.Add(enemy);
            }
        }

        private void HandleMeleeDied(MeleeRemnant enemy)
        {
            enemy.Died -= HandleMeleeDied;
            activeMeleeEnemies.Remove(enemy);
            TryFinishWave();
        }

        private void HandleDashDied(DashRemnant enemy)
        {
            enemy.Died -= HandleDashDied;
            activeDashEnemies.Remove(enemy);
            TryFinishWave();
        }

        private void TryFinishWave()
        {
            if (State != EncounterState.Active || activeMeleeEnemies.Count > 0 || activeDashEnemies.Count > 0) return;
            if (CurrentWaveNumber < data.WaveCount)
            {
                StartWave(CurrentWaveNumber + 1);
                return;
            }

            State = EncounterState.Cleared;
            exitLock?.SetLocked(false);
            PublishState();
            GameManager.Instance?.Events.Publish(new EncounterCleared(data.EncounterId));
        }

        private void PublishState() => GameManager.Instance?.Events.Publish(
            new EncounterStateChanged(data.EncounterId, State, CurrentWaveNumber));

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
