using UnityEngine;

namespace Daeume.Core
{
    [DefaultExecutionOrder(-100)]
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        private readonly StageLoop stageLoop = new();

        public EventBus Events { get; } = new();
        public StageState StageState => stageLoop.State;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SetStageState(StageState nextState)
        {
            if (!stageLoop.TryTransition(nextState))
            {
                return;
            }

            Events.Publish(new StageStateChanged(nextState));
        }

        public bool Fail(StageFailureCause cause)
        {
            if (!stageLoop.TryFail(cause))
            {
                return false;
            }

            Events.Publish(new StageFailed(cause));
            Events.Publish(new StageStateChanged(StageState.Failed));
            return true;
        }

        public void ResetStage(StageState state = StageState.Explore)
        {
            stageLoop.Reset(state);
            Events.Publish(new StageStateChanged(state));
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Events.Clear();
                Instance = null;
            }
        }
    }
}
