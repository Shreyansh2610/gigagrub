using UnityEngine;
using UnityEngine.UI;
using GigaGrub.PowerUps;

namespace GigaGrub.UI
{
    public class PowerUpIndicatorItemUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Image fillRingImage;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Text titleText;
        [SerializeField] private Text timerText;
        [SerializeField] private CanvasGroup canvasGroup;

        private ActivePowerUp activePowerUp;

        public ActivePowerUp BoundPowerUp => activePowerUp;
        public PowerUpType Type => activePowerUp != null ? activePowerUp.Type : PowerUpType.SpeedBoost;

        private void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }
        }

        private void Update()
        {
            if (activePowerUp == null) return;

            float progress = activePowerUp.NormalizedProgress;

            if (fillRingImage != null)
            {
                fillRingImage.fillAmount = progress;
            }

            if (timerText != null)
            {
                float rem = activePowerUp.RemainingTime;
                timerText.text = rem >= 10f ? $"{rem:F0}s" : $"{rem:F1}s";
            }

            // Low time blink animation when < 2.0s
            if (activePowerUp.RemainingTime < 2.0f && canvasGroup != null)
            {
                float blink = 0.65f + Mathf.Sin(Time.unscaledTime * 14f) * 0.35f;
                canvasGroup.alpha = blink;
            }
            else if (canvasGroup != null && canvasGroup.alpha != 1f)
            {
                canvasGroup.alpha = 1f;
            }
        }

        public void Bind(ActivePowerUp powerUp)
        {
            activePowerUp = powerUp;
            if (activePowerUp == null) return;

            if (iconImage != null)
            {
                if (activePowerUp.IconSprite != null)
                {
                    iconImage.sprite = activePowerUp.IconSprite;
                }
                iconImage.color = activePowerUp.ThemeColor;
            }

            if (fillRingImage != null)
            {
                fillRingImage.color = activePowerUp.ThemeColor;
                fillRingImage.fillAmount = activePowerUp.NormalizedProgress;
            }

            if (titleText != null)
            {
                titleText.text = activePowerUp.Name;
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            gameObject.SetActive(true);
        }

        public void Unbind()
        {
            activePowerUp = null;
            gameObject.SetActive(false);
        }
    }
}
