using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Cosmetics;
using GigaGrub.Data;
using GigaGrub.Systems;

namespace GigaGrub.Rewards
{
    public class DailyRewardsAndQuestsUI : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button dailyRewardsTabBtn;
        [SerializeField] private Button questsTabBtn;
        [SerializeField] private GameObject dailyRewardsPanel;
        [SerializeField] private GameObject questsPanel;

        [Header("Daily Rewards Elements")]
        [SerializeField] private Text streakText;
        [SerializeField] private Text countdownText;
        [SerializeField] private Button claimDailyBtn;
        [SerializeField] private Text claimDailyBtnText;
        [SerializeField] private Transform dayCardsParent;
        [SerializeField] private Image[] dayCardBgs;
        [SerializeField] private Text[] dayCardRewardTexts;
        [SerializeField] private GameObject[] dayCardClaimedBadges;

        [Header("Quests Elements")]
        [SerializeField] private Transform questsContentParent;
        [SerializeField] private Text questsCoinsHeader;

        [Header("Common")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Text userCoinsText;

        private bool isShowingQuests = false;

        private void Awake()
        {
            if (dailyRewardsTabBtn != null) dailyRewardsTabBtn.onClick.AddListener(() => SetTab(false));
            if (questsTabBtn != null) questsTabBtn.onClick.AddListener(() => SetTab(true));
            if (claimDailyBtn != null) claimDailyBtn.onClick.AddListener(OnClaimDailyClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnEnable()
        {
            if (DailyRewardManager.Instance != null)
            {
                DailyRewardManager.Instance.OnRewardClaimed += HandleRewardClaimed;
                DailyRewardManager.Instance.OnDailyStatusChanged += RefreshUI;
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestsUpdated += RefreshUI;
                QuestManager.Instance.OnQuestClaimed += HandleQuestClaimed;
            }

            RefreshUI();
        }

        private void OnDisable()
        {
            if (DailyRewardManager.Instance != null)
            {
                DailyRewardManager.Instance.OnRewardClaimed -= HandleRewardClaimed;
                DailyRewardManager.Instance.OnDailyStatusChanged -= RefreshUI;
            }

            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestsUpdated -= RefreshUI;
                QuestManager.Instance.OnQuestClaimed -= HandleQuestClaimed;
            }
        }

        private void Update()
        {
            if (countdownText != null && DailyRewardManager.Instance != null)
            {
                TimeSpan remaining = DailyRewardManager.Instance.GetTimeUntilNextDay();
                countdownText.text = $"Next Reward In: {remaining.Hours:D2}:{remaining.Minutes:D2}:{remaining.Seconds:D2}";
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            SetTab(false);
            RefreshUI();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void SetTab(bool showQuests)
        {
            isShowingQuests = showQuests;

            if (dailyRewardsPanel != null) dailyRewardsPanel.SetActive(!showQuests);
            if (questsPanel != null) questsPanel.SetActive(showQuests);

            if (dailyRewardsTabBtn != null)
            {
                Image img = dailyRewardsTabBtn.GetComponent<Image>();
                if (img != null) img.color = !showQuests ? new Color(0.06f, 0.73f, 0.51f, 1f) : new Color(0.12f, 0.16f, 0.24f, 0.90f);
            }

            if (questsTabBtn != null)
            {
                Image img = questsTabBtn.GetComponent<Image>();
                if (img != null) img.color = showQuests ? new Color(0.06f, 0.73f, 0.51f, 1f) : new Color(0.12f, 0.16f, 0.24f, 0.90f);
            }

            RefreshUI();
        }

        public void RefreshUI()
        {
            UpdateCoinsDisplay();

            if (!isShowingQuests)
            {
                RefreshDailyRewardsUI();
            }
            else
            {
                RefreshQuestsUI();
            }
        }

        private void UpdateCoinsDisplay()
        {
            int coins = 0;
            if (CosmeticManager.Instance != null && CosmeticManager.Instance.Inventory != null)
            {
                coins = CosmeticManager.Instance.Inventory.Coins;
            }
            else if (SaveManager.Instance != null && SaveManager.Instance.Statistics != null)
            {
                coins = SaveManager.Instance.Statistics.Coins;
            }

            if (userCoinsText != null)
            {
                userCoinsText.text = $"COINS: {coins:N0}";
            }
        }

        private void RefreshDailyRewardsUI()
        {
            if (DailyRewardManager.Instance == null) return;

            bool isAvailable = DailyRewardManager.Instance.IsRewardAvailable();
            int currentDay = DailyRewardManager.Instance.GetCurrentStreakDay();

            if (streakText != null)
            {
                streakText.text = $"LOGIN STREAK: DAY {currentDay} OF 7";
            }

            if (claimDailyBtn != null)
            {
                claimDailyBtn.interactable = isAvailable;
                Image btnImg = claimDailyBtn.GetComponent<Image>();
                if (btnImg != null)
                {
                    btnImg.color = isAvailable ? new Color(0.06f, 0.73f, 0.51f, 1f) : new Color(0.20f, 0.25f, 0.35f, 0.8f);
                }
            }

            if (claimDailyBtnText != null)
            {
                int rewardCoins = DailyRewardManager.GetRewardCoinsForDay(currentDay);
                claimDailyBtnText.text = isAvailable ? $"CLAIM DAY {currentDay} (+{rewardCoins} COINS)" : "CLAIMED TODAY";
            }

            // Refresh 7-Day Calendar Cards
            if (dayCardBgs != null)
            {
                for (int i = 0; i < dayCardBgs.Length && i < 7; i++)
                {
                    int dayNum = i + 1;
                    int coins = DailyRewardManager.GetRewardCoinsForDay(dayNum);

                    if (dayCardRewardTexts != null && i < dayCardRewardTexts.Length && dayCardRewardTexts[i] != null)
                    {
                        dayCardRewardTexts[i].text = $"+{coins}\nCOINS";
                    }

                    if (dayNum < currentDay || (dayNum == currentDay && !isAvailable))
                    {
                        // Already claimed
                        dayCardBgs[i].color = new Color(0.08f, 0.12f, 0.18f, 0.7f);
                        if (dayCardClaimedBadges != null && i < dayCardClaimedBadges.Length && dayCardClaimedBadges[i] != null)
                        {
                            dayCardClaimedBadges[i].SetActive(true);
                        }
                    }
                    else if (dayNum == currentDay && isAvailable)
                    {
                        // Ready to claim today
                        dayCardBgs[i].color = new Color(0.12f, 0.45f, 0.30f, 0.95f);
                        if (dayCardClaimedBadges != null && i < dayCardClaimedBadges.Length && dayCardClaimedBadges[i] != null)
                        {
                            dayCardClaimedBadges[i].SetActive(false);
                        }
                    }
                    else
                    {
                        // Future locked day
                        dayCardBgs[i].color = new Color(0.10f, 0.14f, 0.22f, 0.85f);
                        if (dayCardClaimedBadges != null && i < dayCardClaimedBadges.Length && dayCardClaimedBadges[i] != null)
                        {
                            dayCardClaimedBadges[i].SetActive(false);
                        }
                    }
                }
            }
        }

        private void RefreshQuestsUI()
        {
            if (QuestManager.Instance == null || questsContentParent == null) return;

            // Clear old cards
            for (int i = questsContentParent.childCount - 1; i >= 0; i--)
            {
                Destroy(questsContentParent.GetChild(i).gameObject);
            }

            IReadOnlyList<QuestProgress> quests = QuestManager.Instance.ActiveQuests;
            for (int i = 0; i < quests.Count; i++)
            {
                QuestProgress q = quests[i];
                CreateQuestCard(q, questsContentParent);
            }
        }

        private void CreateQuestCard(QuestProgress quest, Transform parent)
        {
            GameObject cardGo = new GameObject($"Quest_{quest.QuestId}", typeof(RectTransform));
            cardGo.transform.SetParent(parent, false);

            RectTransform cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(800f, 90f);

            Image cardBg = cardGo.AddComponent<Image>();
            cardBg.color = quest.IsClaimed ? new Color(0.08f, 0.11f, 0.16f, 0.7f) : (quest.IsCompleted ? new Color(0.12f, 0.35f, 0.25f, 0.95f) : new Color(0.10f, 0.14f, 0.22f, 0.90f));

            // Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(cardGo.transform, false);
            RectTransform titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.03f, 0.52f);
            titleRect.anchorMax = new Vector2(0.60f, 0.95f);
            titleRect.offsetMin = titleRect.offsetMax = Vector2.zero;

            Text titleTxt = titleGo.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 20;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleLeft;
            titleTxt.color = Color.white;
            titleTxt.text = quest.Title;

            // Description
            GameObject descGo = new GameObject("Desc", typeof(RectTransform));
            descGo.transform.SetParent(cardGo.transform, false);
            RectTransform descRect = descGo.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.03f, 0.12f);
            descRect.anchorMax = new Vector2(0.50f, 0.50f);
            descRect.offsetMin = descRect.offsetMax = Vector2.zero;

            Text descTxt = descGo.AddComponent<Text>();
            descTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            descTxt.fontSize = 15;
            descTxt.alignment = TextAnchor.MiddleLeft;
            descTxt.color = new Color(0.75f, 0.80f, 0.88f);
            descTxt.text = $"{quest.Description} ({quest.CurrentAmount}/{quest.TargetAmount})";

            // Progress Bar Background
            GameObject barBgGo = new GameObject("BarBg", typeof(RectTransform));
            barBgGo.transform.SetParent(cardGo.transform, false);
            RectTransform barBgRect = barBgGo.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.52f, 0.35f);
            barBgRect.anchorMax = new Vector2(0.74f, 0.65f);
            barBgRect.offsetMin = barBgRect.offsetMax = Vector2.zero;

            Image barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.05f, 0.08f, 0.12f, 0.9f);

            // Progress Fill
            GameObject barFillGo = new GameObject("BarFill", typeof(RectTransform));
            barFillGo.transform.SetParent(barBgGo.transform, false);
            RectTransform barFillRect = barFillGo.GetComponent<RectTransform>();
            barFillRect.anchorMin = Vector2.zero;
            barFillRect.anchorMax = new Vector2(quest.NormalizedProgress, 1f);
            barFillRect.offsetMin = barFillRect.offsetMax = Vector2.zero;

            Image barFillImg = barFillGo.AddComponent<Image>();
            barFillImg.color = new Color(0.29f, 0.87f, 0.50f, 1f);

            // Action / Claim Button
            GameObject actBtnGo = new GameObject("ClaimButton", typeof(RectTransform));
            actBtnGo.transform.SetParent(cardGo.transform, false);
            RectTransform actRect = actBtnGo.GetComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.76f, 0.15f);
            actRect.anchorMax = new Vector2(0.97f, 0.85f);
            actRect.offsetMin = actRect.offsetMax = Vector2.zero;

