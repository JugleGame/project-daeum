using Daeume.Core;
using Daeume.Flow;
using Daeume.Interaction;
using Daeume.Player;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Editor
{
    public static class RoleASceneSetup
    {
        public static void Configure()
        {
            ConfigureBoot();
            ConfigurePersistent();
            AssetDatabase.SaveAssets();
        }

        public static void BuildWindows()
        {
            var scenes = EditorBuildSettingsScene.GetActiveSceneList(EditorBuildSettings.scenes);
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = "Build/RoleA/Daeume.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException(
                    $"Build failed: {report.summary.result}, errors={report.summary.totalErrors}");
            }

            Debug.Log($"ROLE_A_BUILD_OK path={report.summary.outputPath} errors={report.summary.totalErrors}");
        }

        private static void ConfigureBoot()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Boot.unity", OpenSceneMode.Single);
            Ensure<BootLoader>(GameObject.Find("BootRoot"));
            EditorSceneManager.SaveScene(scene);
        }

        private static void ConfigurePersistent()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Persistent.unity", OpenSceneMode.Single);
            var systems = GameObject.Find("Systems");
            Ensure<GameManager>(systems);
            Ensure<SceneFlowController>(systems);

            var player = GameObject.Find("Player");
            var input = Ensure<PlayerInput>(player);
            input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/DaeumeInputActions.inputactions");
            input.defaultActionMap = "Player";
            input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            var body = Ensure<Rigidbody2D>(player);
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Ensure<CapsuleCollider2D>(player);
            var controller = Ensure<PlayerController>(player);
            Ensure<PlayerHealth>(player);
            var combat = Ensure<PlayerCombat>(player);
            var trauma = Ensure<TraumaContactHandler>(player);
            Ensure<InteractionTargeter>(player);

            var groundProbe = FindOrCreateChild(player.transform, "GroundProbe", new Vector3(0f, -0.52f, 0f));
            var attackOrigin = FindOrCreateChild(player.transform, "AttackOrigin", new Vector3(0.55f, 0f, 0f));
            SetObjectReference(controller, "groundProbe", groundProbe);
            SetObjectReference(combat, "attackOrigin", attackOrigin);
            SetObjectReference(trauma, "controller", controller);

            var camera = Object.FindFirstObjectByType<Camera>();
            if (camera != null)
            {
                camera.orthographic = true;
                camera.orthographicSize = 4.21875f;
                var pixelPerfect = camera.GetComponents<MonoBehaviour>()
                    .FirstOrDefault(component => component.GetType().Name == "PixelPerfectCamera");
                if (pixelPerfect != null)
                {
                    var serialized = new SerializedObject(pixelPerfect);
                    serialized.FindProperty("m_UpscaleRT").boolValue = true;
                    serialized.FindProperty("m_PixelSnapping").boolValue = true;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.SaveScene(scene);
        }

        private static T Ensure<T>(GameObject gameObject) where T : Component
        {
            var existing = gameObject.GetComponent<T>();
            return existing == null ? gameObject.AddComponent<T>() : existing;
        }

        private static Transform FindOrCreateChild(Transform parent, string name, Vector3 localPosition)
        {
            var child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }

            child.localPosition = localPosition;
            return child;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
