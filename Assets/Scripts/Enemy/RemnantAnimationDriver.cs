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

        /// <summary>죽은 뒤 오브젝트를 지우기까지의 시간. Dead 클립 길이와 맞춘다.</summary>
        [SerializeField, Min(0f)] private float despawnDelaySeconds = 0.6f;

        private bool stateApplied;
        private float despawnRemaining = -1f;

        public RemnantState CurrentState { get; private set; }

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (actor == null) actor = GetComponent<RemnantActor>();
        }

        private void Update()
        {
            Tick();
            TickDespawn(Time.deltaTime);
        }

        public void Tick()
        {
            var value = actor == null ? RemnantState.Idle : actor.State;
            if (stateApplied && CurrentState == value) return;
            CurrentState = value;
            stateApplied = true;
            if (value == RemnantState.Dead) despawnRemaining = despawnDelaySeconds;
            if (animator == null || animator.runtimeAnimatorController == null) return;
            animator.SetInteger(CharacterAnimationParameters.State, (int)value);
            animator.Play(value.ToString(), 0, 0f);
        }

        /// <summary>죽은 뒤 남은 시간을 줄이고, 다 되면 오브젝트를 지운다.</summary>
        /// <remarks>
        /// 왜 AnimationEvent가 아니라 타이머인가:
        /// AnimationEvent는 Animator가 클립을 끝까지 재생해야만 발화한다. Animator가 꺼지거나
        /// 컬링되거나 상태가 중간에 바뀌면 이벤트가 영영 오지 않고, 죽은 잔재가 씬에 그대로
        /// 쌓인다. 타이머는 그런 경우에도 반드시 정리된다.
        ///
        /// Time.deltaTime을 쓰는 이유는 연출이 같은 시간축(스케일된 시간)으로 흐르기 때문이다.
        /// 일시정지 중에는 페이드도 함께 멈추므로, 반쯤 보이는 채로 사라지는 일이 없다.
        /// </remarks>
        public void TickDespawn(float deltaTime)
        {
            if (despawnRemaining < 0f) return;
            despawnRemaining -= Mathf.Max(0f, deltaTime);
            if (despawnRemaining > 0f) return;

            despawnRemaining = -1f;
            DespawnAfterDeath();
        }

        /// <summary>죽은 잔재를 씬에서 지운다.</summary>
        /// <remarks>
        /// 예전에는 콜라이더만 끈 채 맵에 그대로 남아 화면이 지저분해졌다.
        /// 실제로 죽은 상태인지 다시 확인하는 것은, 상태가 되돌아간 잔재를 지우지 않기 위해서다.
        ///
        /// 풀링하지 않는 이유: 스테이지 한 판에 생기는 잔재는 Encounter 3개를 합쳐 9마리다.
        /// 이 규모에서 풀은 재사용 시 상태 초기화 버그만 늘린다.
        /// </remarks>
        public void DespawnAfterDeath()
        {
            if (actor != null && actor.State != RemnantState.Dead) return;
            Destroy(gameObject);
        }
    }
}
