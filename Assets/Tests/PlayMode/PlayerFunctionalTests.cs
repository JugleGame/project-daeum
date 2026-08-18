using System.Collections;
using Daeume.Core;
using Daeume.Interaction;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class PlayerFunctionalTests
    {
        [UnityTest]
        public IEnumerator Test_Player_MoveBothDirections()
        {
            var player = CreatePlayer(out var controller);
            controller.SetGroundedForTest(true);
            controller.SetMoveInput(1f);
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.GreaterThan(0f));
            controller.SetMoveInput(-1f);
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.LessThan(0f));
            Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator Test_Player_NoDoubleJump()
        {
            var player = CreatePlayer(out var controller);
            controller.SetGroundedForTest(true);
            Assert.That(controller.TryJump(), Is.True);
            Assert.That(controller.TryJump(), Is.False);
            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Movement_SameRulesDuringChase()
        {
            var manager = new GameObject("Manager").AddComponent<GameManager>();
            var player = CreatePlayer(out var controller);
            var speedBefore = controller.GrabHoldSeconds;
            manager.SetStageState(StageState.Memory);
            manager.SetStageState(StageState.Chase);
            Assert.That(controller.GrabHoldSeconds, Is.EqualTo(speedBefore));
            Object.Destroy(player);
            Object.Destroy(manager.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Movement_ContaminationNeverReversesInput()
        {
            var player = CreatePlayer(out var controller);
            controller.SetGroundedForTest(true);
            controller.SetMoveInput(1f);
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity.x, Is.GreaterThan(0f));
            Object.Destroy(player);
        }

        [UnityTest]
        public IEnumerator Test_Movement_GrabAttachesOnlyToGrabbable()
        {
            var player = CreatePlayer(out var controller);
            controller.SetGroundedForTest(false);
            Assert.That(controller.TryBeginGrab(null), Is.False);
            var surface = new GameObject("Grabbable").AddComponent<GrabbableSurface>();
            Assert.That(controller.TryBeginGrab(surface), Is.True);
            Object.Destroy(surface.gameObject);
            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Movement_GrabAllowsOnlyDeclaredExits()
        {
            var player = CreatePlayer(out var controller);
            controller.SetGroundedForTest(false);
            var surface = new GameObject("Grabbable").AddComponent<GrabbableSurface>();
            controller.TryBeginGrab(surface);
            controller.SetMoveInput(1f);
            yield return new WaitForFixedUpdate();
            Assert.That(player.GetComponent<Rigidbody2D>().linearVelocity, Is.EqualTo(Vector2.zero));
            controller.TickGrab(0f, true);
            Assert.That(controller.IsGrabbing, Is.False);
            Object.Destroy(surface.gameObject);
            Object.Destroy(player);
        }

        [Test]
        public void Test_Movement_InputBoundToActionNames()
        {
            Assert.That(PlayerController.MoveActionName, Is.EqualTo("Move"));
            Assert.That(PlayerController.JumpActionName, Is.EqualTo("Jump"));
            Assert.That(PlayerController.GrabActionName, Is.EqualTo("Grab"));
        }

        [Test]
        public void Test_Combat_InvulnerabilityPreventsRapidHits()
        {
            var player = new GameObject("Player");
            var health = player.AddComponent<PlayerHealth>();
            Assert.That(health.ApplyDamageAt(new DamageRequest(1), 1f).Applied, Is.True);
            Assert.That(health.ApplyDamageAt(new DamageRequest(1), 1.1f).Applied, Is.False);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void Test_Combat_TraumaAttackHasNoEffect()
        {
            var trauma = new GameObject("Trauma").AddComponent<TraumaContactSource>();
            Assert.That(trauma.ApplyDamage(new DamageRequest(1)).Applied, Is.False);
            Object.DestroyImmediate(trauma.gameObject);
        }

        [Test]
        public void Test_Combat_TraumaContactDealsNoDamage()
        {
            var player = new GameObject("Player");
            var health = player.AddComponent<PlayerHealth>();
            var before = health.CurrentHealth;
            player.AddComponent<TraumaContactHandler>().BeginGrab();
            Assert.That(health.CurrentHealth, Is.EqualTo(before));
            Object.DestroyImmediate(player);
        }

        [Test]
        public void Test_Movement_GrabDoesNotBlockDamage()
        {
            var player = CreatePlayer(out var controller);
            var health = player.AddComponent<PlayerHealth>();
            var surface = new GameObject("Grabbable").AddComponent<GrabbableSurface>();
            controller.SetGroundedForTest(false);
            controller.TryBeginGrab(surface);
            Assert.That(health.ApplyDamageAt(new DamageRequest(1), 1f).Applied, Is.True);
            Object.DestroyImmediate(surface.gameObject);
            Object.DestroyImmediate(player);
        }

        [Test]
        public void Test_Combat_ZeroHealthTriggersFailure()
        {
            var manager = new GameObject("Manager").AddComponent<GameManager>();
            var player = new GameObject("Player");
            var health = player.AddComponent<PlayerHealth>();
            health.ApplyDamageAt(new DamageRequest(health.MaxHealth), 1f);
            Assert.That(manager.StageState, Is.EqualTo(StageState.Failed));
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(manager.gameObject);
        }

        [UnityTest]
        public IEnumerator Test_Combat_TraumaContactStartsGrabThenFails()
        {
            var manager = new GameObject("Manager").AddComponent<GameManager>();
            var player = new GameObject("Player");
            var handler = player.AddComponent<TraumaContactHandler>();
            Assert.That(handler.BeginGrab(), Is.True);
            Assert.That(handler.BeginGrab(), Is.False);
            yield return new WaitForSeconds(handler.TraumaGrabSeconds + 0.05f);
            Assert.That(manager.StageState, Is.EqualTo(StageState.Failed));
            Object.Destroy(player);
            Object.Destroy(manager.gameObject);
        }

        [Test]
        public void Test_Combat_GrabSequenceIsDeterministic()
        {
            var first = new GameObject("First").AddComponent<TraumaContactHandler>();
            var second = new GameObject("Second").AddComponent<TraumaContactHandler>();
            Assert.That(first.TraumaGrabSeconds, Is.EqualTo(second.TraumaGrabSeconds));
            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(second.gameObject);
        }

        [UnityTest]
        public IEnumerator Test_Combat_AttackDamagesRemnant()
        {
            var player = new GameObject("Player");
            var combat = player.AddComponent<PlayerCombat>();
            var target = new GameObject("Remnant");
            target.AddComponent<CircleCollider2D>();
            var damageable = target.AddComponent<FakeDamageable>();
            Physics2D.SyncTransforms();
            Assert.That(combat.Attack(), Is.EqualTo(1));
            Assert.That(damageable.HitCount, Is.EqualTo(1));
            Object.Destroy(player);
            Object.Destroy(target);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Combat_AggressionSetOnlyOnHit()
        {
            var player = new GameObject("Player");
            var combat = player.AddComponent<PlayerCombat>();
            Assert.That(combat.Attack(), Is.Zero);
            Assert.That(combat.PlayerAggression, Is.False);
            var target = new GameObject("Remnant");
            target.AddComponent<CircleCollider2D>();
            target.AddComponent<FakeDamageable>();
            Physics2D.SyncTransforms();
            combat.Attack();
            Assert.That(combat.PlayerAggression, Is.True);
            Object.Destroy(player);
            Object.Destroy(target);
            yield return null;
        }

        private static GameObject CreatePlayer(out PlayerController controller)
        {
            var player = new GameObject("Player");
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            controller = player.AddComponent<PlayerController>();
            return player;
        }

        private sealed class FakeDamageable : MonoBehaviour, IDamageable
        {
            public int HitCount { get; private set; }
            public DamageTargetKind TargetKind => DamageTargetKind.Remnant;

            public DamageResult ApplyDamage(DamageRequest request)
            {
                HitCount++;
                return new DamageResult(true, request.Amount);
            }
        }
    }
}
