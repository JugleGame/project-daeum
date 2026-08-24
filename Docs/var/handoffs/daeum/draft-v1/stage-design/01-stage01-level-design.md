# Stage 1 — Bus Stop / Street 레벨디자인

근거 카드: `Game-Planning-RAG` ELEM-061(2D 플랫포머 레벨디자인 크래프트), ELEM-058(박스→기억→추격 리추얼 루프), ELEM-060(캠페인 난이도·동사 티칭 커브), ELEM-051(추격 가독성).
기존 marker: B1 blockout(`Docs/role-b/01-stage-data-blockout.md`) 재사용.

## 스테이지 개요

기본 조작 튜토리얼. 초반 remnant 사실상 없음. 전진하며 세계 이상함만 암시. 끝에서 첫 박스 →
기억 조각 #1 → 짧은 회상 → 첫 트라우마 몬스터 등장 → 추격. 핵심 규칙 학습: "박스를 열면 기억을
얻지만 몬스터를 부른다."

## Zone A — 정류장 (튜토리얼)

- 벤치·연석 낮은 단차만 배치, 이동/점프 순차 1개씩 학습
- 몬스터 0. `stage01.remnant.spawn.01/02`는 비활성(접촉 판정 없는 원경 실루엣 장식만)
- 이상함 신호 1개: 정지된 시계 또는 깜빡이는 신호등(전투 없이 톤만 암시)

## Zone B — 거리 구간

- 붙잡기 벽 1회, 일방향 발판 1회를 순차 배치(동사 단독 학습, 동시 요구 금지 — ELEM-060)
- 낙하 복귀 계단으로 실패해도 루트 복귀 가능
- `stage01.encounter.01.trigger~exit` 구간: 이번 스테이지는 전투 없음이 확정 사항이라
  통과만 하는 빈 통로로 둔다.

## Zone C — 박스 앞 막다른 길

- 폭을 좁히고 배경을 정지시켜 박스에 시선 집중(ELEM-061 가독성)
- `stage01.memory.anchor.01`에 박스 배치. 여기까지 장애물 없음

## Zone D — 추격 구간 (박스 오픈 이후)

- `stage01.chase.start` → `stage01.escape` 직선에 가까운 넓은 통로
- 장애물은 "피하는 것" 하나만(쓰러진 표지판 뛰어넘기) — 미로형 배치 금지, 첫 추격이라
  판정 자체를 가르쳐야 함(ELEM-051)
- 카메라 경계를 좁혀 압박감 형성

## Role B 앞 확인 요청

`stage01.encounter.01.trigger`/`stage01.encounter.01.exit` marker는 Stage1 전투 없음 확정과
충돌한다. 이후 스테이지에서도 이 marker id를 재사용할 계획이 없다면 Stage1 Scene에서
제거를 요청한다. (Encounter 시스템 자체는 Stage2부터 필요 — 이 marker만 Stage1 한정으로
불필요.)
