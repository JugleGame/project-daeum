# B4 — ContaminationDirector와 Variant overlay

## 목표

Stage 1의 `Stable`, `Echo`, `Intrusion` pressure와 additive Variant overlay를 구성하고, 추격 길이와 거리 압박을 소유하는 `ContaminationDirector`를 구현한다.

## 기준 요구사항

- Spec: `daeume__spec-006-trauma-chase` draft v3의 Director/Variant 8일 슬라이스 범위
- Stage 1 Variant 1개와 `Stable`, `Echo`, `Intrusion`을 구현한다. `Collapse` runtime 콘텐츠는 제외한다.
- overlay는 base를 복제하거나 닫지 않고 additive로 적재한다.
- 같은 Variant는 재시도마다 같은 충돌, timing, spawn 결과를 만든다.
- 런타임 지오메트리 생성·이동·삭제로 Variant를 만들지 않는다.
- Director가 `TargetChaseSeconds`, `ChaseSpeed`, `MinDistance`, `MaxDistance`를 소비하고 추격 길이와 pressure를 소유한다.
- Trauma actor는 목표를 실행할 뿐 스스로 chase 종료를 결정하지 않는다.
- 선언된 연출 지점 외 순간이동은 금지한다.

## 선행조건

- B1의 Stage 1 `ContaminationVariantId`, 채널, `TargetChaseSeconds`가 유효하다.
- B3 Encounter 완료 결과를 소비할 수 있다.
- `MemoryComplete` 실제 event가 없으면 debug trigger를 adapter 뒤에 둔다.
- `SceneFlowController.RequestOverlay`와 `OverlaySceneLoadRequested`의 현재 동작을 확인한다.

## 구현 범위

- pressure 상태와 상태 변경 event/payload
- player distance 및 chase state를 Role C가 소비할 수 있는 event/payload
- `ContaminationDirector`의 시간/거리 제어
- `Stage01_Overlay_Echo`, `Stage01_Overlay_Intrusion` additive 저작과 load/unload adapter
- base Scene 보존, overlay 해제 복원, 재시도 결정성
- Scene에서 조절 가능한 debug pressure/distance control

## 범위 제외

- `Collapse`, Stage 2~13 Variant, 병원 이미지 전체 단계표
- 완성 audio/camera/art
- 추격 접촉 판정과 실패 결과 재정의

## 검증

- 슬라이스 범위의 pressure/Variant 선언 검사
- `Test_Contamination_VariantOverlaysBaseWithoutClosingIt`
- `Test_Contamination_OverlayUnloadRestoresBase`
- `Test_Chase_DirectorOwnsChaseLength`
- `Test_Chase_DirectorKeepsDistanceBounds`
- `Test_Chase_DirectorNeverTeleportsOutsideDeclaredPoints`
- `Test_Contamination_RetryUsesSameVariant`
- PlayMode: debug trigger → pressure 변경 → overlay load/unload → base 충돌 복원
- Compile/Console error 0

## 완료 조건

- Director만 chase 길이와 pressure를 결정한다.
- Role C handoff에 contract 위치, sample payload, 발생 조건, 소비 결과가 기록된다.
- 실제 `MemoryComplete`가 없어도 mock이 교체 가능한 adapter로 격리된다.

## 세션 결과

- 상태: 완료
- 커밋: `0e66fd9` (`feat: add Stage 1 contamination director`)
- 구현: `ContaminationVariantData`가 Stage01의 고정 Variant, Echo/Intrusion overlay, 45초 추격, 속도와 거리 범위를 선언한다. `ContaminationDirector`가 Stable/Echo/Intrusion 전환, additive overlay 요청, 추격 시간 종료와 연속 이동 기반 거리 보정을 소유한다. `OverlaySceneLoader`, B3 Encounter adapter, 교체 가능한 Memory debug adapter, Inspector debug pressure/distance control을 Stage01에 연결했다. 두 overlay의 충돌/표시는 Scene에 정적으로 저작했고 build scene 목록에서 활성화했다.
- 테스트: B4 focused EditMode 1/1, PlayMode 7/7. 전체 회귀 EditMode 32/32, PlayMode 45/45. Compile/Console error 0.
- 수동 QA: 저장된 Boot가 clean임을 확인한 뒤 Stage01 base와 Intrusion overlay를 additive로 함께 열어 사각 blockout 지형이 base를 닫거나 복제하지 않고 겹쳐지는 것을 2D capture로 확인했다. QA 뒤 overlay/base를 저장 없이 닫고 활성 Scene을 `Boot`로 복구했다.
- contract 변경: `ContaminationPressureChanged(VariantId, Pressure, OverlayScene)`는 pressure 전환 때 발생한다. `ChaseStateChanged(ChaseId, Active, ElapsedSeconds, TargetSeconds)`는 추격 시작/종료 때 발생한다. `ChaseDirectiveIssued(ChaseId, PlayerPosition, PursuerPosition, Distance, MinDistance, MaxDistance, Speed, RemainingSeconds)`는 추격 tick마다 발생하며 Role C는 위치/속도 목표를 소비하되 종료를 결정하지 않는다. 계약 위치는 `Assets/Scripts/ContaminationRuntime/ContaminationEvents.cs`다.
- 다음 세션 주의점: B5는 이름이 `Trauma`인 actor 또는 명시적 Transform을 Director에 연결하고 위 directive를 실행해야 한다. 실제 `MemoryComplete`가 생기면 `MemoryCompletionAdapter` 입력만 교체한다. B5와 B-QA는 이번 세션에서 수행하지 않았다.
