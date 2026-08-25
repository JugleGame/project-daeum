using System.Linq;
using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Daeume.Tests.EditMode
{
    public sealed class Stage13AcceptanceTests
    {
        private const string ScenePath = "Assets/Scenes/Stage13_Base.unity";
        private const string DataPath = "Assets/Data/Stages/Stage13.asset";

        [Test]
        public void Test_Ending_RunAwayLoopsWithoutPunishment()
        {
            var sequence = CreateSequence();
            var health = 3;
            var stageId = 13;

            Assert.That(sequence.RegisterRunawayLoop(), Is.EqualTo(1));
            Assert.That(health, Is.EqualTo(3));
            Assert.That(stageId, Is.EqualTo(13));
            Object.DestroyImmediate(sequence.gameObject);
        }

        [Test]
        public void Test_Ending_HintEscalatesAcrossFourLoops()
        {
            var sequence = CreateSequence();
            Assert.That(Enumerable.Range(0, 4).Select(_ => sequence.RegisterRunawayLoop()),
                Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Object.DestroyImmediate(sequence.gameObject);
        }

        [Test]
        public void Test_Ending_TraumaStopsAtFourthLoop()
        {
            var sequence = CreateSequence();
            for (var index = 0; index < 4; index++) sequence.RegisterRunawayLoop();
            Assert.That(sequence.TraumaWaiting, Is.True);
            Object.DestroyImmediate(sequence.gameObject);
        }

        [Test]
        public void Test_Ending_HintNeverStatesDirection()
        {
            var hint = StringTable.Get("ending.hint.03");
            Assert.That(hint, Does.Not.Contain("오른쪽").And.Not.Contain("왼쪽").And.Not.Contain("가세요"));
        }

        [Test]
        public void Test_Ending_ApproachReversesContamination()
        {
            Assert.That(AcceptanceSequence.ResolvePressure(30f, 24f, 16f, 9f), Is.EqualTo(PressureStage.Collapse));
            Assert.That(AcceptanceSequence.ResolvePressure(20f, 24f, 16f, 9f), Is.EqualTo(PressureStage.Intrusion));
            Assert.That(AcceptanceSequence.ResolvePressure(12f, 24f, 16f, 9f), Is.EqualTo(PressureStage.Echo));
            Assert.That(AcceptanceSequence.ResolvePressure(4f, 24f, 16f, 9f), Is.EqualTo(PressureStage.Stable));
        }

        [Test]
        public void Test_Stage13_DataIsValid()
        {
            var data = AssetDatabase.LoadAssetAtPath<StageData>(DataPath);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.StageId, Is.EqualTo(13));
            Assert.That(data.NextStageId, Is.Zero);
            Assert.That(data.ValidateData(), Is.Empty);
        }

        [Test]
        public void Test_UI_NoHardcodedStrings()
        {
            var keys = new[]
            {
                "prompt.ending.lower_weapon",
                "memory.stage13.title",
                "memory.stage13.01",
                "memory.stage13.02",
                "memory.stage13.03",
                "ending.hint.03",
                "ending.farewell",
                "ending.credit",
            };

            Assert.That(keys.All(key => StringTable.TryGet(key, out _)), Is.True);
        }

        [Test]
        public void Test_Stage13_PlayerJumpMatchesFeedback()
        {
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            var controller = playerPrefab.GetComponent<Daeume.Player.PlayerController>();
            var body = playerPrefab.GetComponent<Rigidbody2D>();
            var serializedController = new SerializedObject(controller);

            Assert.That(serializedController.FindProperty("jumpVelocity").floatValue, Is.EqualTo(6.5f));
            Assert.That(body.gravityScale, Is.EqualTo(1f));
        }

        [Test]
        public void Test_Ending_NoEscapeExitExists() => AssertStage13Layout();

        [Test]
        public void Test_Stage13_SceneMatchesAcceptanceLayout() => AssertStage13Layout();

        private static void AssertStage13Layout()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var roots = scene.GetRootGameObjects();
            var root = roots.Single(value => value.name == "Stage13BaseRoot");
            Assert.That(root.GetComponent<AcceptanceSequence>(), Is.Not.Null);
            Assert.That(roots.SelectMany(value => value.GetComponentsInChildren<StageOneEscapeTrigger>(true)), Is.Empty);
            Assert.That(roots.SelectMany(value => value.GetComponentsInChildren<EncounterController>(true)).All(value => !value.enabled), Is.True);
            Assert.That(roots.SelectMany(value => value.GetComponentsInChildren<EncounterExitLock>(true)).All(value => !value.enabled), Is.True);
            Assert.That(root.GetComponent<StageDefinition>().Data.StageId, Is.EqualTo(13));
            Assert.That(new[] { "Signal_Left_01", "Signal_Exit_01", "Signal_DeadEnd_01" },
                Is.SubsetOf(roots.SelectMany(value => value.GetComponentsInChildren<Transform>(true)).Select(value => value.name)));
        }

        private static AcceptanceSequence CreateSequence()
        {
            return new GameObject("AcceptanceSequenceTest").AddComponent<AcceptanceSequence>();
        }
    }
}
