using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Core;

namespace GigaGrub.UI
{
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [Tooltip("Button to resume gameplay")]
        [SerializeField] private Button resumeButton;

        [Tooltip("Button to restart the match")]
        [SerializeField] private Button restartButton;

        [Tooltip("Button to return to the main menu")]
        [SerializeField] private Button mainMenuButton;

        [Header("Panel Container")]
        [Tooltip("Root panel GameObject for pause menu")]
        [SerializeField] private GameObject rootPanel;

        [Tooltip("CanvasGroup for smooth alpha transitions and raycast blocking")]
        [SerializeField] private CanvasGroup canvasGroup;

        private void Awake()
        {
            if (rootPanel == null)
            {
                rootPanel = gameObject;
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(OnResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged += HandleGameStateChanged;
            }
            Hide();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
            }

            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveListener(OnResumeClicked);
            }

            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        private void HandleGameStateChanged(GameState newState)
        {
            if (newState == GameState.Paused)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        public void Show()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            if (rootPanel != null)
            {
                rootPanel.SetActive(false);
            }
        }

        public void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
        }

        public void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        public void OnMainMenuClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ReturnToMainMenu();
            }
        }
    }
}
