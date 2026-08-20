# C-QA — Role C Stage 1 통합 검증 (재검증)

## 목표

`a49a688 feat-Role-C` 커밋을 Role B QA 포맷([Docs/role-b/99-integration-qa.md](../role-b/99-integration-qa.md))에 맞춰 재검증하고, B가 PARTIAL로 남긴 실제 `MemoryComplete` 연결 여부를 코드·Scene 레벨에서 확인한다. 빌드 검증은 이번 재검증 범위에서 제외한다.

## 절차 경위

- `a49a688`은 PR 없이 `dev`에 직접 커밋됨. `Docs/collaboration-workflow.md`의 "작은 단위로 dev에 통합" 원칙과는 부합하나, 소비자(B) review·sample payload 공유 절차가 기록에 없다.
- 본 문서가 그 review를 사후에 대체한다.

## 코드 레벨 확인

- `MemoryAnchor.Complete()`가 `MemoryCompleted(memoryId, narrativeFlag)`를 실제로 publish한다. (mock 아님)
- `MemoryCompletionBridge`가 `GameManager.Events`의 `MemoryCompleted`를 구독하고 `StageOneChaseController.BeginChaseFromMemory()`를 호출한다. ([MemoryCompletionBridge.cs](../../Assets/Scripts/Memory/MemoryCompletionBridge.cs))
- `StageOneChaseController.BeginChaseFromMemory()`는 `StageState`가 `Memory`가 아니면 조기 반환하므로, B가 남긴 debug 경로(`MemoryCompletionAdapter.TriggerDebugMemoryComplete`)와 동시에 존재해도 상태 가드로 인해 이중 chase 시작은 발생하지 않는다.

## Scene 레벨 확인 — 최초 재검증 시점(수정 전)

- `Stage01_MemoryAnchor.prefab`(실제 `MemoryCompleted` 발행 경로, `MemoryCompletionBridge` 포함)이 **어떤 `.unity` Scene에도 배치돼 있지 않았다.** (`grep -rn "Stage01_MemoryAnchor" Assets --include=*.unity` 결과 0건)
- `Stage01_Base.unity`에는 여전히 구 debug 경로인 `MemoryCompletionAdapter`만 존재했다.
- `Stage01_Presentation.prefab`, `Stage01_PressurePresentation.prefab`(HUD, pressure 연출) 역시 어떤 Scene에도 배치돼 있지 않았다.
- 즉 `a49a688`은 에셋·스크립트 수준 산출물이었고, 실제 플레이 경로에는 통합되지 않은 상태였다.

## 조치 완료 (같은 branch, Scene 파일은 수정하지 않음)

Scene YAML을 손으로 편집하는 대신, 기존 `StageVisualBootstrap` 패턴을 따라 런타임 부트스트랩으로 해결했다. Scene 소유권 규칙(`Stage01_Base`는 B만 수정)을 지키기 위해 Scene 파일 자체는 건드리지 않았다.

- `Stage01_MemoryAnchor`, `Stage01_Presentation`, `Stage01_PressurePresentation` 세 prefab을 `Assets/Resources/{Memory,UI,Presentation}/`로 이동(`git mv`, GUID 보존).
- [Stage01PresentationBootstrap.cs](../../Assets/Scripts/UI/Stage01PresentationBootstrap.cs) 신설: `[RuntimeInitializeOnLoadMethod]`로 `Stage01_Base` scene 로드 시
  - 기존 `stage01.memory.anchor.01` marker 위치에 `Stage01_MemoryAnchor`를 `Resources.Load`로 instantiate하고 `MemoryCompletionBridge.Configure(StageOneChaseController)`를 실제 연결한다.
  - `Stage01_Presentation`(HUD), `Stage01_PressurePresentation`을 `DontDestroyOnLoad`로 1회 instantiate한다.
- `Daeume.UI.asmdef`에 `Daeume.Memory`, `Daeume.Stage` 참조 추가(순환 참조 없음: 두 asmdef 모두 UI를 참조하지 않음).
- `MemoryCompletionAdapter`(B의 debug 전용 `ContextMenu` 트리거)는 자동 구독 경로가 아니므로 `MemoryCompletionBridge`와 공존해도 이중 호출이 없다. 정리는 하지 않고 유지.

## Gate 판정

