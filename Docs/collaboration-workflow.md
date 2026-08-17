# 《다음에》 Stage 1 버티컬 슬라이스 협업 실행도

> 기준일: 2026-08-17
> 대상: JugleGame/project-daeum
> 목적: 역할별 선행 작업, 병렬 가능 작업, 통합 시점을 하나의 실행 기준으로 고정한다.

## 1. 현재 출발점

협업 기반 설정은 완료되어 있다. Universal 2D, Input Actions, Git LFS, Force Text, Visible Meta Files, Sorting Layer, asmdef, 기본 Scene과 contract stub을 공통 기준선으로 사용한다.

이 문서의 원칙은 다음과 같다.

- contract를 먼저 고정하고 구현을 병렬화한다.
- Scene과 Prefab은 한 시점에 한 명만 소유한다.
- 각 lane은 작은 단위로 dev에 통합하며, 장기 branch를 만들지 않는다.
- build 성공만으로 완료 처리하지 않고, 실제 플레이 증거와 Acceptance Criteria를 함께 확인한다.
- 접근성 baseline은 일정 단축 대상에서 제외한다.

## 2. 역할과 소유권

| 역할 | 주 책임 | Spec | 전용 Scene / 주요 자산 | 다른 역할에 제공할 contract |
|---|---|---|---|---|
| A — 시스템·플레이어 | 상태, 입력, 이동, 잡기, 전투, 상호작용, 저장·흐름 | 001, 002, 003, 010, 011, 015 | Boot, Persistent | EventBus, IInteractable, StageState, SaveData, GameManager |
| B — 추격·레벨 | StageData, blockout, Remnant, Encounter, ContaminationDirector, 추격 | 004, 006, 007, 012 | Stage01_Base, Echo/Intrusion overlay | EncounterState, PressureStage, distance/pressure event |
| C — 내러티브·프레젠테이션 | 기억, 대사, HUD, 접근성, 오디오·카메라, art pipeline | 005, 008, 013, 014 | Title, UI·art·audio asset | StringTable, MemoryComplete event, AssistSettings |

Scene/Prefab 충돌 방지 규칙:

- Boot와 Persistent는 A만 수정한다.
- Stage01_Base 및 Echo/Intrusion overlay는 B만 수정한다.
- Title은 C만 수정한다.
- 공용 Prefab 변경은 담당자를 먼저 지정하고, 나머지는 variant 또는 child Prefab으로 작업한다.

## 3. 전체 의존성과 병렬 lane

