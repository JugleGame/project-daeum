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

```text
- 기준 commit:
- EditMode: <passed>/<total>
- PlayMode: <passed>/<total>
- Scene smoke: <passed>/<total>
- Console errors:
- Windows build:
- 수동 QA: PASS / FAIL
- 실제 contract 미연결 항목:
- 알려진 제한:
- Issue #3 Acceptance Criteria: PASS / PARTIAL / FAIL
```

기능 증거가 없거나 실제 contract가 필요한 구간이 mock 상태라면 functional PASS로 판정하지 않는다.
