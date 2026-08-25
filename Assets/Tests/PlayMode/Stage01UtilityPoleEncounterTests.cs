using System.Collections;
using System.Linq;
using Daeume.Encounter;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Daeume.Tests.PlayMode
{
    public sealed class Stage01UtilityPoleEncounterTests
    {
        [UnityTest]
        public IEnumerator Test_Stage01_UtilityPoleCrossingStartsOneRemnantEncounter()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            var player = Object.FindAnyObjectByType<PlayerController>();
            var encounter = Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None)
                .Single(controller => controller.Data.EncounterId == "stage01.encounter.01");
            var tilemapCollider = Object.FindAnyObjectByType<TilemapCollider2D>();
            var pole = GameObject.Find("05-lamp-utility-pole");
            var poleCollider = pole.transform.Find("Visual").GetComponent<BoxCollider2D>();
            var trigger = GameObject.Find("EncounterTriggerMarker").GetComponent<BoxCollider2D>();

            Assert.That(player, Is.Not.Null);
            Assert.That(encounter, Is.Not.Null);
            Assert.That(tilemapCollider, Is.Not.Null);
            Assert.That(tilemapCollider.enabled, Is.True);
            Assert.That(tilemapCollider.isTrigger, Is.False);
            Assert.That(tilemapCollider.shapeCount, Is.GreaterThan(0));
            Assert.That(poleCollider.enabled, Is.True);
            Assert.That(poleCollider.isTrigger, Is.False);
            Assert.That(trigger.isTrigger, Is.True);
            Assert.That(trigger.bounds.min.x, Is.GreaterThan(poleCollider.bounds.max.x));
            Assert.That(encounter.Data.SpawnCount, Is.EqualTo(1));
            Assert.That(encounter.Data.WaveCount, Is.EqualTo(1));
            Assert.That(encounter.Data.LockExit, Is.False);
            Assert.That(encounter.State, Is.EqualTo(EncounterState.Inactive));

            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(trigger.bounds.min.x - 2f, trigger.bounds.center.y);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            Assert.That(encounter.State, Is.EqualTo(EncounterState.Inactive));

            body.position = trigger.bounds.center;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(encounter.State, Is.EqualTo(EncounterState.Active));
            Assert.That(encounter.TotalSpawnCount, Is.EqualTo(1));
            Assert.That(encounter.ActiveEnemies, Has.Count.EqualTo(1));

            body.position = new Vector2(trigger.bounds.min.x - 2f, trigger.bounds.center.y);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            body.position = trigger.bounds.center;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(encounter.TotalSpawnCount, Is.EqualTo(1));
            Assert.That(encounter.ActiveEnemies, Has.Count.EqualTo(1));
            LogAssert.NoUnexpectedReceived();
        }
    

[UnityTest]
        public IEnumerator Test_Stage01_PlayerLandsOnGroundTilemap()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            var player = Object.FindAnyObjectByType<PlayerController>();
            var tilemapCollider = Object.FindAnyObjectByType<TilemapCollider2D>();
            Assert.That(player, Is.Not.Null);
            Assert.That(tilemapCollider, Is.Not.Null);
            Assert.That(tilemapCollider.shapeCount, Is.GreaterThan(0));

            var body = player.GetComponent<Rigidbody2D>();
            for (var frame = 0; frame < 120; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (player.IsGrounded && Mathf.Abs(body.linearVelocity.y) < 0.1f)
                {
                    break;
                }
            }

            Assert.That(player.IsGrounded, Is.True);
            Assert.That(body.position.y, Is.GreaterThan(tilemapCollider.bounds.min.y));
            Assert.That(Mathf.Abs(body.linearVelocity.y), Is.LessThan(0.1f));
            LogAssert.NoUnexpectedReceived();
        }
}
}
