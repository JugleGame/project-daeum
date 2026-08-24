using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Daeume.Tests.EditMode
{
    /// <summary>Issue #53: Stage01 카메라 프레이밍, Player 프리팹, Echo 발판 회귀 테스트.</summary>
    public sealed class Stage01FramingTests
    {
        [Test]
        public void Test_Stage01_GroundFramingPlayerPrefabAndEchoCleanup()
        {
            var stage = EditorSceneManager.OpenScene(
                "Assets/Scenes/Stage01_Base.unity",
                OpenSceneMode.Single);

            var cameraBounds = FindInScene<StageCameraBounds>(stage);
            var ground = FindNamedInScene<Tilemap>(stage, "GroundTilemap");
            Assert.That(cameraBounds, Is.Not.Null);
            Assert.That(ground, Is.Not.Null);
            Assert.That(cameraBounds.FixedCameraY, Is.EqualTo(1.875f));

            var groundRenderer = ground.GetComponent<TilemapRenderer>();
            Assert.That(groundRenderer, Is.Not.Null);

            // Persistent 씬을 열면 Stage01이 닫히므로 비교에 쓸 값을 먼저 붙잡아 둔다.
            var fixedCameraY = cameraBounds.FixedCameraY;
            var groundBottom = groundRenderer.bounds.min.y;

            var echoRoot = FindNamedInScene<Transform>(stage, "Stage01_Overlay_Echo");
            Assert.That(echoRoot, Is.Not.Null);
            Assert.That(FindChild(echoRoot, "EchoStep_A"), Is.Null);
            Assert.That(FindChild(echoRoot, "EchoStep_B"), Is.Null);

            var intrusionRoot = FindNamedInScene<Transform>(stage, "Stage01_Overlay_Intrusion");
            Assert.That(intrusionRoot, Is.Not.Null);
            Assert.That(FindChild(intrusionRoot, "IntrusionStep_A"), Is.Null);
            Assert.That(FindChild(intrusionRoot, "IntrusionStep_B"), Is.Null);
            Assert.That(intrusionRoot.GetComponentsInChildren<Collider2D>(true), Is.Empty);


            var persistent = EditorSceneManager.OpenScene(
                "Assets/Scenes/Persistent.unity",
                OpenSceneMode.Single);
            var mainCamera = FindMainCamera(persistent);
            Assert.That(mainCamera, Is.Not.Null);

            Component pixelPerfect = null;
            foreach (var component in mainCamera.GetComponents<Component>())
            {
                if (component != null && component.GetType().Name == "PixelPerfectCamera")
                {
                    pixelPerfect = component;
                    break;
                }
            }

            Assert.That(pixelPerfect, Is.Not.Null);
            var pixelPerfectSettings = new SerializedObject(pixelPerfect);
            Assert.That(
                pixelPerfectSettings.FindProperty("m_RefResolutionX").intValue,
                Is.EqualTo(384));
            Assert.That(
                pixelPerfectSettings.FindProperty("m_RefResolutionY").intValue,
                Is.EqualTo(216));
            Assert.That(
                pixelPerfectSettings.FindProperty("m_AssetsPPU").intValue,
                Is.EqualTo(32));

            // 직교 크기의 주인은 PixelPerfectCamera다. StageCameraBounds는 세로 기준만 정한다.
            var orthographicSize =
                pixelPerfectSettings.FindProperty("m_RefResolutionY").intValue
                / (2f * pixelPerfectSettings.FindProperty("m_AssetsPPU").intValue);
            Assert.That(mainCamera.orthographicSize, Is.EqualTo(orthographicSize).Within(0.001f));
            Assert.That(fixedCameraY - orthographicSize, Is.EqualTo(groundBottom).Within(0.001f));

            SpriteRenderer playerVisual = null;
            Collider2D playerCollider = null;
            GameObject playerPrefabRoot = null;
            foreach (var root in persistent.GetRootGameObjects())
            {
                foreach (var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                {
                    if (renderer.gameObject.name != "Visual"
                        || renderer.sprite == null
                        || AssetDatabase.GetAssetPath(renderer.sprite)
                            != "Assets/RoleB/Placeholders/BlockoutWhite.asset")
                        continue;

                    var collider = renderer.GetComponentInParent<Collider2D>();
                    var prefabRoot = PrefabUtility.GetNearestPrefabInstanceRoot(renderer.gameObject);
                    if (collider == null || prefabRoot == null || prefabRoot.name != "Player")
                        continue;

                    playerVisual = renderer;
                    playerCollider = collider;
                    playerPrefabRoot = prefabRoot;
                    break;
                }

                if (playerVisual != null)
                    break;
            }

            Assert.That(playerVisual, Is.Not.Null);
            Assert.That(playerVisual.enabled, Is.True);
            Assert.That(playerCollider, Is.Not.Null);
            Assert.That(playerCollider.enabled, Is.True);
            Assert.That(
                PrefabUtility.GetPrefabInstanceStatus(playerPrefabRoot),
                Is.EqualTo(PrefabInstanceStatus.Connected));
            Assert.That(
                AssetDatabase.GetAssetPath(
                    PrefabUtility.GetCorrespondingObjectFromSource(playerPrefabRoot)),
                Is.EqualTo("Assets/Prefabs/Player/Player.prefab"));
        }

        private static Transform FindChild(Transform parent, string name)
        {
            foreach (var child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child != parent && child.name == name)
                    return child;
            }

            return null;
        }

        private static Camera FindMainCamera(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                {
                    if (camera.CompareTag("MainCamera"))
                        return camera;
                }
            }

            return null;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                var component = root.GetComponentInChildren<T>(true);
                if (component != null)
                    return component;
            }

            return null;
        }

        private static T FindNamedInScene<T>(Scene scene, string name) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var component in root.GetComponentsInChildren<T>(true))
                {
                    if (component.gameObject.name == name)
                        return component;
                }
            }

            return null;
        }
    }
}
