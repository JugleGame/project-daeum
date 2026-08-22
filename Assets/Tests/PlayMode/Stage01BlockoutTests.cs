using System.Collections;
using Daeume.Player;
using Daeume.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class Stage01BlockoutTests
    {
        [UnityTest]
        public IEnumerator Test_Stage01_PlayerMovesJumpsAndUsesGrabSurface()
        {
            yield return LoadStage();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            var keyboard = InputSystem.AddDevice<Keyboard>();

            body.position = Vector2.zero;
            body.linearVelocity = Vector2.zero;
            yield return WaitForGrounded(player);

            var startX = body.position.x;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            InputSystem.Update();

            // Update()가 큐에 쌓인 키 상태를 읽을 기회를 최소 한 번 보장한다.
            // 이게 없으면 첫 WaitForFixedUpdate가 Update보다 먼저 걸려 이번 프레임의 입력을 놓치고 flaky해진다.
            yield return null;

            for (var frame = 0; frame < 30; frame++)
            {
                yield return new WaitForFixedUpdate();
            }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            Assert.That(body.position.x, Is.GreaterThan(startX + 1f));

            Assert.That(player.TryJump(), Is.True);
            var jumpStartY = body.position.y;
            for (var frame = 0; frame < 8; frame++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(body.position.y, Is.GreaterThan(jumpStartY + .25f));

            var grabSurface = GameObject.Find("GrabWall_Zone").GetComponent<GrabbableSurface>();
            player.SetGroundedForTest(false);
            Assert.That(player.TryBeginGrab(grabSurface), Is.True);
            Assert.That(player.IsGrabbing, Is.True);
            InputSystem.RemoveDevice(keyboard);
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Test_Stage01_FallRecoveryAndCameraBoundsAreFunctional()
        {
            yield return LoadStage();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            var bounds = Object.FindAnyObjectByType<StageCameraBounds>();
            var camera = Camera.main;

            body.position = new Vector2(8f, -2.5f);
            body.linearVelocity = Vector2.down;
            yield return WaitForGrounded(player);
            Assert.That(body.position.y, Is.GreaterThan(-4.1f));

            body.position = new Vector2(100f, 0f);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(camera.transform.position.x, Is.EqualTo(bounds.Maximum.x).Within(.01f));
            body.position = new Vector2(-100f, 0f);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(camera.transform.position.x, Is.EqualTo(bounds.Minimum.x).Within(.01f));
            LogAssert.NoUnexpectedReceived();
        }

        private static IEnumerator LoadStage()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;
        }

        private static IEnumerator WaitForGrounded(PlayerController player)
        {
            for (var frame = 0; frame < 120; frame++)
            {
                yield return new WaitForFixedUpdate();
                if (player.IsGrounded)
                {
                    yield break;
                }
            }

            Assert.Fail("Player did not reach authored Stage01 ground within 120 fixed frames.");
        }
    }
}
