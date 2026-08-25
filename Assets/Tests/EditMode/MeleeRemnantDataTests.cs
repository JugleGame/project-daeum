using System;
using System.Linq;
using Daeume.Contamination;
using Daeume.Encounter;
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
            // 공격 예고는 스프라이트가 아니라 "0.9초 동안 제자리에 멈춰 선다"로 전달한다.
            // 발밑으로 삐져나오던 빨간 막대는 실루엣과 겹쳐 읽히지 않아 걷어냈다.
            Assert.That(prefab.transform.Find("AttackTelegraph"), Is.Null);

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var marker = Find(scene, "RemnantSpawnMarker_01").GetComponent<StageMarker>();
                var controller = Find(scene, "EncounterTriggerMarker").GetComponent<EncounterController>();
                Assert.That(controller, Is.Not.Null);
                Assert.That(controller.Data.SpawnMarkerIds, Does.Contain(marker.MarkerId));
                var serialized = new SerializedObject(controller);
                Assert.That(serialized.FindProperty("enemyPrefab").objectReferenceValue, Is.EqualTo(prefabRemnant));
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
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
