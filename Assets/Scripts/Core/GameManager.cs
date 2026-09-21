using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using GigaGrub.AI;
using GigaGrub.Food;
using GigaGrub.Player;
using GigaGrub.Systems;
using GigaGrub.UI;

namespace GigaGrub.Core
{
    public enum GameState
    {
        Playing,
        GameOver,
        Paused
    }

    [Serializable]
    public struct GameStats
    {
        public int FinalScore;
        public int FinalLength;
        public float SurvivalTime;
        public int AICreaturesDefeated;
        public int FoodCollected;

        public string FormattedSurvivalTime
        {
            get
            {
                int minutes = Mathf.FloorToInt(SurvivalTime / 60f);
                int seconds = Mathf.FloorToInt(SurvivalTime % 60f);
                return $"{minutes:00}:{seconds:00}";
            }
        }
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Scene References")]
        [SerializeField] private PlayerBody playerBody;
        [SerializeField] private AISpawner aiSpawner;
        [SerializeField] private FoodSpawner foodSpawner;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private RankingManager rankingManager;
        [SerializeField] private CameraFollow cameraFollow;
        [SerializeField] private GameOverUI gameOverUI;
        [SerializeField] private PauseMenuUI pauseMenuUI;
        [SerializeField] private GameplayHUD gameplayHUD;
        [SerializeField] private ScoreUI scoreUI;

        [SerializeField] private SaveManager saveManager;

        [Header("Initial Configuration")]
        [SerializeField] private Vector2 playerSpawnPoint = Vector2.zero;
        [SerializeField] private int initialAICount = 10;
        [SerializeField] private int initialFoodCount = 120;

        private GameState currentState = GameState.Playing;
        private float survivalTimer = 0f;
        private int foodCollected = 0;
        private int aiCreaturesDefeated = 0;
        private GameStats lastStats;

        public GameState CurrentState => currentState;
        public float SurvivalTimer => survivalTimer;
        public int FoodCollected => foodCollected;
        public int AICreaturesDefeated => aiCreaturesDefeated;
        public GameStats LastStats => lastStats;
        public bool IsPlaying => currentState == GameState.Playing;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsGameOver => currentState == GameState.GameOver;

        public event Action<GameState> OnGameStateChanged;
        public event Action<GameStats> OnGameOver;
        public event Action OnGameRestarted;

        public static void SetInstanceForTest(GameManager manager)
        {
            Instance = manager;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            ResolveReferences();
        }

        private void Start()
        {
            ResolveReferences();
            StartNewGame();
        }

