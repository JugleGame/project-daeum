# Stage 6 — Street / Shop 레벨디자인

근거 카드: ELEM-048(일상 유대·공포 대비), ELEM-060, ELEM-061, ELEM-058, ELEM-051.
근거 spec: `daeume__spec-004-remnant-enemies`, `daeume__spec-012-combat-encounters`.

## 스테이지 개요

물건을 사러 나가거나 놀던 기억. 계획서가 명시적으로 요구하는 최대 대비 지점: 밝은 오후,
귀여운 상점, 웃으며 어울리던 기억 — 그 사이를 뒤틀린 작은 몬스터들이 돌아다닌다. 신규
적 유형 **원거리형** 도입(트인 공간 확보 — Stage3에서 미룬 결정 실행).

## 톤 원칙 (ELEM-048, 이 스테이지에서 가장 강하게 적용)

- 상점 간판·파라솔·진열대 색은 게임 전체에서 가장 채도 높게 유지. 전투·추격 중에도 유지.
- 몬스터는 크기·움직임을 "작고 어색한" 실루엣으로 한정 — 이 스테이지에서 위협감보다
  이물감을 우선(위협감 상승은 Stage7부터).

## Zone A — 거리 초입 (원거리형 튜토리얼)

- `stage06.encounter.01`: `EnemyType=Ranged 1`, `SpawnCount=1`, `WaveCount=1`
- 트인 광장, 노점 파라솔을 엄폐물로 배치해 사거리 끊기·거리 좁히기를 저압 상황에서 학습
  (ELEM-060: 신규 동사 단독 저압 학습 원칙 유지)

## Zone B — 상점 골목 (근접 대비 좁은 공간)

- `stage06.encounter.02`: `Melee 1 + Dash 1`, `WaveCount=1` — Zone A의 개방감과 대비되는
  좁은 진열대 사이 통로(공간 폭 자체가 난이도 변주, ELEM-061)

## Zone C — 메인 거리 (혼합 전종 조우)

- `stage06.encounter.03`: `Melee 2 + Dash 1 + Ranged 1`, `WaveCount=1` — 3종 아키타입 첫 동시 등장
- 노점·벤치로 시야선 차단물 다수 배치, 원거리형 대응에 엄폐 활용 요구

## Zone D — 조용한 상점 구석 / 박스

- `stage06.memory.anchor.01`: 전투 없음. 기억은 "함께 웃으며 물건 사거나 놀던 순간"
- 밝은 톤 유지, 진열 소품(간식, 소품 인형)에 디테일 집중

## Zone E — 추격 구간

- `stage06.chase.start` → `stage06.escape`: Zone A~C 공간 역주행
- 원거리형의 투사체 궤적을 추격 중 회피 신호로 재사용 — 첫 "투사체형 추격 장애물" 등장
  (기존 학습 판정 재활용, 신규 장애물 최소화 원칙 유지)

## Role B 앞 확인 요청

- `RangedRemnant`/`RangedRemnantData`(core 브랜치 작업 중) 완성 시 이 스테이지가 첫 소비처.
- Zone A 엄폐용 파라솔이 `TerrainHazardIds`로 등록될지, 단순 시각 엄폐물(비-hazard)로 둘지 결정 요청.
