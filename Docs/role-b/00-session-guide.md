# Role B Stage 1 세션 가이드

## 작업 계약

- GitHub Issue: [#3 — Role B Stage 1 추격·레벨 구현](https://github.com/JugleGame/project-daeum/issues/3)
- 기준 branch: `dev`
- 작업 branch: `3-feat-role-b-stage1-chase-level`
- 역할: B — 추격·레벨
- 기준 Spec: draft v3의 spec-004, spec-006, spec-007, spec-012
- 목표: Stage 1에서 `탐색 → 기억 완료 → 오염 전환 → 추격 → 탈출`의 Role B 범위를 실제 플레이로 검증한다.

이 디렉터리의 문서는 `Docs/collaboration-workflow.md` 전체를 세션마다 다시 읽지 않도록 만든 실행용 요약이다. 요구사항 충돌 시 GitHub Issue #3과 원본 Spec을 우선하고, 공용 contract 변경은 구현 전에 영향 범위를 기록한다.

## 세션 실행 순서

| 순서 | 세션 문서 | 완료 결과 |
|---|---|---|
| B1 | `01-stage-data-blockout.md` | StageData 스키마, Stage 1 데이터, 이동 가능한 blockout |
| B2 | `02-remnant.md` | 근접형 Remnant와 focused tests |
| B3 | `03-encounter.md` | Encounter, Wave, 출구 잠금, 지형 상호작용 |
| B4 | `04-contamination-director.md` | pressure, additive overlay, Director |
| B5 | `05-chase-slice.md` | Stage 1 추격과 탈출 연결 |
| B-QA | `99-integration-qa.md` | 통합 회귀, 실제 플레이 증거, build |

같은 branch와 Scene을 공유하므로 세션은 동시에 실행하지 않는다. 각 세션은 직전 세션의 변경을 검토하고 테스트한 뒤 작업하며, 완료 시 한 개 이상의 명확한 커밋과 아래 인수인계를 남긴다.

## 세션 시작용 요청

새 Codex 세션에는 다음처럼 요청한다.

```text
Issue #3 Role B 작업을 진행해줘.
Docs/role-b/00-session-guide.md와 Docs/role-b/<해당 문서>.md만 먼저 읽고,
현재 branch와 선행 세션 결과를 확인한 뒤 해당 문서 범위만 구현·검증해줘.
```

## 소유권과 변경 경계

Role B가 직접 소유한다.

- `Assets/Scenes/Stage01_Base.unity`
- `Assets/Scenes/Stage01_Overlay_Echo.unity`
- `Assets/Scenes/Stage01_Overlay_Intrusion.unity`
- `Assets/Scripts/Enemy/`
- `Assets/Scripts/Encounter/`
- `Assets/Scripts/Contamination/`
- Role B용 Stage/Encounter/Variant 데이터와 테스트

직접 수정하지 않는다.

- Role A: `Boot.unity`, `Persistent.unity`, `RoleAPrototype.unity`, Core/Player/Flow/Interaction 구현
- Role C: `Title.unity`, UI, Memory, Audio 및 art 자산
- 다른 역할 소유 Prefab

연결은 public API와 event를 우선한다. 공용 contract 수정이 불가피하면 변경 이유, sample payload, 발생 조건, 소비 결과를 인수인계에 남기고 소비 역할의 통합 확인 전 완료로 처리하지 않는다.

## 현재 사용 가능한 contract

- `Daeume.Core.IDamageable`, `DamageTargetKind.Remnant`, `DamageRequest`, `DamageResult`
- `PlayerAggressionChanged(encounterId)`
- `Daeume.Core.StageState`: `Explore`, `Memory`, `Chase`, `Cleared`, `Failed`
- `Daeume.Encounter.EncounterState`: `Inactive`, `Active`, `Cleared`
- `Daeume.Contamination.PressureStage`: `Stable`, `Echo`, `Intrusion`, `Collapse`
- `SceneFlowController.RequestOverlay(sceneName, load)`
- `SceneFlowController.SaveChaseCheckpoint(checkpointId, position, health, variantId)`
- `TraumaContactSource`: 공격 무효인 Trauma 대상
- `GameManager.Instance.Events`: typed publish/subscribe

`MemoryComplete` event와 `AssistSettings` 실제 구현이 없으면 B4/B5는 교체 가능한 debug trigger/default 설정으로 진행하고, 실제 contract 연결 전에는 통합 완료로 표시하지 않는다.

## 공통 완료 기준

1. Compile error와 예상하지 않은 Console error가 0이다.
2. 문서에 지정된 focused EditMode/PlayMode test가 통과한다.
3. Unity Editor에서 실제 입력과 상태 전이를 확인한다.
4. Role A contract와 통합 smoke test를 수행한다.
5. 테스트 수, 실패 수, Console error 수와 수동 QA 결과를 기록한다.
6. 마지막 B-QA 세션에서 full loop regression과 player build를 확인한다.

## 세션 종료 인수인계 형식

각 기능 문서의 `세션 결과`를 갱신한다.

```text
- 상태: 완료 / 부분 완료 / 차단
- 커밋: <SHA 또는 미커밋 사유>
- 구현: <핵심 결과>
- 테스트: <명령/fixture, 통과 수, 실패 수>
- 수동 QA: <확인한 실제 플레이 흐름>
- contract 변경: <없음 또는 payload/발생 조건/소비 결과>
- 다음 세션 주의점: <경로, 임시 mock, 알려진 문제>
```

`ProjectSettings/ProjectSettings.asset`에는 branch 생성 전에 존재하던 사용자 변경이 감지됐다. 원인을 확인하지 않은 채 수정, stage 또는 되돌리지 않는다.
