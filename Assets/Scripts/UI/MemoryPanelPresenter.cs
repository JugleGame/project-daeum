using Daeume.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Daeume.UI
{
    public sealed class MemoryPanelPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        public string Title { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;

        private void OnEnable() => Connect();
        private void Start() => Connect();
        private void OnDisable() => GameManager.Instance?.Events.Unsubscribe<MemoryPresentationChanged>(Present);

        public void Bind(GameObject root, Text title, Text body) { panel = root; titleText = title; bodyText = body; }

        private void Connect()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Events.Unsubscribe<MemoryPresentationChanged>(Present);
            GameManager.Instance.Events.Subscribe<MemoryPresentationChanged>(Present);
        }

        private void Present(MemoryPresentationChanged value)
        {
            Title = StringTable.Get(value.TitleKey);
            Body = StringTable.Get(value.LineKey);
            if (titleText != null) titleText.text = Title;
            if (bodyText != null) bodyText.text = Body;
            if (panel != null) panel.SetActive(value.Visible);
        }
    }
}
