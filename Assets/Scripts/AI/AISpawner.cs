using System.Collections.Generic;
using UnityEngine;
using GigaGrub.Core;
using GigaGrub.Player;
using GigaGrub.Systems;

namespace GigaGrub.AI
{
    public class AISpawner : MonoBehaviour
    {
        public static AISpawner Instance { get; private set; }

        [Header("Spawn Settings")]
        [Tooltip("Prefab for AI Creature containing AIController, AIStateMachine, AIWorldDetector, and PlayerBody")]
        [SerializeField] private GameObject aiCreaturePrefab;

        [Tooltip("Number of AI creatures to maintain in the arena")]
        [SerializeField] private int targetAICount = 10;

        [Tooltip("Minimum distance from player spawn point when spawning AI")]
        [SerializeField] private float minSpawnDistance = 12f;

        [Tooltip("Parent transform for spawned AI creatures to keep hierarchy clean")]
        [SerializeField] private Transform aiParent;

        private readonly List<AIController> activeAICreatures = new List<AIController>();

        // Distinct vibrant color palettes for AI creatures
        private static readonly Color[] HeadColors = new Color[]
        {
            new Color(1.0f, 0.35f, 0.35f, 1.0f), // Crimson
            new Color(0.35f, 0.75f, 1.0f, 1.0f), // Sky Blue
            new Color(1.0f, 0.65f, 0.15f, 1.0f), // Neon Orange
            new Color(0.85f, 0.35f, 1.0f, 1.0f), // Purple
            new Color(0.2f, 0.95f, 0.85f, 1.0f), // Cyan Teal
            new Color(1.0f, 0.95f, 0.25f, 1.0f), // Electric Yellow
            new Color(1.0f, 0.45f, 0.75f, 1.0f), // Coral Pink
            new Color(0.45f, 0.95f, 0.45f, 1.0f), // Lime
            new Color(0.55f, 0.45f, 1.0f, 1.0f), // Lavender
            new Color(1.0f, 0.5f, 0.3f, 1.0f),  // Amber Tangerine
        };

        public int ActiveAICount => activeAICreatures.Count;
        public IReadOnlyList<AIController> ActiveAICreatures => activeAICreatures;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (aiParent == null)
            {
                GameObject parentGo = new GameObject("AICreatures");
                aiParent = parentGo.transform;
            }
        }

        private void Start()
        {
            SpawnAICreatures(targetAICount);
        }

        public void SetAIPrefab(GameObject prefab)
        {
            aiCreaturePrefab = prefab;
        }

        public void SetTargetAICount(int count)
        {
            targetAICount = Mathf.Max(0, count);
        }

        public void SpawnAICreatures(int count)
        {
            if (aiCreaturePrefab == null) return;

            for (int i = 0; i < count; i++)
            {
                SpawnSingleAI(i);
            }
        }

        public AIController SpawnSingleAI(int index = 0)
        {
            if (aiCreaturePrefab == null) return null;

            Vector2 spawnPos = GetSafeSpawnPosition();
            float startAngle = Random.Range(0f, 360f);
            Quaternion startRot = Quaternion.Euler(0f, 0f, startAngle);

            GameObject go = Instantiate(aiCreaturePrefab, spawnPos, startRot, aiParent);
            go.name = $"AICreature_{index + 1}";
            go.tag = "AICreature";

            AIController ai = go.GetComponent<AIController>();
            PlayerBody body = go.GetComponent<PlayerBody>();

            if (body != null)
            {
                Color themeColor = HeadColors[index % HeadColors.Length];
                Color bodyColor = new Color(themeColor.r * 0.85f, themeColor.g * 0.85f, themeColor.b * 0.85f, 1f);
                body.SetColor(themeColor, bodyColor);
                body.SetIsPlayer(false);
                body.InitializeRuntime();

                if (RankingManager.Instance != null)
                {
                    RankingManager.Instance.RegisterCreature(body);
                }
            }

            if (ai != null)
            {
                // Varied movement speeds for dynamic creature personality
                float randomSpeed = Random.Range(4.2f, 5.4f);
                ai.SetSpeed(randomSpeed);
                activeAICreatures.Add(ai);
            }

            return ai;
        }

        public Vector2 GetSafeSpawnPosition()
        {
            Vector2 arenaHalf = Vector2.one * 40f;
            if (ArenaManager.Instance != null)
            {
                arenaHalf = ArenaManager.Instance.HalfSize * 0.8f;
            }

            for (int attempt = 0; attempt < 15; attempt++)
            {
                Vector2 candidate = new Vector2(
                    Random.Range(-arenaHalf.x, arenaHalf.x),
                    Random.Range(-arenaHalf.y, arenaHalf.y)
                );

                // Check distance from origin (player start)
                if (candidate.magnitude >= minSpawnDistance)
                {
                    return candidate;
                }
            }

            return new Vector2(Random.Range(-arenaHalf.x, arenaHalf.x), Random.Range(-arenaHalf.y, arenaHalf.y));
        }

        public void ClearAllAICreatures()
        {
            for (int i = activeAICreatures.Count - 1; i >= 0; i--)
            {
                if (activeAICreatures[i] != null)
                {
                    PlayerBody body = activeAICreatures[i].CreatureBody;
                    if (body != null && RankingManager.Instance != null)
                    {
                        RankingManager.Instance.UnregisterCreature(body);
                    }

                    if (Application.isPlaying)
                    {
                        Destroy(activeAICreatures[i].gameObject);
                    }
                    else
                    {
                        DestroyImmediate(activeAICreatures[i].gameObject);
                    }
                }
            }
            activeAICreatures.Clear();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
