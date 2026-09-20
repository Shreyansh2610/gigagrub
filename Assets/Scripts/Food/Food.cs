using System;
using UnityEngine;
using GigaGrub.Player;

namespace GigaGrub.Food
{
    [RequireComponent(typeof(Collider2D))]
    public class Food : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private CircleCollider2D circleCollider;

        private FoodData data;
        private Vector3 baseScale = Vector3.one;
        private float randomOffset;
        private bool isConsumed = false;

        public FoodData Data => data;
        public bool IsConsumed => isConsumed;

        public event Action<Food> OnConsumed;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (circleCollider == null)
            {
                circleCollider = GetComponent<CircleCollider2D>();
            }

            randomOffset = UnityEngine.Random.Range(0f, 10f);
        }

        private void Update()
        {
            if (isConsumed || data == null || !data.EnablePulse) return;

            // Subtle pulsing animation (low CPU calculation, zero allocation)
            float sine = Mathf.Sin((Time.time + randomOffset) * data.PulseSpeed);
            float scaleMod = 1f + (sine * data.PulseMagnitude);
            transform.localScale = baseScale * scaleMod;
        }

        public void Initialize(FoodData foodData, Sprite fallbackSprite = null)
        {
            data = foodData;
            isConsumed = false;

            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (circleCollider == null)
            {
                circleCollider = GetComponent<CircleCollider2D>();
            }

            if (data != null)
            {
                if (data.FoodSprite != null)
                {
                    spriteRenderer.sprite = data.FoodSprite;
                }
                else if (fallbackSprite != null && spriteRenderer.sprite == null)
                {
                    spriteRenderer.sprite = fallbackSprite;
                }

                spriteRenderer.color = data.FoodColor;
                baseScale = Vector3.one * data.ScaleMultiplier;
                transform.localScale = baseScale;

                if (circleCollider != null)
                {
                    circleCollider.isTrigger = true;
                    circleCollider.radius = 0.35f * data.ScaleMultiplier;
                    circleCollider.enabled = true;
                }
            }
        }

        public void OnSpawn(Vector2 position)
        {
            transform.position = new Vector3(position.x, position.y, 0f);
            isConsumed = false;

            if (circleCollider != null)
            {
                circleCollider.enabled = true;
            }

            gameObject.SetActive(true);
        }

        public void Consume(PlayerBody playerBody)
        {
            // Thread & frame-safe atomic check: avoid duplicate consumption & duplicate score
            if (isConsumed) return;
            isConsumed = true;

            if (circleCollider != null)
            {
                circleCollider.enabled = false;
            }

            if (playerBody != null && data != null)
            {
                playerBody.OnEatFood(data);
            }

            OnConsumed?.Invoke(this);

            OnReturnToPool();
        }

        public void OnReturnToPool()
        {
            gameObject.SetActive(false);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isConsumed) return;

            // Check if player or AI creature head triggered this food
            if (other.CompareTag("Player") || other.CompareTag("AICreature") || other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                PlayerBody body = other.GetComponent<PlayerBody>();
                if (body == null)
                {
                    body = other.GetComponentInParent<PlayerBody>();
                }

                if (body != null)
                {
                    Consume(body);
                }
            }
        }
    }
}
