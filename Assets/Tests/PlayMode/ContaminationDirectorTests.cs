using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using NUnit.Framework;
using UnityEngine;

namespace Daeume.Tests.PlayMode
{
    // B4 focused validation for Director-owned chase decisions.
    public sealed class ContaminationDirectorTests
    {
        [Test]
        public void Test_Chase_DirectorOwnsChaseLength()
        {
            var context = CreateContext(1f, 10f, 2f, 5f);
            Assert.That(context.Director.BeginChase(), Is.True);
            context.Director.Tick(0.75f);
            Assert.That(context.Director.ChaseActive, Is.True);
            context.Director.Tick(0.25f);
            Assert.That(context.Director.ChaseActive, Is.False);
            Assert.That(context.Director.ElapsedChaseSeconds, Is.EqualTo(1f));
            context.Dispose();
        }

        [Test]
        public void Test_Chase_DirectorClosesToContact()
        {
            // 수정: 예전에는 최소 거리를 두고 그 이상은 다가오지 않는 "공정성 장치"가 있었는데,
            // 그러면 추격자가 절대 플레이어를 붙잡지 못했다. 이제는 막다른 길이 아닌 한
            // 항상 실제 접촉 거리(ContactDistance, 0.9)까지 다가온다. 정확히 0으로 두면 두 콜라이더
            // 중심이 완전히 겹쳐 플레이어가 어느 방향으로도 못 움직이는 벽처럼 느껴지는 버그가 있었다.
            var context = CreateContext(10f, 10f, 2f, 5f);
            context.Player.position = Vector3.zero;
            context.Pursuer.position = new Vector3(-20f, 0f);
            context.Director.BeginChase();
            context.Director.Tick(2f);
            Assert.That(Mathf.Abs(context.Player.position.x - context.Pursuer.position.x), Is.EqualTo(0.9f).Within(0.001f));
            context.Dispose();
        }

        [Test]
        public void Test_Chase_DeadEndStillHoldsMaxDistance()
        {
            var context = CreateContext(10f, 10f, 2f, 5f);
            context.Player.position = Vector3.zero;
            context.Pursuer.position = new Vector3(-20f, 0f);
            context.Director.BeginChase();
            context.Director.SetDeadEndBlocked(true);
            context.Director.Tick(2f);
            Assert.That(Mathf.Abs(context.Player.position.x - context.Pursuer.position.x), Is.EqualTo(5f).Within(0.001f));
            context.Dispose();
        }

        [Test]
        public void Test_Chase_DirectorNeverTeleportsOutsideDeclaredPoints()
        {
            var context = CreateContext(10f, 6f, 2f, 7f);
            context.Pursuer.position = new Vector3(-20f, 0f);
            context.Director.BeginChase();
            var before = context.Pursuer.position;
            context.Director.Tick(0.1f);
            Assert.That(Vector3.Distance(before, context.Pursuer.position), Is.LessThanOrEqualTo(0.6001f));
            Assert.That(context.Director.TeleportCount, Is.Zero);
            Assert.That(context.Data.DeclaredTeleportMarkerIds, Is.Empty);
            context.Dispose();
        }

        [Test]
        public void Test_Contamination_RetryUsesSameVariant()
        {
            var context = CreateContext(10f, 6f, 2f, 7f);
            context.Director.BeginChase();
            var variant = context.Director.VariantId;
            context.Director.Tick(3f);
            context.Director.RetryChase();
            Assert.That(context.Director.VariantId, Is.EqualTo(variant));
            Assert.That(context.Director.Pressure, Is.EqualTo(PressureStage.Intrusion));
            Assert.That(context.Director.ElapsedChaseSeconds, Is.Zero);
            context.Dispose();
        }

        private static Context CreateContext(float targetSeconds, float speed, float minDistance, float maxDistance)
        {
            var data = ScriptableObject.CreateInstance<ContaminationVariantData>();
            data.Configure("variant-stage01", "Stage01_Overlay_Echo", "Stage01_Overlay_Intrusion", targetSeconds, speed, minDistance, maxDistance);
            var root = new GameObject("DirectorTestRoot");
            var player = new GameObject("Player").transform;
            player.SetParent(root.transform);
            var pursuer = new GameObject("Trauma").transform;
            pursuer.SetParent(root.transform);
            var director = root.AddComponent<ContaminationDirector>();
            director.Configure(data, player, pursuer);
            return new Context(root, data, player, pursuer, director);
        }

        private readonly struct Context
        {
            public Context(GameObject root, ContaminationVariantData data, Transform player, Transform pursuer, ContaminationDirector director)
            {
                Root = root;
                Data = data;
                Player = player;
                Pursuer = pursuer;
                Director = director;
            }

            public GameObject Root { get; }
            public ContaminationVariantData Data { get; }
            public Transform Player { get; }
            public Transform Pursuer { get; }
            public ContaminationDirector Director { get; }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Object.DestroyImmediate(Data);
            }
        }
    }
}
