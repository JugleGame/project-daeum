# Issue #40 Character Animation Mapping Functional Test Contract

## Identity

- Contract ID: `ISSUE-40-CHARACTER-ANIMATION`
- Issue: `#40`
- Feature: 플레이어 및 적 캐릭터 애니메이션 매핑
- Acceptance Criterion: 현재 sprite가 대상 prefab에 연결되고 gameplay signal에 따라 식별 가능한 Animator state로 전환되며 기존 기능 contract를 유지한다.
- Source revision: `40-feat-character-animation-mapping` worktree, base `f9de5ec`
- Owner: Codex

## Test selection

- Focal mode: EditMode + PlayMode
- Focal test names:
  - `Test_Animation_PrefabsUseCurrentSpritesAndControllers`
  - `Test_Animation_ControllersContainRequiredStatesAndFallbackClips`
  - `Test_Animation_Stage01UsesMappedCharacterPrefabs`
  - `Test_Animation_FinalPixelArtFramesDriveHeroAndTraumaClips`
  - `Test_Animation_RuntimeCharacterGraphsExcludeLegacyPixelArt`
  - `Test_Animation_PlayerMapsGameplaySignalsAndFacing`
  - `Test_Animation_RemnantMapsStateAndPreservesDeathContract`
  - `Test_Animation_TraumaMapsDirectiveToChase`
- Regression mode and names:
  - PlayMode - `Test_Player_MoveBothDirections`
  - PlayMode - `Test_Player_NoDoubleJump`
  - PlayMode - `Test_Remnant_CommonStateFlowPreservesTelegraph`
  - PlayMode - `Test_Remnant_DeathDisablesDamage`
  - PlayMode - `Test_Remnant_ThreeArchetypesBehaveAsDeclared`
  - PlayMode - `Test_Trauma_ChaseIgnoresPlayerVerticalMovement`
- Additional gates:
  - Animator Controller/prefab inspection
  - Stage01 Scene/Game view capture and human review
  - PlayMode console smoke

## Given

- Scene or prefab: `Player`, `Stage01_MeleeRemnant`, `DashRemnant`, `RangedRemnant`, `Stage01_Trauma`
- Initial state: player grounded with HP 3; Remnant `Idle`; Trauma without a chase directive
- Test data or fixture: `FinalDaeume` 64 PPU Hero 24-frame/Trauma 14-frame sprite, 기존 `RemnantTelegraph`, default gameplay data
- Determinism controls: direct public method calls, explicit damage clock, explicit `Tick` duration, no random animation selection

## When

1. Player move/jump/attack/damage calls, Remnant state-machine ticks, and Trauma chase directive are applied in the declared order.
2. Each animation driver is ticked once immediately after its corresponding gameplay signal.

## Then / Test Oracle

| ID | Observable outcome | Expected value or state | Tolerance or deadline | Evidence field |
|---|---|---|---|---|
| T1 | Prefab asset mapping | current sprite, Animator Controller, driver references exist | EditMode load | NUnit assertions |
| T1-R | Runtime dependency 역검사 | Persistent, Stage01, Player/Remnant/Trauma prefab graph의 legacy sprite 의존성 0건 | EditMode load | `AssetDatabase.GetDependencies` assertions |
| T2 | Player state mapping | `Idle → Move → Airborne/Grab → Attack → Damaged → Dead`; left facing flips sprite | same driver tick | `CurrentState`, `SpriteRenderer.flipX` |
| T3 | Remnant state mapping | `Idle → Alert → Approach → Attack → Hit → Dead`; death damage disabled | same driver tick | `CurrentState`, `CanDealDamage` |
| T4 | Trauma state mapping | `Idle → Chase` after directive | same driver tick | `CurrentState` |

Forbidden side effects:

- gameplay movement, combat damage, AI timing, chase distance, or original sprite file modification
- missing Animator, controller, clip, sprite, or prefab reference

## Execution record

### Compile

