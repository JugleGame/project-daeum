using System.Collections.Generic;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 원거리형 잔재의 데이터 에셋. (spec-004, Issue #9)
    /// 너무 가까워지면 물러나고, 사거리 안에서만 공격해 "거리 유지"를 구현한다.
    /// </summary>
    [CreateAssetMenu(fileName = "RangedRemnantData", menuName = "Daeume/Enemy/Ranged Remnant Data")]
    public sealed class RangedRemnantData : RemnantDataBase
    {
        [Header("Ranged")]
        [SerializeField, Min(0.1f)] private float retreatTriggerRange = 0.6f;   // 이보다 가까워지면 물러난다

        public override RemnantArchetype Archetype => RemnantArchetype.Ranged;
        public float RetreatTriggerRange => retreatTriggerRange;

        public override IReadOnlyList<string> ValidateData()
        {
            var errors = new List<string>(base.ValidateData());
            if (retreatTriggerRange >= AttackRange)
            {
                errors.Add("RetreatTriggerRange must be less than AttackRange.");
            }

            return errors;
        }
    }
}
