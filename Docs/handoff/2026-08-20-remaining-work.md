# 2026-08-20 세션 종료 — 상태 요약 및 다음 작업

브랜치 `5-qa-role-c-integration`. 이 문서는 오늘 세션에서 한 일과 다음에 할 일을 팀원이 코드를 안 열어봐도 판단할 수 있게 정리한 것이다.

## 1. 오늘 한 일

프레젠테이션 P0(화면에서 무슨 일이 일어나는지 보이게) 5개 항목을 전부 처리했다.

1. 회상 앵커에 `SpriteRenderer` 추가(`#FFD98A` 발광색, sortingLayer `Object`)
2. 트라우마를 `Knob` 대신 생성한 실루엣 스프라이트로 교체(`#120e14`), 씬 소유권 때문에 런타임 교체 방식 사용
3. 근접 잔재의 `BlockoutWhite`를 실루엣(Body)/쐐기형(AttackTelegraph)으로 교체 — 색뿐 아니라 형태로도 구분되게(spec-013)
4. 월드 디버그 라벨(`B1_VisualGuide`)을 런타임에서 기본 비활성화
5. HUD에 탐색 중 목표 문구(`hud.objective.memory`) 표시 추가

세부 구현·검증 수치는 [Docs/qa/role-c-integration-qa.md](../qa/role-c-integration-qa.md)의 "프레젠테이션 P0 완료 기록" 절 참고. 요약하면 EditMode 35/35, PlayMode 53/53(1건 flaky 재확인 완료), Console error 0, 씬 소유권 규칙 위반 없음(스크립트 파일과 C 소유 prefab만 수정).

### 작업 중 발견한 버그 1건

새로 추가한 `SpriteRenderer`가 기본으로 `Sprite-Lit-Default`(URP 2D 라이트 셰이더) 머티리얼을 받았는데, 씬에 `Light2D`가 하나도 없어 완전히 검게 렌더링됐다. `Sprite-Unlit-Default`로 교체해 해결. **앞으로 이 프로젝트에서 새 SpriteRenderer를 코드로 추가할 때는 머티리얼을 명시적으로 지정할 것** — 프로젝트의 URP 2D 기본 머티리얼이 Lit이라 조명 없이는 검게 나온다.

## 2-0. 실제 수동 플레이에서 발견된 버그 — 최우선 (2026-08-20 추가, 미해결)

사용자가 Boot→New Game으로 실제 키 입력 플레이해서 다음을 확인했다:

- 회상을 다 읽고 추격이 시작된 뒤, 화면(오염 전환)이 바뀌고 카메라는 흔들리는데 **아무것도 진행이 안 됨**.
- **가만히 서 있는데도** 검은 트라우마 실체(오늘 세션에서 실루엣 스프라이트를 넣은 그 오브젝트)가 닿아서 죽음.
- 탈출을 어떻게 해야 하는지 모르겠다고 함.

### 조사 결과 (원인 미확정, 강한 가설만 세움 — 시간 부족으로 코드 수정 못 함)

- `Signal_Exit_01`(`Assets/Scenes/Stage01_Base.unity`)은 디버그 라벨이 아니라 **진짜 게임 내 길찾기 신호**다(`ChaseRouteSignal.cs`, spec-006/013 요구사항: 색+모양+기호를 함께 갖는 방향 신호). 화면의 "◫ EXIT"는 오늘 세션이 숨긴 `B1_VisualGuide`(레벨 디자이너용 디버그 텍스트, 별도 부모 오브젝트)와는 다른 물건이다 — 실수로 같이 숨긴 건 아닌지 재확인 필요는 없지만, "이 텍스트가 뭔지 모르겠다"는 사용자 피드백은 **신호 자체가 최초 플레이어에게 안 읽힌다는 UX 문제**로 봐야 한다.
- `TraumaContactHandler.cs`(`Assets/Scripts/Player/TraumaContactHandler.cs`)를 읽어 보면: 트라우마 접촉 → 1초 붙잡기 연출 → `GameManager.Fail()` → `ChaseCheckpointRestoreRequested` 발행 → 플레이어 위치/체력만 체크포인트로 복귀.
- **`TraumaChaseActor.cs`(`Assets/Scripts/ContaminationRuntime/TraumaChaseActor.cs`)가 `ChaseCheckpointRestoreRequested`나 `PlayerRestoreRequested`를 구독해서 자기 위치를 초기화하는지 아직 확인 못 했다.** 만약 트라우마가 리셋 없이 계속 플레이어 근처에 남아 있다면: 복귀 직후 트라우마가 바로 다시 닿음 → 1초 연출 → 다시 Fail → 다시 복귀... 무한 루프가 되고, 이게 "카메라만 흔들리고 아무것도 진행 안 됨"으로 보일 가능성이 높다. **다음 세션에서 제일 먼저 `TraumaChaseActor.cs` 전체를 읽고 리스폰 시 위치 리셋 로직이 있는지부터 확인할 것.**
- 재현 순서: Boot 씬 Play → New Game → 오른쪽 끝(x≈27)까지 이동 → Interact로 회상 시작 → Interact 3회로 문장 진행 → Chase 진입 → 제자리에 서서 트라우마가 닿을 때까지 대기 → 이후 상태(움직이는지, HP가 계속 줄어드는지, `Fail`이 반복 발행되는지) Console 로그로 확인.

