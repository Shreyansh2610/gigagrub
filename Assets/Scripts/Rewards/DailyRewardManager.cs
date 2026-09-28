using System;
using UnityEngine;
using GigaGrub.Cosmetics;
using GigaGrub.Systems;
using GigaGrub.Data;

namespace GigaGrub.Rewards
{
    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        private static readonly int[] DayRewards = new int[] { 100, 150, 200, 250, 350, 500, 800 };

        public event Action<int, int> OnRewardClaimed; // (dayIndex, coinsEarned)
        public event Action OnDailyStatusChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public static int GetRewardCoinsForDay(int dayNumber)
        {
            int index = Mathf.Clamp(dayNumber - 1, 0, DayRewards.Length - 1);
            return DayRewards[index];
        }

        public bool IsRewardAvailable()
        {
            string today = GetTodayDateString();
            string lastClaim = GetLastClaimDateString();

            if (string.IsNullOrEmpty(lastClaim))
            {
                return true; // First time playing
            }

            return !string.Equals(today, lastClaim, StringComparison.OrdinalIgnoreCase);
        }

        public int GetCurrentStreakDay()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Statistics == null)
            {
                return 1;
            }

            SaveData data = SaveManager.Instance.Statistics.GetDataSnapshot();
            string lastClaim = data.LastDailyClaimDate;
            int streak = data.DailyStreak;

            if (string.IsNullOrEmpty(lastClaim))
            {
                return 1;
            }

            DateTime today = DateTime.Today;
            if (DateTime.TryParse(lastClaim, out DateTime lastClaimDate))
            {
                int daysDiff = (today - lastClaimDate.Date).Days;
                if (daysDiff == 0)
                {
                    // Already claimed today
                    return Mathf.Clamp(streak, 1, 7);
                }
                else if (daysDiff == 1)
                {
                    // Next consecutive day ready to claim
                    int nextDay = streak + 1;
                    if (nextDay > 7) nextDay = 1; // Cycle back after 7 days
                    return nextDay;
                }
                else
                {
                    // Missed more than 1 day -> reset streak to Day 1
                    return 1;
                }
            }

            return 1;
        }

        public TimeSpan GetTimeUntilNextDay()
        {
            DateTime now = DateTime.Now;
            DateTime tomorrow = DateTime.Today.AddDays(1);
            return tomorrow - now;
        }

        public bool ClaimTodayReward()
        {
            if (!IsRewardAvailable())
            {
                Debug.Log("[DailyRewardManager] Daily reward already claimed today.");
                return false;
            }

            int targetDay = GetCurrentStreakDay();
            int coinReward = GetRewardCoinsForDay(targetDay);
            string todayStr = GetTodayDateString();

            // 1. Award Coins via CosmeticManager / CosmeticInventory
            if (CosmeticManager.Instance != null && CosmeticManager.Instance.Inventory != null)
            {
                CosmeticManager.Instance.Inventory.AddCoins(coinReward);
            }

            // 2. Persist to SaveManager
            if (SaveManager.Instance != null && SaveManager.Instance.Statistics != null)
            {
                SaveManager.Instance.Statistics.RecordDailyRewardClaim(todayStr, targetDay, coinReward);
                SaveManager.Instance.Save();
            }

            Debug.Log($"[DailyRewardManager] Successfully claimed Day {targetDay} reward: {coinReward} Coins!");
            OnRewardClaimed?.Invoke(targetDay, coinReward);
            OnDailyStatusChanged?.Invoke();
            return true;
        }

        private string GetTodayDateString()
        {
            return DateTime.Today.ToString("yyyy-MM-dd");
        }

        private string GetLastClaimDateString()
        {
            if (SaveManager.Instance == null || SaveManager.Instance.Statistics == null)
            {
                return "";
            }

            return SaveManager.Instance.Statistics.GetDataSnapshot().LastDailyClaimDate;
        }
    }
}
