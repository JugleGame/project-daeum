using Daeume.Core;
using NUnit.Framework;

namespace Daeume.Tests.EditMode
{
    public sealed class StageThirteenEndingStateTests
    {
        [Test]
        public void Test_Ending_RunAwayLoopsWithoutPunishment()
        {
            var state = new StageThirteenEndingState();
            Assert.That(state.RegisterRunawayLoop(), Is.EqualTo(1));
            Assert.That(state.LoopCount, Is.EqualTo(1));
            Assert.That(state.EndingCompleted, Is.False);
        }

        [Test]
        public void Test_Ending_HintEscalatesAcrossFourLoops()
        {
            var state = new StageThirteenEndingState();
            Assert.That(state.RegisterRunawayLoop(), Is.EqualTo(1));
            Assert.That(state.RegisterRunawayLoop(), Is.EqualTo(2));
            Assert.That(state.RegisterRunawayLoop(), Is.EqualTo(3));
            Assert.That(state.RegisterRunawayLoop(), Is.EqualTo(4));
        }

        [Test]
        public void Test_Ending_TraumaStopsAtFourthLoop()
        {
            var state = new StageThirteenEndingState();
            for (var index = 0; index < 4; index++) state.RegisterRunawayLoop();
            Assert.That(state.TraumaWaiting, Is.True);
        }

        [Test]
        public void Test_Ending_AttackCannotResolveTrauma()
        {
            Assert.That(new StageThirteenEndingState().CombatAllowed, Is.False);
        }

        [Test]
        public void Test_Ending_TraumaContactDoesNotFailStageThirteen()
        {
            Assert.That(new StageThirteenEndingState().TraumaContactFailsStage, Is.False);
        }

        [Test]
        public void Test_Ending_ApproachReversesContamination()
        {
            var state = new StageThirteenEndingState();
            Assert.That(state.ResolvePressureReversal(10f, 10f), Is.EqualTo(0));
            Assert.That(state.ResolvePressureReversal(7.5f, 10f), Is.EqualTo(1));
            Assert.That(state.ResolvePressureReversal(5f, 10f), Is.EqualTo(2));
            Assert.That(state.ResolvePressureReversal(2.5f, 10f), Is.EqualTo(3));
            Assert.That(state.ResolvePressureReversal(0f, 10f), Is.EqualTo(4));
        }

        [Test]
        public void Test_Ending_PlayerLowersWeaponAndWalks()
        {
            var state = new StageThirteenEndingState();
            Assert.That(state.TryLowerWeapon(3f, 2f), Is.False);
            Assert.That(state.TryLowerWeapon(2f, 2f), Is.True);
            Assert.That(state.WeaponLowered, Is.True);
        }

        [Test]
        public void Test_Ending_CompletesAfterFarewell()
        {
            var state = new StageThirteenEndingState();
            state.TryLowerWeapon(0f, 1f);
            Assert.That(state.CompleteAfterFarewell(true, true), Is.True);
            Assert.That(state.EndingCompleted, Is.True);
        }
    }
}
