#if UNITY_EDITOR
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>
    /// B가 개발 중 사용하던 "회상 완료" 디버그 트리거다.
    ///
    /// 실제 회상 시스템(C)이 없던 기간에 오염·추격을 검증하기 위한 임시 입력이었다.
    /// 지금은 진짜 경로(MemoryAnchor → MemoryCompleted → MemoryCompletionBridge)가 동작한다.
    ///
    /// RoleBIntegrationQaTests/ContaminationOverlayTests가 실제 회상 재생 없이 오염·추격만
    /// 검증하는 데 여전히 이 트리거를 쓴다. 그래서 완전히 삭제하지 않고 UNITY_EDITOR 전용으로
    /// 묶어 실제 플레이어 빌드에는 포함되지 않게 정리했다(클래스 자신의 예전 메모가 제안한 두 옵션
    /// 중 "개발 빌드 전용으로 분리" 쪽).
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
#endif
