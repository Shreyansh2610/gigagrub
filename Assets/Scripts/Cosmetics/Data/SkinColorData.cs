using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "SkinColor_", menuName = "GigaGrub/Cosmetics/Skin Color")]
    public class SkinColorData : CosmeticData
    {
        [Header("Color Palette")]
        [SerializeField] private Color primaryColor = Color.white;
        [SerializeField] private Color secondaryColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] private Color accentColor = Color.cyan;

        [Header("Gradient / Special Styling")]
        [SerializeField] private bool isGradient = false;
        [SerializeField] private Color[] gradientColors = new Color[0];

        public Color PrimaryColor => primaryColor;
        public Color SecondaryColor => secondaryColor;
        public Color AccentColor => accentColor;
        public bool IsGradient => isGradient;
        public Color[] GradientColors => gradientColors;

        public void ConfigureColor(string newId, string newName, CosmeticRarity newRarity, Sprite preview,
            Color primary, Color secondary, Color accent,
            bool gradient, Color[] gradients,
            UnlockType unlock, int itemPrice, string requirement, bool isDefault)
        {
            ConfigureBase(newId, newName, CosmeticCategory.Color, newRarity, preview, unlock, itemPrice, requirement, isDefault);
            primaryColor = primary;
            secondaryColor = secondary;
            accentColor = accent;
            isGradient = gradient;
            gradientColors = gradients ?? new Color[0];
        }

        public Color EvaluateSegmentColor(float normalizedT)
        {
            if (!isGradient || gradientColors == null || gradientColors.Length == 0)
            {
                return normalizedT < 0.5f ? primaryColor : secondaryColor;
            }

            if (gradientColors.Length == 1) return gradientColors[0];

            float t = Mathf.Clamp01(normalizedT);
            float scaledT = t * (gradientColors.Length - 1);
            int idx = Mathf.FloorToInt(scaledT);
            int nextIdx = Mathf.Min(idx + 1, gradientColors.Length - 1);
            float frac = scaledT - idx;

            return Color.Lerp(gradientColors[idx], gradientColors[nextIdx], frac);
        }

        public Color EvaluateColorAtSegment(int segmentIndex, int totalSegments)
        {
            if (!isGradient || gradientColors == null || gradientColors.Length <= 1 || totalSegments <= 1)
            {
                return segmentIndex % 2 == 0 ? primaryColor : secondaryColor;
            }

            float t = Mathf.Clamp01((float)segmentIndex / Mathf.Max(1, totalSegments - 1));
            return EvaluateSegmentColor(t);
        }
    }
}
