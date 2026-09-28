using UnityEngine;

namespace GigaGrub.Data
{
    [CreateAssetMenu(fileName = "CreatureSkin_", menuName = "GigaGrub/Creature Skin Data")]
    public class CreatureSkinData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique string identifier for this creature skin")]
        [SerializeField] private string skinId = "skin_player_default";

        [Tooltip("Display name of the creature skin or bot personality")]
        [SerializeField] private string skinName = "Giga Grub";

        [Header("Sprites")]
        [Tooltip("Custom head sprite with unique facial features and antennae")]
        [SerializeField] private Sprite headSprite;

        [Tooltip("Custom body segment sprite with matching pattern")]
        [SerializeField] private Sprite segmentSprite;

        [Header("Color Palette")]
        [Tooltip("Primary signature theme color for head and glow")]
        [SerializeField] private Color primaryColor = new Color(0.12f, 0.92f, 0.72f, 1f);

        [Tooltip("Secondary color for body segments and shading")]
        [SerializeField] private Color secondaryColor = new Color(0.08f, 0.72f, 0.55f, 1f);

        [Tooltip("Accent color for eye glow, cheek blush, and antennae tips")]
        [SerializeField] private Color accentColor = new Color(0.45f, 1f, 0.85f, 1f);

        public string SkinId => skinId;
        public string SkinName => skinName;
        public Sprite HeadSprite => headSprite;
        public Sprite SegmentSprite => segmentSprite;
        public Color PrimaryColor => primaryColor;
        public Color SecondaryColor => secondaryColor;
        public Color AccentColor => accentColor;

        public void Configure(string id, string name, Sprite head, Sprite segment, Color primary, Color secondary, Color accent)
        {
            skinId = id;
            skinName = name;
            headSprite = head;
            segmentSprite = segment;
            primaryColor = primary;
            secondaryColor = secondary;
            accentColor = accent;
        }
    }
}
