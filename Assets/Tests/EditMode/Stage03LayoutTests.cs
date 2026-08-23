using System.Linq;
using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Enemy;
using Daeume.Encounter;
using Daeume.Player;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    /// <summary>Issue #13: Stage 03 동아리실 블록아웃과 조우 구성을 검증한다.</summary>
    public sealed class Stage03LayoutTests
    {
        private const string ScenePath = "Assets/Scenes/Stage03_Base.unity";

        [Test]
        public void Test_Stage03_ContainsDataCameraBoundsAndUniqueRequiredMarkers()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var root = Find(scene, "Stage03BaseRoot");
            var definition = root.GetComponent<StageDefinition>();
            var bounds = root.GetComponent<StageCameraBounds>();
            var markers = root.GetComponentsInChildren<StageMarker>(true);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.Data, Is.Not.Null);
            Assert.That(definition.Data.StageId, Is.EqualTo(3));
            Assert.That(bounds, Is.Not.Null);
            Assert.That(bounds.Minimum.x, Is.LessThan(bounds.Maximum.x));
            Assert.That(markers.Select(marker => marker.MarkerId), Has.None.Empty);
            Assert.That(markers.Select(marker => marker.MarkerId).Distinct().Count(), Is.EqualTo(markers.Length));
            Assert.That(markers.Select(marker => marker.MarkerId),
                Is.All.Matches<string>(id => id.StartsWith("stage03.")));

            var kinds = markers.Select(marker => marker.Kind).ToArray();
            foreach (var required in new[]
                     {
                         StageMarkerKind.Start, StageMarkerKind.FallRecovery, StageMarkerKind.EncounterTrigger,
                         StageMarkerKind.EncounterExit, StageMarkerKind.MemoryAnchor, StageMarkerKind.ChaseStart,
                         StageMarkerKind.Escape
                     })
            {
                Assert.That(kinds, Does.Contain(required), $"Stage03 is missing a {required} marker.");
            }

            Assert.That(kinds.Count(kind => kind == StageMarkerKind.EncounterTrigger), Is.EqualTo(3));
            Assert.That(kinds.Count(kind => kind == StageMarkerKind.RemnantSpawn), Is.EqualTo(6));
        }

        [Test]
        public void Test_Stage03_DeclaresDashTutorialMixedRoomAndTwoWaveRehearsal()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var controllers = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Stage03EncounterController>(true))
                .OrderBy(controller => controller.Data.EncounterId)
                .ToArray();

            Assert.That(controllers.Length, Is.EqualTo(3));
            AssertComposition(controllers[0], 0, 1, 1);
            AssertComposition(controllers[1], 1, 1, 1);
            AssertComposition(controllers[2], 1, 2, 2);

            foreach (var controller in controllers)
            {
                Assert.That(controller.Data.ClearCondition, Is.EqualTo(EncounterClearCondition.DefeatAll));
                Assert.That(controller.Data.LockExit, Is.True);
                Assert.That(controller.ExitLock, Is.Not.Null);
                Assert.That(controller.Data.SpawnMarkerIds.Count,
                    Is.EqualTo(controller.MeleeSpawnPoints.Count + controller.DashSpawnPoints.Count));
            }
        }

        [Test]
        public void Test_Stage03_BlockoutContainsFiveZonesAndReusesLearnedTraversalVerbs()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (var zone in new[]
                     {
                         "Zone_A_ClubRoomEntrance", "Zone_B_BackRoom", "Zone_C_RehearsalRoom",
                         "Zone_D_QuietCorner", "Zone_E_Chase"
                     })
            {
                Assert.That(Find(scene, zone), Is.Not.Null);
            }

            var grabZone = Find(scene, "GrabWall_Zone");
            Assert.That(grabZone.GetComponent<BoxCollider2D>().isTrigger, Is.True);
            Assert.That(grabZone.GetComponent<GrabbableSurface>(), Is.Not.Null);

            var oneWay = Find(scene, "OneWayPlatform");
            Assert.That(oneWay.GetComponent<BoxCollider2D>().usedByEffector, Is.True);
            Assert.That(oneWay.GetComponent<PlatformEffector2D>().useOneWay, Is.True);

            var shelf = Find(scene, "ShakingShelf_ZoneB");
            var hazard = shelf.GetComponent<ShakingShelfHazard>();
            Assert.That(hazard, Is.Not.Null);
            Assert.That(hazard.HazardId, Is.EqualTo("stage03.hazard.shaking-shelf"));
            Assert.That(hazard.WarningSeconds, Is.EqualTo(0.3f).Within(0.001f));
            Assert.That(hazard.KnockbackDistance, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(hazard.Damage, Is.Zero);
            Assert.That(shelf.GetComponentInChildren<SpriteRenderer>(true).color,
                Is.EqualTo(new Color(0.94f, 0.67f, 0.76f, 1f)).Using(ColorComparer.Instance));
        }

        [Test]
        public void Test_Stage03_UsesItsOwnContaminationVariantAndChase()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var director = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ContaminationDirector>(true)).Single();
            var stage = AssetDatabase.LoadAssetAtPath<StageData>("Assets/Data/Stages/Stage03.asset");

            Assert.That(director.Data.VariantId, Is.EqualTo("Stage03_Overlay_Intrusion"));
            Assert.That(director.Data.EchoOverlayName, Is.EqualTo("Stage03_Overlay_Echo"));
            Assert.That(director.Data.IntrusionOverlayName, Is.EqualTo("Stage03_Overlay_Intrusion"));
            Assert.That(director.Data.TargetChaseSeconds, Is.GreaterThan(36f));
            Assert.That(director.Data.ValidateData(out var error), Is.True, error);
            Assert.That(director.Data.VariantId, Is.EqualTo(stage.ContaminationVariantId));
        }

        [Test]
        public void Test_Stage03_DashDataIsAuthoredAndWiredWithoutPrefabChanges()
        {
            var data = AssetDatabase.LoadAssetAtPath<DashRemnantData>(
                "Assets/Data/Enemies/Stage03_DashRemnant.asset");
            Assert.That(data, Is.Not.Null);
            Assert.That(data.EnemyId, Is.EqualTo("remnant-stage03-dash"));
            Assert.That(data.StageNumber, Is.EqualTo(3));
            Assert.That(data.ValidateData(), Is.Empty);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy/DashRemnant.prefab");
            Assert.That(prefab.GetComponent<DashRemnant>(), Is.Not.Null);
        }

        [Test]
        public void Test_Stage03_SceneIsRegisteredWithoutOverlayScenes()
        {
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path == ScenePath && entry.enabled), Is.True);
            Assert.That(EditorBuildSettings.scenes.Any(entry => entry.path.Contains("Stage03_Overlay_")), Is.False);
        }

        [Test]
        public void Test_Contamination_Stage03OverlaysLiveInsideTheBaseSceneAndStartDisabled()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var variant = AssetDatabase.LoadAssetAtPath<ContaminationVariantData>(
                "Assets/Data/Contamination/Stage03_ContaminationVariant.asset");

            foreach (var overlayName in new[] { variant.EchoOverlayName, variant.IntrusionOverlayName })
            {
                var root = scene.GetRootGameObjects().FirstOrDefault(go => go.name == overlayName);
                Assert.That(root, Is.Not.Null, $"'{overlayName}' 루트가 Stage03_Base에 없다.");
                Assert.That(root.activeSelf, Is.False);
                Assert.That(root.GetComponentsInChildren<Collider2D>(true), Is.Not.Empty);
            }
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
            var found = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                .FirstOrDefault(transform => transform.name == name);
            Assert.That(found, Is.Not.Null, $"Missing Stage03 object: {name}");
            return found.gameObject;
        }

        private sealed class ColorComparer : System.Collections.Generic.IEqualityComparer<Color>
        {
            public static readonly ColorComparer Instance = new();
            public bool Equals(Color x, Color y) => Vector4.Distance(x, y) < 0.001f;
            public int GetHashCode(Color obj) => obj.GetHashCode();
        }
    }
}
