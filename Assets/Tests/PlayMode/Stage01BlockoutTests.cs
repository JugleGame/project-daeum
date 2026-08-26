using System.Collections;
using Daeume.Core;
using Daeume.Flow;
using Daeume.Player;
using Daeume.Stage;
using Daeume.UI;
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

            // 매달림 표면은 blockout 벽이 아니라 가로등 프리팹(05-lamp-utility-pole)이 갖는다.
            var grabSurface = Object.FindAnyObjectByType<GrabbableSurface>();
            Assert.That(grabSurface, Is.Not.Null, "Stage01에 GrabbableSurface가 없다.");
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

            // blockout 발판을 걷어 낸 뒤로 Stage01의 지면은 GroundTilemap 하나뿐이다(윗면 y = -1).
            body.position = new Vector2(8f, 1.5f);
            body.linearVelocity = Vector2.down;
            yield return WaitForGrounded(player);
            Assert.That(body.position.y, Is.GreaterThan(-1.5f));

            // 경계는 카메라 중심이 아니라 화면에 담겨도 되는 세계 좌표의 끝이다.
            // 카메라 중심이 어디까지 갈 수 있는지는 화면 반너비를 뺀 값이며, 그 계산은
            // StageCameraBounds가 소유한다(종횡비가 달라져도 같은 규칙이 적용된다).
            StageCameraBounds.ResolveCameraLimits(camera, bounds.Minimum, bounds.Maximum, out var lower, out var upper);

            body.position = new Vector2(100f, 0f);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(camera.transform.position.x, Is.EqualTo(upper.x).Within(.01f));
            Assert.That(camera.transform.position.x + camera.orthographicSize * camera.aspect,
                Is.LessThanOrEqualTo(bounds.Maximum.x + .01f), "화면 오른쪽 끝이 콘텐츠 밖을 비추면 안 된다.");

            body.position = new Vector2(-100f, 0f);
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(camera.transform.position.x, Is.EqualTo(lower.x).Within(.01f));
            Assert.That(camera.transform.position.x - camera.orthographicSize * camera.aspect,
                Is.GreaterThanOrEqualTo(bounds.Minimum.x - .01f), "화면 왼쪽 끝이 콘텐츠 밖을 비추면 안 된다.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Test_Stage01_FallingIntoVoidRespawnsPlayerWithoutFailingStage()
        {
            yield return LoadStage();
            var manager = GameManager.Instance;
            var player = Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();

            StageMarker recovery = null;
            foreach (var marker in Object.FindObjectsByType<StageMarker>(FindObjectsSortMode.None))
            {
                if (marker.Kind == StageMarkerKind.FallRecovery) { recovery = marker; break; }
            }

            Assert.That(recovery, Is.Not.Null, "Stage01은 낙사 복귀 마커를 선언해야 한다.");

            // 발판(blockout 바닥, minY ~= -4.5) 아래 VoidZone(#11) 안으로 완전히 벗어난다.
            body.position = new Vector2(15f, -15f);
            body.linearVelocity = new Vector2(0f, -20f);
            Physics2D.SyncTransforms();

            for (var frame = 0; frame < 10 && body.position.y < -5f; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            // spec-001: 낙사는 두 허용된 실패 원인(HealthDepleted/TraumaGrabCompleted)에 없으므로
            // 스테이지를 Failed로 만들지 않는다 — "보이지 않는 즉사" 금지 규칙.
            Assert.That(manager.StageState, Is.Not.EqualTo(StageState.Failed));
            Assert.That(body.position.y, Is.GreaterThan(-5f), "Player should have been teleported back onto the playable floor.");
            // 저장 위치가 아니라 레벨이 선언한 낙사 복귀 마커로 되돌린다.
            // 저장 위치(SaveData.PlayerPosition)는 추격 체크포인트에서만 갱신돼서
            // 탐색 구간에서는 계속 (0,0)이었다 — 구덩이에 빠질 때마다 스테이지 시작점으로 튕겼다.
            Assert.That(Vector2.Distance(body.position, recovery.transform.position), Is.LessThan(0.05f),
                "Player should be restored to the FallRecovery marker declared by the level.");
            LogAssert.NoUnexpectedReceived();
        }

        [UnityTest]
        public IEnumerator Test_Stage01_TutorialHudShowsControlHints()
        {
            yield return LoadStage();

            var hud = Object.FindAnyObjectByType<StageHudPresenter>();
            Assert.That(hud, Is.Not.Null, "StagePresentationBootstrap should have spawned the HUD.");

            // A prior PlayMode test can replace the Persistent scene's GameManager after this
            // persistent HUD first enabled. Re-enable it so the fixture reconnects to the current
            // event bus before requesting the Explore-state objective.
            hud.enabled = false;
            hud.enabled = true;
            yield return null;
            GameManager.Instance.ResetStage();

            var jumpLabel = StringTable.Get("options.rebind.jump");
            for (var frame = 0; frame < 30 && !hud.ObjectiveLabel.Contains(jumpLabel); frame++)
            {
                yield return null;
            }

            Assert.That(hud.ObjectiveLabel, Does.Contain(jumpLabel));
            Assert.That(hud.ObjectiveLabel, Does.Contain(StringTable.Get("options.rebind.interact")));
            Assert.That(hud.ObjectiveLabel, Does.Contain(StringTable.Get("hud.objective.memory")));
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
