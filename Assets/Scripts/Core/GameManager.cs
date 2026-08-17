using UnityEngine;

namespace Daeume.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public EventBus Events { get; } = new();
        public StageState StageState { get; private set; } = StageState.Explore;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void SetStageState(StageState nextState)
        {
            StageState = nextState;
        }
    }
}
