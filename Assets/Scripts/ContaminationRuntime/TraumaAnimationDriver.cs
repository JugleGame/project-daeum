using Daeume.Core;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public enum TraumaAnimationState
    {
        Idle,
        Chase
    }

    /// <summary>director가 실제로 전달한 chase directive를 Trauma Animator에 투영한다.</summary>
    [RequireComponent(typeof(Animator), typeof(TraumaChaseActor))]
    public sealed class TraumaAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private TraumaChaseActor actor;
        [SerializeField, Min(0.01f)] private float chaseSignalHoldSeconds = 0.15f;

        private int observedDirectiveCount;
        private float chaseRemaining;
        private bool stateApplied;

        public TraumaAnimationState CurrentState { get; private set; }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (actor == null) actor = GetComponent<TraumaChaseActor>();
            observedDirectiveCount = actor == null ? 0 : actor.AppliedDirectiveCount;
        }

        private void Update() => Tick(Time.deltaTime);

        public void Tick(float deltaTime = 0f)
        {
            chaseRemaining = Mathf.Max(0f, chaseRemaining - Mathf.Max(0f, deltaTime));
            if (actor != null && actor.AppliedDirectiveCount != observedDirectiveCount)
            {
                observedDirectiveCount = actor.AppliedDirectiveCount;
                if (actor.LastDirective.Speed > 0f) chaseRemaining = chaseSignalHoldSeconds;
            }

            ApplyState(chaseRemaining > 0f ? TraumaAnimationState.Chase : TraumaAnimationState.Idle);
        }

        private void ApplyState(TraumaAnimationState value)
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
