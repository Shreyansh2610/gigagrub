using UnityEngine;
using GigaGrub.Player;

namespace GigaGrub.Cosmetics
{
    public class CreatureCosmeticController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerBody playerBody;
        [SerializeField] private SpriteRenderer headBaseRenderer;
        [SerializeField] private SpriteRenderer collarRenderer;
        [SerializeField] private SpriteRenderer eyesRenderer;
        [SerializeField] private SpriteRenderer mouthRenderer;
        [SerializeField] private SpriteRenderer hatRenderer;
        [SerializeField] private SpriteRenderer neckRenderer;
        [SerializeField] private SpriteRenderer backRenderer;
        [SerializeField] private CosmeticEffectInstance effectInstance;

        private EquippedCosmetics currentConfig;
        private CreatureTypeData currentCreature;
        private SkinColorData currentColor;
        private PatternData currentPattern;
        private ClothingData currentClothing;
        private HeadAccessoryData currentHat;
        private FaceData currentEyes;
        private FaceData currentMouth;
        private AccessoryData currentBack;
        private AccessoryData currentNeck;
        private AccessoryData currentTail;
        private CosmeticEffectData currentEffect;

        public EquippedCosmetics CurrentConfig => currentConfig;
        public EquippedCosmetics CurrentEquipped => currentConfig;
        public CreatureTypeData CurrentCreature => currentCreature;
        public SkinColorData CurrentColor => currentColor;
        public ClothingData CurrentClothing => currentClothing;
        public HeadAccessoryData CurrentHat => currentHat;

        public SpriteRenderer HeadBaseRenderer => headBaseRenderer;
        public SpriteRenderer CollarRenderer => collarRenderer;
        public SpriteRenderer EyesRenderer => eyesRenderer;
        public SpriteRenderer MouthRenderer => mouthRenderer;
        public SpriteRenderer HatRenderer => hatRenderer;
        public SpriteRenderer NeckRenderer => neckRenderer;
        public SpriteRenderer BackRenderer => backRenderer;
        public CosmeticEffectInstance EffectInstance => effectInstance;

        public void ApplyCosmetics(EquippedCosmetics config) => ApplyEquippedCosmetics(config);

        private void Awake()
        {
            EnsureLayers();
        }

        public void EnsureLayers()
        {
            if (playerBody == null)
            {
                playerBody = GetComponent<PlayerBody>();
            }

            if (headBaseRenderer == null)
            {
                headBaseRenderer = GetComponent<SpriteRenderer>();
            }

            if (collarRenderer == null)
            {
                collarRenderer = GetOrCreateLayer("Layer_Collar", 101);
            }

            if (eyesRenderer == null)
            {
                eyesRenderer = GetOrCreateLayer("Layer_Eyes", 103);
            }

            if (mouthRenderer == null)
            {
                mouthRenderer = GetOrCreateLayer("Layer_Mouth", 102);
            }

            if (hatRenderer == null)
            {
                hatRenderer = GetOrCreateLayer("Layer_Hat", 105);
            }

            if (neckRenderer == null)
            {
                neckRenderer = GetOrCreateLayer("Layer_Neck", 98);
            }

            if (backRenderer == null)
            {
                backRenderer = GetOrCreateLayer("Layer_Back", 92);
            }

            if (effectInstance == null)
            {
                Transform effT = transform.Find("Layer_Effect");
                if (effT == null)
                {
                    GameObject effGo = new GameObject("Layer_Effect");
                    effGo.transform.SetParent(transform, false);
                    effT = effGo.transform;
                }
                effectInstance = effT.GetComponent<CosmeticEffectInstance>();
                if (effectInstance == null)
                {
                    effectInstance = effT.gameObject.AddComponent<CosmeticEffectInstance>();
                }
            }
        }

