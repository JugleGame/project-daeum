+++
spec_id = "daeume__spec-006-trauma-chase"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ELEM-051", "ARCH-033", "ARCH-002", "ARCH-012", "ARCH-013"]
dependencies = ["daeume__spec-002-movement-platforming", "daeume__spec-005-memory-chest"]
+++

# 트라우마, 기억 역류와 추격

## Goal
트라우마를 처치 불가능한 추격 적이자 공간·잔재·사운드·카메라·규칙에 영향을 주는 기억 역류의 원천으로 만들고, 추격의 길이와 압박을 `ContaminationDirector`가 소유하게 한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: `Stable`·`Echo`·`Intrusion` 3단계, Stage 1 Variant 1개, `ContaminationDirector` 전체, 좌향 도주와 조명 기믹, 탈출점, 실패 조건, 추격 속도 저하 토글.
- 슬라이스에서 제외한다: `Collapse`, Stage 2~13 Variant, 병원 이미지 단계표, Stage 13 역전.

## Implementation scope

### 압박과 Variant
- 압박 단계 `Stable`, `Echo`, `Intrusion`, `Collapse`와 Stage별 `ContaminationVariantId`를 데이터로 선언한다.
- 각 Stage는 공간, 조명, 사운드, 리듬, 문, 동선, 잔재, 기억 재맥락화, 카메라 중 `PrimaryContaminationChannels` 2~3개만 전면에 사용한다.
- Variant는 선택한 Primary 채널과 필요한 보조 신호만 포함하며 모든 채널을 매 Stage 필수로 요구하지 않는다.
- Variant 공간은 원본 공간을 복제하지 않고 기저 레벨 위에 추가 오버레이로 얹는다. 오버레이 적재는 기저 레벨을 닫지 않으며, 같은 `ContaminationVariantId`는 재시도를 포함해 매 진입마다 같은 충돌·타이밍·스폰을 만든다.
- 런타임에 지오메트리를 생성·이동·삭제해 Variant를 만들지 않는다.
- 병원 이미지는 Stage 1~4 추상적 이상, Stage 5~6 의료적 느낌, Stage 7~8 병원 의심, Stage 9 명확한 중첩, Stage 10 확정, Stage 11~12 실제 공간 순으로만 직접성을 높인다.

### ContaminationDirector
- `ContaminationDirector`가 추격의 길이와 압박을 소유한다. 트라우마 액터는 director가 지시한 목표만 실행하며 스스로 추격 종료를 결정하지 않는다.
- 각 Stage는 `TargetChaseSeconds`를 선언한다. director는 이 값을 목표로 압박을 올리고 내린다.
- 트라우마 액터는 `ChaseSpeed`, `MinDistance`, `MaxDistance`를 선언한다. director는 플레이어와의 거리가 `MinDistance` 아래로 내려가면 추격자를 물리고, `MaxDistance`를 넘으면 다시 붙인다.
- 압박 단계는 저작 구간과 director 판단으로만 바뀌고 실패 횟수로 무한 상승하지 않는다.
- director는 플레이어 위치를 항상 알 수 있으나 트라우마를 순간이동시키지 않는다. 예외는 연출 목적의 선언된 지점뿐이며 그 지점은 Stage 데이터에 기록한다.
- 플레이어가 정지하면 director는 추격자를 계속 접근시킨다. 정지가 안전 상태가 되지 않는다.
- 막힌 길에서 director는 추격자를 물리고 `MaxDistance`를 유지한다. 즉시 실패시키지 않는다.

### 추격 구간과 공정성
- Stage 1~12는 출현점, 도주 방향, 재사용 경로, 고유 기믹, 탈출점과 실패 조건을 가진다.
- 추격 실패는 트라우마 접촉 1가지뿐이며 판정과 결과는 `daeume__spec-003-player-combat`가 소유한다.
- 경로 변화는 시각·음향으로 예고하며 입력 반전, 무작위 키 무시, 보이지 않는 즉사를 금지한다.
- 필수 경로의 시각 신호는 색만으로 구분되지 않는다. 형태 또는 기호를 함께 사용한다.
- `ChaseSpeedAssist` 토글은 트라우마의 `ChaseSpeed`와 director의 접근 압박만 낮춘다. 경로, 기믹, 신호 타이밍, 탈출점은 바꾸지 않는다.
- Stage 13은 도주 공간 반복과 접근에 따른 `Collapse→Intrusion→Echo→Stable` 역전만 허용한다.

## Out of scope
- 트라우마 처치, 무작위 미로, 전 스테이지별 완전 별도 기술 시스템
- 트라우마 접촉 시 붙잡기 연출의 판정과 길이 (`daeume__spec-003-player-combat`가 소유)
- 최종 음악·아트 제작

## Acceptance criteria
- `Test_Trauma_StagesOneToTwelveCannotBeKilled`가 공격 후 추격 지속을 확인한다.
- `Test_Contamination_FourPressureStagesDeclared`가 4단계와 유효 Variant 참조를 확인한다.
- `Test_Contamination_EachStageSelectsTwoOrThreePrimaryChannels`가 Stage 1~11의 채널 수와 Stage 12 클라이맥스 예외를 확인한다.
- `Test_Contamination_VariantOverlaysBaseWithoutClosingIt`가 Variant 적재 후에도 기저 레벨이 열려 있음을 확인한다.
- `Test_Contamination_OverlayUnloadRestoresBase`가 오버레이 적재와 해제 후 기저 레벨의 충돌이 최초 적재와 같음을 확인한다.
- `Test_Chase_DirectorOwnsChaseLength`가 트라우마 액터가 추격 종료를 스스로 요청하는 횟수 0회를 확인한다.
- `Test_Chase_DirectorKeepsDistanceBounds`가 추격 중 거리가 `MinDistance` 아래와 `MaxDistance` 위에 머무르지 않음을 확인한다.
- `Test_Chase_DirectorNeverTeleportsOutsideDeclaredPoints`가 선언되지 않은 순간이동 0회를 확인한다.
- `Test_Chase_StandingStillIsNotSafe`가 플레이어 정지 시 거리 감소를 확인한다.
- `Test_Chase_DeadEndBacksOffInsteadOfFailing`이 막힌 길에서 실패 0회와 거리 유지를 확인한다.
- `Test_Chase_RequiredSignalsAreNotColorOnly`가 필수 경로 신호에 형태 또는 기호가 함께 있음을 확인한다.
- `Test_Chase_SpeedAssistChangesSpeedOnly`가 토글 전후의 경로·기믹·탈출점 동일성을 확인한다.
- `Test_Contamination_HospitalImageryEscalatesByStage`가 직접성 단계의 역행이 없음을 확인한다.
- `Test_Contamination_RequiredRouteSignalsExist`가 필수 길 변화의 시각·음향 신호를 확인한다.
- `Test_Contamination_RetryUsesSameVariant`가 재시도 결과의 결정성을 확인한다.
- `Test_Chase_EachStageHasMeaningfulMechanic`가 Stage 1~12에 고유 기믹과 기억 연결 설명이 있음을 확인한다.
- `Test_Trauma_StageThirteenApproachReversesPressure`가 접근 시 단계 역전을 확인한다.

## Verification method
- EditMode Variant·공정성 데이터 검사
- Stage 1, 8, 12, 13 PlayMode named tests
- 사용자 추격 가독성 검수
