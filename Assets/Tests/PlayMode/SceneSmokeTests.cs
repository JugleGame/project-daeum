using System.Collections;
using NUnit.Framework;
using Daeume.Core;
using Daeume.Flow;
using Daeume.Player;
using Daeume.Prototype;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class SceneSmokeTests
    {
        [UnityTest]
        public IEnumerator Test_Runtime_BootPersistentTitle_NoConsoleErrors()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            const int frameLimit = 180;
            for (var frame = 0; frame < frameLimit; frame++)
            {
                if (SceneManager.GetSceneByName("Persistent").isLoaded &&
                    SceneManager.GetSceneByName("Title").isLoaded)
                {
                    LogAssert.NoUnexpectedReceived();
                    yield break;
                }

                yield return null;
            }

            Assert.Fail("Boot did not load Persistent and Title within 180 frames.");
        }

        [UnityTest]
        public IEnumerator Test_PrototypeScene_LoadsWithoutConsoleErrors()
        {
            SceneManager.LoadScene("RoleAPrototype", LoadSceneMode.Single);
            yield return null;
            Assert.That(Object.FindFirstObjectByType<PrototypeHarness>(), Is.Not.Null);
            Assert.That(GameObject.Find("RemnantDummy"), Is.Not.Null);
            Assert.That(GameObject.Find("InteractionDummy"), Is.Not.Null);
            Assert.That(GameObject.Find("TraumaDummy"), Is.Not.Null);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Test_PrototypePlayer_LandsOnGround()
        {
            SceneManager.LoadScene("RoleAPrototype", LoadSceneMode.Single);
            yield return null;

            var player = GameObject.Find("Player");
            var controller = player.GetComponent<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();

            // groundProbe는 실제 콜라이더 바닥보다 살짝 아래까지 뻗어 있어(반지름 포함),
            // 낙하 도중 진짜 충돌 반응이 일어나기 한 프레임 전에 IsGrounded가 먼저 True로 뜰 수 있다.
            // 그래서 grounded만 보고 바로 멈추지 않고, 속도도 함께 가라앉을 때까지 기다린다.
            const int frameLimit = 120;
            for (var frame = 0; frame < frameLimit; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (controller.IsGrounded && Mathf.Abs(body.linearVelocity.y) < 0.1f) break;
            }

            Assert.That(controller.IsGrounded, Is.True, $"Player did not land within {frameLimit} fixed frames.");
            Assert.That(player.transform.position.y, Is.GreaterThan(-0.8f));
            Assert.That(Mathf.Abs(body.linearVelocity.y), Is.LessThan(0.1f));
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Test_PrototypeCheckpoint_ActivatesOnContactAndRestoresPlayer()
        {
            SceneManager.LoadScene("RoleAPrototype", LoadSceneMode.Single);
            yield return null;

            var player = GameObject.Find("Player");
            var body = player.GetComponent<Rigidbody2D>();
            var marker = GameObject.Find("CheckpointMarker");
            var checkpoint = marker.GetComponent<PrototypeCheckpoint>();
            var flow = Object.FindFirstObjectByType<SceneFlowController>();
            Assert.That(checkpoint.Activated, Is.False);

            body.position = marker.transform.position;
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();

            Assert.That(checkpoint.Activated, Is.True);
            Assert.That(flow.CurrentData.PlayerPosition, Is.EqualTo((Vector2)marker.transform.position));

            body.position = new Vector2(8f, 0f);
            GameManager.Instance.Events.Publish(new ChaseCheckpointRestoreRequested("Stage01_Chase"));
            yield return null;
            Assert.That(Vector2.Distance(body.position, marker.transform.position), Is.LessThan(0.01f));
            LogAssert.NoUnexpectedReceived();
        }
    }
}
