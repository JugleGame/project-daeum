using System.Linq;
using Daeume.ContaminationRuntime;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    public sealed class RoleBIntegrationQaTests
    {
        // 오버레이는 Stage01_Base 안의 루트 오브젝트다(#38). 씬 파일은 더 이상 없다.
        private static readonly string[] OwnedScenePaths =
        {
            "Assets/Scenes/Stage01_Base.unity"
        };

        [Test]
        public void Test_RoleB_OwnedScenesHaveNoMissingScriptsAndChaseWiringIsComplete()
        {
            foreach (var path in OwnedScenePaths)
            {
                var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                var objects = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .Select(transform => transform.gameObject)
                    .ToArray();
                Assert.That(objects.Sum(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount), Is.Zero, path);
            }

            var stage = EditorSceneManager.OpenScene(OwnedScenePaths[0], OpenSceneMode.Single);
            var roots = stage.GetRootGameObjects();
            var director = roots.SelectMany(root => root.GetComponentsInChildren<ContaminationDirector>(true)).Single();
            var chase = roots.SelectMany(root => root.GetComponentsInChildren<StageOneChaseController>(true)).Single();
            var trauma = roots.SelectMany(root => root.GetComponentsInChildren<TraumaChaseActor>(true)).Single();
            var signals = roots.SelectMany(root => root.GetComponentsInChildren<ChaseRouteSignal>(true)).ToArray();

            Assert.That(director.Data, Is.Not.Null);
            Assert.That(chase.Director, Is.SameAs(director));
            Assert.That(trauma.gameObject.activeSelf, Is.False);
            Assert.That(signals, Has.Length.EqualTo(3));
            Assert.That(signals, Has.All.Matches<ChaseRouteSignal>(signal => signal.HasNonColorCue));
        }
    }
}
