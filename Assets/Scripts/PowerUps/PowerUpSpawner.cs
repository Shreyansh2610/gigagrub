using System.Collections.Generic;
using UnityEngine;
using GigaGrub.Core;

namespace GigaGrub.PowerUps
{
    public class PowerUpSpawner : MonoBehaviour
    {
        public static PowerUpSpawner Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Available power-up type definitions")]
        [SerializeField] private PowerUpData[] powerUpTypes;

        [Tooltip("Prefab instantiated for power-up pickups in the arena")]
        [SerializeField] private GameObject pickupPrefab;

        [Tooltip("Default fallback sprite for pickup visuals")]
        [SerializeField] private Sprite defaultPickupSprite;

        [Header("Spawn Limits & Timings")]
        [Tooltip("Maximum simultaneous power-ups allowed in the arena")]
        [SerializeField] private int maxActivePickups = 4;

        [Tooltip("Time interval in seconds between spawn attempts")]
        [SerializeField] private float spawnInterval = 7.0f;

        [Tooltip("Safe margin distance from arena walls")]
        [SerializeField] private float wallMargin = 4.0f;

        [Tooltip("Minimum distance from player head when spawning a power-up")]
        [SerializeField] private float minPlayerDistance = 4.0f;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

        private readonly Queue<PowerUpPickup> pool = new Queue<PowerUpPickup>();
        private readonly List<PowerUpPickup> activePickups = new List<PowerUpPickup>();

        private Transform poolParent;
        private float spawnTimer = 0f;
        private float totalSpawnWeight = 0f;

        public IReadOnlyList<PowerUpPickup> ActivePickups => activePickups;
        public int ActiveCount => activePickups.Count;
        public int MaxActivePickups => maxActivePickups;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            CalculateTotalWeight();
            EnsurePoolParent();
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) playerTransform = player.transform;
            }

            PrewarmPool(maxActivePickups + 2);
            InitialSpawn();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            if (activePickups.Count < maxActivePickups)
            {
                spawnTimer += Time.deltaTime;
                if (spawnTimer >= spawnInterval)
                {
                    spawnTimer = 0f;
                    SpawnSinglePowerUp();
                }
            }
        }

        public void SetPickupPrefab(GameObject prefab)
        {
            pickupPrefab = prefab;
        }

        public void SetPowerUpTypes(PowerUpData[] types)
        {
            powerUpTypes = types;
            CalculateTotalWeight();
        }

        public void SetPoolParameters(int maxPickups, float minInterval, float maxInterval)
        {
            maxActivePickups = maxPickups;
            spawnInterval = (minInterval + maxInterval) * 0.5f;
        }

        public void CalculateTotalWeight()
        {
            totalSpawnWeight = 0f;
            if (powerUpTypes == null || powerUpTypes.Length == 0) return;

            for (int i = 0; i < powerUpTypes.Length; i++)
            {
                if (powerUpTypes[i] != null)
                {
                    totalSpawnWeight += Mathf.Max(0.01f, powerUpTypes[i].SpawnWeight);
                }
            }
        }

        private void EnsurePoolParent()
        {
            if (poolParent == null)
            {
                GameObject parentGo = new GameObject("PowerUpPool");
                poolParent = parentGo.transform;
            }
        }

        public void PrewarmPool(int count)
        {
            EnsurePoolParent();
            for (int i = 0; i < count; i++)
            {
                CreatePooledPickup();
            }
        }

        private PowerUpPickup CreatePooledPickup()
        {
            GameObject go;
            if (pickupPrefab != null)
            {
                go = Instantiate(pickupPrefab, Vector3.zero, Quaternion.identity, poolParent);
            }
            else
            {
                go = new GameObject("PowerUpPickup_Pooled");
                go.transform.SetParent(poolParent);
                go.AddComponent<SpriteRenderer>();
                go.AddComponent<CircleCollider2D>();
                go.AddComponent<PowerUpPickup>();
            }

            PowerUpPickup pickup = go.GetComponent<PowerUpPickup>();
            if (pickup == null)
            {
                pickup = go.AddComponent<PowerUpPickup>();
            }

            go.SetActive(false);
            pool.Enqueue(pickup);
            return pickup;
        }

        public void InitialSpawn()
        {
            int initialCount = Mathf.Min(2, maxActivePickups);
            for (int i = 0; i < initialCount; i++)
            {
                SpawnSinglePowerUp();
            }
        }

        public PowerUpPickup SpawnSinglePowerUp()
        {
            if (powerUpTypes == null || powerUpTypes.Length == 0) return null;
            if (activePickups.Count >= maxActivePickups) return null;

            PowerUpData selectedData = GetRandomPowerUpData();
            if (selectedData == null) return null;

            Vector2 spawnPos = GenerateValidSpawnPosition();

            PowerUpPickup pickup = pool.Count > 0 ? pool.Dequeue() : CreatePooledPickup();
            pickup.Initialize(selectedData, defaultPickupSprite);
            pickup.OnSpawn(spawnPos);

            pickup.OnCollected += HandlePickupCollected;
            activePickups.Add(pickup);

            return pickup;
        }

        private PowerUpData GetRandomPowerUpData()
        {
            if (powerUpTypes == null || powerUpTypes.Length == 0) return null;

            if (totalSpawnWeight <= 0f) CalculateTotalWeight();

            float randomRoll = Random.Range(0f, totalSpawnWeight);
            float currentSum = 0f;

            for (int i = 0; i < powerUpTypes.Length; i++)
            {
                if (powerUpTypes[i] == null) continue;

                currentSum += Mathf.Max(0.01f, powerUpTypes[i].SpawnWeight);
                if (randomRoll <= currentSum)
                {
                    return powerUpTypes[i];
                }
            }

            return powerUpTypes[0];
        }

        private Vector2 GenerateValidSpawnPosition()
        {
            Vector2 arenaHalf = ArenaManager.Instance != null ? ArenaManager.Instance.HalfSize : new Vector2(48f, 48f);
            float minX = -arenaHalf.x + wallMargin;
            float maxX = arenaHalf.x - wallMargin;
            float minY = -arenaHalf.y + wallMargin;
            float maxY = arenaHalf.y - wallMargin;

            Vector2 playerPos = playerTransform != null ? (Vector2)playerTransform.position : Vector2.zero;

            for (int attempt = 0; attempt < 15; attempt++)
            {
                Vector2 candidate = new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
                if (Vector2.Distance(candidate, playerPos) >= minPlayerDistance)
                {
                    return candidate;
                }
            }

            return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        }

        private void HandlePickupCollected(PowerUpPickup pickup)
        {
            if (pickup == null) return;
            pickup.OnCollected -= HandlePickupCollected;

            activePickups.Remove(pickup);
            pool.Enqueue(pickup);
        }

        public void ClearAllActivePickups()
        {
            for (int i = activePickups.Count - 1; i >= 0; i--)
            {
                PowerUpPickup pickup = activePickups[i];
                if (pickup != null)
                {
                    pickup.OnCollected -= HandlePickupCollected;
                    pickup.OnReturnToPool();
                    pool.Enqueue(pickup);
                }
            }
            activePickups.Clear();
        }

        public void Configure(PowerUpData[] types, GameObject prefab, Sprite defaultSprite, int maxPickups = 4, float interval = 7f)
        {
            powerUpTypes = types;
            pickupPrefab = prefab;
            defaultPickupSprite = defaultSprite;
            maxActivePickups = Mathf.Max(1, maxPickups);
            spawnInterval = Mathf.Max(1f, interval);
            CalculateTotalWeight();
        }
    }
}
