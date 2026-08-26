using System.Collections;
using System.Linq;
using Daeume.Core;
using Daeume.Encounter;
using Daeume.Enemy;
using Daeume.Flow;
using Daeume.Player;
using Daeume.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace Daeume.Tests.PlayMode
{
    /// <summary>
    /// 사망 후 재시도가 스테이지 씬을 실제로 다시 올리는지 확인한다.
    ///
    /// 회귀 배경: SceneFlowController.ReplaceContent가 "이미 로드된 씬"을 건너뛰는 바람에,
    /// 같은 스테이지에서 죽고 RetryFromFailure가 같은 씬을 다시 요청해도 실제로는 아무것도
    /// 다시 올라오지 않았다. 그래서 씬 안의 모든 것이 죽기 직전 상태로 남았다 —
    /// EncounterController가 스폰한 적은 죽은 채로, 진행 중이던 Wave 번호와 출구 잠금도 그대로였다.
    ///
    /// 이 테스트는 적 스폰이 아니라 "씬이 새로 올라왔는가"를 본다. 적 초기화는 그 결과다.
    /// 스폰 지점 배선 같은 씬 내용에 의존하지 않아야 이 회귀만 정확히 잡아낸다.
    /// </summary>
    public sealed class StageRetryResetTests
    {
        [UnityTest]
        public IEnumerator Test_DeathRetry_ReloadsStageSceneSoSpawnedEnemiesAreGone()
        {
            // SceneFlowController는 Persistent 씬에 산다. Boot을 거치면 Persistent가 비동기로
            // 올라와 첫 프레임에는 아직 없다 — 다른 PlayMode 테스트와 같은 방식으로 직접 연다.
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;

            var flow = Object.FindAnyObjectByType<SceneFlowController>();
            Assert.That(flow, Is.Not.Null);
            Assert.That(flow.StartNewGame(), Is.True);
            yield return WaitForStage();

            // 죽기 전 씬에 있던 오브젝트를 붙잡아 둔다. 재로드되면 파괴돼 null이 된다.
            var encounterBeforeDeath = FindEncounter();
            var objectBeforeDeath = encounterBeforeDeath.gameObject;
            Assert.That(encounterBeforeDeath.State, Is.EqualTo(EncounterState.Inactive));

            Assert.That(GameManager.Instance.Fail(StageFailureCause.HealthDepleted), Is.True);

            // RetryAfterDelay가 1.2초를 기다린 뒤 씬을 갈아 끼운다.
            yield return WaitUntil(() => objectBeforeDeath == null, 600,
                "재시도해도 죽기 전 씬 오브젝트가 살아 있다. 씬이 다시 올라오지 않았다는 뜻이고, "
                + "그러면 처치했던 적도 죽은 채로 남는다.");
            yield return WaitForStage();

            var encounterAfterRetry = FindEncounter();
            Assert.That(encounterAfterRetry, Is.Not.SameAs(encounterBeforeDeath));
            Assert.That(encounterAfterRetry.State, Is.EqualTo(EncounterState.Inactive));
            Assert.That(encounterAfterRetry.ActiveEnemies.Count, Is.EqualTo(0));
            Assert.That(encounterAfterRetry.TotalSpawnCount, Is.EqualTo(0));
        }

        /// <summary>
        /// 세이브 포인트 복귀는 씬을 다시 올리지 않는 경로가 있다(낙사 복귀 등).
        /// 그때도 남아 있던 적을 지우고, 다시 진입하면 스폰 마커에서 처음부터 나와야 한다.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_CheckpointRestore_ClearsEnemiesAndRespawnsThemAtSpawnMarkers()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            // encounter.02만 스폰 마커 참조가 살아 있다(나머지 둘은 씬에서 끊겨 있다).
            var encounter = FindEncounterById("stage01.encounter.02");
            var marker = GameObject.Find("RemnantSpawnMarker_03");
            Assert.That(marker, Is.Not.Null, "이 테스트는 살아 있는 스폰 마커 하나에 기댄다.");

            Assert.That(encounter.TryActivate(), Is.True);
            yield return null;
            Assert.That(encounter.State, Is.EqualTo(EncounterState.Active));
            Assert.That(encounter.ActiveEnemies.Count, Is.GreaterThan(0));

            // 적을 스폰 지점에서 멀리 끌어다 놓는다. 복귀 후 "그 자리에 남아 있는지"를 보기 위함이다.
            var strayEnemy = encounter.ActiveEnemies.First().gameObject;
            strayEnemy.transform.position = new Vector3(-6f, 0f, 0f);

            // 세이브 포인트 복귀. 복귀 지점은 이 구간 밖이라 곧바로 다시 시작되지는 않아야 한다.
            GameManager.Instance.Events.Publish(new PlayerRestoreRequested(new Vector2(0f, 0f), 3));
            yield return null;

            Assert.That(strayEnemy == null, Is.True, "복귀했는데 기존 적이 살아남았다.");
            Assert.That(encounter.State, Is.EqualTo(EncounterState.Inactive));
            Assert.That(encounter.ActiveEnemies.Count, Is.EqualTo(0));
            Assert.That(encounter.CurrentWaveNumber, Is.EqualTo(0));
            Assert.That(encounter.TotalSpawnCount, Is.EqualTo(0));

            // 다시 진입하면 끌려다니던 자리가 아니라 스폰 마커에서 새로 나와야 한다.
            Assert.That(encounter.TryActivate(), Is.True);
            yield return null;
            Assert.That(encounter.ActiveEnemies.Count, Is.GreaterThan(0));
            foreach (var enemy in encounter.ActiveEnemies)
            {
                // y는 접지 보정이 지면 높이로 끌어내리므로 마커의 x로 비교한다.
                Assert.That(enemy.transform.position.x, Is.EqualTo(marker.transform.position.x).Within(0.01f),
                    "새 적은 끌려다니던 자리가 아니라 스폰 마커에서 나와야 한다.");
            }
        }

        /// <summary>
        /// 구덩이에 빠져 복귀할 때도 두 가지가 함께 성립해야 한다.
        /// (1) 복귀 지점은 레벨이 선언한 FallRecovery 마커다. (2) 남아 있던 적은 사라지고 구간이 초기화된다.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_FallingIntoVoid_RecoversAtMarkerAndClearsEnemies()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            StageMarker recovery = null;
            foreach (var marker in Object.FindObjectsByType<StageMarker>(FindObjectsSortMode.None))
            {
                if (marker.Kind == StageMarkerKind.FallRecovery) { recovery = marker; break; }
            }

            Assert.That(recovery, Is.Not.Null);

            // 복귀 마커가 허공에 있으면 복귀 → 낙사 → 복귀 루프가 된다. 반드시 지면 위여야 한다.
            var groundBelow = Physics2D.Raycast(recovery.transform.position, Vector2.down, 30f);
            Assert.That(groundBelow.collider, Is.Not.Null,
                "낙사 복귀 마커 아래에 지면이 없으면 복귀하자마자 다시 떨어진다.");

            var encounter = FindEncounterById("stage01.encounter.02");
            Assert.That(encounter.TryActivate(), Is.True);
            yield return null;
            var strayEnemy = encounter.ActiveEnemies.First().gameObject;

            var player = Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(10f, -15f);   // VoidZone(y -18..-12) 안
            body.linearVelocity = new Vector2(0f, -20f);
            Physics2D.SyncTransforms();

            for (var frame = 0; frame < 10 && body.position.y < -5f; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(Vector2.Distance(body.position, recovery.transform.position), Is.LessThan(0.05f),
                "낙사 복귀는 FallRecovery 마커 위치여야 한다.");

            // Destroy는 프레임 끝에 처리된다. 물리 스텝 루프에서 바로 빠져나온 참이라 한 프레임 넘긴다.
            yield return null;

            Assert.That(strayEnemy == null, Is.True, "복귀했는데 기존 적이 살아남았다.");
            Assert.That(encounter.State, Is.EqualTo(EncounterState.Inactive));
            Assert.That(encounter.ActiveEnemies.Count, Is.EqualTo(0));
        }

        /// <summary>
        /// 잔재는 중력을 받지 않고 x만 움직인다. 낭떠러지 검사가 없으면 바닥이 끊긴 구간 위를
        /// 그대로 떠서 건너 플레이어를 쫓아온다.
        /// </summary>
        /// <remarks>
        /// 잔재의 탐지 범위는 5라 구덩이(폭 9.5) 양 끝에 세우면 아예 움직이지 않는다.
        /// 그래서 플레이어를 구덩이 안 기둥(x 13.5~14.0) 위에 세워 잔재가 확실히 쫓아오게 만든 뒤,
        /// 오른쪽 지면 끝(x 15.0)을 넘어 허공으로 걸어 들어가는지를 본다.
        /// </remarks>
        [UnityTest]
        public IEnumerator Test_Remnant_StopsAtTheLedgeInsteadOfFloatingAcrossThePit()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            var encounter = FindEncounterById("stage01.encounter.02");
            Assert.That(encounter.TryActivate(), Is.True);
            yield return null;

            var remnant = encounter.ActiveEnemies.First();

            // 플레이어는 구덩이 속 기둥 위(윗면 y = -1.625), 잔재는 오른쪽 지면 왼쪽 끝에 세운다.
            var player = Object.FindAnyObjectByType<PlayerController>();
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(13.75f, -1.0f);
            body.linearVelocity = Vector2.zero;
            remnant.transform.position = new Vector3(15.4f, -0.49f, 0f);
            Physics2D.SyncTransforms();

            for (var frame = 0; frame < 200; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(remnant, Is.Not.Null, "잔재가 사라지면 이 테스트는 의미가 없다.");
            Assert.That(remnant.State, Is.Not.EqualTo(RemnantState.Idle),
                "잔재가 플레이어를 인식조차 못 했다면 낭떠러지 검사를 검증한 것이 아니다.");
            Assert.That(remnant.transform.position.x, Is.GreaterThan(15.0f),
                "잔재가 오른쪽 지면 끝(x 15.0)을 넘어 구덩이 위 허공으로 걸어 들어갔다.");
        }

        /// <summary>
        /// 잔재는 중력을 받지 않으므로 스폰 지점 높이 그대로 떠 있는다.
        /// Stage01의 스폰 마커는 지면보다 0.6 이상 높아, 보정이 없으면 40px 가량 떠 보였다.
        /// </summary>
        [UnityTest]
        public IEnumerator Test_Remnant_StandsOnTheGroundInsteadOfHoveringAtSpawnHeight()
        {
            SceneManager.LoadScene("Persistent", LoadSceneMode.Single);
            yield return null;
            yield return SceneManager.LoadSceneAsync("Stage01_Base", LoadSceneMode.Additive);
            yield return null;

            var encounter = FindEncounterById("stage01.encounter.02");
            Assert.That(encounter.TryActivate(), Is.True);
            yield return null;

            var remnant = encounter.ActiveEnemies.First();

            // 보정 후 위치가 아니라 마커가 선언한 높이를 봐야 한다. Tick이 이미 끌어내렸을 수 있다.
            var marker = GameObject.Find("RemnantSpawnMarker_03");
            Assert.That(marker, Is.Not.Null);
            var declaredSpawnY = marker.transform.position.y;

            for (var frame = 0; frame < 10; frame++)
            {
                yield return null;
            }

            // 잔재 몸통은 런타임에 트리거가 된다. 걸러내지 않으면 자기 콜라이더 안에서 출발한 레이가
            // 거리 0으로 자신을 맞고, 지면 대신 레이 시작점을 돌려준다.
            var groundY = float.NaN;
            foreach (var hit in Physics2D.RaycastAll(
                         new Vector2(remnant.transform.position.x, remnant.transform.position.y + 0.5f), Vector2.down, 5f))
            {
                if (hit.collider.isTrigger || hit.collider.transform.IsChildOf(remnant.transform)) continue;
                groundY = hit.point.y;
                break;
            }

            Assert.That(float.IsNaN(groundY), Is.False, "잔재 발밑에 지면이 있어야 이 검사가 성립한다.");

            // 마커 자체가 지면보다 충분히 높아야 이 회귀를 검증한 것이 된다(무효 테스트 방지).
            Assert.That(declaredSpawnY - groundY, Is.GreaterThan(0.3f),
                "스폰 마커가 이미 지면에 붙어 있으면 이 회귀를 검증할 수 없다.");

            // 스프라이트의 발바닥은 캔버스 바닥에서 8px 위에 있다(pivot은 9px).
            var visualBottom = remnant.GetComponentInChildren<SpriteRenderer>().bounds.min.y + 8f / 64f;
            Assert.That(visualBottom - groundY, Is.LessThan(0.05f).And.GreaterThan(-0.1f),
                "잔재의 발이 지면에 닿아야 한다. 떠 있거나 파묻히면 안 된다.");
        }

        private static EncounterController FindEncounterById(string encounterId) =>
            Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None)
                .Single(controller => controller.Data != null && controller.Data.EncounterId == encounterId);

        private static EncounterController FindEncounter() =>
            Object.FindObjectsByType<EncounterController>(FindObjectsSortMode.None)
                .Single(controller => controller.Data != null
                    && controller.Data.EncounterId == "stage01.encounter.01");

        private static IEnumerator WaitForStage() =>
            WaitUntil(() =>
            {
                var scene = SceneManager.GetSceneByName("Stage01_Base");
                return scene.IsValid() && scene.isLoaded;
            }, 600, "Stage01_Base did not load within 600 frames.");

        private static IEnumerator WaitUntil(System.Func<bool> condition, int frames, string message)
        {
            for (var frame = 0; frame < frames; frame++)
            {
                if (condition()) yield break;
                yield return null;
            }

            Assert.Fail(message);
        }
    }
}
