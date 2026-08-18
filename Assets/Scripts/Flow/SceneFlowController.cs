using System.Collections;
using Daeume.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Flow
{
    public readonly struct FlowStepReached
    {
        public FlowStepReached(SceneFlowStep step) => Step = step;
        public SceneFlowStep Step { get; }
    }

    public readonly struct OverlaySceneLoadRequested
    {
        public OverlaySceneLoadRequested(string sceneName, bool load)
        {
            SceneName = sceneName ?? string.Empty;
            Load = load;
        }

        public string SceneName { get; }
        public bool Load { get; }
    }

    public sealed class SceneFlowController : MonoBehaviour
    {
        [SerializeField] private string titleScene = "Title";
        [SerializeField] private string stageOneScene = "Stage01_Base";
        [SerializeField, Min(1)] private int maxHealth = 3;

        private readonly SceneFlowPlan plan = new();
        private SaveSystem saveSystem;
        private SaveData currentData;

        public bool IsTransitioning => plan.IsTransitioning;
        public SaveData CurrentData => currentData;

        private void Awake()
        {
            var path = System.IO.Path.Combine(Application.persistentDataPath, "daeume-save.json");
            var settingsPath = System.IO.Path.Combine(Application.persistentDataPath, "daeume-settings.json");
            saveSystem = new SaveSystem(new FileSaveStore(path), new FileSaveStore(settingsPath));
            currentData = saveSystem.Load(maxHealth).Data;
            GameManager.Instance?.Events.Subscribe<ChaseCheckpointRestoreRequested>(RestoreChaseCheckpoint);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.Events.Unsubscribe<ChaseCheckpointRestoreRequested>(RestoreChaseCheckpoint);
        }

        public bool StartNewGame()
        {
            if (!plan.TryBeginTransition())
            {
                return false;
            }

            currentData = saveSystem.CreateNewGame(maxHealth);
            saveSystem.Save(currentData, maxHealth);
            StartCoroutine(LoadContent(stageOneScene, StageState.Explore, currentData));
            return true;
        }

        public bool ContinueGame()
        {
            if (!plan.TryBeginTransition())
            {
                return false;
            }

            currentData = saveSystem.Load(maxHealth).Data;
            StartCoroutine(LoadContent(SceneForStage(currentData.CurrentStageId), StageState.Explore, currentData));
            return true;
        }

        public bool CompleteStageOne()
        {
            if (GameManager.Instance == null || GameManager.Instance.StageState != StageState.Chase ||
                !plan.TryBeginTransition())
            {
                return false;
            }

            StartCoroutine(StageOneResultToTitle());
            return true;
        }

        public void SaveChaseCheckpoint(string checkpointId, Vector2 playerPosition, int playerHealth, string variantId)
        {
            currentData.CheckpointId = checkpointId ?? string.Empty;
            currentData.PlayerPosition = playerPosition;
            currentData.PlayerHealth = Mathf.Clamp(playerHealth, 1, maxHealth);
            currentData.ContaminationVariantId = variantId ?? string.Empty;
            saveSystem.Save(currentData, maxHealth);
        }

        public void RequestOverlay(string sceneName, bool load)
        {
            GameManager.Instance?.Events.Publish(new OverlaySceneLoadRequested(sceneName, load));
        }

        private IEnumerator StageOneResultToTitle()
        {
            foreach (var step in plan.GetStageClearOrder())
            {
                GameManager.Instance?.Events.Publish(new FlowStepReached(step));
                switch (step)
                {
                    case SceneFlowStep.StageCleared:
                        GameManager.Instance?.SetStageState(StageState.Cleared);
                        break;
                    case SceneFlowStep.Save:
                        saveSystem.Save(currentData, maxHealth);
                        break;
                    case SceneFlowStep.SceneLoad:
                        yield return ReplaceContent(titleScene);
                        break;
                    case SceneFlowStep.Explore:
                        GameManager.Instance?.ResetStage();
                        break;
                    default:
                        yield return null;
                        break;
                }
            }

            plan.CompleteTransition();
        }

        private IEnumerator LoadContent(string sceneName, StageState state, SaveData data)
        {
            yield return ReplaceContent(sceneName);
            GameManager.Instance?.Events.Publish(new PlayerRestoreRequested(data.PlayerPosition, data.PlayerHealth));
            GameManager.Instance?.ResetStage(state);
            plan.CompleteTransition();
        }

        private void RestoreChaseCheckpoint(ChaseCheckpointRestoreRequested request)
        {
            if (plan.IsTransitioning)
            {
                return;
            }

            currentData = saveSystem.Load(maxHealth).Data;
            if (!string.IsNullOrEmpty(request.CheckpointId) && currentData.CheckpointId != request.CheckpointId)
            {
                return;
            }

            var health = SaveSystem.ResolveRespawnHealth(currentData, maxHealth, true, maxHealth);
            GameManager.Instance?.Events.Publish(new PlayerRestoreRequested(currentData.PlayerPosition, health));
            if (!string.IsNullOrEmpty(currentData.ContaminationVariantId))
            {
                RequestOverlay(currentData.ContaminationVariantId, true);
            }

            GameManager.Instance?.ResetStage(StageState.Chase);
        }

        private IEnumerator ReplaceContent(string sceneName)
        {
            for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.name == "Persistent" || scene.name == "Boot" || scene.name == sceneName)
                {
                    continue;
                }

                yield return SceneManager.UnloadSceneAsync(scene);
            }

            if (!SceneManager.GetSceneByName(sceneName).isLoaded)
            {
                yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            }

            var loaded = SceneManager.GetSceneByName(sceneName);
            if (loaded.IsValid())
            {
                SceneManager.SetActiveScene(loaded);
            }
        }

        private string SceneForStage(int stageId) => stageId == 1 ? stageOneScene : titleScene;
    }
}
