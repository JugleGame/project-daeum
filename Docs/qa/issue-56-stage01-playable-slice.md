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
| -3.03 | `Signal_Exit_01` (문·표지 비주얼) | 탈출 트리거와 2유닛 어긋나 있음(보류, 아래 참고) |
| -1.0 | `EscapeMarker` / `Stage01_ChaseEscapeTrigger` | 탈출 지점 |
| 0 | `StartMarker` | 플레이어 시작 |
| 3.36~4.26 | `05-lamp-utility-pole` 매달림 영역 | `GrabbableSurface` + 트리거, 지면 위 0.11~1.31 |
| 4.65 | Encounter01 트리거 | `lockExit: false` (학습용, 출구를 막지 않는다) |
| 7 / 9 | `RemnantSpawnMarker_01` / `_02` | Encounter01 스폰 |
| 8 | `Stage01_WarningPulse` | 지형 해저드 |
| 11 | `EncounterExitMarker` / `EncounterExitLock` | Encounter01 출구 |
| 13.6 / 14.25 | `Signal_DeadEnd_01` / `Stage01_ChaseDeadEndBackoffZone` | 추격 전용 |
| 15 | Encounter02 트리거 | `spawnCount 2`, `waveCount 1`, `lockExit: true` |
| 17 / 18.5 | `RemnantSpawnMarker_03` / `_04` | Encounter02 스폰 |
| 20 | `Encounter02ExitMarker` / `EncounterExitLock02` | Encounter02 출구 |
| 22 | Encounter03 트리거 | `spawnCount 2`, `waveCount 2`, `lockExit: true` |
| 23.5 / 25 | `RemnantSpawnMarker_05` / `_06` | Encounter03 스폰 |
| 26 | `Encounter03ExitMarker` / `EncounterExitLock03` | Encounter03 출구 |
| 26 | `Signal_Left_01` | 왼쪽 도주 신호 |
| 27 | `MemoryAnchorMarker` | 회상 지점 |
| 29 | `ChaseStartMarker` | 추격 시작 |
| 30.5 | `Trauma` | 추격자 초기 위치 |
| 32.25 | `Terrain/Boundary_Right` | 보이지 않는 벽, 타일맵 오른쪽 끝 바깥 |

추격 구간(29 → -1)에는 Encounter를 두지 않는다. 도주 중 전투는 원안 규칙 위반이다.

## 점프력 하향

`PlayerController.jumpVelocity`를 `8` → `6.5`로 낮췄다. 스크립트 기본값과
`Assets/Prefabs/Player/Player.prefab`의 직렬화 값을 함께 고쳤다(프리팹 값이 실제로 쓰인다).
`gravityScale: 1` 기준 도달 높이는 `6.5² / (2 × 9.81) ≈ 2.15`유닛이다.

- 지면이 평지 한 층이라 도달 불가 지형이 생기지 않는다.
- 매달림 영역 최상단은 지면 위 1.31유닛이라 2.15유닛 안에 든다.
- `TraumaChaseActor.jumpSpeed = 13`은 건드리지 않았다. 추격자가 지형을 못 넘으면 추격이 성립하지 않는다.

이 값은 #57(Stage10) · #58(Stage13) 지형 설계의 전제로 쓴다.

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
