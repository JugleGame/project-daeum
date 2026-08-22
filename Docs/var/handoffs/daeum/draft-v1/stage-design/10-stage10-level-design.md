# Stage 10 — The Way to the Hospital 레벨디자인

근거 카드: ELEM-060(캠페인 난이도 커브 정점), ELEM-061, ELEM-051, ELEM-058.
근거 spec: `daeume__spec-004-remnant-enemies`, `daeume__spec-012-combat-encounters`,
`daeume__spec-007-stage-progression`(`HospitalImageryDirectness` 필드 — 이 스테이지가 첫 비영(非零) 값).

## 스테이지 개요

여기서부터 액션 난이도가 최고조. 지금까지 배운 모든 기술을 요구한다. 박스에서 처음으로
병원 관련 기억이 나온다. 트라우마 몬스터가 매우 가까운 형태로 등장.

## 난이도 원칙

- 신규 적 유형·신규 hazard 타입 추가 없음. 대신 **지금까지 도입한 3종 지형 위험요소(얼음
  균열 발판, 눈길 이동 발판, 셔터 문)를 한 스테이지에서 전부 사용** — 이 시점까지의 학습을
  총동원(ELEM-060: 정점 스테이지는 신규 학습이 아니라 기존 습득 총합 시험).

## Zone A — 병원 가는 길 초입

- `stage10.encounter.01`: `Melee 2 + Dash 1 + Ranged 1`, `WaveCount=2`

## Zone B — 골목 관문 (지형 3종 연속 배치)

- 순서대로: 셔터 문 구간 → 눈길 이동 발판 → 얼음 균열 발판. 세 구간을 끊김 없이 이어
  기존 동사를 연속으로 재시험(ELEM-061: 이미 검증된 부품을 새 순서로 조합 — 신규 제작비 없음)
- `stage10.encounter.02`: 구간별 `Melee 1~2`, `WaveCount=3`(구간 진입마다 개별 Wave)

## Zone C — 최종 관문 (지금까지 최대 밀도 갱신)

- `stage10.encounter.03`: `Melee 3 + Dash 2 + Ranged 2`, `WaveCount=2` — Stage9 밀도를 상회

## Zone D — 병원 앞 / 박스

- `stage10.memory.anchor.01`: 전투 없음. **첫 병원 관련 기억** — `HospitalImageryDirectness`
  최저 비영 값(멀리서 본 구급차, 대기실 소리 등 간접 신호로 시작)
- 트라우마 몬스터가 박스 직후 지금까지보다 훨씬 가까운 거리에서 첫 등장(연출 컷)

## Zone E — 추격 구간

- `stage10.chase.start` → `stage10.escape`: Zone A~C 전체를 관통하는 가장 긴 추격 경로
- 트라우마 몬스터와의 거리를 이전 스테이지보다 좁게 설정(director 목표 추격 거리 하한 조정
  요청 — spec-006 소유)

## Role B 앞 확인 요청

- Zone B 지형 3종 연속 배치의 총 소요 시간이 spec-006 director의 목표 추격 시간 산정과
  충돌하지 않는지 확인.
- 트라우마 몬스터 "가까운 형태" 등장의 구체 거리 값은 director 튜닝 담당과 결정 요청.
