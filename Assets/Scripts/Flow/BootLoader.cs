using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Flow
{
    public sealed class BootLoader : MonoBehaviour
    {
        [SerializeField] private string persistentScene = "Persistent";
        [SerializeField] private string titleScene = "Title";

        private IEnumerator Start()
        {
            if (!SceneManager.GetSceneByName(persistentScene).isLoaded)
            {
                yield return SceneManager.LoadSceneAsync(persistentScene, LoadSceneMode.Additive);
            }

            if (!SceneManager.GetSceneByName(titleScene).isLoaded)
            {
                yield return SceneManager.LoadSceneAsync(titleScene, LoadSceneMode.Additive);
            }

            var title = SceneManager.GetSceneByName(titleScene);
            if (title.IsValid())
            {
                SceneManager.SetActiveScene(title);
            }
        }
    }
}
