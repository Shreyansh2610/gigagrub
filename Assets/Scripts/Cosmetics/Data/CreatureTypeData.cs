using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "CreatureType_", menuName = "GigaGrub/Cosmetics/Creature Type")]
    public class CreatureTypeData : CosmeticData
    {
        [Header("Sprites & Shapes")]
        [SerializeField] private Sprite headSprite;
        [SerializeField] private Sprite segmentSprite;
        [SerializeField] private Sprite eyeDefaultSprite;
        [SerializeField] private Sprite mouthDefaultSprite;

        [Header("Default Palette")]
        [SerializeField] private Color defaultPrimaryColor = Color.white;
        [SerializeField] private Color defaultSecondaryColor = Color.white;
        [SerializeField] private Color defaultAccentColor = Color.white;

        [Header("Scale & Spacing Modifiers")]
        [SerializeField] private Vector3 headScale = Vector3.one;
        [SerializeField] private Vector3 segmentScale = Vector3.one;
        [SerializeField] private float segmentSpacing = 0.45f;
        [SerializeField] private float minTailScale = 0.65f;

        public override Sprite PreviewSprite => previewSprite != null ? previewSprite : headSprite;
        public Sprite HeadSprite => headSprite;
        public Sprite SegmentSprite => segmentSprite;
        public Sprite EyeDefaultSprite => eyeDefaultSprite;
        public Sprite MouthDefaultSprite => mouthDefaultSprite;
        public Color DefaultPrimaryColor => defaultPrimaryColor;
        public Color DefaultSecondaryColor => defaultSecondaryColor;
        public Color DefaultAccentColor => defaultAccentColor;
        public Vector3 HeadScale => headScale;
        public Vector3 SegmentScale => segmentScale;
        public float SegmentSpacing => segmentSpacing;
        public float MinTailScale => minTailScale;

        public void ConfigureCreature(string newId, string newName, CosmeticRarity newRarity, Sprite preview,
            Sprite head, Sprite segment, Sprite eyes, Sprite mouth,
            Color primary, Color secondary, Color accent,
            UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            ConfigureBase(newId, newName, CosmeticCategory.Creature, newRarity, preview, unlock, itemPrice, requirement, isDefault);
            headSprite = head;
            segmentSprite = segment;
            eyeDefaultSprite = eyes;
            mouthDefaultSprite = mouth;
            defaultPrimaryColor = primary;
            defaultSecondaryColor = secondary;
            defaultAccentColor = accent;
            headScale = Vector3.one;
            segmentScale = Vector3.one;
            segmentSpacing = 0.45f;
            minTailScale = 0.65f;
        }
    }
}
