using System.Collections;
using System.Linq;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Flow;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    /// <summary>
    /// Stage 02~06을 빌드에서 내리기(#56) 전까지 Stage02FlowTests가 들고 있던 회귀 테스트 중,
    /// Stage 02와 무관하게 성립하는 것들을 옮겨 왔다. 원래 이름은 Stage 02였지만 하나는
    /// 처음부터 Stage01_Base만 열었고, 다른 하나는 어느 Encounter에서나 성립하는 규칙이다.
    /// </summary>
    public sealed class Stage01ChaseAndEncounterRegressionTests
    {
        /// <summary>
        /// #12: 트라우마에게 붙잡히면 게임 오버다. 체크포인트로 되돌리지 않고 타이틀로 나간다.
        ///
        /// 되돌리던 시절에는 부활 지점이 탈출 경로 반대편일 때 추격자를 지나갈 방법이 없어
        /// 붙잡힘 → 복귀 → 붙잡힘이 무한 반복됐다(Stage 01·02 모두).
        /// </summary>
        [UnityTest]
        public IEnumerator Test_Chase_TraumaGrabEndsTheGameInsteadOfRespawning()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null;

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.That(flow.StartNewGame(), Is.True);
            yield return WaitForScene("Stage01_Base");

            GameManager.Instance.SetStageState(StageState.Memory);
            GameManager.Instance.SetStageState(StageState.Chase);
            flow.SaveChaseCheckpoint("Stage01_Chase", new Vector2(5f, 0f), 3, "Stage01_Overlay_Intrusion");
            Assert.That(flow.CurrentData.CheckpointId, Is.Not.Empty);

            Assert.That(GameManager.Instance.Fail(StageFailureCause.TraumaGrabCompleted), Is.True);
            yield return WaitForScene("Title");

            Assert.That(flow.CurrentData.CheckpointId, Is.Empty,
                "게임 오버 후 체크포인트가 남으면 이어하기가 죽은 자리에서 다시 시작한다.");
            Assert.That(SceneManager.GetSceneByName("Stage01_Base").isLoaded, Is.False,
                "스테이지 씬은 내려가야 한다.");
        }

        /// <summary>
        /// 회귀(#12): 잔재 몸통이 solid 콜라이더면 플레이어를 위로 튕겨 올린다.
        ///
        /// 잔재는 Rigidbody2D 없이 transform으로 움직여서 유니티가 정적 콜라이더로 취급한다.
        /// 그게 플레이어를 파고들면 겹침 해소가 최소 축(= 납작한 몸통이라 위쪽)으로 강하게 밀어낸다.
        /// 그 상태에서는 접지가 아니라 점프가 아예 나가지 않는다("Space가 안 먹는" 증상).
        /// </summary>
        [UnityTest]
        public IEnumerator Test_Encounter_RemnantBodyDoesNotPushPlayerOffTheGround()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            var encounter = Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None)
                .Single(controller => controller.Data.EncounterId == "stage01.encounter.01");
            Assert.That(encounter.TryActivate(), Is.True);
            yield return null;

            var enemy = encounter.ActiveEnemies.First();
            Assert.That(enemy.GetComponent<Collider2D>().isTrigger, Is.True,
                "잔재 몸통은 트리거여야 한다. solid면 플레이어를 밀어 올린다.");

            // 잔재와 정확히 겹친 자리에 세워도 땅에 붙어 있어야 한다.
            var player = Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            var startY = enemy.transform.position.y + 0.25f;
            body.position = new Vector2(enemy.transform.position.x, startY);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();

            for (var frame = 0; frame < 30; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(body.position.y, Is.LessThan(startY + 0.5f),
                "잔재와 겹쳐도 플레이어가 위로 솟구치면 안 된다.");
            Assert.That(player.IsGrounded, Is.True, "겹친 상태에서도 접지가 유지돼야 점프가 나간다.");
            Assert.That(player.TryJump(), Is.True);
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
