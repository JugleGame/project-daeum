+++
spec_id = "daeume__spec-011-checkpoint-save"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-004", "ARCH-032", "ARCH-033"]
dependencies = ["daeume__spec-001-core-loop", "daeume__spec-005-memory-chest", "daeume__spec-012-combat-encounters"]
+++

# 체크포인트와 저장

## Goal
사망과 재실행 뒤 Stage, 기억, Encounter, 진실과 기억 역류 상태를 일관되게 복원한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: 최소 저장 필드, `ChaseCheckpoint`, `RespawnHealth`, 미완료 Encounter 리셋, 손상 저장 복구.
- 슬라이스에서 제외한다: `StageThirteenLoopCount`, `WeaponLowered`, `EndingCompleted` 소비처.

## Implementation scope
- 최소 저장 필드는 `CurrentStageId`, `CheckpointId`, `PlayerPosition`, `PlayerHealth`, `CompletedMemoryAnchors`, `CollectedMemoryFragments`, `DefeatedEncounterState`, `NarrativeRevealState`, `EndingCompleted`다. 이전 `OpenedMemoryChest` 데이터가 있으면 동일 Anchor 완료 상태로 1회 마이그레이션한다.
- 추가로 `ContaminationVariantId`, `PressureStage`, `StageThirteenLoopCount`, `WeaponLowered`, `AssistSettings`를 필요 구간에 저장한다.
- `StageThirteenLoopCount`는 bool이 아니라 정수다. `daeume__spec-009-acceptance-ending`의 힌트 4단계가 이 값을 소비한다.
- `AssistSettings`는 진행 상태가 아니라 사용자 설정이므로 저장 슬롯과 무관하게 보존하며 새 게임에서도 유지한다.
- 저장은 씬 참조나 로드된 오브젝트 핸들이 아니라 안정 ID만 담는다. 오염 공간은 활성 `ContaminationVariantId`만 저장하고 복원 시 오버레이를 다시 적재한다.
- 첫 기억 획득 후 `ChaseCheckpoint`를 저장하고 추격 사망 시 회상 없이 같은 Variant의 `Chase`로 복원한다. 붙잡기 연출로 인한 실패도 같은 경로를 사용한다.
- 일반 Checkpoint와 게임 재실행은 저장된 HP를 `1..MaxHealth`로 제한해 복원한다. 사망 복귀와 `ChaseCheckpoint`는 반복 실패로 인한 soft-lock을 막기 위해 해당 체크포인트가 선언한 `RespawnHealth`를 사용하며 기본값은 MaxHealth다.
- 미완료 Encounter는 첫 Wave 전체 리셋, 완료 Encounter는 영구 완료로 복원한다.
- 저장 없음은 Stage 1, 손상·미지원 버전은 명시적 복구 결과를 반환한다.

## Out of scope
- 클라우드 저장, 다중 슬롯, 임의 수동 저장

## Acceptance criteria
- `Test_Save_FirstRunStartsStageOne`이 초기 상태를 확인한다.
- `Test_Save_MemoryNeverDuplicates`가 조각 중복 0회를 확인한다.
- `Test_Save_ChaseDeathSkipsReplayAndKeepsVariant`가 회상 재생 0회와 Variant 복원을 확인한다.
- `Test_Save_RespawnHealthUsesCheckpointPolicy`가 사망 복귀 시 RespawnHealth, 정상 재실행 시 저장 HP를 확인한다.
- `Test_Save_UnclearedEncounterRestartsAllWaves`가 첫 Wave 리셋을 확인한다.
- `Test_Save_ClearedEncounterStaysCleared`가 재스폰·재잠금 0회를 확인한다.
- `Test_Save_EndingStatePersists`가 엔딩 관련 상태를 확인한다.
- `Test_Save_GrabFailureUsesChaseCheckpoint`가 붙잡기 실패 복원이 회상 재생 0회임을 확인한다.
- `Test_Save_LoopCountIsIntegerAndPersists`가 `StageThirteenLoopCount`의 정수 누적과 복원을 확인한다.
- `Test_Save_AssistSettingsSurviveNewGame`이 새 게임 시작 후에도 어시스트 설정이 유지됨을 확인한다.
- `Test_Save_StoresStableIdsOnly`가 저장 문서에 씬 참조와 오브젝트 핸들이 0개임을 확인한다.

## Verification method
- EditMode 직렬화·버전 테스트
- PlayMode 사망·재실행 named tests
