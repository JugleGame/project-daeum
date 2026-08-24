using Daeume.Core;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>공통 Remnant state machine을 Animator에 투영한다.</summary>
    [RequireComponent(typeof(Animator))]
    public sealed class RemnantAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private RemnantActor actor;
        private bool stateApplied;

        public RemnantState CurrentState { get; private set; }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (actor == null) actor = GetComponent<RemnantActor>();
        }

        private void Update() => Tick();

        public void Tick()
        {
            var value = actor == null ? RemnantState.Idle : actor.State;
            if (stateApplied && CurrentState == value) return;
            CurrentState = value;
            stateApplied = true;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetInteger(CharacterAnimationParameters.State, (int)value);
            animator.Play(value.ToString(), 0, 0f);
        }
    }
}
