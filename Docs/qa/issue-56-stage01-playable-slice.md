# Stage01 플레이 완주 QA 기록 — Issue #56

## Identity

- Issue: `#56` — 3스테이지 재구성 1/3, Stage01 시작 스테이지 플레이 완성과 진행 체인 재배선
- Branch: `56-feat-stage01-playable-slice`
- Scene: `Assets/Scenes/Stage01_Base.unity`
- 목표 플레이타임: 8~10분 (`Stage01.asset`의 `targetPlaytimeMinutes: 9`)

## 완주 판정 규칙

`build_project`나 `run_playmode_test` 통과는 기능 PASS가 아니다. 리플렉션이나 RunCommand 직접
호출로도 대체할 수 없다. 아래 경로를 **키보드 입력만으로** 끝까지 밟아야 PASS다.

`Boot` → New Game → Stage01 탐색 → Encounter 01 → Encounter 02 → Encounter 03 →
`MemoryAnchorMarker` 상호작용(회상) → 추격 시작 → 왼쪽 도주 → `Stage01_ChaseEscapeTrigger` 탈출

## 확정된 구간 배치

지면은 `Grid/GroundTilemap` 하나이며 윗면은 `y = -1.0`, 가로 범위는 `x = -8.5 ~ 32.0`이다.
평지 한 층이라 넘을 수 없는 단차가 없다.

| x | 대상 | 비고 |
|---:|---|---|
| -8.75 | `Terrain/Boundary_Left` | 보이지 않는 벽, 타일맵 왼쪽 끝 바깥 |
| -3.03 | `Signal_Exit_01` | 임시 문·표지 비주얼은 꺼 둠. 신호 데이터만 남는다 |
| -1.0 | (탈출 판정) | 아래 `EscapeMarker` 행 참고 |
| -1.0 | `EscapeMarker` / `Stage01_ChaseEscapeTrigger` | 탈출 지점 |
| 0 | `StartMarker` | 플레이어 시작 |
| 3.36~4.26 | `05-lamp-utility-pole` 매달림 영역 | `GrabbableSurface` + 트리거, 지면 위 0.11~1.31 |
| 4.65 | Encounter01 트리거 | `lockExit: false` (학습용, 출구를 막지 않는다) |
| 6.2 / 10.4 | `RemnantSpawnMarker_01` / `_02` | Encounter01 스폰 |
| 11 | `EncounterExitMarker` / `EncounterExitLock` | Encounter01 출구 |
| 13.6 / 14.25 | `Signal_DeadEnd_01` / `Stage01_ChaseDeadEndBackoffZone` | 추격 전용 |
| 15 | Encounter02 트리거 | `spawnCount 2`, `waveCount 1`, `lockExit: true` |
| 16.5 / 19.4 | `RemnantSpawnMarker_03` / `_04` | Encounter02 스폰 |
| 20 | `Encounter02ExitMarker` / `EncounterExitLock02` | Encounter02 출구 |
| 22 | Encounter03 트리거 | `spawnCount 2`, `waveCount 2`, `lockExit: true` |
| 23.2 / 25.6 | `RemnantSpawnMarker_05` / `_06` | Encounter03 스폰 |
| 26 | `Encounter03ExitMarker` / `EncounterExitLock03` | Encounter03 출구 |
| 26 | `Signal_Left_01` | 왼쪽 도주 신호 |
| 27 | `MemoryAnchorMarker` | 회상 지점 |
| 29 | `ChaseStartMarker` | 추격 시작 |
| 30.5 | `Trauma` | 추격자 초기 위치 |
| 32.25 | `Terrain/Boundary_Right` | 보이지 않는 벽, 타일맵 오른쪽 끝 바깥 |

추격 구간(29 → -1)에는 Encounter를 두지 않는다. 도주 중 전투는 원안 규칙 위반이다.

## 점프력

`PlayerController.jumpVelocity`는 `4.5`로 확정한다. 스크립트 기본값과
`Assets/Prefabs/Player/Player.prefab`의 직렬화 값이 같다(프리팹 값이 실제로 쓰인다).
`gravityScale: 1` 기준 도달 높이는 `4.5² / (2 × 9.81) ≈ 1.03`유닛이다.

낮게 잡은 것은 의도다. 벽을 한 번에 넘지 못하게 해서 **매달림을 여러 번 쓰도록** 유도한다.
매달림 영역(`05-lamp-utility-pole`)의 아래쪽은 지면 위 0.11유닛이라 한 번의 점프로 붙잡을 수 있다.

- 지면이 평지 한 층이라 도달 불가 지형이 생기지 않는다.
- `TraumaChaseActor`의 `jumpSpeed = 13`, `gravity = 30`이라 추격자의 도달 높이는 `13² / (2 × 30) ≈ 2.82`유닛이다.
  추격자는 플레이어가 못 넘는 벽도 한 번에 넘는다. 추격자가 지형에 막히면 추격 자체가 성립하지 않아 그대로 둔다.

