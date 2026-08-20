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

## Scene 레벨 확인 — 실패

- `Stage01_MemoryAnchor.prefab`(실제 `MemoryCompleted` 발행 경로, `MemoryCompletionBridge` 포함)이 **어떤 `.unity` Scene에도 배치돼 있지 않다.** (`grep -rn "Stage01_MemoryAnchor" Assets --include=*.unity` 결과 0건)
- `Stage01_Base.unity`에는 여전히 구 debug 경로인 `MemoryCompletionAdapter`만 존재한다.
- `Stage01_Presentation.prefab`, `Stage01_PressurePresentation.prefab`(HUD, pressure 연출) 역시 어떤 Scene에도 배치돼 있지 않다.
- 즉 `a49a688`은 **에셋·스크립트 수준 산출물이며, 실제 플레이 경로에는 아직 통합되지 않았다.** handoff 문서([role-c-stage1-handoff.md:20](../role-c-stage1-handoff.md))가 명시한 "B 담당자가 Memory marker에 Memory prefab을 배치" 단계가 아직 실행되지 않은 상태다.

## Gate 판정

- **G3 (Memory → 오염 전환): 미통과.** 코드는 준비됐지만 Scene에 미배치되어 실제 기억 완료 event가 오염 단계를 전환하는 경로가 플레이에 존재하지 않는다.
- **G4 (Vertical Slice): 미통과.** G3 선행 조건 미충족. HUD·pressure presentation도 Scene 미배치라 Title→Stage1→기억→오염→추격 full loop 자체가 구성되지 않는다.
- **G5 (Release Candidate): 판정 불가.** G4 미통과로 대상 아님.

## Issue #3 (Role B) Acceptance Criteria 재확인

- B-QA 문서의 PARTIAL 판정은 유지된다. `a49a688`은 PARTIAL의 원인(실제 MemoryComplete 미연결)을 코드로는 해소했으나 Scene 통합이 없어 기능적으로는 여전히 미해결이다.

## 다음 조치 (배치 담당: B, `Stage01_Base` 소유자)

1. `Stage01_MemoryAnchor.prefab`을 `Stage01_Base.unity`의 Memory marker에 배치.
2. `MemoryCompletionAdapter`(debug 전용)를 제거하거나 `MemoryCompletionBridge`와 역할 중복 없이 공존하도록 정리.
3. `Stage01_Presentation.prefab`, `Stage01_PressurePresentation.prefab`을 Persistent 또는 C 전용 additive presentation Scene에 로드.
4. 배치 후 B-QA 수동 기능 검증 4번 항목("기억 완료 event 또는 명시된 debug trigger로 Stable → Echo → Intrusion")을 **실제 event 경로**로 재실행.
5. 재검증 후 Issue #3 Acceptance Criteria를 PARTIAL → PASS로 갱신.

## 재검증 효율 가이드

Role B QA 문서의 가이드를 그대로 따른다: focused fixture 우선, 전체 EditMode/PlayMode는 diff 확정 후 1회.
