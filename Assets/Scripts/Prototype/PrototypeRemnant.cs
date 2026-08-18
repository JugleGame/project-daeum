using Daeume.Core;
using UnityEngine;

namespace Daeume.Prototype
{
    public sealed class PrototypeRemnant : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int health = 3;

        public DamageTargetKind TargetKind => DamageTargetKind.Remnant;
        public int Health => health;

        public DamageResult ApplyDamage(DamageRequest request)
        {
            if (request.Amount <= 0 || health <= 0)
            {
                return new DamageResult(false, 0);
            }

            var applied = Mathf.Min(health, request.Amount);
            health -= applied;
            GetComponent<PrototypeVisual>()?.SetColor(health == 0 ? Color.gray : new Color(1f, 0.35f, 0.35f));
            if (health == 0)
            {
                GetComponent<Collider2D>().enabled = false;
            }

            return new DamageResult(true, applied);
        }
    }
}
