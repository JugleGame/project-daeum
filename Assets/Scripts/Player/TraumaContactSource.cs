using Daeume.Core;
using UnityEngine;

namespace Daeume.Player
{
    /// <summary>
    /// 트라우마 본체에 붙이는 표식 컴포넌트. "공격이 통하지 않는 존재"임을 코드로 못 박는다. (spec-003)
    ///
    /// 왜 이렇게 하나:
    /// 공격하는 쪽(PlayerCombat)에 "상대가 트라우마면 무시" 같은 예외를 넣으면,
    /// 나중에 공격 경로가 하나 더 생길 때 그 예외를 빠뜨리게 된다.
    /// 대신 맞는 쪽이 "나는 피해를 받지 않는다"고 스스로 답하게 만들면 어떤 경로로 때려도 결과가 같다.
    /// 규칙을 한곳에 모은다는 점에서 매우 적합한 설계다.
    ///
    /// 또한 이 컴포넌트는 "여기 닿으면 붙잡기 연출이 시작된다"는 표식 역할도 한다
    /// (TraumaContactHandler가 이걸 찾아 접촉을 판정한다).
    /// </summary>
    public sealed class TraumaContactSource : MonoBehaviour, IDamageable
    {
        public DamageTargetKind TargetKind => DamageTargetKind.Trauma;

        // 항상 "적용되지 않음, 피해 0"을 돌려준다.
        // 경직·넉백·진행도 어느 것도 발생하지 않으므로 spec-003의 요구를 그대로 만족한다.
        public DamageResult ApplyDamage(DamageRequest request) => new(false, 0);
    }
}
