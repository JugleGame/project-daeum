using System.Linq;
using Daeume.Contamination;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Flow;
using NUnit.Framework;

namespace Daeume.Tests.EditMode
{
    public sealed class StageLoopTests
    {
        [Test]
        public void Test_StageLoop_ExploreMemoryChaseClear()
        {
            var loop = new StageLoop();
            Assert.That(loop.TryTransition(StageState.Memory), Is.True);
            Assert.That(loop.TryTransition(StageState.Chase), Is.True);
            Assert.That(loop.TryClearAtExit(), Is.True);
            Assert.That(loop.State, Is.EqualTo(StageState.Cleared));
        }

        [Test]
        public void Test_StageLoop_EncounterDoesNotReplaceExplore()
        {
            var loop = new StageLoop();
            var encounter = EncounterState.Active;
            Assert.That(encounter, Is.EqualTo(EncounterState.Active));
            Assert.That(loop.State, Is.EqualTo(StageState.Explore));
        }

        [Test]
        public void Test_StageLoop_ContaminationDoesNotReplaceStageState()
        {
            var loop = new StageLoop();
            var pressure = PressureStage.Intrusion;
            Assert.That(pressure, Is.EqualTo(PressureStage.Intrusion));
            Assert.That(loop.State, Is.EqualTo(StageState.Explore));
        }

        [Test]
        public void Test_StageLoop_ExitLockedBeforeChase()
        {
            Assert.That(new StageLoop().TryClearAtExit(), Is.False);
        }

        [Test]
        public void Test_StageLoop_FailedOnlyFromDeclaredCauses()
        {
            var loop = new StageLoop();
            Assert.That(loop.TryTransition(StageState.Failed), Is.False);
            Assert.That(loop.TryFail(StageFailureCause.HealthDepleted), Is.True);
            loop.Reset();
            Assert.That(loop.TryFail(StageFailureCause.TraumaGrabCompleted), Is.True);
        }

        [Test]
        public void Test_SceneFlow_NewGameLoadsStageOne()
        {
            var route = new SceneFlowPlan().NewGame();
            Assert.That(route.StageId, Is.EqualTo(1));
            Assert.That(route.NewGame, Is.True);
        }

        [Test]
        public void Test_SceneFlow_ContinueLoadsCheckpoint()
        {
            var route = new SceneFlowPlan().Continue(new SaveData { CurrentStageId = 1, CheckpointId = "Stage01_Chase" });
            Assert.That(route.CheckpointId, Is.EqualTo("Stage01_Chase"));
        }

        [Test]
        public void Test_SceneFlow_StageClearOrder()
        {
            var order = new SceneFlowPlan().GetStageClearOrder();
            Assert.That(order.Count, Is.EqualTo(9));
            Assert.That(order.First(), Is.EqualTo(SceneFlowStep.StageCleared));
            Assert.That(order.Last(), Is.EqualTo(SceneFlowStep.Explore));
        }

        [Test]
        public void Test_SceneFlow_RejectsDuplicateTransition()
        {
            var plan = new SceneFlowPlan();
            Assert.That(plan.TryBeginTransition(), Is.True);
            Assert.That(plan.TryBeginTransition(), Is.False);
        }
    }
}
