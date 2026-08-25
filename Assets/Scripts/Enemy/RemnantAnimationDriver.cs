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

        /// <summary>
        /// 소멸 연출(Dead 클립)이 끝나는 시점에 AnimationEvent로 호출된다.
        /// </summary>
        /// <remarks>
        /// 예전에는 죽은 잔재가 콜라이더만 끈 채 맵에 그대로 남아 화면이 지저분해졌다.
        /// 파괴를 클립 끝에 맡기는 이유는 연출이 끝나기 전에 지우면 줄어드는 그림이 잘리기 때문이다.
        /// 실제로 죽은 상태인지 다시 확인하는 것은, 클립을 다른 상태에서 잘못 재생했을 때
        /// 살아 있는 잔재가 사라지는 사고를 막기 위해서다.
        /// </remarks>
        public void DespawnAfterDeath()
        {
            if (actor != null && actor.State != RemnantState.Dead) return;
            Destroy(gameObject);
        }
    }
}
