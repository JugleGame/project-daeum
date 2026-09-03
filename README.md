<div align="center">

<img src="Assets/Art/UI/Title/MainScreen_Background.png" alt="다음에 — 노을진 버스 정류장에 선 주인공과 검은 형상" width="960">

# 다음에 — DAEUME

**기억은 사라지지 않고, 모양을 바꾼다.**

과거의 장소를 다시 걷고, 남겨진 기억을 마주하며,<br>
뒤쫓아오는 트라우마로부터 달아나는 **2D 내러티브 액션 플랫포머**

[![Unity](https://img.shields.io/badge/Unity-6000.5.5f1-000000?style=flat-square&logo=unity)](https://unity.com/)
[![2D](https://img.shields.io/badge/2D-URP-5C2D91?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@17.0/manual/index.html)
[![Input System](https://img.shields.io/badge/Input_System-1.19.0-333333?style=flat-square)](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/index.html)

탐색 · 전투 · 회상 · 오염 · 추격

</div>

---

## 🎮 어떤 게임인가요?

> 평범했던 장소에는 기억이 남아 있고, 기억이 되살아날수록 공간은 일그러집니다.<br>
> 플레이어는 기억에서 태어난 **잔재**를 헤치고, 공격으로는 쓰러뜨릴 수 없는 **트라우마**를 피해 앞으로 나아가야 합니다.

`다음에`는 이야기를 읽는 것과 직접 움직이는 경험을 하나의 흐름으로 묶은 게임입니다.

| 흐름 | 플레이어가 하는 일 | 세계가 보이는 방식 |
|:---:|---|---|
| **탐색** | 낯익은 장소를 걷고 단서를 찾습니다 | 아직은 평범한 현실처럼 보입니다 |
| **조우** | 기억의 파편인 잔재와 싸웁니다 | 공간의 불안이 점차 커집니다 |
| **회상** | 장소에 남은 기억을 끝까지 마주합니다 | 과거의 의미가 현재와 겹칩니다 |
| **오염** | 색, 소리, 카메라와 지형의 변화를 견딥니다 | 기억이 현실의 형태를 바꿉니다 |
| **추격** | 쓰러뜨릴 수 없는 트라우마로부터 탈출합니다 | 익숙했던 길이 도주로가 됩니다 |

```text
탐색  →  잔재와의 조우  →  기억 회상  →  공간 오염  →  트라우마 추격  →  다음 장소
```

### 📷 플레이 화면

<!--
스크린샷을 촬영한 뒤 아래 플레이스홀더를 다음 형식의 이미지 태그로 교체하세요.
<img src="Docs/img/readme/stage01-explore.png" alt="Stage 01 버스정류장과 거리 탐색" width="420">

권장 규격: 16:9, 최소 1280×720, HUD가 잘리지 않은 동일 해상도
-->

| Stage 01 — 탐색 | 잔재 — 전투 |
|:---:|:---:|
| **📷 SCREENSHOT**<br><br>버스정류장에서 거리를 탐색하는 장면<br><br>`Docs/img/readme/stage01-explore.png` | **📷 SCREENSHOT**<br><br>잔재의 공격 예고와 플레이어 전투가 함께 보이는 장면<br><br>`Docs/img/readme/remnant-combat.png` |

| 기억 — 회상 | 오염 — 추격 |
|:---:|:---:|
| **📷 SCREENSHOT**<br><br>기억 제목과 문장이 표시되는 회상 장면<br><br>`Docs/img/readme/memory-recall.png` | **📷 SCREENSHOT**<br><br>오염된 공간에서 트라우마를 피해 달리는 장면<br><br>`Docs/img/readme/trauma-chase.png` |

---

## ✋ 붙잡는다는 것

이 게임의 시그니처 액션은 **붙잡기**입니다.

벽과 표면을 붙잡는 동작은 단순한 이동 기술에서 시작하지만, 이야기 속에서는 누군가가 잡아 준 기억과 혼자 버티는 시간, 그리고 마침내 놓아주는 선택으로 이어집니다. 플레이 방식과 이야기의 주제를 같은 동사로 연결하는 것이 `다음에`의 핵심입니다.

---

## 🕯️ 세 개의 기억

현재 저장소에는 처음과 전환점, 마지막을 잇는 세 스테이지가 구현되어 있습니다.

| Stage | 장소 | 기억 | 플레이 경험 |
|:---:|---|---|---|
| **01** | 버스정류장과 거리 | 기억의 조각 #1 | 탐색과 전투를 익히고, 첫 추격에서 왔던 길을 되돌아 달립니다 |
| **10** | 병원으로 이어지는 밤 도로와 육교 | 달려간 시간 | 속도와 시간에 대한 집착이 짙어진 공간을 통과합니다 |
| **13** | 첫 정류장으로 되돌아오는 기억 길 | 다음에 보자 | 도주와 공격 대신 접근과 수용으로 반복의 규칙을 뒤집습니다 |

### 🗺️ 스테이지 갤러리

| Stage 01 | Stage 10 | Stage 13 |
|:---:|:---:|:---:|
| **📷 SCREENSHOT**<br><br>노을 진 버스정류장<br><br>`Docs/img/readme/stage01.png` | **📷 SCREENSHOT**<br><br>병원으로 이어지는 밤 도로와 육교<br><br>`Docs/img/readme/stage10.png` | **📷 SCREENSHOT**<br><br>첫 정류장으로 되돌아온 기억 길<br><br>`Docs/img/readme/stage13.png` |

<!--
스테이지 이미지를 준비하면 각 셀의 SCREENSHOT 안내를 아래처럼 교체하세요.
<img src="Docs/img/readme/stage01.png" alt="Stage 01 노을 진 버스정류장" width="280">
-->

<details>
<summary><b>이야기의 방향을 조금 더 보기 (가벼운 스포일러)</b></summary>

`다음에`의 적은 단순한 괴물이 아닙니다. **잔재**는 주인공에게서 떨어져 나온 기억의 파편이고, **트라우마**는 공격으로 제거할 수 없는 존재입니다. 초반에는 달아나는 것이 답처럼 보이지만, 마지막에는 같은 행동만 반복해서는 끝낼 수 없다는 사실을 마주하게 됩니다.

</details>

---

## ⚔️ 게임의 특징

- **감정과 연결된 액션** — 이동, 붙잡기, 공격, 도주가 서사의 의미와 함께 변합니다.
- **공간을 덮어쓰는 오염** — 같은 지형 위에 시각·음향·카메라 연출이 겹치며 현실이 기억으로 변합니다.
- **두 종류의 위협** — 잔재는 맞서 싸울 수 있지만 트라우마는 공격이 통하지 않습니다.
- **되돌아오는 길** — 탐색했던 공간이 회상 이후 추격로로 다시 읽힙니다.
- **결정적인 세 장면** — Stage 01 → 10 → 13이 하나의 짧고 완결된 감정선을 만듭니다.
- **접근성 설정** — 추격 속도 보조와 입력 재설정을 저장 데이터와 함께 지원합니다.

---

## ⌨️ 조작법

| 행동 | 키보드·마우스 | 게임패드 |
|---|:---:|:---:|
| 이동 | `W` `A` `S` `D` / 방향키 | 왼쪽 스틱 |
| 점프 | `Space` | South 버튼 |
| 공격 | 마우스 왼쪽 버튼 / `Enter` | West 버튼 |
| 상호작용 / 회상 넘기기 | `E` | North 버튼 |
| 붙잡기 | `C` | East 버튼 |

> 붙잡기는 가능한 표면 가까이에서 사용할 수 있으며, 유지 시간은 제한되어 있습니다. 아래 방향을 입력하거나 점프하면 표면에서 이탈합니다.

---

## 🚀 실행 방법

### 요구 사항

- Unity **6000.5.5f1**
- Git

### 에디터에서 실행

```bash
git clone https://github.com/JugleGame/project-daeum.git
```

1. Unity Hub에서 복제한 폴더를 엽니다.
2. `Assets/Scenes/Boot.unity` 씬을 엽니다.
3. Play 버튼을 누릅니다.
4. 타이틀 화면에서 **처음부터** 또는 **이어하기**를 선택합니다.

> `Boot`는 영속 시스템과 씬 흐름을 준비하는 시작점입니다. 개별 Stage 씬에서 바로 실행하면 정상적인 게임 흐름과 다를 수 있습니다.

---

## 🧩 프로젝트 구조

```text
Assets/
├─ Scenes/       Boot → Persistent → Title → Stage01 / Stage10 / Stage13
├─ Scripts/      Core, Flow, Player, Enemy, Memory, Encounter, UI, Stage
├─ Data/         스테이지·인카운터·적·연출용 ScriptableObject
├─ Prefabs/      플레이어, 잔재, 트라우마, 기억, UI, 스테이지 구성요소
├─ Art/          픽셀 아트, 타일, 배경, UI
├─ Audio/        탐색·회상·전투·추격 BGM과 효과음
└─ Tests/        EditMode / PlayMode 검증
```

게임의 큰 상태 흐름은 아래 다섯 단계로 고정되어 있습니다.

```text
Explore → Memory → Chase → Cleared
   └──────────────────────→ Failed
```

- `GameManager`와 이벤트 계약이 전체 상태 전이를 관리합니다.
- 스테이지별 수치와 서사는 `StageData`로 분리되어 있습니다.
- 오염 연출은 기저 지형을 교체하지 않고 overlay와 presentation 계층으로 덧씌웁니다.
- EditMode·PlayMode 테스트가 씬 흐름, 전투, 기억, 추격, 재시도와 UI 회귀를 검증합니다.

---

## 🛠️ 개발 상태

`project-daeum`은 현재 **개발 중인 프로토타입/버티컬 슬라이스**입니다.

- 플레이 가능한 흐름: `Title → Stage 01 → Stage 10 → Stage 13 → Ending`
- 구현된 핵심 시스템: 이동·점프·붙잡기, 근접 전투, 인카운터, 기억 재생, 오염 단계, 트라우마 추격, 저장·이어하기, 접근성 설정
- 플랫폼: Unity Editor 기준 검증
- 공개 다운로드 및 브라우저 빌드: 아직 제공하지 않음

개발 및 QA 기록은 [`Docs/`](Docs/)와 [`QA/`](QA/)에서 확인할 수 있습니다.

---

## 🔊 오디오와 자산

프로젝트 오디오는 타이틀, 탐색, 회상, 전투, 클리어, 추격의 상태에 맞추어 전환됩니다. 세부 오디오 크레딧과 사용 조건은 [`Assets/Audio/CREDITS.md`](Assets/Audio/CREDITS.md)를 확인해 주세요.

저장소 루트에 별도 라이선스가 명시되기 전까지 코드와 자산의 재사용·재배포 권한을 임의로 가정하지 마세요.

---

<div align="center">

### “다음에 보자.”

Made by **JugleGame**

</div>
