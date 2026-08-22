+++
spec_id = "daeume__spec-003-player-combat"
version = 3
blueprint_version = 1
status = "draft"
refs = ["ARCH-028", "ELEM-051", "ELEM-050"]
dependencies = ["daeume__spec-002-movement-platforming"]
+++

# 플레이어 전투와 무장 상태

## Goal
잔재는 처치할 수 있지만 트라우마는 공격으로 해결할 수 없고, 트라우마 접촉은 보이는 붙잡기 연출로만 실패하며, Stage 13에서는 무장을 능동적으로 내려놓게 한다.

## Build scope
- 8일 슬라이스: **부분 포함**.
- 슬라이스에서 구현한다: 기본 공격, 피격, 체력, 무적 시간, 사망, 트라우마 공격 무효, 트라우마 접촉 붙잡기, `PlayerAggression` 판정.
- 슬라이스에서 제외한다: Stage 13 무장 해제.

## Implementation scope
- 기본 공격, 피격 판정, 체력, 무적 시간, 사망을 제공한다.
- 잔재 공격은 피해와 명확한 적중 피드백을 발생시킨다.
- **플레이어 → 트라우마** 공격은 피해, 경직, 진행도, 반동을 발생시키지 않는다.
- **트라우마 → 플레이어** 접촉은 피해를 0으로 두고 대신 붙잡기 연출을 시작한다. 연출 중 플레이어 입력은 무시하며, 연출이 끝나면 `StageState=Failed`로 전환하고 `daeume__spec-011-checkpoint-save`의 `ChaseCheckpoint` 복귀를 요청한다.
- 붙잡기 연출은 화면 안에서 시작하고 시작 시점에 음향 신호 1개를 발생시킨다. 화면 밖 접촉으로 시작하지 않는다.
- 붙잡기 연출 길이는 `TraumaGrabSeconds`로 선언하며 재시도마다 동일하다.
- `PlayerAggression`은 플레이어의 공격 입력이 잔재의 피격 판정에 **적중한 순간** 해당 Encounter 범위에서 설정되며, Encounter가 `Cleared`되거나 리셋될 때까지 유지된다. 빗나간 공격과 허공 휘두르기는 설정하지 않는다.
- Stage 13 수용 경계에서 공격 UI를 제거하고 상호작용으로 `WeaponLowered`를 1회 설정한다.
- 무장 해제는 체념 연출이 아니라 결말 진행의 명시적 플레이어 행동이다.

## Out of scope
- 장비 파밍, 무기 교체, 복잡한 콤보 트리
- 트라우마 체력과 처치 보상
- 트라우마의 이동·속도·추격 규칙 (`daeume__spec-006-trauma-chase`가 소유)

## Acceptance criteria
- `Test_Combat_AttackDamagesRemnant`가 선언 피해량을 확인한다.
- `Test_Combat_InvulnerabilityPreventsRapidHits`가 무적 시간 중 추가 피해를 차단함을 확인한다.
- `Test_Combat_TraumaAttackHasNoEffect`가 트라우마 상태와 수용 진행 불변을 확인한다.
- `Test_Combat_ZeroHealthTriggersFailure`가 체력 0에서 실패를 확인한다.
- `Test_Combat_TraumaContactDealsNoDamage`가 트라우마 접촉 시 체력 변화 0을 확인한다.
- `Test_Combat_TraumaContactStartsGrabThenFails`가 접촉에서 붙잡기 연출 1회와 `Failed` 전환을 확인한다.
- `Test_Combat_GrabSequenceIsDeterministic`이 동일 체크포인트 재시도에서 `TraumaGrabSeconds`가 같음을 확인한다.
- `Test_Combat_AggressionSetOnlyOnHit`가 빗나간 공격에서 `PlayerAggression` 설정 0회, 적중에서 1회를 확인한다.
- `Test_Combat_LowerWeaponOnceDuringAcceptance`가 지정 경계에서 무장 해제 1회를 확인한다.

## Verification method
- EditMode 피해 계산 테스트
- PlayMode 잔재·트라우마·무장 해제 named tests
