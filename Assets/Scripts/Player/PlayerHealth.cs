using Daeume.Core;
using UnityEngine;

namespace Daeume.Player
{
    public sealed class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(0f)] private float invulnerabilitySeconds = 0.5f;

        private float invulnerableUntil;

        public DamageTargetKind TargetKind => DamageTargetKind.Player;
        public int MaxHealth => maxHealth;
        public int CurrentHealth { get; private set; }

        private void Awake() => CurrentHealth = maxHealth;

        public DamageResult ApplyDamage(DamageRequest request) => ApplyDamageAt(request, Time.time);

        public DamageResult ApplyDamageAt(DamageRequest request, float now)
        {
            if (request.Amount <= 0 || CurrentHealth <= 0 || now < invulnerableUntil)
            {
                return new DamageResult(false, 0);
            }

            var previous = CurrentHealth;
            CurrentHealth = Mathf.Max(0, CurrentHealth - request.Amount);
            invulnerableUntil = now + invulnerabilitySeconds;
            var applied = previous - CurrentHealth;
            GameManager.Instance?.Events.Publish(new PlayerHealthChanged(CurrentHealth, maxHealth));

            if (CurrentHealth == 0)
            {
                GameManager.Instance?.Fail(StageFailureCause.HealthDepleted);
            }

            return new DamageResult(applied > 0, applied);
        }

        public void Restore(int health)
        {
            CurrentHealth = Mathf.Clamp(health, 1, maxHealth);
            invulnerableUntil = 0f;
            GameManager.Instance?.Events.Publish(new PlayerHealthChanged(CurrentHealth, maxHealth));
        }
    }
}
