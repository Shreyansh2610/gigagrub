using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Cosmetics;

namespace GigaGrub.UI.Customization
{
    public class CustomizationUI : MonoBehaviour
    {
        [Header("Preview Section")]
        [SerializeField] private CreaturePreviewStage previewStage;

        [Header("Category Navigation")]
        [SerializeField] private Button creatureTabBtn;
        [SerializeField] private Button colorTabBtn;
        [SerializeField] private Button patternTabBtn;
        [SerializeField] private Button clothingTabBtn;
        [SerializeField] private Button effectTabBtn;

        [Header("Item Grid")]
        [SerializeField] private Transform gridContentParent;
        [SerializeField] private GameObject itemCardPrefab;

        [Header("Action Panel")]
        [SerializeField] private Text coinsText;
        [SerializeField] private Text selectedItemNameText;
        [SerializeField] private Text selectedItemRarityText;
        [SerializeField] private Button actionButton;
        [SerializeField] private Text actionButtonText;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button closeSaveButton;

        private CosmeticCategory currentCategory = CosmeticCategory.Creature;
        private CosmeticData selectedItem = null;
        private EquippedCosmetics stagingEquipped = null;
        private readonly List<CosmeticItemCardUI> activeCards = new List<CosmeticItemCardUI>();

        private void Awake()
        {
            CleanupAndLayoutTabs();
            BindButtons();
        }

        private void Start()
        {
            if (stagingEquipped == null)
            {
                OpenCustomization();
            }
        }

