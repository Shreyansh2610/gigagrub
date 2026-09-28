using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "Pattern_", menuName = "GigaGrub/Cosmetics/Body Pattern")]
    public class PatternData : CosmeticData
    {
        [Header("Pattern Configuration")]
        [SerializeField] private PatternType patternType = PatternType.Solid;
        [SerializeField] private Sprite patternSprite;
        [SerializeField] private float glowIntensity = 1.0f;
        [SerializeField] private bool useSecondaryColor = true;
        [SerializeField] private bool alternateSegments = true;

        public PatternType PatternType => patternType;
        public Sprite PatternSprite => patternSprite;
        public float GlowIntensity => glowIntensity;
        public bool UseSecondaryColor => useSecondaryColor;
        public bool AlternateSegments => alternateSegments;

        public void ConfigurePattern(string newId, string newName, CosmeticRarity newRarity, Sprite preview,
            PatternType type, Sprite sprite, float glow, bool useSecondary, bool alternate,
            UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            ConfigureBase(newId, newName, CosmeticCategory.Pattern, newRarity, preview, unlock, itemPrice, requirement, isDefault);
            patternType = type;
            patternSprite = sprite;
            glowIntensity = glow;
            useSecondaryColor = useSecondary;
            alternateSegments = alternate;
        }
    }
}
