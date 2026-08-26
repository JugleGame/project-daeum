# 《다음에》 메인 화면 UI 디자인 규격

## 디자인 의도

메인 화면 한 장에서 `따뜻한 기억`과 `침식하는 trauma`를 동시에 보여준다. 버스정류장과 내리막길은 반복되는 기억의 장소를, 우측의 거대한 trauma는 회피할 수 없는 현재를 상징한다. 메뉴는 좌측 어둠 안에 배치해 읽기 흐름을 방해하지 않는다.

## 화면 구성

- 기준 해상도: `1920 × 1080`, `Canvas Scaler / Scale With Screen Size`, match `0.5`
- 배경: full stretch. `Preserve Aspect` 활성화, `AspectRatioFitter = Envelope Parent`
- 메뉴 safe area: 화면 좌측 `7%`, 상단 `9%`, 폭 `31%`
- 제목: `다음에`, 좌측 정렬, 약 `132 px`, warm cream
- 부제: `기억은 사라지지 않고, 모양을 바꾼다`, 제목 아래 `24 px`
- 메뉴: `새 게임 / 이어하기 / 설정`, 세로 간격 `6 px`, 각 항목 최소 높이 `82 px`
- 하단: `Enter 또는 클릭으로 선택`과 `콘텐츠 경고`
- 콘텐츠 경고 진입 시 권장 문구: `이 게임은 상실, 죽음, 중환자실과 관련된 장면 및 표현을 포함합니다.`

## Color tokens

- `Memory Cream`: `#F4E8D3` — title, active label
- `Amber Focus`: `#E4A35C` — focus bar, cursor, underline
- `Soot Navy`: `#0C0D14` — background fallback
- `Warm Secondary`: `#C69668` — content warning
- `Muted Copy`: `#EBDECA` at `72%` alpha — subtitle
- `Inactive Label`: `#EEE2CF` at `68%` alpha

## UI state

- `Normal`: transparent background, inactive label color
- `Highlighted / Selected`: 2 px amber left bar, `›` cursor 표시, 좌→우로 사라지는 `18%` amber gradient
- `Pressed`: selected 상태를 유지하며 gradient alpha를 `26%`로 올림
- `Disabled`: label alpha `34%`, cursor와 focus bar 숨김
- initial focus: `새 게임`
- navigation: `Up / Down` 순환, `Enter / Submit` 실행, pointer hover 시 동일 focus state
- focus state는 색뿐 아니라 left bar와 `›` 모양으로도 전달한다.

## Unity hierarchy 제안

```text
TitleCanvas
├─ BackgroundImage
├─ LeftReadabilityGradient
├─ MenuSafeArea
│  ├─ Brand
│  │  ├─ Kicker
│  │  ├─ Heading
│  │  └─ Subtitle
│  ├─ MainActions
│  │  ├─ NewGameButton
│  │  ├─ ContinueButton
│  │  └─ SettingsButton
│  └─ Footer
│     ├─ InputHint
│     └─ ContentWarningButton
└─ VersionLabel
```

`TitleMenuController`의 기존 `StringTable` binding과 `newGameButton / continueButton / settingsButton` 연결은 유지한다. 텍스트를 background PNG에 bake하지 않는다.

## Asset import

- 파일: `Assets/Art/UI/Title/MainScreen_Background.png`
- `Texture Type`: Sprite (2D and UI)
- `Sprite Mode`: Single
- `Mesh Type`: Full Rect
- `Filter Mode`: Point
- `Compression`: None 또는 High Quality 플랫폼 override
- `Generate Mip Maps`: Off
- 색공간: sRGB

## 검수 기준

- `1920 × 1080`, `1600 × 900`, `1280 × 720`에서 menu safe area가 protagonist/trauma 실루엣과 겹치지 않는다.
- keyboard/gamepad만으로 세 항목과 콘텐츠 경고에 도달할 수 있다.
- subtitle 및 inactive menu text가 실제 배경 위에서 읽힌다.
- `이어하기` 비활성 상태는 alpha와 cursor 부재를 함께 사용한다.
- 콘텐츠 경고가 새 게임 시작 전에 열릴 수 있다.
