using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 막다른 길 구간. 플레이어가 이 영역 안에 있는 동안 감독에게 "길이 막혔다"고 알린다. (spec-006)
    ///
    /// 왜 필요한가: 길이 막혔을 때 추격자가 그대로 다가오면 플레이어는 아무 것도 할 수 없이 실패한다.
    /// 스펙은 이런 상황에서 "즉시 실패시키지 말고 추격자를 최대 거리까지 물리라"고 규정한다.
    /// 이 컴포넌트가 그 판단의 입력(들어옴/나감)을 담당한다.
    /// </summary>
    public sealed class ChaseDeadEndZone : MonoBehaviour
    {
        [SerializeField] private StageOneChaseController chase;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) chase?.SetDeadEndBlocked(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) chase?.SetDeadEndBlocked(false);
        }

        public void Configure(StageOneChaseController value) => chase = value;
    }
}
