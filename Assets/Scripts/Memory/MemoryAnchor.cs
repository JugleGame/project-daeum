using System;
using Daeume.Core;
using Daeume.Interaction;
using UnityEngine;

namespace Daeume.Memory
{
    public sealed class MemoryAnchor : MonoBehaviour, IInteractable
    {
        [SerializeField] private string memoryId = "memory-stage01";
        [SerializeField] private string titleKey = "memory.stage01.title";
        [SerializeField] private string narrativeFlag = "stage01-memory-revealed";
        [SerializeField] private string promptKey = "prompt.memory";
        [SerializeField] private string[] lineKeys = { "memory.stage01.01", "memory.stage01.02", "memory.stage01.03" };

        private int lineIndex;
        public string StableId => memoryId;
        public bool IsPresenting { get; private set; }
        public bool IsComplete { get; private set; }

        public bool CanInteract(GameObject interactor) => !IsComplete && GameManager.Instance != null &&
            (GameManager.Instance.StageState == StageState.Explore || GameManager.Instance.StageState == StageState.Memory);

        public InteractionPrompt GetPrompt() => new("Interact", IsPresenting ? "prompt.continue" : promptKey);

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor)) return;
            if (!IsPresenting) Begin(); else Advance();
        }

        public bool Begin()
        {
            if (IsComplete || lineKeys == null || lineKeys.Length == 0 || GameManager.Instance == null) return false;
            if (GameManager.Instance.StageState == StageState.Explore) GameManager.Instance.SetStageState(StageState.Memory);
            if (GameManager.Instance.StageState != StageState.Memory) return false;
            IsPresenting = true;
            lineIndex = 0;
            PublishLine(true);
            return true;
        }

        public bool Advance()
        {
            if (!IsPresenting) return false;
            lineIndex++;
            if (lineIndex < lineKeys.Length) { PublishLine(true); return true; }
            Complete();
            return false;
        }

        private void Complete()
        {
            IsPresenting = false;
            IsComplete = true;
            GameManager.Instance.Events.Publish(new MemoryPresentationChanged(memoryId, titleKey, string.Empty, lineKeys.Length, lineKeys.Length, false));
            GameManager.Instance.Events.Publish(new MemoryCompleted(memoryId, narrativeFlag));
        }

        private void PublishLine(bool visible) => GameManager.Instance.Events.Publish(
            new MemoryPresentationChanged(memoryId, titleKey, lineKeys[lineIndex], lineIndex, lineKeys.Length, visible));

        public void Configure(string id, string title, string flag, params string[] lines)
        {
            memoryId = id ?? string.Empty;
            titleKey = title ?? string.Empty;
            narrativeFlag = flag ?? string.Empty;
            lineKeys = lines ?? Array.Empty<string>();
        }
    }
}
