using System;
using UnityEngine;

namespace GigaGrub.Data
{
    [Serializable]
    public class SaveData
    {
        public const int CurrentSaveVersion = 1;

        [SerializeField] private int version = CurrentSaveVersion;
        [SerializeField] private long timestamp = 0;

        [Header("Career Statistics")]
        [SerializeField] private int bestScore = 0;
        [SerializeField] private int bestLength = 10;
        [SerializeField] private int totalGamesPlayed = 0;
        [SerializeField] private int totalFoodCollected = 0;
        [SerializeField] private int totalAIDefeated = 0;

        [Header("User Settings")]
        [SerializeField] private float musicVolume = 0.8f;
        [SerializeField] private float sfxVolume = 0.8f;
        [SerializeField] private bool vibrationEnabled = true;

        public int Version
        {
            get => version;
            set => version = Mathf.Max(1, value);
        }

        public long Timestamp
        {
            get => timestamp;
            set => timestamp = value;
        }

        public int BestScore
        {
            get => bestScore;
            set => bestScore = Mathf.Max(0, value);
        }

        public int BestLength
        {
            get => bestLength;
            set => bestLength = Mathf.Max(0, value);
        }

        public int TotalGamesPlayed
        {
            get => totalGamesPlayed;
            set => totalGamesPlayed = Mathf.Max(0, value);
        }

        public int TotalFoodCollected
        {
            get => totalFoodCollected;
            set => totalFoodCollected = Mathf.Max(0, value);
        }

        public int TotalAIDefeated
        {
            get => totalAIDefeated;
            set => totalAIDefeated = Mathf.Max(0, value);
        }

        public float MusicVolume
        {
            get => musicVolume;
            set => musicVolume = Mathf.Clamp01(value);
        }

        public float SFXVolume
        {
            get => sfxVolume;
            set => sfxVolume = Mathf.Clamp01(value);
        }

        public bool VibrationEnabled
        {
            get => vibrationEnabled;
            set => vibrationEnabled = value;
        }

        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                version = CurrentSaveVersion,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                bestScore = 0,
                bestLength = 10,
                totalGamesPlayed = 0,
                totalFoodCollected = 0,
                totalAIDefeated = 0,
                musicVolume = 0.8f,
                sfxVolume = 0.8f,
                vibrationEnabled = true
            };
        }

        public bool ValidateAndSanitize()
        {
            bool wasModified = false;

            if (version < 1)
            {
                version = CurrentSaveVersion;
                wasModified = true;
            }

            if (bestScore < 0)
            {
                bestScore = 0;
                wasModified = true;
            }

            if (bestLength < 0)
            {
                bestLength = 0;
                wasModified = true;
            }

            if (totalGamesPlayed < 0)
            {
                totalGamesPlayed = 0;
                wasModified = true;
            }

            if (totalFoodCollected < 0)
            {
                totalFoodCollected = 0;
                wasModified = true;
            }

            if (totalAIDefeated < 0)
            {
                totalAIDefeated = 0;
                wasModified = true;
            }

            if (musicVolume < 0f || musicVolume > 1f)
            {
                musicVolume = Mathf.Clamp01(musicVolume);
                wasModified = true;
            }

            if (sfxVolume < 0f || sfxVolume > 1f)
            {
                sfxVolume = Mathf.Clamp01(sfxVolume);
                wasModified = true;
            }

            return wasModified;
        }
    }
}
