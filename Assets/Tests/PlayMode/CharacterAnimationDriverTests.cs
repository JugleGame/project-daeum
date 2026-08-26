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

            var surface = new GameObject("AnimationGrabbable").AddComponent<GrabbableSurface>();
            Assert.That(controller.TryBeginGrab(surface), Is.True);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Grab));

            combat.Attack();
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Attack));
            driver.Tick(0.6f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Attack));
            driver.Tick(0.08f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Grab));

            health.ApplyDamageAt(new DamageRequest(1), 1f);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Damaged));

            health.ApplyDamageAt(new DamageRequest(health.MaxHealth), 2f);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(PlayerAnimationState.Dead));

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(surface.gameObject);
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
            var visual = new GameObject("Visual");
            visual.transform.SetParent(traumaObject.transform);
            var renderer = visual.AddComponent<SpriteRenderer>();
            var actor = traumaObject.AddComponent<TraumaChaseActor>();
            var driver = traumaObject.AddComponent<TraumaAnimationDriver>();
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(TraumaAnimationState.Idle));

            traumaObject.transform.position = Vector3.right * 3f;
            actor.ApplyDirective(new ChaseDirectiveIssued(
                "animation-test", Vector2.zero, Vector2.right * 3f,
                3f, 2f, 7f, 4f, 5f), 0.1f, 1f);
            driver.Tick();
            Assert.That(driver.CurrentState, Is.EqualTo(TraumaAnimationState.Chase));
            Assert.That(actor.LastHorizontalMovement, Is.LessThan(0f));
            Assert.That(renderer.flipX, Is.False, "실제 왼쪽 이동과 왼쪽 authored-facing이 같으므로 반전하지 않는다.");

            actor.ApplyDirective(new ChaseDirectiveIssued(
                "animation-test", Vector2.right * 6f, Vector2.right * 3f,
                3f, 2f, 7f, 4f, 5f), 0.1f, 1f);
            driver.Tick();
            Assert.That(actor.LastHorizontalMovement, Is.GreaterThan(0f));
            Assert.That(renderer.flipX, Is.True, "실제 오른쪽 이동은 왼쪽 authored-facing을 반전해야 한다.");

            Object.DestroyImmediate(traumaObject);
        }

        [Test]
        public void Test_Animation_TraumaMapsGrabToAttack()
        {
            var manager = new GameObject("AnimationManager").AddComponent<GameManager>();
            var traumaObject = new GameObject("AnimationTrauma");
            var visual = new GameObject("Visual");
            visual.transform.SetParent(traumaObject.transform);
            var renderer = visual.AddComponent<SpriteRenderer>();
            traumaObject.AddComponent<TraumaChaseActor>();
            var driver = traumaObject.AddComponent<TraumaAnimationDriver>();
            var player = new GameObject("AnimationPlayer");
            player.AddComponent<PlayerHealth>();
            var contact = player.AddComponent<TraumaContactHandler>();

            Assert.That(contact.BeginGrab(), Is.True);
            driver.Tick(0f);
            Assert.That(driver.CurrentState, Is.EqualTo(TraumaAnimationState.Attack));
            Assert.That(renderer.flipX, Is.False, "수정된 Attack 원본은 머리와 팔이 모두 왼쪽을 향하므로 반전하지 않는다.");

            driver.Tick(0.41f);
            Assert.That(driver.CurrentState, Is.EqualTo(TraumaAnimationState.Idle));
            Assert.That(renderer.flipX, Is.False, "Attack 종료 후에도 왼쪽 authored-facing을 유지해야 한다.");

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(traumaObject);
            Object.DestroyImmediate(manager.gameObject);
        }
    }
}
