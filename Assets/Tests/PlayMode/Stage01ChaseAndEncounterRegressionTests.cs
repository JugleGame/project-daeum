using System.Collections;
using System.Linq;

using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Flow;
using Daeume.Player;
using Daeume.UI;

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
        /// Trauma 피해로 체력이 0이 되면 저장된 chase checkpoint에서 Stage01을 재시작한다.
        ///
        /// 회귀 조건은 체력 규칙을 거쳐 사망할 것, 접촉 중 입력을 잠그지 않을 것,
        /// Title을 열지 않고 checkpoint와 chase 상태를 복구할 것이다.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_Chase_TraumaDamageRespawnsAtCheckpointAndRestoresControl()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return WaitForScene("Title");

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.StartNewGame(), Is.True);
            yield return WaitForScene("Stage01_Base");

            GameManager.Instance.SetStageState(StageState.Memory);
            GameManager.Instance.SetStageState(StageState.Chase);
            var checkpoint = new Vector2(5f, 0f);
            flow.SaveChaseCheckpoint("Stage01_Chase", checkpoint, 3, "Stage01_Overlay_Intrusion");

            var stageBeforeDeath = SceneManager.GetSceneByName("Stage01_Base");
            var stageRootBeforeDeath = stageBeforeDeath.GetRootGameObjects().First();
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.That(player, Is.Not.Null);
            var traumaContact = player.GetComponent<TraumaContactHandler>();
            var health = player.GetComponent<PlayerHealth>();
            Assert.That(traumaContact, Is.Not.Null);
            Assert.That(health, Is.Not.Null);

            health.Restore(1);
            yield return new WaitForSeconds(1.55f);
            Assert.That(health.CurrentHealth, Is.EqualTo(1));
            Assert.That(traumaContact.BeginGrab(), Is.True);
            Assert.That(health.CurrentHealth, Is.Zero);
            Assert.That(player.InputEnabled, Is.True,
                "Trauma의 lethal contact도 이동 입력을 잠그지 않아야 한다.");

            for (var sample = 0; sample < 100 && stageRootBeforeDeath != null; sample++)
            {
                yield return new WaitForSeconds(0.05f);
            }

            Assert.That(stageRootBeforeDeath == null, Is.True, "Trauma 사망 후 Stage01이 재적재되지 않았다.");
            yield return WaitForScene("Stage01_Base");
            yield return new WaitForFixedUpdate();
            yield return null;

            Assert.That(SceneManager.GetSceneByName("Title").isLoaded, Is.False,
                "Trauma 사망은 Title이 아니라 체크포인트 재시도로 이어져야 한다.");
            Assert.That(flow.CurrentData.CheckpointId, Is.EqualTo("Stage01_Chase"));
            var restoredChase = Object.FindAnyObjectByType<StageOneChaseController>();
            Assert.That(restoredChase, Is.Not.Null);
            Assert.That(restoredChase.ChaseStarted, Is.True, "checkpoint 복귀 시 chase runtime이 시작되지 않았다.");
            Assert.That(restoredChase.Director.ChaseActive, Is.True);
            var restoredTrauma = GameObject.Find("Trauma");
            Assert.That(restoredTrauma, Is.Not.Null, "checkpoint 복귀 후 Trauma가 활성화되지 않았다.");
            Assert.That(restoredTrauma.transform.position.x, Is.GreaterThan(player.transform.position.x),
                "Stage01 재시작 시 Trauma는 오른쪽, 탈출 경로는 왼쪽이어야 한다.");
            Assert.That(Mathf.Abs(restoredTrauma.transform.position.x - player.transform.position.x),
                Is.EqualTo(restoredChase.Director.Data.MaxDistance).Within(0.05f),
                "복원 event가 chase 시작보다 늦게 전달돼야 Trauma 안전거리가 적용된다.");

            var hud = Object.FindAnyObjectByType<StageHudPresenter>();
            Assert.That(health.CurrentHealth, Is.EqualTo(health.MaxHealth));
            Assert.That(StringTable.Get("hud.chase"), Does.Contain("왼쪽"),
                "재시작 직후 Trauma 쪽인 오른쪽으로 오인하지 않도록 탈출 방향을 명시해야 한다.");
            Assert.That(hud, Is.Not.Null);
            Assert.That(hud.HealthLabel,
                Is.EqualTo($"{StringTable.Get("hud.health")} {health.CurrentHealth}/{health.MaxHealth}"),
                "checkpoint 복귀 체력과 HUD 표기가 일치하지 않는다.");

            yield return new WaitForSeconds(0.75f);
            Assert.That(player.InputEnabled, Is.True, "restore grace 중 입력이 다시 잠겼다.");
            Assert.That(Mathf.Abs(player.transform.position.x - checkpoint.x), Is.LessThan(0.05f),
                "checkpoint 복원 직후 중력으로 y가 변해도 저장된 진행 위치의 x는 유지돼야 한다.");
            Assert.That(traumaContact.GrabInProgress, Is.False, "복귀 직후 Trauma가 즉시 재공격했다.");

            foreach (var encounter in Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None))
            {
                Assert.That(encounter.State, Is.EqualTo(EncounterState.Inactive));
                Assert.That(encounter.ActiveEnemies, Is.Empty,
                    "Chase checkpoint 복귀 위치가 encounter trigger와 겹쳐도 일반 몬스터를 spawn하면 안 된다.");
            }

            Assert.That(player.InputEnabled, Is.True, "재시도 후 Player 입력 잠금이 남아 있다.");
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
