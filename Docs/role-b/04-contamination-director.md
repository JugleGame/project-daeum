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

- 상태: 미시작
- 커밋:
- 구현:
- 테스트:
- 수동 QA:
- contract 변경:
- 다음 세션 주의점:
