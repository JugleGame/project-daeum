+++
spec_id = "daeume__spec-005-memory-chest"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-006", "ARCH-012", "ELEM-049"]
dependencies = ["daeume__spec-001-core-loop"]
+++

# MemoryInteractable과 회상

## Goal
장소마다 다른 기억의 매개체를 여는 선택이 행복한 기억, 기억 역류, ChaseCheckpoint와 추격을 하나의 흐름으로 시작하게 한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: 공통 `MemoryInteractable` 1개(Stage 1 버스표 보관함), 회상 재생과 건너뛰기, `Echo` 시작과 `ChaseCheckpoint` 활성화 요청.
- 슬라이스에서 제외한다: 나머지 12개 `PresentationPrefabId`, Stage 13 `기억하기` 프롬프트, 기억 열람 목록.

## Implementation scope
- 내부 공통 계약은 `MemoryInteractable`, 데이터 역할은 `MemoryAnchor`로 정의한다. 기존 spec ID와 파일명은 호환성을 위해 유지한다.
- 회상 자막과 프롬프트는 문자열을 직접 담지 않고 문자열 테이블 키를 참조한다.
- 최초 재생에서도 건너뛰기를 허용하며, 건너뛰기와 정상 종료는 동일한 후속 흐름을 요청한다.
- 엔딩 완료 후 기억 열람 목록이 동일한 회상 재생 경로를 재사용한다. 열람 재생은 `Echo` 시작과 `ChaseCheckpoint` 활성화를 요청하지 않는다.
- 각 Stage는 `PresentationPrefabId`와 `InteractionVerb`로 책상 서랍, 스피커, 제어함, 사진 슬롯 등 서로 다른 외형을 사용한다.
- 범위 내 상호작용으로 매개체를 1회 열거나 작동시키고 Stage의 기억 조각 ID를 저장한다.
- 회상 중 일반 이동·전투 피해를 중지하고 자막·이미지·연출 큐를 재생한다.
- 정상 종료와 건너뛰기 모두 기억 역류 `Echo` 시작과 `ChaseCheckpoint` 활성화를 요청한 뒤 추격을 시작한다.
- 획득한 매개체는 재시작·재실행 후 완료 상태이며 조각을 중복 지급하지 않는다.
- Stage 13 프롬프트는 `열기`가 아니라 `기억하기`를 사용한다.

## Out of scope
- 최종 대사, 갤러리, 저장 직렬화 구현
- 트라우마 이동과 공간 Variant

## Acceptance criteria
- `Test_MemoryInteractable_ActivatesOnce`가 작동과 지급 1회를 확인한다.
- `Test_MemoryInteractable_SupportsDistinctPresentation`이 13개 Stage의 PresentationPrefabId와 공통 회상 흐름을 확인한다.
- `Test_MemoryPlayback_DisablesCombatDamage`가 회상 중 피해 0회를 확인한다.
- `Test_MemoryPlayback_EndOrSkipStartsSameFlow`가 두 종료 경로에서 역류·체크포인트·추격 요청 1회를 확인한다.
- `Test_MemoryInteractable_CollectedStatePersists`가 재로드 후 완료와 조각 획득을 확인한다.
- `Test_MemoryInteractable_StageThirteenUsesRememberPrompt`가 마지막 프롬프트를 확인한다.
- `Test_MemoryGallery_ReplayDoesNotStartChase`가 열람 재생에서 역류 시작과 체크포인트 요청이 0회임을 확인한다.

## Verification method
- EditMode 수집 상태 테스트
- PlayMode 회상 흐름 named tests
