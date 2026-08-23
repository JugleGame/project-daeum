using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Enemy;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.PlayMode
{
    public sealed class CharacterAnimationDriverTests
    {
        [Test]
        public void Test_Animation_PlayerMapsGameplaySignalsAndFacing()
        {
            var player = new GameObject("AnimationPlayer");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(player.transform);
            var renderer = visual.AddComponent<SpriteRenderer>();
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>();
            var controller = player.AddComponent<PlayerController>();
            var combat = player.AddComponent<PlayerCombat>();
            var health = player.AddComponent<PlayerHealth>();
            var driver = player.AddComponent<PlayerAnimationDriver>();

            controller.SetGroundedForTest(true);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Idle));

            controller.SetMoveInput(-1f);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Move));
            Assert.That(renderer.flipX, Is.True);

            Assert.That(controller.TryJump(), Is.True);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Airborne));

            combat.Attack();
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Attack));

            health.ApplyDamageAt(new DamageRequest(1), 1f);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Damaged));

            health.ApplyDamageAt(new DamageRequest(health.MaxHealth), 2f);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Dead));

            Object.DestroyImmediate(player);
        }

        [Test]
        public void Test_Animation_RemnantMapsStateAndPreservesDeathContract()
        {
            var remnantObject = new GameObject("AnimationRemnant");
            remnantObject.AddComponent<BoxCollider2D>();
            var remnant = remnantObject.AddComponent<MeleeRemnant>();
            var data = ScriptableObject.CreateInstance<MeleeRemnantData>();
            remnant.SetData(data);
            var driver = remnantObject.AddComponent<RemnantAnimationDriver>();
            var target = new GameObject("AnimationTarget");
            target.transform.position = remnant.transform.position + Vector3.right * 0.5f;
            target.AddComponent<PlayerHealth>();
            remnant.SetTarget(target.transform);

            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(RemnantState.Idle));
            remnant.Tick(0f);
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(RemnantState.Alert));
            remnant.Tick(data.AlertSeconds);
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(RemnantState.Approach));
            remnant.Tick(0f);
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(RemnantState.Attack));

            remnant.ApplyDamage(new DamageRequest(1));
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(RemnantState.Hit));
            remnant.ApplyDamage(new DamageRequest(data.MaxHealth));
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(RemnantState.Dead));
            Assert.That(remnant.CanDealDamage, Is.False);

            Object.DestroyImmediate(remnantObject);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Test_Animation_TraumaMapsDirectiveToChase()
        {
            var traumaObject = new GameObject("AnimationTrauma");
            var actor = traumaObject.AddComponent<TraumaChaseActor>();
            var driver = traumaObject.AddComponent<TraumaAnimationDriver>();
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(TraumaAnimationState.Idle));

            actor.ApplyDirective(new ChaseDirectiveIssued(
                "animation-test", Vector2.zero, Vector2.right * 3f,
                3f, 2f, 7f, 4f, 5f), 0.1f, 3f);
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(TraumaAnimationState.Chase));

            Object.DestroyImmediate(traumaObject);
        }
    }
}
