using System;
using Daeume.Core;
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
        private Collider2D dashCollider;
        private readonly RaycastHit2D[] terrainHits = new RaycastHit2D[8];

        private const float TerrainCollisionSkin = 0.02f;

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
                    var terrainBlocked = MoveBurstUntilTerrain(DataOrDefault.DashSpeed * deltaTime);

                    if (!dashHitApplied)
                    {
                        dashHitApplied = TryDealContactDamage(0f);
                    }

                    if (stateRemaining <= 0f || dashHitApplied || terrainBlocked)
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

        private bool MoveBurstUntilTerrain(float distance)
        {
            if (distance <= 0f) return false;

            if (dashCollider == null) dashCollider = GetComponent<Collider2D>();
            if (dashCollider == null || !dashCollider.enabled)
            {
                transform.position += Vector3.right * (dashDirection * distance);
                return false;
            }

            var direction = Vector2.right * dashDirection;
            // Rigidbody2D가 없는 트리거 Collider2D.Cast는 결과를 반환하지 않으므로,
            // 현재 몸체 bounds를 같은 방향으로 BoxCast해 이번 프레임의 이동 경로를 검사한다.
            var filter = new ContactFilter2D { useTriggers = false };
            var hitCount = Physics2D.BoxCast(
                dashCollider.bounds.center,
                dashCollider.bounds.size,
                transform.eulerAngles.z,
                direction,
                filter,
                terrainHits,
                distance + TerrainCollisionSkin);
            var allowedDistance = distance;
            var terrainBlocked = false;

            for (var index = 0; index < hitCount; index++)
            {
                var hitCollider = terrainHits[index].collider;
                if (hitCollider == null || hitCollider == dashCollider || hitCollider.isTrigger || IsDamageable(hitCollider)) continue;

                terrainBlocked = true;
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0f, terrainHits[index].distance - TerrainCollisionSkin));
            }

            transform.position += Vector3.right * (dashDirection * allowedDistance);
            return terrainBlocked;
        }

        private static bool IsDamageable(Collider2D candidate)
        {
            var behaviours = candidate.GetComponentsInParent<MonoBehaviour>();
            for (var index = 0; index < behaviours.Length; index++)
            {
                if (behaviours[index] is IDamageable) return true;
            }

            return false;
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
