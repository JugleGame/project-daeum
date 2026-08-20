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

## 다음 조치

1. Unity Editor Play Mode에서 `Stage01_Base` 진입 → Memory 완료 → 실제 `BeginChaseFromMemory` 호출 → Encounter/Chase 전환을 눈으로 확인.
2. HUD/pressure presentation이 `Resources.Load` 경로로 정상 표시되는지 확인.
3. 확인 후 Issue #3 Acceptance Criteria를 PARTIAL → PASS로 갱신하고 G4 실측 증거를 본 문서에 추가.

## 재검증 효율 가이드

Role B QA 문서의 가이드를 그대로 따른다: focused fixture 우선, 전체 EditMode/PlayMode는 diff 확정 후 1회.
