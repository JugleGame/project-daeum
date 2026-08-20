using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// B가 개발 중 사용하던 "회상 완료" 디버그 트리거다.
    ///
    /// 실제 회상 시스템(C)이 없던 기간에 오염·추격을 검증하기 위한 임시 입력이었다.
    /// 지금은 진짜 경로(MemoryAnchor → MemoryCompleted → MemoryCompletionBridge)가 동작한다.
    ///
    /// 공존해도 안전한 이유: 이 스크립트는 자동으로 실행되지 않고 인스펙터 우클릭 메뉴로만 동작하며,
    /// BeginChaseFromMemory 자체가 상태 가드를 갖고 있어 추격이 두 번 시작되지 않는다.
    /// 다만 정리 대상이라는 점은 분명하다 — 마감 전에 제거하거나 개발 빌드 전용으로 분리해야 한다.
    /// </summary>
    public sealed class MemoryCompletionAdapter : MonoBehaviour
    {
        [SerializeField] private ContaminationDirector director;
        [SerializeField] private StageOneChaseController stageOneChase;

        public void Configure(ContaminationDirector value) => director = value;
        public void Configure(StageOneChaseController value) => stageOneChase = value;

        [ContextMenu("Debug/Complete Memory And Start Chase")]
        public void TriggerDebugMemoryComplete()
        {
            if (stageOneChase != null && stageOneChase.BeginChaseFromMemory())
            {
                return;
            }

            director?.BeginChase();
        }
    }
}
