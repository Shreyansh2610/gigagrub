using System;
using System.Collections.Generic;
using UnityEngine;
using GigaGrub.Food;
using GigaGrub.Cosmetics;
using GigaGrub.Systems;
using GigaGrub.Data;

namespace GigaGrub.Rewards
{
    public class QuestManager : MonoBehaviour
    {
        public static QuestManager Instance { get; private set; }

        [SerializeField] private List<QuestProgress> activeQuests = new List<QuestProgress>();
        private string currentQuestDate = "";

        public IReadOnlyList<QuestProgress> ActiveQuests => activeQuests;

        public event Action OnQuestsUpdated;
        public event Action<QuestProgress, int> OnQuestClaimed; // (quest, coins)

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            InitializeDailyQuests();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void InitializeDailyQuests()
        {
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");

            if (SaveManager.Instance != null && SaveManager.Instance.Statistics != null)
            {
                SaveData data = SaveManager.Instance.Statistics.GetDataSnapshot();
                if (string.Equals(data.LastQuestDate, todayStr, StringComparison.OrdinalIgnoreCase) && data.ActiveQuests != null && data.ActiveQuests.Count > 0)
                {
                    // Restore today's saved quests
                    activeQuests = new List<QuestProgress>();
                    for (int i = 0; i < data.ActiveQuests.Count; i++)
                    {
                        activeQuests.Add(data.ActiveQuests[i].Clone());
                    }
                    currentQuestDate = todayStr;
                    OnQuestsUpdated?.Invoke();
                    return;
                }
            }

            // Generate fresh set of daily quests for today
            GenerateNewDailyQuests(todayStr);
        }

        public void GenerateNewDailyQuests(string dateStr)
        {
            currentQuestDate = dateStr;
            activeQuests = new List<QuestProgress>
            {
                new QuestProgress("q_food_50", "Feast Time", "Collect 50 food items in the arena", QuestType.EatTotalFood, 50, 150),
                new QuestProgress("q_mega_3", "Astral Core Hunter", "Eat 3 Mega Food / Astral Cores", QuestType.EatMegaFood, 3, 200),
                new QuestProgress("q_kills_2", "Apex Predator", "Defeat 2 rival AI creatures", QuestType.DefeatAICreatures, 2, 250),
                new QuestProgress("q_length_30", "Giga Growth", "Reach a creature length of 30 segments", QuestType.ReachLength, 30, 200)
            };

            SaveActiveQuests();
            OnQuestsUpdated?.Invoke();
        }

        public void ReportFoodEaten(FoodData food)
        {
            if (food == null) return;

            bool changed = false;
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress q = activeQuests[i];
                if (q.IsClaimed) continue;

                if (q.Type == QuestType.EatTotalFood)
                {
                    q.AddProgress(1);
                    changed = true;
                }
                else if (q.Type == QuestType.EatMegaFood && food.FoodType == FoodType.Mega)
                {
                    q.AddProgress(1);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveActiveQuests();
                OnQuestsUpdated?.Invoke();
            }
        }

        public void ReportAIDefeated(int count = 1)
        {
            bool changed = false;
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress q = activeQuests[i];
                if (q.IsClaimed) continue;

                if (q.Type == QuestType.DefeatAICreatures)
                {
                    q.AddProgress(count);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveActiveQuests();
                OnQuestsUpdated?.Invoke();
            }
        }

        public void ReportLengthReached(int length)
        {
            bool changed = false;
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress q = activeQuests[i];
                if (q.IsClaimed) continue;

                if (q.Type == QuestType.ReachLength && length > q.CurrentAmount)
                {
                    q.SetProgress(length);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveActiveQuests();
                OnQuestsUpdated?.Invoke();
            }
        }

        public void ReportMatchFinished(int finalRank)
        {
            bool changed = false;
            for (int i = 0; i < activeQuests.Count; i++)
            {
                QuestProgress q = activeQuests[i];
                if (q.IsClaimed) continue;

                if (q.Type == QuestType.WinMatch && finalRank == 1)
                {
                    q.SetProgress(1);
                    changed = true;
                }
            }

            if (changed)
            {
                SaveActiveQuests();
                OnQuestsUpdated?.Invoke();
            }
        }

        public bool ClaimQuest(string questId)
        {
            QuestProgress quest = activeQuests.Find(q => q.QuestId == questId);
            if (quest == null || !quest.IsCompleted || quest.IsClaimed)
            {
                return false;
            }

            quest.MarkClaimed();

            // 1. Award Coins
            if (CosmeticManager.Instance != null && CosmeticManager.Instance.Inventory != null)
            {
                CosmeticManager.Instance.Inventory.AddCoins(quest.RewardCoins);
            }

            // 2. Persist
            SaveActiveQuests();

            Debug.Log($"[QuestManager] Claimed Quest '{quest.Title}' -> Awarded {quest.RewardCoins} Coins!");
            OnQuestClaimed?.Invoke(quest, quest.RewardCoins);
            OnQuestsUpdated?.Invoke();
            return true;
        }

        public bool HasAnyClaimableRewards()
        {
            for (int i = 0; i < activeQuests.Count; i++)
            {
                if (activeQuests[i].IsCompleted && !activeQuests[i].IsClaimed)
                {
                    return true;
                }
            }
            return false;
        }

        public void SaveActiveQuests()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Statistics != null)
            {
                SaveManager.Instance.Statistics.SetDailyQuests(currentQuestDate, activeQuests);
                SaveManager.Instance.Save();
            }
        }
    }
}
