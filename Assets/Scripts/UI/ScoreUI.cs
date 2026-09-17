using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Player;

namespace GigaGrub.UI
{
    public class ScoreUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("UI Text component to display player score")]
        [SerializeField] private Text scoreText;

        [Tooltip("Optional UI Text component to display creature length")]
        [SerializeField] private Text lengthText;

        [Tooltip("Target player body to observe")]
        [SerializeField] private PlayerBody playerBody;

        [Header("Animation Settings")]
        [Tooltip("Speed of score counter interpolation")]
        [SerializeField] private float countSpeed = 12f;

        [Tooltip("Punch scale magnitude when score increases")]
        [SerializeField] private float punchScale = 1.25f;

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

            if (playerBody != null)
            {
                playerBody.OnScoreChanged += HandleScoreChanged;
                targetScore = playerBody.CurrentScore;
                displayedScore = targetScore;
                UpdateScoreDisplay(targetScore);
                UpdateLengthDisplay(playerBody.CurrentLength);
            }
        }

        private void OnDestroy()
        {
            if (playerBody != null)
            {
                playerBody.OnScoreChanged -= HandleScoreChanged;
            }
        }

        private void Update()
        {
            // Smooth score number counter
            if (Mathf.Abs(displayedScore - targetScore) > 0.1f)
            {
                displayedScore = Mathf.MoveTowards(displayedScore, targetScore, Time.deltaTime * countSpeed * Mathf.Max(10f, Mathf.Abs(targetScore - displayedScore)));
                UpdateScoreDisplay(Mathf.RoundToInt(displayedScore));
            }

            // Smooth scale punch recovery
            if (scoreTransform != null && scoreTransform.localScale != originalScale)
            {
                scoreTransform.localScale = Vector3.Lerp(scoreTransform.localScale, originalScale, Time.deltaTime * punchReturnSpeed);
            }

            // Periodic length update
            if (lengthText != null && playerBody != null)
            {
                UpdateLengthDisplay(playerBody.CurrentLength);
            }
        }

        public void BindPlayer(PlayerBody body)
        {
            if (playerBody != null)
            {
                playerBody.OnScoreChanged -= HandleScoreChanged;
            }

            playerBody = body;
            if (playerBody != null)
            {
                playerBody.OnScoreChanged += HandleScoreChanged;
                targetScore = playerBody.CurrentScore;
                displayedScore = targetScore;
                UpdateScoreDisplay(targetScore);
                UpdateLengthDisplay(playerBody.CurrentLength);
            }
        }

        private void HandleScoreChanged(int newScore, int addedScore)
        {
            targetScore = newScore;

            if (addedScore > 0 && scoreTransform != null)
            {
                scoreTransform.localScale = originalScale * punchScale;
            }

            if (lengthText != null && playerBody != null)
            {
                UpdateLengthDisplay(playerBody.CurrentLength);
            }
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