- **G3 (Memory → 오염 전환): 코드·런타임 배치 완료.** 실제 `MemoryComplete` → `BeginChaseFromMemory` 경로가 `Stage01_Base` 플레이 시 자동으로 연결된다. Editor Play Mode 실측 재생 확인은 아직 없음(이번 재검증 범위에서 빌드/플레이 제외).
- **G4 (Vertical Slice): 배선 완료, 실측 미검증.** HUD/pressure presentation도 자동 배치되나 실제 플레이로 Title→Stage1→기억→오염→추격 loop를 돈 기록은 없다.
- **G5 (Release Candidate): 판정 불가.** G4 실측 확인 전까지 대상 아님.

## Issue #3 (Role B) Acceptance Criteria 재확인

- 코드 레벨 gap은 해소했다. PARTIAL → PASS 전환은 Editor Play Mode에서 실제 event 경로로 Stable → Echo → Intrusion 재생 확인 후 판정한다(다음 조치 참고).

## 추가 발견 및 조치 — prefab 내부 미완성

Play Mode로 `Stage01_Base` 실측 중 발견: 부트스트랩으로 spawn까지는 됐으나 (`Stage01_MemoryAnchor(Clone)`, `Stage01_Presentation(Clone)`, `Stage01_PressurePresentation(Clone)` Hierarchy 확인됨, Console 0/0/0) 화면에 HUD 텍스트가 전혀 안 보임.

원인: `Stage01_Presentation.prefab`이 Canvas 루트 GameObject 1개뿐이고 자식 UI 요소(Text/Image)가 전혀 없었다. `StageHudPresenter`/`MemoryPanelPresenter`의 `healthText`/`promptText`/`chaseText`/`panel` 등 필드가 전부 `fileID: 0`(미할당)이었고, 이를 채워주는 `Bind()` 호출도 코드베이스 전체에 0건이었다. 즉 `a49a688`의 프레젠테이션 스크립트는 로직만 있고 실제 UI 마크업이 없는 빈 껍데기였다.

조치: `Stage01_Presentation.prefab`에 `HealthText`, `PromptRoot/PromptText`, `ChaseRoot/ChaseText`, `MemoryPanel/TitleText/BodyText`를 legacy `UI.Text`로 추가하고 presenter 필드에 직접 연결했다. `Stage01_PressurePresentation.prefab`은 이미 있는 `AudioSource`를 `ambientSource`에 연결했다(`targetCamera`는 `Awake()`의 `Camera.main` fallback으로 이미 동작).

## 다음 조치

1. Unity Editor Play Mode에서 `Stage01_Base` 진입 → Memory 완료 → 실제 `BeginChaseFromMemory` 호출 → Encounter/Chase 전환과 HUD 텍스트 표시를 눈으로 확인.
2. 확인 후 Issue #3 Acceptance Criteria를 PARTIAL → PASS로 갱신하고 G4 실측 증거를 본 문서에 추가.

## 재검증 효율 가이드

Role B QA 문서의 가이드를 그대로 따른다: focused fixture 우선, 전체 EditMode/PlayMode는 diff 확정 후 1회.

## 실측 검증 기록 (2026-08-20, spec 원문 대조 후)

`Docs/var/handoffs/daeum/draft-v1/specs/` 원문 15건을 대조해 재검토했다. 결과: 시스템 골격은 spec대로 서 있었고, 진행 불가 버그 1건이 그 뒤 전부를 가리고 있었다.

### 근본 원인

- spec-010은 "Memory 상태에서 일반 상호작용을 비활성화한다"를 요구하고 `InteractionTargeter`는 그 규칙을 정확히 지키고 있었다.
- 그런데 회상 문장 진행이 그 상호작용 경로에 의존하고 있었다. 결과적으로 회상 첫 문장에서 영구 정지 → `MemoryCompleted` 미발행 → 오염·추격·탈출 전 구간 도달 불가.
- spec-005/폴더 구조가 지정한 `MemoryPlayback`(회상 재생 입력 소유자)이 아예 없었던 것이 원인이다.

### 조치

`Assets/Scripts/Memory/MemoryPlayback.cs` 신설(진행=Interact, 건너뛰기=Pause). 그 외 spec 위반 교정 13건은 [Docs/handoff/2026-08-20-remaining-work.md](../handoff/2026-08-20-remaining-work.md) 참고.

### 검증 수치

