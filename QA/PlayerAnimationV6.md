# Player Animation v6 Functional Test Contract

## Identity

- Contract ID: player-animation-v6
- Issue: N/A — 현재 외부 Unity 프로젝트에 승인된 애니메이션 에셋을 적용하는 작업
- Feature: Player v6 pixel-art animation integration
- Acceptance Criterion: v6의 31개 프레임이 손실 없이 import되고 기존 Player Animator 상태에서 정확한 순서와 재생 시간으로 표시된다.
- Source revision: `77045b696839297c2d4298ba6778c5d29ab0654f` 기반 working tree
- Owner: Codex host agent

## Test selection

- Focal EditMode: `Daeume.Tests.EditMode.CharacterAnimationMappingTests.Test_Animation_FinalPixelArtFramesDriveHeroAndTraumaClips`
- Focal PlayMode: `Daeume.Tests.PlayMode.CharacterAnimationDriverTests.Test_Animation_PlayerMapsGameplaySignalsAndFacing`
- Regression EditMode: `Daeume.Tests.EditMode.CharacterAnimationMappingTests.Test_Animation_PrefabsUseCurrentSpritesAndControllers`, `Daeume.Tests.EditMode.CharacterAnimationMappingTests.Test_Animation_ControllersContainRequiredStatesAndFallbackClips`, `Daeume.Tests.EditMode.CharacterAnimationMappingTests.Test_Animation_Stage01UsesMappedCharacterPrefabs`
- Regression PlayMode: `Daeume.Tests.PlayMode.PlayerFunctionalTests.Test_Movement_GrabAttachesOnlyToGrabbable`
- Additional gates: Animator/clip structural inspection, 2D scene capture, console smoke, final build

## Given

- Scene or prefab: `Assets/Prefabs/Player/Player.prefab`, `Assets/Scenes/Persistent.unity`
- Initial state: v6 256×256 PNG 31개와 기존 Player Animator Controller
- Test data or fixture: Nearest Neighbor로 64×64 변환한 idle 5, move 8, jump 6, attack 8, grab 4 frames
- Determinism controls: clip frame rate 고정, 직접 `Tick` 호출, fixed elapsed time

## When

1. v6 프레임을 `Assets/Art/Sprites/FinalDaeume/Hero/Frames`에 복사하고 동기화 메뉴를 실행한다.
2. 지정한 EditMode/PlayMode test를 실행하고 Player 상태를 Idle → Move → Airborne → Grab → Attack 순서로 전환한다.

## Then / Test Oracle

| ID | Observable outcome | Expected value or state | Tolerance or deadline | Evidence field |
|---|---|---|---|---|
| T1 | Texture import | 64×64 Sprite, 64 PPU, Point, Uncompressed, mipmap off, BottomCenter | 모든 31개 프레임 | EditMode assertions |
| T2 | Clip mapping | 5/8/6/8/4 frames와 지정 FPS/loop | 정확히 일치 | EditMode animation-curve assertions |
| T3 | Runtime state | Grab 상태와 Attack 전체 재생 시간이 유지됨 | Attack 0.60초 유지, 0.68초 후 복귀 | PlayMode assertions |
| T4 | Pixel presentation | PixelPerfectCamera 64 PPU, pixel snapping, upscale RT | 정확히 일치 | EditMode prefab/scene assertions 및 2D capture |

Forbidden side effects:

- 기존 Animator Controller GUID 변경, 프레임 smoothing/compression/mipmap, idle/run/jump/attack/grab 외 상태 변경, Console error

## Execution record

### Compile

- `EditorUtility.scriptCompilationFailed`: `False`
- Dynamic command compilation: success, compilation log empty
- Unity Console error entries: `0`

### Focal named tests

- EditMode requested: `Daeume.Tests.EditMode.CharacterAnimationMappingTests.Test_Animation_FinalPixelArtFramesDriveHeroAndTraumaClips`
- EditMode result: `Passed`, testCount `1`, failedCount `0`, duration `0.065s`
- PlayMode requested: `Daeume.Tests.PlayMode.CharacterAnimationDriverTests.Test_Animation_PlayerMapsGameplaySignalsAndFacing`
- PlayMode result: `Passed`, testCount `1`, failedCount `0`, duration `0.010s`
- Failures: none

### PlayMode console smoke

- Boot scene를 6초 동안 PlayMode 실행 후 정상 종료
- errorCount: `0`
- errors: `[]`

### Regression and additional gates

- EditMode regression: `3 Passed`, `0 Failed`
  - `Test_Animation_PrefabsUseCurrentSpritesAndControllers`
  - `Test_Animation_ControllersContainRequiredStatesAndFallbackClips`
  - `Test_Animation_Stage01UsesMappedCharacterPrefabs`
- PlayMode regression: `Test_Movement_GrabAttachesOnlyToGrabbable` — `Passed`
- Animator states: `Idle, Move, Airborne, Attack, Damaged, Dead, Grab`
- Hero Texture count: `31`
- Scene renderer: source `idle_00.png`, localScale `(1,1,1)`, bounds `(1,1,0.2)`
- 2D capture: 512×512, 캐릭터 silhouette·alpha·pixel edge 정상

### Final build

- Result: `Succeeded`
- Target: `StandaloneWindows64`
- Artifact: `C:/Users/jyp/Desktop/daeum/Builds/PlayerAnimationV6/daeum.exe`
- totalErrors: `0`
- totalWarnings: `493` — 기존 obsolete API 및 Sentis shader variant warning
- Size: `136199653` bytes
- Duration: `00:04:41.5371900`

## Decision

- QA status: PASS
- Evidence-based reason: focal 2개와 regression 4개가 모두 Passed이고, PlayMode error 0, 구조·화면 검사 통과, Windows build가 error 0으로 성공했다.
- Retry count: 1 — 최초 EditMode oracle이 Trauma 프레임 크기까지 64×64로 제한한 test fixture 오류를 수정하고 동일 test를 재실행했다.
- Next action: Unity Editor에서 사용자 최종 육안 확인
