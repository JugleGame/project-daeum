using System.Collections;
using System.Linq;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Enemy;
using Daeume.Flow;
using Daeume.Memory;
using Daeume.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Daeume.Tests.PlayMode
{
    /// <summary>
    /// 이슈 #12: Stage 02가 실제로 이어지고, 그 안에서 전투 → 상자 → 추격이 흐르는지 확인한다.
    ///
    /// 진행 연결 코드 자체는 #11(PR #30)에서 들어갔지만, 그때는 Stage02_Base 씬이 없어서
    /// "다음 스테이지가 있을 때" 경로를 검증할 수 없었다. 씬이 생긴 지금 비로소 확인 가능한 항목이다.
    /// </summary>
    public sealed class Stage02FlowTests
    {
        [UnityTest]
        public IEnumerator Test_Progression_Stage01ClearAdvancesToStage02AndRaisesCurrentStageId()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.That(flow, Is.Not.Null, "Persistent 씬에 SceneFlowController가 없다.");

            // 새 게임에서 출발해야 CurrentStageId가 1이라는 전제가 보장된다(이전 실행의 저장 파일과 무관해진다).
            Assert.That(flow.StartNewGame(), Is.True);
            yield return WaitForScene("Stage01_Base");
            Assert.That(flow.CurrentData.CurrentStageId, Is.EqualTo(1));

            // spec-001: Cleared는 추격 중 지정 출구에서만 성립한다. 상태 전이 규칙 그대로 밟는다.
            GameManager.Instance.SetStageState(StageState.Memory);
            GameManager.Instance.SetStageState(StageState.Chase);
            Assert.That(flow.CompleteStageOne(), Is.True);

            yield return WaitForScene("Stage02_Base");
            Assert.That(flow.CurrentData.CurrentStageId, Is.EqualTo(2),
                "다음 스테이지 씬이 빌드에 있으면 진행도가 올라가야 한다.");

            // EnterStage는 새 게임과 같은 출발 조건으로 초기화한다 — 체크포인트가 남으면
            // Stage 02에 들어가자마자 추격 상태로 복귀해 버린다.
            Assert.That(flow.CurrentData.CheckpointId, Is.Empty);
            Assert.That(flow.CurrentData.ContaminationVariantId, Is.Empty);
        }

        [UnityTest]
        public IEnumerator Test_Stage02_CombatThenMemoryStartsTheChase()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage02_Base", LoadSceneMode.Additive);
            yield return null;

            // ---- 전투: 교실 구간을 실제로 활성화해 전멸까지 간다 ----
            var classroom = Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None)
                .Single(controller => controller.Data.EncounterId == "stage02.encounter.01");

            Assert.That(classroom.TryActivate(), Is.True);
            Assert.That(classroom.State, Is.EqualTo(EncounterState.Active));
            Assert.That(classroom.ActiveEnemies.Count, Is.EqualTo(classroom.Data.SpawnCount));
            yield return null;

            foreach (var enemy in classroom.ActiveEnemies.ToArray())
            {
                enemy.ApplyDamage(new DamageRequest(999));
            }

            yield return null;
            Assert.That(classroom.State, Is.EqualTo(EncounterState.Cleared));

            // ---- 상자: StagePresentationBootstrap이 stage02 마커 위에 Stage02 앵커를 놓아야 한다 ----
            var anchor = Object.FindAnyObjectByType<MemoryAnchor>();
            Assert.That(anchor, Is.Not.Null, "StagePresentationBootstrap이 Stage02 회상 앵커를 만들지 못했다.");
            Assert.That(anchor.StableId, Is.EqualTo("memory-stage02-fragment-01"));

            // ---- 추격: 회상 완료가 오염 전환과 추격 시작으로 이어진다 ----
            var memoryAdapter = Object.FindAnyObjectByType<MemoryCompletionAdapter>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            Assert.That(memoryAdapter, Is.Not.Null);
            Assert.That(director, Is.Not.Null);

            memoryAdapter.TriggerDebugMemoryComplete();
            yield return null;

            Assert.That(GameManager.Instance.StageState, Is.EqualTo(StageState.Chase));
            Assert.That(director.ChaseActive, Is.True);
            Assert.That(director.VariantId, Is.EqualTo("Stage02_Overlay_Intrusion"),
                "Stage 02는 자기 오염 Variant로 추격해야 한다(재시도 시 같은 공간이 나오는 근거).");
        }

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
        /// 회귀(#12): Stage 02에서 플레이어가 Kinematic으로 굳어 점프도 충돌도 죽었던 문제.
        ///
        /// StageVisualBootstrap이 "Stage01_Base"가 떠 있을 때만 플레이어를 Dynamic으로 켰다.
        /// Stage 02에서는 그 조건이 영영 참이 되지 않아 중력이 사라졌고, 접지가 안 되니 점프가
        /// 나가지 않았으며 충돌 반응도 없어 지형을 그대로 통과했다.
        ///
        /// Boot부터 실제 흐름(Continue)으로 들어가야 재현된다. 에디터에서 씬을 직접 열면
        /// StageVisualBootstrap이 없어 통과해 버린다.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_Stage02_PlayerKeepsDynamicPhysicsAndCanJump()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null;

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.StartNewGame(), Is.True);
            yield return WaitForScene("Stage01_Base");

            GameManager.Instance.SetStageState(StageState.Memory);
            GameManager.Instance.SetStageState(StageState.Chase);
            Assert.That(flow.CompleteStageOne(), Is.True);
            yield return WaitForScene("Stage02_Base");

            var player = Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Dynamic),
                "스테이지 안에서는 플레이어가 Dynamic이어야 중력·충돌이 산다.");

            for (var frame = 0; frame < 120 && !player.IsGrounded; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(player.IsGrounded, Is.True, "Stage 02 바닥에 착지해야 한다.");
            Assert.That(player.TryJump(), Is.True, "접지 상태에서 점프가 나가야 한다.");
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
            yield return SceneManager.LoadSceneAsync("Stage02_Base", LoadSceneMode.Additive);
            yield return null;

            var classroom = Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None)
                .Single(controller => controller.Data.EncounterId == "stage02.encounter.01");
            Assert.That(classroom.TryActivate(), Is.True);
            yield return null;

            var enemy = classroom.ActiveEnemies.First();
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

        /// <summary>
        /// spec-006: 오버레이는 원본 공간을 닫지 않는다. Stage 02는 그 오버레이를 별도 씬이 아니라
        /// 기저 씬 안의 루트 오브젝트로 들고 있으므로, 씬 수가 늘지 않는 것까지 함께 확인한다(#12).
        /// 같은 요청을 두 번 걸어도 결과가 같아야 재시도 시 같은 공간이 나온다.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_Contamination_Stage02VariantOverlaysBaseWithoutClosingIt()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage02_Base", LoadSceneMode.Additive);
            yield return null;

            var loader = Object.FindAnyObjectByType<OverlaySceneLoader>();
            var director = Object.FindAnyObjectByType<ContaminationDirector>();
            Assert.That(loader, Is.Not.Null);

            var sceneCountBefore = SceneManager.sceneCount;
            var echo = OverlaySceneLoader.FindOverlayRoot(director.Data.EchoOverlayName);
            Assert.That(echo, Is.Not.Null, "Echo 오버레이 루트가 Stage02_Base 안에 없다.");
            Assert.That(echo.activeSelf, Is.False);

            loader.ApplyRequest(director.Data.EchoOverlayName, true);
            Assert.That(echo.activeSelf, Is.True);
            Assert.That(SceneManager.GetSceneByName("Stage02_Base").isLoaded, Is.True, "기저 공간은 닫히지 않는다.");
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore), "오버레이는 씬을 추가하지 않는다.");

            // 재시도(같은 Variant로 다시 진입)해도 같은 결과여야 한다.
            loader.ApplyRequest(director.Data.EchoOverlayName, true);
            Assert.That(echo.activeSelf, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore));

            loader.ApplyRequest(director.Data.EchoOverlayName, false);
            Assert.That(echo.activeSelf, Is.False);
            Assert.That(SceneManager.GetSceneByName("Stage02_Base").isLoaded, Is.True);
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
