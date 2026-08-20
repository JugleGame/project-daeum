using System;
using System.Collections.Generic;
using System.Linq;
using Daeume.Contamination;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 압박 단계 하나에서 잔재가 어떻게 달라지는지를 담은 수치 묶음. (spec-004)
    ///
    /// 핵심: 압박이 올라도 "새로운 적"이 생기지 않는다. 같은 적의 선언된 수치만 바뀐다.
    /// 그래서 이 값들이 코드 분기가 아니라 데이터로 존재한다.
    /// </summary>
    [Serializable]
    public struct RemnantPressureProfile
    {
        [SerializeField] private PressureStage stage;
        [SerializeField, Min(0.01f)] private float moveSpeedMultiplier;
        [SerializeField, Min(0.01f)] private float telegraphMultiplier;
        [SerializeField] private bool watchesTrauma;

        public RemnantPressureProfile(
            PressureStage stage,
            float moveSpeedMultiplier,
            float telegraphMultiplier,
            bool watchesTrauma)
        {
            this.stage = stage;
            this.moveSpeedMultiplier = Mathf.Max(0.01f, moveSpeedMultiplier);
            this.telegraphMultiplier = Mathf.Max(0.01f, telegraphMultiplier);
            this.watchesTrauma = watchesTrauma;
        }

        public PressureStage Stage => stage;
        public float MoveSpeedMultiplier => Mathf.Max(0.01f, moveSpeedMultiplier);
        public float TelegraphMultiplier => Mathf.Max(0.01f, telegraphMultiplier);
        public bool WatchesTrauma => watchesTrauma;
    }

    /// <summary>
    /// 근접형 잔재의 모든 수치를 담는 데이터 에셋. (spec-004)
    ///
    /// 체력·피해·사거리 같은 기본값과, 압박 단계별 변화(pressureProfiles)를 함께 선언한다.
    /// 기본 프로필 3종(Stable/Echo/Intrusion)이 미리 채워져 있어 에셋을 새로 만들어도 바로 동작한다.
    /// Echo부터 WatchesTrauma가 true인 것이 "잔재가 트라우마 쪽을 본다"는 복선 연출을 만든다.
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeRemnantData", menuName = "Daeume/Enemy/Melee Remnant Data")]
    public sealed class MeleeRemnantData : ScriptableObject
    {
        [SerializeField] private string enemyId = "remnant-melee";
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(1)] private int contactDamage = 1;
        [SerializeField, Min(0.1f)] private float detectionRange = 5f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.1f;
        [SerializeField, Min(0.01f)] private float moveSpeed = 1.8f;
        [SerializeField, Min(0.01f)] private float alertSeconds = 0.2f;
        [SerializeField, Min(0.05f)] private float attackTelegraphSeconds = 0.55f;
        [SerializeField, Min(0.01f)] private float attackRecoverySeconds = 0.7f;
        [SerializeField, Min(0.01f)] private float hitStunSeconds = 0.18f;
        [SerializeField] private List<RemnantPressureProfile> pressureProfiles = new()
        {
            new RemnantPressureProfile(PressureStage.Stable, 1f, 1f, false),
            new RemnantPressureProfile(PressureStage.Echo, 1.08f, 0.85f, true),
            new RemnantPressureProfile(PressureStage.Intrusion, 1.16f, 0.7f, true)
        };

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
        public IReadOnlyList<RemnantPressureProfile> PressureProfiles => pressureProfiles;

        /// <summary>
        /// 해당 압박 단계의 프로필을 찾는다. 선언돼 있지 않으면 "변화 없음(Stable 기본값)"을 돌려준다.
        /// 데이터가 비어 있어도 게임이 멈추지 않게 하는 안전한 기본값 처리다.
        /// </summary>
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

        /// <summary>
        /// 데이터 오류 목록을 돌려준다(비어 있으면 정상).
        /// </summary>
        /// <remarks>
        /// 가장 중요한 검사는 마지막의 "예고 시간이 0에 가까워지지 않는가"다.
        /// 압박 단계 배수를 곱한 뒤에도 0.05초 이상 남아야 한다 — 예고 없는 공격을 금지하는
        /// spec-004의 공정성 규칙(Test_Remnant_TelegraphNeverReachesZero)을 데이터 단계에서 지키는 장치다.
        /// </remarks>
        public IReadOnlyList<string> ValidateData()
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

            if (GetProfile(PressureStage.Echo).TelegraphMultiplier * attackTelegraphSeconds < 0.05f ||
                GetProfile(PressureStage.Intrusion).TelegraphMultiplier * attackTelegraphSeconds < 0.05f)
            {
                errors.Add("Pressure profiles cannot reduce attack telegraph to zero.");
            }

            return errors;
        }
    }
}