~~~mermaid
flowchart LR
    F["공통 기반 완료<br/>Universal 2D · Input · Git/LFS · asmdef"]
    G0{"G0 Contract Freeze<br/>stub compile · event 이름 · data schema"}

    subgraph A["A — 시스템·플레이어 lane"]
      A1["A1 상태·입력<br/>001"]
      A2["A2 이동·잡기<br/>002"]
      A3["A3 전투<br/>003"]
      A4["A4 상호작용<br/>010"]
      A5["A5 저장·흐름<br/>011 · 015"]
      A1 --> A2 --> A3 --> A4 --> A5
    end

    subgraph B["B — 추격·레벨 lane"]
      B1["B1 StageData·blockout<br/>004 · 012"]
      B2["B2 Remnant<br/>006"]
      B3["B3 Encounter<br/>007"]
      B4["B4 ContaminationDirector<br/>007 통합"]
      B5["B5 추격 slice<br/>008 기록 범위"]
      B1 --> B2 --> B3 --> B4 --> B5
    end

    subgraph C["C — 내러티브·프레젠테이션 lane"]
      C1["C1 palette·StringTable<br/>005"]
      C2["C2 핵심 sprite<br/>art pipeline"]
      C3["C3 Memory<br/>013"]
      C4["C4 HUD·접근성<br/>014"]
      C5["C5 오디오·카메라<br/>014 polish"]
      C1 --> C2
      C1 --> C3 --> C4 --> C5
    end

    G1{"G1 이동 가능한 blockout"}
    G2{"G2 전투 loop"}
    G3{"G3 Memory → 오염 전환"}
    G4{"G4 Vertical Slice"}
    G5{"G5 Functional QA · Build"}

    F --> G0
    G0 --> A1
    G0 --> B1
    G0 --> C1

    A2 -. "movement API" .-> B2
    A3 -. "damage / HP" .-> B2
    A3 -. "HP event" .-> C4
    A4 -. "interaction prompt" .-> C3
    C3 -. "MemoryComplete" .-> B4
    B3 -. "EncounterState" .-> A5
    B4 -. "pressure / distance" .-> C5
    C3 -. "memory state" .-> A5
    C4 -. "AssistSettings" .-> A5

    A2 --> G1
    B1 --> G1
    C2 --> G1

    A3 --> G2
    B2 --> G2
    C4 --> G2

    B3 --> G3
    C3 --> G3
    A4 --> G3

    A5 --> G4
    B5 --> G4
    C5 --> G4
    G1 --> G2 --> G3 --> G4 --> G5

    classDef gate fill:#2b2d42,stroke:#edf2f4,color:#fff,stroke-width:2px;
    classDef roleA fill:#d9edff,stroke:#2979b9,color:#102a43;
    classDef roleB fill:#dff5e1,stroke:#3d8b40,color:#17351a;
    classDef roleC fill:#ffe5d5,stroke:#b85c2a,color:#4b2311;
    class G0,G1,G2,G3,G4,G5 gate;
    class A1,A2,A3,A4,A5 roleA;
    class B1,B2,B3,B4,B5 roleB;
    class C1,C2,C3,C4,C5 roleC;
~~~

실선은 같은 lane 안에서 반드시 순차적으로 진행해야 하는 작업이다. 점선은 다른 역할이 기다리지 않고 임시 mock으로 진행할 수 있지만, 해당 contract가 실제 구현되면 통합 확인이 필요한 handoff다.

## 4. 순차 통합 Gate

| Gate | 들어오기 전에 반드시 완료 | 병렬로 준비 가능한 것 | Gate 통과 증거 |
|---|---|---|---|
| G0 Contract Freeze | 공통 stub compile, event·enum·data schema 합의 | A/B/C 각자 test fixture와 mock 작성 | 전체 compile, contract 목록 review |
| G1 이동 가능한 blockout | A 이동, B Stage01 blockout, C player/environment 핵심 sprite | C palette·StringTable, B encounter marker | Stage01에서 입력→이동→카메라 확인 |
| G2 전투 loop | A 공격·피격, B Remnant, C HP/HUD | Memory 연출, SaveData schema | 공격→피격→상태 변화 실제 플레이 |
| G3 Memory→오염 전환 | A 상호작용, B Encounter/Director, C MemoryComplete | 오디오 cue, chase overlay | 기억 완료 event가 오염 단계 전환 |
| G4 Vertical Slice | A 저장·흐름, B 추격, C HUD·오디오·카메라 | 접근성 점검과 regression test | Title→Stage1→기억→오염→추격 loop |
| G5 Release Candidate | 모든 Acceptance Criteria, functional QA, 오류 정리 | 문서와 캡처 정리 | 플레이 증거, test, build artifact |

Gate를 통과하지 못한 상태에서 다음 Gate의 통합 branch를 열지 않는다. 다만 다음 단계의 독립 asset, mock, test fixture는 병렬로 준비한다.

## 5. 병렬화 가능한 Work Package

### G0 직후: 완전 병렬

- A: StateMachine, InputRouter, player movement prototype.
- B: StageData schema를 소비하는 Stage01 blockout, spawn marker 배치.
- C: palette, StringTable, player/environment 핵심 sprite 제작.
- 공통 조건: stub signature를 바꿀 때는 세 역할 모두에게 먼저 공유한다.

### G1 이후: 제한 병렬

- A: combat과 interaction을 순차 구현한다.
- B: Remnant를 combat contract에 연결하고 Encounter scaffold를 준비한다.
- C: Memory와 HUD를 병렬 제작하되, HP/event 이름은 A contract를 따른다.
- 통합 지점: G2에서 damage, HP, enemy state를 실제 플레이로 함께 검증한다.

