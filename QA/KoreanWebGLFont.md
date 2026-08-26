# WebGL 한글 폰트 Functional Test Contract

## Identity

- Contract ID: `webgl-korean-font`
- Issue: 사용자 직접 요청(2026-08-26, Issue 없음)
- Feature: WebGL 한글 UI 렌더링
- Acceptance Criterion: WebGL에 포함된 font가 현대 한글 전체와 게임 UI 기호를 지원하고, 로드된 `Text`/`TextMesh`에 적용된다.
- Source revision: `main@0f4b03a` 기반 사용자 worktree
- Owner: Codex host agent

## Test selection

- Focal mode: PlayMode
- Focal test names:
  - `Daeume.Tests.PlayMode.KoreanFontTests.Test_KoreanFont_CoversKoreanUiAndAppliesToLoadedText`
  - `Daeume.Tests.PlayMode.Stage01BlockoutTests.Test_Stage01_TutorialHudShowsControlHints`
- Regression mode and names:
  - PlayMode - `Daeume.Tests.PlayMode.TitleMainScreenTests.Test_Title_MainScreenUsesFinalArtAndAccessibleMenu`
  - PlayMode - `Daeume.Tests.PlayMode.Stage10TextUiTests.Test_Stage10TextUi_ShowsOpeningObjectiveAndMemoryInOrder`
- Additional gates:
  - Title 화면 screenshot 및 WebGL 공개 URL HTTP 확인

## Given

- Scene or prefab: 테스트가 생성한 uGUI `Text`와 world-space `TextMesh`
- Initial state: 두 컴포넌트에 한글 font가 할당되지 않은 상태
- Test data or fixture: `Resources/Fonts/NanumGothic-Regular.ttf`
- Determinism controls: 동기식 resource load와 단일 `ApplyToLoadedText` 호출

## When

1. font의 U+AC00-U+D7A3 및 게임 UI 기호 지원 여부를 검사한다.
2. `KoreanFontBootstrap.ApplyToLoadedText()`를 한 번 호출한다.

## Then / Test Oracle

| ID | Observable outcome | Expected value or state | Tolerance or deadline | Evidence field |
|---|---|---|---|---|
| T1 | 한글 glyph coverage | 누락 0개 | 동기식 | `missing` assertion |
| T2 | uGUI font | 포함된 NanumGothic asset | 호출 직후 | `uiText.font` assertion |
| T3 | TextMesh font/material | 포함된 NanumGothic font/material | 호출 직후 | `worldText` assertions |

Forbidden side effects:

- font resource 누락 error, scene 외 asset 변경, 사용자 입력 또는 게임 상태 변경

## Execution record

- Compile: `KoreanFontBootstrap.cs` diagnostics 0; test script warning 1(`GetComponent` null-check 권고), compile error 0
- Focal named tests: `completed`, requested 1, testCount 1, failedCount 0, Passed, 0.001s
- PlayMode console smoke: Stage01_Base 5초 실행/정지, errorCount 0
- Regression and additional gates: requested 2, testCount 2, failedCount 0; Stage10 9.835s Passed, Title 1.161s Passed; Title Game view에서 한글 6개 UI 문구 육안 확인
- Final build: WebGL, `C:/Users/jyp/Desktop/daeum/Builds`, `Build Finished, Result: Success`, player data 40,071,445 bytes

## Decision

- QA status: INCOMPLETE
- Evidence-based reason: Stage01 runtime 생성 timing regression 수정 후 재검증 중이다.
- Retry count for the same failure: 1(asset import worker crash로 인한 최초 `INFRA_ERROR` 후 동일 focal test 재실행 성공)
- Next action: 사용자 지시에 따라 source만 push한다. WebGL final build와 재배포는 수행하지 않는다.