- EditMode 35/35, PlayMode 53/53, 실패 0, Console error 0
- Play Mode 실측(Boot → New Game → 회상 → 추격):
  `interact=True state=Memory inputEnabled=False` → `advance ×3` → `state=Chase` → `pressure=Intrusion chaseActive=True trauma=True` → `inputEnabled after chase=True`

### Gate 판정 갱신

- **G3 (Memory → 오염 전환): PASS.** 실제 이벤트 경로로 회상 완료 → Echo → 추격 진입을 실측 확인했다.
- **G4 (Vertical Slice): 여전히 PARTIAL.** 시스템 경로는 확인했으나 사람이 실제 입력으로 Title→탈출까지 완주한 기록은 없다. 프레젠테이션 P0 작업(투명한 회상 앵커, 플레이스홀더 스프라이트, 상시 노출되는 디버그 라벨) 이후 재판정한다.
- **G5: 판정 불가.** G4 실측 완주 후 대상.

## 프레젠테이션 P0 완료 기록 (2026-08-20, 이어서)

프레젠테이션 P0(화면에서 무슨 일이 일어나는지 보이게) 5개 항목을 전부 처리했다. 상세 내용과 남은 작업(P1)은 [Docs/handoff/2026-08-20-remaining-work.md](../handoff/2026-08-20-remaining-work.md) 참고.

### 처리 내역

1. **회상 앵커 시각화** — `Stage01_MemoryAnchor.prefab`에 `SpriteRenderer`+`PrototypeVisual`(`#FFD98A`, sortingLayer `Object`) 추가. 최초 `Sprite-Lit-Default` 머티리얼로는 씬에 `Light2D`가 하나도 없어 완전히 검게 렌더링되는 문제를 발견해 `Sprite-Unlit-Default`로 교체.
2. **트라우마 스프라이트 교체** — `Stage01_Base`는 B 소유 씬이라 파일을 직접 고치지 않고, `Stage01PresentationBootstrap.ReplaceTraumaVisual()`을 신설해 런타임에 `Knob` → 생성한 실루엣(`Assets/Resources/Trauma/TraumaBody.png`)으로, 색은 `#120e14`로 교체.
3. **잔재 스프라이트 교체** — `Stage01_MeleeRemnant.prefab`의 `Body`/`AttackTelegraph`에서 `BlockoutWhite` 제거. Body는 인간형 실루엣, AttackTelegraph는 쐐기형(방향성 텔레그래프)으로 형태 자체를 다르게 해 spec-013 "색 외 형태로도 구분" 요구를 충족.
4. **월드 디버그 라벨 기본 숨김** — `Stage01PresentationBootstrap.HideVisualGuideDebugLabels()` 신설. `B1_VisualGuide`를 코드로 `SetActive(false)`(씬 파일 미수정). Play Mode에서 `activeInHierarchy=False` 실측 확인.
5. **HUD 목표 문구** — `StageHudPresenter`에 `objectiveText`/`objectiveRoot` 필드 추가, `Explore` 상태에서만 `hud.objective.memory` 표시하도록 `OnStageStateChanged`에 연결. `Stage01_Presentation.prefab`에 `ObjectiveRoot/ObjectiveText` UI 요소 신설.

### 검증 수치 (2026-08-20)

- EditMode 35/35 통과
- PlayMode: 전체 스위트 1회차 52/53 (`Test_Stage01_PlayerMovesJumpsAndUsesGrabSurface` 실패), 해당 테스트만 격리 재실행 시 1/1 통과 → **flaky, 이번 변경과 무관** 판단(입력 이벤트 타이밍 경합으로 추정. PlayerController/InputSystem 코드는 이번 세션에서 건드리지 않음)
- Console error 0
- Play Mode 실측 로그: `anchor=... player=... stageState=Explore` → `Begin=True state=Memory` → `advance=True / advance=True / advance=False → state=Chase` → `chase controller found=True`
- 회상 앵커 `#FFD98A` 렌더링, 트라우마 실루엣 렌더링, `B1_VisualGuide` 비활성 상태를 Scene 2D 캡처로 시각 확인

### Gate 판정 갱신 (2차)

- **G4 (Vertical Slice): PARTIAL 유지, 시각 blockout 문제는 해소.** 사람이 실제 키 입력(마우스/키보드)으로 완주한 기록은 여전히 없음 — 이번 세션은 리플렉션 기반 API 호출로 상태 전이만 검증했다. 다음 세션에서 실제 입력 시뮬레이션 또는 수동 QA로 Title→탈출 완주 필요.
