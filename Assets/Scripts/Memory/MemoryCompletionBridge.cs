using Daeume.ContaminationRuntime;
using Daeume.Core;
using UnityEngine;

namespace Daeume.Memory
{
    public sealed class MemoryCompletionBridge : MonoBehaviour
    {
        [SerializeField] private StageOneChaseController stageOneChase;

        private void OnEnable() => GameManager.Instance?.Events.Subscribe<MemoryCompleted>(OnCompleted);
        private void Start() => Reconnect();
        private void OnDisable() => GameManager.Instance?.Events.Unsubscribe<MemoryCompleted>(OnCompleted);
        public void Configure(StageOneChaseController controller) => stageOneChase = controller;

        private void Reconnect()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Events.Unsubscribe<MemoryCompleted>(OnCompleted);
            GameManager.Instance.Events.Subscribe<MemoryCompleted>(OnCompleted);
        }

        private void OnCompleted(MemoryCompleted message)
        {
            if (stageOneChase == null) stageOneChase = FindAnyObjectByType<StageOneChaseController>();
            stageOneChase?.BeginChaseFromMemory();
        }
    }
}
