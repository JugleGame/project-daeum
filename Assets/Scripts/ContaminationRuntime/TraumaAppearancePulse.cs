using Daeume.Core;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    /// <summary>추격 시작으로 트라우마가 나타날 때만 짧게 맥동시킨다.</summary>
    public sealed class TraumaAppearancePulse : SpritePulse
    {
        [SerializeField, Min(0.05f)] private float durationSeconds = 1.3f;

        private float pulseUntil;

        protected override bool ShouldPulse => Time.time < pulseUntil;

        private void OnEnable() => Play();

        public void Play() => pulseUntil = Time.time + durationSeconds;
    }
}
