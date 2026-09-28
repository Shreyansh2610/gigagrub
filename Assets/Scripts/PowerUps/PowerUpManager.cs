using System;
using System.Collections.Generic;
using UnityEngine;
using GigaGrub.Food;
using GigaGrub.Player;
using GigaGrub.Systems;

namespace GigaGrub.PowerUps
{
    public class PowerUpManager : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBody playerBody;
        [SerializeField] private PlayerController playerController;

        [Header("Magnet Physics")]
        [Tooltip("Attraction lerp speed when pulling food items toward the player head")]
        [SerializeField] private float magnetPullSpeed = 14f;

        private readonly Dictionary<PowerUpType, ActivePowerUp> activePowerUps = new Dictionary<PowerUpType, ActivePowerUp>();
        private readonly List<PowerUpType> expiredKeysBuffer = new List<PowerUpType>();

        public IReadOnlyDictionary<PowerUpType, ActivePowerUp> ActivePowerUps => activePowerUps;
        public int ActiveCount => activePowerUps.Count;

        public bool HasActivePowerUp(PowerUpType type) => activePowerUps.ContainsKey(type);
        public bool IsPowerUpActive(PowerUpType type) => HasActivePowerUp(type);

        public ActivePowerUp GetActivePowerUp(PowerUpType type)
        {
            activePowerUps.TryGetValue(type, out ActivePowerUp active);
            return active;
        }

        public float SpeedBonus
        {
            get
            {
                if (activePowerUps.TryGetValue(PowerUpType.SpeedBoost, out ActivePowerUp boost))
                {
                    return boost.EffectStrength;
                }
                return 0f;
            }
        }

        public float MagnetRadius
        {
            get
            {
                if (activePowerUps.TryGetValue(PowerUpType.FoodMagnet, out ActivePowerUp magnet))
                {
                    return magnet.EffectStrength;
                }
                return 0f;
            }
        }

        public bool IsMagnetActive => activePowerUps.ContainsKey(PowerUpType.FoodMagnet);

        public event Action<ActivePowerUp> OnPowerUpActivated;
        public event Action<ActivePowerUp> OnPowerUpUpdated;
        public event Action<ActivePowerUp> OnPowerUpExpired;

        private void Awake()
        {
            if (playerBody == null)
            {
                playerBody = GetComponent<PlayerBody>();
            }

            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }
        }

        private void Update()
        {
            UpdateActivePowerUps(Time.deltaTime);
            UpdateFoodMagnet();
        }

        public void UpdateTimers(float deltaTime) => UpdateActivePowerUps(deltaTime);

        public void UpdateActivePowerUps(float deltaTime)
        {
            expiredKeysBuffer.Clear();

            foreach (var kvp in activePowerUps)
            {
                ActivePowerUp active = kvp.Value;
                active.Tick(deltaTime);

                if (active.IsExpired)
                {
                    expiredKeysBuffer.Add(kvp.Key);
                }
            }

            for (int i = 0; i < expiredKeysBuffer.Count; i++)
            {
                PowerUpType type = expiredKeysBuffer[i];
                if (activePowerUps.TryGetValue(type, out ActivePowerUp expired))
                {
                    activePowerUps.Remove(type);
                    OnPowerUpEnd(expired);
                    OnPowerUpExpired?.Invoke(expired);
                }
            }
        }

        public void ActivatePowerUp(PowerUpData data) => ApplyPowerUp(data);

        public void ApplyPowerUp(PowerUpData data)
        {
            if (data == null) return;

            if (activePowerUps.TryGetValue(data.Type, out ActivePowerUp existing))
            {
                existing.RefreshDuration();
                OnPowerUpUpdated?.Invoke(existing);
            }
            else
            {
                ActivePowerUp newActive = new ActivePowerUp(data);
                activePowerUps.Add(data.Type, newActive);
                OnPowerUpStart(newActive);
                OnPowerUpActivated?.Invoke(newActive);
            }
        }

        private void OnPowerUpStart(ActivePowerUp active)
        {
            if (active.Type == PowerUpType.ScoreMultiplier)
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.SetMultiplier(active.EffectStrength);
                }
            }
        }

        private void OnPowerUpEnd(ActivePowerUp active)
        {
            if (active.Type == PowerUpType.ScoreMultiplier)
            {
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.SetMultiplier(1.0f);
                }
            }
        }

        private void UpdateFoodMagnet()
        {
            if (!IsMagnetActive || FoodSpawner.Instance == null) return;

            float radius = MagnetRadius;
            float radiusSqr = radius * radius;
            Vector3 headPos = transform.position;

            IReadOnlyList<Food.Food> activeFoodList = FoodSpawner.Instance.ActiveFoods;
            int count = activeFoodList.Count;

            for (int i = 0; i < count; i++)
            {
                Food.Food food = activeFoodList[i];
                if (food == null || food.IsConsumed) continue;

                Vector3 foodPos = food.transform.position;
                Vector3 toHead = headPos - foodPos;
                float sqrDist = toHead.sqrMagnitude;

                if (sqrDist <= radiusSqr && sqrDist > 0.001f)
                {
                    float dist = Mathf.Sqrt(sqrDist);
                    // Attraction accelerates as food gets closer
                    float pullFactor = 1f + (1f - Mathf.Clamp01(dist / radius));
                    Vector3 moveDir = toHead / dist;
                    food.transform.position += moveDir * (magnetPullSpeed * pullFactor * Time.deltaTime);
                }
            }
        }

        public void ClearAllPowerUps()
        {
            foreach (var kvp in activePowerUps)
            {
                OnPowerUpEnd(kvp.Value);
                OnPowerUpExpired?.Invoke(kvp.Value);
            }
            activePowerUps.Clear();
        }
    }
}