### G3 이후: 통합 병렬

- A: SaveData와 scene flow.
- B: ContaminationDirector와 chase.
- C: pressure 기반 audio/camera, 접근성 옵션.
- 통합 지점: 같은 pressure 단계와 AssistSettings를 세 lane이 동시에 소비하므로 daily integration이 필수다.

## 6. Handoff Contract Matrix

| 제공자 → 소비자 | 전달 항목 | 전달 시점 | 소비자 진행 방식 | 통합 확인 |
|---|---|---|---|---|
| A → B | movement, damage, HP contract | G0 stub / G1 implementation | B는 stub으로 enemy prototype | Remnant가 실제 player state를 변경 |
| A → C | HP event, IInteractable, prompt state | G0 stub / G2 implementation | C는 mock event로 HUD·Memory 제작 | 실제 event로 HUD와 prompt 갱신 |
| B → A | EncounterState, stage completion | G0 stub / G3 implementation | A는 enum 기반 flow 작성 | Encounter 종료 후 save/flow 반영 |
| B → C | PressureStage, distance, chase state | G0 stub / G3 implementation | C는 debug slider로 연출 제작 | Director 값으로 audio/camera 변화 |
| C → B | MemoryComplete, narrative flags | G0 stub / G3 implementation | B는 debug trigger로 Director 제작 | 실제 기억 완료가 오염 시작 |
| C → A | memory state, AssistSettings | G0 schema / G3 implementation | A는 default value로 save 작성 | save/load 후 옵션과 진행 상태 유지 |

모든 handoff에는 네 가지가 필요하다: contract 위치, sample payload, 발생 조건, 소비 결과. 이름만 전달하고 의미를 구두로 남기는 방식은 금지한다.

## 7. 8일 실행 일정

~~~mermaid
gantt
    title Stage 1 Vertical Slice — 2026-08-17 ~ 2026-08-24
    dateFormat  YYYY-MM-DD
    axisFormat  %m/%d

    section 공통 Gate
    G0 Contract Freeze                    :milestone, g0, 2026-08-17, 0d
    G1 이동 가능한 blockout               :milestone, g1, 2026-08-18, 0d
    G2 전투 loop                          :milestone, g2, 2026-08-20, 0d
    G3 Memory → 오염                     :milestone, g3, 2026-08-22, 0d
    G4 Vertical Slice                     :milestone, g4, 2026-08-23, 0d
    G5 Functional QA · Build              :milestone, g5, 2026-08-24, 0d

    section A — 시스템·플레이어
    상태·입력·이동                        :a1, 2026-08-17, 2d
    잡기·전투                             :a2, after a1, 2d
    상호작용                              :a3, 2026-08-20, 2d
    저장·scene flow                       :a4, 2026-08-22, 2d
    QA 대응                               :a5, 2026-08-24, 1d

    section B — 추격·레벨
    StageData·blockout                    :b1, 2026-08-17, 2d
    Remnant·Encounter                     :b2, 2026-08-19, 3d
    Director·Overlay                      :crit, b3, 2026-08-22, 1d
    Chase 통합                            :b4, 2026-08-23, 1d
    QA 대응                               :b5, 2026-08-24, 1d

    section C — 내러티브·프레젠테이션
    palette·StringTable·핵심 sprite       :c1, 2026-08-17, 2d
    Memory·HUD                            :c2, 2026-08-19, 3d
    Audio·Camera·접근성                   :crit, c3, 2026-08-22, 2d
    QA 대응                               :c4, 2026-08-24, 1d
~~~

가장 큰 일정 위험은 8월 22일의 Director·Overlay와 audio·camera 연결이다. 이 날 이전까지 pressure contract와 debug control을 고정하지 못하면 B와 C가 동시에 막힌다.

## 8. 역할별 상세 진행 순서

### A — 시스템·플레이어

