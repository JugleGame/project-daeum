# Issue #58 Stage13 Acceptance Ending Functional Test Contract

## Identity

- Contract ID: `ISSUE-58-STAGE13-ACCEPTANCE`
- Issue: `#58`
- Feature: Stage13 수용 엔딩 스테이지
- Source revision: `58-feat-stage13-acceptance-ending`, base `b982941`
- Owner: Codex

## Given

- Scene: `Assets/Scenes/Stage13_Base.unity`
- Initial state: `CurrentStageId = 13`, `StageThirteenLoopCount = 0`, `WeaponLowered = false`, `EndingCompleted = false`
- World: Stage01 지형·실아트 재사용, 적 0, 탈출구 0, 트라우마 1, 동일 벤치 loop return point 1
- Determinism controls: loop당 좌향 이동 누적 20~30초, loop count 최대 4, 직렬화된 거리 임계값 4개

## When

1. 플레이어가 좌향으로 계속 이동해 loop boundary를 네 번 완주한다.
2. 플레이어가 트라우마에게 접근해 `Collapse → Intrusion → Echo → Stable` 임계값을 순서대로 지난다.
3. `Stable` 거리에서 Interact 입력으로 무기를 내려놓는다.
4. 플레이어가 추가 이동해 트라우마와 접촉한다.

## Then / Test Oracle

| ID | Observable outcome | Expected value | Evidence |
|---|---|---|---|
| T1 | 도주 loop | 동일 return point, 피해·진행·저장 penalty 0 | `Test_Ending_RunAwayLoopsWithoutPunishment` |
| T2 | 4단계 hint | camera → directional music → monologue → trauma waits | `Test_Ending_HintEscalatesAcrossFourLoops`, `Test_Ending_TraumaStopsAtFourthLoop` |
| T3 | 방향 직접 지시 | 금지 문구 0건 | `Test_Ending_HintNeverStatesDirection` |
| T4 | 공격·접촉 | 공격 피해/경직 0, 접촉 실패 0 | `Test_Ending_AttackCannotResolveTrauma`, `Test_Ending_TraumaContactDoesNotFailStageThirteen` |
| T5 | 압박 역전 | `Collapse → Intrusion → Echo → Stable` | `Test_Ending_ApproachReversesContamination` |
| T6 | 무장 해제·마지막 걸음 | 1회 저장, 이후 공격 0, 추가 player movement 후만 완료 | `Test_Ending_PlayerLowersWeaponAndWalks` |
| T7 | 엔딩 | farewell → fade → `EndingCompleted = true` → Title | `Test_Ending_CompletesAfterFarewell` |
| T8 | scene/data | escape 0, encounter active 0, Stage13 data valid, build enabled | `Test_Stage13_SceneMatchesAcceptanceLayout`, `Test_Stage13_DataIsValid` |
| T9 | Stage13 초기 연출 | 첫 frame에 어두운 조명, 활성 Trauma, player와 24 units 이상 거리 | `Test_Stage13_StartsDarkWithActiveDistantTrauma` |

Forbidden side effects:

- 방향을 직접 지시하는 문구
- 트라우마 피해·경직·처치
- 도주 loop의 HP, stage progress, checkpoint penalty
- 자동 이동으로 마지막 걸음 대체
- 엔딩 전용 신규 art asset

## Test selection

- EditMode focal names: 위 T1~T8의 `Test_Ending_*`, `Test_Stage13_*`
- PlayMode focal names: `Test_Stage13_RuntimeRulesDisableFailureAndRequireManualFinalStep`
- Regression: `Test_Progression_AllStagesHaveRequiredNarrativeFields`, `Test_UI_NoHardcodedStrings`, Player combat/contact tests, scene smoke
- Additional gates: Stage13 scene layout, build settings, jump height assumption(6.5 / gravityScale 1), manual full flow 2회

## Execution record (2026-08-25)

- Compile: `errorCount=0`, production/test assemblies compiled in Unity 6000.5.5f1.
- EditMode focal + regression: `status=completed`, `testCount=11`, `failedCount=0`.
- PlayMode focal + regression: `status=completed`, `testCount=15`, `failedCount=0`.
- PlayMode console smoke: 10 seconds, `passed=true`, `errorCount=0`.
- Stage13 layout/data/build-setting gate: all Stage13 assertions passed. Repository-wide
  `inspect_project_layout` still reports pre-existing L2/L6 failures in unrelated files; this
  change adds no new structural FAIL (the new runtime/test files only receive L3 warnings).
- Final WebGL build: embedded `BUILD_RESULT` is `Succeeded`, `totalErrors=0`,
  `totalWarnings=362`, `outputPath=Builds/project-daeum`, duration 258 seconds. The MCP wrapper
  surfaced error code 4000 because it treats existing C#/Sentis shader warnings as a failed tool
  response even though Unity's BuildReport succeeded.
- Remaining evidence: two hands-on full-flow passes and Stage10 → Stage13 transition after the
  parallel Stage10 branch is integrated.
- QA status: `INCOMPLETE` until the two remaining integration/manual checks are recorded; all
  automatable Issue #58 gates currently pass.

## Debug verification (2026-08-25)

- Root cause: 비어 있는 Unity 직렬화 object reference에 `??=`를 사용해 `player`와 `trauma`가
  fake-null 상태로 남았고, Stage13 시작 즉시 chase를 시작해 초기 거리와 암전도 곧바로 사라졌다.
- Focal PlayMode: `Test_Stage13_StartsDarkWithActiveDistantTrauma`, `status=completed`,
  `testCount=1`, `failedCount=0`.
- Stage13 PlayMode regression: 6 tests, all `Passed`.
- Stage13 EditMode focal/regression: 11 tests, all `Passed`.
- Console smoke: Stage13 Play Mode 10 seconds, `errorCount=0`; Trauma active, distance `32.00`,
  light intensity `0.55`, light color `(0.16, 0.20, 0.31)`, chase inactive before runaway input.
- Final WebGL build: Unity log `Build Finished, Result: Success`, artifact
  `Builds/project-daeum`, duration `484.368s`, build error 0. Tool response만 300초 transport
  timeout이 발생했으며 Unity build와 artifact 생성은 정상 완료됐다.
- QA status: 자동화 gate는 `PASS`. Issue 계약의 수동 전체 흐름 2회와 Stage10 통합 완주는 여전히
  별도 수동 증거가 필요하다.

### Initial darkness render correction

- 추가 root cause: `StageVisualBootstrap`이 `StageSkyBackground`를 의도적으로 Sprite-Unlit으로
  유지해 Global Light가 지면과 prop에는 적용되지만 화면 대부분인 Sky에는 적용되지 않았다.
- 변경: Stage13의 거리 보간을 Global Light와 Sky sprite tint에 함께 적용한다.
- Render oracle: 64×36 camera frame의 상단 중앙 pixel grayscale `< 0.30`을 focal PlayMode test에
  추가했다. 수정 전 실측값은 약 `0.50`이었다.
- Focal PlayMode: 1 test `Passed`; Stage13 PlayMode regression 6 tests `Passed`;
  EditMode regression 11 tests `Passed`; 10-second console smoke `errorCount=0`.
- Final WebGL build: `Succeeded`, `totalErrors=0`, `totalWarnings=351`,
  output `Builds/project-daeum`, duration `298.406s`. 기존 warning 때문에 wrapper만 error 응답을
  반환했으며 Unity BuildReport는 성공이다.
