using System.Linq;
using Daeume.ContaminationRuntime;
using Daeume.Encounter;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    public sealed class Stage05LayoutTests
    {
        private const string ScenePath = "Assets/Scenes/Stage05_Base.unity";

        [Test]
        public void Test_Stage05_ContainsProjectRoomRouteAndRequiredMarkers()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Find(scene, "Stage05BaseRoot");
            var definition = root.GetComponent<StageDefinition>();
            var bounds = root.GetComponent<StageCameraBounds>();
            var markers = root.GetComponentsInChildren<StageMarker>(true);
            Assert.That(definition.Data.StageId, Is.EqualTo(5));
            Assert.That(bounds, Is.Not.Null);
            Assert.That(markers.Select(marker => marker.MarkerId), Has.None.Empty);
            Assert.That(markers.Select(marker => marker.MarkerId).Distinct().Count(), Is.EqualTo(markers.Length));
            Assert.That(markers.Select(marker => marker.MarkerId), Is.All.StartsWith("stage05."));
            foreach (var zone in new[] { "Zone_A_EntryWorkbench", "Zone_B_StorageCloset", "Zone_C_MainWorkFloor", "Zone_D_QuietCorner", "Zone_E_Chase" })
                Assert.That(Find(scene, zone), Is.Not.Null);

            var kinds = markers.Select(marker => marker.Kind).ToArray();
            foreach (var required in new[] { StageMarkerKind.Start, StageMarkerKind.FallRecovery, StageMarkerKind.EncounterTrigger, StageMarkerKind.EncounterExit, StageMarkerKind.MemoryAnchor, StageMarkerKind.ChaseStart, StageMarkerKind.Escape })
                Assert.That(kinds, Does.Contain(required), $"Stage05 is missing a {required} marker.");
            Assert.That(kinds.Count(kind => kind == StageMarkerKind.EncounterTrigger), Is.EqualTo(3));
            Assert.That(markers.All(marker => marker.transform.position.x >= bounds.Minimum.x && marker.transform.position.x <= bounds.Maximum.x &&
                                             marker.transform.position.y >= bounds.Minimum.y && marker.transform.position.y <= bounds.Maximum.y), Is.True,
                "Stage05 progression markers must remain inside playable camera bounds.");
        }

        [Test]
        public void Test_Stage05_EncountersMatchProjectRoomDensityAndWaves()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controllers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Stage03EncounterController>(true))
                .OrderBy(controller => controller.Data.EncounterId).ToArray();
            Assert.That(controllers, Has.Length.EqualTo(3));
            AssertComposition(controllers[0], 2, 1, 1);
            AssertComposition(controllers[1], 3, 0, 1);
            AssertComposition(controllers[2], 3, 2, 2);
            Assert.That(controllers.Select(controller => controller.Data.EncounterId), Is.EqualTo(new[]
            {
                "stage05.encounter.01", "stage05.encounter.02", "stage05.encounter.03"
            }));
            Assert.That(controllers.All(controller => controller.Data.TerrainHazardIds.Count == 0), Is.True);
        }

        [Test]
        public void Test_Chase_Stage05ReusesMainFloorDesksWithoutNewObstacleSystem()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var desk in new[] { "DeskRow_01", "DeskRow_02", "DeskRow_03" })
            {
                var deskObject = Find(scene, desk);
                Assert.That(deskObject.GetComponent<Collider2D>(), Is.Not.Null, $"{desk} must remain a physical chase obstacle.");
                Assert.That(deskObject.GetComponent<SpriteRenderer>(), Is.Not.Null, $"{desk} must remain visually identifiable.");
            }

            var chaseStart = Find(scene, "Stage05BaseRoot").GetComponentsInChildren<StageMarker>(true).Single(marker => marker.Kind == StageMarkerKind.ChaseStart);
            var escape = Find(scene, "Stage05BaseRoot").GetComponentsInChildren<StageMarker>(true).Single(marker => marker.Kind == StageMarkerKind.Escape);
            Assert.That(escape.transform.position.x, Is.LessThan(chaseStart.transform.position.x));
        }

        [Test]
        public void Test_Stage05_UsesDedicatedVariantAndKeepsOverlaysNonBlocking()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var director = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ContaminationDirector>(true)).Single();
            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage05.asset");
            Assert.That(director.Data.VariantId, Is.EqualTo("Stage05_Overlay_Intrusion"));
            Assert.That(director.Data.VariantId, Is.EqualTo(stage.ContaminationVariantId));
            foreach (var overlayName in new[] { director.Data.EchoOverlayName, director.Data.IntrusionOverlayName })
            {
                var overlay = scene.GetRootGameObjects().Single(root => root.name == overlayName);
                Assert.That(overlay.activeSelf, Is.False);
                Assert.That(overlay.GetComponentsInChildren<Collider2D>(true), Is.Not.Empty);
                Assert.That(overlay.GetComponentsInChildren<Collider2D>(true).All(collider => collider.isTrigger), Is.True);
            }
        }

        [Test]
        public void Test_Stage05_SceneAndReferencesAreRegisteredWithoutMissingAssets()
        {
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path == ScenePath && entry.enabled), Is.True);
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path.Contains("Stage05_Overlay_")), Is.False);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)))
                Assert.That(component, Is.Not.Null, "Stage05 scene contains a missing script reference.");
        }

        private static void AssertComposition(Stage03EncounterController controller, int melee, int dash, int waves)
        {
            Assert.That(controller.MeleeSpawnPoints.Count, Is.EqualTo(melee), controller.Data.EncounterId);
            Assert.That(controller.DashSpawnPoints.Count, Is.EqualTo(dash), controller.Data.EncounterId);
            Assert.That(controller.Data.SpawnCount, Is.EqualTo(melee + dash), controller.Data.EncounterId);
            Assert.That(controller.Data.WaveCount, Is.EqualTo(waves), controller.Data.EncounterId);
        }

        private static GameObject Find(Scene scene, string name)
        {
            var found = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == name);
            Assert.That(found, Is.Not.Null, $"Missing Stage05 object: {name}");
            return found.gameObject;
        }
    }
}
