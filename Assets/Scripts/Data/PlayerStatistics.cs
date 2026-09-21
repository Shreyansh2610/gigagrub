using System;
using UnityEngine;

namespace GigaGrub.Data
{
    public class PlayerStatistics
    {
        private SaveData data;

        public int BestScore => data != null ? data.BestScore : 0;
        public int BestLength => data != null ? data.BestLength : 10;
        public int TotalGamesPlayed => data != null ? data.TotalGamesPlayed : 0;
        public int TotalFoodCollected => data != null ? data.TotalFoodCollected : 0;
        public int TotalAIDefeated => data != null ? data.TotalAIDefeated : 0;
        public float MusicVolume => data != null ? data.MusicVolume : 0.8f;
        public float SFXVolume => data != null ? data.SFXVolume : 0.8f;
        public bool VibrationEnabled => data != null ? data.VibrationEnabled : true;
        public int Version => data != null ? data.Version : SaveData.CurrentSaveVersion;
        public long LastUpdatedTimestamp => data != null ? data.Timestamp : 0;

        public event Action<SaveData> OnStatisticsChanged;
        public event Action<float, float, bool> OnSettingsChanged;

        public PlayerStatistics(SaveData initialData = null)
        {
            SetData(initialData ?? SaveData.CreateDefault());
        }

        public void SetData(SaveData newData)
        {
            data = newData ?? SaveData.CreateDefault();
            data.ValidateAndSanitize();
            OnStatisticsChanged?.Invoke(data);
            OnSettingsChanged?.Invoke(data.MusicVolume, data.SFXVolume, data.VibrationEnabled);
        }

        public SaveData GetDataSnapshot()
        {
            if (data == null)
            {
                data = SaveData.CreateDefault();
            }
            data.ValidateAndSanitize();
            return data;
        }

        public void RecordGameSession(int score, int length, int foodCollected, int aiDefeated)
        {
            if (data == null)
            {
                data = SaveData.CreateDefault();
            }

            // Sanitize input arguments (no negative values allowed)
            int cleanScore = Mathf.Max(0, score);
            int cleanLength = Mathf.Max(0, length);
            int cleanFood = Mathf.Max(0, foodCollected);
            int cleanAI = Mathf.Max(0, aiDefeated);

            if (cleanScore > data.BestScore)
            {
                data.BestScore = cleanScore;
            }

            if (cleanLength > data.BestLength)
            {
                data.BestLength = cleanLength;
            }

            data.TotalGamesPlayed += 1;
            data.TotalFoodCollected += cleanFood;
            data.TotalAIDefeated += cleanAI;
            data.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            data.ValidateAndSanitize();
            OnStatisticsChanged?.Invoke(data);
        }

        public void SetSettings(float musicVol, float sfxVol, bool vibration)
        {
            if (data == null)
            {
                data = SaveData.CreateDefault();
            }

            data.MusicVolume = Mathf.Clamp01(musicVol);
            data.SFXVolume = Mathf.Clamp01(sfxVol);
            data.VibrationEnabled = vibration;
            data.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            data.ValidateAndSanitize();
            OnSettingsChanged?.Invoke(data.MusicVolume, data.SFXVolume, data.VibrationEnabled);
            OnStatisticsChanged?.Invoke(data);
        }

        public void ResetStatistics()
        {
            float music = MusicVolume;
            float sfx = SFXVolume;
            bool vib = VibrationEnabled;

            data = SaveData.CreateDefault();
            data.MusicVolume = music;
            data.SFXVolume = sfx;
            data.VibrationEnabled = vib;

            OnStatisticsChanged?.Invoke(data);
        }
    }
}
