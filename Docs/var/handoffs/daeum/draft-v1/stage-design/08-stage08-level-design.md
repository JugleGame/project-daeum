# Stage 8 — Workshop / Office 레벨디자인

근거 카드: ELEM-060, ELEM-061, ELEM-058, ELEM-056.
근거 spec: `daeume__spec-004-remnant-enemies`, `daeume__spec-012-combat-encounters`(셔터 문 hazard).

## 스테이지 개요

"다음에"라는 말이 본격 등장. 미완성 프로젝트와 지키지 못한 약속이 배경에 남아있다.
remnant는 이전보다 확실히 강함(수·웨이브 밀도 상승, 신규 아키타입은 없음). 기억: "이건
다음에 하자", "나중에 가자", "다음에 보자". 후회가 처음으로 이야기 중심에 들어온다.

## Zone A — 사무실 입구

- `stage08.encounter.01`: `Melee 2 + Dash 1 + Ranged 1`, `WaveCount=2` — Stage6/7 기준치보다
  한 단계 상승한 첫 조우(ELEM-060: "강해졌다"는 서사를 웨이브 수로 즉시 체감시킴)

## Zone B — 칸막이 미로 (셔터 문 도입)

- `TerrainHazardIds`: 순차 폐쇄 셔터 문(spec-012 선언 3종 중 마지막 미사용 유형) 최초 도입
- 구간을 방 단위로 끊어 순차 격파 강제 — "미완성 프로젝트가 방마다 갇혀있다"는 배경과
  구조가 일치(ELEM-061: 공간 구획 자체가 테마를 전달)
- `stage08.encounter.02`: 방마다 `Melee 1~2`, 총 3개 방, 방 진입마다 개별 Wave

## Zone C — 작업장 바닥 (최대 밀도)

- `stage08.encounter.03`: `Melee 2 + Dash 2 + Ranged 1`, `WaveCount=2` — 지금까지 최대 밀도
- 미완성 구조물(뼈대만 있는 선반, 덮개 씌운 시제품)로 시야·동선 복잡화

## Zone D — 조용한 구석 / 박스

- `stage08.memory.anchor.01`: 전투 없음. 기억은 "다음에 하자"류 약속들의 나열
- 배경 소품: 완성 못 한 청사진, 붙여둔 포스트잇("다음에", "나중에")

## Zone E — 추격 구간

- `stage08.chase.start` → `stage08.escape`: Zone B의 셔터 문을 추격 중 플레이어 등 뒤에서
  차례로 닫히게 재사용 — "돌아갈 수 없다"는 후회 테마를 판정으로 체감(ELEM-058: 기존 학습
  기믹의 의미 재해석)

## Role B 앞 확인 요청

- 셔터 문 hazard는 spec-012 요구대로 플레이어·잔재 동일 규칙 적용 확인. 추격 중 "등 뒤에서
  닫히는" 연출이 플레이어 판정에는 영향 없는 연출 전용인지, 실제 차단 판정을 겸하는지 결정 요청.
