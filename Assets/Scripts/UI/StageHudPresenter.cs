using Daeume.ContaminationRuntime;
using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Daeume.UI
{
    /// <summary>
    /// HUD(체력·프롬프트·추격 경고·실패 문구)를 그린다. (spec-013)
    ///
    /// 구조의 핵심: 이 스크립트는 게임 로직을 전혀 모른다.
    /// 이벤트를 구독해 "받은 값을 글자로 옮기는" 일만 한다.
    /// 덕분에 UI를 바꿔도 게임 규칙이 깨지지 않고, 규칙이 바뀌어도 UI 코드를 고칠 일이 거의 없다.
    ///
    /// 모든 문장은 StringTable에서 가져온다. UI 코드에 원고를 담지 않는 것이 규칙이다.
    /// </summary>
    public sealed class StageHudPresenter : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text chaseText;
        [SerializeField] private GameObject promptRoot;   // 프롬프트 묶음(보임/숨김 전환용)
        [SerializeField] private GameObject chaseRoot;
        [SerializeField] private Text failText;
        [SerializeField] private GameObject failRoot;
        [SerializeField] private Text objectiveText;
        [SerializeField] private GameObject objectiveRoot;

        /// <summary>
        /// Stage 1은 튜토리얼 스테이지다. 목표 문구와 함께 조작 안내를 띄우지 않으면
        /// 새 게임을 시작한 플레이어가 아무 설명 없이 놓인다(#11).
        /// 액션 이름과 문자열 테이블 키만 여기 두고, 키 표기는 실제 바인딩에서 읽는다.
        /// </summary>
        private static readonly (string ActionName, string LabelKey)[] TutorialActions =
        {
            ("Move", "options.rebind.move"),
            ("Jump", "options.rebind.jump"),
            ("Interact", "options.rebind.interact"),
            ("Attack", "options.rebind.attack"),
            ("Grab", "options.rebind.grab")
        };

        public string HealthLabel { get; private set; } = string.Empty;
        public string ObjectiveLabel { get; private set; } = string.Empty;
        public string PromptLabel { get; private set; } = string.Empty;
        public bool PromptVisible { get; private set; }
        public bool ChaseVisible { get; private set; }

        // 조사 프롬프트가 "켜져야 한다"고 요청받은 원래 값. 추격 중에는 이 값이 참이어도 실제로는 숨긴다.
        private bool promptRequested;

        // 조작 안내는 바인딩이 바뀌지 않는 한 같은 문자열이라 한 번만 만든다.
        private string controlHint = string.Empty;

        // spec-013 자막 크기 3단계. 씬에 디자인된 원래 크기를 기준(1단계)으로 배율만 곱한다.
        private bool baseFontSizesCaptured;
        private int healthBaseSize, promptBaseSize, chaseBaseSize, failBaseSize, objectiveBaseSize;

        // OnEnable과 Start 두 곳에서 연결한다.
        // HUD가 GameManager보다 먼저 생성되는 순서가 실제로 있어서, 한 번만 시도하면 구독을 놓친다.
        private void OnEnable() => Connect();
        private void Start() => Connect();
        private void OnDisable() => Disconnect();

        /// <summary>코드로 UI 요소를 연결한다(프리팹 대신 런타임 구성이 필요할 때).</summary>
        public void Bind(Text health, Text prompt, Text chase, GameObject promptContainer, GameObject chaseContainer)
        { healthText = health; promptText = prompt; chaseText = chase; promptRoot = promptContainer; chaseRoot = chaseContainer; }

        private void Connect()
        {
            if (GameManager.Instance == null) return;

            // 중복 구독을 막기 위해 먼저 전부 해제한다.
            Disconnect();
            GameManager.Instance.Events.Subscribe<PlayerHealthChanged>(OnHealth);
            GameManager.Instance.Events.Subscribe<InteractionPromptChanged>(OnPrompt);
            GameManager.Instance.Events.Subscribe<ChaseStateChanged>(OnChase);
            GameManager.Instance.Events.Subscribe<StageFailed>(OnFailed);
            GameManager.Instance.Events.Subscribe<StageStateChanged>(OnStageStateChanged);
            ApplySubtitleSize();
        }

        /// <summary>spec-013 자막 크기 3단계를 HUD 문구에 반영한다. 값은 씬을 넘나드는 SceneFlowController가 들고 있다.</summary>
        private void ApplySubtitleSize()
        {
            if (!baseFontSizesCaptured)
            {
                if (healthText != null) healthBaseSize = healthText.fontSize;
                if (promptText != null) promptBaseSize = promptText.fontSize;
                if (chaseText != null) chaseBaseSize = chaseText.fontSize;
                if (failText != null) failBaseSize = failText.fontSize;
                if (objectiveText != null) objectiveBaseSize = objectiveText.fontSize;
                baseFontSizesCaptured = true;
            }

            var tier = FindAnyObjectByType<SceneFlowController>()?.CurrentData?.AssistSettings?.SubtitleSize ?? 1;
            var scale = SubtitleScale.Resolve(tier);
            if (healthText != null) healthText.fontSize = Mathf.RoundToInt(healthBaseSize * scale);
            if (promptText != null) promptText.fontSize = Mathf.RoundToInt(promptBaseSize * scale);
            if (chaseText != null) chaseText.fontSize = Mathf.RoundToInt(chaseBaseSize * scale);
            if (failText != null) failText.fontSize = Mathf.RoundToInt(failBaseSize * scale);
            if (objectiveText != null) objectiveText.fontSize = Mathf.RoundToInt(objectiveBaseSize * scale);
        }

        private void Disconnect()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Events.Unsubscribe<PlayerHealthChanged>(OnHealth);
            GameManager.Instance.Events.Unsubscribe<InteractionPromptChanged>(OnPrompt);
            GameManager.Instance.Events.Unsubscribe<ChaseStateChanged>(OnChase);
            GameManager.Instance.Events.Unsubscribe<StageFailed>(OnFailed);
            GameManager.Instance.Events.Unsubscribe<StageStateChanged>(OnStageStateChanged);
        }

        private void OnFailed(StageFailed value)
        {
            if (failText != null) failText.text = StringTable.Get("hud.failed");
            if (failRoot != null) failRoot.SetActive(true);
        }

        private void OnStageStateChanged(StageStateChanged value)
        {
            // 실패 문구는 상태가 실패에서 벗어나는 순간 자동으로 사라진다.
            // 지우는 책임을 별도 코드에 두지 않아, 문구가 화면에 남아 버리는 실수를 막는다.
            if (value.State != StageState.Failed && failRoot != null) failRoot.SetActive(false);

            // 목표 문구는 탐색 중에만 보인다. 회상을 시작하거나 추격에 들어가면 자연히 사라진다.
            var visible = value.State == StageState.Explore;
            var hint = ControlHint();
            ObjectiveLabel = string.IsNullOrEmpty(hint)
                ? StringTable.Get("hud.objective.memory")
                : $"{hint}{System.Environment.NewLine}{StringTable.Get("hud.objective.memory")}";
            if (objectiveText != null) objectiveText.text = ObjectiveLabel;
            if (objectiveRoot != null) objectiveRoot.SetActive(visible);
        }

        /// <summary>
        /// "[키] 동작" 목록을 만든다. 프롬프트 표기(OnPrompt)와 같은 형식이라 읽는 규칙이 하나뿐이다.
        /// </summary>
        /// <remarks>
        /// PlayerInput은 스테이지 씬에 있고 HUD는 씬을 넘나들며 살아남으므로, 아직 없을 수 있다.
        /// 만들어진 뒤에만 캐시해서 다음 상태 변화 때 다시 시도한다.
        /// </remarks>
        private string ControlHint()
        {
            if (!string.IsNullOrEmpty(controlHint)) return controlHint;

            var actions = FindAnyObjectByType<PlayerInput>()?.actions;
            if (actions == null) return string.Empty;

            var parts = new System.Collections.Generic.List<string>();
            foreach (var (actionName, labelKey) in TutorialActions)
            {
                var action = actions.FindAction(actionName);
                if (action == null || action.bindings.Count == 0) continue;

                // 첫 번째 바인딩만 읽는다. 인자 없이 부르면 연결 여부와 상관없이 모든 바인딩을
                // "E | Y"처럼 합쳐 줘서, 패드를 꽂지 않은 플레이어에게 눌리지 않는 키가 보인다
                // (InteractionTargeter에도 같은 경고가 있다). 0번은 키보드 바인딩이고,
                // 접근성 화면의 키 재설정도 0번을 고치므로 재설정 결과가 그대로 따라온다.
                // ponytail: 패드만 연결된 경우에도 키보드 키가 보인다. 패드 지원을 정식으로 다룰 때
                // PlayerInput.currentControlScheme으로 InputBinding.MaskByGroup을 걸면 된다.
                var keys = action.GetBindingDisplayString(0, InputBinding.DisplayStringOptions.DontIncludeInteractions);
                if (string.IsNullOrWhiteSpace(keys)) continue;

                parts.Add($"[{keys}] {StringTable.Get(labelKey)}");
            }

            if (parts.Count == 0) return string.Empty;

            controlHint = $"{StringTable.Get("hud.tutorial.hint")}  {string.Join("   ", parts)}";
            return controlHint;
        }

        private void OnHealth(PlayerHealthChanged value)
        {
            HealthLabel = $"{StringTable.Get("hud.health")} {value.Current}/{value.Maximum}";
            if (healthText != null) healthText.text = HealthLabel;
        }

        private void OnPrompt(InteractionPromptChanged value)
        {
            promptRequested = value.Visible;

            // 표시 형태: [현재 바인딩된 키] 문장
            // 키 이름은 이벤트가 실어다 준 실제 바인딩 값이라, 키를 재설정하면 표시도 따라 바뀐다.
            PromptLabel = value.Visible ? $"[{value.ActionName}] {StringTable.Get(value.StringTableKey)}" : string.Empty;
            if (promptText != null) promptText.text = PromptLabel;
            ApplyPromptVisibility();
        }

        private void OnChase(ChaseStateChanged value)
        {
            ChaseVisible = value.Active;
            if (chaseText != null) chaseText.text = StringTable.Get("hud.chase");
            if (chaseRoot != null) chaseRoot.SetActive(value.Active);

            // spec-013: 추격 중에는 조사 프롬프트가 아니라 생존 경고(chaseRoot)만 보여야 한다.
            ApplyPromptVisibility();
        }

        /// <summary>조사 프롬프트 표시 여부를 다시 계산한다. 추격 중에는 요청 값과 무관하게 숨긴다.</summary>
        private void ApplyPromptVisibility()
        {
            PromptVisible = promptRequested && !ChaseVisible;
            if (promptRoot != null) promptRoot.SetActive(PromptVisible);
        }

        // 검토 메모(미구현, spec-013):
        // 자막 크기 3단계(AssistSettings.SubtitleSize)가 실제 Text 크기에 반영되지 않는다.
        // 접근성 옵션 화면을 만들 때 붙여야 한다.
    }
}
