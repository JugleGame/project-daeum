namespace Daeume.Encounter
{
    public readonly struct EncounterStateChanged
    {
        public EncounterStateChanged(string encounterId, EncounterState state, int waveNumber)
        {
            EncounterId = encounterId ?? string.Empty;
            State = state;
            WaveNumber = waveNumber;
        }

        public string EncounterId { get; }
        public EncounterState State { get; }
        public int WaveNumber { get; }
    }

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
