# 《다음에》 협업 초기 설정

3인 + AI 파이프라인 / 8일 / Stage 1 버티컬 슬라이스.
빌드 범위와 결정 근거는 [`design-decisions.md`](design-decisions.md), 시스템별 포함 여부는
각 spec의 `## Build scope` 절이 단일 원본이다.

## 0. 확정 사항

| 항목 | 값 |
|---|---|
| 내부 해상도 | 480x270 (1920x1080의 정확히 1/4, 정수배 업스케일) |
| 캐릭터 높이 | 32px / PPU 32 |
| Unity 템플릿 | Universal 2D (URP) |
| 저장소 | 별도 GitHub repo + Git LFS |
| 분업 축 | 시스템(A) / 추격·레벨(B) / 서사·UI·오디오(C) |
| 브랜치 | 짧은 feature 브랜치 + PR, 하루 안에 `dev` 병합 |
| 팔레트 | 노을 호박 → 병원 시안 |

---

## 1. 픽셀 아트 규칙

### 격자와 카메라

- PPU(Pixels Per Unit) **32**. 캐릭터 32px = 1 Unity unit.
- 타일 **16x16** (= 0.5 unit).
- 카메라는 Orthographic, `size = 270 / 2 / 32 = 4.21875`. 이 값을 바꾸면 픽셀이 어긋난다.
- Pixel Perfect Camera 컴포넌트: Reference Resolution `480 x 270`, Assets PPU `32`,
  Upscale Render Texture 켬, Pixel Snapping 켬.
- 스프라이트 임포트 기본값: Filter Mode `Point (no filter)`, Compression `None`,
  Mesh Type `Full Rect`, Pivot `Bottom` (캐릭터) / `Center` (이펙트).

### 그리기 규칙

- 안티에일리어싱 금지. 반투명 픽셀 금지 (알파는 0 또는 255).
- 외곽선 1px. **순수 검정을 쓰지 않는다** — 인접 색의 어두운 변형을 쓴다(selective outline).
  팔레트의 `outline`은 최암부 기준값이고 배경 밝기에 따라 더 밝은 변형을 허용한다.
- 회전 금지. 스프라이트를 각도로 돌리지 않고 프레임을 따로 그린다.
- 스케일은 정수배만. 0.5배·1.5배 스케일 금지.
- 딜리트(dithering)는 Contamination 전이 구간에만 쓴다. Stable 공간에서는 쓰지 않는다.
- 서브픽셀 이동 금지. 모든 이동은 Pixel Snapping이 처리한다.

### 팔레트 — 16색

Stable 8색과 Contamination 8색이 **같은 인덱스로 대응**한다. 스프라이트를 다시 그리지 않고
인덱스 교체만으로 오염 상태를 만든다. 이것이 26개 공간 상태를 8일에 감당하는 유일한 방법이다.

| # | 역할 | Stable (노을 호박) | Contamination (병원 시안) |
|---:|---|---|---|
| 0 | 배경 심부 | `#2b1e2a` | `#161d24` |
| 1 | 배경 | `#4a3142` | `#22303a` |
| 2 | 지형 기본 | `#7d4a48` | `#33505c` |
| 3 | 지형 강조 | `#b06b45` | `#4a7183` |
| 4 | 소품 주 | `#e0954a` | `#7fa8b5` |
| 5 | 소품 보조 | `#f2c46b` | `#b8d4d9` |
| 6 | 하이라이트 | `#ffe8b0` | `#eaf6f6` |
| 7 | 외곽선 | `#1a1119` | `#0e1418` |

캐릭터 전용 4색은 두 상태에서 **바뀌지 않는다**. 주인공만 오염되지 않는다는 규칙이 시각으로 드러난다.

| 역할 | 값 |
|---|---|
| 주인공 주 | `#d9d2c4` |
| 주인공 보조 | `#8a7f72` |
| 트라우마 | `#120e14` |
| 기억 발광 | `#ffd98a` |

UI: 표면 `#1a1119` 80% / 텍스트 `#ffe8b0` / 경고 `#e0954a`.

