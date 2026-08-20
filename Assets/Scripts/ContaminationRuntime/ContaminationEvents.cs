using Daeume.Contamination;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 압박 단계가 바뀌었음을 알린다. 오디오·카메라 연출(C)이 이 값을 읽어 강도를 맞춘다.
    /// C는 거리 계산을 직접 하지 않고 이 신호와 ChaseDirectiveIssued만 소비한다(spec-014).
    /// </summary>
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

    /// <summary>추격 시작·종료와 진행 시간을 알린다. HUD의 "도망치세요" 표시가 이 신호를 쓴다.</summary>
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

    /// <summary>
    /// 감독이 추격자에게 내리는 지시. 매 프레임 발행된다.
    ///
    /// 추격자는 이 값만 보고 움직이며 스스로 판단하지 않는다(spec-006의 "director가 소유" 규칙).
    /// 거리(Distance)는 카메라 압박·발걸음 소리 프리셋의 유일한 출처이기도 하다.
    /// </summary>
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
