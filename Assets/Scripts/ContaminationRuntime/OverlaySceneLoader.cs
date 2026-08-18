using System.Collections;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.ContaminationRuntime
{
    public sealed class OverlaySceneLoader : MonoBehaviour
    {
        private GameManager subscribedManager;

        public string LastRequestedScene { get; private set; } = string.Empty;
        public bool LastRequestWasLoad { get; private set; }

        private void Awake() => EnsureSubscribed();

        private void OnEnable() => EnsureSubscribed();

        private void Start() => EnsureSubscribed();

        private void OnDisable() => Unsubscribe();

        private void OnDestroy() => Unsubscribe();

        private void EnsureSubscribed()
        {
            var manager = GameManager.Instance;
            if (manager == null || manager == subscribedManager) return;
            Unsubscribe();
            subscribedManager = manager;
            subscribedManager.Events.Subscribe<OverlaySceneLoadRequested>(HandleRequest);
        }

        private void Unsubscribe()
        {
            if (subscribedManager == null) return;
            subscribedManager.Events.Unsubscribe<OverlaySceneLoadRequested>(HandleRequest);
            subscribedManager = null;
        }

        public void HandleRequest(OverlaySceneLoadRequested request)
        {
            StartCoroutine(ApplyRequest(request.SceneName, request.Load));
        }

        public IEnumerator ApplyRequest(string sceneName, bool load)
        {
            LastRequestedScene = sceneName ?? string.Empty;
            LastRequestWasLoad = load;
            if (string.IsNullOrWhiteSpace(sceneName)) yield break;

            var scene = SceneManager.GetSceneByName(sceneName);
            if (load)
            {
                if (!scene.isLoaded) yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                yield break;
            }

            if (scene.isLoaded) yield return SceneManager.UnloadSceneAsync(sceneName);
        }
    }
}
