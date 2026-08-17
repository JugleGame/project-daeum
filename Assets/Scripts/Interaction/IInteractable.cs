using UnityEngine;

namespace Daeume.Interaction
{
    public readonly struct InteractionPrompt
    {
        public InteractionPrompt(string actionName, string stringTableKey)
        {
            ActionName = actionName;
            StringTableKey = stringTableKey;
        }

        public string ActionName { get; }
        public string StringTableKey { get; }
    }

    public interface IInteractable
    {
        string StableId { get; }
        bool CanInteract(GameObject interactor);
        InteractionPrompt GetPrompt();
        void Interact(GameObject interactor);
    }
}
