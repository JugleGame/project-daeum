# 다음 세션 인수인계 — Stage 1 프레젠테이션 패스

작성 시점: 2026-08-20 / 브랜치 `5-qa-role-c-integration`

## 0. 이 문서만 읽고 시작할 수 있게

새 세션 요청문 예시:

```text
Docs/handoff/next-session-presentation-pass.md 를 읽고,
"3. 다음 작업"의 P0 항목부터 순서대로 진행해줘.
Unity MCP로 Play Mode 실측까지 확인하고, 끝나면 이 문서의 진행 표를 갱신해줘.
```

## 1. 현재 상태 (실측 확인됨)

Stage 1 전체 루프가 **코드 레벨에서 완주 가능**하다. 직전 세션에서 진행 불가 버그를 해소하고 전 스크립트를 검토·주석·수정했다.

검증 수치:

- EditMode 35/35 통과, PlayMode 53/53 통과, 실패 0
- Unity Console error 0
- Play Mode 실측 로그 (Boot → New Game → 회상 → 추격):

```
MemoryPlayback in scene = Stage01_MemoryAnchor(Clone)
interact=True state=Memory inputEnabled=False
advance=True / advance=True / advance=False → state=Chase
pressure=Intrusion chaseActive=True trauma=True
inputEnabled after chase=True
```

### 직전 세션에서 고친 것

| # | 파일 | 내용 |
|---|---|---|
| 1 | `Assets/Scripts/Memory/MemoryPlayback.cs` (신규) | 회상 진행/건너뛰기 입력 소유. **회상이 첫 문장에서 영구 정지하던 진행 불가 버그 해소**. 진행=Interact, 건너뛰기=Pause |
| 2 | `UI/Stage01PresentationBootstrap.cs` | 이미 열린 씬도 처리(씬 직접 Play 대응), `MemoryPlayback` 자동 부착 |
| 3 | `ContaminationRuntime/StageOneChaseController.cs` | 회상 완료 시 `Echo` 경유 후 추격 (spec-005) |
| 4 | `Player/PlayerController.cs` | 회상 중 이동 잠금, 붙잡기 해제 시 원래 중력 배율 복원, 복귀 시 매달림 정리 |
| 5 | `Player/PlayerHealth.cs` | 회상 중 피해 차단, 시작 시 체력 이벤트 1회 발행 |
| 6 | `Player/PlayerCombat.cs` | Memory/Failed/Cleared에서 공격 차단 |
| 7 | `Player/TraumaContactHandler.cs` | 붙잡기 연출 후 입력 잠금 해제 보장 |
| 8 | `ContaminationRuntime/OverlaySceneLoader.cs` | 오버레이 적재/해제 요청 직렬화(경합 제거) |
| 9 | `Interaction/InteractionTargeter.cs` | 바라보는 방향 자체 산출(동률 우선순위 규칙 복구) |
| 10 | `Enemy/MeleeRemnant.cs` | 대상 탐색 매 프레임 → 0.5초 간격 |
| 11 | `Audio/PressurePresentationController.cs` | 저장된 카메라 흔들림 강도 적용 |
| 12 | `UI/TitleMenuController.cs` | 하드코딩 문자열 제거 → StringTable |
| 13 | `Core/StringTable.cs` | 키 추가(`prompt.memory.skip`, `hud.objective.memory`, `title.*`) |
| 14 | asmdef | `Daeume.Memory`+InputSystem, `Daeume.Audio`+Flow |

전 스크립트에 한국어 주석 완료(유니티 미경험자 기준). 각 주석에 해당 spec 조항과 검토 의견이 함께 적혀 있다.

### 남아 있는 문제 — "게임처럼 안 보이는" 이유

시스템은 동작하지만 화면에 보이는 것이 blockout뿐이다.

- 회상 앵커(게임의 유일한 목표 지점)에 렌더러가 없어 **투명**
- 트라우마 스프라이트가 Unity 기본 UI 스프라이트 `Knob`
- 잔재가 흰 박스(`BlockoutWhite`)
- B1의 월드 디버그 라벨(`START / EXPLORE`, `ENCOUNTER`, `GRAB`, `ONE-WAY`, `ESCAPE`, `BACK OFF`)이 게임 화면에 상시 렌더

## 2. 시작 방법

1. Unity 에디터에서 **`Assets/Scenes/Boot.unity`를 열고 Play**. Stage01_Base만 열고 Play하면 GameManager·플레이어·카메라가 전부 Persistent 씬에 있어 아무것도 동작하지 않는다.
2. 타이틀에서 `새 게임` → 오른쪽 끝(x≈27)의 회상 지점까지 이동 → `Interact`로 회상 시작 → `Interact`로 문장 진행(또는 `Pause`로 건너뛰기) → 추격 시작.
3. Unity MCP가 연결돼 있으면 `Unity_ManageEditor(Play)` + `Unity_RunCommand`로 리플렉션 검증이 가능하다. 테스트 러너도 `TestRunnerApi`를 `Unity_RunCommand`로 돌려 결과를 임시 파일에 기록하는 방식으로 실행했다.

