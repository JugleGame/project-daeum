using Daeume.Contamination;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public readonly struct ContaminationPressureChanged
    {
        public ContaminationPressureChanged(string variantId, PressureStage pressure, string overlayScene)
        {
            VariantId = variantId ?? string.Empty;
            Pressure = pressure;
            OverlayScene = overlayScene ?? string.Empty;
        }

        public string VariantId { get; }
        public PressureStage Pressure { get; }
        public string OverlayScene { get; }
    }

    public readonly struct ChaseStateChanged
    {
        public ChaseStateChanged(string chaseId, bool active, float elapsedSeconds, float targetSeconds)
        {
            ChaseId = chaseId ?? string.Empty;
            Active = active;
            ElapsedSeconds = elapsedSeconds;
            TargetSeconds = targetSeconds;
        }

        public string ChaseId { get; }
        public bool Active { get; }
        public float ElapsedSeconds { get; }
        public float TargetSeconds { get; }
    }

    public readonly struct ChaseDirectiveIssued
    {
        public ChaseDirectiveIssued(string chaseId, Vector2 playerPosition, Vector2 pursuerPosition, float distance, float minDistance, float maxDistance, float speed, float remainingSeconds)
        {
            ChaseId = chaseId ?? string.Empty;
            PlayerPosition = playerPosition;
            PursuerPosition = pursuerPosition;
            Distance = distance;
            MinDistance = minDistance;
            MaxDistance = maxDistance;
            Speed = speed;
            RemainingSeconds = remainingSeconds;
        }

        public string ChaseId { get; }
        public Vector2 PlayerPosition { get; }
        public Vector2 PursuerPosition { get; }
        public float Distance { get; }
        public float MinDistance { get; }
        public float MaxDistance { get; }
        public float Speed { get; }
        public float RemainingSeconds { get; }
    }
}
