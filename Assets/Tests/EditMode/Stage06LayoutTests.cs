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
    public sealed class Stage06LayoutTests
    {
        private const string ScenePath = "Assets/Scenes/Stage06_Base.unity";

        [Test]
        public void Test_Stage06_RequiredDataMarkersAndBrightStreetLayout()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Find(scene, "Stage06BaseRoot");
            var definition = root.GetComponent<StageDefinition>();
            var bounds = root.GetComponent<StageCameraBounds>();
            var markers = root.GetComponentsInChildren<StageMarker>(true);

            Assert.That(definition.Data.StageId, Is.EqualTo(6));
            Assert.That(definition.Data.HospitalImageryDirectness, Is.EqualTo(1));
            Assert.That(bounds, Is.Not.Null);
            Assert.That(markers.Select(marker => marker.MarkerId), Has.None.Empty);
            Assert.That(markers.Select(marker => marker.MarkerId).Distinct().Count(), Is.EqualTo(markers.Length));
            Assert.That(markers.Select(marker => marker.MarkerId), Is.All.StartsWith("stage06."));
            foreach (var zone in new[] {
                "Zone_A_StreetEntrance", "Zone_B_ShopAlley", "Zone_C_MainStreet",
                "Zone_D_MemoryShopCorner", "Zone_E_Chase"
            })
            {
                Assert.That(Find(scene, zone), Is.Not.Null);
            }

            var requiredKinds = new[] {
                StageMarkerKind.Start, StageMarkerKind.FallRecovery, StageMarkerKind.EncounterTrigger,
                StageMarkerKind.EncounterExit, StageMarkerKind.MemoryAnchor, StageMarkerKind.ChaseStart,
                StageMarkerKind.Escape
            };
            var kinds = markers.Select(marker => marker.Kind).ToArray();
            foreach (var required in requiredKinds) Assert.That(kinds, Does.Contain(required));
            Assert.That(kinds.Count(kind => kind == StageMarkerKind.EncounterTrigger), Is.EqualTo(3));
            Assert.That(markers.All(marker =>
                marker.transform.position.x >= bounds.Minimum.x && marker.transform.position.x <= bounds.Maximum.x &&
                marker.transform.position.y >= bounds.Minimum.y && marker.transform.position.y <= bounds.Maximum.y), Is.True,
                "All Stage06 progression markers must remain inside camera bounds.");
        }

        [Test]
        public void Test_Stage06_EncountersIncludeRangedAndCountAllThreeArchetypes()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controllers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Stage03EncounterController>(true))
                .OrderBy(controller => controller.Data.EncounterId)
                .ToArray();

            Assert.That(controllers, Has.Length.EqualTo(3));
            AssertComposition(controllers[0], 0, 0, 1);
            AssertComposition(controllers[1], 1, 1, 0);
            AssertComposition(controllers[2], 2, 1, 1);
            Assert.That(controllers.All(controller => controller.Data.WaveCount == 1), Is.True);
            Assert.That(controllers.All(controller => controller.Data.TerrainHazardIds.Count == 0), Is.True);
            Assert.That(controllers.Where(controller => controller.RangedSpawnPoints.Count > 0)
                .All(controller => controller.GetType().GetField("rangedData",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                    ?.GetValue(controller) != null), Is.True);
        }

        [Test]
        public void Test_Stage06_StreetCoverAndOverlaysRemainNonHazardAndNonBlocking()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var propsRoot = Find(scene, "Stage06_StreetIdentityProps");
            foreach (var name in new[] {
                "Parasol_A_NonHazardCover", "Parasol_B_NonHazardCover",
                "ShopDisplay_A_NonHazardObstacle", "ShopDisplay_B_NonHazardObstacle",
                "MarketStall_NonHazardObstacle", "StreetBench_NonHazardObstacle"
            })
            {
                var prop = Find(scene, name);
                Assert.That(prop.transform.IsChildOf(propsRoot.transform), Is.True);
                Assert.That(prop.GetComponent<SpriteRenderer>(), Is.Not.Null);
                Assert.That(prop.GetComponent<BoxCollider2D>()?.isTrigger, Is.False);
            }

            var director = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ContaminationDirector>(true)).Single();
            Assert.That(director.Data.VariantId, Is.EqualTo("Stage06_Overlay_Intrusion"));
            foreach (var overlayName in new[] { director.Data.EchoOverlayName, director.Data.IntrusionOverlayName })
            {
                var overlay = scene.GetRootGameObjects().Single(root => root.name == overlayName);
                Assert.That(overlay.activeSelf, Is.False);
                Assert.That(overlay.GetComponentsInChildren<Collider2D>(true), Is.Not.Empty);
                Assert.That(overlay.GetComponentsInChildren<Collider2D>(true).All(collider => collider.isTrigger), Is.True);
            }

            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path == ScenePath && entry.enabled), Is.True);
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path.Contains("Stage06_Overlay_")), Is.False);
            foreach (var component in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<Component>(true)))
            {
                Assert.That(component, Is.Not.Null, "Stage06 scene contains a missing script reference.");
            }
        }

        private static void AssertComposition(Stage03EncounterController controller, int melee, int dash, int ranged)
        {
            Assert.That(controller.MeleeSpawnPoints.Count, Is.EqualTo(melee), controller.Data.EncounterId);
            Assert.That(controller.DashSpawnPoints.Count, Is.EqualTo(dash), controller.Data.EncounterId);
            Assert.That(controller.RangedSpawnPoints.Count, Is.EqualTo(ranged), controller.Data.EncounterId);
            Assert.That(controller.Data.SpawnCount, Is.EqualTo(melee + dash + ranged), controller.Data.EncounterId);
        }

        private static GameObject Find(Scene scene, string name)
        {
            var found = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == name);
            Assert.That(found, Is.Not.Null, "Missing Stage06 object: " + name);
            return found.gameObject;
        }
    }
}