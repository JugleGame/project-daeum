using System;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Daeume.UI
{
    /// <summary>
    /// spec-013 접근성 옵션 화면. (컨트롤 리매핑 / 카메라 흔들림 0 / 자막 크기 3단계 / 추격 속도 저하)
    ///
    /// 왜 코드로 UI를 짓나:
    /// 슬라이더·토글·버튼을 씬 파일에 직접 배치하면 중첩 프리팹 구조(배경/손잡이/체크마크 등)를
    /// 손으로 하나하나 만들어야 해서 실수하기 쉽다. 여기서는 Unity UI 기본 컴포넌트를
    /// AddComponent로 조립한다 — 못생겼지만(블록아웃 톤) 정확하고, 나중에 아트를 입힐 때
    /// 이 스크립트가 만든 오브젝트를 프리팹으로 옮기기만 하면 된다.
    ///
    /// 옵션 평가 문구를 넣지 않는다(spec-013): "쉬움 모드" 같은 표현은 어디에도 없다.
    /// </summary>
    [RequireComponent(typeof(AssistSettingsPresenter))]
    public sealed class AccessibilityOptionsPanel : MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;
        [SerializeField] private Color panelColor = new(0.05f, 0.05f, 0.07f, 0.96f);
        [SerializeField] private Color textColor = new(0.95f, 0.94f, 0.9f, 1f);
        [SerializeField] private Color accentColor = new(0.39f, 0.72f, 0.69f, 1f);

        // 이동은 항상 WASD/방향키로 고정한다(재배정 대상은 동작 버튼 4개).
        // 이동까지 재배정 대상에 넣으면 조합 바인딩(상하좌우 4개) UI가 두 배로 늘어나는데,
        // 슬라이스 범위에서는 "조작 재배정" 자체가 있다는 사실이 핵심이고, 이동 고정은 흔한 절충이다.
        private static readonly (string ActionName, string LabelKey)[] RebindableActions =
        {
            ("Jump", "options.rebind.jump"),
            ("Attack", "options.rebind.attack"),
            ("Grab", "options.rebind.grab"),
            ("Interact", "options.rebind.interact")
        };

        private AssistSettingsPresenter presenter;
        private SceneFlowController flow;
        private InputActionRebindingExtensions.RebindingOperation activeRebind;
        private bool isRebinding;
        private Slider shakeSlider;
        private Slider subtitleSlider;
        private Slider bgmSlider;
        private Slider sfxSlider;
        private Toggle chaseAssistToggle;
        private Text[] rebindKeyLabels;

        private void Awake()
        {
            presenter = GetComponent<AssistSettingsPresenter>();
            BuildUi();
        }

        private void OnEnable()
        {
            flow = FindAnyObjectByType<SceneFlowController>();
            var saved = flow?.CurrentData?.AssistSettings;
            if (saved != null)
            {
                presenter.Apply(saved);
                if (actions != null && !string.IsNullOrEmpty(saved.BindingOverridesJson))
                {
                    actions.LoadBindingOverridesFromJson(saved.BindingOverridesJson);
                }
            }

            RefreshControlsFromPresenter();
            RefreshRebindLabels();
        }

        private void OnDisable()
        {
            if (isRebinding) activeRebind.Cancel();
        }

        public void Close() => gameObject.SetActive(false);

        private void RefreshControlsFromPresenter()
        {
            var current = presenter.Current;
            if (shakeSlider != null) shakeSlider.SetValueWithoutNotify(current.CameraShakeStrength);
            if (subtitleSlider != null) subtitleSlider.SetValueWithoutNotify(current.SubtitleSize);
            if (bgmSlider != null) bgmSlider.SetValueWithoutNotify(current.BgmVolume);
            if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(current.SfxVolume);
            if (chaseAssistToggle != null) chaseAssistToggle.isOn = current.ChaseSpeedAssist;
        }

        private void SaveCurrent()
        {
            if (actions != null) presenter.SetBindingOverrides(actions.SaveBindingOverridesAsJson());

            flow ??= FindAnyObjectByType<SceneFlowController>();
            flow?.SaveAssistSettings(presenter.Current);
        }

        // ---- 재배정 ----

        private void StartRebind(int rebindIndex)
        {
            if (actions == null || isRebinding) return;
            var (actionName, _) = RebindableActions[rebindIndex];
            var action = actions.FindAction(actionName);
            if (action == null) return;

            var label = rebindKeyLabels[rebindIndex];
            var previousText = label.text;
            label.text = StringTable.Get("options.rebind.waiting");
            action.Disable();
            isRebinding = true;

            // bindingIndex 0 = 키보드 바인딩. 지정하지 않으면 액션의 다른 바인딩(예: 게임패드)까지
            // 함께 덮어써 버린다 — 실제로 겪은 버그다.
            activeRebind = action.PerformInteractiveRebinding(0)
                .WithControlsExcluding("Mouse")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnMatchWaitForAnother(0.1f)
                .OnComplete(operation => FinishRebind(operation, action, label))
                .OnCancel(operation => CancelRebind(operation, action, label, previousText));

            activeRebind.Start();
        }

        private void FinishRebind(InputActionRebindingExtensions.RebindingOperation operation, InputAction action, Text label)
        {
            operation.Dispose();
            isRebinding = false;
            action.Enable();
            label.text = ReadableBinding(action);
            SaveCurrent();
        }

        private void CancelRebind(InputActionRebindingExtensions.RebindingOperation operation, InputAction action, Text label, string previousText)
        {
            operation.Dispose();
            isRebinding = false;
            action.Enable();
            label.text = previousText;
        }

        private void RefreshRebindLabels()
        {
            if (actions == null || rebindKeyLabels == null) return;
            for (var i = 0; i < RebindableActions.Length; i++)
            {
                var action = actions.FindAction(RebindableActions[i].ActionName);
                if (action != null) rebindKeyLabels[i].text = ReadableBinding(action);
            }
        }

        private static string ReadableBinding(InputAction action)
        {
            return action.bindings.Count == 0
                ? "-"
                : action.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions);
        }

        // ---- 런타임 UI 조립 ----

        private void BuildUi()
        {
            var self = (RectTransform)transform;
            self.anchorMin = Vector2.zero;
            self.anchorMax = Vector2.one;
            self.offsetMin = Vector2.zero;
            self.offsetMax = Vector2.zero;

            var background = CreateImage("Background", self, panelColor);
            StretchFull(background);

            var closeButton = CreateButton("CloseButton", self, StringTable.Get("options.close"), new Vector2(0.5f, 0f), new Vector2(0, 60), new Vector2(240, 56));
            closeButton.onClick.AddListener(Close);

            var y = 420f;
            const float rowSpacing = 90f;

            CreateHeading("Heading", self, StringTable.Get("options.heading"), y);
            y -= rowSpacing;

            bgmSlider = CreateSliderRow(self, StringTable.Get("options.bgm_volume"), 0f, 1f, false, y);
            bgmSlider.onValueChanged.AddListener(value => { presenter.SetBgmVolume(value); SaveCurrent(); });
            y -= rowSpacing;

            sfxSlider = CreateSliderRow(self, StringTable.Get("options.sfx_volume"), 0f, 1f, false, y);
            sfxSlider.onValueChanged.AddListener(value => { presenter.SetSfxVolume(value); SaveCurrent(); });
            y -= rowSpacing;

            shakeSlider = CreateSliderRow(self, StringTable.Get("options.shake"), 0f, 1f, false, y);
            shakeSlider.onValueChanged.AddListener(value => { presenter.SetCameraShake(value); SaveCurrent(); });
            y -= rowSpacing;

            subtitleSlider = CreateSliderRow(self, StringTable.Get("options.subtitle_size"), 0f, 2f, true, y);
            subtitleSlider.onValueChanged.AddListener(value => { presenter.SetSubtitleSize(Mathf.RoundToInt(value)); SaveCurrent(); });
            y -= rowSpacing;

            chaseAssistToggle = CreateToggleRow(self, StringTable.Get("options.chase_assist"), y);
            chaseAssistToggle.onValueChanged.AddListener(value => { presenter.SetChaseAssist(value); SaveCurrent(); });
            y -= rowSpacing;

            rebindKeyLabels = new Text[RebindableActions.Length];
            for (var i = 0; i < RebindableActions.Length; i++)
            {
                var index = i;
                rebindKeyLabels[i] = CreateRebindRow(self, StringTable.Get(RebindableActions[i].LabelKey), y, () => StartRebind(index));
                y -= rowSpacing;
            }
        }

        private static void StretchFull(Graphic graphic)
        {
            var rect = graphic.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private Image CreateImage(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateLabel(string name, Transform parent, string text, int fontSize, Vector2 anchoredPosition, Vector2 sizeDelta, TextAnchor alignment = TextAnchor.MiddleLeft)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = KoreanFontBootstrap.KoreanFont
                ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize;
            label.color = textColor;
            label.alignment = alignment;
            return label;
        }

        private void CreateHeading(string name, Transform parent, string text, float y)
        {
            CreateLabel(name, parent, text, 34, new Vector2(0, y), new Vector2(900, 56), TextAnchor.MiddleCenter);
        }

        private Button CreateButton(string name, Transform parent, string text, Vector2 anchor, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;

            var image = go.GetComponent<Image>();
            image.color = accentColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = image;

            CreateLabel(name + "_Label", go.transform, text, 24, Vector2.zero, sizeDelta, TextAnchor.MiddleCenter);
            return button;
        }

        private Slider CreateSliderRow(Transform parent, string labelText, float min, float max, bool wholeNumbers, float y)
        {
            CreateLabel("Label_" + labelText, parent, labelText, 26, new Vector2(-420, y), new Vector2(420, 48));

            var go = new GameObject("Slider_" + labelText, typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(120, y);
            rect.sizeDelta = new Vector2(420, 32);

            var background = CreateImage("Background", go.transform, new Color(1f, 1f, 1f, 0.15f));
            StretchFull(background);

            var fillArea = new GameObject("FillArea", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);

            var fill = CreateImage("Fill", fillArea.transform, accentColor);
            StretchFull(fill);

            var slider = go.GetComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.targetGraphic = fill;
            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            return slider;
        }

        private Toggle CreateToggleRow(Transform parent, string labelText, float y)
        {
            CreateLabel("Label_" + labelText, parent, labelText, 26, new Vector2(-420, y), new Vector2(420, 48));

            var go = new GameObject("Toggle_" + labelText, typeof(RectTransform), typeof(Toggle));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(120, y);
            rect.sizeDelta = new Vector2(48, 48);

            var background = CreateImage("Background", go.transform, new Color(1f, 1f, 1f, 0.15f));
            StretchFull(background);

            var checkmark = CreateImage("Checkmark", go.transform, accentColor);
            StretchFull(checkmark);

            var toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = background;
            toggle.graphic = checkmark;
            return toggle;
        }

        private Text CreateRebindRow(Transform parent, string labelText, float y, Action onClick)
        {
            CreateLabel("Label_" + labelText, parent, labelText, 26, new Vector2(-420, y), new Vector2(420, 48));

            var button = CreateButton("Rebind_" + labelText, parent, string.Empty, new Vector2(0.5f, 0.5f), new Vector2(120, y), new Vector2(280, 56));
            button.onClick.AddListener(() => onClick());
            return button.GetComponentInChildren<Text>();
        }
    }
}
