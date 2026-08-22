+++
spec_id = "daeume__spec-002-movement-platforming"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-009", "ARCH-016", "GAME-057", "ELEM-050"]
dependencies = ["daeume__spec-001-core-loop"]
+++

# 이동과 플랫폼

## Goal
탐색과 추격에서 동일하고 예측 가능한 이동 규칙으로 저작된 Stable·오염 공간을 통과하게 하고, 붙잡기를 이 게임의 시그니처 이동 동사로 만든다.

## Build scope
- 8일 슬라이스: **전부 포함**.
- 붙잡기는 슬라이스의 필수 구현이다. Stage 1 추격의 담장·정류장 지붕 구간이 이 동사를 사용한다.
- 슬라이스에서 제외한다: Stage 13 접근 구간의 속도 제한.

## Implementation scope
- 좌우 이동, 지상 점프 1회, 낙하, 지면 판정, 공중 제어와 통과형 플랫폼을 제공한다.
- **붙잡기/매달리기**를 제공한다. 지정된 `Grabbable` 표면(난간, 담장 모서리, 배관, 창틀)에 공중이나 미끄러짐 중 접촉하면 붙잡기 입력으로 매달린다.
- 매달린 상태에서 가능한 행동은 좌우 이동 없음, 점프로 이탈, 아래 입력으로 낙하, 지속 시간 만료 시 자동 낙하 4가지뿐이다. 지속 시간은 `GrabHoldSeconds`로 선언한다.
- 붙잡기는 피해를 막지 않는다. 낙사 방지와 경로 연결에만 사용한다.
- 붙잡기 입력은 `daeume__spec-010-interaction-system`의 상호작용과 별개의 입력 액션이다.
- 기억 역류는 좌우 입력을 반전하거나 입력을 무작위로 무시하지 않는다.
- 생존 경로의 문·플랫폼 변형은 활성 전 시각 신호 1개와 음향 신호 1개를 제공한다.
- 재시도 시 동일 Variant의 충돌과 타이밍을 재현한다.
- 모든 이동·붙잡기 입력은 물리 키가 아니라 입력 액션 이름으로 바인딩하며 리매핑을 허용한다.
- Stage 13 마지막 접근 구간은 방향 조작을 유지하며 달리기만 걷기 속도로 제한할 수 있다.

## Out of scope
- 그래플, 벽 달리기, 대시, 비행, 무작위 미로
- 전투와 카메라 연출

## Acceptance criteria
- `Test_Player_MoveBothDirections`가 좌우 이동을 확인한다.
- `Test_Player_NoDoubleJump`가 공중 추가 점프를 차단함을 확인한다.
- `Test_Movement_SameRulesDuringChase`가 탐색·추격 이동 설정 일치를 확인한다.
- `Test_Movement_ContaminationNeverReversesInput`이 모든 압박 단계의 입력 방향 일치를 확인한다.
- `Test_Movement_VariantCollisionDeterministic`가 동일 체크포인트 재시도의 충돌 배치를 확인한다.
- `Test_Movement_GrabAttachesOnlyToGrabbable`이 `Grabbable` 표면에서만 매달림이 성립함을 확인한다.
- `Test_Movement_GrabAllowsOnlyDeclaredExits`가 매달린 상태에서 점프·낙하·자동 낙하 외의 이동이 0회임을 확인한다.
- `Test_Movement_GrabDoesNotBlockDamage`가 매달림 중 피해 차단이 0회임을 확인한다.
- `Test_Movement_InputBoundToActionNames`가 모든 이동 입력이 액션 이름으로 바인딩되고 리매핑 후에도 동작함을 확인한다.

## Verification method
- EditMode 입력·Variant 데이터 테스트
- PlayMode 이동 named tests
