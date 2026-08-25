using Daeume.Core;
using UnityEngine;

namespace Daeume.Memory
{
    /// <summary>
    /// 아직 읽지 않은 회상 지점을 노란빛으로 맥동시켜 "여기를 눌러라"를 알린다.
    /// </summary>
    /// <remarks>
    /// 왜 필요한가: 회상 지점은 거리의 다른 소품과 같은 스프라이트를 쓴다. 가만히 서 있으면
    /// 배경 소품과 구분되지 않아, 처음 플레이하는 사람은 상호작용 대상이 있다는 사실 자체를
    /// 모른 채 지나친다.
    ///
    /// 색을 흔드는 방법 자체는 <see cref="SpritePulse"/>가 갖고 있다. 여기서는 "언제 맥동할지"만 정한다.
    /// </remarks>
    public sealed class MemoryAnchorPulse : SpritePulse
    {
        [SerializeField] private MemoryAnchor anchor;

        // 재생 중에도 멈춘다. 이미 반응한 대상을 계속 부르는 것은 안내가 아니라 방해다.
        protected override bool ShouldPulse => anchor == null || !(anchor.IsComplete || anchor.IsPresenting);

        protected override void Awake()
        {
            base.Awake();
            if (anchor == null) anchor = GetComponent<MemoryAnchor>();
        }
    }
}
