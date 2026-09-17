using UnityEngine;

namespace GigaGrub.Food
{
    public enum FoodType
    {
        Standard,
        Super,
        Mega
    }

    [CreateAssetMenu(fileName = "FoodData_", menuName = "GigaGrub/Food Data", order = 1)]
    public class FoodData : ScriptableObject
    {
        [Header("Identification")]
        [Tooltip("Type category of the food")]
        [SerializeField] private FoodType foodType = FoodType.Standard;

        [Tooltip("Display name of the food item")]
        [SerializeField] private string foodName = "Standard Grub";

        [Header("Gameplay Values")]
        [Tooltip("Score awarded to the player when eaten")]
        [SerializeField] private int scoreValue = 10;

        [Tooltip("Number of body segments added to the player creature")]
        [SerializeField] private int growthValue = 1;

        [Tooltip("Relative probability weight when spawning")]
        [SerializeField] private float spawnWeight = 100f;

        [Header("Visual Properties")]
        [Tooltip("Color tint applied to the food sprite")]
        [SerializeField] private Color foodColor = new Color(0.2f, 0.9f, 0.3f, 1f);

        [Tooltip("Scale multiplier for the food visual")]
        [SerializeField] private float scaleMultiplier = 1f;

        [Tooltip("Optional custom sprite for this food type. If null, uses default spawner sprite.")]
        [SerializeField] private Sprite foodSprite;

        [Header("Animation Settings")]
        [Tooltip("Whether to apply subtle pulsation animation")]
        [SerializeField] private bool enablePulse = true;

        [Tooltip("Speed of pulsation")]
        [SerializeField] private float pulseSpeed = 3f;

        [Tooltip("Amplitude / magnitude of pulsation")]
        [SerializeField] private float pulseMagnitude = 0.1f;

        public FoodType FoodType => foodType;
        public string FoodName => foodName;
        public int ScoreValue => scoreValue;
        public int GrowthValue => growthValue;
        public float SpawnWeight => spawnWeight;
        public Color FoodColor => foodColor;
        public float ScaleMultiplier => scaleMultiplier;
        public Sprite FoodSprite => foodSprite;
        public bool EnablePulse => enablePulse;
        public float PulseSpeed => pulseSpeed;
        public float PulseMagnitude => pulseMagnitude;

        public void Configure(
            FoodType type,
            string name,
            int score,
            int growth,
            float weight,
            Color color,
            float scale = 1f,
            Sprite sprite = null,
            bool pulse = true,
            float pSpeed = 3f,
            float pMag = 0.1f)
        {
            foodType = type;
            foodName = name;
            scoreValue = score;
            growthValue = growth;
            spawnWeight = weight;
            foodColor = color;
            scaleMultiplier = scale;
            foodSprite = sprite;
            enablePulse = pulse;
            pulseSpeed = pSpeed;
            pulseMagnitude = pMag;
        }
    }
}
