+++
spec_id = "daeume__spec-001-core-loop"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-018", "ARCH-032", "GENRE-040"]
dependencies = []
+++

# 스테이지 핵심 루프

## Goal
Stage 1~12가 전진, 플랫폼, 잔재 전투, 기억, 기억 역류, 트라우마 추격, 탈출로 완결되고 Stage 13만 접근과 수용으로 전환되게 한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: `StageState` 5종, `EncounterState` 3종, `PressureStage` 중 `Stable`·`Echo`·`Intrusion`, Stage 1의 `Explore→Memory→Chase→Cleared` 순서와 `Failed` 진입.
- 슬라이스에서 제외한다: Stage 12 Truth 완료 조건, Stage 13 `AcceptanceCompleted`, `Collapse` 단계.

## Implementation scope
- `StageState`는 `Explore`, `Memory`, `Chase`, `Cleared`, `Failed`만 가진다.
- `Failed` 진입 조건은 두 가지뿐이다: 체력 0, 그리고 추격 중 트라우마 접촉에 따른 붙잡기 연출 완료. 판정 규칙은 `daeume__spec-003-player-combat`가 소유한다.
- 전투는 `EncounterState`의 `Inactive`, `Active`, `Cleared`로 분리하며 `Explore`를 대체하지 않는다.
- 기억 역류 압박 단계는 `Stable`, `Echo`, `Intrusion`, `Collapse`로 분리하며 Stage 상태를 대체하지 않는다.
- Stage 1~12는 회상 종료 후 `Chase`로 전환하고 지정 출구에서만 `Cleared`가 된다.
- Stage 12의 완료는 `Truth_DelayDidNotCauseLoss=Revealed`라는 사실 이해를 포함한다.
- Stage 13은 일반 출구 없이 `AcceptanceCompleted`가 완료 조건이다.
- Stage 13은 새 인과 정보를 공개하지 않고 Stage 12에서 이해한 사실을 감정적으로 수용한다.

## Out of scope
- 이동 수치, 공격 판정, 개별 오염 Variant 구현
- 기억 원고와 씬 로딩

## Acceptance criteria
- `Test_StageLoop_ExploreMemoryChaseClear`가 Stage 1~12의 상태 순서를 확인한다.
- `Test_StageLoop_EncounterDoesNotReplaceExplore`가 Encounter 활성 중 `StageState=Explore`를 확인한다.
- `Test_StageLoop_ContaminationDoesNotReplaceStageState`가 압박 단계 변경 중 Stage 상태가 유지됨을 확인한다.
- `Test_StageLoop_ExitLockedBeforeChase`가 추격 전 출구로 완료되지 않음을 확인한다.
- `Test_StageLoop_StageThirteenRequiresAcceptance`가 Stage 13에서 출구가 아닌 `AcceptanceCompleted`만 완료함을 확인한다.
- `Test_StageLoop_UnderstandingPrecedesAcceptance`가 Stage 12 Truth 공개 후에만 Stage 13 수용을 시작함을 확인한다.
- `Test_StageLoop_FailedOnlyFromDeclaredCauses`가 체력 0과 붙잡기 완료 이외의 경로로 `Failed`에 진입하지 않음을 확인한다.

## Verification method
- EditMode 상태 전이 테스트
- PlayMode Stage 1과 Stage 13 named tests
