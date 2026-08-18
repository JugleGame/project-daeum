# B-QA — Role B Stage 1 통합 검증

## 목표

B1~B5 결과를 Issue #3의 하나의 독립적으로 검증 가능한 결과로 판정하고 PR에 넣을 증거를 정리한다.

## 시작 조건

- B1~B5 문서의 `세션 결과`가 갱신돼 있다.
- 알려진 mock/debug adapter와 실제 contract 연결 여부가 명시돼 있다.
- branch에 범위 밖 변경이나 출처 불명 변경이 없는지 확인한다.
- 기존 사용자 변경인 `ProjectSettings/ProjectSettings.asset`을 임의로 stage하거나 되돌리지 않는다.

## 자동 검증

- Role B 관련 EditMode tests 전체
- Role B 관련 PlayMode tests 전체
- 영향받은 Role A EditMode/PlayMode regression
- Scene smoke tests
- Compile error 0, 예상하지 않은 Console error 0
- Windows player build

테스트 이름과 결과 수치를 기록하고 단순히 “통과”라고만 적지 않는다.

## 수동 기능 검증

1. `Stage01_Base` 진입과 Player 이동/점프/붙잡기/낙하 복구를 확인한다.
2. 근접형 Remnant의 인지, 접근, 예고, 공격, 피격, 소멸을 확인한다.
3. Encounter 진입, 출구 잠금, Wave 진행, 전멸, 해제와 재진입 Spawn 0회를 확인한다.
4. 기억 완료 event 또는 명시된 debug trigger로 `Stable → Echo → Intrusion`과 overlay를 확인한다.
5. base Scene이 계속 열려 있고 overlay 해제 후 충돌이 복원되는지 확인한다.
6. 추격 중 정지가 안전하지 않고 막힌 길에서 즉시 실패하지 않는지 확인한다.
7. Trauma 공격 무효와 접촉 실패/체크포인트 복구를 확인한다.
8. `ChaseSpeedAssist`가 속도/압박 외 요소를 바꾸지 않는지 비교한다.
9. 탈출점에서 Stage 1 완료 flow를 확인한다.

## 소유권·diff 검토

- Role A/C 소유 Scene·Prefab 직접 변경 0건
- 공용 contract 변경은 Issue 범위, migration, sample payload와 소비자 확인을 포함
- 임시 mock/debug object는 production 경로에서 비활성화되거나 교체 지점이 명확함
- generated/cache/build artifact가 commit 대상에 포함되지 않음
- Scene/Prefab YAML 충돌과 missing reference 0건

## 최종 기록

- 기준 commit: `32272e7` (`test: add Role B integration QA coverage`)
- EditMode: focused `RoleBIntegrationQaTests` 1/1, 전체 35/35
- PlayMode: focused `RoleBIntegrationQaTests` 2/2, 전체 52/52
- Scene smoke: `SceneSmokeTests` 4/4 (전체 PlayMode 52/52에 포함)
- Console errors: 0
- Windows build: Windows x64 Development `Succeeded`, errors 0, warnings 3, `Build/RoleA/Daeume.exe` (`Build/`은 gitignore)
- 수동 QA: PASS (명시된 debug adapter 경로). 이동/점프/붙잡기/낙하 복구, Remnant 인지·예고·공격·피격·소멸, 2-wave Encounter 잠금/해제와 재진입 Spawn 0, `Stable → Echo → Intrusion`, base/overlay lifecycle, 정지 접근/dead-end 후퇴, Trauma 공격 무효·접촉 실패/checkpoint retry, assist 속도·압박 한정, escape 완료 flow를 확인했다.
- 실제 contract 미연결 항목: 실제 `MemoryComplete`, 실제 `AssistSettings`. 각각 `MemoryCompletionAdapter`, `ChaseSpeedAssistAdapter` 입력만 교체한다.
- 알려진 제한: 실제 contract가 없는 두 구간은 debug adapter로 검증했다. B-QA 중 Editor에서 `Persistent + Stage01_Base`를 함께 연 시작 순서에서 overlay loader 구독 누락을 발견해 `OnEnable/Start` 재연결과 late-manager 회귀 테스트를 추가했다. build warnings 3건은 script가 아직 없는 UI/Memory/Audio asmdef 경고다.
- 소유권/diff: Role A/C 소유 파일 신규 변경 0, 공용 contract 변경 0, Role B Scene missing script 0, build/cache 산출물 commit 0. 기존 사용자 변경 5개는 stage/commit하지 않았다.
- Issue #3 Acceptance Criteria: PARTIAL — Role B 기능과 회귀/build는 통과했지만 문서 규칙에 따라 실제 `MemoryComplete`/`AssistSettings` 연결 전 functional PASS로 판정하지 않는다.

## 재검증 효율 가이드

- 구현 중에는 변경된 focused fixture만 실행하고, 전체 EditMode/PlayMode는 최종 diff가 확정된 뒤 단계당 1회 실행한다.
- 코드/Scene 변경이 없는 상태에서는 동일 PlayMode suite를 반복하지 않는다. build 후 설정 부산물 확인이나 문서 변경만으로 PlayMode를 재실행하지 않는다.
- Test Runner 결과는 개별 로그 전체 대신 `passed/failed/skipped/inconclusive` 요약과 실패 항목만 기록한다. Console은 error filter로 확인한다.
- Windows build는 full warning stream 대신 `result/errors/warnings/outputPath` summary를 기록한다.

기능 증거가 없거나 실제 contract가 필요한 구간이 mock 상태라면 functional PASS로 판정하지 않는다.
