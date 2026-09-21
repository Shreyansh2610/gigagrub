using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GigaGrub.Core;
using GigaGrub.Player;
using GigaGrub.Systems;

namespace GigaGrub.Food
{
    public class FoodSpawner : MonoBehaviour
    {
        public static FoodSpawner Instance { get; private set; }

        [Header("Food Configuration")]
        [Tooltip("Available food data definitions with spawn weights")]
        [SerializeField] private FoodData[] foodTypes;

        [Tooltip("Food base prefab containing SpriteRenderer, CircleCollider2D, and Food component")]
        [SerializeField] private GameObject foodPrefab;

        [Tooltip("Fallback sprite to use if a FoodData does not define a custom sprite")]
        [SerializeField] private Sprite defaultFoodSprite;

        [Header("Spawn Population Limits")]
        [Tooltip("Minimum number of active food items to maintain in the arena")]
        [SerializeField] private int minFoodCount = 100;

        [Tooltip("Maximum number of active food items allowed in the arena")]
        [SerializeField] private int maxFoodCount = 150;

        [Tooltip("Initial number of food items spawned when the game starts")]
        [SerializeField] private int initialFoodCount = 120;

        [Header("Spatial Safety")]
        [Tooltip("Margin from arena boundary walls to prevent spawning inside or touching walls")]
        [SerializeField] private float wallMargin = 2.5f;

        [Tooltip("Minimum clearance distance from player head and body segments")]
        [SerializeField] private float minPlayerDistance = 3.5f;

        [Tooltip("Max attempts to find a safe spawn position before falling back to arena bounds")]
        [SerializeField] private int maxSpawnAttempts = 12;

        [Header("Respawn Settings")]
        [Tooltip("Delay in seconds before respawning food after consumption")]
        [SerializeField] private float respawnDelay = 0.5f;

        [Tooltip("Transform parent for pooled food objects to keep hierarchy tidy")]
        [SerializeField] private Transform foodParent;

        [Header("Target References")]
        [Tooltip("Reference to the player body for distance checks")]
        [SerializeField] private PlayerBody playerBody;

        private readonly Queue<Food> pool = new Queue<Food>();
        private readonly List<Food> activeFoods = new List<Food>();
        private readonly List<float> accumulatedWeights = new List<float>();
        private float totalSpawnWeight;
        private int pendingRespawns = 0;
        private SpatialGrid2D<Food> spatialGrid;

        public int ActiveFoodCount => activeFoods.Count;
        public int TotalPoolCount => pool.Count + activeFoods.Count;
        public IReadOnlyList<Food> ActiveFoods => activeFoods;
        public SpatialGrid2D<Food> SpatialGrid => spatialGrid;

        public static void SetInstanceForTest(FoodSpawner spawner)
        {
            Instance = spawner;
        }

        private void Awake()
        {
            InitializeRuntime();
        }

        public void InitializeRuntime()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            if (foodParent == null)
            {
                GameObject parentGo = new GameObject("FoodPool");
                foodParent = parentGo.transform;
            }

            Vector2 arenaHalf = Vector2.one * 55f;
            if (ArenaManager.Instance != null)
            {
                arenaHalf = ArenaManager.Instance.HalfSize + Vector2.one * 5f;
            }
            spatialGrid = new SpatialGrid2D<Food>(-arenaHalf, arenaHalf, cellSize: 10f);

            RebuildWeightTable();
            PrewarmPool(maxFoodCount + 10);
        }

        private void Start()
        {
            if (playerBody == null)
            {
                playerBody = FindAnyObjectByType<PlayerBody>();
            }

            int targetSpawn = Mathf.Clamp(initialFoodCount, minFoodCount, maxFoodCount);
            SpawnInitialPopulation(targetSpawn);
        }

        public void SetFoodTypes(FoodData[] types)
        {
            foodTypes = types;
            RebuildWeightTable();
        }

        public void SetFoodPrefab(GameObject prefab)
        {
            foodPrefab = prefab;
        }

        public void SetDefaultSprite(Sprite sprite)
        {
            defaultFoodSprite = sprite;
        }

        public void SetPopulationLimits(int min, int max, int initial)
        {
            minFoodCount = min;
            maxFoodCount = max;
            initialFoodCount = initial;
        }

        public void SetPlayerBody(PlayerBody player)
        {
            playerBody = player;
        }

        public void RebuildWeightTable()
        {
            accumulatedWeights.Clear();
            totalSpawnWeight = 0f;

            if (foodTypes == null || foodTypes.Length == 0) return;

            for (int i = 0; i < foodTypes.Length; i++)
            {
                if (foodTypes[i] != null)
                {
                    totalSpawnWeight += Mathf.Max(0f, foodTypes[i].SpawnWeight);
                }
                accumulatedWeights.Add(totalSpawnWeight);
            }
        }

        public void PrewarmPool(int count)
        {
            if (foodPrefab == null) return;

            for (int i = 0; i < count; i++)
            {
                CreatePooledFood();
            }
        }

