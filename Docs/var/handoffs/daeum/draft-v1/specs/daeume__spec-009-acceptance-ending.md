+++
spec_id = "daeume__spec-009-acceptance-ending"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ELEM-050", "ELEM-051", "ELEM-052", "ARCH-032"]
dependencies = ["daeume__spec-003-player-combat", "daeume__spec-006-trauma-chase", "daeume__spec-008-narrative-reveal"]
+++

# Stage 13 수용 엔딩

## Goal
도주와 공격을 무효화하고 플레이어가 트라우마 쪽으로 걷고 무기를 내려놓아 기억 역류를 직접 되돌리게 한다.

## Build scope
- 8일 슬라이스: **제외**. 전체가 이후 구현이다.

## Implementation scope
- Stage 13 전반은 일반 Encounter처럼 시작하고, 이후 적 밀도를 낮추며 비선공 잔재와 길을 비키는 마지막 잔재를 거쳐 최종 구간의 적을 0으로 만든다.
- 마지막 기억 뒤 일반 출구는 0개이며 반대 방향 도주는 20~30초 안에 같은 벤치로 반복된다. 검증은 `Test_Ending_RunAwayLoopsWithoutPunishment`가 수행한다.
- 도주 반복 횟수에 따라 힌트를 4단계로 강화한다. 1회차는 트라우마와 플레이어 사이 빈 길을 프레이밍한다. 2회차는 음악이 접근 방향에만 반응한다. 3회차는 주인공 독백 1줄을 재생한다. 4회차부터 트라우마가 추격을 멈추고 서서 기다린다.
- 힌트 4단계는 어느 단계에서도 이동 방향을 지시하는 문구를 표시하지 않는다.
- 반복 횟수는 `StageThirteenLoopCount`로 저장하며 `daeume__spec-011-checkpoint-save`가 보존한다.
- 도주 반복에는 피해, 진행도 손실, 저장 페널티가 없다.
- 공격은 피해·경직·진행을 만들지 않는다.
- Stage 13에서 트라우마 접촉은 붙잡기 연출과 실패를 발생시키지 않는다. 접촉해도 추격이 계속된다.
- 접근 거리로 압박 단계를 역전하고 음악·속도·카메라 압박을 감산하며 잔재를 본체에서 떨어뜨린다.
- `WeaponLowered`는 `[E] 내려놓기`로 1회 설정하고 마지막 걸음은 플레이어가 직접 이동한다.
- 마지막 “다음에 보자”, 버스 탑승, `EndingCompleted`까지 연결한다.

## Out of scope
- 트라우마 처치·도주·다중 엔딩
- 엔딩 이후 플레이 콘텐츠

## Acceptance criteria
- `Test_Ending_NoEscapeExitExists`가 출구 0개를 확인한다.
- `Test_Ending_EnemyProgressionBecomesNonHostileAndEmpty`가 후반 밀도 감소, 비선공 잔재, 최종 적 0을 확인한다.
- `Test_Ending_RunAwayLoopsWithoutPunishment`가 도주 후 동일 공간 복귀와 추가 벌칙 0회를 확인한다.
- `Test_Ending_HintEscalatesAcrossFourLoops`가 반복 1~4회차의 힌트 4단계 발생을 순서대로 확인한다.
- `Test_Ending_TraumaStopsAtFourthLoop`가 4회차 이후 트라우마 정지를 확인한다.
- `Test_Ending_HintNeverStatesDirection`이 힌트 4단계 전체에서 방향 지시 문구 0건을 확인한다.
- `Test_Ending_TraumaContactDoesNotFailStageThirteen`이 Stage 13 접촉에서 붙잡기와 실패 0회를 확인한다.
- `Test_Ending_AttackCannotResolveTrauma`가 공격 무효를 확인한다.
- `Test_Ending_ApproachReversesContamination`이 4단계 역전을 확인한다.
- `Test_Ending_PlayerLowersWeaponAndWalks`가 수동 무장 해제와 직접 이동을 확인한다.
- `Test_Ending_CompletesAfterFarewell`이 대사·버스 탑승 뒤 완료 저장을 확인한다.

## Verification method
- Stage 13 PlayMode named tests
- 전체 엔딩 사용자 조작 검수
