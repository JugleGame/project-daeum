using UnityEngine;

namespace Daeume.Core
{
    /// <summary>
    /// 스프라이트 색을 사인 곡선으로 오가게 해 "여기를 보라"를 알리는 맥동 연출의 공통 뼈대다.
    /// </summary>
    /// <remarks>
    /// 언제 맥동할지는 상황마다 다르다(아직 읽지 않은 회상 지점, 추격이 시작된 탈출구). 그래서
    /// 켜고 끄는 조건만 <see cref="ShouldPulse"/>로 열어 두고, 색을 흔드는 방법은 여기 한 곳에 둔다.
    ///
    /// 밝기를 1보다 크게 곱한다. SpriteRenderer.color는 곱셈이라 흰색(1,1,1)에서 노란색으로 보간하기만
    /// 하면 어두워지기만 하고 "빛난다"로 읽히지 않는다.
    ///
    /// 맥동이 끝나면 원래 색으로 돌려놓는다. 더 이상 알릴 것이 없는데 계속 깜빡이면 잘못된 안내가 된다.
    /// </remarks>
    [RequireComponent(typeof(SpriteRenderer))]
    public abstract class SpritePulse : MonoBehaviour
    {
        [SerializeField] protected SpriteRenderer target;

        /// <summary>맥동이 가장 밝을 때의 색. 1을 넘겨 실제로 밝아지게 한다.</summary>
        [SerializeField] private Color peakColor = new(1.7f, 1.6f, 1f, 1f);

        [SerializeField, Min(0.05f)] private float cycleSeconds = 1.3f;

        private Color baseColor = Color.white;
        private float elapsed;

        /// <summary>지금 맥동해야 하는가. 파생 클래스가 상황을 판단한다.</summary>
        protected abstract bool ShouldPulse { get; }

        protected virtual void Awake()
        {
            if (target == null) target = GetComponent<SpriteRenderer>();
            if (target != null) baseColor = target.color;
        }

        protected virtual void OnDisable() => Restore();

        protected virtual void Update()
        {
            if (target == null) return;

            if (!ShouldPulse)
            {
                Restore();
                return;
            }

            elapsed += Time.deltaTime;

            // 사인 곡선을 0~1로 옮긴다. 켜졌다 꺼지는 깜빡임이 아니라 숨 쉬듯 오가는 맥동이 된다.
            var wave = (Mathf.Sin(elapsed / cycleSeconds * Mathf.PI * 2f) + 1f) * 0.5f;
            target.color = Color.Lerp(baseColor, peakColor, wave);
        }

        protected void Restore()
        {
            if (target != null) target.color = baseColor;
        }
    }
}
