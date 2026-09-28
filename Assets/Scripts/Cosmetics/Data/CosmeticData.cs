using UnityEngine;

namespace GigaGrub.Cosmetics
{
    public abstract class CosmeticData : ScriptableObject
    {
        [Header("Identification")]
        [SerializeField] protected string id;
        [SerializeField] protected string displayName;
        [SerializeField] protected CosmeticCategory category;
        [SerializeField] protected CosmeticRarity rarity = CosmeticRarity.Common;
        [SerializeField] protected Sprite previewSprite;

        [Header("Economy & Unlocking")]
        [SerializeField] protected UnlockType unlockType = UnlockType.DefaultUnlocked;
        [SerializeField] protected int price = 0;
        [SerializeField] protected string unlockRequirement = "Default";
        [SerializeField] protected bool isDefaultUnlocked = true;

        public string Id => id;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
        public CosmeticCategory Category => category;
        public CosmeticRarity Rarity => rarity;
        public virtual Sprite PreviewSprite => previewSprite;
        public UnlockType UnlockType => unlockType;
        public int Price => price;
        public string UnlockRequirement => unlockRequirement;
        public bool IsDefaultUnlocked => isDefaultUnlocked;

        public virtual void ConfigureBase(string newId, string newName, CosmeticCategory newCategory, CosmeticRarity newRarity, Sprite preview, UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            id = newId;
            displayName = newName;
            category = newCategory;
            rarity = newRarity;
            previewSprite = preview;
            unlockType = unlock;
            price = itemPrice;
            unlockRequirement = requirement;
            isDefaultUnlocked = isDefault;
        }

        public Color GetRarityColor()
        {
            return rarity switch
            {
                CosmeticRarity.Common => new Color(0.7f, 0.75f, 0.8f),
                CosmeticRarity.Uncommon => new Color(0.2f, 0.85f, 0.4f),
                CosmeticRarity.Rare => new Color(0.15f, 0.65f, 1f),
                CosmeticRarity.Epic => new Color(0.75f, 0.25f, 1f),
                CosmeticRarity.Legendary => new Color(1f, 0.75f, 0.1f),
                CosmeticRarity.Mythic => new Color(1f, 0.2f, 0.55f),
                _ => Color.white
            };
        }
    }
}
