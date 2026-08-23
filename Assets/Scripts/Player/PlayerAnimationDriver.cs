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
        [SerializeField, Min(0f)] private float attackHoldSeconds = 0.12f;
        [SerializeField, Min(0f)] private float damagedHoldSeconds = 0.18f;

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
