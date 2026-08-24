using System;
using Daeume.Contamination;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>잔재가 가질 수 있는 공통 상태 6종. (spec-004가 목록을 고정한다)</summary>
    public enum RemnantState
    {
        Idle,      // 대기
        Alert,     // 인지(플레이어를 발견하고 잠시 반응)
        Approach,  // 접근 또는 거리 유지
        Attack,    // 예고 후 공격
        Hit,       // 피격 경직
        Dead       // 소멸
    }

    /// <summary>잔재 archetype 3종. (spec-004) 압박 단계가 올라도 이 3종 자체는 늘지 않는다.</summary>
    public enum RemnantArchetype
    {
        Melee,
        Dash,
        Ranged
    }

    /// <summary>
    /// 외형이 암시하는 정도. 행동과 분리된 태그다(spec-004).
    /// Stage 7부터 HumanoidSilhouette를, Stage 11부터 protagonist_* 중 1개 이상을 요구한다.
    /// </summary>
    [Flags]
    public enum VisualTraitTag
    {
        None = 0,
        HumanoidSilhouette = 1 << 0,
        ProtagonistHand = 1 << 1,
        ProtagonistFace = 1 << 2,
        ProtagonistClothes = 1 << 3
    }

    /// <summary>
    /// 압박 단계 하나에서 잔재가 어떻게 달라지는지를 담은 수치 묶음. (spec-004)
    ///
    /// 핵심: 압박이 올라도 "새로운 적"이 생기지 않는다. 같은 적의 선언된 수치만 바뀐다.
    /// 그래서 이 값들이 코드 분기가 아니라 데이터로 존재한다. 3종 archetype이 공유하는 구조다.
    ///
    /// detectionRangeMultiplier/retreatDirectionBias는 Issue #9에서 추가한 필드다.
    /// 기존에 직렬화된 근접형 에셋에는 이 필드가 없어 역직렬화 시 0으로 채워지는데,
    /// getter가 0을 "값 없음"으로 보고 1(변화 없음)로 대체하므로 기존 데이터가 깨지지 않는다.
    /// </summary>
    [Serializable]
    public struct RemnantPressureProfile
    {
        [SerializeField] private PressureStage stage;
        [SerializeField, Min(0.01f)] private float moveSpeedMultiplier;
        [SerializeField, Min(0.01f)] private float telegraphMultiplier;
        [SerializeField] private bool watchesTrauma;
        [SerializeField] private float detectionRangeMultiplier;
        [SerializeField] private float retreatDirectionBias;

        public RemnantPressureProfile(
            PressureStage stage,
            float moveSpeedMultiplier,
            float telegraphMultiplier,
            bool watchesTrauma,
            float detectionRangeMultiplier = 1f,
            float retreatDirectionBias = 1f)
        {
            this.stage = stage;
            this.moveSpeedMultiplier = Mathf.Max(0.01f, moveSpeedMultiplier);
            this.telegraphMultiplier = Mathf.Max(0.01f, telegraphMultiplier);
            this.watchesTrauma = watchesTrauma;
            this.detectionRangeMultiplier = detectionRangeMultiplier;
            this.retreatDirectionBias = retreatDirectionBias;
        }

        public PressureStage Stage => stage;
        public float MoveSpeedMultiplier => Mathf.Max(0.01f, moveSpeedMultiplier);
        public float TelegraphMultiplier => Mathf.Max(0.01f, telegraphMultiplier);
        public bool WatchesTrauma => watchesTrauma;

        // 0은 "선언 안 됨"이다. 옛 데이터와의 호환을 위해 0이면 "변화 없음"(1배)으로 본다.
        public float DetectionRangeMultiplier => Mathf.Approximately(detectionRangeMultiplier, 0f) ? 1f : detectionRangeMultiplier;

        // 원거리형 후퇴 방향 부호. 0이면 "변화 없음"(평소처럼 플레이어 반대쪽으로 후퇴)으로 본다.
        // 양수: 플레이어 반대쪽으로 후퇴(평소). 음수: 트라우마 쪽으로 후퇴(고압박에서 끌려가는 연출).
        public float RetreatDirectionBias => Mathf.Approximately(retreatDirectionBias, 0f) ? 1f : retreatDirectionBias;
    }
}
