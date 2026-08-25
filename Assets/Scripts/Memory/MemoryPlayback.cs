using Daeume.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Memory
{
    /// <summary>
    /// 회상(기억 재생)이 진행되는 동안의 "다음 문장 / 건너뛰기" 입력을 담당한다. (spec-005)
    ///
    /// ■ 이 스크립트가 왜 새로 필요했는가 — 이번 검토에서 찾은 진행 불가 버그의 원인
    /// 원래는 회상 문장을 넘기는 일을 InteractionTargeter(일반 상호작용)가 대신하고 있었다.
    /// 그런데 spec-010은 "Memory 상태에서는 일반 상호작용을 비활성화한다"고 못 박고 있고,
    /// 코드도 그 규칙을 정확히 지키고 있었다. 그 결과:
    ///   1) 플레이어가 기억을 조사한다 → 상태가 Memory로 바뀐다
    ///   2) 바로 다음 프레임부터 일반 상호작용이 잠긴다
    ///   3) 문장을 넘길 방법이 사라진다 → 회상이 첫 줄에서 영구 정지
    ///   4) MemoryCompleted가 영영 발행되지 않아 오염 전환·추격·탈출 전부 도달 불가
    /// 즉 "게임의 90%가 잠겨 있던" 원인이 바로 이 지점이었다.
    ///
    /// 해결 방향: 회상 재생 입력은 spec-005/폴더 구조가 원래 지정한 대로 Memory 쪽이 직접 소유한다.
    /// 일반 상호작용 규칙(spec-010)은 그대로 두고, 회상 전용 입력만 별도로 받는다.
    ///
    /// ■ 배치 방법
    /// 이 컴포넌트는 씬에 하나만 있으면 된다. Stage01_MemoryAnchor 프리팹이나 HUD 루트 어디든 좋다.
    /// (StagePresentationBootstrap이 회상 앵커를 만들 때 함께 붙여 준다.)
    /// </summary>
    public sealed class MemoryPlayback : MonoBehaviour
    {
        // 입력은 물리 키가 아니라 액션 이름으로 찾는다. 키 재설정을 해도 그대로 동작한다(spec-013).
        public const string AdvanceActionName = "Interact";
        public const string SkipActionName = "Pause";

        [SerializeField] private InputActionReference advanceAction;
        [SerializeField] private InputActionReference skipAction;

        // 같은 프레임에 회상을 시작한 입력이 곧바로 "다음 문장"으로도 처리되는 것을 막기 위한 대기 시간.
        // 이게 없으면 상호작용 키 한 번에 두 줄이 넘어가 첫 문장을 읽을 수 없다.
        [SerializeField, Min(0f)] private float inputCooldownSeconds = 0.15f;

        private InputAction advance;
        private InputAction skip;
        private MemoryAnchor active;
        private float nextInputTime;

        private void Awake() => ResolveActions();

        /// <summary>
        /// 입력 액션을 찾아 둔다. 이미 찾았으면 아무 일도 하지 않는다.
        /// </summary>
        /// <remarks>
        /// Awake에서 한 번만 찾으면 안 된다. 이 컴포넌트는 StagePresentationBootstrap이 회상 앵커를
        /// 만들면서 코드로 붙이는데, 그 시점에 Persistent 씬의 PlayerInput이 아직 없을 수 있다.
        /// 그러면 advance가 영원히 null로 남아 회상이 첫 문장에서 멈춘다 - 상호작용 키를 눌러도
        /// 아무 반응이 없고, MemoryCompleted가 발행되지 않아 추격과 탈출까지 전부 막힌다.
        /// 그래서 아직 못 찾았으면 Update에서 계속 다시 시도한다.
        /// </remarks>
        private void ResolveActions()
        {
            if (advance != null && skip != null) return;

            var playerInput = FindAnyObjectByType<PlayerInput>();
            if (advance == null)
            {
                advance = advanceAction == null ? playerInput?.actions?.FindAction(AdvanceActionName) : advanceAction.action;
                advance?.Enable();
            }

            if (skip == null)
            {
                skip = skipAction == null ? playerInput?.actions?.FindAction(SkipActionName) : skipAction.action;
                skip?.Enable();
            }
        }

        private void OnEnable()
        {
            advance?.Enable();
            skip?.Enable();
        }

        private void OnDisable()
        {
            advance?.Disable();
            skip?.Disable();
        }

        private void Update()
        {
            ResolveActions();

            // 회상 중이 아니면 아무 일도 하지 않는다. 상태 확인이 이 스크립트의 유일한 진입 조건이다.
            if (GameManager.Instance == null || GameManager.Instance.StageState != StageState.Memory)
            {
                active = null;
                return;
            }

            if (active == null || !active.IsPresenting)
            {
                active = FindPresentingAnchor();
                // 회상이 막 시작된 시점이다. 시작 입력이 그대로 다음 문장으로 새지 않도록 잠깐 막는다.
                if (active != null) nextInputTime = Time.unscaledTime + inputCooldownSeconds;
            }

            if (active == null || Time.unscaledTime < nextInputTime)
            {
                return;
            }

            // 건너뛰기: 첫 재생에서도 허용해야 한다(spec-005).
            // 정상 종료와 건너뛰기는 완전히 같은 후속 흐름(MemoryCompleted 1회)을 만들어야 하므로,
            // 두 경로 모두 MemoryAnchor의 함수를 호출해 처리를 한곳으로 모은다.
            if (skip != null && skip.WasPressedThisFrame())
            {
                active.SkipToEnd();
                active = null;
                return;
            }

            if (advance != null && advance.WasPressedThisFrame())
            {
                // Advance가 false를 돌려주면 마지막 문장까지 끝났다는 뜻이다.
                if (!active.Advance())
                {
                    active = null;
                }

                nextInputTime = Time.unscaledTime + inputCooldownSeconds;
            }
        }

        /// <summary>지금 재생 중인 회상 앵커를 찾는다. 스테이지당 하나만 재생되므로 첫 번째를 쓴다.</summary>
        private static MemoryAnchor FindPresentingAnchor()
        {
            foreach (var anchor in FindObjectsByType<MemoryAnchor>(FindObjectsSortMode.None))
            {
                if (anchor.IsPresenting) return anchor;
            }

            return null;
        }
    }
}
