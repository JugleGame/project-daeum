using System;
using Daeume.Contamination;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 3종 잔재 archetype이 공유하는 상태 머신과 연출 규칙. (spec-004, Issue #9)
    ///
    /// 공통 상태는 대기/인지/접근-거리유지/공격/피격/소멸 6종이며, MeleeRemnant/DashRemnant/RangedRemnant가
    /// 이 클래스를 상속해 접근(TickApproach)과 공격(TickAttack)만 archetype에 맞게 구현한다.
    /// 압박 단계·트라우마 주시·Stage 9 모방·Stage 11 Reactive·소멸 흔적은 여기서 한 번만 구현한다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public abstract class RemnantActor : MonoBehaviour, IDamageable
    {
        [SerializeField] private string spawnMarkerId = string.Empty;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer telegraphRenderer;   // 공격 예고 표시

        private Collider2D bodyCollider;
        private Transform target;
        private IDamageable damageTarget;
        private Transform traumaTarget;
        private PressureStage pressureStage;
        private float lastPlayerX;
        private bool wasAttackedByPlayer;   // Reactive 잔재의 "선공당함" 기록
        private Color bodyBaseColor = Color.white;

        /// <summary>피격 점멸이 한 번 켜지거나 꺼져 있는 시간.</summary>
        private const float HitBlinkHalfPeriod = 0.08f;

        /// <summary>점멸이 켜진 순간의 알파. 0으로 두면 사라진 것처럼 보여 맞았다는 신호가 안 된다.</summary>
        private const float HitBlinkAlpha = 0.25f;
        private float nextTargetSearchTime; // 플레이어 탐색 재시도 시각(매 프레임 탐색 방지)

        protected float stateRemaining;      // 현재 상태(혹은 하위 단계)가 끝나기까지 남은 시간
        protected bool attackResolved;       // 이번 공격에서 피해 판정을 이미 했는가

        public abstract RemnantArchetype Archetype { get; }
        protected abstract RemnantDataBase DataBase { get; }

        /// <summary>죽은 뒤 흔적 방향을 굴릴 난수. 테스트가 결정론적으로 바꿔 끼울 수 있다.</summary>
        public Func<float> RandomProvider { get; set; } = () => UnityEngine.Random.value;

        public DamageTargetKind TargetKind => DamageTargetKind.Remnant;
        public RemnantState State { get; private set; }
        public int CurrentHealth { get; private set; }
        public float FacingDirection { get; private set; } = 1f;
        public bool IsTelegraphing { get; private set; }
        public bool TraumaAttentionActive => Profile.WatchesTrauma && traumaTarget != null;
        public bool CanDealDamage => State != RemnantState.Dead && bodyCollider != null && bodyCollider.enabled;
        public string SpawnMarkerId => spawnMarkerId;

        /// <summary>Reactive 잔재가 선공을 기다리며 웅크리거나 길을 비켜주는 중인가. (spec-004 Stage 11)</summary>
        public bool IsYielding { get; private set; }

        /// <summary>Stage 9부터 일부 잔재가 흉내 내는, 플레이어의 이동 방향. (spec-004)</summary>
        public float MirroredPlayerFacingDirection { get; private set; }

        /// <summary>소멸 시 트라우마 방향 흔적을 남겼는가. (spec-004 Stage 7 이후)</summary>
        public bool HasFragmentTrace { get; private set; }

        /// <summary>남긴 흔적이 가리키는 방향(트라우마 쪽 부호). 흔적이 없으면 0.</summary>
        public float FragmentTraceDirection { get; private set; }

        /// <summary>선공 없이는 공격 상태로 들어가지 못하게 막는 조건. Reactive가 아니면 항상 true.</summary>
        protected bool CanInitiateAttack => !DataBase.Reactive || wasAttackedByPlayer;

        // 현재 압박 단계에 해당하는 수치 묶음(인지 거리 배수, 예고 시간 배수, 트라우마 주시 여부 등)
        protected RemnantPressureProfile Profile => DataBase.GetProfile(pressureStage);

        protected virtual void Awake()
        {
            if (bodyRenderer != null) bodyBaseColor = bodyRenderer.color;

            bodyCollider = GetComponent<Collider2D>();
            ForceTriggerBody();
            CurrentHealth = DataBase.MaxHealth;
            EnterState(RemnantState.Idle);
        }

        /// <summary>
        /// 잔재의 몸 콜라이더는 반드시 트리거여야 한다. 프리팹 설정에 의존하지 않고 코드로 강제한다.
        /// </summary>
        /// <remarks>
        /// 실제로 겪은 버그(#12): 잔재는 Rigidbody2D 없이 transform으로 움직인다. 그러면 유니티는
        /// 그 콜라이더를 "정적(static) 콜라이더"로 취급하는데, 정적 콜라이더가 움직이면서
        /// 동적 몸(플레이어)을 파고들면 물리 엔진이 겹침을 푸느라 최소 축 방향으로 강하게 밀어낸다.
        /// 잔재 몸통은 높이 0.5로 납작해서 그 최소 축이 위쪽이라, 플레이어가 하늘로 솟구쳤다.
        /// 공중에 있는 동안은 접지가 아니라 점프가 아예 나가지 않아 "Space 키가 안 먹는" 증상으로 보였다.
        ///
        /// 트리거로 바꿔도 잃는 것이 없다. 잔재의 접촉 피해는 충돌이 아니라 거리(TargetInRange)로 판정하고,
        /// 플레이어 공격 탐지(PlayerCombat)는 OverlapCircleAll이라 트리거도 그대로 잡는다.
        /// 오히려 플레이어가 잔재 머리 위에 올라서던 것도 함께 막힌다(접지 검사는 트리거를 제외한다).
        ///
        /// Stage 1은 잔재를 실제로 소환하지 않아 이 버그가 드러나지 않았고, Stage 2에서 처음 나타났다.
        /// </remarks>
        private void ForceTriggerBody()
        {
            if (bodyCollider != null) bodyCollider.isTrigger = true;
        }

        // Update에서 Tick을 부르되, 실제 로직은 Tick에 모아 뒀다.
        // 테스트가 시간을 직접 넣어 가며 행동을 검증할 수 있어 좋은 구조다.
        private void Update() => Tick(Time.deltaTime);

        protected void ResetRuntimeState()
        {
            CurrentHealth = DataBase.MaxHealth;
            wasAttackedByPlayer = false;
            IsYielding = false;
            HasFragmentTrace = false;
            FragmentTraceDirection = 0f;
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            bodyCollider.enabled = true;
            ForceTriggerBody();
            if (bodyRenderer != null) bodyRenderer.color = bodyBaseColor;
            EnterState(RemnantState.Idle);
        }

        public void SetTarget(Transform value)
        {
            target = value;
            damageTarget = FindDamageable(value);
            lastPlayerX = value == null ? 0f : value.position.x;
        }

        public void SetTraumaTarget(Transform value) => traumaTarget = value;

        /// <summary>압박 단계를 바꾼다. 적 유형이 늘어나는 게 아니라 기존 수치가 바뀐다(spec-004).</summary>
        public void SetPressure(PressureStage value)
        {
            pressureStage = value;
            UpdateTraumaFacing();
        }

        public void Tick(float deltaTime)
        {
            if (State == RemnantState.Dead)
            {
                return;
            }

            if (target == null)
            {
                // 일정 간격으로만 대상을 다시 찾는다(매 프레임 씬 전체 검색 방지).
                if (Time.time >= nextTargetSearchTime)
                {
                    nextTargetSearchTime = Time.time + 0.5f;
                    FindPlayerTarget();
                }
            }

            var step = Mathf.Max(0f, deltaTime);
            switch (State)
            {
                case RemnantState.Idle:
                    UpdateTraumaFacing();   // Echo 이상에서는 가만히 있어도 트라우마 쪽을 본다
                    if (TargetInRange(DataBase.DetectionRange * Profile.DetectionRangeMultiplier)) EnterState(RemnantState.Alert);
                    break;
                case RemnantState.Alert:
                    FaceTarget();
                    stateRemaining -= step;
                    if (stateRemaining <= 0f) EnterState(RemnantState.Approach);
                    break;
                case RemnantState.Approach:
                    TickApproach(step);
                    break;
                case RemnantState.Attack:
                    TickAttack(step);
                    break;
                case RemnantState.Hit:
                    stateRemaining -= step;
                    TickHitBlink();
                    if (stateRemaining <= 0f)
                    {
                        EnterState(TargetInRange(DataBase.DetectionRange * Profile.DetectionRangeMultiplier)
                            ? RemnantState.Alert
                            : RemnantState.Idle);
                    }
                    break;
            }
        }

        public DamageResult ApplyDamage(DamageRequest request)
        {
            if (State == RemnantState.Dead || request.Amount <= 0)
            {
                // 이미 죽은 적이 다시 피해를 받으면 사망 처리가 두 번 일어나 Wave 계산이 어긋난다.
                return new DamageResult(false, 0);
            }

            wasAttackedByPlayer = true;   // Reactive 잔재는 이 순간부터 반응할 수 있다.
            var applied = Mathf.Min(CurrentHealth, request.Amount);
            CurrentHealth -= applied;
            if (CurrentHealth <= 0)
            {
                Die();
            }
            else
            {
                EnterState(RemnantState.Hit);
            }

            return new DamageResult(applied > 0, applied);
        }

        /// <summary>접근/거리유지 단계. archetype마다 다르므로 하위 클래스가 구현한다.</summary>
        protected abstract void TickApproach(float deltaTime);

        /// <summary>공격 단계(예고 → 판정 → 후딜). 근접·원거리형은 기본 구현을 공유한다.</summary>
        protected virtual void TickAttack(float deltaTime)
        {
            FaceTarget();
            stateRemaining -= deltaTime;

            if (!attackResolved && stateRemaining <= 0f)
            {
                SetTelegraph(false);

                // 예고가 끝난 시점에 아직 사거리 안이면 피해를 준다.
                TryDealContactDamage();

                attackResolved = true;
                stateRemaining = DataBase.AttackRecoverySeconds;
                return;
            }

            if (attackResolved && stateRemaining <= 0f)
            {
                EnterState(RemnantState.Approach);
            }
        }

        protected virtual void EnterState(RemnantState value)
        {
            State = value;
            attackResolved = false;
            IsYielding = false;
            SetTelegraph(false);
            if (value != RemnantState.Hit) SetBodyAlpha(1f);
            switch (value)
            {
                case RemnantState.Alert:
                    stateRemaining = DataBase.AlertSeconds;
                    break;
                case RemnantState.Attack:
                    // 압박 단계 배수를 곱한 뒤에도 최소 0.05초는 남긴다. 예고 없는 공격은 공정성 규칙 위반이다.
                    stateRemaining = Mathf.Max(0.05f, DataBase.AttackTelegraphSeconds * Profile.TelegraphMultiplier);
                    SetTelegraph(true);
                    break;
                case RemnantState.Hit:
                    stateRemaining = DataBase.HitStunSeconds;
                    break;
            }
        }

        /// <summary>피격 경직 동안 몸을 깜빡여 맞았다는 사실을 알린다.</summary>
        /// <remarks>
        /// 왜 색이 아니라 알파인가: 잔재 스프라이트는 픽셀 평균이 rgb(31, 24, 28)인 검은 실루엣이다.
        /// SpriteRenderer.color는 곱셈이라 검은 픽셀에 빨강을 곱해도 검정 그대로다.
        /// 알파를 낮추면 밝은 하늘 배경이 비쳐 실루엣이 옅어지므로 실제로 눈에 띈다.
        ///
        /// 남은 경직 시간을 반주기로 나눈 몫의 홀짝으로 켜고 끈다. 별도 타이머를 두지 않아도
        /// 경직이 끝나는 순간 점멸도 함께 끝난다.
        /// </remarks>
        private void TickHitBlink()
        {
            if (bodyRenderer == null) return;
            var lit = Mathf.FloorToInt(Mathf.Max(0f, stateRemaining) / HitBlinkHalfPeriod) % 2 == 0;
            SetBodyAlpha(lit ? HitBlinkAlpha : 1f);
        }

        private void SetBodyAlpha(float value)
        {
            if (bodyRenderer == null) return;
            var color = bodyRenderer.color;
            bodyRenderer.color = new Color(color.r, color.g, color.b, value);
        }

        private void Die()
        {
            CurrentHealth = 0;
            EnterState(RemnantState.Dead);
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

            // 콜라이더를 꺼서 시체가 계속 피해를 주거나 길을 막는 것을 방지한다.
            bodyCollider.enabled = false;
            if (bodyRenderer != null) bodyRenderer.color = new Color(0.2f, 0.2f, 0.24f, 0.35f);

            ResolveFragmentTrace();
            OnDied();
        }

        /// <summary>소멸 시 알린다. 하위 클래스가 자기 타입으로 좁힌 이벤트를 노출한다(Encounter가 구독).</summary>
        protected virtual void OnDied()
        {
        }

        /// <summary>Stage 7 이후 일정 비율로 트라우마 방향 흔적을 남긴다. (spec-004)</summary>
        private void ResolveFragmentTrace()
        {
            if (DataBase.StageNumber < 7 || DataBase.FragmentTraceTowardTraumaRatio <= 0f || traumaTarget == null)
            {
                return;
            }

            if (RandomProvider() > DataBase.FragmentTraceTowardTraumaRatio)
            {
                return;
            }

            HasFragmentTrace = true;
            var offset = traumaTarget.position.x - transform.position.x;
            FragmentTraceDirection = Mathf.Approximately(offset, 0f) ? 1f : Mathf.Sign(offset);
        }

        // 위아래로 얼마나 떨어져 있어도 "닿는다"고 볼지의 한계.
        protected const float MaxVerticalReach = 1.5f;

        protected bool TargetInRange(float range)
        {
            return target != null &&
                Mathf.Abs(target.position.x - transform.position.x) <= range &&
                Mathf.Abs(target.position.y - transform.position.y) <= MaxVerticalReach;
        }

        protected float DistanceToTarget => target == null ? float.PositiveInfinity : Mathf.Abs(target.position.x - transform.position.x);

        protected bool HasTarget => target != null;

        protected float TargetX => target == null ? transform.position.x : target.position.x;

        protected Transform TraumaTarget => traumaTarget;

        /// <summary>사거리 안이면 접촉 피해를 준다(근접·원거리형 공격 판정, 돌진형 충돌 판정이 공유한다).</summary>
        protected bool TryDealContactDamage(float rangeTolerance = 0.2f)
        {
            if (!CanDealDamage || !TargetInRange(DataBase.AttackRange + rangeTolerance))
            {
                return false;
            }

            var result = damageTarget?.ApplyDamage(new DamageRequest(DataBase.ContactDamage, gameObject)) ?? default;
            return result.Applied;
        }

        /// <summary>Stage 9부터 일부 잔재는 상대 위치 대신 플레이어의 실제 이동 방향을 그대로 흉내 낸다.</summary>
        protected void FaceTarget()
        {
            if (target == null) return;

            if (DataBase.MimicsPlayerMotion && DataBase.StageNumber >= 9)
            {
                var delta = target.position.x - lastPlayerX;
                lastPlayerX = target.position.x;
                if (!Mathf.Approximately(delta, 0f))
                {
                    MirroredPlayerFacingDirection = Mathf.Sign(delta);
                }

                SetFacing(MirroredPlayerFacingDirection);
                return;
            }

            SetFacing(target.position.x - transform.position.x);
        }

        /// <summary>Echo 이상 압박에서 트라우마 쪽을 바라본다. (spec-004의 복선 연출)</summary>
        private void UpdateTraumaFacing()
        {
            if (Profile.WatchesTrauma && traumaTarget != null)
            {
                SetFacing(traumaTarget.position.x - transform.position.x);
            }
        }

        protected void SetFacing(float horizontalDelta)
        {
            if (Mathf.Approximately(horizontalDelta, 0f)) return;
            FacingDirection = Mathf.Sign(horizontalDelta);
            if (bodyRenderer != null) bodyRenderer.flipX = FacingDirection < 0f;
        }

        protected void SetTelegraph(bool value)
        {
            IsTelegraphing = value;
            if (telegraphRenderer != null) telegraphRenderer.enabled = value;
        }

        /// <summary>Reactive 잔재가 선공 없이 접근 범위에 들어왔을 때, 공격 대신 멈춰 서서 비켜준다.</summary>
        protected void EnterYielding()
        {
            IsYielding = true;
        }

        private void FindPlayerTarget()
        {
            // 플레이어는 Persistent 씬에 있어 프리팹에 미리 연결해 둘 수 없다. 그래서 실행 중에 찾는다.
            var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IDamageable candidate && candidate.TargetKind == DamageTargetKind.Player)
                {
                    SetTarget(behaviours[index].transform);
                    return;
                }
            }
        }

        private static IDamageable FindDamageable(Transform value)
        {
            if (value == null) return null;
            foreach (var behaviour in value.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IDamageable candidate && candidate.TargetKind == DamageTargetKind.Player)
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
