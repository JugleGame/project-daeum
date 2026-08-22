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

            // X는 지시를 따라 움직여야 하지만, Y는 플레이어를 쫓아가면 안 된다(자기 높이 유지).
            Assert.That(actor.transform.position.y, Is.EqualTo(0f).Within(0.001f));
            Assert.That(actor.transform.position.x, Is.Not.EqualTo(10f).Within(0.001f));

            Object.DestroyImmediate(actor.gameObject);
        }
    }
}
