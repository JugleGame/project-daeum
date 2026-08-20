using System;
using Daeume.Contamination;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>잔재가 가질 수 있는 공통 상태 6종. (spec-004가 목록을 고정한다)</summary>
    public enum RemnantState
    {
        Idle,      // 대기
        Alert,     // 인지(플레이어를 발견하고 잠시 반응)
        Approach,  // 접근
        Attack,    // 예고 후 공격
        Hit,       // 피격 경직
        Dead       // 소멸
    }

    /// <summary>
    /// 근접형 잔재. 8일 슬라이스에서 만드는 유일한 적 유형이다. (spec-004)
    ///
    /// 잔재는 단순한 몬스터가 아니라 "주인공에게서 떨어져 나온 기억의 파편"이라는 설정이다.
    /// 그래서 압박 단계가 오르면 트라우마 쪽을 바라보는 행동이 들어간다(단순 난이도 상승이 아니다).
    ///
    /// 수치는 코드가 아니라 MeleeRemnantData(에셋)가 소유한다.
    /// 스펙이 "단계별 값은 적 데이터가 선언하며 코드 분기로 두지 않는다"고 요구하기 때문이다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class MeleeRemnant : MonoBehaviour, IDamageable
    {
        [SerializeField] private MeleeRemnantData data;
        [SerializeField] private string spawnMarkerId = string.Empty;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer telegraphRenderer;   // 공격 예고 표시

        private Collider2D bodyCollider;
        private Transform target;
        private IDamageable damageTarget;
        private Transform traumaTarget;
        private MeleeRemnantData fallbackData;
        private PressureStage pressureStage;
        private float stateRemaining;      // 현재 상태가 끝나기까지 남은 시간
        private bool attackResolved;       // 이번 공격에서 피해 판정을 이미 했는가
        private float nextTargetSearchTime; // 플레이어 탐색 재시도 시각(매 프레임 탐색 방지)

        public DamageTargetKind TargetKind => DamageTargetKind.Remnant;
        public RemnantState State { get; private set; }
        public int CurrentHealth { get; private set; }
        public float FacingDirection { get; private set; } = 1f;
        public bool IsTelegraphing { get; private set; }
        public bool TraumaAttentionActive => Profile.WatchesTrauma && traumaTarget != null;
        public bool CanDealDamage => State != RemnantState.Dead && bodyCollider != null && bodyCollider.enabled;
        public string SpawnMarkerId => spawnMarkerId;
        public MeleeRemnantData Data => data;

        /// <summary>소멸 시 알린다. Encounter가 이 신호로 다음 Wave를 시작한다.</summary>
        public event Action<MeleeRemnant> Died;

        // 현재 압박 단계에 해당하는 수치 묶음(인지 거리 배수, 예고 시간 배수, 트라우마 주시 여부 등)
        private RemnantPressureProfile Profile => DataOrDefault.GetProfile(pressureStage);

        /// <summary>
        /// 데이터 에셋이 비어 있어도 죽지 않도록 임시 기본값을 만들어 쓴다.
        /// </summary>
        /// <remarks>
        /// 테스트에서 적을 코드로 만들 때 데이터 에셋을 매번 붙이지 않아도 되게 하는 장치다.
        /// HideAndDontSave로 표시해 프로젝트에 저장되지 않게 하고, 파괴 시 직접 정리한다.
        /// </remarks>
        private MeleeRemnantData DataOrDefault
        {
            get
            {
                if (data != null)
                {
                    return data;
                }

                if (fallbackData == null)
                {
                    fallbackData = ScriptableObject.CreateInstance<MeleeRemnantData>();
                    fallbackData.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallbackData;
            }
        }

        private void Awake()
        {
            bodyCollider = GetComponent<Collider2D>();
            CurrentHealth = DataOrDefault.MaxHealth;
            EnterState(RemnantState.Idle);
        }

        // Update에서 Tick을 부르되, 실제 로직은 Tick에 모아 뒀다.
        // 테스트가 시간을 직접 넣어 가며 행동을 검증할 수 있어 좋은 구조다.
        private void Update() => Tick(Time.deltaTime);

        private void OnDestroy()
        {
            if (fallbackData == null) return;
            // 에디터 모드에서는 Destroy가 즉시 동작하지 않아 DestroyImmediate를 써야 한다.
            if (Application.isPlaying) Destroy(fallbackData);
            else DestroyImmediate(fallbackData);
        }

        public void SetData(MeleeRemnantData value)
        {
            data = value;
            CurrentHealth = DataOrDefault.MaxHealth;
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            bodyCollider.enabled = true;
            EnterState(RemnantState.Idle);
        }

        public void SetTarget(Transform value)
        {
            target = value;
            damageTarget = FindDamageable(value);
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
                // 수정: 예전에는 대상이 없을 때 매 프레임 씬 전체를 훑었다(FindObjectsByType).
                // 적이 여러 마리면 프레임마다 씬 전수 검색이 반복돼 눈에 띄게 느려진다.
                // 일정 간격으로만 다시 찾도록 제한한다.
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
                    if (TargetInRange(DataOrDefault.DetectionRange)) EnterState(RemnantState.Alert);
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
                    if (stateRemaining <= 0f)
                    {
                        EnterState(TargetInRange(DataOrDefault.DetectionRange) ? RemnantState.Alert : RemnantState.Idle);
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

        private void TickApproach(float deltaTime)
        {
            if (target == null)
            {
                EnterState(RemnantState.Idle);
                return;
            }

            FaceTarget();
            if (TargetInRange(DataOrDefault.AttackRange))
            {
                EnterState(RemnantState.Attack);
                return;
            }

            // 좌우로만 이동한다. 근접형은 점프하지 않는 단순 추적이며, 이게 슬라이스 범위다.
            var position = transform.position;
            position.x = Mathf.MoveTowards(
                position.x,
                target.position.x,
                DataOrDefault.MoveSpeed * Profile.MoveSpeedMultiplier * deltaTime);
            transform.position = position;
        }

        /// <summary>공격: 예고 → 판정 → 후딜 순서로 진행된다.</summary>
        private void TickAttack(float deltaTime)
        {
            FaceTarget();
            stateRemaining -= deltaTime;

            if (!attackResolved && stateRemaining <= 0f)
            {
                SetTelegraph(false);

                // 예고가 끝난 시점에 아직 사거리 안이면 피해를 준다.
                // +0.2f 여유는 예고 도중 살짝 움직인 경우를 위한 관용값이다.
                // (예고를 보고 피했는데 맞는 느낌을 주지 않으려면 이 값을 키우지 않는 것이 좋다.)
                if (CanDealDamage && TargetInRange(DataOrDefault.AttackRange + 0.2f))
                {
                    damageTarget?.ApplyDamage(new DamageRequest(DataOrDefault.ContactDamage, gameObject));
                }

                attackResolved = true;
                stateRemaining = DataOrDefault.AttackRecoverySeconds;
                return;
            }

            if (attackResolved && stateRemaining <= 0f)
            {
                EnterState(RemnantState.Approach);
            }
        }

        private void EnterState(RemnantState value)
        {
            State = value;
            attackResolved = false;
            SetTelegraph(false);
            switch (value)
            {
                case RemnantState.Alert:
                    stateRemaining = DataOrDefault.AlertSeconds;
                    break;
                case RemnantState.Attack:
                    // Mathf.Max(0.05f, ...): 예고 시간이 0이 되지 않게 하한을 둔다.
                    // spec-004의 "압박 단계가 올라도 예고 시간은 0이 될 수 없다"를 보장하는 부분이다.
                    // 예고 없는 공격은 플레이어가 대응할 수 없어 공정성 규칙 위반이 된다.
                    stateRemaining = Mathf.Max(0.05f, DataOrDefault.AttackTelegraphSeconds * Profile.TelegraphMultiplier);
                    SetTelegraph(true);
                    break;
                case RemnantState.Hit:
                    stateRemaining = DataOrDefault.HitStunSeconds;
                    break;
            }
        }

        private void Die()
        {
            CurrentHealth = 0;
            EnterState(RemnantState.Dead);
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();

            // 콜라이더를 꺼서 시체가 계속 피해를 주거나 길을 막는 것을 방지한다.
            bodyCollider.enabled = false;

            if (bodyRenderer != null) bodyRenderer.color = new Color(0.2f, 0.2f, 0.24f, 0.35f);
            Died?.Invoke(this);
        }

        // 위아래로 얼마나 떨어져 있어도 "닿는다"고 볼지의 한계.
        // 이 값이 없으면 발판 위아래로 떨어져 있어도 공격이 성립해 이상하게 맞는다.
        private const float MaxVerticalReach = 1.5f;

        private bool TargetInRange(float range)
        {
            return target != null &&
                Mathf.Abs(target.position.x - transform.position.x) <= range &&
                Mathf.Abs(target.position.y - transform.position.y) <= MaxVerticalReach;
        }

        private void FaceTarget()
        {
            if (target != null) SetFacing(target.position.x - transform.position.x);
        }

        /// <summary>Echo 이상 압박에서 트라우마 쪽을 바라본다. (spec-004의 복선 연출)</summary>
        private void UpdateTraumaFacing()
        {
            if (Profile.WatchesTrauma && traumaTarget != null)
            {
                SetFacing(traumaTarget.position.x - transform.position.x);
            }
        }

        private void SetFacing(float horizontalDelta)
        {
            if (Mathf.Approximately(horizontalDelta, 0f)) return;
            FacingDirection = Mathf.Sign(horizontalDelta);
            // flipX: 스프라이트를 좌우로 뒤집는다. 회전이 아니라 뒤집기를 쓰는 것이 픽셀아트 규칙에도 맞다.
            if (bodyRenderer != null) bodyRenderer.flipX = FacingDirection < 0f;
        }

        private void SetTelegraph(bool value)
        {
            IsTelegraphing = value;
            if (telegraphRenderer != null) telegraphRenderer.enabled = value;
        }

        private void FindPlayerTarget()
        {
            // 플레이어는 Persistent 씬에 있어 프리팹에 미리 연결해 둘 수 없다. 그래서 실행 중에 찾는다.
            // "TargetKind가 Player인 IDamageable"을 기준으로 찾으므로 오브젝트 이름에 의존하지 않는다.
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
