using System.Collections;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Player
{
    public sealed class TraumaContactSource : MonoBehaviour, IDamageable
    {
        public DamageTargetKind TargetKind => DamageTargetKind.Trauma;
        public DamageResult ApplyDamage(DamageRequest request) => new(false, 0);
    }

    public sealed class TraumaContactHandler : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float traumaGrabSeconds = 1f;
        [SerializeField] private string chaseCheckpointId = "Stage01_Chase";
        [SerializeField] private PlayerController controller;

        public bool GrabInProgress { get; private set; }
        public float TraumaGrabSeconds => traumaGrabSeconds;

        private void OnTriggerEnter2D(Collider2D other)
        {
            var source = other.GetComponentInParent<TraumaContactSource>();
            if (source != null && IsOnScreen(source.transform, Camera.main))
            {
                BeginGrab();
            }
        }

        public bool BeginGrab()
        {
            if (GrabInProgress)
            {
                return false;
            }

            StartCoroutine(GrabSequence());
            return true;
        }

        private IEnumerator GrabSequence()
        {
            GrabInProgress = true;
            GameManager.Instance?.Events.Publish(new TraumaGrabStarted(traumaGrabSeconds));
            if (controller != null)
            {
                controller.InputEnabled = false;
            }

            yield return new WaitForSeconds(traumaGrabSeconds);
            GameManager.Instance?.Fail(StageFailureCause.TraumaGrabCompleted);
            GameManager.Instance?.Events.Publish(new ChaseCheckpointRestoreRequested(chaseCheckpointId));
            GrabInProgress = false;
        }

        public static bool IsOnScreen(Transform source, Camera camera)
        {
            if (source == null || camera == null)
            {
                return false;
            }

            var point = camera.WorldToViewportPoint(source.position);
            return point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
        }
    }
}
