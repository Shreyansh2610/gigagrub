using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "Accessory_", menuName = "GigaGrub/Cosmetics/Accessory Item")]
    public class AccessoryData : CosmeticData
    {
        [Header("Slot & Placement")]
        [SerializeField] private AccessorySlot slot = AccessorySlot.Back;
        [SerializeField] private Sprite accessorySprite;
        [SerializeField] private Vector2 localOffset = Vector2.zero;
        [SerializeField] private Vector2 localScale = Vector2.one;
        [SerializeField] private int sortingOffset = -5; // Negative to render behind head/segment if back/wings
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private bool attachToTail = false;

        public AccessorySlot Slot => slot;
        public override Sprite PreviewSprite => previewSprite != null ? previewSprite : accessorySprite;
        public Sprite AccessorySprite => accessorySprite;
        public Vector2 LocalOffset => localOffset;
        public Vector2 LocalScale => localScale;
        public int SortingOffset => sortingOffset;
        public Color TintColor => tintColor;
        public bool AttachToTail => attachToTail;

        public void ConfigureAccessory(string newId, string newName, AccessorySlot accSlot,
            CosmeticRarity newRarity, Sprite preview, Sprite sprite, Vector2 offset, Vector2 scale,
            int sortOffset, Color tint, bool toTail,
            UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            ConfigureBase(newId, newName, CosmeticCategory.Accessory, newRarity, preview, unlock, itemPrice, requirement, isDefault);
            slot = accSlot;
            accessorySprite = sprite;
            localOffset = offset;
            localScale = scale;
            sortingOffset = sortOffset;
            tintColor = tint;
            attachToTail = toTail;
        }
    }
}
