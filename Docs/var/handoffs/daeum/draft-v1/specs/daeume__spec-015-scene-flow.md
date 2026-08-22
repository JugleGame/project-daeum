+++
spec_id = "daeume__spec-015-scene-flow"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-023", "ARCH-011", "ARCH-004", "ARCH-033"]
dependencies = ["daeume__spec-007-stage-progression", "daeume__spec-009-acceptance-ending", "daeume__spec-011-checkpoint-save", "daeume__spec-013-ui-feedback", "daeume__spec-014-audio-presentation"]
+++

# 씬과 Stage 흐름

## Goal
Title에서 Stage 1~13, Ending, Credits와 Title 복귀까지 저장과 연출 순서를 누락 없이 실행하고, 엔딩 후 기억 열람을 제공한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: Title→Stage 1→결과→Title, New Game, Continue, 중복 전환 차단.
- 슬라이스에서 제외한다: Stage 2~13 연결, Ending, Credits, 기억 열람 진입.

## Implementation scope
- New Game은 Stage 1, Continue는 저장된 Stage와 Checkpoint를 불러온다.
- Contamination Overlay의 적재와 해제는 이 spec의 흐름 소유자만 요청한다. 다른 시스템이 직접 씬 API를 호출하지 않는다.
- Title 메뉴는 `EndingCompleted`가 참일 때만 기억 열람 진입을 표시한다. 열람은 `daeume__spec-005-memory-chest`의 재생 경로를 재사용하고 종료 시 Title로 복귀한다.
- 일반 흐름은 Stage Cleared→Stage Clear 연출→Save→Fade Out→Scene Load→StageData Load→Spawn→Fade In→Explore다.
- Stage 1~12는 순차 `NextStageId`를 사용하고 중복 전환 입력을 차단한다.
- Stage 13은 `AcceptanceCompleted→EndingCompleted Save→Ending→Credits→Title` 순서다.
- 엔딩 뒤에도 `EndingCompleted`를 보존한다.

## Out of scope
- Stage 선택, 다중 슬롯, DLC, 분기 Ending

## Acceptance criteria
- `Test_SceneFlow_NewGameLoadsStageOne`이 신규 시작을 확인한다.
- `Test_SceneFlow_ContinueLoadsCheckpoint`가 저장 복원을 확인한다.
- `Test_SceneFlow_StageClearOrder`가 일반 전환 9단계를 확인한다.
- `Test_SceneFlow_RejectsDuplicateTransition`이 추가 Load 0회를 확인한다.
- `Test_SceneFlow_AllStagesLinkToEnding`이 Stage 1~13 연결을 확인한다.
- `Test_SceneFlow_AcceptanceRoutesEndingCreditsTitle`이 결말 순서를 확인한다.
- `Test_SceneFlow_OverlayLoadRequestedByFlowOwnerOnly`가 흐름 소유자 외의 씬 API 호출 0건임을 확인한다.
- `Test_SceneFlow_GalleryHiddenBeforeEnding`이 `EndingCompleted` 거짓일 때 기억 열람 진입 0회를 확인한다.
- `Test_SceneFlow_GalleryReturnsToTitle`이 열람 종료 후 Title 복귀를 확인한다.

## Verification method
- EditMode 씬·StageData 연결성 테스트
- PlayMode 신규·이어하기·일반·엔딩 전환 named tests