**색만으로 필수 정보를 전달하지 않는다** (spec-013 Acceptance criteria).
공정성 신호는 색 + 형태 또는 기호를 함께 쓴다.

### AI 파이프라인 락

Asset MCP의 팔레트는 `gameId` + `artStyle` 문자열에서 결정적으로 유도된다. 임의 색을 주입할
수 없으므로 **의도한 색으로 유도되는 문자열**을 고정한다.

```text
gameId   = daeume
artStyle = pixel art hospital corridor
```

| 역할 | 값 | |
|---|---|---|
| terrain_base | `#d37245` | 노을 호박 |
| accent | `#73c6ed` | 병원 시안 |
| character_primary | `#45a6d3` | |
| outline | `#392a23` | 따뜻한 암갈색 |
| pixelGrid | 32 | PPU 32와 일치 |

`pixel` 키워드가 grid 32를 보장하고, `hospital corridor`가 호박↔시안 보색축을 만든다.
문자열을 바꾸면 팔레트 전체가 바뀐다. **바꾸지 말 것.**

`var/assets/styles/daeume.json`이 초기화되면 위 두 값으로 `establish_art_style`을
다시 호출하면 같은 팔레트가 나온다. 그 파일은 캐시이지 원본이 아니다.

위 16색 표는 사람이 그릴 때의 아트 바이블이고, 이 유도 팔레트는 AI 생성물의 기본값이다.
생성물은 임포트 전에 16색 표로 다시 인덱싱한다 (C 담당).

### 애니메이션 예산 — 슬라이스 전체

12fps 기준. 이 표를 넘기면 아트가 병목이 된다.

| 대상 | 클립 | 프레임 |
|---|---|---:|
| 주인공 | idle / run / jump / fall / grab / attack / hit / death | 4/6/2/2/2/4/2/5 = **27** |
| 근접 잔재 | idle / move / telegraph / attack / hit / death | 3/4/2/3/2/4 = **18** |
| 트라우마 | idle / move / grab | 4/6/6 = **16** |
| 합계 | | **61 프레임** |

배경 타일은 Stage 1(버스정류장·주택가 거리) 1세트. 오염 버전은 팔레트 교체로 만들고
**따로 그리지 않는다.** 새로 그리는 것은 순차 소등용 가로등 3프레임뿐이다.

---

## 2. Unity 초기 구조

### 프로젝트 생성

Universal 2D 템플릿. 생성 직후 아래를 **먼저** 하고 스프라이트를 넣는다.

1. `Edit > Project Settings > Editor` → Asset Serialization `Force Text`,
   Version Control Mode `Visible Meta Files`. **이걸 안 하면 씬 diff를 볼 수 없다.**
2. Sorting Layer 등록: `Background`, `Far`, `Terrain`, `Object`, `Character`, `Foreground`,
   `Overlay`, `UI`. (순서 고정, 나중에 끼워넣지 말 것)
3. Input Actions 에셋 생성. 액션 이름 고정:
   `Move`, `Jump`, `Attack`, `Grab`, `Interact`, `Pause`.
   프롬프트는 물리 키가 아니라 이 이름을 참조한다 (spec-010, spec-013).
4. Pixel Perfect Camera 설정 (§1).

### 폴더

```text
Assets/
  Scenes/
    Boot.unity                      A
    Persistent.unity                A   매니저·플레이어·카메라·UI, 항상 로드
    Title.unity                     C
    Stage01_Base.unity              B   Stable 공간
    Stage01_Overlay_Echo.unity      B   additive
    Stage01_Overlay_Intrusion.unity B   additive
  Scripts/
    Core/           GameManager, SaveSystem, EventBus, StringTable        A
    Player/         PlayerController, PlayerCombat, PlayerGrab            A
    Interaction/    IInteractable, InteractionTargeter                    A
    Flow/           SceneFlow, StageState                                 A
    Contamination/  ContaminationDirector, VariantLoader, PressureStage   B
    Enemy/          Remnant, RemnantStates                                B
    Encounter/      Encounter, Wave, TerrainHazard                        B
    Memory/         MemoryInteractable, MemoryPlayback                    C
    UI/             Hud, Prompt, Subtitle, OptionsScreen                  C
    Audio/          AudioDirector                                         C
  Art/
    Sprites/Player, Sprites/Remnant, Sprites/Trauma, Tiles/Stage01, UI/
  Audio/  Bgm/, Sfx/
  Data/   StageData/, EncounterData/, VariantData/, StringTable/
  Settings/  URP 에셋, InputActions
```

