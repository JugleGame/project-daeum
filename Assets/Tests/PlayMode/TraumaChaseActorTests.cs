using Daeume.ContaminationRuntime;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.PlayMode
{
    public sealed class TraumaChaseActorTests
    {
        [Test]
        public void Test_Trauma_FollowsPlayerVerticallyWithoutGravity()
        {
            var actor = CreateActor(new Vector3(10f, 0f, 0f));

            actor.ApplyDirective(Directive(actor.transform.position, new Vector2(8f, 5f), 20f), 1f, 3f);

            Assert.That(actor.transform.position.x, Is.EqualTo(11f).Within(0.001f));
            Assert.That(actor.transform.position.y, Is.EqualTo(5f).Within(0.001f));
            Object.DestroyImmediate(actor.gameObject);
        }

        [Test]
        public void Test_Trauma_FollowsPlayerThroughTerrainWithoutClimbing()
        {
            var wall = CreateSolid("Wall", new Vector2(0f, 2f), new Vector2(1f, 6f));
            var actor = CreateActor(new Vector3(2f, 0f, 0f));

            actor.ApplyDirective(Directive(actor.transform.position, new Vector2(-4f, 3f), 20f), 1f, 0f);

            Assert.That(actor.transform.position, Is.EqualTo(new Vector3(-4f, 3f, 0f)));
            Object.DestroyImmediate(actor.gameObject);
            Object.DestroyImmediate(wall);
        }

        [Test]
        public void Test_Trauma_FollowsVerticallyWhenHorizontalDistanceAlreadyMatched()
        {
            var actor = CreateActor(new Vector3(2f, 0f, 0f));
            var directive = Directive(actor.transform.position, new Vector2(0f, 3f), 6f);

            actor.ApplyDirective(directive, 1f, 2f);

            Assert.That(actor.transform.position.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(actor.transform.position.y, Is.EqualTo(3f).Within(0.001f));
            Object.DestroyImmediate(actor.gameObject);
        }

        private static TraumaChaseActor CreateActor(Vector3 position)
        {
            var actor = new GameObject("TraumaChaseActorTest").AddComponent<TraumaChaseActor>();
            actor.transform.position = position;
            return actor;
        }

        private static GameObject CreateSolid(string name, Vector2 center, Vector2 size)
        {
            var solid = new GameObject(name);
            solid.transform.position = center;
            solid.AddComponent<BoxCollider2D>().size = size;
            return solid;
        }

        private static ChaseDirectiveIssued Directive(Vector2 pursuer, Vector2 player, float speed)
        {
            return new ChaseDirectiveIssued(
                "chase-test", playerPosition: player, pursuerPosition: pursuer,
                distance: Vector2.Distance(player, pursuer), minDistance: 0.5f, maxDistance: 7f,
                speed: speed, remainingSeconds: 10f);
        }
    }
}
