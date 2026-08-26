using Daeume.Core;
using Daeume.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daeume.UI
{
    /// <summary>
    /// 타이틀 화면의 "새 게임 / 이어하기" 버튼을 씬 흐름에 연결한다. (spec-015)
    ///
    /// 이 스크립트는 씬을 직접 열지 않는다. SceneFlowController에 요청만 한다.
    /// 씬 조작 주체를 하나로 유지해야 저장 순서와 중복 전환 차단이 무너지지 않기 때문이다.
    /// </summary>
    public sealed class TitleMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text statusText;
        [SerializeField] private Text headingText;   // spec-013: 씬에 직접 박아 두지 않고 여기서 채운다
        [SerializeField] private Text subtitleText;
        [SerializeField] private Button settingsButton;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button contentWarningButton;
        [SerializeField] private GameObject contentWarningPanel;
        [SerializeField] private Text contentWarningBody;
        [SerializeField] private Button contentWarningCloseButton;

        private SceneFlowController flow;

        private void Awake()
        {
            // 버튼 클릭 시 호출할 함수를 등록한다. ?.는 "값이 없으면 건너뛴다"는 뜻이다.
            newGameButton?.onClick.AddListener(StartNewGame);
            continueButton?.onClick.AddListener(ContinueGame);
            settingsButton?.onClick.AddListener(OpenSettings);
            contentWarningButton?.onClick.AddListener(OpenContentWarning);
            contentWarningCloseButton?.onClick.AddListener(CloseContentWarning);


            // spec-013: 씬에 원고를 직접 박지 않는다. 버튼 글자·제목·부제·안내 문구를 전부 StringTable에서 채운다.
            if (headingText != null) headingText.text = StringTable.Get("title.heading");
            if (subtitleText != null) subtitleText.text = StringTable.Get("title.subtitle");
            if (statusText != null) statusText.text = StringTable.Get("title.hint");
            SetButtonLabel(newGameButton, "title.new_game");
            SetButtonLabel(continueButton, "title.continue");
            SetButtonLabel(settingsButton, "title.settings");
            SetButtonLabel(contentWarningButton, "title.content_warning");
            SetButtonLabel(contentWarningCloseButton, "title.close");
            if (contentWarningBody != null)
                contentWarningBody.text = StringTable.Get("title.content_warning.body");


            // 접근성 옵션 화면은 기본적으로 닫혀 있다.
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (contentWarningPanel != null) contentWarningPanel.SetActive(false);

        }

        public void OpenSettings()
        {
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void OpenContentWarning()
        {
            if (contentWarningPanel != null) contentWarningPanel.SetActive(true);
            if (EventSystem.current != null && contentWarningCloseButton != null)
                EventSystem.current.SetSelectedGameObject(contentWarningCloseButton.gameObject);
        }

        public void CloseContentWarning()
        {
            if (contentWarningPanel != null) contentWarningPanel.SetActive(false);
            if (EventSystem.current != null && contentWarningButton != null)
                EventSystem.current.SetSelectedGameObject(contentWarningButton.gameObject);
        }


        private static void SetButtonLabel(Button button, string key)
        {
            var label = button == null ? null : button.GetComponentInChildren<Text>();
            if (label != null) label.text = StringTable.Get(key);
        }

        private void Start()
        {
            ResolveFlow();
            AudioRuntime.PlayTitleMusic();

            // 키보드·패드로도 조작할 수 있게 첫 버튼을 선택 상태로 만든다.
            // 마우스 없이 진행할 수 있어야 한다는 접근성 기준선과 연결된 처리다.
            if (EventSystem.current != null && newGameButton != null)
                EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
        }

        private void OnDestroy()
        {
            // 등록한 리스너는 반드시 해제한다. 남겨 두면 파괴된 객체의 함수가 호출돼 오류가 난다.
            newGameButton?.onClick.RemoveListener(StartNewGame);
            continueButton?.onClick.RemoveListener(ContinueGame);
            settingsButton?.onClick.RemoveListener(OpenSettings);
            contentWarningButton?.onClick.RemoveListener(OpenContentWarning);
            contentWarningCloseButton?.onClick.RemoveListener(CloseContentWarning);

        }

        public void Bind(Button newGame, Button continueGame, Text status)
        {
            newGameButton = newGame;
            continueButton = continueGame;
            statusText = status;
        }

        public void StartNewGame() => StartTransition(true);
        public void ContinueGame() => StartTransition(false);

        private void StartTransition(bool newGame)
        {
            ResolveFlow();
            var started = flow != null && (newGame ? flow.StartNewGame() : flow.ContinueGame());
            if (started)
            {
                AudioRuntime.StopTitleMusic();
                // 전환이 시작되면 버튼을 잠근다. 연타로 전환이 두 번 시작되는 것을 눈에 보이게 막는다.
                SetInteractable(false);

                // 수정: 예전에는 이 문장이 코드에 직접 박혀 있었다(spec-013의 하드코딩 금지 위반).
                // 문자열 테이블 키로 바꿔, 원고 수정이나 번역이 코드 수정 없이 가능하도록 했다.
                if (statusText != null) statusText.text = StringTable.Get("title.loading");
            }
            else if (statusText != null) statusText.text = StringTable.Get("title.retry");
        }

        private void ResolveFlow()
        {
            // 흐름 컨트롤러는 Persistent 씬에 있어 Title 씬에서 미리 연결해 둘 수 없다. 실행 중에 찾는다.
            if (flow == null) flow = FindAnyObjectByType<SceneFlowController>();
        }

        private void SetInteractable(bool value)
        {
            if (newGameButton != null) newGameButton.interactable = value;
            if (continueButton != null) continueButton.interactable = value;
        }
    }
}
