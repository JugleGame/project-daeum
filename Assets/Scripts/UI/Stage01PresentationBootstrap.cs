using Daeume.ContaminationRuntime;
using Daeume.Memory;
using Daeume.Stage;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.UI
{
    internal static class Stage01PresentationBootstrap
    {
        private const string StageSceneName = "Stage01_Base";
        private const string MemoryAnchorMarkerId = "stage01.memory.anchor.01";

        private static GameObject hudInstance;
        private static GameObject pressureInstance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != StageSceneName) return;
            SpawnMemoryAnchor(scene);
            SpawnPersistentPresentation();
        }

        private static void SpawnMemoryAnchor(Scene scene)
        {
            if (Object.FindAnyObjectByType<MemoryAnchor>() != null) return;

            var marker = FindMarker(MemoryAnchorMarkerId);
            var prefab = Resources.Load<GameObject>("Memory/Stage01_MemoryAnchor");
            if (marker == null || prefab == null) return;

            var instance = Object.Instantiate(prefab, marker.transform.position, marker.transform.rotation);
            SceneManager.MoveGameObjectToScene(instance, scene);

            var bridge = instance.GetComponentInChildren<MemoryCompletionBridge>();
            var chase = Object.FindAnyObjectByType<StageOneChaseController>();
            if (bridge != null && chase != null) bridge.Configure(chase);
        }

        private static void SpawnPersistentPresentation()
        {
            if (hudInstance == null)
            {
                var hudPrefab = Resources.Load<GameObject>("UI/Stage01_Presentation");
                if (hudPrefab != null)
                {
                    hudInstance = Object.Instantiate(hudPrefab);
                    Object.DontDestroyOnLoad(hudInstance);
                }
            }

            if (pressureInstance == null)
            {
                var pressurePrefab = Resources.Load<GameObject>("Presentation/Stage01_PressurePresentation");
                if (pressurePrefab != null)
                {
                    pressureInstance = Object.Instantiate(pressurePrefab);
                    Object.DontDestroyOnLoad(pressureInstance);
                }
            }
        }

        private static StageMarker FindMarker(string markerId)
        {
            foreach (var marker in Object.FindObjectsByType<StageMarker>(FindObjectsSortMode.None))
                if (marker.MarkerId == markerId) return marker;
            return null;
        }
    }
}
