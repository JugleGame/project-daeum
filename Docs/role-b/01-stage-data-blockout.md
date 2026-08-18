# B1 — StageData와 Stage01 blockout

## 목표

13개 Stage를 표현할 수 있는 StageData 스키마를 고정하고 Stage 1 레코드 하나를 만든다. `Stage01_Base`에서 Role A Player로 핵심 동선을 이동할 수 있게 blockout과 marker를 구성한다.

## 기준 요구사항

- Spec: `daeume__spec-007-stage-progression` draft v3의 8일 슬라이스 범위
- StageData 필드: 장소, `MemoryId`, `MemoryTitle`, `MemoryPresentationId`, `EmotionalRole`, `EncounterIds`, `ContaminationVariantId`, `PrimaryContaminationChannels`, `ContaminationMeaning`, `HospitalImageryDirectness`, `ChaseId`, `ChaseMeaning`, `TargetChaseSeconds`, `LengthCategory`, `TargetPlaytimeMinutes`, 단서, 강도 5종, `NextStageId`
- `HospitalImageryDirectness`는 0~4 정수다.
- 스키마는 13개 Stage를 담을 수 있어야 하지만 이번 세션에서는 Stage 1만 채운다.
- 런타임 지오메트리 생성 대신 Scene에 저작한 blockout을 사용한다.

## 선행조건

- Issue #3과 작업 branch를 확인한다.
- `00-session-guide.md`의 변경 경계와 기존 사용자 변경을 확인한다.
- 현재 Role A Player와 Scene flow가 기대하는 Layer, Collider, one-way platform, `GrabbableSurface` 사용법을 코드에서 확인한다.

## 구현 범위

- StageData용 직렬화 가능 데이터 또는 ScriptableObject 스키마
- Stage 1 데이터 asset 1개
- `Stage01_Base.unity`의 시작점, 이동 동선, 낙하/복귀 구간, 카메라 경계
- Remnant spawn marker, Encounter trigger/exit marker, Memory 연결 지점, chase 시작/탈출 marker
- marker는 후속 세션이 안정적인 ID로 참조할 수 있게 이름과 식별자를 고정한다.
- 임시 시각 요소는 Role B 전용 placeholder로 두고 Role C art를 수정하지 않는다.

## 범위 제외

- Stage 2~13 데이터 레코드
- Remnant AI, Wave, Director, 실제 chase 동작
- 최종 지오메트리, art, 대사

## 검증

- EditMode: StageData 필수 필드, 범위, 고유 ID와 Stage 1 참조 유효성
- Scene/Layout: marker ID 중복 0, 필수 Collider/LayerMask 확인
- PlayMode: Stage01 진입 후 좌우 이동, 점프, 붙잡기, 낙하 복구와 카메라 경계를 실제 입력으로 확인
- 기대 증거: 이동 가능한 Stage01 blockout 캡처 또는 QA 기록, Compile/Console error 0

## 완료 조건

- StageData 스키마가 Spec의 전체 필드를 표현한다.
- Stage 1 데이터가 유효한 Encounter/Variant/Chase ID 자리를 선언한다.
- 후속 B2~B5가 Scene 오브젝트를 재배치하지 않고 marker에 연결할 수 있다.
- Role A/C 소유 Scene과 Prefab을 수정하지 않는다.

## 세션 결과

- 상태: 완료
- 커밋: `0b17eb3` (`feat: add Stage 1 data and blockout`)
- 구현: 13개 Stage를 수용하는 `StageData` 스키마와 Stage 1 asset을 추가했다. `Stage01_Base`에 시작 지형, 일방향 발판, 붙잡기 벽, 낙하 복귀 계단, encounter 구간, 카메라 경계와 후속 세션용 안정 marker 9개를 Scene 저작 데이터로 배치했다.
- 테스트: B1 EditMode 4/4 통과, B1 PlayMode 2/2 통과, 영향받는 전체 PlayMode 회귀 27/27 통과, 실패 0, 최종 Unity Console error 0.
- 수동 QA: Unity 2D Scene 캡처로 전체 blockout 배치를 확인했다. PlayMode에서 Input System D 키 입력, 점프, 붙잡기, 낙하 복귀 바닥 착지와 카메라 최소/최대 경계를 검증했다.
- contract 변경: 기존 공용 contract 변경 없음. `Daeume.Stage.StageData`, `StageDefinition`, `StageMarker`, `StageCameraBounds`를 새 Role B API로 추가했다. event payload는 추가하지 않았다.
- 다음 세션 주의점: B2는 `stage01.remnant.spawn.01`, `stage01.remnant.spawn.02` marker를 사용한다. B3~B5는 각각 `stage01.encounter.01.trigger`, `stage01.encounter.01.exit`, `stage01.memory.anchor.01`, `stage01.chase.start`, `stage01.escape`를 재배치 없이 소비한다.
