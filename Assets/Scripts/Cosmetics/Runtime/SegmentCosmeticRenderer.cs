using UnityEngine;
using GigaGrub.Player;

namespace GigaGrub.Cosmetics
{
    public class SegmentCosmeticRenderer : MonoBehaviour
    {
        [Header("Renderers")]
        [SerializeField] private SpriteRenderer baseRenderer;
        [SerializeField] private SpriteRenderer clothingRenderer;
        [SerializeField] private SpriteRenderer tailAccessoryRenderer;

        public SpriteRenderer BaseRenderer => baseRenderer;
        public SpriteRenderer ClothingRenderer => clothingRenderer;
        public SpriteRenderer ClothingOverlayRenderer => clothingRenderer;
        public SpriteRenderer TailAccessoryRenderer => tailAccessoryRenderer;

        private PlayerSegment playerSegment;
        private int segmentIndex = 0;

        private void Awake()
        {
            EnsureLayers();
        }

        public void EnsureLayers()
        {
            if (playerSegment == null)
            {
                playerSegment = GetComponent<PlayerSegment>();
            }

            if (baseRenderer == null)
            {
                baseRenderer = GetComponent<SpriteRenderer>();
            }

            if (clothingRenderer == null)
            {
                Transform clothingT = transform.Find("ClothingLayer");
                if (clothingT == null)
                {
                    GameObject cGo = new GameObject("ClothingLayer");
                    cGo.transform.SetParent(transform, false);
                    clothingT = cGo.transform;
                }
                clothingRenderer = clothingT.GetComponent<SpriteRenderer>();
                if (clothingRenderer == null)
                {
                    clothingRenderer = clothingT.gameObject.AddComponent<SpriteRenderer>();
                }
            }

            if (tailAccessoryRenderer == null)
            {
                Transform tailT = transform.Find("TailAccessoryLayer");
                if (tailT == null)
                {
                    GameObject tGo = new GameObject("TailAccessoryLayer");
                    tGo.transform.SetParent(transform, false);
                    tailT = tGo.transform;
                }
                tailAccessoryRenderer = tailT.GetComponent<SpriteRenderer>();
                if (tailAccessoryRenderer == null)
                {
                    tailAccessoryRenderer = tailT.gameObject.AddComponent<SpriteRenderer>();
                }
            }
        }

        public void ApplyCosmetics(
            int index,
            int totalSegments,
            Sprite segmentSprite,
            Color segmentColor,
            ClothingData clothingData,
            Color clothingColor,
            AccessoryData tailAccessory,
            int baseSortingOrder)
        {
            EnsureLayers();
            segmentIndex = index;

            // 1. Base Segment Layer
            if (baseRenderer != null)
            {
                if (segmentSprite != null)
                {
                    baseRenderer.sprite = segmentSprite;
                }
                baseRenderer.color = segmentColor;
                baseRenderer.sortingOrder = baseSortingOrder;
            }

            // 2. Clothing Overlay Layer
            if (clothingRenderer != null)
            {
                if (clothingData != null && clothingData.SegmentClothingSprite != null && clothingData.ShouldCoverSegment(index))
                {
                    clothingRenderer.gameObject.SetActive(true);
                    clothingRenderer.sprite = clothingData.SegmentClothingSprite;
                    clothingRenderer.color = clothingColor;
                    clothingRenderer.sortingOrder = baseSortingOrder + clothingData.SortingOffset;
                }
                else
                {
                    clothingRenderer.gameObject.SetActive(false);
                }
            }

            // 3. Tail Accessory Layer (Only visible on last segment)
            if (tailAccessoryRenderer != null)
            {
                bool isTail = (index == totalSegments - 1) && totalSegments > 0;
                if (isTail && tailAccessory != null && tailAccessory.AccessorySprite != null)
                {
                    tailAccessoryRenderer.gameObject.SetActive(true);
                    tailAccessoryRenderer.sprite = tailAccessory.AccessorySprite;
                    tailAccessoryRenderer.color = tailAccessory.TintColor;
                    tailAccessoryRenderer.transform.localPosition = tailAccessory.LocalOffset;
                    tailAccessoryRenderer.transform.localScale = tailAccessory.LocalScale;
                    tailAccessoryRenderer.sortingOrder = baseSortingOrder + tailAccessory.SortingOffset;
                }
                else
                {
                    tailAccessoryRenderer.gameObject.SetActive(false);
                }
            }
        }
    }
}
