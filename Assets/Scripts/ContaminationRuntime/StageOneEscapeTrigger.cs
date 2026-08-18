using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public sealed class StageOneEscapeTrigger : MonoBehaviour
    {
        [SerializeField] private StageOneChaseController chase;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerController>() != null) chase?.CompleteAtEscape();
        }

        public void Configure(StageOneChaseController value) => chase = value;
    }
}
