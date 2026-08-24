using System;
using System.Collections.Generic;
using System.Linq;
using Daeume.Contamination;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 3종 archetype(근접/돌진/원거리)이 공유하는 잔재 데이터 기반 클래스. (spec-004, Issue #9)
    ///
    /// 체력·피해·사거리 같은 기본값, 압박 단계별 변화(pressureProfiles), 외형 태그(VisualTraitTags),
    /// Stage 9 모방 행동, Stage 11 Reactive 행동, Stage 7 이후 소멸 흔적 비율을 함께 선언한다.
    /// archetype별 고유 수치(돌진 거리, 원거리 후퇴 트리거 등)는 하위 클래스가 추가한다.
    /// </summary>
    public abstract class RemnantDataBase : ScriptableObject
    {
        [SerializeField] private string enemyId = "remnant";
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(1)] private int contactDamage = 1;
        [SerializeField, Min(0.1f)] private float detectionRange = 5f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.1f;
        [SerializeField, Min(0.01f)] private float moveSpeed = 1.8f;
        [SerializeField, Min(0.01f)] private float alertSeconds = 0.2f;
        [SerializeField, Min(0.05f)] private float attackTelegraphSeconds = 0.55f;
        [SerializeField, Min(0.01f)] private float attackRecoverySeconds = 0.7f;
        [SerializeField, Min(0.01f)] private float hitStunSeconds = 0.18f;

        [Header("Fragment identity (spec-004)")]
        [SerializeField, Range(1, 13)] private int stageNumber = 1;
        [SerializeField] private VisualTraitTag visualTraitTags = VisualTraitTag.None;
        [SerializeField] private bool mimicsPlayerMotion;
        [SerializeField] private bool reactive;
        [SerializeField, Range(0f, 1f)] private float fragmentTraceTowardTraumaRatio;

        [SerializeField] private List<RemnantPressureProfile> pressureProfiles = new()
        {
            new RemnantPressureProfile(PressureStage.Stable, 1f, 1f, false),
            new RemnantPressureProfile(PressureStage.Echo, 1.08f, 0.85f, true),
            new RemnantPressureProfile(PressureStage.Intrusion, 1.16f, 0.7f, true)
        };

        public abstract RemnantArchetype Archetype { get; }

        public string EnemyId => enemyId;
        public int MaxHealth => maxHealth;
        public int ContactDamage => contactDamage;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float MoveSpeed => moveSpeed;
        public float AlertSeconds => alertSeconds;
        public float AttackTelegraphSeconds => attackTelegraphSeconds;
        public float AttackRecoverySeconds => attackRecoverySeconds;
        public float HitStunSeconds => hitStunSeconds;
        public int StageNumber => stageNumber;
        public VisualTraitTag VisualTraitTags => visualTraitTags;
        public bool MimicsPlayerMotion => mimicsPlayerMotion;
        public bool Reactive => reactive;
        public float FragmentTraceTowardTraumaRatio => fragmentTraceTowardTraumaRatio;
        public IReadOnlyList<RemnantPressureProfile> PressureProfiles => pressureProfiles;

        /// <summary>테스트나 툴에서 스테이지 번호와 외형 태그를 한 번에 채워 넣는다.</summary>
        public void SetStageIdentity(int stage, VisualTraitTag tags)
        {
            stageNumber = Mathf.Clamp(stage, 1, 13);
            visualTraitTags = tags;
        }

        /// <summary>테스트나 툴에서 Stage 9 모방/Stage 11 Reactive 플래그를 채워 넣는다.</summary>
        public void SetBehaviorFlags(bool mimicsMotion, bool reactiveFlag)
        {
            mimicsPlayerMotion = mimicsMotion;
            reactive = reactiveFlag;
        }

        /// <summary>테스트나 툴에서 소멸 흔적 비율을 채워 넣는다.</summary>
        public void SetFragmentTraceRatio(float ratio) => fragmentTraceTowardTraumaRatio = Mathf.Clamp01(ratio);

        /// <summary>해당 압박 단계의 프로필을 찾는다. 선언돼 있지 않으면 "변화 없음(Stable 기본값)"을 돌려준다.</summary>
        public RemnantPressureProfile GetProfile(PressureStage stage)
        {
            if (pressureProfiles != null)
            {
                for (var index = 0; index < pressureProfiles.Count; index++)
                {
                    if (pressureProfiles[index].Stage == stage)
                    {
                        return pressureProfiles[index];
                    }
                }
            }

            return new RemnantPressureProfile(PressureStage.Stable, 1f, 1f, false);
        }

        /// <summary>데이터 오류 목록을 돌려준다(비어 있으면 정상). 하위 클래스는 이 목록에 항목을 더한다.</summary>
        public virtual IReadOnlyList<string> ValidateData()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(enemyId)) errors.Add("EnemyId is required.");
            if (maxHealth < 1) errors.Add("MaxHealth must be at least 1.");
            if (contactDamage < 1) errors.Add("ContactDamage must be at least 1.");
            if (detectionRange <= attackRange) errors.Add("DetectionRange must be greater than AttackRange.");
            if (attackTelegraphSeconds < 0.05f) errors.Add("AttackTelegraphSeconds must preserve a visible warning.");

            var profiles = pressureProfiles ?? new List<RemnantPressureProfile>();
            if (profiles.Select(profile => profile.Stage).Distinct().Count() != profiles.Count)
            {
                errors.Add("PressureProfiles contains duplicate stages.");
            }

            foreach (var required in new[] { PressureStage.Stable, PressureStage.Echo, PressureStage.Intrusion })
            {
                if (!profiles.Any(profile => profile.Stage == required))
                {
                    errors.Add($"PressureProfiles is missing {required}.");
                }
            }

            // 압박 단계가 예고 시간을 줄일 수는 있어도 0으로 만들 수는 없다(spec-004 공정성 규칙).
            if (GetProfile(PressureStage.Echo).TelegraphMultiplier * attackTelegraphSeconds < 0.05f ||
                GetProfile(PressureStage.Intrusion).TelegraphMultiplier * attackTelegraphSeconds < 0.05f)
            {
                errors.Add("Pressure profiles cannot reduce attack telegraph to zero.");
            }

            // Stage 7부터 인간형을 암시하고, Stage 11부터 주인공 신체 태그가 최소 1개 필요하다.
            if (stageNumber >= 7 && !visualTraitTags.HasFlag(VisualTraitTag.HumanoidSilhouette))
            {
                errors.Add("VisualTraitTags must imply a humanoid silhouette from Stage 7 onward.");
            }

            const VisualTraitTag protagonistTraits =
                VisualTraitTag.ProtagonistHand | VisualTraitTag.ProtagonistFace | VisualTraitTag.ProtagonistClothes;
            if (stageNumber >= 11 && (visualTraitTags & protagonistTraits) == VisualTraitTag.None)
            {
                errors.Add("VisualTraitTags must include at least one protagonist trait from Stage 11 onward.");
            }

            if (mimicsPlayerMotion && stageNumber < 9)
            {
                errors.Add("MimicsPlayerMotion cannot be declared before Stage 9.");
            }

            if (reactive && stageNumber < 11)
            {
                errors.Add("Reactive cannot be declared before Stage 11.");
            }

            if (fragmentTraceTowardTraumaRatio > 0f && stageNumber < 7)
            {
                errors.Add("FragmentTraceTowardTraumaRatio cannot be declared before Stage 7.");
            }

            return errors;
        }
    }
}
