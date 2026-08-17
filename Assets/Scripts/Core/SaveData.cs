using System;
using System.Collections.Generic;
using UnityEngine;

namespace Daeume.Core
{
    [Serializable]
    public sealed class AssistSettings
    {
        public float CameraShakeStrength = 1f;
        public int SubtitleSize = 1;
        public bool ChaseSpeedAssist;
        public string BindingOverridesJson = string.Empty;
    }

    [Serializable]
    public sealed class SaveData
    {
        public int SchemaVersion = 1;
        public int CurrentStageId = 1;
        public string CheckpointId = string.Empty;
        public Vector2 PlayerPosition;
        public int PlayerHealth = 1;
        public List<string> CompletedMemoryAnchors = new();
        public List<string> CollectedMemoryFragments = new();
        public List<string> DefeatedEncounterState = new();
        public List<string> NarrativeRevealState = new();
        public bool EndingCompleted;
        public string ContaminationVariantId = string.Empty;
        public string PressureStage = "Stable";
        public int StageThirteenLoopCount;
        public bool WeaponLowered;
        public AssistSettings AssistSettings = new();
    }
}
