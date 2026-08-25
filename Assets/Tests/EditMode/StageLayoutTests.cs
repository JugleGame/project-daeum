using System.Linq;
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
