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

        // 한 번의 공격에서 같은 대상을 두 번 때리지 않게 기록해 두는 집합.
        // 적의 몸에 콜라이더가 여러 개면(몸통+예고 표시) 같은 적이 두 번 검출되기 때문에 꼭 필요하다.
        private readonly HashSet<IDamageable> damaged = new();
        private InputAction attack;
        private PlayerController controller;
        private float attackOriginOffsetX;   // 공격 원점의 좌우 거리(절댓값). 바라보는 방향에 따라 부호만 바꾼다.

        public bool PlayerAggression { get; private set; }

        private void Awake()
        {
            var playerInput = GetComponentInParent<PlayerInput>();
            attack = attackAction == null ? playerInput?.actions?.FindAction(AttackActionName) : attackAction.action;
            controller = GetComponentInParent<PlayerController>();
            if (attackOrigin != null) attackOriginOffsetX = Mathf.Abs(attackOrigin.localPosition.x);
        }

        private void OnEnable() => attack?.Enable();
        private void OnDisable() => attack?.Disable();

        private void Update()
        {
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
                Attack();
            }
        }

        /// <summary>공격을 1회 수행하고, 실제로 피해를 입힌 잔재 수를 돌려준다.</summary>
        public int Attack()
        {
            // spec-005: 회상 재생 중에는 전투 피해가 발생하지 않는다.
            // spec-001: 실패/클리어 상태에서도 조작이 진행에 개입하면 안 된다.
            // 상태 확인을 공격 진입점 한 곳에서만 하므로, 입력·테스트·AI 어느 경로로 불러도 같은 규칙이 적용된다.
            if (!IsCombatAllowed())
            {
                return 0;
            }

            damaged.Clear();
            var center = attackOrigin == null ? transform.position : attackOrigin.position;
            var hits = Physics2D.OverlapCircleAll(center, attackRadius, targetMask);
            var appliedCount = 0;

            for (var index = 0; index < hits.Length; index++)
            {
                var target = FindDamageable(hits[index]);
                // damaged.Add가 false면 이미 이번 공격에서 처리한 대상이라는 뜻이다.
                if (target == null || !damaged.Add(target))
                {
                    continue;
                }

                var result = target.ApplyDamage(new DamageRequest(damage, gameObject));

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
