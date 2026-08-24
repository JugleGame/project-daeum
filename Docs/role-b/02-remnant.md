# B2 — Stage 1 근접형 Remnant

## 목표

Role A 전투 contract에 연결되는 근접형 Remnant 한 종을 구현하고 Stage 1 blockout에서 공격, 피격, 소멸과 pressure 반응을 검증한다.

## 기준 요구사항

- Spec: `daeume__spec-004-remnant-enemies` draft v3의 8일 슬라이스 범위
- 구현 대상: 근접형 1종, 공통 상태 6종, `Echo` 이상에서 트라우마를 바라보는 행동
- 공통 상태: 대기, 인지, 접근, 공격, 피격, 소멸
- Remnant는 `IDamageable`과 `DamageTargetKind.Remnant`를 사용한다.
- 소멸 후에는 Player에 피해를 줄 수 없다.
- pressure 변화는 데이터에 선언하고 예고 신호를 제거하지 않는다.

## 선행조건

- B1 완료: Stage 1 데이터와 spawn marker가 존재한다.
- Role A `PlayerCombat`, `PlayerHealth`, `IDamageable`의 현재 구현을 다시 확인한다.
- `PressureStage`는 기존 enum을 사용하며 signature를 임의로 바꾸지 않는다.

## 구현 범위

- 근접형 Remnant 데이터와 runtime component
- 상태 전이와 공격 전 명확한 telegraph
- Player 공격을 통한 피격/소멸
- Player에 대한 damage 요청
- 소멸 시 Collider, 공격 판정과 재피해 완전 비활성화
- `Echo`, `Intrusion`에서 트라우마 방향을 소비하는 최소 반응
- B1 spawn marker에 배치 가능한 Role B 소유 Prefab 또는 Scene object

## 범위 제외

- 돌진형, 원거리형
- Stage 9 모방, Stage 11 `Reactive`, 인간형 trait 강제
- 전체 pressure 변화표와 최종 animation/art
- Trauma 이동과 chase 판단

## 검증

- `Test_Remnant_DeathDisablesDamage`
- 슬라이스 범위의 `Test_Remnant_RespondsToTraumaPressure`
- 근접형의 선언된 공통 상태와 telegraph를 확인하는 focused test
- PlayMode: Player 공격 → Remnant 피격/소멸, Remnant 공격 → Player HP 변화, 소멸 후 피해 0회
- Compile/Console error 0

## 완료 조건

- Remnant가 Role A contract만으로 상호 피해 흐름에 연결된다.
- 공격 예고가 존재하고 pressure 변화 후에도 0초가 되지 않는다.
- Scene의 spawn marker와 연결되며 Encounter 없이도 focused test가 가능하다.

## 세션 결과

- 상태: 완료
- 커밋: `90fabc8` (`feat: add Stage 1 melee remnant`)
- 구현: Stage 1 근접형 Remnant 데이터, `Idle/Alert/Approach/Attack/Hit/Dead` 6상태 runtime, 공격 telegraph, Role A 상호 피해, 사망 후 Collider·공격 차단, `Echo/Intrusion` 트라우마 방향 반응을 추가했다. Prefab을 `stage01.remnant.spawn.01`에 연결했다. 둥근 내장 UI sprite로 읽기 어려웠던 B1 blockout도 Role B 사각 placeholder와 구역 label/marker 표시로 교체했다.
- 테스트: B2 EditMode 2/2, B2 PlayMode 5/5 통과. 시각 수정 후 전체 EditMode 29/29, 전체 PlayMode 32/32 통과. 최종 실패 0, Unity Console error 0.
- 수동 QA: Unity 2D Scene 캡처로 START/RECOVERY/ENCOUNTER/MEMORY·CHASE 구역, spawn marker와 Remnant 배치를 확인했다. Stage01 Prefab의 telegraph renderer 활성화, Player 공격 3회 소멸, 소멸 후 판정 비활성화를 PlayMode에서 확인했다.
- contract 변경: 기존 공용 contract 변경 없음. `Daeume.Enemy.MeleeRemnant`, `MeleeRemnantData`, `RemnantState`, `RemnantPressureProfile`을 새 Role B API로 추가했다. B4가 `SetPressure`와 `SetTraumaTarget`을 호출할 수 있다.
- 다음 세션 주의점: B3는 `Assets/Prefabs/Enemy/Stage01_MeleeRemnant.prefab`을 wave spawn 대상으로 사용하고, spawn 시 `SetTarget`을 생략해도 `DamageTargetKind.Player`를 자동 탐색한다. Scene의 기존 focused instance는 Encounter 연결 시 직접 재배치하지 말고 marker/prefab 참조로 전환한다.
