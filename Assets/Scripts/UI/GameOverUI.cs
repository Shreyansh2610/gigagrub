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

        [Tooltip("Text component to display coins earned in the match")]
        [SerializeField] private Text coinsEarnedText;

        [Tooltip("Text component to display final rank placement")]
        [SerializeField] private Text rankPlacementText;

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

        private float targetAlpha = 0f;
        private Vector3 targetScale = Vector3.one;
        private bool isTransitioning = false;
        private float animatedScore = 0f;
        private int targetFinalScore = 0;

        private void Update()
        {
            if (isTransitioning)
            {
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 4f);
                }

                if (rootPanel != null)
                {
                    rootPanel.transform.localScale = Vector3.Lerp(rootPanel.transform.localScale, targetScale, Time.unscaledDeltaTime * 10f);
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

            if (finalScoreText != null && Mathf.Abs(animatedScore - targetFinalScore) > 0.5f)
            {
                animatedScore = Mathf.MoveTowards(animatedScore, targetFinalScore, Time.unscaledDeltaTime * Mathf.Max(100f, targetFinalScore * 3f));
                finalScoreText.text = $"{Mathf.RoundToInt(animatedScore):N0}";
            }
        }

        public void Show(GameStats stats, int coinsEarned = 0, int finalRank = 0)
        {
            targetFinalScore = stats.FinalScore;
            animatedScore = 0f;

            if (finalScoreText != null)
            {
                finalScoreText.text = "0";
            }

            if (bestScoreText != null)
            {
                int best = stats.FinalScore;
                if (Systems.SaveManager.Instance != null)
                {
                    best = Systems.SaveManager.Instance.Statistics.BestScore;
                }
                else if (Systems.ScoreManager.Instance != null)
                {
                    best = Systems.ScoreManager.Instance.HighScore;
                }
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

            if (coinsEarnedText != null)
            {
                coinsEarnedText.text = $"+{coinsEarned:N0} COINS";
            }

            if (rankPlacementText != null)
            {
                rankPlacementText.text = finalRank > 0 ? $"RANK #{finalRank}" : "";
            }

            if (rootPanel != null)
            {
                rootPanel.SetActive(true);
                rootPanel.transform.localScale = Vector3.one * 0.92f;
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
            targetScale = Vector3.one * 0.92f;
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