## 2. 아직 안 한 것 — 최우선

**사람이 실제 입력(키보드/마우스)으로 Title → 새 게임 → 이동 → 회상 상호작용 → 문장 진행/건너뛰기 → 추격 → 탈출까지 1회 완주한 기록이 없다.** 오늘 세션은 시간 제약상 `MemoryAnchor.Begin()/Advance()`를 리플렉션(Unity RunCommand)으로 직접 호출해 상태 전이(Explore→Memory→Chase)만 검증했다. 이건 로직이 도달 가능함을 증명하지만, 입력 바인딩·프롬프트 UI·카메라 추적 같은 "사람이 실제로 플레이했을 때 막히는 지점"은 검증하지 못한다.

Issue #3의 G4(Vertical Slice) 실측 PASS 판정은 이 완주 없이는 낼 수 없다.

## 3. 남은 spec 미준수 (P1)

`Docs/var/handoffs/daeum/draft-v1/specs/`(git 미추적, 로컬 전용) 원문 대조 후 이전 세션에서 정리한 목록. 우선순위순은 아니고 병렬 처리 가능.

| 작업 | 근거 | 비고 |
|---|---|---|
| 접근성 옵션 화면 5종(리매핑/흔들림 0/자막 크기 3단계/추격 속도 저하) | spec-013 | 값 그릇은 `UI/AssistSettingsPresenter.cs`에 이미 있음. UI와 자막 크기 반영만 없음 |
| 추격 중 일반 조사 프롬프트 숨김 | spec-013 | `StageHudPresenter.cs` 주석에 위치 표시됨 |
| Memory→Chase 오디오 6단계 큐, 상태별 BGM 5종, `ChaseLookaheadUnits` | spec-014 | 대부분 미구현 |
| Encounter Cleared → `PlayerAggression` 리셋 | spec-003 | 이벤트 구독 방식 권장 |
| `Title.unity`의 하드코딩 문자열 → StringTable 키 | spec-013 `Test_UI_NoHardcodedStrings` | Title은 C 소유 씬이라 직접 수정 가능 |
| `MemoryCompletionAdapter` 디버그 경로 제거 | — | 실경로가 동작하므로 정리 대상 |
| HUD/pressure 프리팹 `DontDestroyOnLoad` 정리 규칙 | — | Title 복귀 후에도 HUD가 남음 |

## 4. "Game-Develop-Orchestration" 레포에서 시작해야 하나?

**아니다, 그 레포를 별도로 "시작(실행)"할 필요는 없다.** `C:\dev\Game-Develop-Orchestration`은 실행 프로세스가 아니라 정책·MCP 서버 모음 레포이고, 그 안의 Unity/Asset MCP 서버는 이미 이 세션에 연결돼 있다(`Unity_*`, `Unity_AssetGeneration_*` 툴이 이미 사용 가능한 상태였음).

다만 주의할 점 하나:

- 오늘 만든 회상 앵커/트라우마/잔재 스프라이트는 **전부 즉석 실루엣(RunCommand로 픽셀 알파마스크 생성, 무료·즉시)**이다. blockout을 벗어나긴 했지만 여전히 플레이스홀더 수준이다.
- 이 레포의 `docs/contracts.md` "Asset MCP" 절을 보면, 진짜 아트(PixelLab 등 외부 생성)로 교체하려면 **비용이 발생하고 사람 승인이 필요한 정식 파이프라인**(`generate_2d_variations`, `generate_2d_animation` 등, `humanApproved` 필수)을 거쳐야 한다.
- 즉: **정식 아트로 교체하는 작업을 시작할 때만** 그 레포의 워크플로우(`docs/architecture.md`, `docs/contracts.md` 순서로 읽기)를 따라가면 된다. 지금 당장 급한 P0/P1 작업에는 필요 없다.

## 5. 다음 세션 시작 방법

```text
Docs/handoff/2026-08-20-remaining-work.md 를 읽고,
"2. 아직 안 한 것"부터 처리해줘.
Unity MCP로 Play Mode에서 실제 키 입력 시뮬레이션(또는 수동 확인)으로
Title→탈출 완주를 확인하고, Docs/qa/role-c-integration-qa.md에 기록해줘.
```
