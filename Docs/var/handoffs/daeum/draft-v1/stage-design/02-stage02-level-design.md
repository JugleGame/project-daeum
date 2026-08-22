# Stage 2 — School 레벨디자인

근거 카드: ELEM-061(레벨디자인 크래프트), ELEM-060(동사 티칭 커브), ELEM-058(리추얼 루프), ELEM-051(추격 가독성).
근거 spec: `daeume__spec-012-combat-encounters`(Encounter 데이터: `EncounterId/TriggerArea/EnemyType/SpawnPoint/SpawnCount/WaveCount/ClearCondition/LockExit`).
전작 대비 변화: Stage1은 encounter marker 비활성이었으나, Stage2부터 실제 `Active`(전투 최초 도입).

## 스테이지 개요

두 사람이 처음 만난 공간. 교실 → 복도 → 계단을 지나며 실전 전투를 처음 제대로 가르친다.
적은 1종(기본 근접 remnant)만 사용 — 변주는 Stage3에서 추가. 마지막 기억 조각은 "두 사람이
처음 만난 순간". 몬스터 재등장 → 추격.

## Zone A — 입구 교실 (전투 튜토리얼)

- `stage02.encounter.01`: `EnemyType=Melee 1`, `SpawnCount=1`, `WaveCount=1`, `ClearCondition=DefeatAll`
- 책상·의자로 좁은 원형 아레나 형성, 도주 불가하게 `LockExit`로 강제 교전(ELEM-060: 첫 전투는 회피 선택지 없이 순수 습득)
- 지형 위험요소 없음 — 순수 콤보 입력만 학습

## Zone B — 복도 (연속 교전)

- `stage02.encounter.02`: `EnemyType=Melee 1`, `SpawnCount=2`, `WaveCount=1`
- 직선 복도, 폭 좁게 유지해 다수 적 동시 대면 압박 형성(ELEM-061 가독성: 좌우 회피 공간만 최소 확보)
- 사물함 줄을 `TerrainHazardIds`용 예비 구조로만 배치(이번 스테이지는 미사용, Stage3 이후 재사용 대비)

## Zone C — 계단 (수직 전투)

- Stage1 도입 동사(붙잡기, 일방향 발판)를 전투와 결합해 재확인 — 계단참마다 적 1기, 오르며 처치
- `stage02.encounter.03`: `SpawnCount=1 × 계단참 3곳`, `WaveCount=3`(계단참 진입마다 개별 Wave)

## Zone D — 마지막 교실 / 박스

- `stage02.memory.anchor.01`: 박스 배치, 전투 없는 완충 구간(Zone C 직후 긴장 이완 — ELEM-060 완급 곡선)
- 기억 재생 후 트라우마 몬스터 재등장

## Zone E — 추격 구간

- `stage02.chase.start` → `stage02.escape`: 왔던 복도를 역주행하는 구조(공간 재사용으로 제작비 절감,
  동시에 "안전했던 공간이 위협으로 바뀐다"는 의미 부여)
- 장애물은 Zone B의 사물함 잔해 낙하 1종만 추가 — 신규 장애물 최소화(ELEM-051: 첫 재등장 추격이라
  판정 난이도를 Stage1과 동일 수준으로 유지)

## Role B 앞 확인 요청

- Stage2 Scene/marker가 아직 없다면 이 구조 그대로 `Stage02_Base`에 저작 요청.
- `TerrainHazardIds` 예비 구조(Zone B 사물함)는 Stage2에서 비활성 상태로만 배치 — Stage3 문서에서
  활성화 여부 결정.
