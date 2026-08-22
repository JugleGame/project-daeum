# Stage 7 — Winter Dormitory 레벨디자인

근거 카드: ELEM-056(적 실루엣=주인공 신체 조각 — 이 스테이지부터 전조 시작), ELEM-060, ELEM-061, ELEM-058.
근거 spec: `daeume__spec-004-remnant-enemies`(Stage7부터 `VisualTraitTags` 인간형 암시,
소멸 파편이 트라우마 방향 흔적을 남기기 시작), `daeume__spec-012-combat-encounters`.

## 스테이지 개요

톤이 어두워지기 시작하는 첫 스테이지(Stage4~6의 밝은 톤 유지 원칙이 여기서 처음 깨짐).
위험하거나 웃겼던 사건의 기억. remnant 외형이 조금씩 친구나 주인공을 닮기 시작 —
"이게 그냥 몬스터가 맞나?"라는 의심이 시작되는 지점.

## Zone A — 안뜰 (얼음 지형 도입)

- `stage07.encounter.01`: `Melee 2`, `WaveCount=1`
- `TerrainHazardIds`: 시간 경과로 무너지는 발판(spec-012 선언 3종 중 1)을 얼어붙은 바닥 균열로
  구현 — 첫 얼음 지형 위험요소. 활성 전 시각(균열 확산)·음향(얼음 갈라짐) 신호 필수

## Zone B — 복도

- `stage07.encounter.02`: `Melee 1 + Dash 1 + Ranged 1`, `WaveCount=1` — Stage6에서 확립한
  3종 동시 조우를 이번엔 좁은 실내 복도로 옮겨 압박 강화

## Zone C — 외부 발코니/계단 (수직 + 얼음)

- 이동 플랫폼(spec-012 선언 3종 중 1)을 눈 덮인 미끄러지는 발판으로 구현
- `stage07.encounter.03`: `Dash 2`, `WaveCount=1` — 미끄러짐 중 돌진 예고 회피로 기존 동사 재시험

## Zone D — 방 / 박스

- `stage07.memory.anchor.01`: 전투 없음. 기억은 "위험하거나 웃겼던 사건"
- 조명을 Stage4~6보다 한 단계 낮춤(계획서 톤 전환 지점) — 완전히 어둡지는 않음, 첫 신호 수준

## Zone E — 추격 구간

- `stage07.chase.start` → `stage07.escape`: Zone A~C 재사용, 무너진 발판 잔해가 그대로
  추격 장애물로 남아 재활용(ELEM-051)
- 처치한 잔재의 소멸 흔적이 처음으로 트라우마 방향을 가리키는 연출 큐 추가(spec-004
  `FragmentTracePointsToTrauma` 요구사항과 일치) — 플레이어가 추격 도주 경로를 이 흔적으로
  암묵적으로 유도받게

## Role B 앞 확인 요청

- 얼음 지형(균열 발판·미끄러지는 발판)이 spec-012의 "즉사 없음" 요구를 만족하는 마찰 계수
  범위를 아트/물리 쪽과 확정 요청.
- `VisualTraitTags` 인간형 암시 강도(이번 스테이지 기준치)를 Role C와 조율 요청.
