# Role A Stage 1 Functional Test Contract

## Identity

- Contract ID: `ROLE-A-STAGE1`
- Issue: `#1`
- Feature: Role A Stage 1 핵심 시스템
- Acceptance Criterion: 상태·이동·붙잡기·전투·상호작용·저장·씬 흐름이 Role B/C 소유권을 침범하지 않고 관찰 가능한 결과를 낸다.
- Source revision: `1-feat-role-a-core-systems` worktree, base `9d28416`
- Owner: Codex / Role A

## Test selection

- Focal mode: EditMode + PlayMode
- EditMode focal names:
  - `Test_StageLoop_ExploreMemoryChaseClear`
  - `Test_StageLoop_EncounterDoesNotReplaceExplore`
  - `Test_StageLoop_ContaminationDoesNotReplaceStageState`
  - `Test_StageLoop_ExitLockedBeforeChase`
  - `Test_StageLoop_FailedOnlyFromDeclaredCauses`
  - `Test_Save_FirstRunStartsStageOne`
  - `Test_Save_MemoryNeverDuplicates`
  - `Test_Save_ChaseDeathSkipsReplayAndKeepsVariant`
  - `Test_Save_RespawnHealthUsesCheckpointPolicy`
  - `Test_Save_UnclearedEncounterRestartsAllWaves`
  - `Test_Save_ClearedEncounterStaysCleared`
  - `Test_Save_GrabFailureUsesChaseCheckpoint`
  - `Test_Save_AssistSettingsSurviveNewGame`
  - `Test_Save_StoresStableIdsOnly`
  - `Test_Save_CorruptDataReturnsExplicitRecovery`
  - `Test_SceneFlow_NewGameLoadsStageOne`
  - `Test_SceneFlow_ContinueLoadsCheckpoint`
  - `Test_SceneFlow_StageClearOrder`
  - `Test_SceneFlow_RejectsDuplicateTransition`
- PlayMode focal names:
  - `Test_Player_MoveBothDirections`, `Test_Player_NoDoubleJump`
  - `Test_Movement_SameRulesDuringChase`, `Test_Movement_ContaminationNeverReversesInput`
  - `Test_Movement_GrabAttachesOnlyToGrabbable`, `Test_Movement_GrabAllowsOnlyDeclaredExits`
  - `Test_Movement_GrabDoesNotBlockDamage`, `Test_Movement_InputBoundToActionNames`
  - `Test_Combat_AttackDamagesRemnant`, `Test_Combat_InvulnerabilityPreventsRapidHits`
  - `Test_Combat_TraumaAttackHasNoEffect`, `Test_Combat_ZeroHealthTriggersFailure`
  - `Test_Combat_TraumaContactDealsNoDamage`, `Test_Combat_TraumaContactStartsGrabThenFails`
  - `Test_Combat_GrabSequenceIsDeterministic`, `Test_Combat_AggressionSetOnlyOnHit`
  - `Test_Interaction_ClosestValidTargetSelected`, `Test_Interaction_PromptOnlyInRange`
  - `Test_Interaction_DisabledDuringMemoryOrFailure`, `Test_Interaction_InvokesOnce`
  - `Test_Interaction_PromptCarriesActionAndKey`
- Regression: 전체 Role A EditMode assembly 22건 재실행
- Additional gates: Role A scene layout 3건, Boot runtime console smoke 1건, Git diff ownership 검사

## Given

- Scene or prefab: `Boot.unity`, `Persistent.unity`, 독립 physics fixture
- Initial state: `StageState.Explore`, HP 3, 저장 없음 또는 테스트별 stable ID 저장 데이터
- Test data or fixture: in-memory `ISaveStore`, fake Remnant `IDamageable`, fake `IInteractable`, `GrabbableSurface`
- Determinism controls: 명시적 physics frame, 명시적 damage clock, 고정 `TraumaGrabSeconds`, frame limit 180

## When

1. 입력 action 또는 public gameplay contract를 테스트별 정확히 1회 호출한다.
2. physics/coroutine 기능은 선언된 frame 또는 duration까지만 진행한다.
3. `Boot` scene을 load하고 `Persistent`와 `Title`이 load될 때까지 최대 180 frame 기다린다.

## Then / Test Oracle

| ID | Observable outcome | Expected value or state | Tolerance or deadline | Evidence field |
|---|---|---|---|---|
| T1 | Stage 1 상태 순서 | `Explore → Memory → Chase → Cleared` | 동기 호출 | NUnit assertion |
| T2 | 이동·붙잡기 | 양방향 속도, 공중 2단 점프 0회, 비 Grabbable 부착 0회 | 1 physics frame | PlayMode result |
| T3 | 전투·트라우마 | Remnant damage 1회, Trauma damage 0, 접촉 후 `Failed` | 1.05초 | PlayMode result |
| T4 | 상호작용 | 최근접 유효 대상 1개, 입력당 invoke 1회 | 즉시 | PlayMode result |
| T5 | 저장·복원 | stable ID와 Variant 유지, HP policy 적용, 손상 복구 결과 명시 | 동기 호출 | EditMode result |
| T6 | runtime scene | `Persistent`와 `Title` load, unexpected Console log 0 | 180 frame | scene smoke result |

Forbidden side effects:

- Role B/C 소유 scene 또는 prefab 변경
- Trauma damage/knockback, duplicate transition, duplicate memory, 하드코딩 prompt 문자열

## Execution record

### Compile

- Unity 6000.5.5f1 batchmode compile: production compile error 0

### Focal named tests

- Call: Unity Test Framework batchmode, dedicated `Daeume.Tests.EditMode` / `Daeume.Tests.PlayMode` assemblies
- `status`: `completed`
- `requested`: 위 focal name 전체
- `testCount`: EditMode 22, PlayMode 22
- `failedCount`: 0, 0
- `results`: 모든 요청 test `Passed`; XML은 ignored `Logs/editmode-regression-final.xml`, `Logs/playmode-final2.xml`
- `failures`: `[]`

### PlayMode console smoke

- Call: `Test_Runtime_BootPersistentTitle_NoConsoleErrors`
- `passed`: `true`
- `errorCount`: 0
- `errors`: `[]`

### Regression and additional gates

- Regression results: EditMode 22/22 Passed
- Additional gate results: Boot/Persistent/Input layout 3/3 Passed; Role B/C scene diff 0건

### Final build

- Call: `Daeume.Editor.RoleASceneSetup.BuildWindows`
- Target: `StandaloneWindows64` development build
- Artifact path: `Build/RoleA/Daeume.exe`
- `totalErrors`: 0

## Decision

- QA status: `PASS`
- Evidence-based reason: Role A 범위의 compile, focal functional, runtime console smoke, regression, layout, final build gate가 모두 통과했다.
- Retry count for the same failure: 1 (`DontDestroyOnLoad` hierarchy warning의 product 원인을 제거한 뒤 동일 smoke 재실행)
- Next action: Role B/C가 공개 event와 stable contract에 연결한 뒤 전체 Stage 1 integration QA를 수행한다.

## Completion checklist

- [x] 모든 계약 값이 채워졌다.
- [x] 모든 focal name이 실제 결과에 존재한다.
- [x] focal run은 1건 이상 실행했고 failure가 0이다.
- [x] PlayMode runtime smoke의 Console error가 0이다.
- [x] impacted regression과 layout gate를 실행했다.
- [x] 모든 선행 gate 뒤 final build를 실행했다.
- [x] QA status를 하나만 선택했다.
