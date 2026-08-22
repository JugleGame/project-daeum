+++
spec_id = "daeume__spec-004-remnant-enemies"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-005", "ARCH-020", "ELEM-049", "ELEM-051"]
dependencies = ["daeume__spec-003-player-combat"]
+++

# 잔재 적과 파편 복선

## Goal
근접·돌진·원거리 잔재가 전투를 만들고, 압박 단계가 그 세 유형의 행동을 바꿔 중반 변주를 만들며, 행동과 외형으로 트라우마에서 떨어진 주인공의 기억 파편임을 점진적으로 암시한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: 근접형 1종, 공통 상태 6종, `Echo` 이상에서 트라우마를 바라보는 행동.
- 슬라이스에서 제외한다: 돌진형, 원거리형, Stage 9 모방, Stage 11 `Reactive`, 압박 단계별 행동 변화표 전체.

## Implementation scope
- 근접형, 예고 후 돌진형, 거리 유지 원거리형을 데이터로 제공한다.
- 공통 상태는 대기, 인지, 접근 또는 거리 유지, 공격, 피격, 소멸이다.
- `VisualTraitTags`는 행동과 분리하며 Stage 7부터 인간형을 암시하고, Stage 11에서 모든 유형에 `protagonist_hand`, `protagonist_face`, `protagonist_clothes` 중 1개 이상을 요구한다.
- Stage 9부터 일부 잔재가 주인공의 이동·공격 준비 동작을 모방한다.
- Stage 11의 `Reactive` 잔재는 플레이어가 먼저 공격하지 않으면 공격하지 않고 웅크리거나 길을 비켜 준다.
- `Echo` 이상에서 일부 잔재가 트라우마를 바라보고, `Intrusion` 이상에서 본체 방향으로 끌린다.
- 압박 단계는 새 적 유형을 만들지 않고 기존 3종의 선언된 수치와 행동을 바꾼다. `Stable` 기준 대비 `Echo`는 인지 거리, `Intrusion`은 돌진 예고 시간과 원거리형의 후퇴 방향을 바꾼다. 단계별 값은 적 데이터가 선언하며 코드 분기로 두지 않는다.
- 압박 단계에 따른 행동 변화는 예고 신호를 삭제하지 않는다. 예고 시간은 줄일 수 있으나 0이 될 수 없다.
- `Reactive` 잔재의 선제 공격 판정은 `daeume__spec-003-player-combat`의 `PlayerAggression`을 사용하며 별도 정의를 두지 않는다.
- 잔재 처치는 모든 일반 진행의 의무 조건이 아니며, Encounter 데이터가 `DefeatAll`, `Survive`, `PassWithoutAggression` 중 완료 조건을 선언할 수 있다.
- 소멸 파편은 Stage 7 이후 일정 비율로 트라우마 방향 흔적을 남긴다.

## Out of scope
- 절차 생성 적, 우호 캐릭터, 4번째 적 아키타입
- 트라우마의 이동과 추격 director (`daeume__spec-006-trauma-chase`가 소유)
- 고유 인간 애니메이션 대량 제작

## Acceptance criteria
- `Test_Remnant_ThreeArchetypesBehaveAsDeclared`가 3종 행동을 확인한다.
- `Test_Remnant_DeathDisablesDamage`가 소멸 후 피해 0회를 확인한다.
- `Test_Remnant_StageElevenAllContainProtagonistTrait`가 모든 Stage 11 유형의 필수 태그를 확인한다.
- `Test_Remnant_RespondsToTraumaPressure`가 단계별 바라봄·끌림 설정을 확인한다.
- `Test_Remnant_StageNineMirrorsPlayerMotion`이 Stage 9 모방 행동을 확인한다.
- `Test_Remnant_ReactiveWaitsForPlayerAggression`이 Stage 11 비선공 잔재의 선제 공격 0회와 공격받은 뒤 반응을 확인한다.
- `Test_Remnant_FragmentTracePointsToTrauma`가 후반 소멸 흔적 방향을 확인한다.
- `Test_Remnant_PressureChangesDeclaredValuesOnly`가 압박 단계 변화 시 적 유형 수가 3으로 유지되고 선언된 수치만 바뀜을 확인한다.
- `Test_Remnant_TelegraphNeverReachesZero`가 모든 압박 단계에서 돌진 예고 시간이 0보다 큼을 확인한다.

## Verification method
- EditMode 적 데이터·태그 검사
- PlayMode 유형별 행동 named tests
