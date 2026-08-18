using System.Collections;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.ContaminationRuntime
{
    public sealed class OverlaySceneLoader : MonoBehaviour
    {
        public string LastRequestedScene { get; private set; } = string.Empty;
        public bool LastRequestWasLoad { get; private set; }

        private void Awake()
        {
            GameManager.Instance?.Events.Subscribe<OverlaySceneLoadRequested>(HandleRequest);
        }

        private void OnDestroy()
        {
            GameManager.Instance?.Events.Unsubscribe<OverlaySceneLoadRequested>(HandleRequest);
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