        private Food CreatePooledFood()
        {
            if (foodPrefab == null) return null;

            GameObject go = Instantiate(foodPrefab, Vector3.zero, Quaternion.identity, foodParent);
            go.SetActive(false);

            Food food = go.GetComponent<Food>();
            if (food == null)
            {
                food = go.AddComponent<Food>();
            }

            food.OnConsumed += HandleFoodConsumed;
            pool.Enqueue(food);
            return food;
        }

        public void SpawnInitialPopulation(int count)
        {
            for (int i = 0; i < count; i++)
            {
                SpawnFood();
            }
        }

        public Food SpawnFood()
        {
            if (activeFoods.Count >= maxFoodCount) return null;

            FoodData chosenData = SelectRandomFoodData();
            if (chosenData == null) return null;

            Vector2 spawnPos = GetSafeSpawnPosition();
            return SpawnFoodAt(spawnPos, chosenData);
        }

        public Food SpawnFoodAt(Vector2 position, FoodData overrideData = null)
        {
            FoodData chosenData = overrideData != null ? overrideData : SelectRandomFoodData();
            if (chosenData == null) return null;

            if (pool.Count == 0)
            {
                CreatePooledFood();
            }

            Food food = pool.Dequeue();
            food.Initialize(chosenData, defaultFoodSprite);
            food.OnSpawn(position);

            activeFoods.Add(food);
            if (spatialGrid != null)
            {
                spatialGrid.Insert(food, position);
            }
            return food;
        }

        public FoodData SelectRandomFoodData()
        {
            if (foodTypes == null || foodTypes.Length == 0) return null;
            if (totalSpawnWeight <= 0f) return foodTypes[0];

            float randomVal = Random.Range(0f, totalSpawnWeight);

            for (int i = 0; i < foodTypes.Length; i++)
            {
                if (randomVal <= accumulatedWeights[i] && foodTypes[i] != null)
                {
                    return foodTypes[i];
                }
            }

            return foodTypes[0];
        }

        public Vector2 GetSafeSpawnPosition()
        {
            Vector2 arenaHalf = Vector2.one * 45f;
            if (ArenaManager.Instance != null)
            {
                arenaHalf = ArenaManager.Instance.HalfSize;
            }

            float minX = -arenaHalf.x + wallMargin;
            float maxX = arenaHalf.x - wallMargin;
            float minY = -arenaHalf.y + wallMargin;
            float maxY = arenaHalf.y - wallMargin;

            // Ensure bounds are valid
            if (minX > maxX) { minX = -10f; maxX = 10f; }
            if (minY > maxY) { minY = -10f; maxY = 10f; }

            float minSqrDist = minPlayerDistance * minPlayerDistance;

            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                Vector2 candidate = new Vector2(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY)
                );

                if (IsPositionSafeFromPlayer(candidate, minSqrDist))
                {
                    return candidate;
                }
            }

            // Fallback candidate
            return new Vector2(Random.Range(minX, maxX), Random.Range(minY, maxY));
        }

        private bool IsPositionSafeFromPlayer(Vector2 position, float minSqrDistance)
        {
            if (playerBody == null)
            {
                playerBody = FindAnyObjectByType<PlayerBody>();
                if (playerBody == null) return true;
            }

            // Check distance from player head
            Vector2 headPos = playerBody.transform.position;
            if ((position - headPos).sqrMagnitude < minSqrDistance)
            {
                return false;
            }

            // Check distance from player active body segments
            var segments = playerBody.ActiveSegments;
            if (segments != null)
            {
                int segmentCount = segments.Count;
                for (int i = 0; i < segmentCount; i++)
                {
                    if (segments[i] != null)
                    {
                        Vector2 segPos = segments[i].transform.position;
                        if ((position - segPos).sqrMagnitude < minSqrDistance)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private void HandleFoodConsumed(Food food)
        {
            if (activeFoods.Remove(food))
            {
                if (spatialGrid != null)
                {
                    spatialGrid.Remove(food, food.transform.position);
                }
                pool.Enqueue(food);

                // Queue respawn if population is below minimum or target
                if (activeFoods.Count + pendingRespawns < minFoodCount)
                {
                    ScheduleRespawn();
                }
                else if (activeFoods.Count + pendingRespawns < initialFoodCount)
                {
                    ScheduleRespawn();
                }
            }
        }

        private void ScheduleRespawn()
        {
            pendingRespawns++;
            if (respawnDelay <= 0.001f)
            {
                pendingRespawns--;
                SpawnFood();
            }
            else
            {
                StartCoroutine(RespawnRoutine());
            }
        }

        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(respawnDelay);
            pendingRespawns--;

            if (activeFoods.Count < maxFoodCount)
            {
                SpawnFood();
            }
        }

        public void ClearAllActiveFood()
        {
            if (spatialGrid != null)
            {
                spatialGrid.Clear();
            }

            for (int i = activeFoods.Count - 1; i >= 0; i--)
            {
                Food food = activeFoods[i];
                food.OnReturnToPool();
                pool.Enqueue(food);
            }
            activeFoods.Clear();
            pendingRespawns = 0;
        }

        private void OnDestroy()
        {
            if (foodParent != null)
            {
                Destroy(foodParent.gameObject);
            }
        }
    }
}
