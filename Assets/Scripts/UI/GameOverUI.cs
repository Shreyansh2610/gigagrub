using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Core;

namespace GigaGrub.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("UI Text Displays")]
        [Tooltip("Text component to display the final score")]
        [SerializeField] private Text finalScoreText;

        [Tooltip("Text component to display the all-time best high score")]
        [SerializeField] private Text bestScoreText;

        [Tooltip("Text component to display the final creature length")]
        [SerializeField] private Text finalLengthText;

        [Tooltip("Text component to display total time survived")]
        [SerializeField] private Text survivalTimeText;

        [Tooltip("Text component to display number of AI defeated")]
        [SerializeField] private Text aiDefeatedText;

        [Tooltip("Text component to display total food items collected")]
        [SerializeField] private Text foodCollectedText;

        [Header("Buttons")]
        [Tooltip("Button to restart the match")]
        [SerializeField] private Button restartButton;

        [Tooltip("Button to return to the main menu")]
        [SerializeField] private Button mainMenuButton;

        [Header("Panel Container")]
        [Tooltip("Root panel GameObject or CanvasGroup for show/hide animation")]
        [SerializeField] private GameObject rootPanel;

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

            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        private void OnDestroy()
        {
            if (restartButton != null)
            {
                restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            }
        }

        public void Show(GameStats stats)
        {
            if (finalScoreText != null)
            {
                finalScoreText.text = $"{stats.FinalScore:N0}";
            }

            if (bestScoreText != null)
            {
                int best = Systems.ScoreManager.Instance != null ? Systems.ScoreManager.Instance.HighScore : stats.FinalScore;
                bestScoreText.text = $"{best:N0}";
            }

            if (finalLengthText != null)
            {
                finalLengthText.text = $"{stats.FinalLength}";
            }

            if (survivalTimeText != null)
            {
                survivalTimeText.text = stats.FormattedSurvivalTime;
            }

            if (aiDefeatedText != null)
            {
                aiDefeatedText.text = $"{stats.AICreaturesDefeated}";
            }

            if (foodCollectedText != null)
            {
                foodCollectedText.text = $"{stats.FoodCollected}";
            }

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
