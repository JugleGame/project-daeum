using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// 탈출 지점. 플레이어가 닿으면 스테이지 완료 절차를 요청한다. (spec-001, spec-015)
    ///
    /// 완료 가능 여부(추격 중인가)는 여기서 판단하지 않고 StageOneChaseController에 맡긴다.
    /// 판단이 두 곳에 흩어지면 "회상 전에 출구로 걸어가 클리어되는" 류의 구멍이 생긴다.
    /// </summary>
    public sealed class StageOneEscapeTrigger : MonoBehaviour
    {
        [SerializeField] private StageOneChaseController chase;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) chase?.CompleteAtEscape();
        }

        public void Configure(StageOneChaseController value) => chase = value;
    }
}
