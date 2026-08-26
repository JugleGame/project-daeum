using System.Collections;
using System.Collections.Generic;
using Daeume.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Player
{
    /// <summary>
    /// 플레이어의 근접 공격을 담당한다. (spec-003)
    ///
    /// 핵심 규칙 두 가지:
    /// 1. 잔재에게는 피해가 들어가지만, 트라우마에게는 어떤 효과도 없다(공격으로 해결할 수 없는 존재).
    /// 2. "실제로 맞혔을 때만" PlayerAggression을 켠다. 헛스윙은 켜지 않는다.
    ///    Stage 11의 비선공 통과 판정이 이 값을 그대로 쓰기 때문에, 판정 기준이 여기 하나뿐이어야 한다.
    /// </summary>
    public sealed class PlayerCombat : MonoBehaviour
    {
        public const string AttackActionName = "Attack";

        [SerializeField] private InputActionReference attackAction;
        [SerializeField] private Transform attackOrigin;                 // 공격 판정 원의 중심(캐릭터 앞쪽 자식 오브젝트)
        [SerializeField, Min(0.01f)] private float attackRadius = 0.55f;
        [SerializeField, Min(0)] private int damage = 1;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private string encounterId = "Stage01_Encounter01";
        [SerializeField] private SpriteRenderer swipeVisual;              // 공격 판정 범위를 잠깐 보여 주는 블록아웃 표시
        [SerializeField, Min(0f)] private float swipeVisibleSeconds = 0.12f;
        [SerializeField, Min(0f)] private float attackLockSeconds = 0.67f;   // 공격 중 이동이 잠기는 시간(Player_Attack 길이)
        [SerializeField, Min(0f)] private float attackWindupSeconds = 0.25f; // 입력에서 타격 판정까지의 예비 동작 시간

        // 한 번의 공격에서 같은 대상을 두 번 때리지 않게 기록해 두는 집합.
        // 적의 몸에 콜라이더가 여러 개면(몸통+예고 표시) 같은 적이 두 번 검출되기 때문에 꼭 필요하다.
        private readonly HashSet<IDamageable> damaged = new();
        private InputAction attack;
        private PlayerController controller;
        private float attackOriginOffsetX;   // 공격 원점의 좌우 거리(절댓값). 바라보는 방향에 따라 부호만 바꾼다.
        private Coroutine swipeRoutine;
        private float attackLockRemaining;
        private float windupRemaining = -1f;   // 음수면 대기 중인 공격이 없다는 뜻이다.

        public bool PlayerAggression { get; private set; }
        public int AttackSequence { get; private set; }
        public bool CombatEnabled { get; private set; } = true;

        /// <summary>공격 동작이 끝나기 전인가. PlayerController가 이 값을 보고 이동을 막는다.</summary>
        /// <remarks>
        /// 예전에는 달리면서 공격하면 다리가 멈춘 공격 자세로 미끄러져 "애니메이션이 멈췄다"로 보였다.
        /// 시간은 Player_Attack 클립 길이(0.667초)에 맞춘다. PlayerAnimationDriver의
        /// attackHoldSeconds와 값이 같지만 역할이 다르다 - 저쪽은 어떤 상태를 그릴지,
        /// 이쪽은 움직일 수 있는지를 정한다. 연출만 늘리고 조작은 그대로 두는 조정이 가능해야 한다.
        /// </remarks>
        public bool IsAttacking => attackLockRemaining > 0f;

        private void Awake()
        {
            var playerInput = GetComponentInParent<PlayerInput>();
            attack = attackAction == null ? playerInput?.actions?.FindAction(AttackActionName) : attackAction.action;
            controller = GetComponentInParent<PlayerController>();
            if (attackOrigin != null) attackOriginOffsetX = Mathf.Abs(attackOrigin.localPosition.x);
        }

        private void OnEnable()
        {
            attack?.Enable();
            GameManager.Instance?.Events.Subscribe<EncounterCleared>(OnEncounterCleared);
        }

        private void OnDisable()
        {
            attack?.Disable();
            GameManager.Instance?.Events.Unsubscribe<EncounterCleared>(OnEncounterCleared);
        }

        /// <summary>spec-003: Encounter가 Cleared되면 선공 여부를 초기화한다. 이 Encounter가 낸 신호만 반영한다.</summary>
        private void OnEncounterCleared(EncounterCleared value)
        {
            if (value.EncounterId == encounterId)
            {
                ResetAggression();
            }
        }

        private void Update()
        {
            attackLockRemaining = Mathf.Max(0f, attackLockRemaining - Time.deltaTime);
            TickWindup(Time.deltaTime);

            // 바라보는 방향에 맞춰 공격 판정 위치를 좌우로 뒤집는다.
            // 이 처리가 없으면 왼쪽을 보고 있어도 오른쪽 허공을 때린다(실제로 발생했던 버그다).
            if (attackOrigin != null && controller != null)
            {
                var position = attackOrigin.localPosition;
                position.x = attackOriginOffsetX * controller.FacingDirection;
                attackOrigin.localPosition = position;
            }

            if (attack != null && attack.WasPressedThisFrame())
            {
                BeginAttack();
            }
        }

        /// <summary>예비 동작 시간을 줄이고, 다 되면 타격을 판정한다.</summary>
        private void TickWindup(float deltaTime)
        {
            if (windupRemaining < 0f) return;

            windupRemaining -= Mathf.Max(0f, deltaTime);
            if (windupRemaining > 0f) return;

            windupRemaining = -1f;

            // 예비 동작 사이에 회상이 시작되거나 스테이지가 끝났을 수 있다. 판정 직전에 다시 확인한다.
            if (IsCombatAllowed()) ResolveAttack();
        }

        /// <summary>
        /// 입력으로 공격을 시작한다. 연출을 먼저 띄우고 타격은 예비 동작 뒤에 판정한다.
        /// </summary>
        /// <remarks>
        /// 예전에는 버튼을 누른 프레임에 피해가 들어갔다. 휘두르는 그림은 0.667초에 걸쳐 나오는데
        /// 판정만 먼저 끝나서 "때리기 전에 맞는" 그림이 됐다.
        ///
        /// 동작 중 재입력은 무시한다. 무시하지 않으면 연타로 예비 동작을 건너뛰며
        /// 애니메이션이 계속 처음부터 다시 재생된다.
        /// </remarks>
        public bool BeginAttack()
        {
            // 매달린 동안에는 공격 입력을 받지 않는다. 벽을 잡은 자세에서 휘두르면
            // 이동·방향이 잠긴 채로 매달리기까지 겹쳐 어느 동작인지 읽히지 않는다.
            if (controller != null && controller.IsGrabbing)
            {
                return false;
            }

            if (!IsCombatAllowed() || IsAttacking)
            {
                return false;
            }

            AttackSequence++;
            attackLockRemaining = attackLockSeconds;
            windupRemaining = attackWindupSeconds;
            AudioRuntime.PlaySfx("PlayerAttack");
            return true;
        }

        /// <summary>공격을 1회 수행하고, 실제로 피해를 입힌 잔재 수를 돌려준다.</summary>
        /// <remarks>
        /// 예비 동작 없이 그 자리에서 판정까지 끝낸다. 시간을 직접 다루지 않는 호출자
        /// (테스트, 스크립트로 거는 공격)를 위한 동기 경로다.
        /// 사람이 누르는 입력은 BeginAttack을 거쳐 예비 동작 뒤에 판정된다.
        /// </remarks>
        public int Attack()
        {
            // spec-005: 회상 재생 중에는 전투 피해가 발생하지 않는다.
            // spec-001: 실패/클리어 상태에서도 조작이 진행에 개입하면 안 된다.
            // 상태 확인을 공격 진입점 한 곳에서만 하므로, 입력·테스트·AI 어느 경로로 불러도 같은 규칙이 적용된다.
            if (!CombatEnabled || !IsCombatAllowed())
            {
                return 0;
            }

            AttackSequence++;
            attackLockRemaining = attackLockSeconds;
            windupRemaining = -1f;   // 대기 중이던 예비 동작이 있으면 이 공격이 대신한다.
            return ResolveAttack();
        }

        /// <summary>타격 판정만 수행한다. 연출 시작(AttackSequence)과 잠금은 호출자가 이미 처리했다.</summary>
        private int ResolveAttack()
        {
            damaged.Clear();
            var center = attackOrigin == null ? transform.position : attackOrigin.position;
            var hits = Physics2D.OverlapCircleAll(center, attackRadius, targetMask);
            var appliedCount = 0;

            // 맞았든 헛스윙이든 공격을 시도했다는 사실 자체는 보여야 한다.
            // 예전에는 아무 시각 효과가 없어서 플레이어가 공격이 나가는지 알 수 없었다.
            if (swipeVisual != null)
            {
                if (swipeRoutine != null) StopCoroutine(swipeRoutine);
                swipeRoutine = StartCoroutine(FlashSwipe());
            }

            for (var index = 0; index < hits.Length; index++)
            {
                // 공격 판정 원이 attackOrigin을 중심으로 하다 보니 플레이어 자신의 콜라이더(캡슐)나
                // AttackOrigin/Visual 같은 자식 오브젝트까지 겹칠 수 있다. 그걸 걸러내지 않으면
                // FindDamageable이 부모를 타고 올라가 플레이어 자신의 PlayerHealth를 찾아내
                // 스스로를 공격해 체력이 깎이는 실제 버그가 된다(이동·점프 중 특히 자주 겹쳤다).
                if (hits[index].transform.IsChildOf(transform))
                {
                    continue;
                }

                var target = FindDamageable(hits[index]);
                // damaged.Add가 false면 이미 이번 공격에서 처리한 대상이라는 뜻이다.
                if (target == null || !damaged.Add(target))
                {
                    continue;
                }

                var result = target.ApplyDamage(new DamageRequest(damage, gameObject));

                if (target.TargetKind == DamageTargetKind.Trauma)
                {
                    AudioRuntime.PlaySfx("TraumaHit");
                }

                // 피해가 실제로 들어갔고(무적이 아니었고) 대상이 잔재일 때만 "선공"으로 친다.
                // 트라우마는 ApplyDamage가 항상 실패를 돌려주므로 여기서 자연히 걸러진다.
                if (!result.Applied || target.TargetKind != DamageTargetKind.Remnant)
                {
                    continue;
                }

                appliedCount++;
                if (!PlayerAggression)
                {
                    // 한 Encounter 안에서 한 번만 알린다. 매 타격마다 이벤트를 뿌리면 구독자가 중복 처리한다.
                    PlayerAggression = true;
                    GameManager.Instance?.Events.Publish(new PlayerAggressionChanged(encounterId));
                }
            }

            return appliedCount;
        }

        /// <summary>Encounter가 끝나거나 리셋될 때 선공 여부를 초기화한다.</summary>
        public void ResetAggression() => PlayerAggression = false;

        /// <summary>무기를 내려놓은 뒤에는 공격 입력과 판정을 한 진입점에서 막는다.</summary>
        public void SetCombatEnabled(bool enabled) => CombatEnabled = enabled;

        private IEnumerator FlashSwipe()
        {
            swipeVisual.enabled = true;
            yield return new WaitForSeconds(swipeVisibleSeconds);
            swipeVisual.enabled = false;
            swipeRoutine = null;
        }

        private static bool IsCombatAllowed()
        {
            // GameManager가 없는 상황(단독 테스트 씬)에서는 막지 않는다. 테스트가 불필요하게 실패하지 않도록.
            var state = GameManager.Instance == null ? StageState.Explore : GameManager.Instance.StageState;
            return state != StageState.Memory && state != StageState.Failed && state != StageState.Cleared;
        }

        /// <summary>
        /// 부딪힌 콜라이더에서 "피해를 받을 수 있는 주체"를 찾아 올라간다.
        /// </summary>
        /// <remarks>
        /// GetComponentInParent 계열을 쓰는 이유: 콜라이더는 보통 자식 오브젝트에 달려 있고
        /// 실제 체력 스크립트는 부모에 있다. 자식만 보면 대상을 놓친다.
        /// </remarks>
        private static IDamageable FindDamageable(Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            foreach (var component in collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is IDamageable damageable)
                {
                    return damageable;
                }
            }

            return null;
        }
    }
}
