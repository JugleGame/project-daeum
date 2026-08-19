using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.PlayMode
{
    public sealed class StageOneChaseTests
    {
        [Test]
        public void Test_Trauma_StagesOneToTwelveCannotBeKilled_StageOne()
        {
            var trauma = new GameObject("Trauma");
            var source = trauma.AddComponent<TraumaContactSource>();
            Assert.That(source.TargetKind, Is.EqualTo(DamageTargetKind.Trauma));
            Assert.That(source.ApplyDamage(new DamageRequest(999)).Applied, Is.False);
            Object.DestroyImmediate(trauma);
        }

        [Test]
        public void Test_Chase_StandingStillIsNotSafe()
        {
            var context = CreateContext(false);
            context.Pursuer.position = new Vector3(-6f, 0f);
            context.Director.BeginChase();
            context.Director.Tick(0.5f);
            Assert.That(Mathf.Abs(context.Player.position.x - context.Pursuer.position.x), Is.LessThan(6f));
            context.Dispose();
        }

        [Test]
        public void Test_Chase_DeadEndBacksOffInsteadOfFailing()
        {
            var context = CreateContext(false, true);
            context.Pursuer.position = new Vector3(-2.5f, 0f);
            context.Director.BeginChase();
            context.Director.SetDeadEndBlocked(true);
            context.Director.Tick(0.5f);
            Assert.That(Mathf.Abs(context.Player.position.x - context.Pursuer.position.x), Is.GreaterThan(2.5f));
            Assert.That(context.Manager.StageState, Is.Not.EqualTo(StageState.Failed));
            context.Dispose();
        }

        [Test]
        public void Test_Chase_SpeedAssistChangesSpeedOnly()
        {
            var normal = CreateContext(false);
            var assisted = CreateContext(true);
            Assert.That(assisted.Director.EffectiveChaseSpeed, Is.LessThan(normal.Director.EffectiveChaseSpeed));
            Assert.That(assisted.Director.EffectiveMinDistance, Is.GreaterThan(normal.Director.EffectiveMinDistance));
            Assert.That(assisted.Data.VariantId, Is.EqualTo(normal.Data.VariantId));
            Assert.That(assisted.Data.TargetChaseSeconds, Is.EqualTo(normal.Data.TargetChaseSeconds));
            Assert.That(assisted.Data.OverlayFor(PressureStage.Intrusion), Is.EqualTo(normal.Data.OverlayFor(PressureStage.Intrusion)));
            normal.Dispose();
            assisted.Dispose();
        }

        [Test]
        public void Test_Chase_SpeedAssistUsesSavedSetting()
        {
            var root = new GameObject("ChaseSpeedAssistTest");
            var assist = root.AddComponent<ChaseSpeedAssistAdapter>();
            assist.Apply(new AssistSettings { ChaseSpeedAssist = true });
            Assert.That(assist.Enabled, Is.True);
            Object.DestroyImmediate(root);
        }

        [Test]
        public void Test_Chase_MemoryToEscapeUsesRoleAStateContract()
        {
            var context = CreateContext(false, true);
            var controller = context.Root.AddComponent<StageOneChaseController>();
            controller.Configure(context.Director, null, context.Player, context.Pursuer.gameObject);
            Assert.That(controller.BeginChaseFromMemory(), Is.True);
            Assert.That(context.Manager.StageState, Is.EqualTo(StageState.Chase));
            Assert.That(controller.CompleteAtEscape(), Is.True);
            Assert.That(context.Manager.StageState, Is.EqualTo(StageState.Cleared));
            context.Dispose();
        }

        private static Context CreateContext(bool assistEnabled, bool needsManager = false)
        {
            if (needsManager && GameManager.Instance != null)
                Object.DestroyImmediate(GameManager.Instance.gameObject);
            var root = new GameObject("StageOneChaseTestRoot");
            var manager = needsManager ? root.AddComponent<GameManager>() : null;
            var data = ScriptableObject.CreateInstance<ContaminationVariantData>();
            data.Configure("Stage01_Overlay_Intrusion", "Stage01_Overlay_Echo", "Stage01_Overlay_Intrusion", 45f, 6f, 2f, 7f);
            var player = new GameObject("Player").transform;
            player.SetParent(root.transform);
            var pursuer = new GameObject("Trauma").transform;
            pursuer.SetParent(root.transform);
            var assist = root.AddComponent<ChaseSpeedAssistAdapter>();
            assist.Configure(assistEnabled, 0.5f, 0.5f);
            var director = root.AddComponent<ContaminationDirector>();
            director.Configure(data, player, pursuer);
            director.SetSpeedAssist(assist);
            return new Context(root, manager, data, player, pursuer, director);
        }

        private readonly struct Context
        {
            public Context(GameObject root, GameManager manager, ContaminationVariantData data, Transform player, Transform pursuer, ContaminationDirector director)
            { Root = root; Manager = manager; Data = data; Player = player; Pursuer = pursuer; Director = director; }
            public GameObject Root { get; }
            public GameManager Manager { get; }
            public ContaminationVariantData Data { get; }
            public Transform Player { get; }
            public Transform Pursuer { get; }
            public ContaminationDirector Director { get; }
            public void Dispose() { Object.DestroyImmediate(Root); Object.DestroyImmediate(Data); }
        }
    }
}
