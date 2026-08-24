using UnityEngine;

namespace Daeume.Player
{
    /// <summary>
    /// "여기는 매달릴 수 있는 표면"이라고 표시하기만 하는 컴포넌트다. (spec-002)
    ///
    /// 내용이 비어 있는 게 정상이다. 유니티에서는 이렇게 빈 컴포넌트를 "표식(marker)"으로 쓴다.
    /// 난간·담장 모서리·배관 오브젝트에 이걸 붙여 두면, PlayerController가 충돌 상대에게서
    /// 이 컴포넌트를 찾는 것만으로 "붙잡아도 되는 곳인지"를 판단할 수 있다.
    ///
    /// 대안이었다면 레이어나 태그로 구분할 수도 있지만,
    /// 레이어는 개수가 32개로 제한되고 태그는 오타를 컴파일러가 잡아 주지 못한다.
    /// 표식 컴포넌트가 가장 안전한 방식이라 적합하다.
    ///
    /// spec-002 관련 주의: 붙잡기는 피해를 막지 않는다. 그 규칙은 PlayerHealth 쪽에 있으며
    /// 이 표식에는 어떤 방어 로직도 넣지 않는다.
    /// </summary>
    public sealed class GrabbableSurface : MonoBehaviour
    {
    }
}
