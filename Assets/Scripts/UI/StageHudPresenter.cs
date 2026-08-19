using Daeume.ContaminationRuntime;
using Daeume.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Daeume.UI
{
    public sealed class StageHudPresenter : MonoBehaviour
    {
        [SerializeField] private Text healthText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text chaseText;
        [SerializeField] private GameObject promptRoot;
        [SerializeField] private GameObject chaseRoot;

        public string HealthLabel { get; private set; } = string.Empty;
        public string PromptLabel { get; private set; } = string.Empty;
        public bool PromptVisible { get; private set; }
        public bool ChaseVisible { get; private set; }

        private void OnEnable() => Connect();
        private void Start() => Connect();
        private void OnDisable() => Disconnect();

        public void Bind(Text health, Text prompt, Text chase, GameObject promptContainer, GameObject chaseContainer)
        { healthText = health; promptText = prompt; chaseText = chase; promptRoot = promptContainer; chaseRoot = chaseContainer; }

        private void Connect()
        {
            if (GameManager.Instance == null) return;
            Disconnect();
            GameManager.Instance.Events.Subscribe<PlayerHealthChanged>(OnHealth);
            GameManager.Instance.Events.Subscribe<InteractionPromptChanged>(OnPrompt);
            GameManager.Instance.Events.Subscribe<ChaseStateChanged>(OnChase);
        }

        private void Disconnect()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Events.Unsubscribe<PlayerHealthChanged>(OnHealth);
            GameManager.Instance.Events.Unsubscribe<InteractionPromptChanged>(OnPrompt);
            GameManager.Instance.Events.Unsubscribe<ChaseStateChanged>(OnChase);
        }

        private void OnHealth(PlayerHealthChanged value)
        {
            HealthLabel = $"{StringTable.Get("hud.health")} {value.Current}/{value.Maximum}";
            if (healthText != null) healthText.text = HealthLabel;
        }

        private void OnPrompt(InteractionPromptChanged value)
        {
            PromptVisible = value.Visible;
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
    }
}
