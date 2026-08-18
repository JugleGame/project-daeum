using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public sealed class MemoryCompletionAdapter : MonoBehaviour
    {
        [SerializeField] private ContaminationDirector director;

        public void Configure(ContaminationDirector value) => director = value;

        [ContextMenu("Debug/Complete Memory And Start Chase")]
        public void TriggerDebugMemoryComplete()
        {
            director?.BeginChase();
        }
    }
}
