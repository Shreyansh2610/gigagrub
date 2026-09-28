using System;
using UnityEngine;

namespace GigaGrub.Player
{
    public class BoostSystem : MonoBehaviour
    {
        [Header("Speed Settings")]
        [Tooltip("Standard cruising speed in units per second")]
        [SerializeField] private float normalSpeed = 5.0f;

        [Tooltip("Accelerated boost speed in units per second")]
        [SerializeField] private float boostSpeed = 9.5f;

        [Header("Energy Settings")]
        [Tooltip("Maximum energy capacity for speed boosting")]
        [SerializeField] private float maxEnergy = 100.0f;

        [Tooltip("Energy consumed per second while boosting")]
        [SerializeField] private float consumptionRate = 35.0f;

        [Tooltip("Energy regenerated per second when boost is inactive")]
        [SerializeField] private float regenerationRate = 22.0f;

        [Tooltip("Delay in seconds after releasing boost before regeneration starts")]
        [SerializeField] private float regenerationDelay = 0.4f;

        [Tooltip("Minimum energy threshold required to start boosting from idle")]
        [SerializeField] private float minEnergyToStartBoost = 5.0f;

        private float currentEnergy;
        private float regenTimer;
        private bool wantsToBoost;
        private bool isBoosting;

        public float NormalSpeed => normalSpeed;
        public float BoostSpeed => boostSpeed;
        public float MaxEnergy => maxEnergy;
        public float CurrentEnergy => currentEnergy;
        public float ConsumptionRate => consumptionRate;
        public float RegenerationRate => regenerationRate;
        public float RegenerationDelay => regenerationDelay;
        public float MinEnergyToStartBoost => minEnergyToStartBoost;

        public bool IsBoosting => isBoosting;
        public bool WantsToBoost => wantsToBoost;
        public float EnergyNormalized => maxEnergy > 0f ? Mathf.Clamp01(currentEnergy / maxEnergy) : 0f;
        public float TargetSpeed => isBoosting ? boostSpeed : normalSpeed;

        public event Action<float, float> OnEnergyChanged;
        public event Action<bool> OnBoostStateChanged;

        private void Awake()
        {
            currentEnergy = maxEnergy;
            regenTimer = regenerationDelay;
        }

        private void Update()
        {
            UpdateEnergy(Time.deltaTime);
        }

        public void UpdateEnergy(float deltaTime)
        {
            bool previousBoosting = isBoosting;

            if (wantsToBoost && currentEnergy > 0f)
            {
                // If not currently boosting, require minimum energy threshold to avoid fluttering
                if (!isBoosting && currentEnergy < minEnergyToStartBoost)
                {
                    isBoosting = false;
                }
                else
                {
                    isBoosting = true;
                    currentEnergy = Mathf.Max(0f, currentEnergy - consumptionRate * deltaTime);
                    regenTimer = 0f;

                    // Automatically cancel boost when energy is fully exhausted
                    if (currentEnergy <= 0f)
                    {
                        isBoosting = false;
                    }
                }
            }
            else
            {
                isBoosting = false;
                regenTimer += deltaTime;

                if (regenTimer >= regenerationDelay && currentEnergy < maxEnergy)
                {
                    currentEnergy = Mathf.Min(maxEnergy, currentEnergy + regenerationRate * deltaTime);
                }
            }

            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

            if (previousBoosting != isBoosting)
            {
                OnBoostStateChanged?.Invoke(isBoosting);
            }
        }

        public void SetBoostIntent(bool boost)
        {
            wantsToBoost = boost;
        }

        public void ResetEnergy()
        {
            currentEnergy = maxEnergy;
            regenTimer = regenerationDelay;
            isBoosting = false;
            wantsToBoost = false;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            OnBoostStateChanged?.Invoke(false);
        }

        public void Configure(float normal, float boost, float maxE, float consumeRate, float regenRate, float regenDel = 0.4f, float minEnergy = 5.0f)
        {
            normalSpeed = Mathf.Max(1.0f, normal);
            boostSpeed = Mathf.Max(normalSpeed, boost);
            maxEnergy = Mathf.Max(1.0f, maxE);
            consumptionRate = Mathf.Max(0.1f, consumeRate);
            regenerationRate = Mathf.Max(0.1f, regenRate);
            regenerationDelay = Mathf.Max(0f, regenDel);
            minEnergyToStartBoost = Mathf.Clamp(minEnergy, 0f, maxEnergy);

            currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        public void SetCurrentEnergy(float energy)
        {
            currentEnergy = Mathf.Clamp(energy, 0f, maxEnergy);
            if (currentEnergy <= 0f && isBoosting)
            {
                isBoosting = false;
                OnBoostStateChanged?.Invoke(false);
            }
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }
    }
}
