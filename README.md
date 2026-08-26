<div align="center">

<img src="Docs/var/concept-art/daeume/01-stage01-stable-memory.png" alt="노을 진 버스 정류장과 기억의 조각" width="100%">

# 《다음에》

### 행복한 기억을 되찾을수록, 세계는 망가진다.

열두 번 도망치는 법을 배운 뒤 마지막 한 번은 스스로 뒤돌아 걷는  
**2D 픽셀 아트 내러티브 액션 플랫포머**

![Unity](https://img.shields.io/badge/Unity-6000.5.5f1-111111?style=flat-square&logo=unity)
![C%23](https://img.shields.io/badge/C%23-Gameplay-512BD4?style=flat-square&logo=csharp)
![URP](https://img.shields.io/badge/2D_URP-17.6.0-0B84F3?style=flat-square)
![Status](https://img.shields.io/badge/Status-Vertical_Slice-E0954A?style=flat-square)
![Team](https://img.shields.io/badge/Team-3_Developers-2B1E2A?style=flat-square)

한국어 · PC · 키보드/게임패드 · 3개 스테이지 포트폴리오 빌드

</div>

> **Content note**  
> 상실, 죽음, 중환자실을 연상시키는 장면과 추격 연출을 포함합니다.

## 프로젝트 개요

《다음에》는 **행복한 기억을 복원하는 행위가 오히려 현실을 오염시킨다**는 역설에서 출발합니다.
플레이어는 익숙한 거리를 탐색하고, 기억의 잔재와 싸우며, 회상을 되찾은 뒤 형체 없는 트라우마에게 쫓깁니다.

게임은 반복해 학습시킨 행동을 결말에서 뒤집습니다. 앞선 스테이지가 “도망쳐서 살아남는 법”을 가르쳤다면,
마지막 스테이지는 공격과 탈출을 무효화하고 플레이어가 직접 무기를 내려놓은 뒤 두려움 쪽으로 걷게 합니다.

| 항목 | 내용 |
| --- | --- |
| 장르 | 2D 내러티브 액션 플랫포머 · 추격 · 심리 공포 |
| 핵심 루프 | 탐색 → 전투 → 회상 → 기억 역류 → 추격 → 탈출 |
| 현재 범위 | `Stage01 → Stage10 → Stage13`으로 이어지는 3개 스테이지 |
| 엔진 | Unity `6000.5.5f1`, Universal Render Pipeline `17.6.0` |
| 개발 방식 | 3인 역할 분담, 데이터 기반 제작, EditMode/PlayMode 기능 검증 |

## 플레이 경험

### 1. 기억을 향해 전진한다

낮의 주택가에서 이동·점프·매달리기를 익히고 세 차례 Encounter를 통과합니다.
빛나는 기억의 매개체에 도달하면 평온했던 장면이 짧은 회상으로 재생됩니다.

### 2. 되찾은 기억이 공간을 오염시킨다

회상이 끝나는 순간 `Stable → Echo → Intrusion`으로 압박이 상승합니다.
조명, 사운드, 카메라, 적의 움직임이 같은 상태 계약을 공유하며 익숙했던 길을 낯선 추격 공간으로 바꿉니다.

### 3. 달아나는 습관을 스스로 끝낸다

밤의 도로에서 병원으로 이어진 시간의 진실을 마주한 뒤 마지막 스테이지에 도착합니다.
계속 달아나면 같은 장소로 돌아옵니다. 게임은 정답을 문장으로 지시하지 않고 카메라, 음악, 적의 태도로만 다음 행동을 제안합니다.

<div align="center">

<img src="Docs/var/concept-art/daeume/02-trauma-concept-amber.png" alt="버스 정류장에서 트라우마와 마주한 주인공" width="100%">

<sub>아트 디렉션 콘셉트 — 따뜻한 기억의 색과 검은 트라우마 실루엣의 대비</sub>

</div>

## 핵심 시스템

| 시스템 | 구현 포인트 |
| --- | --- |
| 이동과 매달리기 | 좌우 이동, 지상 점프, 통과형 플랫폼, `GrabbableSurface` 기반 매달리기 |
| 전투 | 근접 공격, 체력·무적 시간, 피격 피드백, 근접·돌진·원거리 잔재 3종 |
| 기억 | 문자열 테이블 기반 회상, 진행/건너뛰기, 중복 수집 방지, 추격 전환 이벤트 |
| 기억 역류 | 공간 복제 없이 씬 내부 Variant를 전환하는 `Stable / Echo / Intrusion` 단계 |
| 추격 Director | 거리·압박·목표 시간을 Director가 소유하고 Trauma 액터는 지시만 수행 |
| 저장과 흐름 | New Game/Continue, 체크포인트 복원, 중복 씬 전환 차단, 3개 스테이지 진행 체인 |
| 접근성 | 동작 키 재배정, 카메라 흔들림 강도, 자막 크기 3단계, 추격 속도 보조 |

### 설계에서 중요하게 본 것

- **상태를 섞지 않았습니다.** `StageState`, `EncounterState`, `PressureStage`는 서로 독립적으로 움직여 전투와 오염이 스테이지 진행을 덮어쓰지 않습니다.
- **추격의 긴장을 액터 AI에 맡기지 않았습니다.** `ContaminationDirector`가 목표 시간과 거리를 조절해 정지 플레이를 안전지대로 만들지 않으면서도 막힌 길에서 즉사시키지 않습니다.
- **서사와 조작을 같은 규칙으로 만들었습니다.** 매달리기, 도주, 무장 해제는 별도 컷신이 아니라 플레이어가 직접 수행하는 핵심 동사입니다.
- **기능 검증을 구현의 일부로 다뤘습니다.** Unity Test Framework 테스트와 장면별 QA 계약으로 상태 전이, 입력, 레이아웃, 회귀, 빌드를 확인합니다.

## 스테이지 구성

| Stage | 공간 | 플레이 역할 |
| ---: | --- | --- |
| 01 | 노을 진 주택가와 버스 정류장 | 이동·매달리기·전투·회상·첫 추격을 한 번에 학습하는 시작 스테이지 |
| 10 | 병원으로 이어지는 밤 도로와 육교 | 적 3종을 누적해 전투 밀도를 높이고, 시간 기록을 통해 사건의 인과를 다시 해석하는 중간 스테이지 |
| 13 | 반복되는 마지막 거리 | 도주·공격 규칙을 뒤집고 접근과 무장 해제로 끝맺는 수용 엔딩 |

```text
Boot → Title
          └─ Stage01 ── Encounter ×3 ── Memory ── Chase ── Escape
                                                    ↓
             Stage10 ── Night Road ── Memory ── Chase ── Escape
                                                    ↓
             Stage13 ── Runaway Loop ── Approach ── Lower Weapon ── Ending
```

## 조작법

| 동작 | 키보드 | 게임패드 |
| --- | :---: | :---: |
| 이동 | `A` `D` / 방향키 | Left Stick |
| 점프 | `Space` | South Button |
| 공격 | `J` | West Button |
| 매달리기 | `K` | East Button |
| 상호작용 / 회상 진행 | `E` | North Button |
| 일시정지 / 회상 건너뛰기 | `Esc` | Start |

동작 키 4종(점프·공격·매달리기·상호작용)은 게임 내 접근성 옵션에서 다시 지정할 수 있습니다.

## 실행 방법

### 요구 환경

- [Unity Hub](https://unity.com/download)
- Unity Editor `6000.5.5f1`
- [Git LFS](https://git-lfs.com/)

### 로컬 실행

```bash
git clone https://github.com/JugleGame/project-daeum.git
cd project-daeum
git lfs pull
```

1. Unity Hub에서 저장소 루트를 프로젝트로 추가합니다.
2. Unity `6000.5.5f1`로 프로젝트를 엽니다.
3. `Assets/Scenes/Boot.unity`를 엽니다.
4. Play Mode를 시작하고 Title 화면에서 **New Game**을 선택합니다.

> `Boot.unity`가 영속 시스템과 Title 흐름을 초기화합니다. 개별 Stage 씬부터 실행하면 정상 플레이 흐름이 보장되지 않습니다.

## 기술 구성

| 영역 | 기술 |
| --- | --- |
| Runtime | Unity 6, C#, URP 2D, Input System |
| 콘텐츠 | Tilemap, ScriptableObject, Prefab, 2D Animation, Timeline |
| UI | Unity UI, 문자열 테이블, 런타임 접근성 옵션 |
| 품질 | Unity Test Framework, EditMode/PlayMode 테스트, 기능별 QA 계약 |
| 협업 | Git/Git LFS, 역할별 Scene·Prefab 소유권, 작은 단위 통합 |

## 저장소 구조

```text
project-daeum/
├─ Assets/
│  ├─ Art/                  # 캐릭터·잔재·배경·타일 픽셀 아트
│  ├─ Audio/                # 상태별 BGM·행동 SFX와 출처 기록
│  ├─ Data/                 # Stage·Encounter·Variant ScriptableObject
│  ├─ Prefabs/              # Player·Enemy·Memory·UI·Presentation
│  ├─ Scenes/               # Boot·Persistent·Title·Stage01·10·13
│  ├─ Scripts/              # Core·Player·Encounter·Memory·Flow·UI
│  └─ Tests/                # EditMode·PlayMode 기능 검증
├─ Docs/
│  ├─ qa/                   # Issue별 기능 QA와 실행 증거
│  └─ collaboration-workflow.md
├─ Packages/                # Unity 패키지 잠금 정보
└─ ProjectSettings/         # Unity 6000.5.5f1 프로젝트 설정
```

## 팀과 역할

이 프로젝트는 시스템 경계를 먼저 합의한 뒤 세 개의 개발 Lane을 병렬로 운영했습니다.

| 팀원 | 역할 | 주요 기여 |
| --- | --- | --- |
| [@bbie-6772](https://github.com/bbie-6772) | Role A — 시스템·플레이어 | 상태·입력, 이동·매달리기, 전투, 상호작용, 저장, 씬 흐름, 캐릭터 애니메이션 |
| [@ys143112](https://github.com/ys143112) | Role B — 추격·레벨 | StageData, 레벨·Encounter, 잔재 3종, 기억 역류 Director, 추격, 스테이지 통합 |
| [@SmallWaterracoon](https://github.com/SmallWaterracoon) | Role C — 내러티브·프레젠테이션 | 회상, HUD, 접근성, 오디오·카메라 연출, BGM·SFX 이벤트 연결 |

역할 간 연동은 직접 참조를 늘리는 대신 `EventBus`, `MemoryCompleted`, `ContaminationPressureChanged` 등 명시적인 계약으로 연결했습니다. 자세한 협업 구조는 [Docs/collaboration-workflow.md](Docs/collaboration-workflow.md)에서 확인할 수 있습니다.

## QA와 문서

- [Stage01 플레이 완주 계약](Docs/qa/issue-56-stage01-playable-slice.md)
- [Stage10 텍스트 UI 기능 계약](Docs/qa/issue-66-stage10-text-ui.md)
- [Stage13 수용 엔딩 기능 계약](Docs/qa/issue-58-stage13-acceptance-ending.md)
- [Role A 기능 검증](Docs/qa/role-a-stage1-functional-test.md)
- [Role B 통합 QA](Docs/role-b/99-integration-qa.md)
- [Role C 통합 QA](Docs/qa/role-c-integration-qa.md)

## 크레딧과 사용 범위

- 오디오 출처와 사용 내역: [Assets/Audio/CREDITS.md](Assets/Audio/CREDITS.md)
- 본 저장소에는 별도의 오픈소스 라이선스가 선언되어 있지 않습니다. 코드와 아트의 재사용·재배포 권한을 자동으로 부여하지 않습니다.

<div align="center">

---

**JugleGame — 기억을 되찾는 일이 언제나 회복을 뜻하지는 않는다.**

</div>
