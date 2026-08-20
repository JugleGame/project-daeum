namespace Daeume.Encounter
{
    /// <summary>
    /// Encounter 상태가 바뀔 때 발행한다.
    /// 오염 담당(B)은 이 신호로 전투 종료 후 압박 단계를 조정하고, UI(C)는 잠금 표시를 갱신한다.
    /// </summary>
    public readonly struct EncounterStateChanged
    {
        public EncounterStateChanged(string encounterId, EncounterState state, int waveNumber)
        {
            // 어느 Encounter인지 ID로 구분한다. 스테이지에 전투 구간이 여러 개 생겨도 구독자가 헷갈리지 않는다.
            EncounterId = encounterId ?? string.Empty;
            State = state;
            WaveNumber = waveNumber;
        }

        public string EncounterId { get; }
        public EncounterState State { get; }
        public int WaveNumber { get; }
    }

    /// <summary>새 Wave가 시작될 때 발행한다. 스폰 수까지 함께 알려 연출·오디오가 강도를 맞출 수 있다.</summary>
    public readonly struct EncounterWaveStarted
    {
        public EncounterWaveStarted(string encounterId, int waveNumber, int spawnCount)
        {
            EncounterId = encounterId ?? string.Empty;
            WaveNumber = waveNumber;
            SpawnCount = spawnCount;
        }

        public string EncounterId { get; }
        public int WaveNumber { get; }
        public int SpawnCount { get; }
    }
}
