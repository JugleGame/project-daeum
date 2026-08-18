using Daeume.Core;
using Daeume.Flow;
using Daeume.Player;
using UnityEngine;

namespace Daeume.ContaminationRuntime
{
    public sealed class StageOneChaseController : MonoBehaviour
    {
        public const string CheckpointId = "Stage01_Chase";

        [SerializeField] private ContaminationDirector director;
        [SerializeField] private SceneFlowController flow;
        [SerializeField] private Transform player;
        [SerializeField] private GameObject trauma;

        public ContaminationDirector Director => director;
        public bool ChaseStarted { get; private set; }

        public void Configure(ContaminationDirector chaseDirector, SceneFlowController sceneFlow, Transform playerTransform, GameObject traumaActor)
        {
            director = chaseDirector;
            flow = sceneFlow;
            player = playerTransform;
            trauma = traumaActor;
        }

        public bool BeginChaseFromMemory()
        {
            ResolveReferences();
            var manager = GameManager.Instance;
            if (manager == null || director == null) return false;

            if (manager.StageState == StageState.Explore) manager.SetStageState(StageState.Memory);
            if (manager.StageState != StageState.Memory) return false;

            manager.SetStageState(StageState.Chase);
            if (manager.StageState != StageState.Chase || !director.BeginChase()) return false;

            trauma?.SetActive(true);
            var health = player == null ? 3 : player.GetComponent<PlayerHealth>()?.CurrentHealth ?? 3;
            flow?.SaveChaseCheckpoint(CheckpointId, player == null ? Vector2.zero : (Vector2)player.position, health, director.VariantId);
            ChaseStarted = true;
            return true;
        }

        public bool CompleteAtEscape()
        {
            ResolveReferences();
            var manager = GameManager.Instance;
            if (manager == null || manager.StageState != StageState.Chase) return false;
            if (flow != null) return flow.CompleteStageOne();
            manager.SetStageState(StageState.Cleared);
            return manager.StageState == StageState.Cleared;
        }

        public void SetDeadEndBlocked(bool blocked) => director?.SetDeadEndBlocked(blocked);

        private void ResolveReferences()
        {
            if (director == null) director = FindFirstObjectByType<ContaminationDirector>();
            if (flow == null) flow = FindFirstObjectByType<SceneFlowController>();
            if (player == null)
            {
                var found = GameObject.Find("Player");
                if (found != null) player = found.transform;
            }
            if (trauma == null)
            {
                var found = GameObject.Find("Trauma");
                if (found != null) trauma = found;
            }
        }
    }
}
