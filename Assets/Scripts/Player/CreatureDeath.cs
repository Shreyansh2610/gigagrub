using System;
using UnityEngine;
using GigaGrub.AI;
using GigaGrub.Audio;
using GigaGrub.Core;
using GigaGrub.Food;

namespace GigaGrub.Player
{
    public class CreatureDeath : MonoBehaviour
    {
        [Header("Death Settings")]
        [Tooltip("Whether to drop collectible food items along the body upon death")]
        [SerializeField] private bool dropFoodOnDeath = true;

        [Tooltip("Proportion of body segments converted into food (0.0 to 1.0)")]
        [Range(0f, 1f)]
        [SerializeField] private float foodDropRatio = 0.75f;

        [Tooltip("Optional custom food data to drop upon death")]
        [SerializeField] private FoodData overrideDropFood;

        [Header("References")]
        [SerializeField] private PlayerBody creatureBody;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private AIController aiController;
        [SerializeField] private Collider2D headCollider;
        [SerializeField] private AudioSource audioSource;

        private bool isDead = false;

        public bool IsDead => isDead;
        public PlayerBody CreatureBody => creatureBody;

        public event Action<CreatureDeath, DeathReason> OnCreatureDied;

        private void Awake()
        {
            if (creatureBody == null)
            {
                creatureBody = GetComponent<PlayerBody>();
            }

            if (playerController == null)
            {
                playerController = GetComponent<PlayerController>();
            }

            if (aiController == null)
            {
                aiController = GetComponent<AIController>();
            }

            if (headCollider == null)
            {
                headCollider = GetComponent<Collider2D>();
            }

            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        public void Die(DeathReason reason)
        {
            // Atomic double-death prevention
            if (isDead) return;
            isDead = true;

            bool wasPlayer = creatureBody != null && creatureBody.IsPlayer;

            // 1. Disable colliders and controllers immediately
            if (headCollider != null)
            {
                headCollider.enabled = false;
            }

            if (playerController != null)
            {
                playerController.enabled = false;
            }

            if (aiController != null)
            {
                aiController.enabled = false;
            }

            // 2. Convert body segments into collectible food
            if (dropFoodOnDeath && FoodSpawner.Instance != null && creatureBody != null)
            {
                ConvertBodyToFood();
            }

            // 3. Clear remaining body segments back to pool
            if (creatureBody != null)
            {
                creatureBody.ClearAllSegmentsToPool();
            }

            // 4. Visual death explosion effect
            Vector3 headPos = transform.position;
            EatingEffect.Spawn(headPos, new Color(1f, 0.2f, 0.2f, 1f), 2.2f);

            // 5. Audio feedback
            if (audioSource != null && wasPlayer)
            {
                SoundEffectGenerator.PlayDeathSound(audioSource, 0.8f);
            }

            // 6. Notify listeners and GameManager
            OnCreatureDied?.Invoke(this, reason);

            if (GameManager.Instance != null)
            {
                if (wasPlayer)
                {
                    GameManager.Instance.HandlePlayerDeath(reason);
                }
                else
                {
                    GameManager.Instance.RecordAIDefeated();
                }
            }

            // 7. Clean deactivation
            gameObject.SetActive(false);
        }

        private void ConvertBodyToFood()
        {
            var segments = creatureBody.ActiveSegments;
            if (segments == null || segments.Count == 0) return;

            int totalSegments = segments.Count;
            int foodsToDrop = Mathf.Max(1, Mathf.RoundToInt(totalSegments * foodDropRatio));
            int step = Mathf.Max(1, totalSegments / foodsToDrop);

            for (int i = 0; i < totalSegments; i += step)
            {
                if (i < segments.Count && segments[i] != null)
                {
                    Vector2 segPos = segments[i].transform.position;
                    // Add slight scatter offset
                    Vector2 scatter = UnityEngine.Random.insideUnitCircle * 0.25f;
                    FoodSpawner.Instance.SpawnFoodAt(segPos + scatter, overrideDropFood);
                }
            }
        }

        public void ResetState()
        {
            isDead = false;
            gameObject.SetActive(true);

            if (headCollider != null)
            {
                headCollider.enabled = true;
            }

            if (playerController != null)
            {
                playerController.enabled = true;
            }

            if (aiController != null)
            {
                aiController.enabled = true;
            }
        }
    }
}
