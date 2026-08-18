using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Daeume.Core
{
    [Serializable]
    public sealed class AssistSettings
    {
        public float CameraShakeStrength = 1f;
        public int SubtitleSize = 1;
        public bool ChaseSpeedAssist;
        public string BindingOverridesJson = string.Empty;

        public AssistSettings Copy()
        {
            return new AssistSettings
            {
                CameraShakeStrength = CameraShakeStrength,
                SubtitleSize = SubtitleSize,
                ChaseSpeedAssist = ChaseSpeedAssist,
                BindingOverridesJson = BindingOverridesJson ?? string.Empty
            };
        }
    }

    [Serializable]
    public sealed class SaveData
    {
        public int SchemaVersion = 1;
        public int CurrentStageId = 1;
        public string CheckpointId = string.Empty;
        public Vector2 PlayerPosition;
        public int PlayerHealth = 1;
        [FormerlySerializedAs("OpenedMemoryChest")]
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

        public SaveData Copy()
        {
            return new SaveData
            {
                SchemaVersion = SchemaVersion,
                CurrentStageId = CurrentStageId,
                CheckpointId = CheckpointId ?? string.Empty,
                PlayerPosition = PlayerPosition,
                PlayerHealth = PlayerHealth,
                CompletedMemoryAnchors = new List<string>(CompletedMemoryAnchors ?? new List<string>()),
                CollectedMemoryFragments = new List<string>(CollectedMemoryFragments ?? new List<string>()),
                DefeatedEncounterState = new List<string>(DefeatedEncounterState ?? new List<string>()),
                NarrativeRevealState = new List<string>(NarrativeRevealState ?? new List<string>()),
                EndingCompleted = EndingCompleted,
                ContaminationVariantId = ContaminationVariantId ?? string.Empty,
                PressureStage = PressureStage ?? "Stable",
                StageThirteenLoopCount = StageThirteenLoopCount,
                WeaponLowered = WeaponLowered,
                AssistSettings = (AssistSettings ?? new AssistSettings()).Copy()
            };
        }
    }
}
