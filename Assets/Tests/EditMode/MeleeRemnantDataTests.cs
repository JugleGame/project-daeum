using System;
using System.Linq;
using Daeume.Contamination;
using Daeume.Enemy;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    public sealed class MeleeRemnantDataTests
    {
        private const string DataPath = "Assets/Data/Enemies/Stage01_MeleeRemnant.asset";
        private const string PrefabPath = "Assets/Prefabs/Enemy/Stage01_MeleeRemnant.prefab";
        private const string ScenePath = "Assets/Scenes/Stage01_Base.unity";

        [Test]
        public void Test_Remnant_DataDeclaresPressureWithoutRemovingTelegraph()
        {
            var data = AssetDatabase.LoadAssetAtPath<MeleeRemnantData>(DataPath);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.ValidateData(), Is.Empty);
            Assert.That(data.PressureProfiles.Select(profile => profile.Stage),
                Is.EquivalentTo(new[] { PressureStage.Stable, PressureStage.Echo, PressureStage.Intrusion }));
            Assert.That(data.GetProfile(PressureStage.Echo).WatchesTrauma, Is.True);
            Assert.That(data.GetProfile(PressureStage.Intrusion).WatchesTrauma, Is.True);
            Assert.That(data.AttackTelegraphSeconds * data.GetProfile(PressureStage.Intrusion).TelegraphMultiplier,
                Is.GreaterThanOrEqualTo(.05f));
            Assert.That(Enum.GetValues(typeof(RemnantState)).Length, Is.EqualTo(6));
        }

        [Test]
        public void Test_Remnant_PrefabIsLinkedToStage01SpawnMarker()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.That(prefab, Is.Not.Null);
            var prefabRemnant = prefab.GetComponent<MeleeRemnant>();
            Assert.That(prefabRemnant, Is.Not.Null);
            Assert.That(prefabRemnant.Data, Is.Not.Null);
            Assert.That(prefabRemnant.GetComponent<Collider2D>(), Is.Not.Null);
            var telegraph = prefab.transform.Find("AttackTelegraph").GetComponent<SpriteRenderer>();
            Assert.That(telegraph, Is.Not.Null);
            Assert.That(telegraph.enabled, Is.False);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var marker = Find(scene, "RemnantSpawnMarker_01").GetComponent<StageMarker>();
            var instance = Find(scene, "Stage01_MeleeRemnant_01").GetComponent<MeleeRemnant>();
            Assert.That(instance, Is.Not.Null);
            Assert.That(instance.SpawnMarkerId, Is.EqualTo(marker.MarkerId));
            Assert.That(Vector2.Distance(instance.transform.position, marker.transform.position), Is.LessThan(.01f));
            Assert.That(PrefabUtility.IsPartOfPrefabInstance(instance), Is.True);
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
