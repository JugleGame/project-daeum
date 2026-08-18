# B3 — Stage 1 Encounter와 Wave

## 목표

Stage 1에서 구간 진입, 근접형 Remnant Spawn, 출구 잠금, Wave 전멸과 해제를 데이터로 제어하고 지형 상호작용 한 종을 연결한다.

## 기준 요구사항

- Spec: `daeume__spec-012-combat-encounters` draft v3의 8일 슬라이스 범위
- Encounter 데이터: `EncounterId`, `TriggerArea`, `EnemyType`, `SpawnPoint`, `SpawnCount`, `WaveCount`, `ClearCondition`, `LockExit`, `TerrainHazardIds`
- 이번 세션의 실제 완료 조건은 `DefeatAll`이다.
- 진입 시 `Active`, 출구 잠금, Wave 1 Spawn을 실행한다.
- 현재 Wave 전멸 뒤 다음 Wave는 정확히 한 번 시작한다.
- 마지막 전멸 뒤 `Cleared`와 출구 해제를 실행한다.
- 완료 Encounter 재진입 시 Spawn은 0회다.
- 지형 요소는 Player와 Remnant에 같은 규칙으로 작용하며 사전 시각·음향 신호를 제공하고 단독 즉사를 만들지 않는다.

## 선행조건

- B1 marker와 Stage 1 데이터가 존재한다.
- B2 근접형 Remnant의 spawn/despawn 및 소멸 완료 판정이 안정적이다.
- 기존 `EncounterState`의 `Inactive`, `Active`, `Cleared`를 사용한다.

## 구현 범위

- Encounter 데이터 스키마와 Stage 1 Encounter asset
- Trigger 진입, Spawn, Wave 진행, 출구 잠금/해제
- 완료 상태의 재진입 방지
- Stage 1 지형 상호작용 한 종
- Encounter 상태 변경을 Role A flow와 후속 세션이 소비할 수 있는 명시적 contract/event로 제공
- test fixture에서 실제 Remnant 또는 책임이 같은 test double을 사용할 수 있게 경계를 분리

## 범위 제외

- `Survive`, `PassWithoutAggression`, `OptionalReactive`의 실제 Stage 콘텐츠
- 무작위 Wave, 적 AI 재구현, Trauma Spawn
- Stage 11/13 규칙

## 검증

- `Test_Encounter_EntryStartsFirstWaveAndLocksExit`
- `Test_Encounter_NextWaveAfterElimination`
- `Test_Encounter_ClearUnlocksExit`
- `Test_Encounter_ClearedDoesNotReactivate`
- 슬라이스 지형 요소의 양측 적용, 사전 신호, 단독 즉사 방지 focused tests
- PlayMode: Stage01 진입 → Wave → 전멸 → 다음 Wave/해제 → 재진입 Spawn 0회
- Compile/Console error 0

## 완료 조건

- 상태 전이와 Spawn 횟수가 데이터와 일치한다.
- Encounter 완료 결과가 후속 flow에서 구독 가능하다.
- 지형 상호작용이 Scene/Prefab 소유권을 침범하지 않는다.

## 세션 결과

- 상태: 미시작
- 커밋:
- 구현:
- 테스트:
- 수동 QA:
- contract 변경:
- 다음 세션 주의점:
