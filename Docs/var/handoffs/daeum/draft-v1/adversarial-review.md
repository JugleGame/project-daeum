# 《다음에》 draft-v1 적대적 검토

검토 대상: `blueprint.json`, `narrative-trauma-design.md`, `specs/daeume__spec-001~015`
검토 기준: 계약 검증 가능성, 시스템 책임 공백, 페이싱·상업 리스크
RAG 근거: 기존 리서치 DB(177장, trigram 검색)에서 인용. **신규 카드는 만들지 않았다.**

## 0. 판정

서사 설계는 완결적이다. Truth 순서, 복선 회수, Stage 13 수용 시퀀스는 구멍이 없다.
**깨지는 곳은 서사가 아니라 액션 계약이다.** 이 게임은 추격 12회가 핵심인데
"추격에서 잡히면 무슨 일이 일어나는가"와 "추격자는 어떻게 움직이는가"가
15개 spec 어디에도 없다. 현 상태로 착수하면 vertical slice에서 막힌다.

---

## 1. 치명 결함 — 착수 전 해결 필요

### C1. 추격 실패 판정이 존재하지 않는다

- spec-003은 **플레이어 → 트라우마** 공격이 무효라고만 정의한다.
- **트라우마 → 플레이어**의 피해량, 접촉 판정, 붙잡기 연출, 유예 시간, 무적 시간,
  `StageState=Failed` 진입 조건이 어느 spec에도 없다.
- spec-006 Implementation scope에 "실패 조건"이라는 단어만 있고 대응 Acceptance criterion이 없다.
- spec-003의 유일한 실패 조건은 `Test_Combat_ZeroHealthTriggersFailure`(체력 0)다.
  그렇다면 트라우마 접촉은 잔재와 같은 피해량인가? 즉사인가? 명시가 없다.

즉사로 두면 blueprint nonGoal "보이지 않는 즉사"와 충돌하고(보이는 즉사는 허용이지만
계약에 없음), 피해로 두면 12회 추격 내내 HP 관리가 새 시스템이 된다.
**둘 중 무엇인지 정하지 않은 상태로 추격 난이도를 설계할 수 없다.**

### C2. 트라우마 추격 AI가 책임 공백에 빠져 있다

- spec-004 Out of scope: "트라우마 추격 AI" — 명시적 배제.
- spec-006은 출현점·도주 방향·기믹·탈출점만 다루고 추격자 이동 규칙을 다루지 않는다.
- 그런데 spec-014는 "**트라우마 거리**와 압박 단계로 발걸음, 카메라 시야 압박,
  흔들림 프리셋을 선택한다"며 **정의되지 않은 값을 소비한다.**

미정의 항목: 이동 속도, 유지 거리 하한·상한, 러버밴딩 유무, 플레이어 정지 시 행동,
막힌 길에서의 처리, 화면 밖 이탈 시 복귀 규칙.
이것이 추격 게임의 핵심 튜닝 파라미터 전부다.

### C3. 검증 불가능한 Acceptance Criteria 3건

| Spec | 테스트 | 왜 통과시킬 수 없나 |
|---|---|---|
| 006 | `Test_Contamination_HospitalImageryEscalatesByStage` | "병원 imagery 직접성 0~4"는 `narrative-trauma-design.md` 산문에만 있다. spec-007의 Stage 필수 필드 목록에 해당 필드가 없다 — 데이터에 없는 값을 검사한다. |
| 008 | `Test_Narrative_EarlyHealthCluesAreLimited` | "명확한 건강 단서"를 판별할 필드가 기억 데이터 스키마(MemoryId, 시간, 장소, 사건, 핵심 대화, 행복 요소, ForeshadowingIds, RevealStage)에 없다. 무엇을 세는지 기계가 알 수 없다. |
| 004 / 012 | `Test_Remnant_ReactiveWaitsForPlayerAggression`, `Test_Encounter_StageElevenSupportsNonAggressivePass` | `PassWithoutAggression`의 "선제 공격" 정의가 spec-003에 없다. 공격 입력인가, 명중인가, 특정 반경 내인가, 시간창이 있는가. `PlayerAggression` 판정 규칙 부재. |

세 건 모두 **spec-007/008에 필드를 추가하거나 spec-003에 판정 규칙을 추가**해야 테스트가 성립한다.

### C4. Stage 13 이탈 위험 — 힌트가 1단계뿐

12개 Stage 동안 "트라우마가 나오면 뒤로 도망친다"를 근육 기억으로 굳힌 뒤,
정반대 행동을 **카메라 프레이밍 1회**로만 가르친다. 저장 필드도
`StageThirteenLoopHintSeen` bool 하나다.

> GAME-005(Twelve Minutes) 불만 키워드: "같은 대사를 반복해 다시 듣는 것",
> "막히면 루프 전체를 다시 해야 하는 것". [source: Engadget/Metacritic user reviews, 2021-08]
> ELEM-004(Loop Mechanic) 리스크: "반복 자체는 콘텐츠가 아니다 — 매 루프마다
> 새 정보·새 대사·새 빌드 중 최소 하나가 갱신되어야 한다."

Stage 13의 도주 루프는 설계상 **갱신이 0이다**(새 진실 없음, 새 공간 없음, 새 능력 없음).
GAME-005가 실패한 형태와 정확히 같다. 반복 횟수에 따른 힌트 에스컬레이션이 필요하다.
예: 2회차 오디오 큐 → 3회차 주인공 독백 → 4회차 트라우마가 멈춰 서서 기다림.

### C5. 플레이어 능력 곡선이 평탄하다

- 잔재 3종은 Stage 2·3·5에 전부 도입된다. **Stage 6~13, 즉 8개 Stage에 신규 전투 요소가 0이다.**
- 이동은 좌우 + 1단 점프로 고정. 더블 점프·대시·그래플 전부 Out of scope(spec-002).
- 성장·해금·장비 전부 Out of scope. 4~5시간 동안 플레이어의 능력은 변하지 않는다.

> GAME-033(Little Nightmares III): 분위기는 유지됐지만 "추격자와 환경의 창의성 부족",
> "반복 공식"으로 시리즈 최저 평가. [source: Metacritic user-review summary, verified 2026-07]
> [source: Into Indie Games review roundup, verified 2026-07]
> 카드 결론: **"분위기는 진입 조건이지 유지 조건이 아니다."**

Stage별 고유 추격 기믹 12개가 유일한 변주 축이다. 기믹은 저작 비용이 높고 재사용이 안 된다.
전투·이동 쪽에 저비용 변주 축(잔재 상태 태그, 지형 상호작용, 1개짜리 이동 동사)을
하나라도 두지 않으면 중반부가 무너진다.

### C6. 시그니처 이동 동사가 없다

가장 가까운 성공 레퍼런스가 이미 DB에 있다.

> GAME-057(SANABI) 카드 결론: "재사용 가능한 교훈은 '반전을 추가하라'가 아니다.
> **핵심 이동이 플레이어가 이야기를 다 이해하기 전에 이미 만족스러워야 한다**는
> 패키지를 시장 신호가 지지한다." [source: NEOWIZ press release, as of 2024-07-31]
> [source: Steam, as of 2026-08-13]

SANABI는 체인 훅이라는 단일 동사가 이동·전투·정체성을 동시에 감당한다.
《다음에》의 도주 동사는 "달리기"뿐이며, spec-002가 대안을 전부 배제했다.
ELEM-050(Core Verb as Narrative Metaphor)에 이 게임이 정확히 해당하는데,
정작 그 핵심 동사가 무장 해제(1회, 마지막 1분)에만 배정되어 있다.

**재검토 권고**: 저비용 동사 1개(짧은 회피 슬라이드, 또는 난간·구조물 붙잡기)를 추가하고,
그 동사를 Stage 7 "잡아 주었던 기억"과 Stage 13 "내려놓기"에 의미로 연결한다.
Stage 7 기믹("미끄러질 때 공격 대신 난간을 잡아 균형 회복")이 이미 그 동사를 요구하고 있는데
spec-002에 없다 — 이건 결함이기도 하다.

---

## 2. 시스템 명세 공백

### C7. 색 의존 공정성 신호 (접근성 결함)

Stage 8 공정성 신호: "올바른 다음 문은 원본 타임스탬프와 **같은 색**으로 점멸한다."
색이 유일한 구분자다. spec-013에 "접근성 대체 신호"라는 한 줄이 있으나
Implementation scope 항목일 뿐 Acceptance criterion이 없다.
색각 이상 플레이어는 Stage 8을 통과할 수 없다.

blueprint fairnessRules가 "시각 신호 1개 + 음향 신호 1개"를 요구하므로
**색이 아닌 시각 신호(형태·기호·숫자)**로 재정의해야 규칙 자체를 만족한다.

### C8. 난이도·어시스트 옵션이 0

4~5시간 추격 액션에 옵션이 없다. 유일한 접근성 계약은 spec-014의 "흔들림 강도 0 지원"이다.
동시에 spec-011은 미완료 Encounter 사망 시 **첫 Wave 전체 리셋**을 요구한다.
서사 목적의 게임에서 전투 반복은 직접적 이탈 요인이다.

DB에서 이 주제를 검색하면 최상위가 ELEM-014(Punishing Death Loop)로,
정확히 **반대 방향** 카드다. 즉 어시스트 옵션 지식이 DB에 없다(§4 참조).

### C9. `[E]` 프롬프트가 물리 키에 하드 커플링

spec-005/009/013이 `[E] 기억하기`, `[E] 내려놓기`를 **문자열 계약**으로 고정했고
`Test_UI_StageThirteenPromptSequence`가 이를 검사한다.
spec-013은 입력 리바인딩을 Out of scope로 두고 게임패드를 언급하지 않는다.

> ARCH-016(Input System): 키보드·게임패드 등 장치별 버튼을 코드 여기저기 쓰지 않고
> "이동", "상호작용" 같은 **액션 이름 하나로 미리 묶는** 입력 처리 구조.

프롬프트는 키가 아니라 액션을 참조해야 한다. 지금 계약대로 구현하면
게임패드 지원 시점에 UI 테스트 5개가 전부 깨진다.

### C10. Backflow Overlay 저작 파이프라인이 없다 — 최대 제작 리스크

검토 시점의 `narrative-trauma-design.md`는 "과거 장소 모듈과 Overlay를 재사용한다"고만 적었다.
그런데 **Overlay가 무엇인지 15개 spec 어디에도 없다.** 씬인가, 프리팹인가, 타일맵 레이어인가.

물량: 13 Stage × (Stable 공간 + Variant 공간) = 최소 26개 공간 상태.
추가로 Stage 9는 이전 Stage 지오메트리를 **결합**하고, Stage 12는 전 채널을 **총집합**한다.
이 재사용 구조가 프로젝트 총 제작비를 좌우하는데 계약이 0줄이다.

DB에 근접 카드가 셋 있으나 어느 것도 "같은 공간의 두 상태"를 다루지 않는다(§4 참조).
- ARCH-002 (Scene Streaming): 필요한 조각 씬만 additive로 켜고 끈다 — 오버레이 토글의 뼈대로 전용 가능.
- ARCH-024 (Tilemap Level Structure): 그리기·충돌·감지를 레이어로 분리 — Variant 충돌만 교체하는 근거.
- ARCH-012 (ScriptableObject Data): `ContaminationVariantId` 정의를 씬 밖 데이터로 유지.

**착수 전 이 세 카드를 조합한 Overlay 계약을 spec-006 또는 신규 spec에 확정해야 한다.**

### C11. 용어가 둘로 갈라져 있다

문서는 Memory Backflow / 기억 역류, 코드·테스트는 `Contamination*`
(`ContaminationVariantId`, `PressureStage`, `Test_Contamination_*`).
같은 개념에 두 이름이 붙어 있고 어느 spec도 둘을 연결한다고 선언하지 않는다.
지금 통일하지 않으면 구현·QA에서 계속 비용을 낸다.

### C12. 성능·해상도 예산이 없다

URP 2D 조명(Stage 1 순차 소등, Stage 5 발판 조명) + Overlay 결합 + 60fps 추격.
목표 해상도·프레임·최소 사양이 blueprint에도 spec에도 없다.
spec-013은 "대표 해상도 안전 영역"이라고만 하고 값을 주지 않는다 —
`Test_UI` 레이아웃 검사가 기준 없이 돌아간다.

### C13. 로컬라이제이션 미고려

자막 중심 4~5시간 서사. 대사 키 체계·문자열 테이블·폰트 계약이 없고
blueprint에 지원 언어 항목이 없다. 나중에 넣으면 spec-013의 하드코딩 문자열
(`[E] 기억하기` 등)과 정면 충돌한다.

### C14. 마이너 계약 결함

- blueprint `truthState`의 값 집합(`implicit`/`hidden`/`revealed`)이 정의되지 않았다.
- spec-007 필수 필드 목록에 `MemoryTitle`, `EmotionalRole`, 병원 직접성 필드가 빠졌다
  (13개 Stage 산문에는 전부 있다).
- 추격 카메라 lookahead 규칙이 없다. 좌향 도주가 기본인데 진행 방향 가시거리 계약이 없으면
  "봤을 때 이미 늦은" 장애물이 나온다. ARCH-013(2D Camera Follow / Cinemachine)을 소비해 명시할 것.
- spec-005 회상 "건너뛰기"의 **최초 재생 시 허용 여부**가 미정이다.
- spec-011은 `ChaseCheckpoint` 복원을 정의하지만 **추격 실패 자체가 미정의**라 연결이 끊긴다(C1).

---

## 3. 상업·페이싱 리스크

### R1. 환불 임계(2시간) 구간의 훅이 약하다

"Stage별 목표 플레이타임 표"의 중심값 기준 Stage 1~5 = 20+20+15+15+20 ≈ **95분**. 여기에 재시도·탐색을 더하면
환불 가능 구간이 대략 Stage 1~5다. 그 구간의 상태:

- Reveal 강도 1~2 ("Stage별 감정·공포·액션·오염·Reveal 강도 표")
- 병원 imagery 직접성 0~1/4
- 잔재 3종 중 마지막이 Stage 5에 나온다
- 명확한 건강 단서 1개(Stage 5 의료 연락)

즉 첫 2시간은 "추상 잔재 + 정체불명 이상음"만으로 버틴다.
Stage 1의 첫 Backflow·첫 추격이 예고편 수준으로 강해야 하며,
이것이 vertical slice의 실제 합격 기준이 되어야 한다.

### R2. 사후 콘텐츠가 0

단일 엔딩, Stage 선택·갤러리·NG+ 전부 배제(spec-005/015 Out of scope).
**기억 13개를 모으는 게임인데 모은 것을 다시 볼 방법이 없다.**
ELEM-032(New Game+)는 이 저장 구조에서 저비용 후보다 — 이미
`CollectedMemoryFragments`, `NarrativeRevealState`, `EndingCompleted`를 저장한다.
최소한 엔딩 후 기억 목록 열람은 spec-005 데이터로 거의 공짜다.

### R3. 착수 차단 항목이 openDecisions에 숨어 있다

blueprint `openDecisions` 5개 중 둘은 "열린 결정"이 아니라 **차단**이다.

- **주인공·친구 이름**: 미정이면 13개 Stage 대사 원고를 쓸 수 없다.
- **픽셀 아트 vs 고해상도 2D**: 미정이면 26개 공간 상태의 배경 물량 견적이 불가능하다.
  ELEM-013(Pixel Art Style)은 이 선택이 제작비와 파이프라인 요구를 직접 낮춘다고 기록한다.

나머지 셋(의학적 명칭, 무기 외형, 최종 원고)은 병행 가능하다.

### R4. 관계 구축 구간이 "지연된 게임플레이"로 읽힐 수 있다

> ELEM-048(Mundane Bonding / Horror Contrast) 리스크:
> "상호작용이 적은 유대 장면을 반복하면 도입부가 **지연된 게임플레이**처럼 느껴진다."
> "호러는 **특정한 공유 디테일**을 변형해야 한다. 일반적인 괴물은 관계 구축을 회수하지 못한다."

《다음에》는 후자를 잘 지킨다(각 Stage의 BackflowMeaning이 그 기억의 디테일을 변형).
전자는 위험하다 — Stage 3·4·6·7이 15~20분짜리 "행복한 일상" 4연속이다.

또한 같은 카드가 **콘텐츠 경고**를 필수로 본다.
GENRE-040은 GAME-058·059·060 세 작품이 모두 주제 경고를 게시한다고 기록한다.
[source: Steam, as of 2026-08-13] blueprint·spec 어디에도 콘텐츠 경고 항목이 없다. **추가 필요.**

### R5. 서사 검증 리스크 (참고 — 현 설계는 방어됨)

> ELEM-049 리스크: "수집 목록 자체는 재구성이 아니다. 플레이어가 조각을 **사용해
> 믿음이나 행동을 수정**해야 한다." / "이전 단서가 엔딩 전에는 한 가지 의미만,
> 엔딩 후에는 무관한 의미를 가지면 공정성이 깨진다."

《다음에》는 Stage 8의 시간순 문 선택과 Stage 9의 재맥락화로 전자를 만족하고,
"행복한 기억을 거짓으로 뒤집지 않는다"는 nonGoal로 후자를 만족한다. **이 부분은 견고하다.**

---

## 4. RAG 카드 매핑 — 기존 DB에서 회수

인용 가능한 카드 ID만 사용했다. 신규 카드는 만들지 않았다.

| 결함 | 회수한 카드 | 카드가 제공하는 것 |
|---|---|---|
| C4 Stage 13 루프 이탈 | GAME-005, ELEM-004 | 갱신 없는 반복의 실패 형태와 불만 키워드 |
| C5 능력 곡선 평탄 | GAME-033, GENRE-014 | "분위기는 진입 조건이지 유지 조건이 아니다" |
| C6 시그니처 동사 부재 | GAME-057, ELEM-050 | 이동이 서사 이해보다 먼저 만족스러워야 한다 |
| C9 `[E]` 하드코딩 | ARCH-016 | 액션 이름 기반 입력 바인딩 |
| C10 Overlay 파이프라인 | ARCH-002, ARCH-024, ARCH-012 | additive 토글 + 레이어 분리 + 데이터 외재화 |
| C14 추격 카메라 | ARCH-013 | Cinemachine 추종·경계 구속 |
| 상태·플래그 관리 전반 | ARCH-032 | Truth·기억·1회성 비트의 단일 원장, 중복 완료 방지 |
| 저장 구조 | ARCH-004 | JSON 직렬화·복원 |
| 잔재 상태기계 | ARCH-005, ARCH-028 | 상태 전이 / IDamageable 계약 |
| 오디오 큐 | ARCH-017 | BGM·SFX 분리와 풀 |
| R2 사후 콘텐츠 | ELEM-032 | New Game+ |
| R3 아트 방식 결정 | ELEM-013 | 픽셀 아트의 제작비·파이프라인 효과 |
| R4 도입부·콘텐츠 경고 | ELEM-048, GENRE-040 | 유대 장면 지연 리스크, 경고 게시 관례 |
| 서사 공정성(방어됨) | ELEM-049 | 재구성 조건과 단서 공정성 |
| 장르 좌표 | GENRE-040, GENRE-014 | 클러스터 관례와 기대치 |

### DB 공백 → 보충 완료 (2026-08-17)

