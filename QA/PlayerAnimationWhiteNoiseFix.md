# Player Animation White Noise Fix Contract

## Identity

- Contract ID: player-animation-white-noise-fix
- Issue: N/A — 현재 외부 Unity 프로젝트의 승인된 animation asset 결함 수정
- Feature: Player sprite bright-edge noise removal
- Acceptance Criterion: Hero 31프레임의 투명 경계에 밝은 무채색 halo 픽셀이 없고, 캐릭터 silhouette·alpha·AnimationClip mapping이 유지된다.
- Source revision: `77045b696839297c2d4298ba6778c5d29ab0654f` 기반 working tree
- Owner: Codex host agent

## Test selection

- Focal EditMode: `Daeume.Tests.EditMode.CharacterAnimationMappingTests.Test_Animation_HeroFramesHaveNoBrightNeutralBoundaryPixels`
- Regression EditMode: `Daeume.Tests.EditMode.CharacterAnimationMappingTests.Test_Animation_FinalPixelArtFramesDriveHeroAndTraumaClips`
- Regression PlayMode: `Daeume.Tests.PlayMode.CharacterAnimationDriverTests.Test_Animation_PlayerMapsGameplaySignalsAndFacing`
- Additional gates: 확대 edge capture, Player scene capture, PlayMode console smoke, final Windows build

## Given / When / Then

- Given: 64×64 Hero PNG 31개가 Point/Uncompressed/64 PPU로 import되어 있다.
- When: alpha 경계에 접한 RGB 채널 편차 28 이하, 평균 밝기 72 이상의 픽셀을 검사한다.
- Then: 검출 수는 모든 프레임에서 0이고, alpha silhouette와 clip frame count는 유지된다.
- Evidence: focal test result와 확대 edge capture.

Forbidden side effects:

- 의상·피부 palette 변경, alpha silhouette 삭제, AnimationClip/Animator GUID 변경, Console error

## Execution record

- Compile: `EditorUtility.scriptCompilationFailed=False`, dynamic compilation log empty
- Pre-fix focal: `Failed`, `attack_00.png` bright boundary pixels `12` — `PRODUCT_FAIL`
- Correction: 31프레임에서 밝은 무채색 외곽 픽셀 `411`개를 인접 dark outline 색으로 치환; alpha 변경 `0`
- Post-fix focal: `Passed`, testCount `1`, failedCount `0`, duration `0.044s`
- EditMode regression: `Test_Animation_FinalPixelArtFramesDriveHeroAndTraumaClips` — `Passed`, duration `0.046s`
- PlayMode regression: `Test_Animation_PlayerMapsGameplaySignalsAndFacing` — `Passed`, duration `0.011s`
- PlayMode console smoke: Boot 6초 실행, errorCount `0`, errors `[]`
- Additional gates: 8× edge 확대 검사 및 512×512 Player scene capture에서 bright speckle 없음; silhouette와 31개 texture 유지
- Final build: `Succeeded`, `StandaloneWindows64`, totalErrors `0`, totalWarnings `485`, size `136199653` bytes
- Artifact: `C:/Users/jyp/Desktop/daeum/Builds/PlayerAnimationNoiseFix/daeum.exe`

## Decision

- QA status: PASS
- Evidence-based reason: 동일 focal test 재실행과 EditMode/PlayMode regression, Console smoke, 확대·씬 capture, Windows build가 모두 통과했다.
- Retry count: 1 — 확인된 sprite edge product defect를 수정하고 동일 focal test를 재실행했다.
- Next action: 수정 빌드에서 사용자 최종 육안 확인
