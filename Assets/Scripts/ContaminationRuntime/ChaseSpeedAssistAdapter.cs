using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public sealed class ChaseSpeedAssistAdapter : MonoBehaviour
    {
        [SerializeField] private bool enabledForDebug;
        [SerializeField, Range(0.1f, 1f)] private float speedMultiplier = 0.75f;
        [SerializeField, Range(0f, 1f)] private float approachPressure = 0.5f;

        public bool Enabled => enabledForDebug;
        public float SpeedMultiplier => speedMultiplier;
        public float ApproachPressure => approachPressure;

        public void Configure(bool enabled, float speedScale = 0.75f, float pressure = 0.5f)
        {
            enabledForDebug = enabled;
            speedMultiplier = Mathf.Clamp(speedScale, 0.1f, 1f);
            approachPressure = Mathf.Clamp01(pressure);
        }

        public float ResolveSpeed(float baseSpeed)
        {
            return Mathf.Max(0f, baseSpeed) * (enabledForDebug ? speedMultiplier : 1f);
        }

        public float ResolveApproachDistance(float minimumDistance, float maximumDistance)
        {
            if (!enabledForDebug) return minimumDistance;
            return Mathf.Lerp(maximumDistance, minimumDistance, approachPressure);
        }
    }
}
