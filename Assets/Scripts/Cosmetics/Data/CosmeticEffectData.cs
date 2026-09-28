using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "Effect_", menuName = "GigaGrub/Cosmetics/Cosmetic Effect")]
    public class CosmeticEffectData : CosmeticData
    {
        [Header("Effect Configuration")]
        [SerializeField] private EffectType effectType = EffectType.Sparkles;
        [SerializeField] private Color primaryEffectColor = Color.cyan;
        [SerializeField] private Color secondaryEffectColor = Color.white;
        [SerializeField] private Sprite particleSprite;
        [SerializeField] private float emissionRate = 12f;
        [SerializeField] private float particleSize = 0.35f;
        [SerializeField] private float particleLifetime = 0.6f;
        [SerializeField] private float speed = 1.5f;

        public EffectType EffectType => effectType;
        public Color PrimaryEffectColor => primaryEffectColor;
        public Color SecondaryEffectColor => secondaryEffectColor;
        public override Sprite PreviewSprite => previewSprite != null ? previewSprite : particleSprite;
        public Sprite ParticleSprite => particleSprite;
        public float EmissionRate => emissionRate;
        public float ParticleSize => particleSize;
        public float ParticleLifetime => particleLifetime;
        public float Speed => speed;

        public void ConfigureEffect(string newId, string newName, CosmeticRarity newRarity, Sprite preview,
            EffectType type, Color primary, Color secondary, Sprite pSprite,
            float rate, float size, float lifetime,
            UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            ConfigureBase(newId, newName, CosmeticCategory.Effect, newRarity, preview, unlock, itemPrice, requirement, isDefault);
            effectType = type;
            primaryEffectColor = primary;
            secondaryEffectColor = secondary;
            particleSprite = pSprite;
            emissionRate = rate;
            particleSize = size;
            particleLifetime = lifetime;
            speed = 1.5f;
        }
    }
}
