using System;
using System.Collections.Generic;
using UnityEngine;

namespace GigaGrub.Cosmetics
{
    public class CosmeticInventory
    {
        private int coins = 1000; // Starting bonus for testing and progression
        private readonly HashSet<string> unlockedItemIds = new HashSet<string>();
        private EquippedCosmetics equipped = new EquippedCosmetics();

        public int Coins => coins;
        public EquippedCosmetics Equipped => equipped;
        public IReadOnlyCollection<string> UnlockedItemIds => unlockedItemIds;

        public event Action OnInventoryChanged;
        public event Action<CosmeticData> OnItemUnlocked;
        public event Action<CosmeticData> OnItemEquipped;

        public void InitializeFromSave(int savedCoins, List<string> savedUnlocked, EquippedCosmetics savedEquipped)
        {
            coins = Mathf.Max(0, savedCoins);
            unlockedItemIds.Clear();

            if (savedUnlocked != null)
            {
                for (int i = 0; i < savedUnlocked.Count; i++)
                {
                    if (!string.IsNullOrEmpty(savedUnlocked[i]))
                    {
                        unlockedItemIds.Add(savedUnlocked[i]);
                    }
                }
            }

            // Ensure all default unlocked items from database are unlocked
            if (CosmeticDatabase.Instance != null)
            {
                UnlockDefaultsFromDatabase(CosmeticDatabase.Instance);
            }

            equipped = savedEquipped != null ? savedEquipped.Clone() : EquippedCosmetics.CreateDefault();
            OnInventoryChanged?.Invoke();
        }

        public void UnlockDefaultsFromDatabase(CosmeticDatabase db)
        {
            if (db == null) return;

            CheckAndUnlockDefaults(db.Creatures);
            CheckAndUnlockDefaults(db.SkinColors);
            CheckAndUnlockDefaults(db.Patterns);
            CheckAndUnlockDefaults(db.Clothing);
            CheckAndUnlockDefaults(db.Hats);
            CheckAndUnlockDefaults(db.Eyes);
            CheckAndUnlockDefaults(db.Mouths);
            CheckAndUnlockDefaults(db.Accessories);
            CheckAndUnlockDefaults(db.Effects);
        }

        private void CheckAndUnlockDefaults<T>(IReadOnlyList<T> items) where T : CosmeticData
        {
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (item != null && (item.IsDefaultUnlocked || item.UnlockType == UnlockType.DefaultUnlocked))
                {
                    unlockedItemIds.Add(item.Id);
                }
            }
        }

        public bool IsUnlocked(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return true; // Empty means unequipped / default
            return unlockedItemIds.Contains(itemId);
        }

        public bool IsUnlocked(CosmeticData item)
        {
            if (item == null) return true;
            if (item.IsDefaultUnlocked || item.UnlockType == UnlockType.DefaultUnlocked) return true;
            return unlockedItemIds.Contains(item.Id);
        }

        public bool CanAfford(CosmeticData item)
        {
            if (item == null) return false;
            return coins >= item.Price;
        }

        public bool TryUnlockItem(CosmeticData item)
        {
            if (item == null) return false;
            if (IsUnlocked(item.Id)) return true;

            if (item.UnlockType == UnlockType.Coins)
            {
                if (coins < item.Price) return false;
                coins -= item.Price;
            }

            unlockedItemIds.Add(item.Id);
            OnItemUnlocked?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            coins += amount;
            OnInventoryChanged?.Invoke();
        }

        public bool EquipItem(CosmeticData item)
        {
            if (item == null) return false;
            if (!IsUnlocked(item.Id))
            {
                bool unlocked = TryUnlockItem(item);
                if (!unlocked) return false;
            }

            switch (item.Category)
            {
                case CosmeticCategory.Creature:
                    equipped.CreatureId = item.Id;
                    break;
                case CosmeticCategory.Color:
                    equipped.ColorId = item.Id;
                    equipped.UseCustomColors = false;
                    break;
                case CosmeticCategory.Pattern:
                    equipped.PatternId = item.Id;
                    break;
                case CosmeticCategory.Clothing:
                    equipped.ClothingId = item.Id;
                    break;
                case CosmeticCategory.Hat:
                    equipped.HatId = item.Id;
                    break;
                case CosmeticCategory.Eyes:
                    equipped.EyesId = item.Id;
                    break;
                case CosmeticCategory.Mouth:
                    equipped.MouthId = item.Id;
                    break;
                case CosmeticCategory.Accessory:
                    if (item is AccessoryData acc)
                    {
                        if (acc.Slot == AccessorySlot.Back) equipped.BackAccessoryId = item.Id;
                        else if (acc.Slot == AccessorySlot.Neck) equipped.NeckAccessoryId = item.Id;
                        else if (acc.Slot == AccessorySlot.Tail) equipped.TailAccessoryId = item.Id;
                        else equipped.BackAccessoryId = item.Id;
                    }
                    else
                    {
                        equipped.BackAccessoryId = item.Id;
                    }
                    break;
                case CosmeticCategory.Effect:
                    equipped.EffectId = item.Id;
                    break;
            }

            OnItemEquipped?.Invoke(item);
            OnInventoryChanged?.Invoke();
            return true;
        }

        public void UnequipCategory(CosmeticCategory category)
        {
            switch (category)
            {
                case CosmeticCategory.Clothing:
                    equipped.ClothingId = "";
                    break;
                case CosmeticCategory.Hat:
                    equipped.HatId = "";
                    break;
                case CosmeticCategory.Accessory:
                    equipped.BackAccessoryId = "";
                    equipped.NeckAccessoryId = "";
                    equipped.TailAccessoryId = "";
                    break;
                case CosmeticCategory.Effect:
                    equipped.EffectId = "";
                    break;
            }
            OnInventoryChanged?.Invoke();
        }

        public bool IsItemEquipped(CosmeticData item)
        {
            if (item == null) return false;
            return item.Category switch
            {
                CosmeticCategory.Creature => equipped.CreatureId == item.Id,
                CosmeticCategory.Color => equipped.ColorId == item.Id,
                CosmeticCategory.Pattern => equipped.PatternId == item.Id,
                CosmeticCategory.Clothing => equipped.ClothingId == item.Id,
                CosmeticCategory.Hat => equipped.HatId == item.Id,
                CosmeticCategory.Eyes => equipped.EyesId == item.Id,
                CosmeticCategory.Mouth => equipped.MouthId == item.Id,
                CosmeticCategory.Accessory => equipped.BackAccessoryId == item.Id || equipped.NeckAccessoryId == item.Id || equipped.TailAccessoryId == item.Id,
                CosmeticCategory.Effect => equipped.EffectId == item.Id,
                _ => false
            };
        }

        public void SetCustomColors(Color primary, Color secondary, Color accent)
        {
            equipped.UseCustomColors = true;
            equipped.CustomPrimary = primary;
            equipped.CustomSecondary = secondary;
            equipped.CustomAccent = accent;
            OnInventoryChanged?.Invoke();
        }

        public bool UnlockCosmetic(CosmeticData item) => TryUnlockItem(item);
        public bool EquipCosmetic(CosmeticData item) => EquipItem(item);
        public void UnequipCosmetic(CosmeticCategory category) => UnequipCategory(category);

        public List<string> GetUnlockedList()
        {
            return new List<string>(unlockedItemIds);
        }
    }
}