### 반드시 지킬 것

- **씬 소유권**: `Stage01_Base` / `Overlay_Echo` / `Overlay_Intrusion`은 B, `Boot`·`Persistent`는 A, `Title`은 C. 씬 파일은 병합이 불가능하다. 다른 역할의 씬을 고쳐야 하면 런타임 부트스트랩(`Stage01PresentationBootstrap` 패턴)을 쓴다.
- **프리팹 수정은 가능**: `Assets/Resources/Memory|UI|Presentation/` 아래 프리팹은 C 소유라 직접 수정해도 된다.
- **픽셀 규격 고정**: PPU 32, 카메라 orthographic size 4.21875, Point 필터, 정수배 스케일. 이 값을 바꾸면 화면 전체 픽셀 격자가 어긋난다. (`Assets/Editor/DaeumeSpriteImportSettings.cs`가 임포트 시 자동 적용)
- **spec 원문 위치**: `Docs/var/handoffs/daeum/draft-v1/specs/daeume__spec-001~015`. (git 미추적 로컬 폴더다. `Assets/` 안에 두면 pytest 픽스처의 중복 asmdef 때문에 Unity 컴파일이 깨지므로 절대 되돌려 놓지 말 것.)

## 3. 다음 작업

### P0 — 화면에서 무슨 일이 일어나는지 보이게 (이번 세션 목표)

| 상태 | 작업 | 위치 | 완료 기준 |
|---|---|---|---|
| ☐ | 회상 앵커에 시각 표현 부여 | `Assets/Resources/Memory/Stage01_MemoryAnchor.prefab` | SpriteRenderer 추가, sortingLayer `Object`, 기억 발광색 `#ffd98a` 계열. 플레이어가 접근 전에 위치를 알 수 있다 |
| ☐ | 트라우마 스프라이트 교체 | `Stage01_Base` 씬의 `Trauma` (B 소유 씬 → 프리팹화하거나 런타임 교체) | `Knob` 제거. 트라우마 색 `#120e14`, 실루엣만이라도 캐릭터로 읽힌다 |
| ☐ | 잔재 스프라이트 교체 | `Assets/Prefabs/...` 근접 잔재 프리팹 | `BlockoutWhite` 제거. 예고(telegraph) 표시가 색 외 형태로도 구분된다(spec-013) |
| ☐ | 월드 디버그 라벨 기본 비활성화 | `Stage01_Base`의 `B1_VisualGuide` | 게임 화면에서 사라진다. 디버그 토글로만 표시(런타임 스크립트로 끄면 씬 수정 불필요) |
| ☐ | HUD 목표 문구 | `UI/StageHudPresenter.cs` + `Stage01_Presentation.prefab` | 탐색 중 `hud.objective.memory` 표시(키는 이미 StringTable에 있음) |

P0 완료 후: Boot부터 Play해 Title → 회상 → 오염 → 추격 → 탈출을 실제 입력으로 1회 완주하고, 그 증거를 `Docs/qa/role-c-integration-qa.md`에 기록한다. 그 시점에 Issue #3의 G4를 실측 PASS로 판정할 수 있다.

### P1 — spec 미준수 해소

| 상태 | 작업 | 근거 |
|---|---|---|
| ☐ | 접근성 옵션 화면 5종(리매핑 / 흔들림 0 / 자막 크기 3단계 / 추격 속도 저하) | spec-013, "자르지 않는다"고 명시된 항목. 값 그릇은 `UI/AssistSettingsPresenter.cs`에 이미 있고 저장 경로도 있다. UI와 자막 크기 반영만 없다 |
| ☐ | 추격 중 일반 조사 프롬프트 숨김 | spec-013 |
| ☐ | Memory→Chase 오디오 6단계 큐, 상태별 BGM 5종, `ChaseLookaheadUnits` | spec-014 대부분 미구현 |
| ☐ | Encounter Cleared → `PlayerAggression` 리셋 | spec-003. 이벤트 구독 방식 권장(모듈 참조 방향 유지) |
| ☐ | `Title.unity`의 하드코딩 문자열을 StringTable 키로 | spec-013 `Test_UI_NoHardcodedStrings`. Title은 C 소유 씬 |
| ☐ | `MemoryCompletionAdapter` 디버그 경로 제거 | 실제 경로가 동작하므로 정리 대상 |
| ☐ | HUD/pressure 프리팹 `DontDestroyOnLoad` 정리 규칙 | Title 복귀 후에도 HUD가 남는다 |

각 항목의 상세 위치와 이유는 해당 스크립트의 "검토 메모" 주석에 적혀 있다. 파일을 열면 바로 보인다.

## 4. 작업 완료 시

1. `Docs/qa/role-c-integration-qa.md`에 실측 증거(테스트 수치, Console error 수, 완주 기록)를 추가한다.
2. 이 문서의 진행 표(☐ → ☑)를 갱신한다.
3. 커밋은 작은 단위로. 브랜치는 `5-qa-role-c-integration`을 그대로 쓰거나 `dev`에 통합한다.
