# Title Main Screen Functional Test Contract

## Identity

- Contract ID: `TITLE-MAIN-SCREEN`
- Feature: 《다음에》 메인 화면 최종 아트 및 접근 가능한 메뉴
- Source revision: `27d10d3` + current worktree
- Acceptance Criterion: 최종 concept-art 배경과 메뉴가 `Title` scene에서 표시되고 keyboard/gamepad focus 및 콘텐츠 경고가 동작한다.

## Test selection

- Focal mode: PlayMode
- Focal name: `Test_Title_MainScreenUsesFinalArtAndAccessibleMenu`
- Regression: `Test_Runtime_BootPersistentTitle_NoConsoleErrors`
- Additional gates: `1920 × 1080` Game view capture, Console error smoke, Windows build

## Given

- Scene: `Assets/Scenes/Title.unity`
- Initial state: scene 단독 load, 기존 EventSystem 활성, 콘텐츠 경고 panel 비활성
- Asset: `Assets/Art/UI/Title/MainScreen_Background.png`

## When

1. `Title` scene을 load하고 두 frame 대기한다.
2. background, menu hierarchy, button RectTransform과 초기 selection을 읽는다.
3. `ContentWarningButton.onClick`을 호출한다.
4. `ContentWarningCloseButton.onClick`을 호출한다.

## Then

- Background `Image.sprite.name == "MainScreen_Background"`, `preserveAspect == true`
- `MenuSafeArea`의 right anchor가 화면 폭 40% 이하에 있다.
- `NewGameButton`, `ContinueButton`, `SettingsButton`, `ContentWarningButton`의 높이가 모두 44 px 이상이다.
- 초기 selection은 `NewGameButton`이다.
- warning open 후 panel이 active이고 selection은 close button이다.
- warning close 후 panel이 inactive이고 selection은 warning button으로 복원된다.
- warning body는 `StringTable`의 `title.content_warning.body`를 사용한다.

## Evidence

- Focal result: Unity Test Runner의 exact full name과 `Passed` 상태
- Runtime smoke: Console `Error` 0
- Layout: 1920 × 1080 capture에서 menu와 protagonist/trauma silhouette 비중첩
- Build: StandaloneWindows64 artifact 및 build error 0

## Status

- Current: `INCOMPLETE`
- PASS condition: focal, regression, Console smoke, capture, final build가 모두 성공
