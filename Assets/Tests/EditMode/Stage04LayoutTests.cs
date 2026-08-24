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
    public sealed class Stage04LayoutTests
    {
        private const string ScenePath = "Assets/Scenes/Stage04_Base.unity";

        [Test]
        public void Test_Stage04_ContainsDormitoryRouteAndRequiredMarkers()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Find(scene, "Stage04BaseRoot");
            var definition = root.GetComponent<StageDefinition>();
            var markers = root.GetComponentsInChildren<StageMarker>(true);
            Assert.That(definition.Data.StageId, Is.EqualTo(4));
            var bounds = root.GetComponent<StageCameraBounds>();
            Assert.That(bounds, Is.Not.Null);
            Assert.That(markers.Select(marker => marker.MarkerId), Has.None.Empty);
            Assert.That(markers.Select(marker => marker.MarkerId).Distinct().Count(), Is.EqualTo(markers.Length));
            Assert.That(markers.Select(marker => marker.MarkerId), Is.All.StartsWith("stage04."));
            foreach (var zone in new[] { "Zone_A_CommonLounge", "Zone_B_Hallway", "Zone_C_Stairs", "Zone_D_Bedroom", "Zone_E_Chase" })
                Assert.That(Find(scene, zone), Is.Not.Null);

            var kinds = markers.Select(marker => marker.Kind).ToArray();
            foreach (var required in new[] { StageMarkerKind.Start, StageMarkerKind.FallRecovery, StageMarkerKind.EncounterTrigger, StageMarkerKind.EncounterExit, StageMarkerKind.MemoryAnchor, StageMarkerKind.ChaseStart, StageMarkerKind.Escape })
                Assert.That(kinds, Does.Contain(required), $"Stage04 is missing a {required} marker.");
            Assert.That(kinds.Count(kind => kind == StageMarkerKind.EncounterTrigger), Is.EqualTo(3));
            Assert.That(markers.All(marker => marker.transform.position.x >= bounds.Minimum.x &&
                                             marker.transform.position.x <= bounds.Maximum.x), Is.True,
                "Stage04 progression markers must remain inside playable camera bounds.");
        }

        [Test]
        public void Test_Stage04_EncountersMatchLoungeHallwayAndStairsDesign()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controllers = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Stage03EncounterController>(true)).OrderBy(controller => controller.Data.EncounterId).ToArray();
            Assert.That(controllers, Has.Length.EqualTo(3));
            AssertComposition(controllers[0], 1, 1);
            AssertComposition(controllers[1], 2, 0);
            AssertComposition(controllers[2], 0, 2);
            Assert.That(controllers.Select(controller => controller.Data.EncounterId), Is.EqualTo(new[] { "stage04.encounter.01", "stage04.encounter.02", "stage04.encounter.03" }));
            Assert.That(controllers.All(controller => controller.Data.WaveCount == 1), Is.True);
            Assert.That(controllers.All(controller => controller.Data.TerrainHazardIds.Count == 0), Is.True);
        }

        [Test]
        public void Test_Stage04_UsesDedicatedVariantAndKeepsOverlaysNonBlocking()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var director = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ContaminationDirector>(true)).Single();
            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage04.asset");
            Assert.That(director.Data.VariantId, Is.EqualTo("Stage04_Overlay_Intrusion"));
            Assert.That(director.Data.VariantId, Is.EqualTo(stage.ContaminationVariantId));
            foreach (var overlayName in new[] { director.Data.EchoOverlayName, director.Data.IntrusionOverlayName })
            {
                var overlay = scene.GetRootGameObjects().Single(root => root.name == overlayName);
                Assert.That(overlay.activeSelf, Is.False);
                var colliders = overlay.GetComponentsInChildren<Collider2D>(true);
                Assert.That(colliders, Is.Not.Empty);
                Assert.That(colliders.All(collider => collider.isTrigger), Is.True, $"{overlayName} must not close the base route.");
            }
        }

        [Test]
        public void Test_Chase_Stage04BacktracksThroughBrightDormitoryIdentityProps()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var prop in new[] { "BlanketFort", "SnackBags", "BoardGame", "DormShoes", "LaundryBasket", "SharedPoster" })
            {
                var visual = Find(scene, prop).GetComponent<SpriteRenderer>();
                Assert.That(visual, Is.Not.Null, $"{prop} must remain identifiable during the chase.");
                Color.RGBToHSV(visual.color, out _, out _, out var value);
                Assert.That(value, Is.GreaterThanOrEqualTo(0.7f), $"{prop} must preserve Stage04's bright tone.");
            }

            var chaseStart = Find(scene, "Stage04BaseRoot").GetComponentsInChildren<StageMarker>(true)
                .Single(marker => marker.Kind == StageMarkerKind.ChaseStart);
            var escape = Find(scene, "Stage04BaseRoot").GetComponentsInChildren<StageMarker>(true)
                .Single(marker => marker.Kind == StageMarkerKind.Escape);
            Assert.That(escape.transform.position.x, Is.LessThan(chaseStart.transform.position.x),
                "Stage04 chase must backtrack through the already learned bright route.");
        }

        [Test]
        public void Test_Stage04_SceneAndReferencesAreRegisteredWithoutMissingAssets()
        {
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path == ScenePath && entry.enabled), Is.True);
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path.Contains("Stage04_Overlay_")), Is.False);
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var component in scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Component>(true)))
                Assert.That(component, Is.Not.Null, "Stage04 scene contains a missing script reference.");
        }

        private static void AssertComposition(Stage03EncounterController controller, int melee, int dash)
        {
            Assert.That(controller.MeleeSpawnPoints.Count, Is.EqualTo(melee), controller.Data.EncounterId);
            Assert.That(controller.DashSpawnPoints.Count, Is.EqualTo(dash), controller.Data.EncounterId);
            Assert.That(controller.Data.SpawnCount, Is.EqualTo(melee + dash), controller.Data.EncounterId);
        }

        private static GameObject Find(Scene scene, string name)
        {
            var found = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<Transform>(true)).FirstOrDefault(transform => transform.name == name);
            Assert.That(found, Is.Not.Null, $"Missing Stage04 object: {name}");
            return found.gameObject;
        }
    }
}
