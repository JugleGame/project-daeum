using System;
using System.Collections;
using System.Collections.Generic;
using Daeume.Core;
using Daeume.Enemy;
using Daeume.Player;
using UnityEngine;

namespace Daeume.Encounter
{
    [RequireComponent(typeof(Collider2D), typeof(AudioSource))]
    public sealed class WarningPulseHazard : MonoBehaviour
    {
        [SerializeField] private string hazardId = "hazard-stage01-warning-pulse";
        [SerializeField, Min(0.05f)] private float warningSeconds = 0.65f;
        [SerializeField, Min(1)] private int damage = 1;
        [SerializeField] private SpriteRenderer warningRenderer;

        private readonly List<IDamageable> queuedTargets = new();
        private AudioSource warningAudio;
        private Coroutine sequence;

        public string HazardId => hazardId;
        public bool IsWarningActive { get; private set; }
        public bool AudioWarningAvailable => warningAudio != null && warningAudio.clip != null;
        public int WarningSequenceCount { get; private set; }
        public event Action WarningIssued;

        private void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            warningAudio = GetComponent<AudioSource>();
            warningAudio.playOnAwake = false;
            if (warningAudio.clip == null) warningAudio.clip = CreateWarningClip();
            SetWarning(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var target = FindDamageable(other.transform);
            if (target == null || (target.TargetKind != DamageTargetKind.Player && target.TargetKind != DamageTargetKind.Remnant)) return;
            QueueTarget(target);
        }

        public void Configure(float telegraphSeconds, int pulseDamage)
        {
            warningSeconds = Mathf.Max(0.05f, telegraphSeconds);
            damage = Mathf.Max(1, pulseDamage);
        }

        public void Configure(string id, float telegraphSeconds, int pulseDamage, SpriteRenderer renderer)
        {
            hazardId = id ?? string.Empty;
            warningRenderer = renderer;
            Configure(telegraphSeconds, pulseDamage);
            SetWarning(false);
        }

        public void QueueTarget(IDamageable target)
        {
            if (target == null || queuedTargets.Contains(target)) return;
            queuedTargets.Add(target);
            if (sequence == null) sequence = StartCoroutine(PulseSequence());
        }

        public void BeginWarningForTests(IDamageable target)
        {
            if (target != null && !queuedTargets.Contains(target)) queuedTargets.Add(target);
            IssueWarning();
        }

        public void ResolvePulse()
        {
            foreach (var target in queuedTargets.ToArray()) ApplyNonlethalDamage(target);
            queuedTargets.Clear();
            SetWarning(false);
            sequence = null;
        }

        private IEnumerator PulseSequence()
        {
            IssueWarning();
            yield return new WaitForSeconds(warningSeconds);
            ResolvePulse();
        }

        private void IssueWarning()
        {
            WarningSequenceCount++;
            SetWarning(true);
            if (warningAudio != null && warningAudio.clip != null) warningAudio.Play();
            WarningIssued?.Invoke();
        }

        private void ApplyNonlethalDamage(IDamageable target)
        {
            var health = CurrentHealth(target);
            var amount = Mathf.Min(damage, Mathf.Max(0, health - 1));
            if (amount > 0) target.ApplyDamage(new DamageRequest(amount, gameObject));
        }

        private static int CurrentHealth(IDamageable target)
        {
            if (target is PlayerHealth player) return player.CurrentHealth;
            if (target is MeleeRemnant remnant) return remnant.CurrentHealth;
            return 1;
        }

        private void SetWarning(bool value)
        {
            IsWarningActive = value;
            if (warningRenderer != null) warningRenderer.enabled = value;
        }

        private static IDamageable FindDamageable(Transform value)
        {
            foreach (var behaviour in value.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IDamageable damageable) return damageable;
            }

            return null;
        }

        private static AudioClip CreateWarningClip()
        {
            const int frequency = 880;
            const int sampleRate = 22050;
            const int sampleCount = 2205;
            var samples = new float[sampleCount];
            for (var i = 0; i < sampleCount; i++)
            {
                var fade = 1f - (float)i / sampleCount;
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * i / sampleRate) * fade * 0.18f;
            }

            var clip = AudioClip.Create("EncounterWarningPlaceholder", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