폴더당 asmdef 1개 (`Daeume.Core`, `Daeume.Player`, …). 모듈 경계를 컴파일러가 강제하고
3인 동시 작업에서 재컴파일 시간이 줄어든다. 의존 방향은 한 방향만 허용한다:
`Core ← 나머지 전부`, `Contamination ← Enemy/Encounter`, UI와 Audio는 아무도 참조하지 않는다.

### 씬 소유권 — 가장 중요한 규칙

**한 씬을 두 사람이 동시에 열지 않는다.** Unity 씬과 프리팹은 실질적으로 머지가 불가능하다.
위 표의 소유자 열이 그 씬을 여는 유일한 사람이다.

- 다른 사람 씬을 고쳐야 하면 채팅으로 넘겨받고, 넘긴 사람은 그동안 그 씬을 열지 않는다.
- `Persistent.unity`는 A만 연다. B·C가 필요한 오브젝트는 프리팹으로 만들어 A에게 배치를 요청한다.
- 프리팹도 같은 규칙이다. 공용 프리팹 수정은 소유자를 통한다.

---

## 3. 저장소와 파일 공유

Unity 프로젝트는 **별도 GitHub repo**. 이 오케스트레이션 repo는 기획·spec만 보유한다.

### 초기 1회 설정

```bash
git init daeume-unity && cd daeume-unity
curl -o .gitignore https://raw.githubusercontent.com/github/gitignore/main/Unity.gitignore
git lfs install
```

`.gitattributes`:

```text
*.png  filter=lfs diff=lfs merge=lfs -text
*.psd  filter=lfs diff=lfs merge=lfs -text
*.aseprite filter=lfs diff=lfs merge=lfs -text
*.wav  filter=lfs diff=lfs merge=lfs -text
*.ogg  filter=lfs diff=lfs merge=lfs -text
*.unity     -merge
*.prefab    -merge
*.asset     -merge
```

`-merge`는 씬·프리팹의 자동 머지를 막는다. 조용히 깨진 씬이 나오는 것보다 충돌로 멈추는 편이 낫다.

### 규칙

- LFS 설치 전에는 아무도 png를 커밋하지 않는다. 한 번 들어가면 히스토리에서 빼기 어렵다.
- `Library/`, `Temp/`, `Logs/`, `Build/`는 커밋하지 않는다.
- `.meta` 파일은 반드시 함께 커밋한다. 빠지면 다른 사람 프로젝트에서 참조가 끊긴다.
- 기획 문서는 이 repo에 남긴다. Unity repo에 복사하지 않는다.

### 브랜치

- `main` 보호, `dev`가 통합 브랜치.
- `<기능>-<이름>` 형식의 짧은 feature 브랜치. 예: `chase-director-b`.
- **하루 안에 `dev`로 병합한다.** 이틀 넘게 살아 있는 브랜치는 8일 일정에서 사고가 된다.
- PR 리뷰는 눈으로 1분. 확인할 것은 둘뿐이다: 컴파일 에러 0, 남의 씬 건드리지 않았나.
- Unity 프로젝트 커밋은 이 repo의 GitHub Issue 워크플로 대상이 아니다.
  기획·spec 변경만 Issue를 연다.

---

## 4. 분업

### 담당 spec

| 담당 | 영역 | spec |
|---|---|---|
| **A** | 시스템·상태·저장·입력 | 001 핵심 루프, 002 이동·붙잡기, 003 전투·붙잡힘, 010 상호작용, 011 저장, 015 씬 흐름 |
| **B** | 추격·레벨·적 | 004 잔재, 006 Contamination·Director, 007 StageData, 012 Encounter·지형 |
| **C** | 서사·표현 | 005 회상, 008 서사 데이터, 013 UI·접근성, 014 오디오·카메라 |

