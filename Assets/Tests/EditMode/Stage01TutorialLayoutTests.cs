using System.Linq;
using Daeume.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class Stage01TutorialLayoutTests
    {
        private const string ScenePath = "Assets/Scenes/Stage01_Base.unity";

        [Test]
        public void Test_Stage01_TutorialTraversalHasOrderedMechanicBeats()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var all = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .ToArray();

            var traversal = all.Single(transform => transform.name == "TutorialTraversal");
            var moveJump = Find(traversal, "01_MoveJump");
            var combat = Find(traversal, "02_CombatIntroduction");
            var grab = Find(traversal, "03_GrabAscent");
            var elevated = Find(traversal, "04_ElevatedCombat");
            var descent = Find(traversal, "05_Descent");

            Assert.That(moveJump.position.x, Is.LessThan(combat.position.x));
            Assert.That(combat.position.x, Is.LessThan(grab.position.x));
            Assert.That(grab.position.x, Is.LessThan(elevated.position.x));
            Assert.That(elevated.position.x, Is.LessThan(descent.position.x));

            var jumpObstacle = Find(moveJump, "Tutorial_JumpObstacle").GetComponent<BoxCollider2D>();
            Assert.That(jumpObstacle, Is.Not.Null);
            Assert.That(jumpObstacle.isTrigger, Is.False);
            Assert.That(jumpObstacle.bounds.max.y, Is.GreaterThan(-0.65f));
            Assert.That(jumpObstacle.bounds.max.y, Is.LessThanOrEqualTo(0.1f));

            var grabTrigger = Find(grab, "Tutorial_GrabTrigger");
            Assert.That(grabTrigger.GetComponent<GrabbableSurface>(), Is.Not.Null);
            var triggerCollider = grabTrigger.GetComponent<BoxCollider2D>();
            Assert.That(triggerCollider, Is.Not.Null);
            Assert.That(triggerCollider.isTrigger, Is.True);
            Assert.That(triggerCollider.bounds.size.y, Is.GreaterThanOrEqualTo(1.5f));
            Assert.That(grabTrigger.position.x, Is.InRange(11.25f, 14.25f));

            var terrace = Find(elevated, "Tutorial_ElevatedCombatFloor").GetComponent<BoxCollider2D>();
            Assert.That(terrace, Is.Not.Null);
            Assert.That(terrace.isTrigger, Is.False);
            Assert.That(terrace.bounds.max.y, Is.GreaterThanOrEqualTo(-0.05f));
        }

        [Test]
        public void Test_Stage01_TutorialTraversalReusesStagePrefabs()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var traversal = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .Single(transform => transform.name == "TutorialTraversal");

            var prefabPaths = traversal.GetComponentsInChildren<Transform>(true)
                .Select(transform => PrefabUtility.GetCorrespondingObjectFromSource(transform.gameObject))
                .Where(source => source != null)
                .Select(AssetDatabase.GetAssetPath)
                .Where(path => path.StartsWith("Assets/Prefabs/Stage/Stage01/"))
                .Distinct()
                .ToArray();

            Assert.That(prefabPaths.Length, Is.GreaterThanOrEqualTo(5));
            Assert.That(prefabPaths, Has.Some.EndsWith("05-lamp-utility-pole.prefab"));
            Assert.That(prefabPaths, Has.Some.EndsWith("13-maintenance-platform.prefab"));
            Assert.That(prefabPaths, Has.Some.Contains("BackgroundProps"));
        }

        private static Transform Find(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Single(transform => transform.name == name);
        }
    }
}