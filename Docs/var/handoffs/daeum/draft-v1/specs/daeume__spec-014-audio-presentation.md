+++
spec_id = "daeume__spec-014-audio-presentation"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-017", "ARCH-013", "ELEM-051"]
dependencies = ["daeume__spec-001-core-loop", "daeume__spec-005-memory-chest", "daeume__spec-006-trauma-chase", "daeume__spec-012-combat-encounters"]
+++

# 오디오·카메라 프레젠테이션

## Goal
상태와 기억 역류를 음악, 생활음, 효과음과 카메라 압박으로 전달하고 Stage 13에서는 접근할수록 이를 감산한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: 상태별 큐 5종, Memory→Chase 6단계, 압박 3단계 프리셋, 흔들림 강도 0, 추격 카메라 선행 시야.
- 슬라이스에서 제외한다: Stage 12 총집합, Stage 13 감산.

## Implementation scope
- Explore Ambient, Encounter Combat, Memory, Chase, Cleared 큐를 제공한다.
- Memory→Chase는 마지막 대사→짧은 무음→환경음 정지→괴물 효과음→등장→Chase BGM 순서다.
- Stage의 `PrimaryContaminationChannels`에 Sound 또는 Camera가 포함될 때만 해당 채널을 전면 변화시키고, 포함되지 않은 채널은 공정성 신호와 상태 전환에 필요한 최소 수준만 사용한다.
- 추격 카메라는 진행 방향으로 선행 시야를 확보한다. 좌향 도주에서 플레이어 앞쪽 가시 거리를 `ChaseLookaheadUnits`로 선언하며, 이 값보다 가까운 곳에서 생존 경로 장애물이 처음 보이게 배치하지 않는다.
- 카메라 압박과 발걸음 프리셋이 소비하는 "트라우마 거리"는 `daeume__spec-006-trauma-chase`의 `ContaminationDirector`가 소유한 값이다. 이 spec은 거리를 계산하지 않는다.
- Stage 1~8의 전자음은 병원 장비 샘플을 직접 재생하지 않고 추상 리듬에서 의료적 느낌으로 단계적으로 이동한다.
- Stage 12는 이전 주요 사운드·카메라 모티프를 함께 사용하는 multi-channel 클라이맥스다.
- 트라우마 거리와 압박 단계로 발걸음, 카메라 시야 압박, 흔들림 프리셋을 선택한다.
- 흔들림은 월드 위치를 바꾸지 않고 강도 0을 지원한다.
- Stage 13 접근 시 음악 Stem, 속도와 카메라 압박을 단계별로 제거해 Stable 생활음으로 돌아간다.

## Out of scope
- 최종 작곡·성우·서라운드 믹싱

## Acceptance criteria
- `Test_Presentation_ExploreCombatTransition`이 상태별 BGM을 확인한다.
- `Test_Presentation_MemoryToChaseCueOrder`가 6단계 순서를 확인한다.
- `Test_Presentation_PressureMapsToPreset`이 4단계 프리셋을 확인한다.
- `Test_Presentation_OnlyPrimaryChannelsReceiveFullTreatment`가 Stage별 비주요 채널의 과잉 연출이 없음을 확인한다.
- `Test_Presentation_ChaseCameraLeadsMovementDirection`이 좌향 도주에서 선행 시야가 `ChaseLookaheadUnits` 이상임을 확인한다.
- `Test_Presentation_TraumaDistanceComesFromDirector`가 이 spec의 거리 계산 코드가 0건임을 확인한다.
- `Test_Presentation_StageTwelveCombinesPriorMotifs`가 Stage 12 클라이맥스 매핑을 확인한다.
- `Test_Presentation_ShakeDoesNotMovePlayerAndSupportsZero`가 위치 불변과 0 강도를 확인한다.
- `Test_Presentation_AcceptanceRemovesPressure`가 접근 단계별 감산을 확인한다.

## Verification method
- EditMode 큐·매핑 테스트
- Stage 1·13 PlayMode named tests와 사용자 검수
