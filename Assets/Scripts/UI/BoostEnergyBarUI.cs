using UnityEngine;
using UnityEngine.UI;
using GigaGrub.Player;

namespace GigaGrub.UI
{
    public class BoostEnergyBarUI : MonoBehaviour
    {
        [Header("UI References")]
        [Tooltip("Fill image that scales with remaining boost energy")]
        [SerializeField] private Image fillImage;

        [Tooltip("Background or border image for low energy pulse feedback")]
        [SerializeField] private Image backgroundImage;

        [Tooltip("Optional text display for energy percentage or label")]
        [SerializeField] private Text energyText;

        [Header("Gradient Colors")]
        [SerializeField] private Color fullColor = new Color(0.22f, 0.74f, 0.97f, 1f);   // Neon Sky Cyan #38BDF8
        [SerializeField] private Color midColor = new Color(0.99f, 0.83f, 0.30f, 1f);    // Amber #FCD34D
        [SerializeField] private Color lowColor = new Color(0.97f, 0.44f, 0.44f, 1f);    // Coral Red #F87171

        [Header("Animation & Smoothing")]
        [SerializeField] private float smoothSpeed = 12f;
        [SerializeField] private float lowEnergyThreshold = 0.20f;
        [SerializeField] private float pulseSpeed = 8f;

        private BoostSystem boundBoostSystem;
        private float currentFill = 1f;
        private float targetFill = 1f;
        private bool isDepleted;

        public float NormalizedEnergy => targetFill;

        private void Awake()
        {
            if (fillImage != null && fillImage.type != Image.Type.Filled)
            {
                fillImage.type = Image.Type.Filled;
                fillImage.fillMethod = Image.FillMethod.Horizontal;
            }
        }

        private void Update()
        {
            if (boundBoostSystem != null)
            {
                targetFill = boundBoostSystem.EnergyNormalized;
            }

            // Smooth interpolation
            currentFill = Mathf.Lerp(currentFill, targetFill, Time.unscaledDeltaTime * smoothSpeed);

            if (fillImage != null)
            {
                fillImage.fillAmount = currentFill;
                fillImage.color = EvaluateEnergyColor(currentFill);
            }

            UpdateLowEnergyPulse();
        }

        public void BindBoostSystem(BoostSystem boostSystem)
        {
            if (boundBoostSystem != null)
            {
                boundBoostSystem.OnEnergyChanged -= HandleEnergyChanged;
            }

            boundBoostSystem = boostSystem;

            if (boundBoostSystem != null)
            {
                boundBoostSystem.OnEnergyChanged += HandleEnergyChanged;
                targetFill = boundBoostSystem.EnergyNormalized;
                currentFill = targetFill;
                UpdateDisplayInstant(targetFill);
            }
        }

        private void OnDestroy()
        {
            if (boundBoostSystem != null)
            {
                boundBoostSystem.OnEnergyChanged -= HandleEnergyChanged;
            }
        }

        private void HandleEnergyChanged(float current, float max)
        {
            targetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            isDepleted = targetFill <= 0.001f;

            if (energyText != null)
            {
                int percent = Mathf.CeilToInt(targetFill * 100f);
                energyText.text = $"{percent}%";
            }
        }

        private Color EvaluateEnergyColor(float fill)
        {
            if (fill > 0.5f)
            {
                float t = (fill - 0.5f) * 2f;
                return Color.Lerp(midColor, fullColor, t);
            }
            else
            {
                float t = fill * 2f;
                return Color.Lerp(lowColor, midColor, t);
            }
        }

        private void UpdateLowEnergyPulse()
        {
            if (targetFill < lowEnergyThreshold && fillImage != null)
            {
                float pulse = 0.75f + Mathf.Sin(Time.unscaledTime * pulseSpeed) * 0.25f;
                Color baseCol = EvaluateEnergyColor(currentFill);
                fillImage.color = new Color(baseCol.r, baseCol.g, baseCol.b, pulse);
            }
        }

        public void UpdateDisplayInstant(float normalized)
        {
            targetFill = Mathf.Clamp01(normalized);
            currentFill = targetFill;
            if (fillImage != null)
            {
                fillImage.fillAmount = currentFill;
                fillImage.color = EvaluateEnergyColor(currentFill);
            }
        }
    }
}
