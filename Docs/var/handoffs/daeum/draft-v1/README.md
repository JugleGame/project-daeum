# 《다음에》 기획 패키지 draft-v1

이 디렉터리는 《다음에》의 현재 기획 원본이다. `var/`는 Git에 포함되지 않는 로컬
작업 영역이므로 초기화될 수 있으며, 이 패키지는 Research MCP에 publish된 hand-off가
아니다.

## 문서

1. `blueprint.json` — 최상위 게임 계약
2. `narrative-trauma-design.md` — 게임 정체성, 기억 역류, 13개 Stage, 복선과 범위
3. `specs/daeume__spec-001-*.md` ~ `daeume__spec-015-*.md` — 공식 기능 명세
4. `adversarial-review.md` — 적대적 검토 결과와 착수 전 해결 항목 (리서치 카드 근거 포함)
5. `design-decisions.md` — 검토가 제기한 판단 항목 14건의 확정 결정과 spec 수정 지시
6. `collaboration-setup.md` — 픽셀 규격·팔레트, Unity 구조, 저장소, 3인 분업과 8일 일정

## 현재 빌드 범위

가용 공수는 개발자 3인 × 8일 = 약 96 person-hour다. 13 Stage는 이 예산으로 불가능하므로
**8일 빌드는 Stage 1 버티컬 슬라이스만 구현한다.** 13 Stage 설계 계약은 축소하지 않고
그대로 보존하며 구현만 미룬다. 무엇이 포함되는지는 각 spec의 `## Build scope` 절이 단일 원본이다.

공식 spec은 001~015뿐이며 spec-016 이후 파일이나 plannedSpecIds는 없다. Memory Contamination
책임은 기존 006, 007, 014 안에 배치한다.

## 개발 에이전트 착수 순서

의존 순서이자 자르는 순서다. 공수가 모자라면 **아래에서부터** 자른다.

1. `spec-001` 상태 기계 → `spec-011` 저장 → `spec-015` Title→Stage 1→결과
2. `spec-002` 이동과 붙잡기 → `spec-010` 상호작용
3. `spec-003` 전투와 트라우마 접촉 → `spec-004` 근접 잔재 1종 → `spec-012` Encounter
4. `spec-005` MemoryInteractable와 회상
5. `spec-006` Contamination Overlay와 `ContaminationDirector` ← 슬라이스의 핵심 리스크
6. `spec-013` UI와 접근성 옵션 5종 → `spec-014` 오디오와 추격 카메라
7. `spec-007` StageData 스키마와 Stage 1 레코드
8. `spec-008` Stage 1 기억 1건, `spec-009` 제외

`spec-013`의 접근성 옵션은 자르지 않는다. 색 비의존 신호와 리매핑은 기준선이며,
공정성 신호를 저작하는 시점에 함께 만들지 않으면 나중 비용이 훨씬 크다.

## 용어

한국어 표기는 "기억 역류"를 유지한다. 식별자·필드·enum·테스트명 등 기계 표면과
영문 표기는 `Contamination` 계열로 통일한다. `Backflow`는 사용하지 않는다.

## 핵심 문장

> 행복한 기억을 되찾는 행위 자체가 세계를 오염시키고, 플레이어가 열두 번 도망치는
> 행동을 학습한 뒤 마지막 한 번만 스스로 뒤돌아 트라우마를 향해 걸어가는 게임.

복원 규칙은 `specs/daeume__spec-011-checkpoint-save.md`가 단일 원본이다.
