using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "Hat_", menuName = "GigaGrub/Cosmetics/Head Accessory")]
    public class HeadAccessoryData : CosmeticData
    {
        [Header("Visual Placement")]
        [SerializeField] private Sprite accessorySprite;
        [SerializeField] private Vector2 localOffset = new Vector2(0f, 0.45f);
        [SerializeField] private Vector2 localScale = Vector2.one;
        [SerializeField] private int sortingOffset = 10;
        [SerializeField] private Color tintColor = Color.white;

        public override Sprite PreviewSprite => previewSprite != null ? previewSprite : accessorySprite;
        public Sprite AccessorySprite => accessorySprite;
        public Vector2 LocalOffset => localOffset;
        public Vector2 LocalScale => localScale;
        public int SortingOffset => sortingOffset;
        public Color TintColor => tintColor;

        public void ConfigureHat(string newId, string newName, CosmeticRarity newRarity, Sprite preview,
            Sprite hatSprite, Vector2 offset, Vector2 scale, Color tint,
            UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            ConfigureBase(newId, newName, CosmeticCategory.Hat, newRarity, preview, unlock, itemPrice, requirement, isDefault);
            accessorySprite = hatSprite;
            localOffset = offset;
            localScale = scale;
            tintColor = tint;
            sortingOffset = 10;
        }
    }
}
