# Stage 5 — Project Room 레벨디자인

근거 카드: ELEM-060(캠페인 난이도 커브), ELEM-061, ELEM-058, ELEM-048.
근거 spec: `daeume__spec-004-remnant-enemies`, `daeume__spec-012-combat-encounters`.

## 스테이지 개요

두 사람이 함께 무언가를 만들던 공간. 신규 적 유형은 없음(원거리형은 Stage6 개방 공간에서
도입 예정 — Stage3 결정 유지) — 대신 **소형 remnant 수를 크게 늘려** 전투 난이도 자체를
올린다. 기억은 밤새 작업하거나 장난치던 순간. "몬스터 외형이 조금 더 선명해진다"는 연출
지침이며 신규 시스템 아님(VisualTraitTags 인간형 암시는 spec-004 기준 Stage7부터 — 이 스테이지는
실루엣 해상도·조명만 또렷하게).

## Zone A — 입구 작업대

- `stage05.encounter.01`: `EnemyType=Melee 2 + Dash 1`, `WaveCount=1` — Stage4 대비 밀도 상승 시작

## Zone B — 자료 보관 벽장 (밀집 근접전)

- `stage05.encounter.02`: `Melee 3`, `WaveCount=1` — 좁은 통로에 다수 근접형, 회피 공간 최소화로
  포지셔닝 압박(ELEM-061: 좁은 폭 + 다수 = 신규 장애물 없이도 난이도 상승)

## Zone C — 메인 작업층 (스테이지 최대 웨이브)

- `stage05.encounter.03`: `Melee 3 + Dash 2`, `WaveCount=2`(1차 처치 후 2차 증원)
- 넓은 오픈플랜, 책상 배치로 카이팅 동선 확보 — 밀집 물량을 넓은 공간에서 소화하는 이번
  스테이지의 핵심 조우(ELEM-060: 물량 증가는 좁은 공간이 아니라 넓은 공간에서 먼저 검증)

## Zone D — 조용한 구석 / 박스

- `stage05.memory.anchor.01`: 전투 없음. 기억은 "밤새 작업하거나 장난친 순간"
- 톤은 Stage4와 동일하게 밝음 유지(ELEM-048 원칙 지속)

## Zone E — 추격 구간

- `stage05.chase.start` → `stage05.escape`: Zone C의 책상 배치를 넘어뜨려 장애물화(신규 장애물
  대신 기존 구조 재활용 — ELEM-051)
- 추격 중 몬스터 실루엣을 처음으로 뚜렷하게 스치듯 노출(연출 큐, 판정에는 영향 없음)

## Role B 앞 확인 요청

- Zone C 최대 웨이브(`Melee 3 + Dash 2` 동시)가 성능/AI 부하상 무리면 대체안: `SpawnCount`를
  각 -1(`Melee 2 + Dash 1`)로 낮추고 `WaveCount`를 2→3으로 늘려 총량은 유지하되 동시 처리
  부담만 분산. 최종 값은 실측 프레임 확인 후 확정.
