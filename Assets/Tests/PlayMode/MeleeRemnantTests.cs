using System.Collections;
using System.Linq;
using Daeume.Contamination;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Enemy;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class MeleeRemnantTests
    {
        [UnityTest]
        public IEnumerator Test_Remnant_DeathDisablesDamage()
        {
            var remnant = CreateRemnant(out var data);
            var player = CreatePlayer(out var health);
            player.transform.position = Vector2.right * .5f;
            remnant.SetTarget(player.transform);
            AdvanceToAttack(remnant, data);
            Assert.That(remnant.IsTelegraphing, Is.True);

            var healthBefore = health.CurrentHealth;
            var deathDamage = remnant.ApplyDamage(new DamageRequest(data.MaxHealth));
            Assert.That(deathDamage.Applied, Is.True);
            Assert.That(deathDamage.Amount, Is.EqualTo(data.MaxHealth));
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Dead));
            Assert.That(remnant.GetComponent<Collider2D>().enabled, Is.False);
            Assert.That(remnant.CanDealDamage, Is.False);
            remnant.Tick(data.AttackTelegraphSeconds + data.AttackRecoverySeconds + 1f);
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBefore));
            Assert.That(remnant.ApplyDamage(new DamageRequest(1)).Applied, Is.False);

            Cleanup(remnant.gameObject, player, data);
            yield return null;
        }

        [Test]
        public void Test_Remnant_RespondsToTraumaPressure()
        {
            var remnant = CreateRemnant(out var data);
            var trauma = new GameObject("TraumaDirection");
            trauma.transform.position = Vector2.left * 3f;
            remnant.SetTraumaTarget(trauma.transform);

            remnant.SetPressure(PressureStage.Stable);
            remnant.Tick(0f);
            Assert.That(remnant.TraumaAttentionActive, Is.False);
            remnant.SetPressure(PressureStage.Echo);
            remnant.Tick(0f);
            Assert.That(remnant.TraumaAttentionActive, Is.True);
            Assert.That(remnant.FacingDirection, Is.EqualTo(-1f));
            trauma.transform.position = Vector2.right * 3f;
            remnant.SetPressure(PressureStage.Intrusion);
            remnant.Tick(0f);
            Assert.That(remnant.FacingDirection, Is.EqualTo(1f));
            Assert.That(data.AttackTelegraphSeconds * data.GetProfile(PressureStage.Intrusion).TelegraphMultiplier,
                Is.GreaterThanOrEqualTo(.05f));

            Cleanup(remnant.gameObject, trauma, data);
        }

        [UnityTest]
        public IEnumerator Test_Remnant_CommonStateFlowPreservesTelegraph()
        {
            var remnant = CreateRemnant(out var data);
            var player = CreatePlayer(out var health);
            player.transform.position = Vector2.right * .5f;
            remnant.SetTarget(player.transform);

            Assert.That(remnant.State, Is.EqualTo(RemnantState.Idle));
            remnant.Tick(0f);
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Alert));
            remnant.Tick(data.AlertSeconds);
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Approach));
            remnant.Tick(0f);
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Attack));
            Assert.That(remnant.IsTelegraphing, Is.True);

            var healthBefore = health.CurrentHealth;
            remnant.Tick(data.AttackTelegraphSeconds * .5f);
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBefore));
            Assert.That(remnant.IsTelegraphing, Is.True);
            remnant.Tick(data.AttackTelegraphSeconds);
            Assert.That(health.CurrentHealth, Is.EqualTo(healthBefore - data.ContactDamage));
            Assert.That(remnant.IsTelegraphing, Is.False);

            remnant.ApplyDamage(new DamageRequest(1));
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Hit));
            remnant.Tick(data.HitStunSeconds);
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Alert));
            remnant.ApplyDamage(new DamageRequest(data.MaxHealth));
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Dead));

            Cleanup(remnant.gameObject, player, data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Remnant_PlayerCombatCausesHitAndDeath()
        {
            var player = new GameObject("Player");
            var combat = player.AddComponent<PlayerCombat>();
            var remnant = CreateRemnant(out var data);
            remnant.transform.position = Vector2.zero;
            Physics2D.SyncTransforms();

            Assert.That(combat.Attack(), Is.EqualTo(1));
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Hit));
            Assert.That(combat.Attack(), Is.EqualTo(1));
            Assert.That(combat.Attack(), Is.EqualTo(1));
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Dead));
            Assert.That(combat.PlayerAggression, Is.True);

            Cleanup(remnant.gameObject, player, data);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Test_Remnant_Stage01PrefabUsesRoleACombat()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            var combat = Object.FindAnyObjectByType<PlayerCombat>();
            var encounter = Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None)
                .Single(controller => controller.Data.EncounterId == "stage01.encounter.01");
            Assert.That(combat, Is.Not.Null);
            Assert.That(encounter, Is.Not.Null);
            Assert.That(encounter.TryActivate(), Is.True);
            var remnant = encounter.ActiveEnemies[0];
            Assert.That(remnant, Is.Not.Null);
            remnant.transform.position = combat.transform.position + Vector3.right * .55f;
            Physics2D.SyncTransforms();

            remnant.SetTarget(combat.transform);
            AdvanceToAttack(remnant, remnant.Data);
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Attack));
            Assert.That(remnant.transform.Find("AttackTelegraph").GetComponent<SpriteRenderer>().enabled, Is.True);

            Assert.That(combat.Attack(), Is.EqualTo(1));
            Assert.That(combat.Attack(), Is.EqualTo(1));
            Assert.That(combat.Attack(), Is.EqualTo(1));
            Assert.That(remnant.State, Is.EqualTo(RemnantState.Dead));
            Assert.That(remnant.GetComponent<Collider2D>().enabled, Is.False);
            LogAssert.NoUnexpectedReceived();
            yield return ResetLoadedScenes();
        }

        private static MeleeRemnant CreateRemnant(out MeleeRemnantData data)
        {
            var gameObject = new GameObject("MeleeRemnant");
            gameObject.AddComponent<BoxCollider2D>();
            var remnant = gameObject.AddComponent<MeleeRemnant>();
            data = ScriptableObject.CreateInstance<MeleeRemnantData>();
            remnant.SetData(data);
            return remnant;
        }

        private static GameObject CreatePlayer(out PlayerHealth health)
        {
            var player = new GameObject("Player");
            health = player.AddComponent<PlayerHealth>();
            return player;
        }

        private static void AdvanceToAttack(MeleeRemnant remnant, MeleeRemnantData data)
        {
            remnant.Tick(0f);
            remnant.Tick(data.AlertSeconds);
            remnant.Tick(0f);
        }

        private static void Cleanup(GameObject first, GameObject second, ScriptableObject data)
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
            Object.DestroyImmediate(data);
        }

        private static IEnumerator ResetLoadedScenes()
        {
            var cleanup = SceneManager.CreateScene("B2_TestCleanup");
            SceneManager.SetActiveScene(cleanup);
            for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene == cleanup) continue;
                yield return SceneManager.UnloadSceneAsync(scene);
            }
        }
    }
}
