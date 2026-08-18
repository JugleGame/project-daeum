using System.Linq;
using Daeume.Player;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    public sealed class StageLayoutTests
    {
        private const string ScenePath = "Assets/Scenes/Stage01_Base.unity";

        [Test]
        public void Test_Stage01_ContainsDataCameraBoundsAndUniqueRequiredMarkers()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Find(scene, "Stage01BaseRoot");
            var definition = root.GetComponent<StageDefinition>();
            var bounds = root.GetComponent<StageCameraBounds>();
            var markers = root.GetComponentsInChildren<StageMarker>(true);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Data, Is.Not.Null);
            Assert.That(definition.Data.StageId, Is.EqualTo(1));
            Assert.That(bounds, Is.Not.Null);
            Assert.That(bounds.Minimum.x, Is.LessThan(bounds.Maximum.x));
            Assert.That(markers.Select(marker => marker.MarkerId), Has.None.Empty);
            Assert.That(markers.Select(marker => marker.MarkerId).Distinct().Count(), Is.EqualTo(markers.Length));
            Assert.That(markers.Count(marker => marker.Kind == StageMarkerKind.RemnantSpawn), Is.GreaterThanOrEqualTo(1));
            Assert.That(markers.Select(marker => marker.Kind), Does.Contain(StageMarkerKind.Start));
            Assert.That(markers.Select(marker => marker.Kind), Does.Contain(StageMarkerKind.FallRecovery));
            Assert.That(markers.Select(marker => marker.Kind), Does.Contain(StageMarkerKind.EncounterTrigger));
            Assert.That(markers.Select(marker => marker.Kind), Does.Contain(StageMarkerKind.EncounterExit));
            Assert.That(markers.Select(marker => marker.Kind), Does.Contain(StageMarkerKind.MemoryAnchor));
            Assert.That(markers.Select(marker => marker.Kind), Does.Contain(StageMarkerKind.ChaseStart));
            Assert.That(markers.Select(marker => marker.Kind), Does.Contain(StageMarkerKind.Escape));
        }

        [Test]
        public void Test_Stage01_BlockoutUsesCompatibleCollidersAndAuthoredRecoveryRoute()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var startGround = Find(scene, "Ground_Start");
            var recoveryFloor = Find(scene, "FallRecovery_Floor");
            var oneWay = Find(scene, "OneWayPlatform");
            var grabZone = Find(scene, "GrabWall_Zone");

            Assert.That(startGround.GetComponent<BoxCollider2D>().isTrigger, Is.False);
            Assert.That(recoveryFloor.GetComponent<BoxCollider2D>().isTrigger, Is.False);
            Assert.That(recoveryFloor.transform.position.y, Is.LessThan(startGround.transform.position.y));
            Assert.That(Find(scene, "FallRecovery_Step01").GetComponent<BoxCollider2D>(), Is.Not.Null);
            Assert.That(Find(scene, "FallRecovery_Step02").GetComponent<BoxCollider2D>(), Is.Not.Null);
            Assert.That(oneWay.GetComponent<BoxCollider2D>().usedByEffector, Is.True);
            Assert.That(oneWay.GetComponent<PlatformEffector2D>().useOneWay, Is.True);
            Assert.That(grabZone.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(grabZone.GetComponent<GrabbableSurface>(), Is.Not.Null);
            Assert.That(startGround.layer, Is.EqualTo(0));
            Assert.That(Physics2D.GetIgnoreLayerCollision(0, 0), Is.False);
        }

        private static GameObject Find(Scene scene, string name)
        {
            var found = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == name);
            Assert.That(found, Is.Not.Null, $"Missing Stage01 object: {name}");
            return found.gameObject;
        }
    }
}
