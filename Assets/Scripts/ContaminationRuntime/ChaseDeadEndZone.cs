using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public sealed class ChaseDeadEndZone : MonoBehaviour
    {
        [SerializeField] private StageOneChaseController chase;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) chase?.SetDeadEndBlocked(true);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) chase?.SetDeadEndBlocked(false);
        }

        public void Configure(StageOneChaseController value) => chase = value;
    }
}
