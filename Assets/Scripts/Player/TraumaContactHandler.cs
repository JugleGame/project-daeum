using System.Collections;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Player
{
    /// <summary>
    /// Trauma 접촉 피해와 재타격 cooldown을 담당한다. (spec-003, spec-011)
    ///
    /// 접촉 1회는 체력 1만 감소시키며 PlayerHealth의 사망·무적 시간 규칙을 그대로 사용한다.
    /// 공격 연출 중에도 PlayerController 입력은 잠그지 않아 플레이어가 접촉 상태에서 빠져나올 수 있다.
    /// </summary>
    public sealed class TraumaContactHandler : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float traumaGrabSeconds = 1f;

        private PlayerHealth health;

        // 기존 animation/event 계약을 유지하는 이름이다. 이제는 grab 사망 연출이 아니라 재타격 cooldown을 뜻한다.
        public bool GrabInProgress { get; private set; }
        public float TraumaGrabSeconds => traumaGrabSeconds;
        public bool ContactFailureEnabled { get; private set; } = true;

        private void Awake()
        {
            health = GetComponent<PlayerHealth>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            var source = other.GetComponentInParent<TraumaContactSource>();
            if (ContactFailureEnabled && source != null && IsOnScreen(source.transform, Camera.main))
            {
                BeginGrab();
            }
        }

        /// <summary>
        /// 접촉이 계속되면 cooldown이 끝난 뒤 다시 피해를 준다.
        /// PlayerHealth의 무적 시간도 함께 적용되므로 한 frame에 중복 피해가 들어가지 않는다.
        /// </summary>
        private void OnTriggerStay2D(Collider2D other) => OnTriggerEnter2D(other);

        /// <summary>Trauma 접촉 피해를 한 번 적용한다. cooldown이나 무적 중이면 무시한다.</summary>
        public bool BeginGrab()
        {
            if (!ContactFailureEnabled || GrabInProgress)
            {
                return false;
            }

            if (health == null) health = GetComponent<PlayerHealth>();
            if (health == null || !health.ApplyDamage(new DamageRequest(1)).Applied)
            {
                return false;
            }

            StartCoroutine(GrabSequence());
            AudioRuntime.PlaySfx("TraumaAttack");
            return true;
        }

        /// <summary>Stage13에서는 접촉 피해와 공격 연출을 시작하지 않는다.</summary>
        public void SetContactFailureEnabled(bool enabled) => ContactFailureEnabled = enabled;

        private void OnDisable()
        {
            GrabInProgress = false;
        }

        private IEnumerator GrabSequence()
        {
            GrabInProgress = true;
            GameManager.Instance?.Events.Publish(new TraumaGrabStarted(traumaGrabSeconds));
            yield return new WaitForSeconds(traumaGrabSeconds);
            GrabInProgress = false;
        }

        /// <summary>대상이 카메라 화면 안에 있는지 검사한다.</summary>
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
