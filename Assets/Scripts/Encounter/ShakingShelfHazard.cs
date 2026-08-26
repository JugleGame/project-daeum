using System.Collections;
using System.Collections.Generic;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Encounter
{
    /// <summary>Stage 03의 흔들리는 선반. 예고 후 수평으로 밀어내며 피해는 주지 않는다.</summary>
    [RequireComponent(typeof(Collider2D), typeof(AudioSource), typeof(SpriteRenderer))]
    public sealed class ShakingShelfHazard : MonoBehaviour
    {
        [SerializeField] private string hazardId = "stage03.hazard.shaking-shelf";
        [SerializeField, Min(0.05f)] private float warningSeconds = 0.3f;
        [SerializeField, Min(0.1f)] private float knockbackDistance = 0.75f;
        [SerializeField] private SpriteRenderer warningRenderer;

        private readonly HashSet<Transform> queuedTargets = new();
        private AudioSource warningAudio;
        private float warningVolume = 1f;
        private Coroutine sequence;
        private Color idleColor;

        public string HazardId => hazardId;
        public float WarningSeconds => warningSeconds;
        public float KnockbackDistance => knockbackDistance;
        public int Damage => 0;
        public bool IsWarningActive { get; private set; }

        private void Awake()
        {
            if (warningRenderer == null) warningRenderer = GetComponent<SpriteRenderer>();
            idleColor = warningRenderer.color;
            warningAudio = GetComponent<AudioSource>();
            warningVolume = warningAudio.volume;
            warningAudio.playOnAwake = false;
            GetComponent<Collider2D>().isTrigger = true;
            SetWarning(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (FindTarget(other.transform) != null) QueueTarget(other.transform);
        }

        public void Configure(string id, float telegraphSeconds, float distance, SpriteRenderer renderer)
        {
            hazardId = id ?? string.Empty;
            warningSeconds = Mathf.Max(0.05f, telegraphSeconds);
            knockbackDistance = Mathf.Max(0.1f, distance);
            warningRenderer = renderer;
            if (warningRenderer != null) idleColor = warningRenderer.color;
            SetWarning(false);
        }

        public void QueueTarget(Transform target)
        {
            if (target == null || !queuedTargets.Add(target)) return;
            if (sequence == null) sequence = StartCoroutine(ResolveSequence());
        }

        private IEnumerator ResolveSequence()
        {
            SetWarning(true);
            if (warningAudio != null)
            {
                warningAudio.volume = warningVolume * AudioRuntime.SfxVolume;
                warningAudio.Play();
            }
            yield return new WaitForSeconds(warningSeconds);
            foreach (var target in queuedTargets)
            {
                if (target == null) continue;
                var direction = Mathf.Sign(target.position.x - transform.position.x);
                if (Mathf.Approximately(direction, 0f)) direction = 1f;
                var body = target.GetComponentInParent<Rigidbody2D>();
                if (body != null) body.position += Vector2.right * (direction * knockbackDistance);
                else target.position += Vector3.right * (direction * knockbackDistance);
            }

            queuedTargets.Clear();
            sequence = null;
            SetWarning(false);
        }

        private void SetWarning(bool active)
        {
            IsWarningActive = active;
            if (warningRenderer != null)
                warningRenderer.color = active ? Color.Lerp(idleColor, Color.white, 0.55f) : idleColor;
        }

        private static IDamageable FindTarget(Transform value)
        {
            foreach (var behaviour in value.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IDamageable damageable &&
                    (damageable.TargetKind == DamageTargetKind.Player || damageable.TargetKind == DamageTargetKind.Remnant))
                    return damageable;
            }

            return null;
        }
    }
}
