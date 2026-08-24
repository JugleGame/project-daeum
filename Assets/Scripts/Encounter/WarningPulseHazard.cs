using System;
using System.Collections;
using System.Collections.Generic;
using Daeume.Core;
using Daeume.Enemy;
using Daeume.Player;
using UnityEngine;

namespace Daeume.Encounter
{
    /// <summary>
    /// 전투 공간을 바꾸는 지형 요소 1종: 예고 후 범위에 충격을 주는 장치. (spec-012)
    ///
    /// 스펙이 지형 요소에 요구하는 3가지를 모두 지킨다.
    /// 1) 활성 전에 시각·음향 신호를 낸다 (IssueWarning)
    /// 2) 플레이어와 잔재 모두에게 같은 규칙으로 작용한다 (OnTriggerEnter2D의 대상 판정)
    /// 3) 단독으로 즉사시키지 않는다 (ApplyNonlethalDamage에서 체력 1은 남긴다)
    ///
    /// 세 규칙 중 하나라도 빠지면 "보이지 않는 즉사"가 되어 기획이 금지한 경험이 된다.
    /// </summary>
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

        /// <summary>
        /// 체력을 1 이상 남기고 피해를 준다. 지형 요소 단독으로는 절대 죽지 않게 하는 계산이다(spec-012).
        /// health - 1이 상한이므로, 체력이 1이면 피해량이 0이 되어 아무 일도 일어나지 않는다.
        /// </summary>
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

        /// <summary>
        /// 경고음 에셋이 아직 없을 때 쓰는 임시 소리를 코드로 만들어 낸다(짧게 감쇠하는 880Hz 톤).
        /// </summary>
        /// <remarks>
        /// 오디오 담당(C)의 실제 효과음이 붙기 전에도 "음향 신호 1개" 요구를 만족시키기 위한 자리 채움이다.
        /// 실제 효과음이 준비되면 인스펙터에서 AudioSource의 clip을 지정하면 이 코드는 자동으로 건너뛴다.
        /// </remarks>
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
