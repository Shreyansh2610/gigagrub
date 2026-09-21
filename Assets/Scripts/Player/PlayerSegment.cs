using UnityEngine;

namespace GigaGrub.Player
{
    public class PlayerSegment : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private PlayerBody owner;
        private Vector3 targetScale = Vector3.one;
        private Vector3 currentScale = Vector3.one;
        private float scaleSpeed = 8f;
        private bool isScaling = false;

        public PlayerBody Owner => owner;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private float growthTimer = 0f;
        private float growthDuration = 0.22f;
        private bool isGrowthPop = false;

        private void Update()
        {
            if (isScaling)
            {
                if (isGrowthPop)
                {
                    growthTimer += Time.deltaTime;
                    float progress = Mathf.Clamp01(growthTimer / growthDuration);
                    // Smooth overshoot ease-out: starts fast, overshoots to ~1.08, settles at 1.0
                    float t = progress - 1f;
                    float easeOutBack = (t * t * ((1.70158f + 1f) * t + 1.70158f) + 1f);
                    currentScale = targetScale * Mathf.Max(0f, easeOutBack);
                    transform.localScale = currentScale;

                    if (progress >= 1f)
                    {
                        currentScale = targetScale;
                        transform.localScale = targetScale;
                        isScaling = false;
                        isGrowthPop = false;
                    }
                }
                else
                {
                    currentScale = Vector3.Lerp(currentScale, targetScale, Time.deltaTime * scaleSpeed);
                    transform.localScale = currentScale;

                    if (Vector3.Distance(currentScale, targetScale) < 0.005f)
                    {
                        currentScale = targetScale;
                        transform.localScale = targetScale;
                        isScaling = false;
                    }
                }
            }
        }

        public void SetOwner(PlayerBody newOwner)
        {
            owner = newOwner;
        }

        public void SetSortingOrder(int order)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = order;
            }
        }

        public void SetColor(Color color)
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        public void SetScaleMultiplier(float multiplier, bool immediate = false, float speed = 8f)
        {
            targetScale = Vector3.one * multiplier;
            scaleSpeed = speed;

            if (immediate)
            {
                currentScale = targetScale;
                transform.localScale = targetScale;
                isScaling = false;
            }
            else
            {
                isScaling = true;
            }
        }

        public void OnSpawnFromPool(PlayerBody bodyOwner, Vector3 position, Quaternion rotation, int sortingOrder, float scaleMultiplier = 1f, bool smoothScaleIn = true, float growthSpeed = 8f)
        {
            owner = bodyOwner;
            transform.position = position;
            transform.rotation = rotation;
            SetSortingOrder(sortingOrder);

            targetScale = Vector3.one * scaleMultiplier;
            scaleSpeed = growthSpeed;

            if (smoothScaleIn)
            {
                currentScale = Vector3.zero;
                transform.localScale = Vector3.zero;
                isScaling = true;
                isGrowthPop = true;
                growthTimer = 0f;
            }
            else
            {
                currentScale = targetScale;
                transform.localScale = targetScale;
                isScaling = false;
                isGrowthPop = false;
            }

            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            isScaling = false;
            isGrowthPop = false;
            growthTimer = 0f;
            gameObject.SetActive(false);
        }
    }
}
