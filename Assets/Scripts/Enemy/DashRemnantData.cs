using System.Collections.Generic;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 돌진형 잔재의 데이터 에셋. (spec-004, Issue #9)
    /// 접근 범위에서 예고 후 빠르게 돌진해 부딪힌 순간 피해를 준다.
    /// </summary>
    [CreateAssetMenu(fileName = "DashRemnantData", menuName = "Daeume/Enemy/Dash Remnant Data")]
    public sealed class DashRemnantData : RemnantDataBase
    {
        [Header("Dash")]
        [SerializeField, Min(0.1f)] private float dashTriggerRange = 3f;   // 이 거리 안이면 접근을 멈추고 돌진을 예고한다
        [SerializeField, Min(0.1f)] private float dashSpeed = 9f;
        [SerializeField, Min(0.05f)] private float dashMaxDurationSeconds = 0.6f;

        public override RemnantArchetype Archetype => RemnantArchetype.Dash;
        public float DashTriggerRange => dashTriggerRange;
        public float DashSpeed => dashSpeed;
        public float DashMaxDurationSeconds => dashMaxDurationSeconds;

        public override IReadOnlyList<string> ValidateData()
        {
            var errors = new List<string>(base.ValidateData());
            if (dashTriggerRange <= AttackRange)
            {
                errors.Add("DashTriggerRange must be greater than AttackRange.");
            }

            if (dashTriggerRange > DetectionRange)
            {
                errors.Add("DashTriggerRange cannot exceed DetectionRange.");
            }

            return errors;
        }
    }
}
