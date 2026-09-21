using System;
using UnityEngine;

namespace GigaGrub.Systems
{
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score Settings")]
        [Tooltip("Key used for storing high score in PlayerPrefs")]
        [SerializeField] private string highScoreKey = "GigaGrub_HighScore";

        [Tooltip("Current score multiplier")]
        [SerializeField] private float scoreMultiplier = 1f;

        private int currentScore = 0;
        private int highScore = 0;

        public int CurrentScore => currentScore;
        public int HighScore => highScore;
        public float ScoreMultiplier => scoreMultiplier;

        public event Action<int, int> OnScoreChanged;
        public event Action<int> OnHighScoreChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LoadHighScore();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static void SetInstanceForTest(ScoreManager manager)
        {
            Instance = manager;
        }

        public void AddScore(int amount)
        {
            if (amount <= 0) return;

            int calculatedPoints = Mathf.RoundToInt(amount * scoreMultiplier);
            currentScore += calculatedPoints;

            OnScoreChanged?.Invoke(currentScore, calculatedPoints);

            if (currentScore > highScore)
            {
                highScore = currentScore;
                SaveHighScore();
                OnHighScoreChanged?.Invoke(highScore);
            }
        }

        public void SetMultiplier(float multiplier)
        {
            scoreMultiplier = Mathf.Max(1f, multiplier);
        }

        public void ResetScore()
        {
            currentScore = 0;
            OnScoreChanged?.Invoke(currentScore, 0);
        }

        public void SetScore(int score)
        {
            currentScore = Mathf.Max(0, score);
            OnScoreChanged?.Invoke(currentScore, 0);

            if (currentScore > highScore)
            {
                highScore = currentScore;
                SaveHighScore();
                OnHighScoreChanged?.Invoke(highScore);
            }
        }

        private void LoadHighScore()
        {
            if (SaveManager.Instance != null)
            {
                highScore = SaveManager.Instance.Statistics.BestScore;
            }
            else
            {
                highScore = PlayerPrefs.GetInt(highScoreKey, 0);
            }
        }

        private void SaveHighScore()
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Statistics.RecordGameSession(highScore, 0, 0, 0);
            }
            PlayerPrefs.SetInt(highScoreKey, highScore);
            PlayerPrefs.Save();
        }
    }
}
