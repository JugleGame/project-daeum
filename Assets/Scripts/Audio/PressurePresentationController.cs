using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Audio
{
    public sealed class PressurePresentationController : MonoBehaviour
    {
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private Camera targetCamera;
        [SerializeField, Range(0f, 0.25f)] private float maximumShake = 0.08f;
        private Vector3 cameraOrigin;
        private float pressureAmount;
        private float shakeAssist = 1f;
        public float PressureAmount => pressureAmount;

        private void Awake() { if (targetCamera == null) targetCamera = Camera.main; if (targetCamera != null) cameraOrigin = targetCamera.transform.localPosition; }
        private void OnEnable() => Connect();
        private void Start() => Connect();
        private void OnDisable() { GameManager.Instance?.Events.Unsubscribe<ContaminationPressureChanged>(OnPressure); RestoreCamera(); }
        private void LateUpdate()
        {
            if (targetCamera == null || pressureAmount <= 0f || shakeAssist <= 0f) return;
            var offset = Random.insideUnitCircle * maximumShake * pressureAmount * shakeAssist;
            targetCamera.transform.localPosition = cameraOrigin + new Vector3(offset.x, offset.y, 0f);
        }

        public void ApplyAssist(AssistSettings settings) => shakeAssist = Mathf.Clamp01(settings?.CameraShakeStrength ?? 1f);
        public void Bind(AudioSource source, Camera camera) { ambientSource = source; targetCamera = camera; if (camera != null) cameraOrigin = camera.transform.localPosition; }
        private void Connect()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Events.Unsubscribe<ContaminationPressureChanged>(OnPressure);
            GameManager.Instance.Events.Subscribe<ContaminationPressureChanged>(OnPressure);
        }
        private void OnPressure(ContaminationPressureChanged value)
        {
            pressureAmount = value.Pressure switch { PressureStage.Stable => 0f, PressureStage.Echo => 0.35f, PressureStage.Intrusion => 1f, _ => 0f };
            if (ambientSource != null) ambientSource.volume = Mathf.Lerp(0.25f, 0.8f, pressureAmount);
            if (pressureAmount <= 0f) RestoreCamera();
        }
        private void RestoreCamera() { if (targetCamera != null) targetCamera.transform.localPosition = cameraOrigin; }
    }
}
