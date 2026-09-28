using System.Collections.Generic;
using UnityEngine;
using GigaGrub.PowerUps;

namespace GigaGrub.UI
{
    public class ActivePowerUpHUD : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Prefab instantiated for each active power-up badge")]
        [SerializeField] private GameObject indicatorPrefab;

        [Tooltip("Parent transform where indicator items are placed")]
        [SerializeField] private Transform indicatorsContainer;

        private PowerUpManager boundManager;
        private readonly Dictionary<PowerUpType, PowerUpIndicatorItemUI> activeItems = new Dictionary<PowerUpType, PowerUpIndicatorItemUI>();
        private readonly Queue<PowerUpIndicatorItemUI> itemPool = new Queue<PowerUpIndicatorItemUI>();

        private void Awake()
        {
            if (indicatorsContainer == null)
            {
                indicatorsContainer = transform;
            }
        }

        private void OnDestroy()
        {
            if (boundManager != null)
            {
                boundManager.OnPowerUpActivated -= HandlePowerUpActivated;
                boundManager.OnPowerUpUpdated -= HandlePowerUpUpdated;
                boundManager.OnPowerUpExpired -= HandlePowerUpExpired;
            }
        }

        public void BindManager(PowerUpManager manager)
        {
            if (boundManager != null)
            {
                boundManager.OnPowerUpActivated -= HandlePowerUpActivated;
                boundManager.OnPowerUpUpdated -= HandlePowerUpUpdated;
                boundManager.OnPowerUpExpired -= HandlePowerUpExpired;
            }

            ClearAllIndicators();
            boundManager = manager;

            if (boundManager != null)
            {
                boundManager.OnPowerUpActivated += HandlePowerUpActivated;
                boundManager.OnPowerUpUpdated += HandlePowerUpUpdated;
                boundManager.OnPowerUpExpired += HandlePowerUpExpired;

                foreach (var kvp in boundManager.ActivePowerUps)
                {
                    HandlePowerUpActivated(kvp.Value);
                }
            }
        }

        private void HandlePowerUpActivated(ActivePowerUp powerUp)
        {
            if (powerUp == null) return;

            if (activeItems.TryGetValue(powerUp.Type, out PowerUpIndicatorItemUI existing))
            {
                existing.Bind(powerUp);
            }
            else
            {
                PowerUpIndicatorItemUI item = GetOrCreateIndicatorItem();
                item.Bind(powerUp);
                activeItems.Add(powerUp.Type, item);
            }
        }

        private void HandlePowerUpUpdated(ActivePowerUp powerUp)
        {
            if (powerUp == null) return;

            if (activeItems.TryGetValue(powerUp.Type, out PowerUpIndicatorItemUI item))
            {
                item.Bind(powerUp);
            }
        }

        private void HandlePowerUpExpired(ActivePowerUp powerUp)
        {
            if (powerUp == null) return;

            if (activeItems.TryGetValue(powerUp.Type, out PowerUpIndicatorItemUI item))
            {
                activeItems.Remove(powerUp.Type);
                item.Unbind();
                itemPool.Enqueue(item);
            }
        }

        private PowerUpIndicatorItemUI GetOrCreateIndicatorItem()
        {
            if (itemPool.Count > 0)
            {
                return itemPool.Dequeue();
            }

            GameObject go;
            if (indicatorPrefab != null)
            {
                go = Instantiate(indicatorPrefab, indicatorsContainer);
            }
            else
            {
                go = new GameObject("PowerUpIndicator_Pooled");
                go.transform.SetParent(indicatorsContainer, false);
                go.AddComponent<RectTransform>();
                go.AddComponent<PowerUpIndicatorItemUI>();
            }

            PowerUpIndicatorItemUI item = go.GetComponent<PowerUpIndicatorItemUI>();
            if (item == null)
            {
                item = go.AddComponent<PowerUpIndicatorItemUI>();
            }

            return item;
        }

        public void ClearAllIndicators()
        {
            foreach (var kvp in activeItems)
            {
                kvp.Value.Unbind();
                itemPool.Enqueue(kvp.Value);
            }
            activeItems.Clear();
        }
    }
}
