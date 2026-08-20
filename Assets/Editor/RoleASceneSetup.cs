using Daeume.Core;
using Daeume.Flow;
using Daeume.Interaction;
using Daeume.Player;
using Daeume.Prototype;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Daeume.Editor
{
    /// <summary>
    /// Boot/Persistent/프로토타입 씬을 코드로 구성해 주는 개발용 도구다(에디터 전용).
    ///
    /// 씬을 손으로 만들면 사람마다 구성이 달라지고, 씬 파일은 병합이 불가능해 되돌리기도 어렵다.
    /// 그래서 "필요한 오브젝트와 컴포넌트를 코드로 다시 만들 수 있게" 해 둔 것이다.
    /// 씬이 깨졌을 때 복구용으로도 쓰인다.
    ///
    /// 주의: 이 코드는 Assets/Editor 폴더에 있어 게임 빌드에는 포함되지 않는다.
    /// 유니티는 Editor 폴더의 스크립트를 에디터 전용으로 취급한다.
    /// </summary>
    public static class RoleASceneSetup
    {
        public static void Configure()
        {
            ConfigureBoot();
            ConfigurePersistent();
            ConfigurePrototype();
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

        public static void BuildPrototypeWindows()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/RoleAPrototype.unity" },
                locationPathName = "Build/RoleAPrototype/DaeumeRoleAPrototype.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                throw new System.InvalidOperationException(
                    $"Prototype build failed: {report.summary.result}, errors={report.summary.totalErrors}");
            }

            Debug.Log($"ROLE_A_PROTOTYPE_BUILD_OK path={report.summary.outputPath} errors={report.summary.totalErrors}");
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
            var (controller, combat, trauma) = ConfigurePlayer(player);
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

        private static void ConfigurePrototype()
        {
            const string scenePath = "Assets/Scenes/RoleAPrototype.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var systems = new GameObject("PrototypeSystems");
            systems.AddComponent<GameManager>();
            var flow = systems.AddComponent<SceneFlowController>();

            var player = CreateBlock("Player", new Vector2(-7f, 0f), new Vector2(0.75f, 1f), new Color(0.27f, 0.65f, 0.83f));
            var (controller, combat, trauma) = ConfigurePlayer(player);
            player.GetComponent<CapsuleCollider2D>().size = new Vector2(0.75f, 1f);
            Ensure<InteractionTargeter>(player);
            var groundProbe = FindOrCreateChild(player.transform, "GroundProbe", new Vector3(0f, -0.52f, 0f));
            var attackOrigin = FindOrCreateChild(player.transform, "AttackOrigin", new Vector3(0.55f, 0f, 0f));
            SetObjectReference(controller, "groundProbe", groundProbe);
            SetObjectReference(combat, "attackOrigin", attackOrigin);
            SetObjectReference(trauma, "controller", controller);

            CreateSolidBlock("Ground", new Vector2(0f, -1.25f), new Vector2(20f, 0.5f), new Color(0.17f, 0.12f, 0.16f));

            var platform = CreateSolidBlock("OneWayPlatform", new Vector2(-3.2f, 0.4f), new Vector2(3f, 0.25f), new Color(0.88f, 0.58f, 0.29f));
            var platformCollider = platform.GetComponent<BoxCollider2D>();
            platformCollider.usedByEffector = true;
            platform.AddComponent<PlatformEffector2D>().useOneWay = true;
            CreateLabel("이동 · 점프 · 통과형 플랫폼", new Vector2(-3.2f, 1.15f));

            var wall = CreateSolidBlock("GrabWall", new Vector2(0.8f, 0f), new Vector2(0.45f, 2.5f), new Color(0.29f, 0.71f, 0.79f));
            var grabZone = new GameObject("GrabbableZone");
            grabZone.transform.position = new Vector2(0.45f, 0.75f);
            var grabCollider = grabZone.AddComponent<BoxCollider2D>();
            grabCollider.isTrigger = true;
            grabCollider.size = new Vector2(0.35f, 0.8f);
            grabZone.AddComponent<GrabbableSurface>();
            CreateLabel("K: 붙잡기", new Vector2(0.8f, 1.65f));

            var remnant = CreateBlock("RemnantDummy", new Vector2(3f, -0.5f), new Vector2(0.8f, 1f), new Color(0.75f, 0.18f, 0.2f));
            remnant.AddComponent<BoxCollider2D>().size = Vector2.one;
            remnant.AddComponent<PrototypeRemnant>();
            CreateLabel("J: 공격 3회", new Vector2(3f, 0.45f));

            var interactable = CreateBlock("InteractionDummy", new Vector2(5.2f, -0.5f), new Vector2(0.8f, 1f), new Color(1f, 0.85f, 0.35f));
            var interactionCollider = interactable.AddComponent<BoxCollider2D>();
            interactionCollider.size = Vector2.one;
            interactionCollider.isTrigger = true;
            interactable.AddComponent<PrototypeInteractable>();
            CreateLabel("E: 상호작용", new Vector2(5.2f, 0.45f));

            var checkpoint = CreateBlock("CheckpointMarker", new Vector2(6.7f, -0.5f), new Vector2(0.15f, 1f), new Color(0.3f, 0.9f, 0.9f));
            checkpoint.GetComponent<SpriteRenderer>().sortingOrder = -1;
            var checkpointCollider = checkpoint.AddComponent<BoxCollider2D>();
            checkpointCollider.size = Vector2.one;
            checkpointCollider.isTrigger = true;
            var prototypeCheckpoint = checkpoint.AddComponent<PrototypeCheckpoint>();
            SetObjectReference(prototypeCheckpoint, "flow", flow);
            var traumaSource = CreateBlock("TraumaDummy", new Vector2(8.1f, -0.35f), new Vector2(1f, 1.5f), new Color(0.04f, 0.03f, 0.05f));
            var traumaCollider = traumaSource.AddComponent<BoxCollider2D>();
            traumaCollider.size = Vector2.one;
            traumaCollider.isTrigger = true;
            traumaSource.AddComponent<TraumaContactSource>();
            CreateLabel("접촉: Checkpoint 복귀", new Vector2(8.1f, 0.65f));

            var cameraObject = new GameObject("PrototypeCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(-4f, 1.25f, -10f);
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.21875f;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.13f);
            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<PrototypeCameraFollow>();
            SetObjectReference(follow, "target", player.transform);

            var harness = systems.AddComponent<PrototypeHarness>();

            EditorSceneManager.SaveScene(scene, scenePath);
            EnsureBuildScene(scenePath);
        }

        private static (PlayerController controller, PlayerCombat combat, TraumaContactHandler trauma) ConfigurePlayer(GameObject player)
        {
            var input = Ensure<PlayerInput>(player);
            input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/Settings/DaeumeInputActions.inputactions");
            input.defaultActionMap = "Player";
            input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            var body = Ensure<Rigidbody2D>(player);
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            Ensure<CapsuleCollider2D>(player);
            Ensure<PlayerHealth>(player);
            return (Ensure<PlayerController>(player), Ensure<PlayerCombat>(player), Ensure<TraumaContactHandler>(player));
        }

        private static GameObject CreateSolidBlock(string name, Vector2 position, Vector2 size, Color color)
        {
            var gameObject = CreateBlock(name, position, size, color);
            gameObject.AddComponent<BoxCollider2D>().size = Vector2.one;
            return gameObject;
        }

        private static GameObject CreateBlock(string name, Vector2 position, Vector2 size, Color color)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.position = position;
            gameObject.transform.localScale = size;
            var renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
            renderer.color = color;
            var visual = gameObject.AddComponent<PrototypeVisual>();
            var serialized = new SerializedObject(visual);
            serialized.FindProperty("color").colorValue = color;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return gameObject;
        }

        private static void CreateLabel(string text, Vector2 position)
        {
            var gameObject = new GameObject($"Label_{text}");
            gameObject.transform.position = position;
            var label = gameObject.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 32;
            label.characterSize = 0.08f;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = Color.white;
        }

        private static void EnsureBuildScene(string path)
        {
            if (EditorBuildSettings.scenes.Any(scene => scene.path == path))
            {
                return;
            }

            EditorBuildSettings.scenes = EditorBuildSettings.scenes
                .Concat(new[] { new EditorBuildSettingsScene(path, true) })
                .ToArray();
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
