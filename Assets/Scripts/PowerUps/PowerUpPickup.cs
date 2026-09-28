using System;
using UnityEngine;
using GigaGrub.Player;

namespace GigaGrub.PowerUps
{
    [RequireComponent(typeof(Collider2D))]
    public class PowerUpPickup : MonoBehaviour
    {
        [Header("Visual Components")]
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private SpriteRenderer auraRenderer;
        [SerializeField] private CircleCollider2D circleCollider;

        [Header("Animation Settings")]
        [SerializeField] private float rotationSpeed = 60f;
        [SerializeField] private float floatSpeed = 3f;
        [SerializeField] private float floatDistance = 0.15f;

        private PowerUpData data;
        private Vector3 basePosition;
        private Vector3 baseScale = Vector3.one;
        private float randomPhase;
        private bool isCollected;

        public PowerUpData Data => data;
        public bool IsCollected => isCollected;

        public event Action<PowerUpPickup> OnCollected;

        private void Awake()
        {
            if (iconRenderer == null)
            {
                iconRenderer = GetComponent<SpriteRenderer>();
            }

            if (circleCollider == null)
            {
                circleCollider = GetComponent<CircleCollider2D>();
            }

            randomPhase = UnityEngine.Random.Range(0f, 10f);
            baseScale = transform.localScale;
        }

        private void Update()
        {
            if (isCollected || data == null) return;

            // Subtle floating and rotating visual animation
            float floatOffset = Mathf.Sin((Time.time + randomPhase) * floatSpeed) * floatDistance;
            transform.position = new Vector3(basePosition.x, basePosition.y + floatOffset, basePosition.z);

            if (auraRenderer != null)
            {
                auraRenderer.transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
            }
            else
            {
                transform.Rotate(0f, 0f, (rotationSpeed * 0.5f) * Time.deltaTime);
            }

            if (data.EnablePulsing)
            {
                float pulse = 1f + Mathf.Sin((Time.time + randomPhase) * data.PulseSpeed) * data.PulseMagnitude;
                transform.localScale = baseScale * pulse;
            }
        }

        public void Initialize(PowerUpData powerUpData, Sprite fallbackSprite = null)
        {
            data = powerUpData;
            isCollected = false;

            if (circleCollider == null)
            {
                circleCollider = GetComponent<CircleCollider2D>();
            }

            if (circleCollider != null)
            {
                circleCollider.isTrigger = true;
                circleCollider.radius = 0.65f;
                circleCollider.enabled = true;
            }

            if (data != null)
            {
                if (iconRenderer != null)
                {
                    if (data.IconSprite != null)
                    {
                        iconRenderer.sprite = data.IconSprite;
                    }
                    else if (fallbackSprite != null && iconRenderer.sprite == null)
                    {
                        iconRenderer.sprite = fallbackSprite;
                    }
                    iconRenderer.color = data.ThemeColor;
                }

                if (auraRenderer != null)
                {
                    auraRenderer.color = new Color(data.ThemeColor.r, data.ThemeColor.g, data.ThemeColor.b, 0.45f);
                }
            }

            this.enabled = true;
        }

        public void OnSpawn(Vector2 position)
        {
            basePosition = new Vector3(position.x, position.y, 0f);
            transform.position = basePosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = baseScale;
            isCollected = false;

            if (circleCollider != null)
            {
                circleCollider.enabled = true;
            }

            this.enabled = true;
            gameObject.SetActive(true);
        }

        public void Collect(PlayerBody playerBody)
        {
            if (isCollected) return;
            isCollected = true;

            this.enabled = false;
            if (circleCollider != null)
            {
                circleCollider.enabled = false;
            }

            if (playerBody != null && data != null)
            {
                PowerUpManager manager = playerBody.GetComponent<PowerUpManager>();
                if (manager == null)
                {
                    manager = playerBody.gameObject.AddComponent<PowerUpManager>();
                }

                manager.ApplyPowerUp(data);
            }

            OnCollected?.Invoke(this);
            OnReturnToPool();
        }

        public void OnReturnToPool()
        {
            this.enabled = false;
            gameObject.SetActive(false);
        }

        public void OnTriggerEnter2D(Collider2D other)
        {
            if (isCollected || other == null) return;

            PlayerBody body = other.GetComponent<PlayerBody>() ?? other.GetComponentInParent<PlayerBody>();
            if (body == null)
            {
                var seg = other.GetComponent<PlayerSegment>() ?? other.GetComponentInParent<PlayerSegment>();
                if (seg != null)
                {
                    body = seg.Owner;
                }
            }

            if (body != null)
            {
                Collect(body);
            }
        }
    }
}
