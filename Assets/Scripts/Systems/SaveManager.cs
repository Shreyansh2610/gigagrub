using System;
using System.IO;
using UnityEngine;
using GigaGrub.Data;

namespace GigaGrub.Systems
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        private const string DefaultSaveFileName = "gigagrub_save.json";

        [Header("Settings")]
        [SerializeField] private bool autoSaveOnPause = true;
        [SerializeField] private bool autoSaveOnQuit = true;

        private PlayerStatistics statistics;
        private string customSavePath = null;
        private bool isInitialized = false;

        public PlayerStatistics Statistics
        {
            get
            {
                if (statistics == null)
                {
                    statistics = new PlayerStatistics();
                }
                return statistics;
            }
        }

        public string SaveFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(customSavePath))
                {
                    return customSavePath;
                }
                return Path.Combine(Application.persistentDataPath, DefaultSaveFileName);
            }
        }

        public bool IsInitialized => isInitialized;

        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;

        public static void SetInstanceForTest(SaveManager manager)
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
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && autoSaveOnPause && isInitialized)
            {
                Save();
            }
        }

        private void OnApplicationQuit()
        {
            if (autoSaveOnQuit && isInitialized)
            {
                Save();
            }
        }

        public void SetCustomSavePath(string path)
        {
            customSavePath = path;
        }

        public void Initialize()
        {
            if (statistics == null)
            {
                statistics = new PlayerStatistics();
            }

            Load();
            isInitialized = true;
        }

        public bool Save()
        {
            try
            {
                SaveData snapshot = Statistics.GetDataSnapshot();
                string json = JsonUtility.ToJson(snapshot, true);

                string filePath = SaveFilePath;
                string dir = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                // Atomic write using temporary file swap to eliminate corrupted writes during unexpected shutdown
                string tempFilePath = filePath + ".tmp";
                File.WriteAllText(tempFilePath, json);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(tempFilePath, filePath);

                OnSaveCompleted?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save game data: {ex.Message}");
                return false;
            }
        }

        public bool Load()
        {
            string filePath = SaveFilePath;

            // 1. Handle First Launch (Missing Save File)
            if (!File.Exists(filePath))
            {
                Debug.Log("[SaveManager] No existing save file found. Initializing with default save data (First Launch).");
                Statistics.SetData(SaveData.CreateDefault());
                Save();
                OnLoadCompleted?.Invoke();
                return true;
            }

            // 2. Read Existing Save File
            try
            {
                string json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning("[SaveManager] Save file is empty. Recovering with default save data.");
                    RecoverCorruptedSave();
                    return true;
                }

                SaveData loadedData = JsonUtility.FromJson<SaveData>(json);
                if (loadedData == null)
                {
                    Debug.LogWarning("[SaveManager] Deserialization returned null. Recovering with default save data.");
                    RecoverCorruptedSave();
                    return true;
                }

                // 3. Validate and Sanitize (Negative values and versioning)
                bool wasModified = loadedData.ValidateAndSanitize();
                Statistics.SetData(loadedData);

                if (wasModified)
                {
                    Debug.LogWarning("[SaveManager] Save data contained invalid or negative values. Sanitized and saved clean copy.");
                    Save();
                }

                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                // 4. Handle Corrupted Save Data gracefully without crashing
                Debug.LogWarning($"[SaveManager] Failed to parse save file ({ex.Message}). Recovering with clean default data.");
                RecoverCorruptedSave();
                return false;
            }
        }

        private void RecoverCorruptedSave()
        {
            SaveData fallback = SaveData.CreateDefault();
            Statistics.SetData(fallback);
            Save();
            OnLoadCompleted?.Invoke();
        }

        public void RecordGameSession(int score, int length, int foodCollected, int aiDefeated)
        {
            Statistics.RecordGameSession(score, length, foodCollected, aiDefeated);
            Save();
        }

        public void SetSettings(float musicVol, float sfxVol, bool vibration)
        {
            Statistics.SetSettings(musicVol, sfxVol, vibration);
            Save();
        }

        public void ResetSaveData()
        {
            Statistics.ResetStatistics();
            Save();
        }

        public bool DeleteSaveFile()
        {
            try
            {
                string filePath = SaveFilePath;
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                string tempFilePath = filePath + ".tmp";
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }

                Statistics.ResetStatistics();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to delete save file: {ex.Message}");
                return false;
            }
        }
    }
}
