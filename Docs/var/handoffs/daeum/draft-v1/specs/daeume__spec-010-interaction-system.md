+++
spec_id = "daeume__spec-010-interaction-system"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-006", "ARCH-016"]
dependencies = ["daeume__spec-001-core-loop", "daeume__spec-002-movement-platforming"]
+++

# 공통 상호작용

## Goal
상자, 문, 조사 오브젝트와 Stage 13 행동이 일관된 대상 선택과 실행 규칙을 사용하게 한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: `IInteractable` 계약, 대상 선택, 프롬프트 전달, 상태별 비활성화.
- 슬라이스에서 제외한다: Stage 13 `기억하기`와 `내려놓기`.

## Implementation scope
- 대상은 공통 `IInteractable` 계약의 `CanInteract`와 `Interact` 동작을 제공한다.
- 상호작용은 입력 액션 이름으로 바인딩한다. 붙잡기는 별개의 입력 액션이며 이 계약을 사용하지 않는다.
- 범위 안 가장 가까운 유효 대상 1개를 선택하고 동률은 바라보는 방향, 안정 ID 순으로 결정한다.
- 대상 프롬프트는 문자열이 아니라 입력 액션 이름과 문자열 테이블 키를 UI로 전달한다.
- `Memory`, `Failed`, `Cleared` 중 일반 상호작용을 비활성화한다.
- `MemoryInteractable`은 공통 기능을 유지하면서 Stage별 `PresentationPrefabId`, 동사, 완료 외형을 지원한다.
- 오염 Variant의 조사 오브젝트, Stage 13 `기억하기`와 `내려놓기`를 같은 계약으로 처리한다.

## Out of scope
- 개별 보상, 씬 로딩, 대사 분기
- 붙잡기/매달리기 (`daeume__spec-002-movement-platforming`가 소유)

## Acceptance criteria
- `Test_Interaction_ClosestValidTargetSelected`가 대상 1개 선택을 확인한다.
- `Test_Interaction_PromptOnlyInRange`가 범위 진입·이탈 표시를 확인한다.
- `Test_Interaction_DisabledDuringMemoryOrFailure`가 비허용 상태 실행 0회를 확인한다.
- `Test_Interaction_InvokesOnce`가 입력 1회당 실행 1회를 확인한다.
- `Test_Interaction_AcceptanceActionsUseCommonContract`가 마지막 2개 행동의 계약 준수를 확인한다.
- `Test_Interaction_MemoryPresentationDoesNotChangeCoreFlow`가 외형이 다른 매개체의 동일 이벤트 순서를 확인한다.
- `Test_Interaction_PromptCarriesActionAndKey`가 프롬프트 전달값에 하드코딩 문자열이 0개임을 확인한다.

## Verification method
- EditMode 우선순위 테스트
- PlayMode 상호작용 named tests
