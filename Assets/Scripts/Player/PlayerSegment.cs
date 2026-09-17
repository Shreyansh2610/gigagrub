using UnityEngine;

namespace GigaGrub.Player
{
    public class PlayerSegment : MonoBehaviour
    {
        [Header("Visuals")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        private Vector3 targetScale = Vector3.one;
        private Vector3 currentScale = Vector3.one;
        private float scaleSpeed = 8f;
        private bool isScaling = false;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (isScaling)
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

        public void OnSpawnFromPool(Vector3 position, Quaternion rotation, int sortingOrder, float scaleMultiplier = 1f, bool smoothScaleIn = true, float growthSpeed = 8f)
        {
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
            }
            else
            {
                currentScale = targetScale;
                transform.localScale = targetScale;
                isScaling = false;
            }

            gameObject.SetActive(true);
        }

        public void OnReturnToPool()
        {
            isScaling = false;
            gameObject.SetActive(false);
        }
    }
}
