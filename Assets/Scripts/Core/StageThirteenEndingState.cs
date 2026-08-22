using System;
using UnityEngine;

namespace Daeume.Core
{
    /// <summary>도주 루프가 보여 줄 힌트의 의미다. 실제 문구는 Stage 13 콘텐츠가 소유하며 방향을 직접 지시하지 않는다.</summary>
    public enum StageThirteenHint
    {
        None = 0,
        EmptyPathFraming = 1,
        DirectionalMusic = 2,
        InnerMonologue = 3,
        TraumaWaits = 4
    }

    /// <summary>
    /// Stage 13의 수용 엔딩 규칙을 보관하는 순수 상태 기계다.
    /// 씬/입력/연출은 이 값을 소비할 뿐, 도주 횟수·무기 상태·완료 조건을 각자 계산하지 않는다.
    /// </summary>
    public sealed class StageThirteenEndingState
    {
        public const int StageId = 13;
        public const int HintStageCount = 4;

        public int LoopCount { get; private set; }
        public bool WeaponLowered { get; private set; }
        public bool EndingCompleted { get; private set; }
        public int HintStage => Math.Min(HintStageCount, Math.Max(0, LoopCount));
        public bool TraumaWaiting => LoopCount >= HintStageCount;
        public bool CombatAllowed => false;
        public bool TraumaContactFailsStage => false;
        public bool HasEscapeExit => false;
        public bool EnemiesAreNonHostile => true;
        public int FinalEnemyCount => 0;
        public StageThirteenHint Hint => (StageThirteenHint)HintStage;

        /// <summary>저장된 진행 상태를 복원한다. 음수 루프는 손상 데이터이므로 0으로 보정한다.</summary>
        public void Restore(int loopCount, bool weaponLowered, bool endingCompleted)
        {
            LoopCount = Math.Max(0, loopCount);
            WeaponLowered = weaponLowered;
            EndingCompleted = endingCompleted;
        }

        /// <summary>도주 루프를 하나 기록하고, 이번에 표시할 힌트 단계를 돌려준다.</summary>
        public int RegisterRunawayLoop()
        {
            if (!EndingCompleted)
            {
                LoopCount++;
            }

            return HintStage;
        }

        /// <summary>
        /// 플레이어와 트라우마의 거리에 따른 압박 역전 단계(0=Collapse, 4=Stable)를 계산한다.
        /// 가까워질수록 수치가 커져 음악·속도·카메라 연출을 한 단계씩 감산할 수 있다.
        /// </summary>
        public int ResolvePressureReversal(float distance, float acceptanceDistance)
        {
            if (acceptanceDistance <= 0f) throw new ArgumentOutOfRangeException(nameof(acceptanceDistance));
            var progress = 1f - Mathf.Clamp01(distance / acceptanceDistance);
            return Mathf.Clamp((int)Math.Ceiling(progress * HintStageCount), 0, HintStageCount);
        }

        /// <summary>수용 거리 안에서만 무기를 내려놓을 수 있다.</summary>
        public bool TryLowerWeapon(float distance, float acceptanceDistance)
        {
            if (EndingCompleted || distance > acceptanceDistance || acceptanceDistance <= 0f)
            {
                return false;
            }

            WeaponLowered = true;
            return true;
        }

        /// <summary>작별 대사와 버스 탑승이 끝난 뒤에만 엔딩 저장을 허용한다.</summary>
        public bool CompleteAfterFarewell(bool farewellPlayed, bool boardedBus)
        {
            if (EndingCompleted || !WeaponLowered || !farewellPlayed || !boardedBus)
            {
                return false;
            }

            EndingCompleted = true;
            return true;
        }
    }
}
