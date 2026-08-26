# Issue #66 Stage10 Text UI Functional Test Contract

## Identity

- Contract ID: `ISSUE-66-STAGE10-TEXT-UI`
- Issue: `#66`
- Feature: Stage10 진입 독백·목표·회상 텍스트 UI
- Source revision: `66-fix-stage10-text-ui`
- Owner: Codex

## Given

- `Persistent`와 `Stage10_Base`가 로드되고 `CurrentStageId = 10`이다.
- 게임 상태는 `Explore`, Stage10 회상은 미완료다.
- 공용 `Stage01_Presentation` 프리팹이 런타임에 생성된다.
- 자막 크기는 각각 0, 1, 2로 실행한다.

## When

1. Stage10에 진입한다.
2. 탐색 상태에서 HUD 목표를 확인한다.
3. 육교의 표지판 회상 앵커를 조사하고 세 문장을 순서대로 진행한다.
4. 1920×1080에서 자막 크기 3단계를 각각 적용한다.

## Then / Test Oracle

| Observable outcome | Expected value | Evidence |
|---|---|---|
| 문자열 키 | 독백·목표·프롬프트·제목·3문장 모두 빈 값이나 `[key]` 폴백 없음 | `Test_Stage10TextUi_AllRequiredKeysResolve` |
| 진입 독백 | Stage10 전용 2문장, 진입당 1회 | `Test_Stage10TextUi_ShowsOpeningObjectiveAndMemoryInOrder` |
| 탐색 목표 | Stage10 전용 목표 표시, Stage01 목표 미표시 | `Test_Stage10TextUi_ShowsOpeningObjectiveAndMemoryInOrder` |
| 회상 | 제목과 `.01 → .02 → .03` 순서 표시 | `Test_Stage10TextUi_ShowsOpeningObjectiveAndMemoryInOrder` |
| 레이아웃 | 자막 크기 0·1·2 모두 1920×1080 안에서 잘림 없음 | `Test_Stage10TextUi_SubtitleSizesStayInsideSafeArea` |

Forbidden side effects:

- Stage01·Stage13 문구 또는 표시 동작 변경
- Stage10 지형·전투·추격·조명·카메라 변경
- UI·씬에 표시 문자열 하드코딩

## Test selection

- EditMode focal: `Test_Stage10TextUi_AllRequiredKeysResolve`, `Test_Stage10TextUi_SubtitleSizesStayInsideSafeArea`
- PlayMode focal: `Test_Stage10TextUi_ShowsOpeningObjectiveAndMemoryInOrder`
- Regression: `Stage01BlockoutTests`, `Stage13AcceptanceTests`, UI/회상 PlayMode tests
- Additional gate: 1920×1080 Game View에서 자막 크기 3단계 화면 확인

## Execution record

- Compile: pending Unity Editor startup.
- Focal named tests: pending Unity Editor startup.
- PlayMode console smoke: pending Unity Editor startup.
- Regression: pending Unity Editor startup.
- Layout: pending Unity Editor startup.
- Final build: pending Unity Editor startup.
- QA status: `INCOMPLETE` — Unity Editor 실행 증거가 아직 없다.
