using Daeume.Core;
using Daeume.Encounter;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public sealed class EncounterContaminationAdapter : MonoBehaviour
    {
        [SerializeField] private ContaminationDirector director;

        private void Awake()
        {
            GameManager.Instance?.Events.Subscribe<EncounterStateChanged>(HandleEncounterStateChanged);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.Events.Unsubscribe<EncounterStateChanged>(HandleEncounterStateChanged);
        }

        public void Configure(ContaminationDirector value) => director = value;

        public void HandleEncounterStateChanged(EncounterStateChanged message)
        {
            if (message.State == EncounterState.Cleared) director?.HandleEncounterCleared();
        }
    }
}
