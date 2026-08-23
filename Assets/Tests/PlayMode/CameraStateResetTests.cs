using System.Collections;
using Daeume.Contamination;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Flow;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    public sealed class CameraStateResetTests
    {
        private const float StageOneVerticalPosition = 0f;
        private const float PositionTolerance = 0.001f;

        [UnityTest]
        public IEnumerator Test_Camera_NewGameAfterVerticalStageResetsStage01VerticalPosition()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage03_Base", LoadSceneMode.Additive);
            yield return MovePlayerVerticallyAndWaitForCamera(3f);

            var camera = Camera.main;
            Assert.That(camera.transform.position.y, Is.GreaterThan(1f),
                "Stage03의 세로 추적이 먼저 카메라 Y를 변경해야 회귀 조건이 성립한다.");

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.StartNewGame(), Is.True);
            yield return WaitForScene("Stage01_Base");
            yield return null;

            Assert.That(camera.transform.position.y,
                Is.EqualTo(StageOneVerticalPosition).Within(PositionTolerance),
                "새 게임으로 Stage01에 돌아오면 이전 세로 스테이지의 Y를 상속하면 안 된다.");
        }

        [UnityTest]
        public IEnumerator Test_Camera_StageTransitionDoesNotCarryPreviousVerticalPosition()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage02_Base", LoadSceneMode.Additive);
            yield return MovePlayerVerticallyAndWaitForCamera(3f);

            var camera = Camera.main;
            Assert.That(camera.transform.position.y, Is.GreaterThan(1f));

            yield return SceneManager.UnloadSceneAsync("Stage02_Base");
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            Assert.That(camera.transform.position.y,
                Is.EqualTo(StageOneVerticalPosition).Within(PositionTolerance),
                "followVertical=false인 Stage01은 현재 카메라 Y가 아니라 스테이지 기준 Y를 사용해야 한다.");
        }

        [UnityTest]
        public IEnumerator Test_Presentation_ShakeDoesNotDriftAcrossStageTransition()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage03_Base", LoadSceneMode.Additive);
            yield return MovePlayerVerticallyAndWaitForCamera(3f);

            GameManager.Instance.Events.Publish(
                new ContaminationPressureChanged("stage03-test", PressureStage.Intrusion, string.Empty));
            yield return null;

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.That(flow.StartNewGame(), Is.True);
            yield return WaitForScene("Stage01_Base");
            yield return null;

            Assert.That(Camera.main.transform.position.y,
                Is.EqualTo(StageOneVerticalPosition).Within(PositionTolerance),
                "이전 스테이지의 마지막 흔들림 오프셋이 Stage01 기준 위치에 남으면 안 된다.");
        }

        private static IEnumerator MovePlayerVerticallyAndWaitForCamera(float y)
        {
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null);
            var position = player.transform.position;
            position.y = y;
            player.transform.position = position;
            var body = player.GetComponent<Rigidbody2D>();
            if (body != null) body.position = position;
            Physics2D.SyncTransforms();
            yield return null;
        }

        private static IEnumerator WaitForScene(string sceneName)
        {
            for (var frame = 0; frame < 600; frame++)
            {
                var scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid() && scene.isLoaded) yield break;
                yield return null;
            }

            Assert.Fail($"{sceneName} did not load within 600 frames.");
        }
    }
}