검토 시점에 DB에 없던 지식 3건을 카드로 채웠다. 세 장 모두 lint·section·link 검사를
통과했고 DB 미러링에서 `unresolved_refs: 0`을 확인했다.

| 카드 | 채운 결함 | 핵심 근거 |
|---|---|---|
| **ELEM-051** Unkillable Pursuer Chase | C1, C2, C4 | Alien: Isolation의 director-AI / pursuer-AI 2계층 분리, menace gauge, "긴장이 정점에 이르면 추격자를 물린다", 전 캠페인 텔레포트 2회 제한이라는 공정성 계약 [source: Game Developer, Tommy Thompson, as of 2017-10-31] |
| **ELEM-052** Assist and Accessibility Options | C7, C8, C9 | Game Accessibility Guidelines의 Basic 항목 "Ensure no essential information is conveyed by a fixed colour alone", "Allow controls to be remapped / reconfigured" [source: gameaccessibilityguidelines.com full list, as of 2026-08-17] / Hades God Mode 수치와 Kasavin 발언 [source: Inverse, as of 2021-08-11] |
| **ARCH-033** Level State Overlay | C10 | `LoadSceneMode.Additive` = "Adds the Scene to the current loaded Scenes" [source: Unity Scripting Reference, as of 2026-08-17] / Prefab Variant 상속·override [source: Unity Manual, as of 2026-08-17] / baseline chunk·broadcast·logging 규칙 |

세 카드가 새로 제공하는, 검토 시점에 없던 결론:

1. **ELEM-051** — 추격 게임에는 추격자 AI 말고 **director**가 따로 있어야 한다.
   C2가 요구한 "속도·거리·러버밴딩"보다 상위 문제는 **누가 추격을 끝내기로 결정하는가**다.
   《다음에》 spec-006은 출현점·도주 방향·탈출점만 정하고 압박 조절 주체를 정하지 않았다.
   또 같은 카드의 SOMA 근거가 이 프로젝트에 직접 겨눠진다 —
   > "사람들이 싫었다고 말할 때, 거의 언제나 몬스터 조우 때문이다 — 핵심이 아닌 부분."
   > [source: Frictional Games, "SOMA One Year Later", as of 2016-09-23]
   SOMA는 추격이 비핵심이어서 옵션으로 뺄 수 있었다. 《다음에》는 추격이 **핵심 12회**라
   같은 탈출구가 없다. 그만큼 추격 1회당 읽을거리가 갱신되어야 한다.
2. **ELEM-052** — 색 단독 신호 금지와 리매핑은 **Basic 등급**이다. C7·C9는 "있으면 좋은 것"이
   아니라 업계 기준선 미달이다. 또 Celeste·Hades·SOMA 세 사례 전부 **출시 후 추가**였고,
   그래서 비용이 컸다는 것이 이 카드의 실패 사례다.
3. **ARCH-033** — C10의 답이 "추가 씬이냐 프리팹이냐"가 아니라 **차이의 크기로 나눈다**는 것.
   공간 전체 차이(복도 추가, 충돌 교체, 조명 리그)는 additive overlay 씬,
   오브젝트 단위 차이는 prefab variant. 그리고 결정성 규칙 —
   "같은 state ID는 실패 후 재시도를 포함해 매 진입마다 동일한 충돌·타이밍·스폰을 만든다" —
   가 spec-006의 `Test_Contamination_RetryUsesSameVariant`에 그대로 대응한다.

---

## 5. 조치 결과

이 검토의 C·R 항목은 전부 닫혔다. 결정과 근거는 [`design-decisions.md`](design-decisions.md),
spec별 반영 내용은 같은 문서 §3, 착수·삭감 순서는 [`README.md`](README.md)에 있다.
이 문서는 그 결정들이 왜 필요했는지를 남기는 기록이며, 작업 지시서가 아니다.

**후순위**
12. C5 저비용 전투 변주 축, R2 엔딩 후 기억 열람, C14 나머지
