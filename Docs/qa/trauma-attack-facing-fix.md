# Trauma Attack / Facing Functional Test Contract

## Identity

- Contract ID: `TRAUMA-ATTACK-FACING-FIX`
- Issue: `N/A - external Unity project follow-up requested in the current conversation`
- Feature: 트라우마 공격 state 연결 및 추격 방향 보정
- Acceptance Criterion: Unity renderer에서 확인한 왼쪽 authored-facing을 기준으로 실제 수평 이동 방향을 반영해 트라우마가 플레이어 쪽을 보며 추격·공격하고, 모든 Trauma frame 경계에 밝은 중성색 halo가 남지 않는다.
- Source revision: current `daeum` worktree
- Owner: Codex

## Test selection

- Focal mode: PlayMode + EditMode
- Focal test names:
  - `Test_Animation_TraumaMapsDirectiveToChase`
  - `Test_Animation_TraumaMapsGrabToAttack`
  - `Test_Animation_FinalPixelArtFramesDriveHeroAndTraumaClips`
  - `Test_Animation_TraumaFramesPreserveTransparentBackground`
  - `Test_Animation_TraumaFramesHaveNoBrightNeutralBoundaryPixels`
- Regression mode and names:
  - PlayMode - `Test_Combat_TraumaContactStartsGrabThenFails`
  - PlayMode - `Test_Trauma_ChaseIgnoresPlayerVerticalMovement`
  - PlayMode - `Test_Animation_PlayerMapsGameplaySignalsAndFacing`
- Additional gates:
  - `Stage01_Trauma` prefab / Trauma Animator 구조 검사
  - Trauma Idle/Chase/Attack frame 방향 human review
  - PlayMode console smoke

## Given

- Scene or prefab: isolated Trauma driver fixture and `Assets/Prefabs/Enemy/Stage01_Trauma.prefab`
- Initial state: Trauma Idle, authored sprite faces left, no grab event pending
- Test data or fixture: player positions on both sides, `TraumaGrabStarted`, 4-frame 10 fps Attack clip
- Determinism controls: direct directive/event calls and explicit driver tick durations

## When

1. 왼쪽 및 오른쪽 플레이어 위치를 포함한 chase directive를 각각 1회 적용한다.
2. 붙잡기 시작 이벤트를 1회 발행하고 driver를 0초, 이어서 0.41초 tick한다.

## Then / Test Oracle

| ID | Observable outcome | Expected value or state | Tolerance or deadline | Evidence field |
|---|---|---|---|---|
| T1 | 실제 왼쪽 추적 이동 | `LastHorizontalMovement < 0`, Chase `SpriteRenderer.flipX == false` | 같은 driver tick | NUnit assertion |
| T2 | 실제 오른쪽 추적 이동 | `LastHorizontalMovement > 0`, Chase `SpriteRenderer.flipX == true` | 같은 driver tick | NUnit assertion |
| T3 | 왼쪽 붙잡기 공격 진입 | `CurrentState == Attack`, `flipX == false` | 이벤트 직후 tick | NUnit assertion |
| T4 | 공격 종료 | `CurrentState == Idle`, `flipX == false` | 0.41초 tick | NUnit assertion |
| T5 | 공격 clip 구성 | 4 frames, 10 fps, non-loop | EditMode asset load | NUnit assertion |
| T6 | 생성 frame 배경 | 네 모서리 alpha 0, opaque coverage 75% 미만 | EditMode PNG load | NUnit assertion |
| T7 | Trauma frame 밝은 테두리 | 밝은 중성색이 투명 픽셀과 맞닿은 픽셀 수 `0` | EditMode PNG load | NUnit assertion |

Forbidden side effects:

- chase 거리/속도, 접촉 실패 시간, player HP 변경

## Execution record

- Compile: Unity script compilation 성공, compiler error `0` (수정 전에는 `TraumaAnimationState.Attack` 부재를 test compile failure로 재현)
- Focal named tests:
  - PlayMode `2/2` passed, failed `0`: `Test_Animation_TraumaMapsDirectiveToChase`, `Test_Animation_TraumaMapsGrabToAttack`
  - EditMode `4/4` passed, failed `0`: `Test_Animation_TraumaFramesHaveNoBrightNeutralBoundaryPixels`, `Test_Animation_TraumaFramesPreserveTransparentBackground`, `Test_Animation_FinalPixelArtFramesDriveHeroAndTraumaClips`, `Test_Animation_HeroFramesHaveNoBrightNeutralBoundaryPixels`
- PlayMode console smoke: `Test_Runtime_BootPersistentTitle_NoConsoleErrors` `1/1` passed, Console Error `0`
- Regression and additional gates:
  - impacted PlayMode regression `3/3` passed, failed `0`
  - prefab/controller/Stage01 structural EditMode gate `3/3` passed, failed `0`
  - `imagegen` 편집 및 human review: `attack_01`은 굽힌 왼쪽 reach, `attack_02`는 완전한 왼쪽 extension으로 변경해 머리와 공격 방향을 통일함
  - 생성 frame을 기존 canvas/content bounds에 nearest-neighbor로 정규화하고 실제 alpha 배경을 복원함
  - Unity renderer 좌우 비교 캡처로 `move_00`을 포함한 현재 Trauma frame이 왼쪽 authored-facing임을 확인하고, directive 좌표 추정 대신 `TraumaChaseActor.LastHorizontalMovement`의 실제 이동 delta를 정면 판정에 사용함
  - 수정 전 `attack_00.png`에서 밝은 중성색 경계 픽셀 `1,156`개를 재현했고, 동기화 도구가 Trauma 14 frame 전체에서 `14,891`개를 주변의 어두운 불투명 색으로 치환함. 수정 후 T7은 전 frame `0`으로 통과함
- Final build: Windows x64 산출물 생성 확인, output `Builds/TraumaDirectionNoiseFix/daeum.exe`
- QA status: `PASS`
- Evidence-based reason: 모든 focal/회귀/구조/smoke gate가 failure 및 Console Error 0으로 완료됐고 final Windows build도 error 0으로 성공했다.
- Retry count for the same failure: `0`; 잘못된 authored-facing 가정을 Unity renderer 비교로 바로잡고 실제 이동 delta 기반으로 수정한 뒤 동일 PlayMode focal `2/2` 통과
- Next action: 완료
