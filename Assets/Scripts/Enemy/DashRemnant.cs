using System;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 돌진형 잔재. (spec-004, Issue #9)
    /// 접근 범위에 들어오면 멈춰서 돌진을 예고한 뒤, 빠르게 직선으로 돌진해 부딪힌 순간 피해를 준다.
    /// </summary>
    public sealed class DashRemnant : RemnantActor
    {
        private enum DashPhase
        {
            Telegraph,
            Bursting,
            Recovering
        }

        [SerializeField] private DashRemnantData data;

        private DashRemnantData fallbackData;
        private DashPhase dashPhase;
        private float dashDirection = 1f;
        private bool dashHitApplied;

        public override RemnantArchetype Archetype => RemnantArchetype.Dash;
        public DashRemnantData Data => data;
        protected override RemnantDataBase DataBase => DataOrDefault;

        public event Action<DashRemnant> Died;

        private DashRemnantData DataOrDefault
        {
            get
            {
                if (data != null) return data;
                if (fallbackData == null)
                {
                    fallbackData = ScriptableObject.CreateInstance<DashRemnantData>();
                    fallbackData.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallbackData;
            }
        }

        private void OnDestroy()
        {
            if (fallbackData == null) return;
            if (Application.isPlaying) Destroy(fallbackData);
            else DestroyImmediate(fallbackData);
        }

        public void SetData(DashRemnantData value)
        {
            data = value;
            ResetRuntimeState();
        }

        protected override void TickApproach(float deltaTime)
        {
            if (!HasTarget)
            {
                EnterState(RemnantState.Idle);
                return;
            }

            FaceTarget();
            if (TargetInRange(DataOrDefault.DashTriggerRange))
            {
                if (CanInitiateAttack)
                {
                    EnterState(RemnantState.Attack);
                }
                else
                {
                    EnterYielding();
                }

                return;
            }

            var position = transform.position;
            position.x = Mathf.MoveTowards(position.x, TargetX, DataBase.MoveSpeed * Profile.MoveSpeedMultiplier * deltaTime);
            transform.position = position;
        }

        protected override void TickAttack(float deltaTime)
        {
            switch (dashPhase)
            {
                case DashPhase.Telegraph:
                    FaceTarget();
                    stateRemaining -= deltaTime;
                    if (stateRemaining <= 0f)
                    {
                        SetTelegraph(false);
                        dashPhase = DashPhase.Bursting;
                        stateRemaining = DataOrDefault.DashMaxDurationSeconds;
                    }

                    break;
                case DashPhase.Bursting:
                    stateRemaining -= deltaTime;
                    var position = transform.position;
                    position.x += dashDirection * DataOrDefault.DashSpeed * deltaTime;
                    transform.position = position;

                    if (!dashHitApplied)
                    {
                        dashHitApplied = TryDealContactDamage(0f);
                    }

                    if (stateRemaining <= 0f || dashHitApplied)
                    {
                        dashPhase = DashPhase.Recovering;
                        stateRemaining = DataBase.AttackRecoverySeconds;
                    }

                    break;
                case DashPhase.Recovering:
                    stateRemaining -= deltaTime;
                    if (stateRemaining <= 0f)
                    {
                        attackResolved = true;
                        EnterState(RemnantState.Approach);
                    }

                    break;
            }
        }

        protected override void EnterState(RemnantState value)
        {
            base.EnterState(value);
            if (value == RemnantState.Attack)
            {
                dashPhase = DashPhase.Telegraph;
                dashDirection = FacingDirection;
                dashHitApplied = false;
            }
        }

        protected override void OnDied() => Died?.Invoke(this);
    }
}
