+++
spec_id = "daeume__spec-013-ui-feedback"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-014", "ARCH-030", "ARCH-016", "ELEM-052"]
dependencies = ["daeume__spec-003-player-combat", "daeume__spec-005-memory-chest", "daeume__spec-010-interaction-system"]
+++

# UI, 피드백과 접근성 옵션

## Goal
HP, 상호작용, 기억, 자막, 실패·완료와 기억 역류의 공정한 경고를 과도한 HUD 없이 전달하고, 기준선 접근성 옵션을 제공한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: HP, 프롬프트, 기억 획득, 자막, Game Over, Stage Clear, 접근성 옵션 5종 전체, 문자열 테이블.
- 슬라이스에서 제외한다: Stage 13 프롬프트 3단계, 기억 열람 화면.
- 접근성 옵션은 슬라이스에서 뺄 수 없다. 공정성 신호를 저작하는 시점에 함께 만든다.

## Implementation scope
- HP, 상호작용 프롬프트, 기억 획득, 화자·자막, Game Over, Stage Clear를 제공한다.
- 프롬프트는 물리 키 문자열이 아니라 입력 액션 이름과 문자열 테이블 키로 구성하며, 현재 바인딩된 키 또는 버튼 표시를 액션에서 조회해 그린다.
- 모든 표시 문자열은 문자열 테이블에서 조회한다. UI와 연출 코드에 원고를 담지 않는다.
- 추격 중 일반 조사 프롬프트를 숨기고 생존 경고는 유지한다.
- 압박 단계 이름을 상시 표시하지 않고 문·플랫폼 변화의 시각 신호를 제공한다.
- 필수 정보를 색 단독으로 전달하지 않는다. 모든 필수 신호는 형태, 기호 또는 위치를 함께 사용한다.
- 다음 접근성 옵션을 제공한다: 컨트롤 리매핑, 카메라 흔들림 강도 0, 자막 크기 3단계, 추격 속도 저하 토글. 설정은 `daeume__spec-011-checkpoint-save`의 `AssistSettings`에 보존한다.
- 접근성 옵션 화면은 옵션 사용을 평가하는 문구를 표시하지 않는다.
- Stage 13에서 `열기→기억하기`, 공격 UI 제거, `내려놓기`를 순서대로 표시한다.
- 자막과 핵심 경고는 1920x1080 기준 안전 영역 안에 둔다.

## Out of scope
- 인벤토리, 상세 그래픽 설정, 최종 폰트 아트
- 번역 원고 (문자열 테이블 구조만 이 spec의 범위다)

## Acceptance criteria
- `Test_UI_HealthUpdatesAfterDamage`가 HP 일치를 확인한다.
- `Test_UI_InteractionPromptAppearsOnlyForTarget`이 대상 유무 표시를 확인한다.
- `Test_UI_MemoryAcquiredShownOnce`가 최초 표시 1회를 확인한다.
- `Test_UI_GameOverAndStageClearMatchState`가 상태별 결과 UI를 확인한다.
- `Test_UI_StageThirteenPromptSequence`가 3개 프롬프트 의미 변화를 확인한다.
- `Test_UI_PromptReflectsRebinding`이 리매핑 후 프롬프트 표시가 새 바인딩을 따름을 확인한다.
- `Test_UI_NoHardcodedStrings`가 UI 계층의 하드코딩 표시 문자열 0개를 확인한다.
- `Test_UI_NoColorOnlySignals`가 필수 신호에 형태·기호·위치 중 1개 이상이 함께 있음을 확인한다.
- `Test_UI_AccessibilityOptionsPresentAndPersist`가 옵션 5종의 존재와 재실행 후 유지를 확인한다.
- `Test_UI_ShakeZeroDisablesShake`가 흔들림 강도 0에서 카메라 흔들림 0회를 확인한다.
- `Test_UI_SubtitleScaleStaysInSafeArea`가 자막 크기 3단계 전부가 1920x1080 안전 영역 안에 있음을 확인한다.

## Verification method
- PlayMode UI named tests
- 1920x1080 레이아웃 검사
- 접근성 옵션 5종 사용자 검수
