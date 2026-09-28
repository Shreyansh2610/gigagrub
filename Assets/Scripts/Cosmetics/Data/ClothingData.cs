using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "Clothing_", menuName = "GigaGrub/Cosmetics/Clothing Item")]
    public class ClothingData : CosmeticData
    {
        [Header("Sprites")]
        [SerializeField] private Sprite headCollarSprite;
        [SerializeField] private Sprite segmentClothingSprite;

        [Header("Coverage & Placement")]
        [Tooltip("Number of consecutive front segments covered by clothing. Set to -1 for full length body suit.")]
        [SerializeField] private int maxSegmentsCovered = 8;
        [SerializeField] private bool tintWithSecondary = false;
        [SerializeField] private Color customColor = Color.white;
        [SerializeField] private int sortingOffset = 2;

        public override Sprite PreviewSprite => previewSprite != null ? previewSprite : (headCollarSprite != null ? headCollarSprite : segmentClothingSprite);
        public Sprite HeadCollarSprite => headCollarSprite;
        public Sprite SegmentClothingSprite => segmentClothingSprite;
        public int MaxSegmentsCovered => maxSegmentsCovered;
        public bool TintWithSecondary => tintWithSecondary;
        public Color CustomColor => customColor;
        public int SortingOffset => sortingOffset;

        public void ConfigureClothing(string newId, string newName, CosmeticRarity newRarity, Sprite preview,
            Sprite headCollar, Sprite segmentClothing, int coverage, bool tintSecondary, Color color,
            UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            ConfigureBase(newId, newName, CosmeticCategory.Clothing, newRarity, preview, unlock, itemPrice, requirement, isDefault);
            headCollarSprite = headCollar;
            segmentClothingSprite = segmentClothing;
            maxSegmentsCovered = coverage;
            tintWithSecondary = tintSecondary;
            customColor = color;
            sortingOffset = 2;
        }

        public bool ShouldCoverSegment(int segmentIndex)
        {
            if (maxSegmentsCovered < 0) return true;
            return segmentIndex < maxSegmentsCovered;
        }
    }
}
