using Daeume.Interaction;
using UnityEngine;

namespace Daeume.Prototype
{
    public sealed class PrototypeInteractable : MonoBehaviour, IInteractable
    {
        public string StableId => "Prototype_Interactable";
        public bool Interacted { get; private set; }
        public bool CanInteract(GameObject interactor) => !Interacted;
        public InteractionPrompt GetPrompt() => new("Interact", "prototype.interact");

        public void Interact(GameObject interactor)
        {
            Interacted = true;
            GetComponent<PrototypeVisual>()?.SetColor(Color.green);
        }
    }
}
