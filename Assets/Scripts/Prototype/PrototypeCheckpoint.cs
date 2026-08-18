using Daeume.Flow;
using Daeume.Player;
using UnityEngine;

namespace Daeume.Prototype
{
    public sealed class PrototypeCheckpoint : MonoBehaviour
    {
        [SerializeField] private SceneFlowController flow;
        [SerializeField] private string checkpointId = "Stage01_Chase";

        public bool Activated { get; private set; }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (Activated || flow == null)
            {
                return;
            }

            var health = other.GetComponentInParent<PlayerHealth>();
            if (health == null)
            {
                return;
            }

            flow.SaveChaseCheckpoint(checkpointId, transform.position, health.CurrentHealth, string.Empty);
            Activated = true;
            GetComponent<PrototypeVisual>()?.SetColor(Color.green);
        }
    }
}
