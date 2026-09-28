using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "Face_", menuName = "GigaGrub/Cosmetics/Face Feature")]
    public class FaceData : CosmeticData
    {
        [Header("Feature Type & Placement")]
        [SerializeField] private FaceFeatureType featureType = FaceFeatureType.Eyes;
        [SerializeField] private Sprite featureSprite;
        [SerializeField] private Vector2 localOffset = Vector2.zero;
        [SerializeField] private Vector2 localScale = Vector2.one;
        [SerializeField] private int sortingOffset = 5;
        [SerializeField] private Color tintColor = Color.white;

        public FaceFeatureType FeatureType => featureType;
        public override Sprite PreviewSprite => previewSprite != null ? previewSprite : featureSprite;
        public Sprite FeatureSprite => featureSprite;
        public Vector2 LocalOffset => localOffset;
        public Vector2 LocalScale => localScale;
        public int SortingOffset => sortingOffset;
        public Color TintColor => tintColor;

        public void ConfigureFace(string newId, string newName, CosmeticCategory cat, FaceFeatureType fType,
            CosmeticRarity newRarity, Sprite preview, Sprite sprite, Vector2 offset, Vector2 scale, Color tint,
            UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            ConfigureBase(newId, newName, cat, newRarity, preview, unlock, itemPrice, requirement, isDefault);
            featureType = fType;
            featureSprite = sprite;
            localOffset = offset;
            localScale = scale;
            tintColor = tint;
            sortingOffset = 5;
        }
    }
}
