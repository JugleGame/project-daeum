using System;
using UnityEngine;

namespace Daeume.Enemy
{
    /// <summary>
    /// 원거리형 잔재. (spec-004, Issue #9)
    /// 너무 가까워지면 물러나 거리를 유지하고, 사거리 안에서만 예고 후 공격한다.
    /// 후퇴 방향은 압박 단계가 선언한다 — 평소엔 플레이어 반대쪽, 고압박에선 트라우마 쪽으로 끌릴 수 있다.
    /// </summary>
    public sealed class RangedRemnant : RemnantActor
    {
        [SerializeField] private RangedRemnantData data;

        private RangedRemnantData fallbackData;

        public override RemnantArchetype Archetype => RemnantArchetype.Ranged;
        public RangedRemnantData Data => data;
        protected override RemnantDataBase DataBase => DataOrDefault;

        /// <summary>이번 틱에 거리 유지를 위해 물러났는가.</summary>
        public bool IsRetreating { get; private set; }

        public event Action<RangedRemnant> Died;

        private RangedRemnantData DataOrDefault
        {
            get
            {
                if (data != null) return data;
                if (fallbackData == null)
                {
                    fallbackData = ScriptableObject.CreateInstance<RangedRemnantData>();
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

        public void SetData(RangedRemnantData value)
        {
            data = value;
            ResetRuntimeState();
        }

        protected override void TickApproach(float deltaTime)
        {
            IsRetreating = false;
            if (!HasTarget)
            {
                EnterState(RemnantState.Idle);
                return;
            }

            FaceTarget();
            var distance = DistanceToTarget;

            if (distance < DataOrDefault.RetreatTriggerRange)
            {
                Retreat(deltaTime);
                return;
            }

            if (distance <= DataBase.AttackRange)
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

        /// <summary>
        /// 물러나는 방향을 정한다. 압박 단계의 RetreatDirectionBias가 부호를 결정한다(spec-004).
        /// 기본(양수)은 플레이어 반대쪽. 음수면 트라우마 쪽으로 끌리듯 물러난다.
        /// </summary>
        private void Retreat(float deltaTime)
        {
            IsRetreating = true;
            var awayFromPlayer = Mathf.Sign(transform.position.x - TargetX);
            if (Mathf.Approximately(awayFromPlayer, 0f)) awayFromPlayer = -1f;

            var direction = awayFromPlayer;
            if (Profile.RetreatDirectionBias < 0f && TraumaTarget != null)
            {
                var offset = TraumaTarget.position.x - transform.position.x;
                direction = Mathf.Approximately(offset, 0f) ? awayFromPlayer : Mathf.Sign(offset);
            }

            var position = transform.position;
            position.x += direction * DataBase.MoveSpeed * Profile.MoveSpeedMultiplier * deltaTime;
            transform.position = position;
        }

        protected override void OnDied() => Died?.Invoke(this);
    }
}
