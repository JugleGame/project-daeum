# B5 — Stage 1 chase slice

## 목표

Stage 1에서 기억 완료 이후 오염 전환, 좌향 도주, 조명 기믹, Trauma 접근, 접촉 실패, 탈출점까지 이어지는 추격 slice를 통합한다.

## 기준 요구사항

- Spec: `daeume__spec-006-trauma-chase` draft v3의 Stage 1 chase 범위
- Stage 1은 출현점, 도주 방향, 재사용 경로, 고유 기믹, 탈출점과 실패 조건을 가진다.
- 추격 실패는 Trauma 접촉뿐이며 Role A contract가 판정과 결과를 소유한다.
- Player가 정지하면 Trauma는 계속 접근한다.
- 막힌 길에서는 Trauma를 물리고 `MaxDistance`를 유지하며 즉시 실패시키지 않는다.
- 경로 변화는 시각·음향으로 예고하고 필수 신호를 색만으로 구분하지 않는다.
- `ChaseSpeedAssist`는 속도와 접근 압박만 낮추며 경로, 기믹, 신호 timing, 탈출점은 바꾸지 않는다.

## 선행조건

- B1~B4가 완료되고 focused tests가 통과한다.
- Role A `TraumaContactSource`, `StageState`, `GameManager.Fail`, checkpoint/restore contract를 확인한다.
- 실제 `MemoryComplete`가 없으면 debug trigger 상태임을 QA 결과에 명시한다.
- Assist 설정 contract가 없으면 기본값과 debug toggle을 adapter로 격리한다.

## 구현 범위

- Stage 1 chase start, Trauma spawn/appearance, 좌향 도주 경로
- 조명 기반 고유 기믹과 색 외 형태/기호 신호
- Director 목표에 따르는 처치 불가능 Trauma actor
- 정지/막힌 길 거리 제어
- Trauma 접촉과 Role A 실패 contract 연결
- chase checkpoint 저장/복원과 동일 Variant 재적재
- 탈출점에서 `Chase → Cleared` 및 Stage 1 완료 flow 연결
- `ChaseSpeedAssist` adapter와 속도 전용 변화

## 범위 제외

- Stage 2~13 chase, 무작위 미로, 입력 반전/무작위 키 무시
- 최종 붙잡기 연출, audio/camera polish
- Trauma 접촉 실패 원인의 재정의

## 검증

- `Test_Trauma_StagesOneToTwelveCannotBeKilled` 중 Stage 1 범위
- `Test_Chase_StandingStillIsNotSafe`
- `Test_Chase_DeadEndBacksOffInsteadOfFailing`
- `Test_Chase_RequiredSignalsAreNotColorOnly`
- `Test_Chase_SpeedAssistChangesSpeedOnly`
- `Test_Contamination_RequiredRouteSignalsExist`
- Stage 1 retry/Variant 결정성 회귀
- PlayMode: 기억 완료/debug trigger → overlay → chase → 접촉 실패/복구 또는 탈출 → Cleared
- Compile/Console error 0

## 완료 조건

- Stage 1의 Role B full loop가 실제 입력으로 재현된다.
- 접촉 실패와 탈출 성공이 Role A flow contract를 통해 처리된다.
- assist on/off의 차이가 속도/접근 압박으로만 제한된다.
- B-QA 세션이 재현할 checkpoint와 조작 절차가 기록된다.

## 세션 결과

- 상태: 완료
- 커밋: `9e57d7e` (`feat: complete Stage 1 chase slice`)
- 구현: `StageOneChaseController`가 debug memory 완료 입력을 `Memory → Chase`로 연결하고 `Stage01_Chase` checkpoint에 player 위치/health/동일 contamination Variant를 저장한다. `TraumaChaseActor`는 기존 `ChaseDirectiveIssued`의 속도·거리 지시를 실행하되 추격 종료를 결정하지 않으며 `TraumaContactSource`를 유지한다. Director는 정지 player에게 `MinDistance`까지 계속 접근하고 dead-end zone에서는 `MaxDistance`까지 물러난다. 좌향/막힌 길/탈출 신호는 조명 색과 함께 `←`, `║`, `▣` 형태·문자 cue를 제공한다. `ChaseSpeedAssistAdapter`는 speed와 접근 거리 압박만 완화하고 route, signal, timing, escape, Variant는 바꾸지 않는다. 탈출 trigger는 Role A `SceneFlowController.CompleteStageOne`을 우선 사용하고 standalone smoke에서는 `GameManager` 상태 contract로 `Cleared`를 확인한다.
- 테스트: focused EditMode `StageOneChaseLayoutTests` 2/2, focused PlayMode `StageOneChaseTests` 5/5. 전체 회귀 EditMode 34/34, PlayMode 50/50. Compile error 0, Console error 0.
- 수동 QA: 저장하지 않은 QA player/runtime을 `Stage01_Base`에 임시 배치해 Context Menu debug memory trigger를 실행했다. `StageState.Chase`, `Stage01_Overlay_Intrusion` additive load, 좌향 입력 이동, Trauma directive 실행 및 접근을 확인했고 접촉 후 `StageState.Failed`를 확인했다. 이어 `Stage01_Chase` retry를 동일 Variant `Stage01_Overlay_Intrusion`으로 시작하고 escape trigger에 진입해 `StageState.Cleared`를 확인했다. 임시 객체는 Play Mode 종료 후 저장된 `Stage01_Base`를 다시 열어 제거했다.
- contract 변경: Role A public contract 변경 없음. 기존 `ChaseDirectiveIssued`, `TraumaContactSource`, `SceneFlowController.SaveChaseCheckpoint`, `SceneFlowController.CompleteStageOne`, `GameManager` 상태 전이를 그대로 소비한다. 실제 `MemoryComplete` contract는 아직 없어 `MemoryCompletionAdapter` 뒤에 격리했고, `AssistSettings.ChaseSpeedAssist` 저장값은 `ChaseSpeedAssistAdapter`가 소비한다.
- 다음 세션 주의점: B-QA는 `Stage01_Base`의 `B4_ContaminationDirector/MemoryCompletionAdapter`에서 debug memory 완료를 시작하고 왼쪽으로 달려 `Signal_Left_01 → Signal_DeadEnd_01 → Signal_Exit_01` 순서와 escape trigger를 재현한다. 실제 `MemoryComplete`가 생기면 `MemoryCompletionAdapter` 입력만 교체한다. `ChaseSpeedAssistAdapter`는 `SceneFlowController.CurrentData.AssistSettings`의 저장값을 사용한다. QA 종료 후 저장된 `Boot`를 활성 Scene으로 복구한다.
