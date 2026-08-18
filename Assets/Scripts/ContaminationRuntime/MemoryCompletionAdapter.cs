using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public sealed class MemoryCompletionAdapter : MonoBehaviour
    {
        [SerializeField] private ContaminationDirector director;
        [SerializeField] private StageOneChaseController stageOneChase;

        public void Configure(ContaminationDirector value) => director = value;
        public void Configure(StageOneChaseController value) => stageOneChase = value;

        [ContextMenu("Debug/Complete Memory And Start Chase")]
        public void TriggerDebugMemoryComplete()
        {
            if (stageOneChase != null && stageOneChase.BeginChaseFromMemory())
            {
                return;
            }

            director?.BeginChase();
        }
    }
}
