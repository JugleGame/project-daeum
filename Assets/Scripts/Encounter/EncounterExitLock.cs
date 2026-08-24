using UnityEngine;

namespace Daeume.Encounter
{
    /// <summary>
    /// 전투 중 출구를 막는 벽. (spec-012)
    ///
    /// 물리적으로 길을 막는 콜라이더와, 그 사실을 눈으로 알려 주는 표시를 함께 켜고 끈다.
    /// 보이지 않는 벽에 막히면 플레이어는 버그로 인식하므로, 시각 표시를 같이 다루는 것이 중요하다.
    /// (spec-013의 "필수 정보를 색만으로 전달하지 않는다" 규칙에 따라,
    ///  표시 스프라이트는 색뿐 아니라 형태로도 잠김을 알 수 있어야 한다.)
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class EncounterExitLock : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer lockRenderer;
        private Collider2D barrier;

        public bool IsLocked { get; private set; }

        /// <summary>표시용 렌더러를 런타임에 지정한다. 지정 즉시 현재 잠금 상태에 맞춰 준다.</summary>
        public void ConfigureRenderer(SpriteRenderer value)
        {
            lockRenderer = value;
            if (lockRenderer != null) lockRenderer.enabled = IsLocked;
        }

        private void Awake()
        {
            barrier = GetComponent<Collider2D>();
            // 시작은 항상 열린 상태다. 씬에 저장된 값이 어떻든 전투 진입 전에는 통행이 가능해야 한다.
            SetLocked(false);
        }

        public void SetLocked(bool value)
        {
            IsLocked = value;
            if (barrier == null) barrier = GetComponent<Collider2D>();

            // isTrigger = false: 통과되는 감지 영역이 아니라 실제로 막는 벽으로 동작시킨다.
            barrier.isTrigger = false;

            // 콜라이더 자체를 끄고 켜서 막는다. 오브젝트를 통째로 비활성화하지 않는 이유는,
            // 비활성 오브젝트는 코드에서 찾기 어려워지고 표시 렌더러 제어도 함께 끊기기 때문이다.
            barrier.enabled = value;

            if (lockRenderer != null) lockRenderer.enabled = value;
        }
    }
}
