using System;
using System.Collections.Generic;
using Daeume.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Interaction
{
    /// <summary>
    /// 플레이어 주변에서 조사할 대상 1개를 고르고, 프롬프트를 띄우고, 입력이 오면 실행한다. (spec-010)
    ///
    /// 대상 선택 규칙(스펙이 정한 순서 그대로):
    /// 1) 범위 안에서 가장 가까운 것
    /// 2) 거리가 같으면 바라보는 방향에 있는 것
    /// 3) 그것도 같으면 안정 ID 순서
    /// 순서를 못 박아 두는 이유는, 같은 상황에서 항상 같은 대상이 선택되게 하기 위해서다.
    /// 무작위로 바뀌면 플레이어가 조작을 신뢰할 수 없고 테스트도 불가능해진다.
    /// </summary>
    public sealed class InteractionTargeter : MonoBehaviour
    {
        public const string InteractActionName = "Interact";

        [SerializeField] private InputActionReference interactAction;
        [SerializeField, Min(0.01f)] private float range = 1.25f;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private float facingDirection = 1f;

        private IInteractable current;
        private InputAction interact;
        private Rigidbody2D body;

        public IInteractable Current => current;

        private void Awake()
        {
            // 바라보는 방향을 스스로 알아내기 위해 물리 몸체를 찾아 둔다(아래 Update 주석 참고).
            body = GetComponentInParent<Rigidbody2D>();

            var playerInput = GetComponentInParent<PlayerInput>();
            interact = interactAction == null
                ? playerInput?.actions?.FindAction(InteractActionName)
                : interactAction.action;
        }

        private void OnEnable() => interact?.Enable();

        private void OnDisable()
        {
            interact?.Disable();
            // 꺼질 때 대상을 비우면서 프롬프트 숨김 이벤트도 함께 나간다.
            // 이걸 빼면 화면에 프롬프트가 남은 채로 얼어붙는다.
            SetCurrent(null);
        }

        private void Update()
        {
            // 수정: 예전에는 SetFacingDirection을 아무도 호출하지 않아 방향이 항상 초기값(오른쪽)이었다.
            // 그래서 "거리가 같을 때 바라보는 쪽을 우선한다"는 spec-010 규칙이 사실상 동작하지 않았다.
            // 이동 코드(Player)를 직접 참조하면 모듈 의존이 늘어나므로, 물리 속도에서 방향을 스스로 읽는다.
            if (body != null && Mathf.Abs(body.linearVelocity.x) > 0.01f)
            {
                SetFacingDirection(body.linearVelocity.x);
            }

            RefreshTarget();
            if (interact != null && interact.WasPressedThisFrame())
            {
                TryInteract();
            }
        }

        /// <summary>바라보는 방향을 알려 준다(이동 코드가 호출).</summary>
        public void SetFacingDirection(float direction)
        {
            if (!Mathf.Approximately(direction, 0f))
            {
                facingDirection = Mathf.Sign(direction);
            }
        }

        /// <summary>범위 안 대상을 다시 계산하고 가장 적절한 하나를 고른다.</summary>
        public IInteractable RefreshTarget()
        {
            if (!IsInteractionStateAllowed())
            {
                SetCurrent(null);
                return null;
            }

            var overlaps = Physics2D.OverlapCircleAll(transform.position, range, targetMask);
            var candidates = new List<Candidate>(overlaps.Length);
            var seen = new HashSet<IInteractable>();   // 콜라이더가 여러 개인 대상을 중복 집계하지 않기 위함

            for (var index = 0; index < overlaps.Length; index++)
            {
                var target = FindInteractable(overlaps[index]);
                if (target == null || !seen.Add(target) || !target.CanInteract(gameObject))
                {
                    continue;
                }

                var component = target as Component;
                if (component == null)
                {
                    // 씬 오브젝트가 아닌 구현체는 위치를 알 수 없어 거리 비교가 불가능하다. 후보에서 제외한다.
                    continue;
                }

                var offset = component.transform.position - transform.position;
                candidates.Add(new Candidate(target, offset.sqrMagnitude, Mathf.Sign(offset.x) == facingDirection));
            }

            candidates.Sort(Candidate.Compare);
            SetCurrent(candidates.Count == 0 ? null : candidates[0].Target);
            return current;
        }

        /// <summary>현재 대상을 실행한다. 조건이 맞지 않으면 아무 일도 하지 않는다.</summary>
        public bool TryInteract()
        {
            if (!IsInteractionStateAllowed() || current == null || !current.CanInteract(gameObject))
            {
                return false;
            }

            current.Interact(gameObject);
            return true;
        }

        /// <summary>
        /// 지금 일반 상호작용이 허용되는 상태인가.
        /// </summary>
        /// <remarks>
        /// spec-010: "Memory, Failed, Cleared 중 일반 상호작용을 비활성화한다."
        /// 즉 회상이 재생되는 동안 문을 열거나 다른 물건을 조사할 수 없어야 한다.
        ///
        /// 중요: 이 규칙 때문에 "회상 문장 넘기기"를 이 경로로 처리하면 안 된다.
        /// 회상 진행·건너뛰기 입력은 Daeume.Memory의 MemoryPlayback이 따로 소유한다.
        /// (예전에는 이 구분이 없어서 회상이 첫 문장에서 영구 정지하는 버그가 있었다.)
        /// </remarks>
        private bool IsInteractionStateAllowed()
        {
            var state = GameManager.Instance == null ? StageState.Explore : GameManager.Instance.StageState;
            return state != StageState.Memory && state != StageState.Failed && state != StageState.Cleared;
        }

        /// <summary>현재 대상이 바뀔 때만 프롬프트 이벤트를 발행한다.</summary>
        private void SetCurrent(IInteractable next)
        {
            // 매 프레임 같은 이벤트를 쏟아내면 UI가 불필요하게 다시 그려진다. 변화가 있을 때만 알린다.
            if (ReferenceEquals(current, next))
            {
                return;
            }

            current = next;
            if (current == null)
            {
                GameManager.Instance?.Events.Publish(new InteractionPromptChanged(false, string.Empty, string.Empty));
                return;
            }

            var prompt = current.GetPrompt();

            // 실제로 지금 어떤 키에 묶여 있는지를 입력 시스템에서 조회해 표시한다(예: "E").
            // 키를 재설정하면 표시도 자동으로 따라간다 — spec-013의 Test_UI_PromptReflectsRebinding 요구사항.
            //
            // 주의: GetBindingDisplayString()을 인자 없이 부르면 연결 여부와 상관없이 모든 바인딩을
            // "E | Y" 식으로 합쳐서 반환한다(예: 키보드 E + 미연결 게임패드 buttonNorth=Y).
            // 실제로 입력 가능한 컨트롤(현재 연결된 기기)만 표시해야 눌리지 않는 키가 뜨지 않는다.
            var keyLabel = interact != null && interact.controls.Count > 0
                ? interact.controls[0].displayName
                : prompt.ActionName;
            GameManager.Instance?.Events.Publish(
                new InteractionPromptChanged(true, string.IsNullOrEmpty(keyLabel) ? prompt.ActionName : keyLabel, prompt.StringTableKey));
        }

        private static IInteractable FindInteractable(Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            // 콜라이더는 자식에, 실제 스크립트는 부모에 있는 구성이 흔하므로 부모까지 거슬러 올라간다.
            foreach (var component in collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is IInteractable interactable)
                {
                    return interactable;
                }
            }

            return null;
        }

        /// <summary>대상 하나에 대한 비교 정보. 정렬 규칙을 한곳에 모아 두기 위한 구조체다.</summary>
        private readonly struct Candidate
        {
            public Candidate(IInteractable target, float distanceSquared, bool facesTarget)
            {
                Target = target;
                DistanceSquared = distanceSquared;
                FacesTarget = facesTarget;
            }

            public IInteractable Target { get; }

            // 제곱 거리로 비교한다. 실제 거리를 구하려면 제곱근 계산이 필요한데,
            // 크기 비교만 할 거라면 제곱 상태로 비교해도 결과가 같고 더 빠르다. 흔히 쓰는 최적화다.
            private float DistanceSquared { get; }
            private bool FacesTarget { get; }

            public static int Compare(Candidate left, Candidate right)
            {
                var distance = left.DistanceSquared.CompareTo(right.DistanceSquared);
                if (distance != 0)
                {
                    return distance;
                }

                // 바라보는 쪽을 우선한다(true가 앞으로 오도록 right/left 순서를 뒤집어 비교).
                var facing = right.FacesTarget.CompareTo(left.FacesTarget);
                return facing != 0
                    ? facing
                    // 마지막 기준은 ID 사전순. 완전한 동률에서도 결과가 흔들리지 않게 만드는 장치다.
                    : string.Compare(left.Target.StableId, right.Target.StableId, StringComparison.Ordinal);
            }
        }
    }
}
