using System;
using Daeume.Contamination;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Enemy
{
    public enum RemnantState
    {
        Idle,
        Alert,
        Approach,
        Attack,
        Hit,
        Dead
    }

    [RequireComponent(typeof(Collider2D))]
    public sealed class MeleeRemnant : MonoBehaviour, IDamageable
    {
        [SerializeField] private MeleeRemnantData data;
        [SerializeField] private string spawnMarkerId = string.Empty;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer telegraphRenderer;

        private Collider2D bodyCollider;
        private Transform target;
        private IDamageable damageTarget;
        private Transform traumaTarget;
        private MeleeRemnantData fallbackData;
        private PressureStage pressureStage;
        private float stateRemaining;
        private bool attackResolved;

        public DamageTargetKind TargetKind => DamageTargetKind.Remnant;
        public RemnantState State { get; private set; }
        public int CurrentHealth { get; private set; }
        public float FacingDirection { get; private set; } = 1f;
        public bool IsTelegraphing { get; private set; }
        public bool TraumaAttentionActive => Profile.WatchesTrauma && traumaTarget != null;
        public bool CanDealDamage => State != RemnantState.Dead && bodyCollider != null && bodyCollider.enabled;
        public string SpawnMarkerId => spawnMarkerId;
        public MeleeRemnantData Data => data;
        public event Action<MeleeRemnant> Died;

        private RemnantPressureProfile Profile => DataOrDefault.GetProfile(pressureStage);
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

        private void Update() => Tick(Time.deltaTime);

        private void OnDestroy()
        {
            if (fallbackData == null) return;
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
                FindPlayerTarget();
            }

            var step = Mathf.Max(0f, deltaTime);
            switch (State)
            {
                case RemnantState.Idle:
                    UpdateTraumaFacing();
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

            var position = transform.position;
            position.x = Mathf.MoveTowards(
                position.x,
                target.position.x,
                DataOrDefault.MoveSpeed * Profile.MoveSpeedMultiplier * deltaTime);
            transform.position = position;
        }

        private void TickAttack(float deltaTime)
        {
            FaceTarget();
            stateRemaining -= deltaTime;
            if (!attackResolved && stateRemaining <= 0f)
            {
                SetTelegraph(false);
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
            bodyCollider.enabled = false;
            if (bodyRenderer != null) bodyRenderer.color = new Color(0.2f, 0.2f, 0.24f, 0.35f);
            Died?.Invoke(this);
        }

        private bool TargetInRange(float range)
        {
            return target != null && Mathf.Abs(target.position.x - transform.position.x) <= range;
        }

        private void FaceTarget()
        {
            if (target != null) SetFacing(target.position.x - transform.position.x);
        }

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
            if (bodyRenderer != null) bodyRenderer.flipX = FacingDirection < 0f;
        }

        private void SetTelegraph(bool value)
        {
            IsTelegraphing = value;
            if (telegraphRenderer != null) telegraphRenderer.enabled = value;
        }

        private void FindPlayerTarget()
        {
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
