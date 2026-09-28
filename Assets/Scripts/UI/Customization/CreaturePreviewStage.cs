using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GigaGrub.Cosmetics
{
    public class CreaturePreviewStage : MonoBehaviour
    {
        [Header("Preview Root & Elements")]
        [SerializeField] private RectTransform previewContainer;
        [SerializeField] private Image headImage;
        [SerializeField] private Image collarImage;

        [Header("Body Segments Preview")]
        [SerializeField] private int previewSegmentCount = 5;
        [SerializeField] private float segmentSpacing = 28f;
        [SerializeField] private float waveSpeed = 3.5f;
        [SerializeField] private float waveAmplitude = 10f;

        [Header("Effect Preview")]
        [SerializeField] private int effectParticleCount = 6;
        private readonly List<Image> effectParticleImages = new List<Image>();
        private readonly List<RectTransform> effectParticleTransforms = new List<RectTransform>();

        private readonly List<Image> segmentBaseImages = new List<Image>();
        private readonly List<Image> segmentClothingImages = new List<Image>();
        private readonly List<RectTransform> segmentTransforms = new List<RectTransform>();

        private EquippedCosmetics previewConfig;
        private CosmeticEffectData activePreviewEffect;
        private float waveTimer = 0f;

        private void Awake()
        {
            EnsurePreviewHierarchy();
        }

        private void Update()
        {
            // Gentle idle undulating breathing / swimming animation in the preview window
            waveTimer += Time.unscaledDeltaTime * waveSpeed;
            AnimatePreviewBody();
            AnimateEffectParticles();
        }

        public void EnsurePreviewHierarchy()
        {
            if (previewContainer == null)
            {
                previewContainer = GetComponent<RectTransform>();
            }

            if (headImage == null)
            {
                headImage = GetOrCreateImage("HeadImage", 100);
            }

            if (collarImage == null)
            {
                collarImage = GetOrCreateImage("CollarImage", 101);
            }

            // Create preview body segments behind head
            if (segmentTransforms.Count == 0)
            {
                Transform bodyParent = previewContainer.Find("BodySegments");
                if (bodyParent == null)
                {
                    GameObject bpGo = new GameObject("BodySegments", typeof(RectTransform));
                    bpGo.transform.SetParent(previewContainer, false);
                    bpGo.transform.SetSiblingIndex(0); // Underneath head layers
                    bodyParent = bpGo.transform;
                }

                for (int i = 0; i < previewSegmentCount; i++)
                {
                    GameObject segGo = new GameObject($"PreviewSeg_{i}", typeof(RectTransform), typeof(Image));
                    segGo.transform.SetParent(bodyParent, false);
                    RectTransform rt = segGo.GetComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(70f, 70f);

                    Image baseImg = segGo.GetComponent<Image>();
                    baseImg.raycastTarget = false;

                    GameObject clothGo = new GameObject("ClothLayer", typeof(RectTransform), typeof(Image));
                    clothGo.transform.SetParent(segGo.transform, false);
                    RectTransform clothRt = clothGo.GetComponent<RectTransform>();
                    clothRt.sizeDelta = new Vector2(70f, 70f);
                    Image clothImg = clothGo.GetComponent<Image>();
                    clothImg.raycastTarget = false;

                    segmentTransforms.Add(rt);
                    segmentBaseImages.Add(baseImg);
                    segmentClothingImages.Add(clothImg);
                }
            }

            // Create floating effect preview particles
            if (effectParticleTransforms.Count == 0)
            {
                Transform effParent = previewContainer.Find("EffectParticles");
                if (effParent == null)
                {
                    GameObject epGo = new GameObject("EffectParticles", typeof(RectTransform));
                    epGo.transform.SetParent(previewContainer, false);
                    epGo.transform.SetSiblingIndex(previewContainer.childCount - 1);
                    effParent = epGo.transform;
                }

                for (int i = 0; i < effectParticleCount; i++)
                {
                    GameObject pGo = new GameObject($"EffectPart_{i}", typeof(RectTransform), typeof(Image));
                    pGo.transform.SetParent(effParent, false);
                    RectTransform prt = pGo.GetComponent<RectTransform>();
                    prt.sizeDelta = new Vector2(32f, 32f);

                    Image pImg = pGo.GetComponent<Image>();
                    pImg.raycastTarget = false;
                    pGo.SetActive(false);

                    effectParticleTransforms.Add(prt);
                    effectParticleImages.Add(pImg);
                }
            }
        }

        private Image GetOrCreateImage(string name, int siblingOrder)
        {
            Transform t = previewContainer.Find(name);
            if (t == null)
            {
                GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image));
                go.transform.SetParent(previewContainer, false);
                t = go.transform;
            }

            RectTransform rt = t.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(85f, 85f);
            Image img = t.GetComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        public void UpdatePreview(EquippedCosmetics config)
        {
            if (config == null) return;
            previewConfig = config.Clone();
            EnsurePreviewHierarchy();

            CosmeticDatabase db = CosmeticDatabase.Instance;
            if (db == null) return;

            CreatureTypeData creature = db.GetItemById<CreatureTypeData>(config.CreatureId);
            SkinColorData color = db.GetItemById<SkinColorData>(config.ColorId);
            PatternData pattern = db.GetItemById<PatternData>(config.PatternId);
            ClothingData clothing = db.GetItemById<ClothingData>(config.ClothingId);
            activePreviewEffect = db.GetItemById<CosmeticEffectData>(config.EffectId);

            Color primary = config.UseCustomColors ? config.CustomPrimary : (color != null ? color.PrimaryColor : (creature != null ? creature.DefaultPrimaryColor : Color.white));
            Color secondary = config.UseCustomColors ? config.CustomSecondary : (color != null ? color.SecondaryColor : (creature != null ? creature.DefaultSecondaryColor : Color.white));

            Vector2 headOrigin = new Vector2(0f, 65f);

            // Head
            if (headImage != null && creature != null)
            {
                headImage.sprite = creature.HeadSprite;
                headImage.color = primary;
                headImage.rectTransform.anchoredPosition = headOrigin;
            }

            // Collar
            if (collarImage != null)
            {
                if (clothing != null && clothing.HeadCollarSprite != null)
                {
                    collarImage.gameObject.SetActive(true);
                    collarImage.sprite = clothing.HeadCollarSprite;
                    collarImage.color = clothing.TintWithSecondary ? secondary : clothing.CustomColor;
                    collarImage.rectTransform.anchoredPosition = headOrigin;
                }
                else
                {
                    collarImage.gameObject.SetActive(false);
                }
            }

            // Segments
            Sprite segSprite = (pattern != null && pattern.PatternSprite != null)
                ? pattern.PatternSprite
                : (creature != null ? creature.SegmentSprite : null);

            Color clothColor = clothing != null ? (clothing.TintWithSecondary ? secondary : clothing.CustomColor) : Color.white;

            for (int i = 0; i < segmentBaseImages.Count; i++)
            {
                Image baseImg = segmentBaseImages[i];
                Image clothImg = segmentClothingImages[i];

                if (baseImg != null)
                {
                    if (segSprite != null) baseImg.sprite = segSprite;
                    Color segColor = color != null ? color.EvaluateColorAtSegment(i, segmentBaseImages.Count) : (i % 2 == 0 ? primary : secondary);
                    baseImg.color = segColor;
                }

                if (clothImg != null)
                {
                    if (clothing != null && clothing.SegmentClothingSprite != null && clothing.ShouldCoverSegment(i))
                    {
                        clothImg.gameObject.SetActive(true);
                        clothImg.sprite = clothing.SegmentClothingSprite;
                        clothImg.color = clothColor;
                    }
                    else
                    {
                        clothImg.gameObject.SetActive(false);
                    }
                }
            }

            // Effect Preview Particles
            bool hasEffect = activePreviewEffect != null && (activePreviewEffect.ParticleSprite != null || activePreviewEffect.PreviewSprite != null);
            for (int i = 0; i < effectParticleImages.Count; i++)
            {
                if (effectParticleImages[i] == null) continue;
                if (hasEffect)
                {
                    effectParticleImages[i].gameObject.SetActive(true);
                    effectParticleImages[i].sprite = activePreviewEffect.ParticleSprite != null ? activePreviewEffect.ParticleSprite : activePreviewEffect.PreviewSprite;
                    effectParticleImages[i].color = (i % 2 == 0) ? activePreviewEffect.PrimaryEffectColor : activePreviewEffect.SecondaryEffectColor;
                }
                else
                {
                    effectParticleImages[i].gameObject.SetActive(false);
                }
            }
        }

        private void AnimatePreviewBody()
        {
            Vector2 headOrigin = new Vector2(0f, 65f);
            for (int i = 0; i < segmentTransforms.Count; i++)
            {
                RectTransform rt = segmentTransforms[i];
                if (rt == null) continue;

                float offset = headOrigin.y - (i + 1) * segmentSpacing;
                float lateralOffset = Mathf.Sin(waveTimer - (i * 0.65f)) * waveAmplitude;
                float scaleT = 1f - (i * 0.05f);

                rt.anchoredPosition = new Vector2(lateralOffset, offset);
                rt.localScale = Vector3.one * Mathf.Max(0.65f, scaleT);
            }
        }

        private void AnimateEffectParticles()
        {
            if (activePreviewEffect == null || effectParticleTransforms.Count == 0) return;

            Vector2 headOrigin = new Vector2(0f, 30f);
            float angleStep = (Mathf.PI * 2f) / effectParticleTransforms.Count;

            for (int i = 0; i < effectParticleTransforms.Count; i++)
            {
                RectTransform prt = effectParticleTransforms[i];
                if (prt == null || !prt.gameObject.activeSelf) continue;

                float angle = waveTimer * 2f + (i * angleStep);
                float radiusX = 55f + Mathf.Sin(waveTimer * 3f + i) * 12f;
                float radiusY = 70f + Mathf.Cos(waveTimer * 2.5f + i) * 15f;

                float x = Mathf.Cos(angle) * radiusX;
                float y = headOrigin.y + Mathf.Sin(angle) * radiusY;

                prt.anchoredPosition = new Vector2(x, y);

                float pulse = 0.8f + Mathf.Sin(waveTimer * 4f + i) * 0.35f;
                prt.localScale = Vector3.one * pulse;
            }
        }
    }
}
