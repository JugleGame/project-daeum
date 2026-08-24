using Daeume.Core;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public enum TraumaAnimationState
    {
        Idle,
        Chase,
        Attack
    }

    /// <summary>director가 실제로 전달한 chase directive를 Trauma Animator에 투영한다.</summary>
    [RequireComponent(typeof(Animator), typeof(TraumaChaseActor))]
    public sealed class TraumaAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private TraumaChaseActor actor;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Min(0.01f)] private float chaseSignalHoldSeconds = 0.15f;
        [SerializeField, Min(0.01f)] private float attackHoldSeconds = 0.4f;

        private int observedDirectiveCount;
        private float chaseRemaining;
        private float attackRemaining;
        private bool stateApplied;
        private bool facingLeft = true;

        public TraumaAnimationState CurrentState { get; private set; }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (actor == null) actor = GetComponent<TraumaChaseActor>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            observedDirectiveCount = actor == null ? 0 : actor.AppliedDirectiveCount;
        }

        private void OnEnable()
        {
            GameManager.Instance?.Events.Unsubscribe<TraumaGrabStarted>(HandleGrabStarted);
            GameManager.Instance?.Events.Subscribe<TraumaGrabStarted>(HandleGrabStarted);
        }

        private void OnDisable() => GameManager.Instance?.Events.Unsubscribe<TraumaGrabStarted>(HandleGrabStarted);

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime = 0f)
        {
            chaseRemaining = Mathf.Max(0f, chaseRemaining - Mathf.Max(0f, deltaTime));
            attackRemaining = Mathf.Max(0f, attackRemaining - Mathf.Max(0f, deltaTime));
            if (actor != null && actor.AppliedDirectiveCount != observedDirectiveCount)
            {
                observedDirectiveCount = actor.AppliedDirectiveCount;
                if (actor.LastDirective.Speed > 0f) chaseRemaining = chaseSignalHoldSeconds;
                UpdateFacing(actor.LastDirective);
            }

            ApplyState(attackRemaining > 0f
                ? TraumaAnimationState.Attack
                : chaseRemaining > 0f
                    ? TraumaAnimationState.Chase
                    : TraumaAnimationState.Idle);
        }

        private void HandleGrabStarted(TraumaGrabStarted signal) => attackRemaining = attackHoldSeconds;

        private void UpdateFacing(ChaseDirectiveIssued directive)
        {
            if (spriteRenderer == null) return;
            var movement = actor == null ? 0f : actor.LastHorizontalMovement;
            if (!Mathf.Approximately(movement, 0f))
            {
                facingLeft = movement < 0f;
            }
            else
            {
                var playerOffset = directive.PlayerPosition.x - directive.PursuerPosition.x;
                if (!Mathf.Approximately(playerOffset, 0f)) facingLeft = playerOffset < 0f;
            }

            ApplyRendererFacing();
        }

        private void ApplyRendererFacing()
        {
            if (spriteRenderer == null) return;

            // Unity renderer 비교 캡처로 확인한 모든 현재 Trauma frame의 authored-facing은 왼쪽이다.
            spriteRenderer.flipX = !facingLeft;
        }

        private void ApplyState(TraumaAnimationState value)
        {
            if (stateApplied && CurrentState == value) return;
            CurrentState = value;
            stateApplied = true;
            ApplyRendererFacing();
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetInteger(CharacterAnimationParameters.State, (int)value);
            animator.Play(value.ToString(), 0, 0f);
        }
    }
}
