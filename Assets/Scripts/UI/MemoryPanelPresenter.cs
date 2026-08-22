using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.UI;

namespace Daeume.UI
{
    /// <summary>
    /// 회상 자막 패널을 그린다. (spec-005, spec-013)
    ///
    /// MemoryPresentationChanged 이벤트만 구독하며, 회상을 진행시키는 일에는 관여하지 않는다
    /// (진행·건너뛰기 입력은 Daeume.Memory의 MemoryPlayback이 소유).
    /// 표시와 진행을 나눠 두면 UI를 바꿔도 회상 흐름이 깨지지 않는다.
    /// </summary>
    public sealed class MemoryPanelPresenter : MonoBehaviour
    {
        [SerializeField] private GameObject panel;   // 자막 패널 전체(보임/숨김 전환용)
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;

        public string Title { get; private set; } = string.Empty;
        public string Body { get; private set; } = string.Empty;

        // spec-013 자막 크기 3단계. 씬에 디자인된 원래 크기를 기준(1단계)으로 배율만 곱한다.
        private bool baseFontSizesCaptured;
        private int titleBaseSize, bodyBaseSize;

        // 생성 순서 문제를 피하기 위해 OnEnable과 Start 양쪽에서 연결을 시도한다.
        private void OnEnable() => Connect();
        private void Start() => Connect();
        private void OnDisable() => GameManager.Instance?.Events.Unsubscribe<MemoryPresentationChanged>(Present);

        public void Bind(GameObject root, Text title, Text body) { panel = root; titleText = title; bodyText = body; }

        private void Connect()
        {
            if (GameManager.Instance == null) return;
            GameManager.Instance.Events.Unsubscribe<MemoryPresentationChanged>(Present);
            GameManager.Instance.Events.Subscribe<MemoryPresentationChanged>(Present);
            ApplySubtitleSize();
        }

        private void ApplySubtitleSize()
        {
            if (!baseFontSizesCaptured)
            {
                if (titleText != null) titleBaseSize = titleText.fontSize;
                if (bodyText != null) bodyBaseSize = bodyText.fontSize;
                baseFontSizesCaptured = true;
            }

            var tier = FindAnyObjectByType<SceneFlowController>()?.CurrentData?.AssistSettings?.SubtitleSize ?? 1;
            var scale = SubtitleScale.Resolve(tier);
            if (titleText != null) titleText.fontSize = Mathf.RoundToInt(titleBaseSize * scale);
            if (bodyText != null) bodyText.fontSize = Mathf.RoundToInt(bodyBaseSize * scale);
        }

        private void Present(MemoryPresentationChanged value)
        {
            // 이벤트는 문장이 아니라 "키"를 실어 온다. 여기서 문자열 테이블을 통해 실제 문장으로 바꾼다.
            Title = StringTable.Get(value.TitleKey);
            Body = StringTable.Get(value.LineKey);

            if (titleText != null) titleText.text = Title;
            if (bodyText != null) bodyText.text = Body;

            // 회상이 끝나면 Visible=false가 담긴 이벤트가 오고, 그때 패널이 닫힌다.
            // 닫는 시점 판단을 UI가 스스로 하지 않는다는 점이 중요하다 — 판단은 회상 쪽에만 있다.
            if (panel != null) panel.SetActive(value.Visible);
        }
    }
}
