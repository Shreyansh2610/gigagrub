using System;
using UnityEngine;

namespace GigaGrub.Rewards
{
    public enum QuestType
    {
        EatTotalFood,
        EatMegaFood,
        DefeatAICreatures,
        ReachLength,
        SurviveDuration,
        UseBoostDuration,
        WinMatch
    }

    [Serializable]
    public class QuestProgress
    {
        [SerializeField] private string questId = "";
        [SerializeField] private string title = "";
        [SerializeField] private string description = "";
        [SerializeField] private QuestType type = QuestType.EatTotalFood;
        [SerializeField] private int targetAmount = 50;
        [SerializeField] private int currentAmount = 0;
        [SerializeField] private int rewardCoins = 150;
        [SerializeField] private bool isClaimed = false;

        public string QuestId => questId;
        public string Title => title;
        public string Description => description;
        public QuestType Type => type;
        public int TargetAmount => targetAmount;
        public int CurrentAmount => currentAmount;
        public int RewardCoins => rewardCoins;
        public bool IsClaimed => isClaimed;

        public bool IsCompleted => currentAmount >= targetAmount;
        public float NormalizedProgress => targetAmount > 0 ? Mathf.Clamp01((float)currentAmount / targetAmount) : 0f;

        public QuestProgress(string id, string title, string desc, QuestType qType, int target, int coins)
        {
            this.questId = id;
            this.title = title;
            this.description = desc;
            this.type = qType;
            this.targetAmount = Mathf.Max(1, target);
            this.currentAmount = 0;
            this.rewardCoins = Mathf.Max(0, coins);
            this.isClaimed = false;
        }

        public void AddProgress(int amount)
        {
            if (isClaimed) return;
            currentAmount = Mathf.Clamp(currentAmount + amount, 0, targetAmount);
        }

        public void SetProgress(int amount)
        {
            if (isClaimed) return;
            currentAmount = Mathf.Clamp(amount, 0, targetAmount);
        }

        public void MarkClaimed()
        {
            isClaimed = true;
        }

        public QuestProgress Clone()
        {
            return new QuestProgress(questId, title, description, type, targetAmount, rewardCoins)
            {
                currentAmount = this.currentAmount,
                isClaimed = this.isClaimed
            };
        }
    }
}
