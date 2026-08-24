using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 근접형 잔재의 데이터 에셋. (spec-004)
    /// 공통 필드는 RemnantDataBase가 소유하며, 근접형은 별도 고유 필드가 없다.
    /// </summary>
    [CreateAssetMenu(fileName = "MeleeRemnantData", menuName = "Daeume/Enemy/Melee Remnant Data")]
    public sealed class MeleeRemnantData : RemnantDataBase
    {
        public override RemnantArchetype Archetype => RemnantArchetype.Melee;
    }
}
