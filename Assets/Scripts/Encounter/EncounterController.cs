using System.Collections.Generic;
using Daeume.Core;
using Daeume.Enemy;
using UnityEngine;

namespace Daeume.Encounter
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class EncounterController : MonoBehaviour
    {
        [SerializeField] private EncounterData data;
        [SerializeField] private MeleeRemnant enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private EncounterExitLock exitLock;

        private readonly List<MeleeRemnant> activeEnemies = new();

        public EncounterState State { get; private set; } = EncounterState.Inactive;
        public int CurrentWaveNumber { get; private set; }
        public int TotalSpawnCount { get; private set; }
        public IReadOnlyList<MeleeRemnant> ActiveEnemies => activeEnemies;
        public EncounterData Data => data;

        private void Awake()
        {
            var trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
            exitLock?.SetLocked(false);
        }

        private void OnDestroy()
        {
            foreach (var enemy in activeEnemies)
            {
                if (enemy != null) enemy.Died -= HandleEnemyDied;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var damageable = FindDamageable(other.transform);
            if (damageable?.TargetKind == DamageTargetKind.Player) TryActivate();
        }

        public void Configure(EncounterData encounterData, MeleeRemnant prefab, Transform[] points, EncounterExitLock encounterExitLock)
        {
            data = encounterData;
            enemyPrefab = prefab;
            spawnPoints = points;
            exitLock = encounterExitLock;
            exitLock?.SetLocked(false);
        }

        public bool TryActivate()
        {
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
                var point = spawnPoints[(TotalSpawnCount + i) % spawnPoints.Length];
                var enemy = Instantiate(enemyPrefab, point.position, point.rotation, transform);
                enemy.name = $"{enemyPrefab.name}_Wave{waveNumber}_{i + 1}";
                enemy.Died += HandleEnemyDied;
                activeEnemies.Add(enemy);
            }

            TotalSpawnCount += data.SpawnCount;
            GameManager.Instance?.Events.Publish(new EncounterWaveStarted(data.EncounterId, waveNumber, data.SpawnCount));
            PublishState();
        }

        private void HandleEnemyDied(MeleeRemnant enemy)
        {
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