C가 아트 파이프라인(AI 생성 → 팔레트 정합 → 임포트 설정)도 소유한다.

### 병렬을 가능하게 하는 첫 작업

96시간에서 가장 큰 손실은 서로를 기다리는 시간이다. **첫날 2시간에 A가 계약 스텁만 먼저
커밋한다.** 알맹이는 비어 있어도 된다. B·C는 그 순간부터 자기 코드를 컴파일할 수 있다.

A가 1일차에 올릴 것:

- `EventBus` — 발행/구독만
- `IInteractable`
- `StageState`, `PressureStage`, `EncounterState` enum
- `StringTable.Get(key)` — 키를 그대로 돌려주는 임시 구현
- `SaveData` 필드 정의 (직렬화 없이 클래스만)
- `GameManager` 싱글턴 껍데기

### 경계 계약

세 사람이 서로의 내부를 읽지 않게 하는 규칙이다.

- A는 `StageState`를 소유한다. B·C는 이벤트로 구독만 하고 직접 바꾸지 않는다.
- B는 `PressureStage`와 트라우마 거리를 소유한다. C의 오디오·카메라는 그 값을 **읽기만** 한다
  (spec-014는 거리를 계산하지 않는다).
- C는 모든 표시 문자열을 소유한다. A·B는 문자열을 코드에 쓰지 않고 키만 넘긴다.
- 아무도 남의 컴포넌트 필드를 직접 읽지 않는다. 필요하면 이벤트를 추가한다.

### 일정 — 8/17(월) ~ 8/24(월), 1인 32h

평일 2h, 주말 10h. 총 96 person-hour.

| 날짜 | 1인 | A 시스템 | B 추격·레벨 | C 서사·표현 |
|---|---:|---|---|---|
| 8/17 월 | 2h | 프로젝트 생성, 계약 스텁, repo·LFS | asmdef·폴더, StageData 스키마 | 팔레트 락, 임포트 프리셋, 문자열 테이블 |
| 8/18 화 | 2h | 이동·점프 | Stage01_Base 블록아웃 | 주인공 스프라이트 idle/run |
| 8/19 수 | 2h | 붙잡기 | 근접 잔재 FSM | 주인공 jump/fall/grab |
| 8/20 목 | 2h | 전투·피격·체력 | Encounter·Wave | HUD, 프롬프트 |
| 8/21 금 | 2h | 상호작용·프롬프트 연결 | 지형 요소 1종 | 잔재 스프라이트 |
| 8/22 토 | 10h | 저장·체크포인트·씬 흐름 | **Director + Overlay** ← 최대 리스크 | 회상 재생, 자막, 오디오 큐 |
| 8/23 일 | 10h | 붙잡힘 연출 → 복귀 | 추격 구간 저작, 조명 기믹 | 트라우마 스프라이트, 접근성 옵션 |
| 8/24 월 | 2h | 통합 테스트 | 추격 타이밍 튜닝 | 마감 점검 |

8/22의 Director + Overlay가 슬라이스 전체를 좌우한다. 그 전에 B가 막히면 그날 A가 붙는다.

### 자르는 순서

밀리면 아래에서부터 자른다. 순서는 [`README.md`](README.md)의 착수 순서를 뒤집은 것이다.

1. 지형 요소 (spec-012)
2. Stage 1 기억 원고 분량 (spec-008)
3. 오디오 큐 일부 (spec-014)
4. Title 화면 연출 (spec-015)

**spec-013의 접근성 옵션 5종은 자르지 않는다.** 색 비의존 신호와 리매핑은 기준선이고,
공정성 신호를 저작하는 시점에 같이 만들지 않으면 나중 비용이 훨씬 크다.

---

## 5. 착수 전 남은 것

- Unity repo 생성과 첫 커밋 — A가 8/17에 수행. GitHub repo 이름과 가시성은 미정.
- Stage 1 튜닝 값은 비어 있다: `TargetChaseSeconds`, `ChaseLookaheadUnits`,
  `GrabHoldSeconds`, `TraumaGrabSeconds`. 8/23 추격 저작 시 채운다.
