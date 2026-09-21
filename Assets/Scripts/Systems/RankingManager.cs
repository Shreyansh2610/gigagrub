using System;
using System.Collections.Generic;
using UnityEngine;
using GigaGrub.Player;

namespace GigaGrub.Systems
{
    [Serializable]
    public struct CreatureRankEntry
    {
        public string Name;
        public int Score;
        public bool IsPlayer;
        public int Rank;

        public CreatureRankEntry(string name, int score, bool isPlayer, int rank)
        {
            Name = name;
            Score = score;
            IsPlayer = isPlayer;
            Rank = rank;
        }
    }

    public class RankingManager : MonoBehaviour
    {
        public static RankingManager Instance { get; private set; }

        [Header("Ranking Settings")]
        [Tooltip("Interval in seconds between full ranking recalculations")]
        [SerializeField] private float updateInterval = 0.5f;

        [Header("Observed Creatures")]
        [SerializeField] private PlayerBody playerBody;

        private readonly List<PlayerBody> activeCreatures = new List<PlayerBody>();
        private readonly List<CreatureRankEntry> leaderboardSnapshot = new List<CreatureRankEntry>();

        private float timer = 0f;
        private int currentRank = 1;
        private int totalCreatures = 1;

        public int CurrentRank => currentRank;
        public int TotalCreatures => totalCreatures;
        public float UpdateInterval => updateInterval;
        public IReadOnlyList<CreatureRankEntry> LeaderboardSnapshot => leaderboardSnapshot;

        public event Action<int, int> OnRankChanged;

        public static void SetInstanceForTest(RankingManager manager)
        {
            Instance = manager;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            if (playerBody == null)
            {
                playerBody = FindPlayerBody();
            }

            if (playerBody != null && !activeCreatures.Contains(playerBody))
            {
                RegisterCreature(playerBody);
            }

            RecalculateRankings();
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= updateInterval)
            {
                timer = 0f;
                RecalculateRankings();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void SetUpdateInterval(float interval)
        {
            updateInterval = Mathf.Max(0.05f, interval);
        }

        public void BindPlayer(PlayerBody player)
        {
            playerBody = player;
            if (playerBody != null && !activeCreatures.Contains(playerBody))
            {
                RegisterCreature(playerBody);
            }
            RecalculateRankings();
        }

        public void RegisterCreature(PlayerBody creature)
        {
            if (creature == null) return;

            if (!activeCreatures.Contains(creature))
            {
                activeCreatures.Add(creature);
            }

            if (creature.IsPlayer)
            {
                playerBody = creature;
            }

            RecalculateRankings();
        }

        public void UnregisterCreature(PlayerBody creature)
        {
            if (creature == null) return;

            if (activeCreatures.Remove(creature))
            {
                RecalculateRankings();
            }
        }

        public void ClearAll()
        {
            activeCreatures.Clear();
            leaderboardSnapshot.Clear();
            currentRank = 1;
            totalCreatures = 0;
            OnRankChanged?.Invoke(currentRank, totalCreatures);
        }

        public void ResetState()
        {
            timer = 0f;
            CleanupInactiveCreatures();
            RecalculateRankings();
        }

        public void RecalculateRankings()
        {
            CleanupInactiveCreatures();

            int count = activeCreatures.Count;
            if (count == 0)
            {
                currentRank = 1;
                totalCreatures = 0;
                OnRankChanged?.Invoke(currentRank, totalCreatures);
                return;
            }

            int playerScore = 0;
            bool playerFound = false;

            if (playerBody != null && activeCreatures.Contains(playerBody))
            {
                playerScore = playerBody.CurrentScore;
                playerFound = true;
            }

            int rank = 1;
            for (int i = 0; i < count; i++)
            {
                PlayerBody creature = activeCreatures[i];
                if (creature == null || creature == playerBody) continue;

                if (creature.CurrentScore > playerScore)
                {
                    rank++;
                }
            }

            int previousRank = currentRank;
            int previousTotal = totalCreatures;

            currentRank = playerFound ? rank : rank;
            totalCreatures = count;

            if (currentRank != previousRank || totalCreatures != previousTotal)
            {
                OnRankChanged?.Invoke(currentRank, totalCreatures);
            }
        }

        public List<CreatureRankEntry> GenerateFullLeaderboard()
        {
            CleanupInactiveCreatures();
            leaderboardSnapshot.Clear();

            for (int i = 0; i < activeCreatures.Count; i++)
            {
                PlayerBody creature = activeCreatures[i];
                if (creature == null) continue;

                string creatureName = creature.IsPlayer ? "Player" : creature.gameObject.name;
                leaderboardSnapshot.Add(new CreatureRankEntry(
                    creatureName,
                    creature.CurrentScore,
                    creature.IsPlayer,
                    0
                ));
            }

            leaderboardSnapshot.Sort((a, b) => b.Score.CompareTo(a.Score));

            for (int i = 0; i < leaderboardSnapshot.Count; i++)
            {
                CreatureRankEntry entry = leaderboardSnapshot[i];
                entry.Rank = i + 1;
                leaderboardSnapshot[i] = entry;
            }

            return leaderboardSnapshot;
        }

        private void CleanupInactiveCreatures()
        {
            for (int i = activeCreatures.Count - 1; i >= 0; i--)
            {
                PlayerBody creature = activeCreatures[i];
                if (creature == null || !creature.gameObject.activeInHierarchy)
                {
                    activeCreatures.RemoveAt(i);
                }
            }
        }

        private PlayerBody FindPlayerBody()
        {
            PlayerBody[] bodies = FindObjectsByType<PlayerBody>(FindObjectsSortMode.None);
            for (int i = 0; i < bodies.Length; i++)
            {
                if (bodies[i] != null && bodies[i].IsPlayer)
                {
                    return bodies[i];
                }
            }
            return null;
        }
    }
}
