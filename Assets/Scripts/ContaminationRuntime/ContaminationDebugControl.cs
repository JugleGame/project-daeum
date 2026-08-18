using Daeume.Contamination;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public sealed class ContaminationDebugControl : MonoBehaviour
    {
        [SerializeField] private ContaminationDirector director;
        [SerializeField] private PressureStage debugPressure = PressureStage.Stable;
        [SerializeField, Range(0f, 15f)] private float debugDistance = 5f;

        public void Configure(ContaminationDirector value) => director = value;

        [ContextMenu("Debug/Apply Pressure")]
        public void ApplyPressure() => director?.SetPressure(debugPressure);

        [ContextMenu("Debug/Apply Distance")]
        public void ApplyDistance() => director?.SetDebugDistance(debugDistance);

        public void ApplyPressure(PressureStage value)
        {
            debugPressure = value;
            ApplyPressure();
        }

        public void ApplyDistance(float value)
        {
            debugDistance = value;
            ApplyDistance();
        }
    }
}
