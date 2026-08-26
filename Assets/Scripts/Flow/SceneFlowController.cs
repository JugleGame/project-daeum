using System.Collections;
using Daeume.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Daeume.Flow
{
    /// <summary>씬 전환 단계가 하나 진행될 때마다 알리는 메시지. 연출(C)이 페이드 타이밍에 사용한다.</summary>
    public readonly struct FlowStepReached
    {
        public FlowStepReached(SceneFlowStep step) => Step = step;
        public SceneFlowStep Step { get; }
    }

    /// <summary>
    /// 오염 오버레이를 켜거나 꺼 달라는 요청. 실제 처리는 OverlaySceneLoader가 한다.
    /// SceneName은 <b>StageNN_Base 안의 오버레이 루트 오브젝트 이름</b>이다 — 별도 씬 이름이 아니다(#38).
    /// </summary>
    public readonly struct OverlaySceneLoadRequested
    {
        public OverlaySceneLoadRequested(string sceneName, bool load)
        {
            SceneName = sceneName ?? string.Empty;
            Load = load;
        }

        public string SceneName { get; }
        public bool Load { get; }
    }

    /// <summary>
    /// 게임 전체의 씬 흐름과 저장을 소유한다. (spec-015, spec-011)
    ///
    /// 스펙상 "씬 API를 직접 호출해도 되는 유일한 주체"가 이 클래스(와 그가 위임한 오버레이 로더)다.
    /// 다른 시스템이 마음대로 씬을 열고 닫으면 저장 시점과 순서가 어긋나 진행이 사라지기 때문이다.
    ///
    /// 배치 위치: Persistent 씬(Role A 소유). 게임 내내 하나만 존재한다.
    /// </summary>
    public sealed class SceneFlowController : MonoBehaviour
    {
        [SerializeField] private string titleScene = "Title";
        [SerializeField] private string stageOneScene = "Stage01_Base";
        [SerializeField, Min(1)] private int maxHealth = 3;

        /// <summary>스테이지 번호의 상한(spec-007). StageData.MaximumStageId와 같은 값이다.</summary>
        private const int MaximumStageId = 13;

        private readonly SceneFlowPlan plan = new();   // 순서·중복 방지 규칙
        private SaveSystem saveSystem;
        private SaveData currentData;

        public bool IsTransitioning => plan.IsTransitioning;
        public SaveData CurrentData => currentData;

        private void Awake()
        {
            // Application.persistentDataPath: OS가 정해 주는 "앱 전용 저장 폴더"다.
            // 프로젝트 폴더에 저장하면 빌드 후 권한 문제로 저장이 실패할 수 있어 반드시 이 경로를 쓴다.
            var path = System.IO.Path.Combine(Application.persistentDataPath, "daeume-save.json");
            var settingsPath = System.IO.Path.Combine(Application.persistentDataPath, "daeume-settings.json");

            // 진행 상황과 사용자 설정을 다른 파일로 나눈다(새 게임에도 접근성 설정이 유지되도록).
            saveSystem = new SaveSystem(new FileSaveStore(path), new FileSaveStore(settingsPath));
            currentData = saveSystem.Load(maxHealth).Data;

            GameManager.Instance?.Events.Subscribe<ChaseCheckpointRestoreRequested>(RestoreChaseCheckpoint);
            GameManager.Instance?.Events.Subscribe<StageFailed>(OnStageFailed);
            GameManager.Instance?.Events.Subscribe<PlayerFellOutOfBounds>(OnPlayerFellOutOfBounds);
        }

        private void OnDestroy()
        {
            // 구독 해제를 빼먹으면 씬을 다시 로드했을 때 죽은 객체가 이벤트를 받아 오류가 난다.
            GameManager.Instance?.Events.Unsubscribe<ChaseCheckpointRestoreRequested>(RestoreChaseCheckpoint);
            GameManager.Instance?.Events.Unsubscribe<StageFailed>(OnStageFailed);
            GameManager.Instance?.Events.Unsubscribe<PlayerFellOutOfBounds>(OnPlayerFellOutOfBounds);
        }

        private Coroutine pendingRetry;

        /// <summary>
        /// 실패 원인에 따라 뒷처리가 갈린다. (#12)
        /// </summary>
        /// <remarks>
        /// 트라우마에게 붙잡히면 게임 오버다 — 체크포인트로 되돌리지 않고 타이틀로 나간다.
        /// 되돌리던 시절에는 부활 지점이 탈출 경로 반대편일 때 추격자를 지나갈 방법이 없어
        /// 붙잡힘 → 복귀 → 붙잡힘이 무한 반복됐다. 체력 소진은 종전대로 체크포인트에서 재시도한다.
        /// </remarks>
        private void OnStageFailed(StageFailed value)
        {
            pendingRetry = value.Cause == StageFailureCause.TraumaGrabCompleted
                ? StartCoroutine(GameOverAfterDelay())
                : StartCoroutine(RetryAfterDelay());
        }

        /// <summary>붙잡힘 연출을 볼 시간을 준 뒤 타이틀로 내보낸다.</summary>
        private IEnumerator GameOverAfterDelay()
        {
            yield return new WaitForSeconds(1.2f);
            pendingRetry = null;
            ReturnToTitleAfterGameOver();
        }

        /// <summary>
        /// 게임 오버 처리. 진행 스테이지는 남기고 추격 체크포인트만 지운다.
        /// </summary>
        /// <remarks>
        /// CurrentStageId를 남기므로 "이어하기"는 붙잡힌 그 스테이지의 처음부터 다시 시작한다.
        /// 반대로 CheckpointId를 남기면 이어하기가 곧바로 추격 상태로 복귀해, 방금 죽은 그 자리에서
        /// 다시 시작하는 꼴이 된다 — 게임 오버로 만든 의미가 사라진다.
        /// </remarks>
        public bool ReturnToTitleAfterGameOver()
        {
            if (!plan.TryBeginTransition()) return false;

            currentData.CheckpointId = string.Empty;
            currentData.ContaminationVariantId = string.Empty;
            currentData.PressureStage = "Stable";
            currentData.PlayerPosition = Vector2.zero;
            currentData.PlayerHealth = maxHealth;
            saveSystem.Save(currentData, maxHealth);

            StartCoroutine(LoadTitleAfterGameOver());
            return true;
        }

        private IEnumerator LoadTitleAfterGameOver()
        {
            yield return ReplaceContent(titleScene);
            plan.CompleteTransition();
        }

        /// <summary>실패 후 잠깐 기다렸다가 재시작한다. 실패 화면을 볼 시간을 주기 위한 지연이다.</summary>
        private IEnumerator RetryAfterDelay()
        {
            yield return new WaitForSeconds(1.2f);
            pendingRetry = null;
            RetryFromFailure();
        }

        /// <summary>실패 지점에서 다시 시작한다. 체크포인트가 있으면 추격 상태로, 없으면 탐색부터.</summary>
        public bool RetryFromFailure()
        {
            if (!plan.TryBeginTransition())
            {
                return false;
            }

            currentData = saveSystem.Load(maxHealth).Data;

            // 사망 복귀는 저장된 체력이 아니라 체크포인트가 선언한 부활 체력을 쓴다(spec-011).
            // 체력 1로 되살아나 즉시 다시 죽는 무한 루프를 막기 위한 규칙이다.
            currentData.PlayerHealth = SaveSystem.ResolveRespawnHealth(currentData, maxHealth, true, maxHealth);

            // 추격 체크포인트가 있으면 회상을 다시 보여 주지 않고 곧바로 추격 상태로 복귀한다.
            var state = string.IsNullOrEmpty(currentData.CheckpointId) ? StageState.Explore : StageState.Chase;
            StartCoroutine(LoadContent(SceneForStage(currentData.CurrentStageId), state, currentData));
            return true;
        }

        public bool StartNewGame()
        {
            if (!plan.TryBeginTransition())
            {
                return false;
            }

            // CreateNewGame은 진행만 초기화하고 접근성 설정은 그대로 물려준다.
            currentData = saveSystem.CreateNewGame(maxHealth);
            saveSystem.Save(currentData, maxHealth);
            StartCoroutine(LoadContent(stageOneScene, StageState.Explore, currentData));
            return true;
        }

        public bool ContinueGame()
        {
            if (!plan.TryBeginTransition())
            {
                return false;
            }

            currentData = saveSystem.Load(maxHealth).Data;
            StartCoroutine(LoadContent(SceneForStage(currentData.CurrentStageId), StageState.Explore, currentData));
            return true;
        }

        /// <summary>탈출 지점 도달 시 호출된다. 추격 중일 때만 성립한다.</summary>
        public bool CompleteStageOne()
        {
            if (GameManager.Instance == null || GameManager.Instance.StageState != StageState.Chase ||
                !plan.TryBeginTransition())
            {
                return false;
            }

            StartCoroutine(AdvanceAfterStageClear());
            return true;
        }

        /// <summary>추격 시작 시점의 상태를 체크포인트로 저장한다. (spec-011)</summary>
        public void SaveChaseCheckpoint(string checkpointId, Vector2 playerPosition, int playerHealth, string variantId)
        {
            currentData.CheckpointId = checkpointId ?? string.Empty;
            currentData.PlayerPosition = playerPosition;
            currentData.PlayerHealth = Mathf.Clamp(playerHealth, 1, maxHealth);

            // 어떤 오염 Variant였는지도 저장한다. 재시도 때 같은 공간·타이밍을 재현하기 위한 값이다.
            currentData.ContaminationVariantId = variantId ?? string.Empty;
            saveSystem.Save(currentData, maxHealth);
        }

        /// <summary>Stage13의 무벌점 loop 진행과 영구 무장 해제를 즉시 저장한다.</summary>
        public void SaveStageThirteenProgress(int loopCount, bool weaponLowered)
        {
            currentData.StageThirteenLoopCount = Mathf.Clamp(loopCount, 0, 4);
            currentData.WeaponLowered = weaponLowered;
            saveSystem.Save(currentData, maxHealth);
        }

        /// <summary>수용 엔딩을 저장한 뒤 기존 content 교체 경로로 Title에 복귀한다.</summary>
        public bool CompleteEnding()
        {
            if (!plan.TryBeginTransition()) return false;
            currentData.EndingCompleted = true;
            currentData.CheckpointId = string.Empty;
            currentData.ContaminationVariantId = string.Empty;
            currentData.PressureStage = "Stable";
            saveSystem.Save(currentData, maxHealth);
            StartCoroutine(LoadTitleAfterGameOver());
            return true;
        }

        /// <summary>
        /// 접근성 설정을 저장한다. (spec-013)
        /// 설정은 SaveSystem이 진행 슬롯과 별개 파일로 관리하므로, 새 게임을 시작하기 전(타이틀)에도
        /// 안전하게 호출할 수 있다 — 진행 데이터는 그대로 두고 설정 파일만 갱신된다.
        /// </summary>
        public void SaveAssistSettings(AssistSettings settings)
        {
            currentData.AssistSettings = (settings ?? new AssistSettings()).Copy();
            saveSystem.Save(currentData, maxHealth);
        }

        /// <summary>오버레이 적재/해제를 "요청"만 한다. 실제 씬 조작은 로더가 순서대로 처리한다.</summary>
        public void RequestOverlay(string sceneName, bool load)
        {
            GameManager.Instance?.Events.Publish(new OverlaySceneLoadRequested(sceneName, load));
        }

        /// <summary>
        /// 스테이지 클리어 → 저장 → 다음 스테이지(없으면 타이틀)까지의 정해진 순서를 실행한다.
        /// </summary>
        /// <remarks>
        /// 다음 스테이지 콘텐츠가 아직 없으면 진행도를 올리지 않고 타이틀로 보낸다.
        /// 올려 버리면 "이어하기"가 존재하지 않는 스테이지를 가리켜 곧바로 타이틀로 튕기는,
        /// 저장 파일만 망가진 상태가 된다.
        /// </remarks>
        private IEnumerator AdvanceAfterStageClear()
        {
            var nextStageId = currentData.CurrentStageId + 1;
            var nextScene = PlayableStageScene(nextStageId);
            var hasNextStage = !string.IsNullOrEmpty(nextScene);

            foreach (var step in plan.GetStageClearOrder())
            {
                // 각 단계 진입을 알린다. 연출(C)은 이 신호로 페이드와 클리어 화면을 맞춘다.
                GameManager.Instance?.Events.Publish(new FlowStepReached(step));
                switch (step)
                {
                    case SceneFlowStep.StageCleared:
                        GameManager.Instance?.SetStageState(StageState.Cleared);
                        if (hasNextStage) EnterStage(nextStageId);
                        break;
                    case SceneFlowStep.Save:
                        // 저장은 씬 교체(SceneLoad)보다 항상 먼저다. 순서가 뒤바뀌면 진행이 날아간다.
                        saveSystem.Save(currentData, maxHealth);
                        break;
                    case SceneFlowStep.SceneLoad:
                        yield return ReplaceContent(hasNextStage ? nextScene : titleScene);
                        break;
                    case SceneFlowStep.Explore:
                        // 씬이 올라온 뒤에 복원을 요청한다(LoadContent와 같은 순서 규칙).
                        if (hasNextStage)
                        {
                            GameManager.Instance?.Events.Publish(
                                new PlayerRestoreRequested(currentData.PlayerPosition, currentData.PlayerHealth));
                        }

                        GameManager.Instance?.ResetStage();
                        break;
                    default:
                        // 아직 구현되지 않은 연출 단계(페이드 등)는 한 프레임만 넘긴다.
                        // 단계 자체는 순서대로 통지되므로, 나중에 연출을 붙일 때 이 자리만 채우면 된다.
                        yield return null;
                        break;
                }
            }

            plan.CompleteTransition();
        }

        private IEnumerator LoadContent(string sceneName, StageState state, SaveData data)
        {
            yield return ReplaceContent(sceneName);

            // 씬이 올라온 뒤에 플레이어 복원을 요청해야 한다. 순서가 반대면 아직 없는 지형 위에 놓인다.
            GameManager.Instance?.Events.Publish(new PlayerRestoreRequested(data.PlayerPosition, data.PlayerHealth));
            GameManager.Instance?.ResetStage(state);
            plan.CompleteTransition();
        }

        /// <summary>추격 중 사망 후 같은 지점·같은 오염 공간으로 되돌린다. (spec-011)</summary>
        private void RestoreChaseCheckpoint(ChaseCheckpointRestoreRequested request)
        {
            if (plan.IsTransitioning)
            {
                // 이미 다른 전환이 진행 중이면 끼어들지 않는다. 두 복귀가 겹치면 위치가 뒤엉킨다.
                return;
            }

            currentData = saveSystem.Load(maxHealth).Data;
            if (!string.IsNullOrEmpty(request.CheckpointId) && currentData.CheckpointId != request.CheckpointId)
            {
                // 요청한 체크포인트와 저장된 체크포인트가 다르면 복귀하지 않는다.
                // 다른 스테이지의 체크포인트로 잘못 되돌아가는 것을 막는 검사다.
                return;
            }

            // 이 함수가 실패를 이미 제자리에서 처리했으므로, OnStageFailed가 예약해 둔 전체 씬 재로드
            // 재시도(RetryAfterDelay)는 취소한다. 취소하지 않으면 1.2초 뒤 Stage01_Base가 통째로 다시
            // 로드되면서 트라우마(추격자)가 씬에 저장된 원래 위치로 되돌아가 버린다. 그러면 방금 이
            // 함수가 ContaminationDirector를 통해 벌려 둔 안전 거리가 사라지고, 부활하자마자 다시 붙잡혀
            // 죽고 부활하는 무한 루프가 생긴다.
            if (pendingRetry != null)
            {
                StopCoroutine(pendingRetry);
                pendingRetry = null;
            }

            var health = SaveSystem.ResolveRespawnHealth(currentData, maxHealth, true, maxHealth);
            GameManager.Instance?.Events.Publish(new PlayerRestoreRequested(currentData.PlayerPosition, health));

            // 오염 공간도 같은 Variant로 다시 올린다. "재시도 결정성" 요구사항의 핵심이다.
            if (!string.IsNullOrEmpty(currentData.ContaminationVariantId))
            {
                RequestOverlay(currentData.ContaminationVariantId, true);
            }

            // 회상을 다시 재생하지 않고 곧바로 추격 상태로 되돌린다.
            GameManager.Instance?.ResetStage(StageState.Chase);
        }

        /// <summary>
        /// 낙사 구간(void) 접촉 처리. (#11)
        /// 스테이지를 실패시키지 않고, 체력도 깎지 않는다 — spec-001이 낙사로 인한 즉사를 금지하기
        /// 때문이다. 대신 SaveSystem.ResolveRespawnHealth(deathRestore: false)로 마지막 저장 위치·체력을
        /// 그대로 복원해 되돌린다(전체 씬 리로드도 하지 않는다. 사망 재시도보다 가벼운 경로다).
        /// </summary>
        private void OnPlayerFellOutOfBounds(PlayerFellOutOfBounds request)
        {
            var health = SaveSystem.ResolveRespawnHealth(currentData, maxHealth, false, 0);

            // 레벨이 낙사 복귀 지점(StageMarkerKind.FallRecovery)을 선언했으면 그쪽을 쓴다.
            // 저장된 위치는 추격 체크포인트에서만 갱신돼서, 탐색 구간에서는 계속 (0,0)이었다 —
            // 구덩이에 빠질 때마다 스테이지 시작점으로 튕겨 나갔다는 뜻이다.
            var position = request.RecoveryPosition ?? currentData.PlayerPosition;
            GameManager.Instance?.Events.Publish(new PlayerRestoreRequested(position, health));
        }

        /// <summary>내용 씬만 갈아 끼운다. Persistent와 Boot는 절대 내리지 않는다.</summary>
        /// <remarks>
        /// 올리려는 씬이 이미 떠 있어도 같이 내렸다가 다시 올린다. 예전에는 이미 로드된 씬을
        /// 건너뛰었는데, 그러면 같은 스테이지에서 죽어 RetryFromFailure가 같은 씬을 다시 요청했을 때
        /// 실제로는 아무것도 다시 올라오지 않아 씬이 죽기 직전 상태 그대로 남았다 —
        /// 이미 처치한 적(EncounterController가 스폰한 Remnant)은 죽은 채로, 출구 잠금과 Wave 번호도
        /// 그대로 유지됐다. 재시도는 스테이지를 처음 상태로 되돌려야 하므로 항상 다시 올린다.
        ///
        /// 플레이어와 카메라는 Persistent 씬에 있어 이 재로드에 흔들리지 않는다.
        /// 복귀 위치는 호출한 쪽(LoadContent)이 씬을 올린 뒤 PlayerRestoreRequested로 정한다.
        /// </remarks>
        private IEnumerator ReplaceContent(string sceneName)
        {
            // 뒤에서부터 순회하는 이유: 씬을 내리면 목록의 인덱스가 당겨져,
            // 앞에서부터 돌면 일부 씬을 건너뛰게 된다. 역순 순회는 이런 실수를 피하는 표준 방식이다.
            for (var index = SceneManager.sceneCount - 1; index >= 0; index--)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.name == "Persistent" || scene.name == "Boot")
                {
                    continue;
                }

                yield return SceneManager.UnloadSceneAsync(scene);
            }

            yield return SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

            var loaded = SceneManager.GetSceneByName(sceneName);
            if (loaded.IsValid())
            {
                SceneManager.SetActiveScene(loaded);
            }
        }

        /// <summary>
        /// 다음 스테이지로 진행할 때 저장 데이터에서 "이전 스테이지에만 의미가 있던 값"을 털어 낸다.
        /// 새 게임 시작(SaveSystem.CreateNewGame)과 같은 출발 조건으로 맞추는 것이 목적이다.
        /// 특히 CheckpointId를 남기면 다음 스테이지에 들어가자마자 추격 상태로 복귀해 버린다.
        /// </summary>
        private void EnterStage(int stageId)
        {
            currentData.CurrentStageId = stageId;
            currentData.CheckpointId = string.Empty;
            currentData.ContaminationVariantId = string.Empty;
            currentData.PressureStage = "Stable";
            currentData.PlayerPosition = Vector2.zero;
            currentData.PlayerHealth = maxHealth;
        }

        /// <summary>
        /// 스테이지 번호 → 씬 이름. 빌드에 아직 없는 스테이지면 빈 문자열을 돌려준다.
        /// </summary>
        /// <remarks>
        /// 씬 이름 규칙("Stage01_Base")으로 푸는 이유: StageData(NextStageId)를 읽으려면 Daeume.Flow가
        /// Daeume.Stage를 참조해야 하는데, Stage → ContaminationRuntime → Flow 참조가 이미 있어서
        /// 어셈블리 순환이 된다. 규칙은 Stage 1~13 전부 동일하므로 여기서는 규칙으로 충분하다.
        ///
        /// 이름 규칙만 계산하고 씬이 실제로 있는지는 보지 않는다(순수 함수라 테스트가 쉽다).
        /// 존재 확인은 PlayableStageScene이 한다.
        /// </remarks>
        public static string StageSceneName(int stageId)
        {
            return stageId < 1 || stageId > MaximumStageId ? string.Empty : $"Stage{stageId:00}_Base";
        }

        /// <summary>
        /// 실제로 열 수 있는 스테이지 씬 이름. 아직 만들지 않은 스테이지면 빈 문자열이다.
        /// </summary>
        /// <remarks>
        /// 존재 확인을 하지 않으면 아직 만들지 않은 스테이지로 넘어가려다 씬 로드가 실패한다.
        /// 확인은 빌드 설정 목록을 직접 훑는다(Application.CanStreamedLevelBeLoaded는 에디터에서
        /// 빌드 설정에 들어 있는 씬에도 false를 돌려준다).
        /// </remarks>
        public static string PlayableStageScene(int stageId)
        {
            var sceneName = StageSceneName(stageId);
            return !string.IsNullOrEmpty(sceneName) && IsSceneInBuild(sceneName) ? sceneName : string.Empty;
        }

        private static bool IsSceneInBuild(string sceneName)
        {
            for (var index = 0; index < SceneManager.sceneCountInBuildSettings; index++)
            {
                var path = SceneUtility.GetScenePathByBuildIndex(index);
                if (string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), sceneName, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private string SceneForStage(int stageId)
        {
            var sceneName = PlayableStageScene(stageId);
            return string.IsNullOrEmpty(sceneName) ? titleScene : sceneName;
        }
    }
}
