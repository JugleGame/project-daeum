using System;
using Daeume.Core;
using Daeume.Interaction;
using UnityEngine;

namespace Daeume.Memory
{
    /// <summary>
    /// 스테이지마다 하나씩 놓이는 "기억의 매개체". Stage 1에서는 버스표 보관함에 해당한다. (spec-005)
    ///
    /// 하는 일:
    /// 1) 플레이어가 조사하면 회상을 시작하고 상태를 Explore → Memory로 올린다.
    /// 2) 문장을 한 줄씩 넘긴다(자막은 C의 UI가 이벤트를 받아 그린다).
    /// 3) 끝나면 MemoryCompleted를 발행한다 → 이 신호가 오염 전환과 추격의 방아쇠다.
    ///
    /// 중요한 설계 원칙: 이 스크립트는 문장을 직접 갖고 있지 않고 "문자열 키"만 갖는다.
    /// 원고 수정이 코드 수정이 되지 않게 하기 위한 규칙이다(spec-008, spec-013).
    /// </summary>
    public sealed class MemoryAnchor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string memoryId = "memory-stage01";
        [SerializeField] private string titleKey = "memory.stage01.title";
        [SerializeField] private string narrativeFlag = "stage01-memory-revealed";
        [SerializeField] private string promptKey = "prompt.memory";
        [SerializeField] private string[] lineKeys = { "memory.stage01.01", "memory.stage01.02", "memory.stage01.03" };

        private int lineIndex;

        public string StableId => memoryId;
        public bool IsPresenting { get; private set; }
        public bool IsComplete { get; private set; }

        /// <summary>
        /// 조사 가능 여부. 이미 완료했거나, 회상·추격 이후 상태에서는 다시 열 수 없다.
        /// (한 번만 열리고 조각을 중복 지급하지 않는다 — spec-005)
        /// </summary>
        public bool CanInteract(GameObject interactor) => !IsComplete && GameManager.Instance != null &&
            (GameManager.Instance.StageState == StageState.Explore || GameManager.Instance.StageState == StageState.Memory);

        /// <summary>
        /// 화면에 띄울 프롬프트. 문장이 아니라 "액션 이름 + 문자열 키"만 넘긴다.
        /// </summary>
        public InteractionPrompt GetPrompt() => new("Interact", IsPresenting ? "prompt.continue" : promptKey);

        /// <summary>
        /// 일반 상호작용 진입점. 실제로는 "회상 시작"에만 쓰인다.
        /// </summary>
        /// <remarks>
        /// 회상이 시작되면 상태가 Memory가 되고, spec-010에 따라 일반 상호작용은 잠긴다.
        /// 따라서 그 뒤의 문장 넘기기·건너뛰기는 이 경로가 아니라 MemoryPlayback이 담당한다.
        /// (이 구분을 놓쳐서 회상이 첫 줄에서 멈추는 버그가 있었다. 아래 Advance 주석 참고.)
        /// </remarks>
        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            if (!IsPresenting) Begin(); else Advance();
        }

        /// <summary>회상을 시작한다. 성공하면 상태가 Memory가 되고 첫 문장이 발행된다.</summary>
        public bool Begin()
        {
            if (IsComplete || lineKeys == null || lineKeys.Length == 0 || GameManager.Instance == null) return false;

            if (GameManager.Instance.StageState == StageState.Explore) GameManager.Instance.SetStageState(StageState.Memory);

            // 상태 전이가 거절됐을 수 있으므로(추격 중 등) 다시 확인하고, 아니면 시작하지 않는다.
            // GameManager.SetStageState가 실패를 조용히 무시하기 때문에 이 재확인이 반드시 필요하다.
            if (GameManager.Instance.StageState != StageState.Memory) return false;

            IsPresenting = true;
            lineIndex = 0;
            PublishLine(true);
            return true;
        }

        /// <summary>
        /// 다음 문장으로 넘긴다. 마지막 문장까지 넘겼으면 회상을 완료하고 false를 돌려준다.
        /// </summary>
        /// <remarks>
        /// 호출자는 MemoryPlayback이다. 반환값의 의미: true=아직 남음, false=끝남.
        /// </remarks>
        public bool Advance()
        {
            if (!IsPresenting) return false;
            lineIndex++;
            if (lineIndex < lineKeys.Length) { PublishLine(true); return true; }
            Complete();
            return false;
        }

        /// <summary>
        /// 회상을 건너뛴다. (spec-005: 최초 재생에서도 건너뛰기를 허용한다)
        /// </summary>
        /// <remarks>
        /// 정상 종료와 완전히 같은 Complete()를 부른다.
        /// "두 종료 경로가 같은 후속 흐름을 요청한다"는 스펙 요구를 코드 구조로 보장하기 위해서다.
        /// 별도 처리를 만들면 한쪽만 체크포인트를 저장하는 식의 어긋남이 반드시 생긴다.
        /// </remarks>
        public bool SkipToEnd()
        {
            if (!IsPresenting) return false;
            lineIndex = Mathf.Max(0, (lineKeys?.Length ?? 1) - 1);
            Complete();
            return true;
        }

        private void Complete()
        {
            IsPresenting = false;
            IsComplete = true;

            // 자막 패널을 닫으라는 신호(visible=false).
            GameManager.Instance.Events.Publish(new MemoryPresentationChanged(memoryId, titleKey, string.Empty, lineKeys.Length, lineKeys.Length, false));

            // 이 한 줄이 스테이지 후반부 전체(오염 → 추격 → 탈출)의 방아쇠다.
            // 구독자는 MemoryCompletionBridge이며, 거기서 Echo 압박과 추격 시작을 요청한다.
            GameManager.Instance.Events.Publish(new MemoryCompleted(memoryId, narrativeFlag));
        }

        private void PublishLine(bool visible) => GameManager.Instance.Events.Publish(
            new MemoryPresentationChanged(memoryId, titleKey, lineKeys[lineIndex], lineIndex, lineKeys.Length, visible));

        /// <summary>테스트나 다른 스테이지 데이터로 내용을 바꿔 끼울 때 사용한다.</summary>
        public void Configure(string id, string title, string flag, params string[] lines)
        {
            memoryId = id ?? string.Empty;
            titleKey = title ?? string.Empty;
            narrativeFlag = flag ?? string.Empty;
            lineKeys = lines ?? Array.Empty<string>();
        }
    }
}
