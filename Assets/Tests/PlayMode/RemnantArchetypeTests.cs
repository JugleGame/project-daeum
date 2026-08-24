using System.Collections;
using Daeume.Core;
using Daeume.Enemy;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    /// <summary>Issue #9: 돌진형/원거리형 archetype, 모방, Reactive, 소멸 흔적 named tests.</summary>
    public sealed class RemnantArchetypeTests
    {
        [UnityTest]
        public IEnumerator Test_Remnant_ThreeArchetypesBehaveAsDeclared()
        {
            // 근접형: 사거리 안에 들어와야만 공격한다.
            var melee = CreateMelee(out var meleeData);
            var meleePlayer = CreatePlayer(out var meleeHealth);
            meleePlayer.transform.position = Vector2.right * .5f;
            melee.SetTarget(meleePlayer.transform);
            AdvanceThroughAlert(melee, meleeData.AlertSeconds);
            Assert.That(melee.State, Is.EqualTo(RemnantState.Attack));
            melee.Tick(meleeData.AttackTelegraphSeconds);
            Assert.That(meleeHealth.CurrentHealth, Is.EqualTo(meleeHealth.MaxHealth - meleeData.ContactDamage));

            // 돌진형: 예고 후 한 틱 만에 근접형보다 훨씬 먼 거리를 이동한다(burst).
            var dash = CreateDash(out var dashData);
            var dashPlayer = CreatePlayer(out _);
            dashPlayer.transform.position = Vector2.right * (dashData.DashTriggerRange - 0.1f);
            dash.SetTarget(dashPlayer.transform);
            AdvanceThroughAlert(dash, dashData.AlertSeconds);
            Assert.That(dash.State, Is.EqualTo(RemnantState.Attack));
            dash.Tick(dashData.AttackTelegraphSeconds); // 예고 종료 → 돌진 시작
            var beforeBurst = dash.transform.position.x;
            dash.Tick(0.05f);
            var burstDelta = Mathf.Abs(dash.transform.position.x - beforeBurst);
            Assert.That(burstDelta, Is.GreaterThan(dashData.MoveSpeed * 0.05f), "Dash burst should outrun normal approach speed.");

            // 원거리형: 너무 가까워지면 붙지 않고 물러난다.
            var ranged = CreateRanged(out var rangedData);
            var rangedPlayer = CreatePlayer(out _);
            rangedPlayer.transform.position = Vector2.right * (rangedData.RetreatTriggerRange * .5f);
            ranged.SetTarget(rangedPlayer.transform);
            AdvanceThroughAlert(ranged, rangedData.AlertSeconds);
            var distanceBefore = Mathf.Abs(ranged.transform.position.x - rangedPlayer.transform.position.x);
            ranged.Tick(0.1f);
            var distanceAfter = Mathf.Abs(ranged.transform.position.x - rangedPlayer.transform.position.x);
            Assert.That(ranged.IsRetreating, Is.True);
            Assert.That(distanceAfter, Is.GreaterThan(distanceBefore));

            Object.DestroyImmediate(melee.gameObject);
            Object.DestroyImmediate(meleePlayer);
            Object.DestroyImmediate(meleeData);
            Object.DestroyImmediate(dash.gameObject);
            Object.DestroyImmediate(dashPlayer);
            Object.DestroyImmediate(dashData);
            Object.DestroyImmediate(ranged.gameObject);
            Object.DestroyImmediate(rangedPlayer);
            Object.DestroyImmediate(rangedData);
            yield return null;
        }

        [Test]
        public void Test_Remnant_StageNineMirrorsPlayerMotion()
        {
            var remnant = CreateMelee(out var data);
            data.SetStageIdentity(9, VisualTraitTag.None);
            data.SetBehaviorFlags(mimicsMotion: true, reactiveFlag: false);
            var player = new GameObject("Player");
            player.transform.position = Vector2.left * 3f;
            remnant.SetTarget(player.transform);
            remnant.Tick(0f); // Idle -> Alert (아직 FaceTarget을 부르지 않은 상태)

            // 플레이어가 몸쪽으로(오른쪽으로) 움직인다. 실제 위치는 여전히 왼쪽이라
            // 단순히 "위치를 바라보는" 것과는 다른 결과가 나와야 모방이 증명된다.
            player.transform.position = Vector2.left * 2f;
            remnant.Tick(0f);
            Assert.That(remnant.MirroredPlayerFacingDirection, Is.EqualTo(1f));
            Assert.That(remnant.FacingDirection, Is.EqualTo(1f));

            // 플레이어가 이번엔 반대(왼쪽)로 움직인다.
            player.transform.position = Vector2.left * 5f;
            remnant.Tick(0f);
            Assert.That(remnant.MirroredPlayerFacingDirection, Is.EqualTo(-1f));
            Assert.That(remnant.FacingDirection, Is.EqualTo(-1f));

            Object.DestroyImmediate(remnant.gameObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Test_Remnant_ReactiveWaitsForPlayerAggression()
        {
            var remnant = CreateMelee(out var data);
            data.SetStageIdentity(11, VisualTraitTag.HumanoidSilhouette | VisualTraitTag.ProtagonistHand);
            data.SetBehaviorFlags(mimicsMotion: false, reactiveFlag: true);
            var player = CreatePlayer(out var health);
            player.transform.position = Vector2.right * .5f; // 이미 공격 사거리 안

            remnant.SetTarget(player.transform);
            for (var i = 0; i < 20; i++)
            {
                remnant.Tick(0.1f);
                Assert.That(remnant.State, Is.Not.EqualTo(RemnantState.Attack), "Reactive remnant must not self-initiate.");
            }

            Assert.That(remnant.IsYielding, Is.True);
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));

            // 플레이어가 먼저 때린다(선공) → 이제부터는 반응할 수 있다.
            remnant.ApplyDamage(new DamageRequest(1));
            var becameAttack = false;
            for (var i = 0; i < 20 && !becameAttack; i++)
            {
                remnant.Tick(0.1f);
                becameAttack = remnant.State == RemnantState.Attack;
            }

            Assert.That(becameAttack, Is.True, "Reactive remnant should be able to fight back once hit.");

            Object.DestroyImmediate(remnant.gameObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Test_Remnant_FragmentTracePointsToTrauma()
        {
            var traced = CreateMelee(out var tracedData);
            tracedData.SetStageIdentity(8, VisualTraitTag.HumanoidSilhouette);
            tracedData.SetFragmentTraceRatio(1f);
            var trauma = new GameObject("TraumaDirection");
            trauma.transform.position = Vector2.right * 5f;
            traced.SetTraumaTarget(trauma.transform);
            traced.RandomProvider = () => 0f;
            traced.ApplyDamage(new DamageRequest(tracedData.MaxHealth));

            Assert.That(traced.State, Is.EqualTo(RemnantState.Dead));
            Assert.That(traced.HasFragmentTrace, Is.True);
            Assert.That(traced.FragmentTraceDirection, Is.EqualTo(1f));

            var untraced = CreateMelee(out var untracedData);
            untracedData.SetStageIdentity(8, VisualTraitTag.HumanoidSilhouette);
            untracedData.SetFragmentTraceRatio(0f);
            untraced.SetTraumaTarget(trauma.transform);
            untraced.ApplyDamage(new DamageRequest(untracedData.MaxHealth));

            Assert.That(untraced.HasFragmentTrace, Is.False);

            Object.DestroyImmediate(traced.gameObject);
            Object.DestroyImmediate(tracedData);
            Object.DestroyImmediate(untraced.gameObject);
            Object.DestroyImmediate(untracedData);
            Object.DestroyImmediate(trauma);
        }

        [Test]
        public void Test_DashRemnant_StopsBurstBeforeSolidTerrain()
        {
            var dash = CreateDash(out var data);
            var player = CreatePlayer(out _);
            var wall = CreateSolidTerrain("Wall", new Vector2(1.25f, 0f), new Vector2(0.2f, 3f));
            player.transform.position = Vector2.right * (data.DashTriggerRange - 0.1f);
            dash.SetTarget(player.transform);

            AdvanceThroughAlert(dash, data.AlertSeconds);
            dash.Tick(data.AttackTelegraphSeconds);
            Physics2D.SyncTransforms();
            dash.Tick(0.2f);
            Physics2D.SyncTransforms();

            var dashCollider = dash.GetComponent<Collider2D>();
            var wallCollider = wall.GetComponent<Collider2D>();
            Assert.That(dashCollider.bounds.max.x, Is.LessThanOrEqualTo(wallCollider.bounds.min.x + 0.001f));
            Assert.That(Physics2D.Distance(dashCollider, wallCollider).isOverlapped, Is.False);

            Object.DestroyImmediate(dash.gameObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(wall);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void Test_DashRemnant_DoesNotEnterFloorOrWallDuringBurst()
        {
            var dash = CreateDash(out var data);
            var player = CreatePlayer(out _);
            var platform = CreateSolidTerrain("PlatformEdge", new Vector2(1.5f, 0f), new Vector2(1.5f, 0.5f));
            player.transform.position = Vector2.right * (data.DashTriggerRange - 0.1f);
            dash.SetTarget(player.transform);

            AdvanceThroughAlert(dash, data.AlertSeconds);
            dash.Tick(data.AttackTelegraphSeconds);
            Physics2D.SyncTransforms();
            dash.Tick(data.DashMaxDurationSeconds);
            Physics2D.SyncTransforms();

            var dashCollider = dash.GetComponent<Collider2D>();
            var platformCollider = platform.GetComponent<Collider2D>();
            Assert.That(Physics2D.Distance(dashCollider, platformCollider).isOverlapped, Is.False);
            Assert.That(dashCollider.bounds.max.x, Is.LessThanOrEqualTo(platformCollider.bounds.min.x + 0.001f));

            Object.DestroyImmediate(dash.gameObject);
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(platform);
            Object.DestroyImmediate(data);
        }

        private static void AdvanceThroughAlert(RemnantActor remnant, float alertSeconds)
        {
            remnant.Tick(0f);
            remnant.Tick(alertSeconds);
            remnant.Tick(0f);
        }

        private static MeleeRemnant CreateMelee(out MeleeRemnantData data)
        {
            var gameObject = new GameObject("MeleeRemnant");
            gameObject.AddComponent<BoxCollider2D>();
            var remnant = gameObject.AddComponent<MeleeRemnant>();
            data = ScriptableObject.CreateInstance<MeleeRemnantData>();
            remnant.SetData(data);
            return remnant;
        }

        private static DashRemnant CreateDash(out DashRemnantData data)
        {
            var gameObject = new GameObject("DashRemnant");
            gameObject.AddComponent<BoxCollider2D>();
            var remnant = gameObject.AddComponent<DashRemnant>();
            data = ScriptableObject.CreateInstance<DashRemnantData>();
            remnant.SetData(data);
            return remnant;
        }

        private static RangedRemnant CreateRanged(out RangedRemnantData data)
        {
            var gameObject = new GameObject("RangedRemnant");
            gameObject.AddComponent<BoxCollider2D>();
            var remnant = gameObject.AddComponent<RangedRemnant>();
            data = ScriptableObject.CreateInstance<RangedRemnantData>();
            remnant.SetData(data);
            return remnant;
        }

        private static GameObject CreatePlayer(out Daeume.Player.PlayerHealth health)
        {
            var player = new GameObject("Player");
            health = player.AddComponent<Daeume.Player.PlayerHealth>();
            return player;
        }

        private static GameObject CreateSolidTerrain(string name, Vector2 position, Vector2 size)
        {
            var terrain = new GameObject(name);
            terrain.transform.position = position;
            var collider = terrain.AddComponent<BoxCollider2D>();
            collider.size = size;
            return terrain;
        }
    }
}
