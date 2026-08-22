# Stage 11 — The Hospital 레벨디자인

근거 카드: **ELEM-056(적 실루엣=주인공 신체 조각)** — 이 스테이지가 정식 공개 지점. ELEM-057(비전투
수용형 결말 — 이 스테이지의 `OptionalReactive` 구간이 예고편). ELEM-061, ELEM-058.
근거 spec: `daeume__spec-004-remnant-enemies`(모든 유형이 `protagonist_hand/face/clothes` 중
1개 이상 필수), `daeume__spec-012-combat-encounters`(`OptionalReactive` 최소 1구간, 양쪽
선택 모두 페널티 없음).

## 스테이지 개요

병원 복도 자체가 사이드스크롤 액션 스테이지가 된다. remnant가 지금까지보다 훨씬 인간형에
가까워진다. 여기서 처음 명시적으로 드러난다 — 지금까지 죽인 작은 몬스터들의 실루엣을
자세히 보면 전부 주인공의 손·얼굴·옷 일부를 지니고 있었다는 것.

## Zone A — 병원 복도 입구 (전신 공개)

- `stage11.encounter.01`: `Melee 2 + Dash 1`, `WaveCount=1`
- 이 구간부터 모든 remnant 모델이 `protagonist_hand`/`protagonist_face`/`protagonist_clothes`
  중 최소 1개를 시각적으로 드러냄 — 조명은 이 디테일이 보이게 정면·근접 앵글 확보(ELEM-056:
  반전은 플레이어가 "눈치챌 수 있어야" 성립, 숨기지 않음)

## Zone B — 대기실 (OptionalReactive 구간)

- `stage11.encounter.02`: `EnemyType=Reactive 1`, `ClearCondition=PassWithoutAggression` 또는
  `DefeatAll` 둘 다 유효
- 좁은 대기실, remnant 1기가 등을 보이고 서 있음. 공격하지 않으면 웅크리거나 길을 비켜줌,
  공격하면 기존 전투 시작(spec-012 `PassWithoutAggression`) — 보상·엔딩 페널티 없음
- 이 구역은 Zone A/C보다 조용하고 느리게 설계 — 선택이 읽혀야 하므로 시간 압박 요소(추격,
  타이머) 배치 금지(ELEM-057: 수용/비폭력 선택은 항상 여유 있는 공간에서 제시)

## Zone C — 병동 (표준 인간형 전투)

- `stage11.encounter.03`: `Melee 2 + Dash 1 + Ranged 1`, `WaveCount=2`

## Zone D — 병실 / 박스

- `stage11.memory.anchor.01`: 전투 없음. `HospitalImageryDirectness`를 Stage10보다 한 단계
  올림(병실 침대, 모니터 소리 등 더 직접적인 신호)

## Zone E — 추격 구간

- `stage11.chase.start` → `stage11.escape`: Zone A/C 복도 재사용
- 추격 중 스치는 remnant 잔영 얼굴이 처음으로 낯익게 스치는 연출 큐(판정 없음, 순수 연출)

## Role B 앞 확인 요청

- (미해결, 비차단) Zone B 위치가 최적인지는 실제 플레이테스트로만 확정 가능 — 이 문서의
  Zone 순서(A 공개→B 저압 선택→C 표준 전투)는 설계 의도이며 그대로 구현 후 재배치해도
  다른 Zone과의 marker 의존관계 없음.
- `protagonist_*` 태그의 구체 표현(부위별 실제 스프라이트)은 Role C 아트 결정 사항 — 이
  문서는 배치 요구(정면·근접 앵글 확보)만 선언.
