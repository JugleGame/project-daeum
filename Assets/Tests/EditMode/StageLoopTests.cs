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
        public void Test_SceneFlow_StageSceneNameFollowsConvention()
        {
            // 진행 연결(#12 준비)의 핵심 규칙. 이 이름 규칙이 깨지면 클리어 후 다음 스테이지로 못 넘어간다.
            Assert.That(SceneFlowController.StageSceneName(1), Is.EqualTo("Stage01_Base"));
            Assert.That(SceneFlowController.StageSceneName(2), Is.EqualTo("Stage02_Base"));

            // 범위 밖 스테이지는 빈 문자열이다. 호출부는 이걸 보고 타이틀로 보낸다.
            Assert.That(SceneFlowController.StageSceneName(0), Is.Empty);
            Assert.That(SceneFlowController.StageSceneName(14), Is.Empty);

            // 아직 만들지 않은 Stage 2는 "열 수 있는 씬"이 아니다(빌드 설정에 없다).
            Assert.That(SceneFlowController.PlayableStageScene(1), Is.EqualTo("Stage01_Base"));
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
