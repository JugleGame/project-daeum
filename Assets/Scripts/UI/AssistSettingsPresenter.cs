using Daeume.Core;
using UnityEngine;

namespace Daeume.UI
{
    public sealed class AssistSettingsPresenter : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float cameraShakeStrength = 0.5f;
        [SerializeField, Range(0, 2)] private int subtitleSize = 1;
        [SerializeField] private bool chaseSpeedAssist;

        public AssistSettings Current => new() { CameraShakeStrength = cameraShakeStrength, SubtitleSize = subtitleSize, ChaseSpeedAssist = chaseSpeedAssist };
        public void Apply(AssistSettings settings)
        {
            settings ??= new AssistSettings();
            cameraShakeStrength = Mathf.Clamp01(settings.CameraShakeStrength);
            subtitleSize = Mathf.Clamp(settings.SubtitleSize, 0, 2);
            chaseSpeedAssist = settings.ChaseSpeedAssist;
        }
        public void SetCameraShake(float value) => cameraShakeStrength = Mathf.Clamp01(value);
        public void SetSubtitleSize(int value) => subtitleSize = Mathf.Clamp(value, 0, 2);
        public void SetChaseAssist(bool value) => chaseSpeedAssist = value;
    }
}
