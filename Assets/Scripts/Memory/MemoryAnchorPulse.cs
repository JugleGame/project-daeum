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
    /// 밝기를 1보다 크게 곱한다. SpriteRenderer.color는 곱셈이라 흰색(1,1,1)에서 노란색으로
    /// 보간하기만 하면 어두워지기만 하고 "빛난다"로 읽히지 않는다.
    ///
    /// 다 읽은 뒤에는 멈춘다. 누를 수 없는 대상이 계속 깜빡이면 잘못된 안내가 된다.
    /// </remarks>
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class MemoryAnchorPulse : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer target;
        [SerializeField] private MemoryAnchor anchor;

        /// <summary>맥동이 가장 밝을 때의 색. 1을 넘겨 실제로 밝아지게 한다.</summary>
        [SerializeField] private Color peakColor = new(1.7f, 1.6f, 1f, 1f);

        [SerializeField, Min(0.05f)] private float cycleSeconds = 1.3f;

        private Color baseColor = Color.white;
        private float elapsed;

        private void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (anchor == null) anchor = GetComponent<MemoryAnchor>();
            if (target != null) baseColor = target.color;
        }

        private void OnDisable() => Restore();

        private void Update()
        {
            if (target == null) return;

            // 재생 중에도 멈춘다. 이미 반응한 대상을 계속 부르는 것은 안내가 아니라 방해다.
            if (anchor != null && (anchor.IsComplete || anchor.IsPresenting))
            {
                Restore();
                return;
            }

            elapsed += Time.deltaTime;

            // 사인 곡선을 0~1로 옮긴다. 켜졌다 꺼지는 깜빡임이 아니라 숨 쉬듯 오가는 맥동이 된다.
            var wave = (Mathf.Sin(elapsed / cycleSeconds * Mathf.PI * 2f) + 1f) * 0.5f;
            target.color = Color.Lerp(baseColor, peakColor, wave);
        }

        private void Restore()
        {
            if (target != null) target.color = baseColor;
        }
    }
}
