+++
spec_id = "daeume__spec-007-stage-progression"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-012", "ARCH-033", "GENRE-040", "ELEM-048"]
dependencies = ["daeume__spec-004-remnant-enemies", "daeume__spec-006-trauma-chase", "daeume__spec-012-combat-encounters"]
+++

# 13개 Stage 진행 데이터

## Goal
13개 Stage가 같은 핵심 루프를 유지하면서 장소, 기억, Encounter, 기억 역류와 추격의 의미를 독립적으로 변화시키게 한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: StageData 스키마 전체와 Stage 1 레코드 1개.
- 슬라이스에서 제외한다: Stage 2~13 레코드, 전체 플레이타임 검증, Stage 13 밀도 규칙.
- 스키마는 13개 전부를 담을 수 있게 지금 확정한다. Stage 1만 채운다.

## Implementation scope
- 각 Stage에 장소, MemoryId, MemoryTitle, MemoryPresentationId, EmotionalRole, EncounterIds, ContaminationVariantId, PrimaryContaminationChannels, ContaminationMeaning, HospitalImageryDirectness, ChaseId, ChaseMeaning, TargetChaseSeconds, LengthCategory, TargetPlaytimeMinutes, 단서, 강도 5종, NextStageId를 선언한다.
- `HospitalImageryDirectness`는 0~4 정수다. 0은 완전 추상, 4는 실제 병원 공간이다. `daeume__spec-006-trauma-chase`의 단계 규칙이 이 필드를 검사한다.
- `TargetChaseSeconds`는 `ContaminationDirector`가 소비하는 Stage별 목표 추격 시간이다.
- 세부 사건과 추격 의미는 `narrative-trauma-design.md`의 Stage 1~13 계약을 따른다.
- Stage 1~3은 호기심·따뜻함, 4~6은 행복·불안, 7~9는 이상함·후회·의심, 10~12는 공포·진실·상실, 13은 저항→수용→작별을 따른다.
- Stage 13은 전반보다 후반의 적 밀도를 낮추고, 최소 1개 비선공 구간을 거쳐 최종 기억 구간의 적을 0으로 만든다. 정확한 수는 밸런싱 데이터다.
- Stage 1~12 `NextStageId`는 순차 연결하고 Stage 13은 Ending으로 연결한다.

## Out of scope
- 최종 레벨 지오메트리·아트·대사 원고
- 분기 캠페인과 절차 생성

## Acceptance criteria
- `Test_Progression_HasThirteenOrderedStages`가 고유 Stage 13개를 확인한다.
- `Test_Progression_AllStagesHaveRequiredNarrativeFields`가 필수 데이터와 강도 5종을 확인한다.
- `Test_Progression_EachStageHasPrimaryContaminationMeaningAndChaseMeaning`이 Stage별 채널과 의미를 확인한다.
- `Test_Progression_HospitalDirectnessInRange`가 모든 Stage의 `HospitalImageryDirectness`가 0~4 정수임을 확인한다.
- `Test_Progression_EachStageDeclaresTargetChaseSeconds`가 Stage 1~13의 목표 추격 시간 선언을 확인한다.
- `Test_Progression_TargetPlaytimeTotalsFourToFiveHours`가 재시도·탐색을 포함한 전체 목표 범위를 확인한다.
- `Test_Progression_StagesOneToTwelveHaveUniqueChases`가 ChaseId 12개와 의미 설명을 확인한다.
- `Test_Progression_StageEightPreservesThreeNextTimeLines`가 필수 문장 3개를 확인한다.
- `Test_Progression_FinalStageEnemyDensityDecreases`가 후반 밀도가 전반보다 낮고, 비선공 구간이 1개 이상이며, 최종 기억 구간 적이 0임을 확인한다.

## Verification method
- EditMode StageData 스키마·연결성 테스트
- Stage 1·13 PlayMode 기능 테스트
