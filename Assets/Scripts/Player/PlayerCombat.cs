using System.Collections.Generic;
using Daeume.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Player
{
    public sealed class PlayerCombat : MonoBehaviour
    {
        public const string AttackActionName = "Attack";

        [SerializeField] private InputActionReference attackAction;
        [SerializeField] private Transform attackOrigin;
        [SerializeField, Min(0.01f)] private float attackRadius = 0.55f;
        [SerializeField, Min(0)] private int damage = 1;
        [SerializeField] private LayerMask targetMask = ~0;
        [SerializeField] private string encounterId = "Stage01_Encounter01";

        private readonly HashSet<IDamageable> damaged = new();
        private InputAction attack;

        public bool PlayerAggression { get; private set; }

        private void Awake()
        {
            var playerInput = GetComponentInParent<PlayerInput>();
            attack = attackAction == null ? playerInput?.actions?.FindAction(AttackActionName) : attackAction.action;
        }

        private void OnEnable() => attack?.Enable();
        private void OnDisable() => attack?.Disable();

        private void Update()
        {
            if (attack != null && attack.WasPressedThisFrame())
            {
                Attack();
            }
        }

        public int Attack()
        {
            damaged.Clear();
            var center = attackOrigin == null ? transform.position : attackOrigin.position;
            var hits = Physics2D.OverlapCircleAll(center, attackRadius, targetMask);
            var appliedCount = 0;

            for (var index = 0; index < hits.Length; index++)
            {
                var target = FindDamageable(hits[index]);
                if (target == null || !damaged.Add(target))
                {
                    continue;
                }

                var result = target.ApplyDamage(new DamageRequest(damage, gameObject));
                if (!result.Applied || target.TargetKind != DamageTargetKind.Remnant)
                {
                    continue;
                }

                appliedCount++;
                if (!PlayerAggression)
                {
                    PlayerAggression = true;
                    GameManager.Instance?.Events.Publish(new PlayerAggressionChanged(encounterId));
                }
            }

            return appliedCount;
        }

        public void ResetAggression() => PlayerAggression = false;

        private static IDamageable FindDamageable(Collider2D collider)
        {
            if (collider == null)
            {
                return null;
            }

            foreach (var component in collider.GetComponentsInParent<MonoBehaviour>())
            {
                if (component is IDamageable damageable)
                {
                    return damageable;
                }
            }

            return null;
        }
    }
}
