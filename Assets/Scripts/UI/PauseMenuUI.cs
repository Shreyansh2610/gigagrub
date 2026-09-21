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

        private float targetAlpha = 0f;
        private Vector3 targetScale = Vector3.one;
        private bool isTransitioning = false;

        private void Update()
        {
            if (isTransitioning)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 6f);
                }

                if (rootPanel != null)
                {
                    rootPanel.transform.localScale = Vector3.Lerp(rootPanel.transform.localScale, targetScale, Time.unscaledDeltaTime * 12f);
                }

                if (canvasGroup != null && Mathf.Approximately(canvasGroup.alpha, targetAlpha))
                {
                    if (targetAlpha <= 0.01f && rootPanel != null)
                    {
                        rootPanel.SetActive(false);
                    }
                    isTransitioning = false;
                }
            }
        }

        public void Show()
        {
            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
                rootPanel.transform.localScale = Vector3.one * 0.94f;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            targetAlpha = 1f;
            targetScale = Vector3.one;
            isTransitioning = true;
        }

        public void Hide()
        {
            targetAlpha = 0f;
            targetScale = Vector3.one * 0.94f;
            isTransitioning = true;

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
