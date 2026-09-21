using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using GigaGrub.Systems;
using GigaGrub.Data;

namespace GigaGrub.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Main Menu Controls")]
        [Tooltip("Button to launch the game match")]
        [SerializeField] private Button playButton;

        [Tooltip("Button to open career statistics modal")]
        [SerializeField] private Button statisticsButton;

        [Tooltip("Button to open settings modal")]
        [SerializeField] private Button settingsButton;

        [Tooltip("Text displaying all-time best high score")]
        [SerializeField] private Text bestScoreText;

        [Header("Statistics Modal Dialog")]
        [SerializeField] private GameObject statisticsPanel;
        [SerializeField] private CanvasGroup statisticsCanvasGroup;
        [SerializeField] private Text statsBestScoreText;
        [SerializeField] private Text statsBestLengthText;
        [SerializeField] private Text statsGamesPlayedText;
        [SerializeField] private Text statsFoodCollectedText;
        [SerializeField] private Text statsAIDefeatedText;
        [SerializeField] private Button statsCloseButton;

        [Header("Settings Modal Dialog")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private CanvasGroup settingsCanvasGroup;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private Slider sfxVolumeSlider;
        [SerializeField] private Button vibrationToggleButton;
        [SerializeField] private Text vibrationToggleText;
        [SerializeField] private Image vibrationToggleBg;
        [SerializeField] private Button settingsCloseButton;

        [Header("Scene Destination")]
        [SerializeField] private string gameSceneName = "Game";

        private bool vibrationState = true;
        private bool isStatsOpen = false;
        private bool isSettingsOpen = false;

        public bool IsStatsOpen => isStatsOpen;
        public bool IsSettingsOpen => isSettingsOpen;

        private void Update()
        {
            // Android hardware back button / Escape key handling
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (isSettingsOpen)
                {
                    CloseSettings();
                }
                else if (isStatsOpen)
                {
                    CloseStatistics();
                }
            }
        }

        private void Awake()
        {
            if (playButton != null)
            {
                playButton.onClick.AddListener(OnPlayClicked);
            }

            if (statisticsButton != null)
            {
                statisticsButton.onClick.AddListener(OpenStatistics);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OpenSettings);
            }

            if (statsCloseButton != null)
            {
                statsCloseButton.onClick.AddListener(CloseStatistics);
            }

            if (settingsCloseButton != null)
            {
                settingsCloseButton.onClick.AddListener(CloseSettings);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            if (vibrationToggleButton != null)
            {
                vibrationToggleButton.onClick.AddListener(OnVibrationToggled);
            }
        }

        private void Start()
        {
            RefreshAllViews();
            HideModalImmediate(statisticsPanel, statisticsCanvasGroup);
            HideModalImmediate(settingsPanel, settingsCanvasGroup);
        }

        private void OnDestroy()
        {
            if (playButton != null) playButton.onClick.RemoveListener(OnPlayClicked);
            if (statisticsButton != null) statisticsButton.onClick.RemoveListener(OpenStatistics);
            if (settingsButton != null) settingsButton.onClick.RemoveListener(OpenSettings);
            if (statsCloseButton != null) statsCloseButton.onClick.RemoveListener(CloseStatistics);
            if (settingsCloseButton != null) settingsCloseButton.onClick.RemoveListener(CloseSettings);
            if (musicVolumeSlider != null) musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
            if (sfxVolumeSlider != null) sfxVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
            if (vibrationToggleButton != null) vibrationToggleButton.onClick.RemoveListener(OnVibrationToggled);
        }

        public void RefreshAllViews()
        {
            // 1. Refresh Best Score
            int bestScore = 0;
            if (SaveManager.Instance != null)
            {
                bestScore = SaveManager.Instance.Statistics.BestScore;
            }

            if (bestScoreText != null)
            {
                bestScoreText.text = $"BEST SCORE: {bestScore:N0}";
            }

            // 2. Refresh Settings
            float music = 0.8f;
            float sfx = 0.8f;
            vibrationState = true;

            if (SaveManager.Instance != null)
            {
                music = SaveManager.Instance.Statistics.MusicVolume;
                sfx = SaveManager.Instance.Statistics.SFXVolume;
                vibrationState = SaveManager.Instance.Statistics.VibrationEnabled;
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = music;
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.value = sfx;
            }

            UpdateVibrationUI();
        }

        public void OnPlayClicked()
        {
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene(gameSceneName);
            }
            else
            {
                SceneManager.LoadScene(gameSceneName);
            }
        }

        public void OpenStatistics()
        {
            isStatsOpen = true;
            RefreshStatisticsData();
            ShowModal(statisticsPanel, statisticsCanvasGroup);
        }

        public void CloseStatistics()
        {
            isStatsOpen = false;
            HideModal(statisticsPanel, statisticsCanvasGroup);
        }

        public void RefreshStatisticsData()
        {
            PlayerStatistics stats = SaveManager.Instance != null ? SaveManager.Instance.Statistics : null;

            int bestScore = stats != null ? stats.BestScore : 0;
            int bestLength = stats != null ? stats.BestLength : 10;
            int gamesPlayed = stats != null ? stats.TotalGamesPlayed : 0;
            int foodCollected = stats != null ? stats.TotalFoodCollected : 0;
            int aiDefeated = stats != null ? stats.TotalAIDefeated : 0;

            if (statsBestScoreText != null) statsBestScoreText.text = $"{bestScore:N0}";
            if (statsBestLengthText != null) statsBestLengthText.text = $"{bestLength}";
            if (statsGamesPlayedText != null) statsGamesPlayedText.text = $"{gamesPlayed:N0}";
            if (statsFoodCollectedText != null) statsFoodCollectedText.text = $"{foodCollected:N0}";
            if (statsAIDefeatedText != null) statsAIDefeatedText.text = $"{aiDefeated:N0}";
        }

        public void OpenSettings()
        {
            isSettingsOpen = true;
            RefreshAllViews();
            ShowModal(settingsPanel, settingsCanvasGroup);
        }

        public void CloseSettings()
        {
            isSettingsOpen = false;
            SaveSettings();
            HideModal(settingsPanel, settingsCanvasGroup);
        }

        private void OnMusicVolumeChanged(float val)
        {
            SaveSettings();
        }

        private void OnSFXVolumeChanged(float val)
        {
            SaveSettings();
        }

        private void OnVibrationToggled()
        {
            vibrationState = !vibrationState;
            UpdateVibrationUI();
            SaveSettings();

            // Light tactile feedback on toggle if enabled
            if (vibrationState)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                Handheld.Vibrate();
#endif
            }
        }

        private void UpdateVibrationUI()
        {
            if (vibrationToggleText != null)
            {
                vibrationToggleText.text = vibrationState ? "ON" : "OFF";
            }

            if (vibrationToggleBg != null)
            {
                vibrationToggleBg.color = vibrationState ? new Color(0.1f, 0.85f, 0.45f, 1f) : new Color(0.35f, 0.4f, 0.48f, 1f);
            }
        }

        private void SaveSettings()
        {
            if (SaveManager.Instance != null)
            {
                float music = musicVolumeSlider != null ? musicVolumeSlider.value : 0.8f;
                float sfx = sfxVolumeSlider != null ? sfxVolumeSlider.value : 0.8f;
                SaveManager.Instance.SetSettings(music, sfx, vibrationState);
            }
        }

        private void ShowModal(GameObject panel, CanvasGroup cg)
        {
            if (panel == null) return;
            panel.SetActive(true);
            panel.transform.localScale = Vector3.one * 0.94f;

            if (cg != null)
            {
                cg.alpha = 1f;
                cg.interactable = true;
                cg.blocksRaycasts = true;
            }

            panel.transform.localScale = Vector3.one;
        }

        private void HideModal(GameObject panel, CanvasGroup cg)
        {
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }

        private void HideModalImmediate(GameObject panel, CanvasGroup cg)
        {
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }

            if (panel != null)
            {
                panel.SetActive(false);
            }
        }
    }
}