        private SpriteRenderer GetOrCreateLayer(string name, int sortingOrder)
        {
            Transform t = transform.Find(name);
            if (t == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(transform, false);
                t = go.transform;
            }

            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = t.gameObject.AddComponent<SpriteRenderer>();
            }
            sr.sortingOrder = sortingOrder;
            return sr;
        }

        public void ApplyEquippedCosmetics(EquippedCosmetics config)
        {
            if (config == null) return;
            currentConfig = config.Clone();
            EnsureLayers();

            CosmeticDatabase db = CosmeticDatabase.Instance;
            if (db == null) return;

            currentCreature = db.GetItemById<CreatureTypeData>(config.CreatureId);
            currentColor = db.GetItemById<SkinColorData>(config.ColorId);
            currentPattern = db.GetItemById<PatternData>(config.PatternId);
            currentClothing = db.GetItemById<ClothingData>(config.ClothingId);
            currentHat = db.GetItemById<HeadAccessoryData>(config.HatId);
            currentEyes = db.GetItemById<FaceData>(config.EyesId);
            currentMouth = db.GetItemById<FaceData>(config.MouthId);
            currentBack = db.GetItemById<AccessoryData>(config.BackAccessoryId);
            currentNeck = db.GetItemById<AccessoryData>(config.NeckAccessoryId);
            currentTail = db.GetItemById<AccessoryData>(config.TailAccessoryId);
            currentEffect = db.GetItemById<CosmeticEffectData>(config.EffectId);

            Color primaryColor = config.UseCustomColors ? config.CustomPrimary : (currentColor != null ? currentColor.PrimaryColor : (currentCreature != null ? currentCreature.DefaultPrimaryColor : Color.white));
            Color secondaryColor = config.UseCustomColors ? config.CustomSecondary : (currentColor != null ? currentColor.SecondaryColor : (currentCreature != null ? currentCreature.DefaultSecondaryColor : Color.white));
            Color accentColor = config.UseCustomColors ? config.CustomAccent : (currentColor != null ? currentColor.AccentColor : (currentCreature != null ? currentCreature.DefaultAccentColor : Color.cyan));

            // 1. Head Base
            if (headBaseRenderer != null && currentCreature != null)
            {
                headBaseRenderer.sprite = currentCreature.HeadSprite;
                headBaseRenderer.color = primaryColor;
                transform.localScale = currentCreature.HeadScale;
            }

            // 2. Collar / Shirt Collar
            if (collarRenderer != null)
            {
                if (currentClothing != null && currentClothing.HeadCollarSprite != null)
                {
                    collarRenderer.gameObject.SetActive(true);
                    collarRenderer.sprite = currentClothing.HeadCollarSprite;
                    collarRenderer.color = currentClothing.TintWithSecondary ? secondaryColor : currentClothing.CustomColor;
                }
                else
                {
                    collarRenderer.gameObject.SetActive(false);
                }
            }

            // 3. Eyes / Mouth / Neck / Back / Hat (Disabled - eyes/mouth are built into creature head sprites, accessories/hats removed)
            if (eyesRenderer != null) eyesRenderer.gameObject.SetActive(false);
            if (mouthRenderer != null) mouthRenderer.gameObject.SetActive(false);
            if (neckRenderer != null) neckRenderer.gameObject.SetActive(false);
            if (backRenderer != null) backRenderer.gameObject.SetActive(false);
            if (hatRenderer != null) hatRenderer.gameObject.SetActive(false);

            // 4. Effect
            if (effectInstance != null)
            {
                effectInstance.ApplyEffect(currentEffect);
            }

            // 9. Synchronize Segments
            RefreshAllSegmentCosmetics();
        }

        public void RefreshAllSegmentCosmetics()
        {
            if (playerBody == null) return;

            var segments = playerBody.ActiveSegments;
            int total = segments.Count;
            if (total == 0) return;

            Sprite segSprite = (currentPattern != null && currentPattern.PatternSprite != null)
                ? currentPattern.PatternSprite
                : (currentCreature != null ? currentCreature.SegmentSprite : null);

            Color primary = currentConfig != null && currentConfig.UseCustomColors ? currentConfig.CustomPrimary : (currentColor != null ? currentColor.PrimaryColor : Color.white);
            Color secondary = currentConfig != null && currentConfig.UseCustomColors ? currentConfig.CustomSecondary : (currentColor != null ? currentColor.SecondaryColor : Color.white);
            Color clothingColor = currentClothing != null ? (currentClothing.TintWithSecondary ? secondary : currentClothing.CustomColor) : Color.white;

            for (int i = 0; i < total; i++)
            {
                PlayerSegment seg = segments[i];
                if (seg == null) continue;

                SegmentCosmeticRenderer segRenderer = seg.GetComponent<SegmentCosmeticRenderer>();
                if (segRenderer == null)
                {
                    segRenderer = seg.gameObject.AddComponent<SegmentCosmeticRenderer>();
                }

                Color segColor = currentColor != null ? currentColor.EvaluateColorAtSegment(i, total) : (i % 2 == 0 ? primary : secondary);
                int baseSortOrder = 100 - (i + 1);

                segRenderer.ApplyCosmetics(
                    i,
                    total,
                    segSprite,
                    segColor,
                    currentClothing,
                    clothingColor,
                    currentTail,
                    baseSortOrder
                );
            }
        }
    }
}
