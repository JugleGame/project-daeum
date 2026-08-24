using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    /// <summary>Issue #53: URP 2D용 Stage01 하늘 스프라이트 연결 및 화면 커버 회귀 테스트.</summary>
    public sealed class Stage01SkyBackgroundTests
    {
        [Test]
        public void Test_Stage01_SkySpriteCoversPersistentCameraBehindGameplay()
        {
            var stage = EditorSceneManager.OpenScene(
                "Assets/Scenes/Stage01_Base.unity",
                OpenSceneMode.Single);

            Assert.That(RenderSettings.skybox, Is.Null);

            var skyObject = FindRoot(stage, "StageSkyBackground");
            Assert.That(skyObject, Is.Not.Null);

            var renderer = skyObject.GetComponent<SpriteRenderer>();
            var background = skyObject.GetComponent<StageSkyBackground>();
            Assert.That(renderer, Is.Not.Null);
            Assert.That(background, Is.Not.Null);
            Assert.That(renderer.sprite, Is.Not.Null);
            Assert.That(renderer.sprite.name, Is.EqualTo("sky-late-afternoon"));
            Assert.That(renderer.sortingLayerName, Is.EqualTo("Background"));
            Assert.That(renderer.sortingOrder, Is.EqualTo(-1000));
            Assert.That(renderer.sharedMaterial, Is.Not.Null);
            Assert.That(renderer.sharedMaterial.name, Is.EqualTo("Player_Unlit"));

            var cameraObject = new GameObject("SkyCoverageTestCamera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.21875f;
            camera.aspect = 16f / 9f;
            cameraObject.transform.position = new Vector3(12f, 1f, -10f);

            background.RefreshNow();

            var spriteWorldSize = Vector2.Scale(
                renderer.sprite.bounds.size,
                skyObject.transform.lossyScale);
            var requiredHeight = camera.orthographicSize * 2f;
            var requiredWidth = requiredHeight * camera.aspect;
            Assert.That(spriteWorldSize.x, Is.GreaterThanOrEqualTo(requiredWidth));
            Assert.That(spriteWorldSize.y, Is.GreaterThanOrEqualTo(requiredHeight));
            Assert.That(skyObject.transform.position.x, Is.EqualTo(cameraObject.transform.position.x));
            Assert.That(skyObject.transform.position.y, Is.EqualTo(cameraObject.transform.position.y));

            Object.DestroyImmediate(cameraObject);

            var persistent = EditorSceneManager.OpenScene(
                "Assets/Scenes/Persistent.unity",
                OpenSceneMode.Single);
            var mainCamera = FindMainCamera(persistent);
            Assert.That(mainCamera, Is.Not.Null);
            Assert.That(mainCamera.clearFlags, Is.EqualTo(CameraClearFlags.SolidColor));
        }

        private static GameObject FindRoot(Scene scene, string name)
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == name)
                    return root;
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
    }
}