- Evidence: Unity script compilation 성공, compiler error `0`

### Focal named tests

- EditMode 최초 실행: `4/4` passed, failed `0` (`FinalPixelArtFramesDriveHeroAndTraumaClips` 포함)
- legacy 재검토 후 focal contract 직접 재실행: `5/5` passed, failed `0` (`RuntimeCharacterGraphsExcludeLegacyPixelArt` 포함)
- PlayMode: `3/3` passed, failed `0`
- 모든 focal name이 Unity Test Runner 결과에 존재함

### PlayMode console smoke

- `SceneSmokeTests.Test_Runtime_BootPersistentTitle_NoConsoleErrors`: `1/1` passed
- smoke 종료 후 Unity Console Error: `0`

### Regression and additional gates

- impacted PlayMode regression: `6/6` passed, failed `0`
- Unity import contract: Hero `24/24`, Trauma `14/14`, `64 PPU + Point + Uncompressed + mipmap off`, mismatch `0`
- prefab/controller 구조 검사: `2/2` passed
- Stage01/Persistent prefab 연결 구조 검사: `1/1` passed
- 적대적 runtime dependency 검사: Persistent/Stage01 및 캐릭터 prefab 7개 모두 legacy sprite dependency `0`, failure `0`
- legacy asset 제거 검사: `Player_Core` PNG/embedded texture, `RemnantBody`, Art/Resources의 `TraumaBody`가 모두 AssetDatabase에서 존재하지 않음
- legacy asset GUID 전역 검사: 참조 `0`
- Animator state 직접 sampling: Player 7개, Remnant 3종 각 6개, Trauma 3개 state가 모두 `FinalDaeume/Hero/Frames` 또는 `FinalDaeume/Trauma/Frames` sprite를 출력함
- Hero animation 최종 구성: Idle 4, Move 6, Attack 6, Airborne 4, Grab 4 frame. Attack은 양손 가방 swing/회수, Grab은 catch-settle-hold-recover loop를 사용함
- 최종 표시 규격: `FinalDaeume` 38개 frame을 `64 PPU + Point`, 모든 캐릭터 Visual을 `Scale 1`, Persistent Pixel Perfect Camera를 `64 Assets PPU + 1920×1080`으로 통일함
- Player/Trauma/Remnant Collider를 원본 크기 Visual의 발 기준 bounds에 맞춰 확장함
- 최종 focal EditMode contract `5/5`, PlayMode animation focal contract `3/3`, Console smoke/regression `7/7`, Console Error `0`
- Player 7-state, Remnant 6-state, Trauma Idle/Chase 및 별도 Attack clip을 2D Scene capture로 검토함
- 고해상도 Hero `Idle/Move/Attack/Jump/Grab`과 Trauma `Idle/Chase/Attack`의 원본 frame, pivot, 월드 크기를 확인함
- Trauma renderer가 `Character` sorting layer에서 모든 preview 상태로 표시됨을 확인함

### Final build

- 사용자 요청에 따라 프로젝트 build는 검증 범위에서 제외함

## Decision

- QA status: `INCOMPLETE`
- Evidence-based reason: 64 PPU import/clip contract, focal EditMode/PlayMode, console smoke, impacted regression은 통과했으나 사용자 요청에 따라 final build gate는 제외했다.
- Retry count for the same failure: 2 (Hero BottomCenter alignment에 맞게 stale test oracle을 수정한 뒤 동일 focal test 통과)

## Completion checklist

- [x] 모든 계약 값이 채워졌다.
- [x] 모든 focal name이 실제 결과에 존재한다.
- [x] focal run은 1건 이상 실행했고 failure가 0이다.
- [x] PlayMode runtime smoke의 Console error가 0이다.
- [x] impacted regression과 Animator/layout gate를 실행했다.
- [ ] 모든 선행 gate 뒤 final build를 실행했다. (사용자 요청으로 제외)
- [x] QA status를 하나만 선택했다.