            Image actImg = actBtnGo.AddComponent<Image>();
            Button actBtn = actBtnGo.AddComponent<Button>();

            GameObject actTxtGo = new GameObject("Text", typeof(RectTransform));
            actTxtGo.transform.SetParent(actBtnGo.transform, false);
            RectTransform actTxtRect = actTxtGo.GetComponent<RectTransform>();
            actTxtRect.anchorMin = Vector2.zero;
            actTxtRect.anchorMax = Vector2.one;
            actTxtRect.offsetMin = actTxtRect.offsetMax = Vector2.zero;

            Text actTxt = actTxtGo.AddComponent<Text>();
            actTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            actTxt.fontSize = 18;
            actTxt.fontStyle = FontStyle.Bold;
            actTxt.alignment = TextAnchor.MiddleCenter;

            if (quest.IsClaimed)
            {
                actImg.color = new Color(0.15f, 0.20f, 0.28f, 0.6f);
                actTxt.color = new Color(0.6f, 0.6f, 0.6f);
                actTxt.text = "CLAIMED";
                actBtn.interactable = false;
            }
            else if (quest.IsCompleted)
            {
                actImg.color = new Color(0.06f, 0.73f, 0.51f, 1f);
                actTxt.color = Color.white;
                actTxt.text = $"+{quest.RewardCoins}\nCLAIM";
                actBtn.interactable = true;
                string qId = quest.QuestId;
                actBtn.onClick.AddListener(() => OnQuestClaimClicked(qId));
            }
            else
            {
                actImg.color = new Color(0.18f, 0.23f, 0.32f, 0.8f);
                actTxt.color = new Color(0.8f, 0.85f, 0.9f);
                actTxt.text = $"+{quest.RewardCoins}\nCOINS";
                actBtn.interactable = false;
            }
        }

        private void OnClaimDailyClicked()
        {
            if (DailyRewardManager.Instance != null)
            {
                DailyRewardManager.Instance.ClaimTodayReward();
            }
        }

        private void OnQuestClaimClicked(string questId)
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.ClaimQuest(questId);
            }
        }

        private void HandleRewardClaimed(int day, int coins)
        {
            RefreshUI();
        }

        private void HandleQuestClaimed(QuestProgress quest, int coins)
        {
            RefreshUI();
        }

        private void OnCloseClicked()
        {
            Hide();
        }
    }
}
