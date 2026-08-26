using System.Linq;
using Daeume.ContaminationRuntime;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Daeume.Tests.EditMode
{
    public sealed class StageOneChaseLayoutTests
    {
        private const string ScenePath = "Assets/Scenes/Stage01_Base.unity";

        [Test]
        public void Test_Chase_RequiredSignalsAreNotColorOnly()
        {
            var signals = LoadSignals();
            Assert.That(signals, Is.Not.Empty);
            Assert.That(signals, Has.All.Matches<ChaseRouteSignal>(signal => signal.HasNonColorCue));
        }

        [Test]
        public void Test_Contamination_RequiredRouteSignalsExist()
        {
            var signals = LoadSignals();
            Assert.That(signals.Select(signal => signal.Shape), Does.Contain(ChaseSignalShape.LeftArrow));
            Assert.That(signals.Select(signal => signal.Shape), Does.Contain(ChaseSignalShape.Barrier));
            Assert.That(signals.Select(signal => signal.Shape), Does.Contain(ChaseSignalShape.ExitDoor));
            Assert.That(signals.Select(signal => signal.SignalId).Distinct().Count(), Is.EqualTo(signals.Length));
        }

        [Test]
        public void Test_Chase_StageOneSceneKeepsTraumaActiveUntilEscape()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var chase = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<StageOneChaseController>(true))
                .Single();

            Assert.That(chase.KeepChaseActiveUntilEscape, Is.True);
        }

        private static ChaseRouteSignal[] LoadSignals()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<ChaseRouteSignal>(true)).ToArray();
        }
    }
}