        private void Update()
        {
            if (currentState == GameState.Playing)
            {
                survivalTimer += Time.deltaTime;
            }
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void ResolveReferences()
        {
            if (playerBody == null)
            {
                playerBody = FindAnyObjectByType<PlayerBody>();
            }

            if (aiSpawner == null)
            {
                aiSpawner = FindAnyObjectByType<AISpawner>();
            }

            if (foodSpawner == null)
            {
                foodSpawner = FindAnyObjectByType<FoodSpawner>();
            }

            if (scoreManager == null)
            {
                scoreManager = FindAnyObjectByType<ScoreManager>();
            }

            if (rankingManager == null)
            {
                rankingManager = FindAnyObjectByType<RankingManager>();
            }

            if (saveManager == null)
            {
                saveManager = FindAnyObjectByType<SaveManager>();
            }

            if (cameraFollow == null)
            {
                cameraFollow = FindAnyObjectByType<CameraFollow>();
            }

            if (gameOverUI == null)
            {
                gameOverUI = FindAnyObjectByType<GameOverUI>(FindObjectsInactive.Include);
            }

            if (pauseMenuUI == null)
            {
                pauseMenuUI = FindAnyObjectByType<PauseMenuUI>(FindObjectsInactive.Include);
            }

            if (gameplayHUD == null)
            {
                gameplayHUD = FindAnyObjectByType<GameplayHUD>(FindObjectsInactive.Include);
            }

            if (scoreUI == null)
            {
                scoreUI = FindAnyObjectByType<ScoreUI>();
            }
        }

        public void StartNewGame()
        {
            Time.timeScale = 1f;
            currentState = GameState.Playing;
            survivalTimer = 0f;
            foodCollected = 0;
            aiCreaturesDefeated = 0;

            if (gameOverUI != null)
            {
                gameOverUI.Hide();
            }

            if (pauseMenuUI != null)
            {
                pauseMenuUI.Hide();
            }

            OnGameStateChanged?.Invoke(currentState);
        }

        public void PauseGame()
        {
            if (currentState != GameState.Playing) return;

            currentState = GameState.Paused;
            Time.timeScale = 0f;

            if (pauseMenuUI != null)
            {
                pauseMenuUI.Show();
            }

            OnGameStateChanged?.Invoke(currentState);
        }

        public void ResumeGame()
        {
            if (currentState != GameState.Paused) return;

            currentState = GameState.Playing;
            Time.timeScale = 1f;

            if (pauseMenuUI != null)
            {
                pauseMenuUI.Hide();
            }

            OnGameStateChanged?.Invoke(currentState);
        }

        public void RecordFoodCollected()
        {
            if (currentState == GameState.Playing)
            {
                foodCollected++;
            }
        }

        public void RecordAIDefeated()
        {
            if (currentState == GameState.Playing)
            {
                aiCreaturesDefeated++;
            }
        }

        public void HandlePlayerDeath(DeathReason reason)
        {
            if (currentState == GameState.GameOver) return;

            currentState = GameState.GameOver;

            int finalScore = scoreManager != null ? scoreManager.CurrentScore : (playerBody != null ? playerBody.CurrentScore : 0);
            int finalLength = playerBody != null ? playerBody.CurrentLength : 10;

            lastStats = new GameStats
            {
                FinalScore = finalScore,
                FinalLength = finalLength,
                SurvivalTime = survivalTimer,
                AICreaturesDefeated = aiCreaturesDefeated,
                FoodCollected = foodCollected
            };

            // Persist session stats to SaveManager
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.RecordGameSession(finalScore, finalLength, foodCollected, aiCreaturesDefeated);
            }

            if (pauseMenuUI != null)
            {
                pauseMenuUI.Hide();
            }

            if (gameOverUI != null)
            {
                gameOverUI.Show(lastStats);
            }

            OnGameStateChanged?.Invoke(currentState);
            OnGameOver?.Invoke(lastStats);
        }

        public void RestartGame()
        {
            Time.timeScale = 1f;

            // 1. Reset Stats and Timers
            survivalTimer = 0f;
            foodCollected = 0;
            aiCreaturesDefeated = 0;
            currentState = GameState.Playing;

            // 2. Reset Score
            if (scoreManager != null)
            {
                scoreManager.ResetScore();
            }

            // 3. Reset Player Creature
            if (playerBody != null)
            {
                playerBody.ResetCreature(playerSpawnPoint);
            }

            // 4. Reset Camera
            if (cameraFollow != null && playerBody != null)
            {
                cameraFollow.SnapToTarget();
            }

            // 5. Reset AI Creatures
            if (aiSpawner != null)
            {
                aiSpawner.ClearAllAICreatures();
                aiSpawner.SpawnAICreatures(initialAICount);
            }

            // 6. Reset Food State
            if (foodSpawner != null)
            {
                foodSpawner.ClearAllActiveFood();
                foodSpawner.SpawnInitialPopulation(initialFoodCount);
            }

            // 7. Hide Overlays & Refresh HUDs
            if (gameOverUI != null)
            {
                gameOverUI.Hide();
            }

            if (pauseMenuUI != null)
            {
                pauseMenuUI.Hide();
            }

            if (gameplayHUD != null && playerBody != null)
            {
                gameplayHUD.BindPlayer(playerBody);
            }

            if (scoreUI != null && playerBody != null)
            {
                scoreUI.BindPlayer(playerBody);
            }

            if (rankingManager != null)
            {
                rankingManager.ResetState();
            }

            // 8. Fire lifecycle events
            OnGameStateChanged?.Invoke(currentState);
            OnGameRestarted?.Invoke();
        }

        public void ReturnToMainMenu()
        {
            Time.timeScale = 1f;

            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene("MainMenu");
            }
            else
            {
                // Fallback direct load
                try
                {
                    SceneManager.LoadScene("MainMenu");
                }
                catch
                {
                    RestartGame();
                }
            }
        }
    }
}
