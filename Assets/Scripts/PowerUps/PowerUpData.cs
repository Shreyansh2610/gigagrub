using UnityEngine;

namespace GigaGrub.PowerUps
{
    [CreateAssetMenu(fileName = "PowerUpData_", menuName = "GigaGrub/PowerUp Data")]
    public class PowerUpData : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Unique string identifier for this power-up")]
        [SerializeField] private string id = "powerup_speed_boost";

        [Tooltip("Display name of the power-up in UI")]
        [SerializeField] private string powerUpName = "Speed Boost";

        [Tooltip("Type category of power-up")]
        [SerializeField] private PowerUpType powerUpType = PowerUpType.SpeedBoost;

        [Header("Effect Parameters")]
        [Tooltip("Duration in seconds the power-up remains active")]
        [SerializeField] private float duration = 8.0f;

        [Tooltip("Strength multiplier or value (e.g. +4.0 speed, 8.0m magnet radius, 2.0x score)")]
        [SerializeField] private float effectStrength = 4.0f;

        [Tooltip("Relative spawn probability weight in the spawner pool")]
        [SerializeField] private float spawnWeight = 1.0f;

        [Header("Visual & Audio")]
        [Tooltip("Signature theme color used for pickup visuals, aura, and HUD indicators")]
        [SerializeField] private Color themeColor = new Color(0.22f, 0.74f, 0.97f, 1f);

        [Tooltip("Sprite icon used for the pickup and HUD badge")]
        [SerializeField] private Sprite iconSprite;

        [Tooltip("Enable subtle floating pulse animation on the pickup entity")]
        [SerializeField] private bool enablePulsing = true;

        [SerializeField] private float pulseSpeed = 4.0f;
        [SerializeField] private float pulseMagnitude = 0.12f;

        public string Id => id;
        public string PowerUpName => powerUpName;
        public PowerUpType Type => powerUpType;
        public float Duration => duration;
        public float EffectStrength => effectStrength;
        public float SpawnWeight => spawnWeight;
        public Color ThemeColor => themeColor;
        public Sprite IconSprite => iconSprite;
        public bool EnablePulsing => enablePulsing;
        public float PulseSpeed => pulseSpeed;
        public float PulseMagnitude => pulseMagnitude;

        public void Configure(string id, string name, PowerUpType type, float duration, float effectStrength, float spawnWeight, Color themeColor, Sprite icon = null, bool pulse = true, float pulseSpeed = 4f, float pulseMagnitude = 0.12f)
        {
            this.id = id;
            this.powerUpName = name;
            this.powerUpType = type;
            this.duration = Mathf.Max(0.5f, duration);
            this.effectStrength = Mathf.Max(0.1f, effectStrength);
            this.spawnWeight = Mathf.Max(0.01f, spawnWeight);
            this.themeColor = themeColor;
            this.iconSprite = icon;
            this.enablePulsing = pulse;
            this.pulseSpeed = pulseSpeed;
            this.pulseMagnitude = pulseMagnitude;
        }

        public void ConfigureRuntime(string newId, string newName, PowerUpType type, float dur, float strength, float weight, Color color, Sprite sprite = null)
        {
            id = newId;
            powerUpName = newName;
            powerUpType = type;
            duration = Mathf.Max(0.5f, dur);
            effectStrength = Mathf.Max(0.1f, strength);
            spawnWeight = Mathf.Max(0.01f, weight);
            themeColor = color;
            iconSprite = sprite;
        }
    }
}
