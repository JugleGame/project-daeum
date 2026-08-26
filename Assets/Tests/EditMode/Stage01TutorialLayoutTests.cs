using System.Linq;
using Daeume.Player;
using Daeume.Stage;
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

        /// <summary>
        /// 추격은 오른쪽(x 30)에서 왼쪽(탈출구)으로 달리므로, 구덩이(x 5.6~15.0) 왼쪽에 있는
        /// 탐색용 FallRecovery로 복귀시키면 낙사가 구덩이를 건너뛰는 지름길이 된다.
        /// 추격용 복귀 지점은 반드시 구덩이 오른쪽(진행 전) 지면에 있어야 한다.
        /// </summary>
        [Test]
        public void Test_Stage01_ChaseFallRecoveryIsBeyondThePit()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var markers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StageMarker>(true))
                .ToArray();

            var fall = markers.Single(marker => marker.Kind == StageMarkerKind.FallRecovery);
            var chaseFall = markers.Single(marker => marker.Kind == StageMarkerKind.ChaseFallRecovery);

            Assert.That(chaseFall.transform.position.x, Is.GreaterThan(15f),
                "추격 복귀 지점은 구덩이 오른쪽 끝(x 15.0) 너머 지면에 있어야 한다.");
            Assert.That(chaseFall.transform.position.x, Is.GreaterThan(fall.transform.position.x),
                "추격 복귀 지점이 탐색용보다 왼쪽에 있으면 낙사가 지름길이 된다.");
        }

        private static Transform Find(Transform root, string name)
        {
            return root.GetComponentsInChildren<Transform>(true)
                .Single(transform => transform.name == name);
        }
    }
}