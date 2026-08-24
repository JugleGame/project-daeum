using Daeume.ContaminationRuntime;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.PlayMode
{
    /// <summary>Issue #9 QA 확장: 트라우마가 플레이어 점프에 같이 튀어오르던 버그 회귀 테스트.</summary>
    public sealed class TraumaChaseActorTests
    {
        [Test]
        public void Test_Trauma_ChaseIgnoresPlayerVerticalMovement()
        {
            var actor = new GameObject("TraumaChaseActorTest").AddComponent<TraumaChaseActor>();
            actor.transform.position = new Vector3(10f, 0f, 0f);

            // 플레이어가 제자리에서 점프해 y가 크게 튀는 상황을 흉내 낸다.
            var jumpedDirective = new ChaseDirectiveIssued(
                "chase-test", playerPosition: new Vector2(8f, 5f), pursuerPosition: actor.transform.position,
                distance: 2f, minDistance: 2f, maxDistance: 7f, speed: 20f, remainingSeconds: 10f);

            actor.ApplyDirective(jumpedDirective, 1f, 3f);

            // X는 지시를 따라 움직여야 한다.
            Assert.That(actor.transform.position.x, Is.Not.EqualTo(10f).Within(0.001f));

            // Y는 플레이어를 쫓아가면 안 된다. 딛을 지형이 없는 허공이므로 중력만 받아 내려가야 하고(#12),
            // 플레이어가 점프했다고 따라 올라가면 예전 버그가 되살아난 것이다.
            Assert.That(actor.transform.position.y, Is.LessThan(0f));
            Assert.That(actor.IsGrounded, Is.False);

            Object.DestroyImmediate(actor.gameObject);
        }

        /// <summary>
        /// #12: 트라우마가 중력을 받아 지형 위에 선다.
        /// 예전에는 Y를 아예 건드리지 않아 공중에 뜬 채 좌우로만 움직였다.
        /// </summary>
        [Test]
        public void Test_Trauma_FallsAndLandsOnTerrain()
        {
            var ground = CreateSolid("Ground", new Vector2(0f, -1f), new Vector2(20f, 1f));
            var actor = CreateActor(new Vector3(0f, 5f, 0f));

            // 0.05초씩 여러 번 밟아 낙하 → 착지까지 진행시킨다.
            for (var step = 0; step < 120; step++)
            {
                actor.ApplyDirective(Directive(actor.transform.position, new Vector2(0f, -0.5f)), 0.05f, 0f);
                if (actor.IsGrounded) break;
            }

            Assert.That(actor.IsGrounded, Is.True, "트라우마가 바닥에 착지해야 한다.");

            // 바닥 윗면(-0.5) 위에 반지름만큼 떠서 선다. 바닥을 뚫고 내려가면 안 된다.
            Assert.That(actor.transform.position.y, Is.GreaterThan(-0.5f));

            Object.DestroyImmediate(actor.gameObject);
            Object.DestroyImmediate(ground);
        }

        /// <summary>
        /// #12: 벽에 막히면 통과하지 않고 타고 오른다.
        /// 예전에는 막힘 검사가 없어 벽을 그대로 통과했고, 위쪽으로는 아예 따라오지 못했다.
        /// </summary>
        [Test]
        public void Test_Trauma_ClimbsBlockingWallInsteadOfPassingThrough()
        {
            var wall = CreateSolid("Wall", new Vector2(0f, 1f), new Vector2(1f, 6f));
            var actor = CreateActor(new Vector3(2f, 0f, 0f));
            var startX = actor.transform.position.x;
            var startY = actor.transform.position.y;

            // 플레이어가 벽 너머 왼쪽 위에 있다고 지시한다 → 왼쪽으로 가려다 벽에 막힌다.
            for (var step = 0; step < 20; step++)
            {
                actor.ApplyDirective(Directive(actor.transform.position, new Vector2(-4f, 3f)), 0.05f, 0f);
            }

            Assert.That(actor.transform.position.x, Is.GreaterThan(wall.transform.position.x),
                "벽을 통과하면 안 된다.");
            Assert.That(actor.IsClimbing, Is.True);
            Assert.That(actor.transform.position.y, Is.GreaterThan(startY + 0.5f),
                "막혔으면 벽을 타고 올라야 한다.");
            Assert.That(actor.transform.position.x, Is.LessThan(startX + 0.001f));

            Object.DestroyImmediate(actor.gameObject);
            Object.DestroyImmediate(wall);
        }

        /// <summary>
        /// #12: 플레이어가 위에 없으면 벽에 막혀도 오르지 않는다.
        /// 조건 없이 오르게 두었더니 레벨 경계벽에 눌린 추격자가 화면 밖까지 끝없이 올라갔다.
        /// </summary>
        [Test]
        public void Test_Trauma_DoesNotClimbBoundaryWallWhenPlayerIsNotAbove()
        {
            var ground = CreateSolid("Ground", new Vector2(0f, -1f), new Vector2(20f, 1f));
            var wall = CreateSolid("Boundary", new Vector2(3f, 2f), new Vector2(1f, 8f));
            var actor = CreateActor(new Vector3(2f, 0f, 0f));

            // 플레이어가 벽 너머 오른쪽 아래(같은 높이)에 있다 → 벽에 막히지만 오를 이유가 없다.
            for (var step = 0; step < 60; step++)
            {
                actor.ApplyDirective(Directive(actor.transform.position, new Vector2(8f, 0f)), 0.05f, 0f);
            }

            Assert.That(actor.IsClimbing, Is.False);
            Assert.That(actor.transform.position.y, Is.LessThan(1f), "경계벽을 타고 하늘로 올라가면 안 된다.");
            Assert.That(actor.IsGrounded, Is.True);

            Object.DestroyImmediate(actor.gameObject);
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(ground);
        }

        /// <summary>
        /// #12: 지형에 파묻힌 채 시작해도 빠져나올 수 있어야 한다.
        ///
        /// Stage 01은 트라우마가 경계벽 안쪽에 배치돼 있다. 막힘 검사를 넣은 뒤 거리 0 히트를
        /// 막힘으로 취급하는 바람에 추격이 시작되자마자 그 자리에 굳어 플레이어를 쫓지 못했다.
        /// </summary>
        [Test]
        public void Test_Trauma_EscapesWhenSpawnedInsideTerrain()
        {
            var wall = CreateSolid("Boundary", new Vector2(0f, 0f), new Vector2(1f, 8f));
            var actor = CreateActor(new Vector3(0f, 0f, 0f));   // 벽 한가운데에서 시작
            var startX = actor.transform.position.x;

            for (var step = 0; step < 20; step++)
            {
                actor.ApplyDirective(Directive(actor.transform.position, new Vector2(-10f, 0f)), 0.05f, 0f);
            }

            Assert.That(actor.transform.position.x, Is.LessThan(startX - 0.5f),
                "지형에 파묻힌 상태에서도 플레이어 쪽으로 빠져나와야 한다.");

            Object.DestroyImmediate(actor.gameObject);
            Object.DestroyImmediate(wall);
        }

        /// <summary>
        /// #53: 콜라이더가 경계벽에 걸쳐 있어도 지면 위에 선다.
        ///
        /// Stage 01의 트라우마는 x=32에 있고 Boundary_Right가 x 32.0~32.5를 막고 있다.
        /// 콜라이더 offset이 (0, 1)이라 중심이 벽 안에 들어가는데, 벽 안에서 시작한
        /// 아래 방향 레이캐스트는 거리 0으로 즉시 맞았다고 알려 준다. 그걸 지형으로 치면
        /// 착지한 것으로 판단해 공중에 뜬 채 좌우로만 움직였다.
        /// </summary>
        [Test]
        public void Test_Trauma_LandsOnGroundWhileOverlappingBoundaryWall()
        {
            const float groundTop = -1f;
            var ground = CreateSolid("Ground", new Vector2(28f, groundTop - 0.25f), new Vector2(16f, 0.5f));
            var boundary = CreateSolid("Boundary_Right", new Vector2(32.25f, 1.5f), new Vector2(0.5f, 6f));

            var actor = CreateActor(new Vector3(32f, 3f, 0f));
            var body = actor.GetComponent<CircleCollider2D>();
            body.radius = 1f;
            body.offset = new Vector2(0f, 1f);   // 중심이 경계벽 안에 들어간다.

            for (var step = 0; step < 200; step++)
            {
                actor.ApplyDirective(Directive(actor.transform.position, new Vector2(28f, groundTop)), 0.05f, 0f);
                if (actor.IsGrounded) break;
            }

            Assert.That(actor.IsGrounded, Is.True, "경계벽에 걸쳐 있어도 착지해야 한다.");

            // 콜라이더 하단(루트 + offset.y - radius = 루트)이 지면 윗면에 닿아야 한다.
            Assert.That(actor.transform.position.y, Is.EqualTo(groundTop).Within(0.05f),
                "트라우마가 지면 위에 서야 한다. 공중에 뜨면 회귀다.");

            Object.DestroyImmediate(actor.gameObject);
            Object.DestroyImmediate(ground);
            Object.DestroyImmediate(boundary);
        }

        private static TraumaChaseActor CreateActor(Vector3 position)
        {
            var actor = new GameObject("TraumaChaseActorTest").AddComponent<TraumaChaseActor>();
            actor.GetComponent<CircleCollider2D>().radius = 0.5f;
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

        private static ChaseDirectiveIssued Directive(Vector2 pursuer, Vector2 player)
        {
            return new ChaseDirectiveIssued(
                "chase-test", playerPosition: player, pursuerPosition: pursuer,
                distance: Vector2.Distance(player, pursuer), minDistance: 0.5f, maxDistance: 7f,
                speed: 6f, remainingSeconds: 10f);
        }
    }
}
