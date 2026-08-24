using Daeume.ContaminationRuntime;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Memory
{
    /// <summary>
    /// "회상 완료(C 영역)"와 "오염·추격 시작(B 영역)"을 이어 주는 다리 역할이다.
    ///
    /// 왜 다리가 따로 있나:
    /// 회상 쪽(MemoryAnchor)은 추격이 어떻게 굴러가는지 몰라야 하고,
    /// 추격 쪽(StageOneChaseController)은 회상 문장을 몰라야 한다.
    /// 그래서 양쪽은 MemoryCompleted 이벤트만 알고, 실제 연결은 이 작은 스크립트 하나가 맡는다.
    /// 역할 분업(A/B/C)에서 서로의 내부를 읽지 않게 하는 표준적인 방법이며 적합하다.
    /// </summary>
    public sealed class MemoryCompletionBridge : MonoBehaviour
    {
        [SerializeField] private StageOneChaseController stageOneChase;

        // OnEnable과 Start 양쪽에서 연결을 시도하는 이유:
        // GameManager가 아직 생성되지 않은 순서로 씬이 열리면 OnEnable 시점의 구독이 실패한다.
        // Start에서 한 번 더 붙여 그 타이밍 문제를 덮는다(실제 QA에서 발견됐던 문제다).
        private void OnEnable() => GameManager.Instance?.Events.Subscribe<MemoryCompleted>(OnCompleted);
        private void Start() => Reconnect();
        private void OnDisable() => GameManager.Instance?.Events.Unsubscribe<MemoryCompleted>(OnCompleted);

        /// <summary>런타임에 추격 컨트롤러를 지정한다(부트스트랩이 프리팹을 배치하며 호출한다).</summary>
        public void Configure(StageOneChaseController controller) => stageOneChase = controller;

        private void Reconnect()
        {
            if (GameManager.Instance == null) return;

            // 먼저 해제하고 다시 구독한다. OnEnable에서 이미 붙었을 수 있어
            // 이 과정을 빼면 같은 함수가 두 번 등록돼 추격이 두 번 시작될 수 있다.
            GameManager.Instance.Events.Unsubscribe<MemoryCompleted>(OnCompleted);
            GameManager.Instance.Events.Subscribe<MemoryCompleted>(OnCompleted);
        }

        private void OnCompleted(MemoryCompleted message)
        {
            // 인스펙터 연결이 비어 있어도 동작하도록 마지막에 씬에서 찾아본다.
            // 씬 소유권 규칙 때문에 B의 씬을 C가 직접 편집할 수 없어 생긴 현실적인 보완책이다.
            if (stageOneChase == null) stageOneChase = FindAnyObjectByType<StageOneChaseController>();

            // 여기서 압박 단계나 체크포인트를 직접 다루지 않는다.
            // 그 순서(Echo 시작 → 체크포인트 저장 → 추격 시작)는 spec-006에 따라 B의 컨트롤러가 소유한다.
            stageOneChase?.BeginChaseFromMemory();
        }
    }
}
