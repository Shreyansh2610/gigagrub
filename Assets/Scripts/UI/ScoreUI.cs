using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Player;
using GigaGrub.Systems;

namespace GigaGrub.UI
{
    public class ScoreUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("UI Text component to display player score")]
        [SerializeField] private Text scoreText;

        [Tooltip("UI Text component to display creature length")]
        [SerializeField] private Text lengthText;

        [Tooltip("Target player body to observe")]
        [SerializeField] private PlayerBody playerBody;

        [Tooltip("Target growth system to observe")]
        [SerializeField] private GrowthSystem growthSystem;

        [Header("Animation Settings")]
        [Tooltip("Speed of score counter interpolation")]
        [SerializeField] private float countSpeed = 24f;

        [Tooltip("Punch scale magnitude when score increases")]
        [SerializeField] private float punchScale = 1.2f;

        [Tooltip("Punch scale return speed")]
        [SerializeField] private float punchReturnSpeed = 8f;

        private float displayedScore = 0f;
        private int targetScore = 0;
        private Vector3 originalScale = Vector3.one;
        private Transform scoreTransform;

        private void Awake()
        {
            if (scoreText != null)
            {
                scoreTransform = scoreText.transform;
                originalScale = scoreTransform.localScale;
            }
        }

        private void Start()
        {
            if (playerBody == null)
            {
                playerBody = FindAnyObjectByType<PlayerBody>();
            }

            if (playerBody != null && growthSystem == null)
            {
                growthSystem = playerBody.GetComponent<GrowthSystem>();
            }

            SubscribeEvents();
            RefreshAllDisplays();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
        }

        public void Bind(PlayerBody body, GrowthSystem growth = null)
        {
            UnsubscribeEvents();

            playerBody = body;
            growthSystem = growth != null ? growth : (body != null ? body.GetComponent<GrowthSystem>() : null);

            SubscribeEvents();
            RefreshAllDisplays();
        }

        public void BindPlayer(PlayerBody body)
        {
            Bind(body, null);
        }

        private void SubscribeEvents()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            }
            else if (playerBody != null)
            {
                playerBody.OnScoreChanged += HandleScoreChanged;
            }

            if (growthSystem != null)
            {
                growthSystem.OnLengthChanged += HandleGrowthLengthChanged;
            }
        }

        private void UnsubscribeEvents()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
            }

            if (playerBody != null)
            {
                playerBody.OnScoreChanged -= HandleScoreChanged;
            }

            if (growthSystem != null)
            {
                growthSystem.OnLengthChanged -= HandleGrowthLengthChanged;
            }
        }

        private void RefreshAllDisplays()
        {
            int score = 0;
            if (ScoreManager.Instance != null)
            {
                score = ScoreManager.Instance.CurrentScore;
            }
            else if (playerBody != null)
            {
                score = playerBody.CurrentScore;
            }

            targetScore = score;
            displayedScore = score;
            UpdateScoreDisplay(targetScore);

            int length = playerBody != null ? playerBody.CurrentLength : 10;
            UpdateLengthDisplay(length);
        }

        private void Update()
        {
            // Smooth score number interpolation
            if (Mathf.Abs(displayedScore - targetScore) > 0.1f)
            {
                displayedScore = Mathf.MoveTowards(
                    displayedScore,
                    targetScore,
                    Time.deltaTime * countSpeed * Mathf.Max(10f, Mathf.Abs(targetScore - displayedScore))
                );
                UpdateScoreDisplay(Mathf.RoundToInt(displayedScore));
            }

            // Punch scale return
            if (scoreTransform != null && scoreTransform.localScale != originalScale)
            {
                scoreTransform.localScale = Vector3.Lerp(scoreTransform.localScale, originalScale, Time.deltaTime * punchReturnSpeed);
            }

            // Periodic fallback check for length
            if (lengthText != null && playerBody != null)
            {
                UpdateLengthDisplay(playerBody.CurrentLength);
            }
        }

        private void HandleScoreChanged(int newScore, int addedScore)
        {
            targetScore = newScore;

            // Immediate responsive update for direct visual feedback
            if (addedScore > 0 && scoreTransform != null)
            {
                scoreTransform.localScale = originalScale * punchScale;
            }

            UpdateScoreDisplay(newScore);

            if (playerBody != null)
            {
                UpdateLengthDisplay(playerBody.CurrentLength);
            }
        }

        private void HandleGrowthLengthChanged(int currentLength, int targetLength, int added)
        {
            UpdateLengthDisplay(currentLength);
        }

        private void UpdateScoreDisplay(int value)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE  {value:N0}";
            }
        }

        private void UpdateLengthDisplay(int length)
        {
            if (lengthText != null)
            {
                lengthText.text = $"LENGTH  {length}";
            }
        }
    }
}
