using System;
using System.Collections.Generic;
using UnityEngine;
using GigaGrub.Cosmetics;

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
        public int Coins => data != null ? data.Coins : 1000;
        public List<string> UnlockedCosmetics => data != null ? data.UnlockedCosmetics : new List<string>();
        public EquippedCosmetics EquippedCosmetics => data != null ? data.EquippedCosmetics : EquippedCosmetics.CreateDefault();
        public int Version => data != null ? data.Version : SaveData.CurrentSaveVersion;
        public long LastUpdatedTimestamp => data != null ? data.Timestamp : 0;

        public event Action<SaveData> OnStatisticsChanged;
        public event Action<float, float, bool> OnSettingsChanged;
        public event Action<int, List<string>, EquippedCosmetics> OnCosmeticsChanged;

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
            OnCosmeticsChanged?.Invoke(data.Coins, data.UnlockedCosmetics, data.EquippedCosmetics);
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

            // Earn reward coins per match based on food and AI defeated
            int earnedCoins = (cleanFood / 5) + (cleanAI * 20);
            data.Coins += earnedCoins;

            data.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            data.ValidateAndSanitize();
            OnStatisticsChanged?.Invoke(data);
            OnCosmeticsChanged?.Invoke(data.Coins, data.UnlockedCosmetics, data.EquippedCosmetics);
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

        public void SetCosmetics(int newCoins, List<string> newUnlocked, EquippedCosmetics newEquipped)
        {
            if (data == null)
            {
                data = SaveData.CreateDefault();
            }

            data.Coins = Mathf.Max(0, newCoins);
            data.UnlockedCosmetics = newUnlocked ?? new List<string>();
            data.EquippedCosmetics = newEquipped ?? EquippedCosmetics.CreateDefault();
            data.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            data.ValidateAndSanitize();
            OnCosmeticsChanged?.Invoke(data.Coins, data.UnlockedCosmetics, data.EquippedCosmetics);
            OnStatisticsChanged?.Invoke(data);
        }

        public void RecordDailyRewardClaim(string todayDateStr, int streakDay, int awardedCoins)
        {
            if (data == null) data = SaveData.CreateDefault();
            data.LastDailyClaimDate = todayDateStr;
            data.DailyStreak = streakDay;
            data.Coins += awardedCoins;
            data.Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            data.ValidateAndSanitize();
            OnCosmeticsChanged?.Invoke(data.Coins, data.UnlockedCosmetics, data.EquippedCosmetics);
            OnStatisticsChanged?.Invoke(data);
        }

        public void SetDailyQuests(string questDateStr, List<GigaGrub.Rewards.QuestProgress> quests)
        {
            if (data == null) data = SaveData.CreateDefault();
            data.LastQuestDate = questDateStr;
            data.ActiveQuests = quests;
            data.Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            data.ValidateAndSanitize();
            OnStatisticsChanged?.Invoke(data);
        }

        public void ResetStatistics()
        {
            float music = MusicVolume;
            float sfx = SFXVolume;
            bool vib = VibrationEnabled;
            int coins = Coins;
            List<string> unlocked = new List<string>(UnlockedCosmetics);
            EquippedCosmetics equipped = EquippedCosmetics.Clone();

            data = SaveData.CreateDefault();
            data.MusicVolume = music;
            data.SFXVolume = sfx;
            data.VibrationEnabled = vib;
            data.Coins = coins;
            data.UnlockedCosmetics = unlocked;
            data.EquippedCosmetics = equipped;

            OnStatisticsChanged?.Invoke(data);
        }
    }
}
