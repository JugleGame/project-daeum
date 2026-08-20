using Daeume.ContaminationRuntime;
using Daeume.Core;
using UnityEngine;
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

        public string HealthLabel { get; private set; } = string.Empty;
        public string PromptLabel { get; private set; } = string.Empty;
        public bool PromptVisible { get; private set; }
        public bool ChaseVisible { get; private set; }

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
        }

        private void OnHealth(PlayerHealthChanged value)
        {
            HealthLabel = $"{StringTable.Get("hud.health")} {value.Current}/{value.Maximum}";
            if (healthText != null) healthText.text = HealthLabel;
        }

        private void OnPrompt(InteractionPromptChanged value)
        {
            PromptVisible = value.Visible;

            // 표시 형태: [현재 바인딩된 키] 문장
            // 키 이름은 이벤트가 실어다 준 실제 바인딩 값이라, 키를 재설정하면 표시도 따라 바뀐다.
            PromptLabel = value.Visible ? $"[{value.ActionName}] {StringTable.Get(value.StringTableKey)}" : string.Empty;
            if (promptText != null) promptText.text = PromptLabel;
            if (promptRoot != null) promptRoot.SetActive(value.Visible);
        }

        private void OnChase(ChaseStateChanged value)
        {
            ChaseVisible = value.Active;
            if (chaseText != null) chaseText.text = StringTable.Get("hud.chase");
            if (chaseRoot != null) chaseRoot.SetActive(value.Active);
        }

        // 검토 메모(미구현, spec-013):
        // 1) 추격 중에는 일반 조사 프롬프트를 숨기고 생존 경고만 남겨야 한다. 지금은 프롬프트가 그대로 뜬다.
        //    (다만 추격 상태에서 조사 대상이 거의 없어 실사용상 노출은 드물다.)
        // 2) 자막 크기 3단계(AssistSettings.SubtitleSize)가 실제 Text 크기에 반영되지 않는다.
        //    접근성 옵션 화면을 만들 때 이 두 가지를 함께 붙여야 한다.
    }
}
