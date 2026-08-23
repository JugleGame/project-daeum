using System.Collections;
using System.Linq;
using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Enemy;
using Daeume.Flow;
using Daeume.Memory;
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
            var echo = OverlaySceneLoader.FindOverlayRoot(director.Data.EchoOverlayScene);
            Assert.That(echo, Is.Not.Null, "Echo 오버레이 루트가 Stage02_Base 안에 없다.");
            Assert.That(echo.activeSelf, Is.False);

            yield return loader.ApplyRequest(director.Data.EchoOverlayScene, true);
            Assert.That(echo.activeSelf, Is.True);
            Assert.That(SceneManager.GetSceneByName("Stage02_Base").isLoaded, Is.True, "기저 공간은 닫히지 않는다.");
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore), "오버레이는 씬을 추가하지 않는다.");

            // 재시도(같은 Variant로 다시 진입)해도 같은 결과여야 한다.
            yield return loader.ApplyRequest(director.Data.EchoOverlayScene, true);
            Assert.That(echo.activeSelf, Is.True);
            Assert.That(SceneManager.sceneCount, Is.EqualTo(sceneCountBefore));

            yield return loader.ApplyRequest(director.Data.EchoOverlayScene, false);
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
