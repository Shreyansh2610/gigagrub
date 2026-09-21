using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Core;
using GigaGrub.Player;
using GigaGrub.Systems;

namespace GigaGrub.UI
{
    public class GameplayHUD : MonoBehaviour
    {
        [Header("Top HUD Displays")]
        [Tooltip("UI Text displaying the current score")]
        [SerializeField] private Text scoreText;

        [Tooltip("UI Text displaying the current creature length")]
        [SerializeField] private Text lengthText;

        [Tooltip("UI Text displaying the current ranking position")]
        [SerializeField] private Text rankText;

        [Tooltip("UI Text displaying the elapsed survival time")]
        [SerializeField] private Text timeText;

        [Header("Control Buttons")]
        [Tooltip("Button to pause the game and open the pause menu")]
        [SerializeField] private Button pauseButton;

        [Tooltip("Interactive touch button placeholder for speed boost")]
        [SerializeField] private Button boostButton;

        [Header("Animation Settings")]
        [Tooltip("Scale punch magnitude when score increases")]
        [SerializeField] private float punchScale = 1.18f;

        [Tooltip("Punch scale return interpolation speed")]
        [SerializeField] private float punchReturnSpeed = 9f;

        private PlayerBody targetPlayer;
        private GrowthSystem targetGrowth;
        private Transform scoreTransform;
        private Vector3 originalScoreScale = Vector3.one;
        private Coroutine timerCoroutine;
        private int lastSeconds = -1;

        private void Awake()
        {
            if (scoreText != null)
            {
                scoreTransform = scoreText.transform;
                originalScoreScale = scoreTransform.localScale;
            }

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(OnPauseClicked);
            }

            if (boostButton != null)
            {
                boostButton.onClick.AddListener(OnBoostClicked);
            }
        }

        private void Start()
        {
            if (targetPlayer == null)
            {
                targetPlayer = FindAnyObjectByType<PlayerBody>();
            }

            if (targetPlayer != null && targetGrowth == null)
            {
                targetGrowth = targetPlayer.GetComponent<GrowthSystem>();
            }

            SubscribeEvents();
            RefreshAllDisplays();
            StartTimerRoutine();
        }

        private void OnEnable()
        {
            StartTimerRoutine();
        }

        private void OnDisable()
        {
            StopTimerRoutine();
        }

        private void OnDestroy()
        {
            UnsubscribeEvents();
            StopTimerRoutine();

            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseClicked);
            }

            if (boostButton != null)
            {
                boostButton.onClick.RemoveListener(OnBoostClicked);
            }
        }

        public void BindPlayer(PlayerBody player, GrowthSystem growth = null)
        {
            UnsubscribeEvents();

            targetPlayer = player;
            targetGrowth = growth != null ? growth : (player != null ? player.GetComponent<GrowthSystem>() : null);

            SubscribeEvents();
            RefreshAllDisplays();
        }

        private void SubscribeEvents()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            }

            if (targetGrowth != null)
            {
                targetGrowth.OnLengthChanged += HandleLengthChanged;
            }

            if (RankingManager.Instance != null)
            {
                RankingManager.Instance.OnRankChanged += HandleRankChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted += HandleGameRestarted;
            }
        }

        private void UnsubscribeEvents()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
            }

            if (targetGrowth != null)
            {
                targetGrowth.OnLengthChanged -= HandleLengthChanged;
            }

            if (RankingManager.Instance != null)
            {
                RankingManager.Instance.OnRankChanged -= HandleRankChanged;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameRestarted -= HandleGameRestarted;
            }
        }

        public void RefreshAllDisplays()
        {
            int score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : (targetPlayer != null ? targetPlayer.CurrentScore : 0);
            UpdateScoreDisplay(score);

            int length = targetPlayer != null ? targetPlayer.CurrentLength : 10;
            UpdateLengthDisplay(length);

            int rank = RankingManager.Instance != null ? RankingManager.Instance.CurrentRank : 1;
            int total = RankingManager.Instance != null ? RankingManager.Instance.TotalCreatures : 1;
            UpdateRankDisplay(rank, total);

            float time = GameManager.Instance != null ? GameManager.Instance.SurvivalTimer : 0f;
            UpdateTimeDisplay(time);
        }

        private void Update()
        {
            // Return score transform scale after punch animation (only when animating)
            if (scoreTransform != null && scoreTransform.localScale != originalScoreScale)
            {
                scoreTransform.localScale = Vector3.Lerp(scoreTransform.localScale, originalScoreScale, Time.unscaledDeltaTime * punchReturnSpeed);
                if (Vector3.Distance(scoreTransform.localScale, originalScoreScale) < 0.005f)
                {
                    scoreTransform.localScale = originalScoreScale;
                }
            }
        }

        private void StartTimerRoutine()
        {
            StopTimerRoutine();
            if (gameObject.activeInHierarchy)
            {
                timerCoroutine = StartCoroutine(TimerUpdateRoutine());
            }
        }

        private void StopTimerRoutine()
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }

        private IEnumerator TimerUpdateRoutine()
        {
            // Updates time at 0.5s intervals to eliminate per-frame string allocations
            WaitForSecondsRealtime wait = new WaitForSecondsRealtime(0.5f);
            while (true)
            {
                if (GameManager.Instance != null && GameManager.Instance.IsPlaying)
                {
                    int totalSeconds = Mathf.FloorToInt(GameManager.Instance.SurvivalTimer);
                    if (totalSeconds != lastSeconds)
                    {
                        lastSeconds = totalSeconds;
                        UpdateTimeDisplay(GameManager.Instance.SurvivalTimer);
                    }
                }
                yield return wait;
            }
        }

        private void HandleScoreChanged(int newScore, int addedScore)
        {
            UpdateScoreDisplay(newScore);

            if (addedScore > 0 && scoreTransform != null)
            {
                scoreTransform.localScale = originalScoreScale * punchScale;
            }
        }

        private void HandleLengthChanged(int currentLength, int targetLength, int added)
        {
            UpdateLengthDisplay(currentLength);
        }

        private void HandleRankChanged(int rank, int total)
        {
            UpdateRankDisplay(rank, total);
        }

        private void HandleGameRestarted()
        {
            lastSeconds = -1;
            RefreshAllDisplays();
        }

        private void UpdateScoreDisplay(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE  {score:N0}";
            }
        }

        private void UpdateRankDisplay(int rank, int total)
        {
            if (rankText != null)
            {
                rankText.text = $"RANK  #{rank} / {total}";
            }
        }

        private void UpdateLengthDisplay(int length)
        {
            if (lengthText != null)
            {
                lengthText.text = $"LENGTH  {length}";
            }
        }

        private void UpdateTimeDisplay(float seconds)
        {
            if (timeText != null)
            {
                int mins = Mathf.FloorToInt(seconds / 60f);
                int secs = Mathf.FloorToInt(seconds % 60f);
                timeText.text = $"TIME  {mins:00}:{secs:00}";
            }
        }

        private void OnPauseClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.PauseGame();
            }
        }

        private void OnBoostClicked()
        {
            // Boost button placeholder interaction (ready for boost speed sprint integration)
            Debug.Log("[GigaGrub] Boost button clicked!");
        }
    }
}
