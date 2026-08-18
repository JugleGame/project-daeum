using System;
using System.Collections.Generic;
using System.Linq;
using Daeume.Contamination;
using UnityEngine;

namespace Daeume.Enemy
{
    [Serializable]
    public struct RemnantPressureProfile
    {
        [SerializeField] private PressureStage stage;
        [SerializeField, Min(0.01f)] private float moveSpeedMultiplier;
        [SerializeField, Min(0.01f)] private float telegraphMultiplier;
        [SerializeField] private bool watchesTrauma;

        public RemnantPressureProfile(
            PressureStage stage,
            float moveSpeedMultiplier,
            float telegraphMultiplier,
            bool watchesTrauma)
        {
            this.stage = stage;
            this.moveSpeedMultiplier = Mathf.Max(0.01f, moveSpeedMultiplier);
            this.telegraphMultiplier = Mathf.Max(0.01f, telegraphMultiplier);
            this.watchesTrauma = watchesTrauma;
        }

        public PressureStage Stage => stage;
        public float MoveSpeedMultiplier => Mathf.Max(0.01f, moveSpeedMultiplier);
        public float TelegraphMultiplier => Mathf.Max(0.01f, telegraphMultiplier);
        public bool WatchesTrauma => watchesTrauma;
    }

    [CreateAssetMenu(fileName = "MeleeRemnantData", menuName = "Daeume/Enemy/Melee Remnant Data")]
    public sealed class MeleeRemnantData : ScriptableObject
    {
        [SerializeField] private string enemyId = "remnant-melee";
        [SerializeField, Min(1)] private int maxHealth = 3;
        [SerializeField, Min(1)] private int contactDamage = 1;
        [SerializeField, Min(0.1f)] private float detectionRange = 5f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.1f;
        [SerializeField, Min(0.01f)] private float moveSpeed = 1.8f;
        [SerializeField, Min(0.01f)] private float alertSeconds = 0.2f;
        [SerializeField, Min(0.05f)] private float attackTelegraphSeconds = 0.55f;
        [SerializeField, Min(0.01f)] private float attackRecoverySeconds = 0.7f;
        [SerializeField, Min(0.01f)] private float hitStunSeconds = 0.18f;
        [SerializeField] private List<RemnantPressureProfile> pressureProfiles = new()
        {
            new RemnantPressureProfile(PressureStage.Stable, 1f, 1f, false),
            new RemnantPressureProfile(PressureStage.Echo, 1.08f, 0.85f, true),
            new RemnantPressureProfile(PressureStage.Intrusion, 1.16f, 0.7f, true)
        };

        public string EnemyId => enemyId;
        public int MaxHealth => maxHealth;
        public int ContactDamage => contactDamage;
        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float MoveSpeed => moveSpeed;
        public float AlertSeconds => alertSeconds;
        public float AttackTelegraphSeconds => attackTelegraphSeconds;
        public float AttackRecoverySeconds => attackRecoverySeconds;
        public float HitStunSeconds => hitStunSeconds;
        public IReadOnlyList<RemnantPressureProfile> PressureProfiles => pressureProfiles;

        public RemnantPressureProfile GetProfile(PressureStage stage)
        {
            if (pressureProfiles != null)
            {
                for (var index = 0; index < pressureProfiles.Count; index++)
                {
                    if (pressureProfiles[index].Stage == stage)
                    {
                        return pressureProfiles[index];
                    }
                }
            }

            return new RemnantPressureProfile(PressureStage.Stable, 1f, 1f, false);
        }

        public IReadOnlyList<string> ValidateData()
        {
            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(enemyId)) errors.Add("EnemyId is required.");
            if (maxHealth < 1) errors.Add("MaxHealth must be at least 1.");
            if (contactDamage < 1) errors.Add("ContactDamage must be at least 1.");
            if (detectionRange <= attackRange) errors.Add("DetectionRange must be greater than AttackRange.");
            if (attackTelegraphSeconds < 0.05f) errors.Add("AttackTelegraphSeconds must preserve a visible warning.");

            var profiles = pressureProfiles ?? new List<RemnantPressureProfile>();
            if (profiles.Select(profile => profile.Stage).Distinct().Count() != profiles.Count)
            {
                errors.Add("PressureProfiles contains duplicate stages.");
            }

            foreach (var required in new[] { PressureStage.Stable, PressureStage.Echo, PressureStage.Intrusion })
            {
                if (!profiles.Any(profile => profile.Stage == required))
                {
                    errors.Add($"PressureProfiles is missing {required}.");
                }
            }

            if (GetProfile(PressureStage.Echo).TelegraphMultiplier * attackTelegraphSeconds < 0.05f ||
                GetProfile(PressureStage.Intrusion).TelegraphMultiplier * attackTelegraphSeconds < 0.05f)
            {
                errors.Add("Pressure profiles cannot reduce attack telegraph to zero.");
            }

            return errors;
        }
    }
}
