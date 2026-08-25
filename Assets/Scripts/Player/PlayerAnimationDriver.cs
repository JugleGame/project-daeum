using Daeume.Core;
using UnityEngine;

namespace Daeume.Player
{
    public enum PlayerAnimationState
    {
        Idle,
        Move,
        Airborne,
        Attack,
        Damaged,
        Dead,
        Grab
    }

    /// <summary>플레이어 gameplay signal을 Animator state에만 투영한다.</summary>
    [RequireComponent(typeof(Animator), typeof(PlayerController), typeof(PlayerHealth))]
    public sealed class PlayerAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController controller;
        [SerializeField] private PlayerCombat combat;
        [SerializeField] private PlayerHealth health;
        [SerializeField, Min(0f)] private float attackHoldSeconds = 0.67f;
        [SerializeField, Min(0f)] private float damagedHoldSeconds = 0.18f;

        /// <summary>피격 동안 덧입히는 색. 알파는 Animator가 쥐고 있어 rgb만 바꾼다.</summary>
        private static readonly Color DamagedFlashColor = new(1f, 0.35f, 0.35f, 1f);

        private Color visualBaseColor = Color.white;
        private bool flashApplied;
        private int observedAttackSequence;
        private int observedDamageSequence;
        private float attackRemaining;
        private float damagedRemaining;
        private bool stateApplied;

        public PlayerAnimationState CurrentState { get; private set; }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (controller == null) controller = GetComponent<PlayerController>();
            if (combat == null) combat = GetComponent<PlayerCombat>();
            if (health == null) health = GetComponent<PlayerHealth>();
            observedAttackSequence = combat == null ? 0 : combat.AttackSequence;
            observedDamageSequence = health == null ? 0 : health.DamageSequence;
            var renderer = controller == null ? null : controller.VisualRenderer;
            if (renderer != null) visualBaseColor = renderer.color;
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime)
        {
            var step = Mathf.Max(0f, deltaTime);
            attackRemaining = Mathf.Max(0f, attackRemaining - step);
            damagedRemaining = Mathf.Max(0f, damagedRemaining - step);

            if (combat != null && combat.AttackSequence != observedAttackSequence)
            {
                observedAttackSequence = combat.AttackSequence;
                attackRemaining = attackHoldSeconds;
            }

            if (health != null && health.DamageSequence != observedDamageSequence)
            {
                observedDamageSequence = health.DamageSequence;
                damagedRemaining = damagedHoldSeconds;
            }

            ApplyDamagedFlash(damagedRemaining > 0f);

            if (health != null && health.CurrentHealth <= 0)
            {
                ApplyState(PlayerAnimationState.Dead);
            }
            else if (damagedRemaining > 0f)
            {
                ApplyState(PlayerAnimationState.Damaged);
            }
            else if (attackRemaining > 0f)
            {
                ApplyState(PlayerAnimationState.Attack);
            }
            else if (controller != null && controller.IsGrabbing)
            {
                ApplyState(PlayerAnimationState.Grab);
            }
            else if (controller != null && !controller.IsGrounded)
            {
                ApplyState(PlayerAnimationState.Airborne);
            }
            else if (controller != null && !Mathf.Approximately(controller.HorizontalInput, 0f))
            {
                ApplyState(PlayerAnimationState.Move);
            }
            else
            {
                ApplyState(PlayerAnimationState.Idle);
            }
        }

        /// <summary>피격 연출이 흐르는 동안 몸을 붉게 물들인다.</summary>
        /// <remarks>
        /// 알파는 건드리지 않는다. 모든 Player 클립이 Visual.m_Color.a를 애니메이션하고 있어
        /// 여기서 알파를 쓰면 Animator와 매 프레임 싸운다. rgb는 어떤 클립도 잡지 않으므로
        /// 코드가 칠한 값이 그대로 남는다.
        ///
        /// 상태가 바뀔 때만 대입하는 이유는, 매 프레임 색을 쓰면 다른 연출이 색을 바꿔도
        /// 즉시 되돌려 버리기 때문이다.
        /// </remarks>
        private void ApplyDamagedFlash(bool active)
        {
            if (flashApplied == active) return;
            flashApplied = active;

            var renderer = controller == null ? null : controller.VisualRenderer;
            if (renderer == null) return;

            var target = active ? DamagedFlashColor : visualBaseColor;
            renderer.color = new Color(target.r, target.g, target.b, renderer.color.a);
        }

        private void ApplyState(PlayerAnimationState value)
        {
            if (stateApplied && CurrentState == value) return;
            CurrentState = value;
            stateApplied = true;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetInteger(CharacterAnimationParameters.State, (int)value);
            animator.Play(value.ToString(), 0, 0f);
        }
    }
}