이 값은 #57(Stage10) · #58(Stage13) 지형 설계의 전제로 쓴다.

## 탈출구 맥동

추격이 시작되면 `01-bus-stop-shelter`(x = 0)가 노란빛으로 맥동한다(`ChaseExitPulse`).
회상 지점의 맥동(`MemoryAnchorPulse`)과 같은 규칙을 재사용해 "빛나는 것 = 목표"로 읽히게 한다.
추격 전에는 맥동하지 않는다. 아직 갈 수 없는 곳을 미리 가리키면 탐색 동선이 망가진다.

두 맥동은 `SpritePulse`(`Daeume.Core`)를 함께 쓴다. 색을 흔드는 방법은 한 곳에만 있고,
언제 맥동할지만 각자 정한다.

탈출 판정 자체는 그대로 `Stage01_ChaseEscapeTrigger`(x = -1)가 하고, 추격 중인지는
`StageOneChaseController.CompleteAtEscape`가 판단한다. 정류장 바로 왼쪽이라 정류장을 지나치는
즉시 닿는다.

`Signal_Exit_01`의 임시 문·표지 비주얼(흰 사각형 + 초록 표지 + "▣ EXIT" 글자)은 껐다. 정류장이
탈출구 역할을 대신하므로 화면에 두 개의 출구 표시가 생긴다. `ChaseRouteSignal` 데이터는 남겨
둔다(경로 신호 검사 테스트가 읽는다).

## 진입 독백

`StageOpeningLine`(HUD 프리팹 `Stage01_Presentation`)이 스테이지에 처음 들어선 순간 화면 가운데에
독백을 한 줄씩 띄웠다가 지운다. 원고는 `StringTable`의 `stage.opening.stage01.01`부터 번호순이며,
없는 번호가 나오면 멈춘다.

| 순서 | 문구 | 목적 |
|---:|---|---|
| 01 | 무슨 일이 일어난 거지…? | 잔재·트라우마 등장의 개연성. 이야기를 모르는 플레이어에게 "지금이 비정상"임을 먼저 알린다 |
| 02 | 오른쪽으로 가 볼까? | 진행 방향 유도. 별도 튜토리얼 문구 없이 오른쪽으로 움직이게 한다 |

한 줄당 페이드 인 0.6초 · 유지 2.2초 · 페이드 아웃 0.6초, 줄 사이 0.35초.
스테이지 번호로 기억하므로 같은 씬이 다시 로드돼도 두 번 재생하지 않는다.

## 안내 표시

- 레벨 디자이너용 디버그 텍스트(`Guide_*` 7개)와 구간 색칠(`Zone_*`)은 `B1_VisualGuide`째로 삭제했다.
- 플레이어용 안내는 `StringTable`의 `hud.objective.memory`를 통한 HUD 목표 문구로만 제공한다.
- `Signal_Left_01` / `Signal_Exit_01` / `Signal_DeadEnd_01`은 디버그가 아니라 실제 길찾기
  신호(`ChaseRouteSignal`)라 그대로 둔다. 최초 플레이어에게 읽히는지 아래 완주에서 확인한다.

## 완주 기록

> 실제 키보드 입력 완주 후 채운다. 회차 2회 — 초회 플레이 1회, 추격 중 사망 후 재시도 1회.

### 1회차 — 초회 플레이

- 실행 일시:
- 소요 시간 (목표 8~10분):
- Encounter 01 트리거 / 클리어:
- Encounter 02 트리거 / 클리어 / `EncounterExitLock02` 해제:
- Encounter 03 트리거 / 클리어 / `EncounterExitLock03` 해제:
- 회상 상호작용 성공 여부:
- 추격 시작 → 탈출 성공 여부:
- 길찾기 신호가 안내 없이 읽혔는가:
- console error 수:
- 특이사항:

### 2회차 — 추격 중 사망 후 재시도

- 실행 일시:
- 소요 시간:
- 사망 지점:
- 체크포인트 복귀 지점이 회상 이후인가 (회상 재생 반복 없음):
- 재시도 후 탈출 성공 여부:
- console error 수:
- 특이사항:

## 자동 테스트

> 실행 후 채운다.

- EditMode 전체:
- PlayMode 전체:
- compile error 수:

## 남은 확인 대상

- **탈출 시 `Stage10_Base` 로드**: `Stage01.asset`의 `nextStageId`는 `10`이지만 `Stage10_Base` 씬이
  아직 없다. `SceneFlowController.PlayableStageScene(10)`이 빈 문자열을 돌려주므로 지금은 Title로
  빠진다. #57 완료 후 재확인한다.
- `Signal_Exit_01`의 문·표지 비주얼(`x ≈ -3.03`)이 실제 탈출 트리거(`x = -1`)와 2유닛 어긋나 있다.
  이번 범위에서는 보류했다.
- `FallRecoveryMarker`(`8, -3.75`)는 blockout 발판을 걷어낸 뒤 참조하는 코드가 없다.
  낙하 복구는 `VoidZone` → 체크포인트 복귀 경로가 담당한다.