1. G0에서 State, Input, EventBus, SaveData contract를 고정한다.
2. 상태·입력을 만든 뒤 이동·잡기를 붙인다.
3. 이동이 G1을 통과하면 combat을 구현한다.
4. combat event를 B의 Remnant와 C의 HUD에 handoff한다.
5. interaction을 Memory에 연결한다.
6. Encounter와 Memory state가 안정된 뒤 save와 scene flow를 완성한다.
7. 마지막으로 full loop의 입력 복구, pause, retry, save/load를 기능 검증한다.

### B — 추격·레벨

1. StageData와 Stage01_Base blockout을 먼저 만든다.
2. A의 movement stub을 이용해 동선, 낙하, 카메라 경계를 검증한다.
3. combat contract가 들어오면 Remnant를 연결한다.
4. Encounter scaffold를 만든 뒤 MemoryComplete를 기다린다.
5. C의 실제 event와 연결해 ContaminationDirector를 완성한다.
6. pressure와 distance를 C에 제공하고 chase slice를 통합한다.
7. Stage01_Base 변경은 B만 수행하며 overlay는 additive scene 또는 전용 Prefab으로 분리한다.

### C — 내러티브·프레젠테이션

1. palette, StringTable, naming 규칙을 먼저 고정한다.
2. G1에 필요한 player·terrain 핵심 sprite를 우선 공급한다.
3. mock interaction으로 Memory를 제작하고 MemoryComplete event를 제공한다.
4. A의 실제 HP/event가 들어오면 HUD를 연결한다.
5. B의 pressure debug value로 audio·camera를 먼저 제작한다.
6. 실제 Director 값과 AssistSettings를 연결한다.
7. 접근성 baseline, readability, flash·shake 제한을 full loop에서 점검한다.

## 9. Daily Integration Process

~~~mermaid
flowchart TD
    S["09:30 contract·blocker 15분 확인"]
    W["각 역할 전용 branch 작업"]
    T["역할별 edit/play test"]
    H{"handoff contract 변경?"}
    R["소비자에게 sample payload와 변경 이유 공유"]
    P["작은 PR → dev"]
    I["dev 통합 smoke test"]
    E{"기능 증거 확보?"}
    C["Gate board 갱신"]
    B["원인 role이 당일 수정"]

    S --> W --> T --> H
    H -- "예" --> R --> P
    H -- "아니오" --> P
    P --> I --> E
    E -- "예" --> C
    E -- "아니오" --> B --> T
~~~

권장 branch 단위는 하나의 검증 가능한 결과다. 예: player movement, Remnant hit reaction, Memory interaction처럼 실제 플레이에서 독립적으로 확인할 수 있어야 한다.

## 10. 완료 판정 순서

각 기능은 아래 순서를 모두 통과해야 완료다.

1. compile 및 Console error 0.
2. 해당 역할의 focused test.
3. Unity Editor에서 실제 입력과 상태 전이를 확인.
4. 다른 역할 contract와 통합 smoke test.
5. Acceptance Criteria별 증거 기록.
6. full loop regression.
7. 마지막에 player build 확인.

build_project 또는 playmode test 한 종류만 통과한 결과는 functional PASS로 간주하지 않는다.

## 11. 일정 단축 시 Cut Order

다음 순서로 축소한다.

1. terrain variation 추가분.
2. Stage 1 기억 서술의 추가 문장.
3. 일부 audio cue.
4. Title animation.

다음 항목은 축소하지 않는다.

- 입력, 이동, combat, Memory→오염→추격의 core loop.
- 정보 전달에 필요한 HUD.
- 접근성 baseline과 flash·shake 제한.
- save/flow의 핵심 진행 상태.
- 기능 검증과 회귀 확인.

## 12. 즉시 실행 Checklist

1. main에서 dev branch를 만들고 branch protection 기준을 정한다.
2. G0 contract 목록을 세 역할이 함께 review한다.
3. A는 state/input/movement branch를 시작한다.
4. B는 StageData/blockout branch를 시작한다.
5. C는 palette/StringTable/core sprite branch를 시작한다.
6. G1에서 세 결과를 합친 뒤 combat lane으로 이동한다.
