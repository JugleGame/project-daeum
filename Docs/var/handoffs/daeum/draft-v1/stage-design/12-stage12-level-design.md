# Stage 12 — Intensive Care Unit 레벨디자인

근거 카드: ELEM-060(캠페인 정점), ELEM-051(추격 가독성 — 최장 추격), ELEM-058, ELEM-056.
근거 spec: `daeume__spec-004-remnant-enemies`, `daeume__spec-012-combat-encounters`,
`daeume__spec-006-trauma-chase`(목표 추격 시간 — 이 스테이지가 전체 최장값).

## 스테이지 개요

잔재와의 최종 대규모 전투. 박스에서 가장 중요한 기억 조각 — 중환자실 기억이 재생되고,
플레이어가 지금까지 알던 진실이 깨진다. 이후 트라우마 몬스터가 완전한 형태로 등장,
지금까지 중 가장 긴 추격.

## Zone A — 중환자실 입구 (전투 재개)

- `stage12.encounter.01`: `Melee 3 + Dash 2 + Ranged 1`, `WaveCount=2`

## Zone B — 병상 구획 (지형 총동원 + 밀도)

- 셔터 문 + 이동 발판을 병상 칸막이·커튼 레일로 재스킨해 재사용
- `stage12.encounter.02`: `Melee 2 + Dash 2 + Ranged 2`, `WaveCount=2`

## Zone C — 최종 대규모 전투 (전체 최대 밀도)

- `stage12.encounter.03`: `Melee 3 + Dash 3 + Ranged 2`, `WaveCount=3` — 캠페인 전체 최댓값
  (ELEM-060: 정점은 여기, Stage13은 밀도가 아니라 규칙 자체를 바꾸는 방향이므로 물량 경쟁은
  이 스테이지에서 끝낸다)

## Zone D — 병상 / 박스 (진실 공개)

- `stage12.memory.anchor.01`: 전투 없음. `HospitalImageryDirectness` 최댓값(4) — 가장
  중요한 기억 조각, 진실 반전이 여기서 발생
- 박스 개봉 직후 트라우마 몬스터가 완전한 형태로 첫 등장(연출 컷, Stage10의 "가까운 형태"보다
  한 단계 더 명확한 전신 노출)

## Zone E — 최장 추격 구간

- `stage12.chase.start` → `stage12.escape`: 캠페인 내 `TargetChaseSeconds` 최댓값(director
  튜닝 대상, spec-006 소유)
- 경로는 Zone A~C 전체를 관통 — 지금까지 스테이지 중 가장 긴 직선+분기 혼합 추격로
- 장애물은 전부 기존 학습분 재사용(신규 장애물 없음, ELEM-051 원칙 끝까지 유지)

## Role B 앞 확인 요청

- Zone C 최대 밀도가 성능상 무리면 대체안: Stage5와 동일하게 `WaveCount`를 3→4로 늘려
  동시 스폰 수를 낮추고 총량 유지(`Melee 2+Dash2+Ranged1`로 매 웨이브 소폭 조정).
- 제안값: 최장 추격 목표 시간 60~90초(Stage1 최단 10~15초 대비 전체 최댓값 — 상대적 배율
  기준, 정확한 초는 director 튜닝 담당 확정).