        private void CleanupAndLayoutTabs()
        {
            Transform tabsContainer = transform.Find("CategoryTabs");
            if (tabsContainer == null) tabsContainer = transform.Find("DialogBox/CategoryTabs");
            if (tabsContainer == null)
            {
                // Search recursively
                Transform[] all = GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i].name == "CategoryTabs")
                    {
                        tabsContainer = all[i];
                        break;
                    }
                }
            }

            if (tabsContainer == null) return;

            string[] removedNames = new string[] { "Tab_Hat", "Tab_Eyes", "Tab_Mouth", "Tab_Accessories", "Tab_Accessory" };
            for (int i = tabsContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = tabsContainer.GetChild(i);
                for (int r = 0; r < removedNames.Length; r++)
                {
                    if (child.name.Equals(removedNames[r], System.StringComparison.OrdinalIgnoreCase))
                    {
                        child.gameObject.SetActive(false);
                        if (Application.isPlaying)
                        {
                            Destroy(child.gameObject);
                        }
                        else
                        {
                            DestroyImmediate(child.gameObject);
                        }
                        break;
                    }
                }
            }

            // Auto-bind button references if null
            if (creatureTabBtn == null) creatureTabBtn = FindButtonInTabs(tabsContainer, "Tab_Creature");
            if (colorTabBtn == null) colorTabBtn = FindButtonInTabs(tabsContainer, "Tab_Color");
            if (patternTabBtn == null) patternTabBtn = FindButtonInTabs(tabsContainer, "Tab_Pattern");
            if (clothingTabBtn == null) clothingTabBtn = FindButtonInTabs(tabsContainer, "Tab_Clothes");
            if (effectTabBtn == null) effectTabBtn = FindButtonInTabs(tabsContainer, "Tab_Effects");

            // Re-layout remaining active tabs evenly
            List<RectTransform> activeTabs = new List<RectTransform>();
            for (int i = 0; i < tabsContainer.childCount; i++)
            {
                Transform child = tabsContainer.GetChild(i);
                if (child.gameObject.activeSelf)
                {
                    activeTabs.Add(child.GetComponent<RectTransform>());
                }
            }

            if (activeTabs.Count > 0)
            {
                float step = 1f / activeTabs.Count;
                for (int i = 0; i < activeTabs.Count; i++)
                {
                    RectTransform rt = activeTabs[i];
                    if (rt != null)
                    {
                        rt.anchorMin = new Vector2(i * step + 0.005f, 0f);
                        rt.anchorMax = new Vector2((i + 1) * step - 0.005f, 1f);
                        rt.offsetMin = Vector2.zero;
                        rt.offsetMax = Vector2.zero;
                    }
                }
            }
        }

        private Button FindButtonInTabs(Transform parent, string name)
        {
            Transform t = parent.Find(name);
            return t != null ? t.GetComponent<Button>() : null;
        }

        public void BindButtons()
        {
            if (creatureTabBtn != null) creatureTabBtn.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Creature));
            if (colorTabBtn != null) colorTabBtn.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Color));
            if (patternTabBtn != null) patternTabBtn.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Pattern));
            if (clothingTabBtn != null) clothingTabBtn.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Clothing));
            if (effectTabBtn != null) effectTabBtn.onClick.AddListener(() => SwitchCategory(CosmeticCategory.Effect));

            if (actionButton != null) actionButton.onClick.AddListener(OnActionButtonClicked);
            if (resetButton != null) resetButton.onClick.AddListener(OnResetClicked);
            if (closeSaveButton != null) closeSaveButton.onClick.AddListener(OnCloseSaveClicked);
        }

        public void OpenCustomization()
        {
            gameObject.SetActive(true);

            CosmeticManager mgr = CosmeticManager.Instance;
            if (mgr != null)
            {
                stagingEquipped = mgr.Inventory.Equipped.Clone();
            }
            else
            {
                stagingEquipped = EquippedCosmetics.CreateDefault();
            }

            if (previewStage != null)
            {
                previewStage.UpdatePreview(stagingEquipped);
            }

            SwitchCategory(CosmeticCategory.Creature);
            RefreshCoins();
        }

        public void SwitchCategory(CosmeticCategory category)
        {
            currentCategory = category;
            selectedItem = null;
            PopulateGrid();
            RefreshActionPanel();
        }

        public void RefreshCoins()
        {
            if (coinsText != null)
            {
                int coins = CosmeticManager.Instance != null ? CosmeticManager.Instance.Inventory.Coins : 0;
                coinsText.text = $"COINS: {coins:N0}";
            }
        }

        private void PopulateGrid()
        {
            ClearGrid();

            CosmeticDatabase db = CosmeticDatabase.Instance;
            if (db == null || gridContentParent == null) return;

            CosmeticInventory inv = CosmeticManager.Instance != null ? CosmeticManager.Instance.Inventory : null;
            IReadOnlyList<CosmeticData> items = db.GetItemsByCategory(currentCategory);

            // For optional categories (clothing, effect), add a "None/Unequip" card first
            bool allowsNone = (currentCategory == CosmeticCategory.Clothing ||
                              currentCategory == CosmeticCategory.Effect);

            if (allowsNone)
            {
                bool isNoneEquipped = IsCategoryUnequipped(currentCategory);
                CreateCard(null, true, isNoneEquipped, selectedItem == null && isNoneEquipped);
            }

            for (int i = 0; i < items.Count; i++)
            {
                CosmeticData data = items[i];
                if (data == null) continue;

                bool isUnlocked = inv != null ? inv.IsUnlocked(data) : true;
                bool isEquipped = IsItemEquippedInStaging(data);
                bool isSelected = selectedItem == data;

                CreateCard(data, isUnlocked, isEquipped, isSelected);
            }
        }

        private void CreateCard(CosmeticData data, bool isUnlocked, bool isEquipped, bool isSelected)
        {
            GameObject cardGo = null;
            if (itemCardPrefab != null)
            {
                cardGo = Instantiate(itemCardPrefab, gridContentParent);
            }
            else
            {
                // Fallback procedural card
                cardGo = new GameObject(data != null ? data.DisplayName : "NoneCard", typeof(RectTransform), typeof(Image), typeof(Button));
                cardGo.transform.SetParent(gridContentParent, false);
            }

            CosmeticItemCardUI cardUI = cardGo.GetComponent<CosmeticItemCardUI>();
            if (cardUI == null)
            {
                cardUI = cardGo.AddComponent<CosmeticItemCardUI>();
            }

            cardUI.Bind(data, isUnlocked, isEquipped, isSelected, OnCardSelected);
            activeCards.Add(cardUI);
        }

        private void ClearGrid()
        {
            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i] != null)
                {
                    Destroy(activeCards[i].gameObject);
                }
            }
            activeCards.Clear();
        }

        private void OnCardSelected(CosmeticData data)
        {
            selectedItem = data;

            // Update preview in real-time
            if (data == null)
            {
                UnequipCategoryInStaging(currentCategory);
            }
            else
            {
                EquipInStaging(data);
            }

            if (previewStage != null)
            {
                previewStage.UpdatePreview(stagingEquipped);
            }

            // Update selection visuals on cards
            for (int i = 0; i < activeCards.Count; i++)
            {
                if (activeCards[i] != null)
                {
                    activeCards[i].SetSelected(activeCards[i].ItemData == selectedItem);
                }
            }

            RefreshActionPanel();
        }

        private void EquipInStaging(CosmeticData data)
        {
            if (stagingEquipped == null || data == null) return;

            switch (data.Category)
            {
                case CosmeticCategory.Creature:
                    stagingEquipped.CreatureId = data.Id;
                    break;
                case CosmeticCategory.Color:
                    stagingEquipped.ColorId = data.Id;
                    stagingEquipped.UseCustomColors = false;
                    break;
                case CosmeticCategory.Pattern:
                    stagingEquipped.PatternId = data.Id;
                    break;
                case CosmeticCategory.Clothing:
                    stagingEquipped.ClothingId = data.Id;
                    break;
                case CosmeticCategory.Effect:
                    stagingEquipped.EffectId = data.Id;
                    break;
            }
        }

        private void UnequipCategoryInStaging(CosmeticCategory cat)
        {
            if (stagingEquipped == null) return;

            switch (cat)
            {
                case CosmeticCategory.Clothing:
                    stagingEquipped.ClothingId = "";
                    break;
                case CosmeticCategory.Effect:
                    stagingEquipped.EffectId = "";
                    break;
            }
        }

        private bool IsCategoryUnequipped(CosmeticCategory cat)
        {
            if (stagingEquipped == null) return true;
            return cat switch
            {
                CosmeticCategory.Clothing => string.IsNullOrEmpty(stagingEquipped.ClothingId),
                CosmeticCategory.Effect => string.IsNullOrEmpty(stagingEquipped.EffectId),
                _ => false
            };
        }

        private bool IsItemEquippedInStaging(CosmeticData item)
        {
            if (stagingEquipped == null || item == null) return false;
            return item.Category switch
            {
                CosmeticCategory.Creature => stagingEquipped.CreatureId == item.Id,
                CosmeticCategory.Color => stagingEquipped.ColorId == item.Id,
                CosmeticCategory.Pattern => stagingEquipped.PatternId == item.Id,
                CosmeticCategory.Clothing => stagingEquipped.ClothingId == item.Id,
                CosmeticCategory.Effect => stagingEquipped.EffectId == item.Id,
                _ => false
            };
        }

        private void RefreshActionPanel()
        {
            CosmeticInventory inv = CosmeticManager.Instance != null ? CosmeticManager.Instance.Inventory : null;

            if (selectedItem == null)
            {
                if (selectedItemNameText != null) selectedItemNameText.text = "None";
                if (selectedItemRarityText != null) selectedItemRarityText.text = "Standard";
                if (actionButton != null) actionButton.interactable = true;
                if (actionButtonText != null) actionButtonText.text = "Equipped";
                return;
            }

            if (selectedItemNameText != null) selectedItemNameText.text = selectedItem.DisplayName;
            if (selectedItemRarityText != null)
            {
                selectedItemRarityText.text = selectedItem.Rarity.ToString().ToUpper();
                selectedItemRarityText.color = selectedItem.GetRarityColor();
            }

            bool isUnlocked = inv != null ? inv.IsUnlocked(selectedItem) : true;
            bool isEquippedInActual = inv != null ? inv.IsItemEquipped(selectedItem) : false;

            if (actionButton != null && actionButtonText != null)
            {
                if (!isUnlocked)
                {
                    if (selectedItem.UnlockType == UnlockType.Coins)
                    {
                        bool canAfford = inv != null && inv.CanAfford(selectedItem);
                        actionButton.interactable = canAfford;
                        actionButtonText.text = $"Unlock ({selectedItem.Price:N0} Coins)";
                    }
                    else
                    {
                        actionButton.interactable = false;
                        actionButtonText.text = selectedItem.UnlockRequirement;
                    }
                }
                else
                {
                    actionButton.interactable = true;
                    actionButtonText.text = isEquippedInActual ? "Equipped" : "Equip";
                }
            }
        }

        private void OnActionButtonClicked()
        {
            if (selectedItem == null)
            {
                UnequipCategoryInStaging(currentCategory);
                if (CosmeticManager.Instance != null)
                {
                    CosmeticManager.Instance.Inventory.UnequipCategory(currentCategory);
                }
                PopulateGrid();
                RefreshActionPanel();
                return;
            }

            CosmeticInventory inv = CosmeticManager.Instance != null ? CosmeticManager.Instance.Inventory : null;
            if (inv == null) return;

            if (!inv.IsUnlocked(selectedItem))
            {
                bool unlocked = inv.TryUnlockItem(selectedItem);
                if (unlocked)
                {
                    inv.EquipItem(selectedItem);
                    RefreshCoins();
                }
            }
            else
            {
                inv.EquipItem(selectedItem);
            }

            stagingEquipped = inv.Equipped.Clone();
            if (previewStage != null)
            {
                previewStage.UpdatePreview(stagingEquipped);
            }

            PopulateGrid();
            RefreshActionPanel();
        }

        private void OnResetClicked()
        {
            CosmeticManager mgr = CosmeticManager.Instance;
            if (mgr != null)
            {
                stagingEquipped = mgr.Inventory.Equipped.Clone();
            }
            else
            {
                stagingEquipped = EquippedCosmetics.CreateDefault();
            }

            if (previewStage != null)
            {
                previewStage.UpdatePreview(stagingEquipped);
            }

            PopulateGrid();
            RefreshActionPanel();
        }

        public event System.Action OnClosed;
        private bool isClosing = false;

        public void OnCloseSaveClicked()
        {
            if (isClosing) return;
            isClosing = true;

            // 1. Commit equipped configuration to CosmeticManager & SaveManager
            if (CosmeticManager.Instance != null && stagingEquipped != null)
            {
                CosmeticDatabase db = CosmeticDatabase.Instance;
                if (db != null)
                {
                    CosmeticData cr = db.GetItemById(stagingEquipped.CreatureId);
                    CosmeticData co = db.GetItemById(stagingEquipped.ColorId);
                    CosmeticData pa = db.GetItemById(stagingEquipped.PatternId);
                    CosmeticData cl = db.GetItemById(stagingEquipped.ClothingId);
                    CosmeticData ef = db.GetItemById(stagingEquipped.EffectId);

                    if (cr != null) CosmeticManager.Instance.Inventory.EquipItem(cr);
                    if (co != null) CosmeticManager.Instance.Inventory.EquipItem(co);
                    if (pa != null) CosmeticManager.Instance.Inventory.EquipItem(pa);
                    if (cl != null) CosmeticManager.Instance.Inventory.EquipItem(cl); else CosmeticManager.Instance.Inventory.UnequipCategory(CosmeticCategory.Clothing);
                    if (ef != null) CosmeticManager.Instance.Inventory.EquipItem(ef); else CosmeticManager.Instance.Inventory.UnequipCategory(CosmeticCategory.Effect);
                }

                CosmeticManager.Instance.SaveToSaveManager();
            }

            // 2. Hide Modal CanvasGroup / Parent if nested
            Transform p = transform.parent;
            if (p != null)
            {
                CanvasGroup cg = p.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 0f;
                    cg.blocksRaycasts = false;
                    cg.interactable = false;
                }
                p.gameObject.SetActive(false);
            }

            gameObject.SetActive(false);

            // 3. Notify listeners
            OnClosed?.Invoke();

            // 4. Update MainMenuUI directly
            MainMenuUI menu = FindFirstObjectByType<MainMenuUI>();
            if (menu != null && menu.IsCustomizeOpen)
            {
                menu.CloseCustomization();
            }

            isClosing = false;
        }
    }
}
