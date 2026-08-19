using Daeume.Flow;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Daeume.UI
{
    public sealed class TitleMenuController : MonoBehaviour
    {
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Text statusText;
        private SceneFlowController flow;

        private void Awake()
        {
            newGameButton?.onClick.AddListener(StartNewGame);
            continueButton?.onClick.AddListener(ContinueGame);
        }

        private void Start()
        {
            ResolveFlow();
            if (EventSystem.current != null && newGameButton != null)
                EventSystem.current.SetSelectedGameObject(newGameButton.gameObject);
        }

        private void OnDestroy()
        {
            newGameButton?.onClick.RemoveListener(StartNewGame);
            continueButton?.onClick.RemoveListener(ContinueGame);
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
                SetInteractable(false);
                if (statusText != null) statusText.text = "불러오는 중…";
            }
            else if (statusText != null) statusText.text = "잠시 후 다시 시도해 주세요.";
        }

        private void ResolveFlow()
        {
            if (flow == null) flow = FindAnyObjectByType<SceneFlowController>();
        }

        private void SetInteractable(bool value)
        {
            if (newGameButton != null) newGameButton.interactable = value;
            if (continueButton != null) continueButton.interactable = value;
        }
    }
}
