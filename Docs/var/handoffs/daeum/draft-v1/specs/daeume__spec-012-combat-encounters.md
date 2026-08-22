+++
spec_id = "daeume__spec-012-combat-encounters"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-028", "ARCH-015", "ARCH-033", "ELEM-051"]
dependencies = ["daeume__spec-003-player-combat", "daeume__spec-004-remnant-enemies"]
+++

# 전투 Encounter와 Wave

## Goal
구간 진입, 적 Spawn, 출구 잠금, Wave 전멸과 해제를 데이터로 제어하고, 지형 상호작용으로 같은 적 3종에서 전투 변주를 만든다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: Encounter 데이터, 진입·Spawn·잠금·해제, `DefeatAll`, Stage 1의 지형 상호작용 1종.
- 슬라이스에서 제외한다: `Survive`, `PassWithoutAggression`, `OptionalReactive`, Stage 13 밀도 규칙.

## Implementation scope
- 데이터는 `EncounterId`, `TriggerArea`, `EnemyType`, `SpawnPoint`, `SpawnCount`, `WaveCount`, `ClearCondition`, `LockExit`, `TerrainHazardIds`를 가진다.
- `TerrainHazardIds`는 전투 공간을 바꾸는 지형 요소를 선언한다. 최소 3종을 지원한다: 순차 폐쇄 셔터 문, 시간 경과로 무너지는 발판, 이동 플랫폼.
- 지형 요소는 Contamination Overlay가 소유한 오브젝트를 재사용하며 Encounter 전용 신규 에셋을 요구하지 않는다.
- 지형 요소는 플레이어와 잔재 모두에게 같은 규칙으로 작용하며 활성 전 시각·음향 신호를 제공한다.
- 지형 요소는 단독으로 플레이어를 즉사시키지 않는다.
- 진입 시 `Active`, 출구 잠금, Wave 1 Spawn을 실행한다.
- 현재 Wave 전멸 후 다음 Wave 1회, 마지막 전멸 후 `Cleared`와 출구 해제를 실행한다.
- 완료 Encounter 재진입은 Spawn 0회다.
- 완료 조건은 `DefeatAll`, `Survive`, `PassWithoutAggression`을 지원한다.
- Stage 11의 최소 1개 `OptionalReactive` 구간은 플레이어가 먼저 공격하면 기존 전투를 시작하고, 기다리면 잔재가 웅크리거나 길을 비켜 전투 없이 통과하게 한다. 어느 선택에도 엔딩·보상 패널티가 없다.
- `PassWithoutAggression`의 판정은 `daeume__spec-003-player-combat`의 `PlayerAggression`만 사용한다. 이 spec은 별도 판정 규칙을 정의하지 않는다.
- Stage 13은 전반보다 후반의 적 밀도가 낮고, 비선공 구간을 포함하며, 최종 기억 구간은 적이 없다. 정확한 수열은 계약이 아니다.

## Out of scope
- 적 AI, 무작위 Wave, 트라우마 Spawn

## Acceptance criteria
- `Test_Encounter_EntryStartsFirstWaveAndLocksExit`가 활성·Spawn·잠금을 확인한다.
- `Test_Encounter_NextWaveAfterElimination`이 다음 Wave 1회를 확인한다.
- `Test_Encounter_ClearUnlocksExit`가 완료·해제를 확인한다.
- `Test_Encounter_ClearedDoesNotReactivate`가 추가 Spawn 0회를 확인한다.
- `Test_Encounter_StageElevenSupportsNonAggressivePass`가 선제 공격 0회일 때 전투 없이 통과하고 패널티가 없음을 확인한다.
- `Test_Encounter_FinalStageSemanticDensityDecrease`가 후반 밀도 감소, 비선공 구간, 최종 적 0을 확인한다.
- `Test_Encounter_TerrainHazardAffectsBothSides`가 지형 요소가 플레이어와 잔재에 같은 규칙으로 작용함을 확인한다.
- `Test_Encounter_TerrainHazardSignalsBeforeActivation`이 활성 전 시각·음향 신호 각 1개를 확인한다.
- `Test_Encounter_TerrainHazardNeverKillsAlone`이 지형 요소 단독 즉사 0회를 확인한다.
- `Test_Encounter_AggressionJudgedByCombatSpec`이 이 spec에 별도 선제 공격 판정이 0건임을 확인한다.

## Verification method
- EditMode Encounter 데이터 검사
- PlayMode 다중 Wave named tests
