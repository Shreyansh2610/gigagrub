using System.Collections.Generic;
using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [CreateAssetMenu(fileName = "CosmeticDatabase", menuName = "GigaGrub/Cosmetics/Database")]
    public class CosmeticDatabase : ScriptableObject
    {
        private static CosmeticDatabase instance;

        [Header("Cosmetic Asset Collections")]
        [SerializeField] private List<CreatureTypeData> creatures = new List<CreatureTypeData>();
        [SerializeField] private List<SkinColorData> skinColors = new List<SkinColorData>();
        [SerializeField] private List<PatternData> patterns = new List<PatternData>();
        [SerializeField] private List<ClothingData> clothing = new List<ClothingData>();
        [SerializeField] private List<HeadAccessoryData> hats = new List<HeadAccessoryData>();
        [SerializeField] private List<FaceData> eyes = new List<FaceData>();
        [SerializeField] private List<FaceData> mouths = new List<FaceData>();
        [SerializeField] private List<AccessoryData> accessories = new List<AccessoryData>();
        [SerializeField] private List<CosmeticEffectData> effects = new List<CosmeticEffectData>();

        private readonly Dictionary<string, CosmeticData> idToItemMap = new Dictionary<string, CosmeticData>();
        private bool isIndexed = false;

        public static CosmeticDatabase Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<CosmeticDatabase>("Cosmetics/CosmeticDatabase");
                }
                return instance;
            }
        }

        public IReadOnlyList<CreatureTypeData> Creatures => creatures;
        public IReadOnlyList<SkinColorData> SkinColors => skinColors;
        public IReadOnlyList<PatternData> Patterns => patterns;
        public IReadOnlyList<ClothingData> Clothing => clothing;
        public IReadOnlyList<HeadAccessoryData> Hats => hats;
        public IReadOnlyList<FaceData> Eyes => eyes;
        public IReadOnlyList<FaceData> Mouths => mouths;
        public IReadOnlyList<AccessoryData> Accessories => accessories;
        public IReadOnlyList<CosmeticEffectData> Effects => effects;

        public void SetInstanceForTest(CosmeticDatabase testDb)
        {
            instance = testDb;
            instance.RebuildIndex();
        }

        public void SetCollections(
            List<CreatureTypeData> newCreatures,
            List<SkinColorData> newColors,
            List<PatternData> newPatterns,
            List<ClothingData> newClothing,
            List<HeadAccessoryData> newHats,
            List<FaceData> newEyes,
            List<FaceData> newMouths,
            List<AccessoryData> newAccessories,
            List<CosmeticEffectData> newEffects)
        {
            creatures = newCreatures ?? new List<CreatureTypeData>();
            skinColors = newColors ?? new List<SkinColorData>();
            patterns = newPatterns ?? new List<PatternData>();
            clothing = newClothing ?? new List<ClothingData>();
            hats = newHats ?? new List<HeadAccessoryData>();
            eyes = newEyes ?? new List<FaceData>();
            mouths = newMouths ?? new List<FaceData>();
            accessories = newAccessories ?? new List<AccessoryData>();
            effects = newEffects ?? new List<CosmeticEffectData>();

            RebuildIndex();
        }

        public void RebuildIndex()
        {
            idToItemMap.Clear();

            IndexCollection(creatures);
            IndexCollection(skinColors);
            IndexCollection(patterns);
            IndexCollection(clothing);
            IndexCollection(hats);
            IndexCollection(eyes);
            IndexCollection(mouths);
            IndexCollection(accessories);
            IndexCollection(effects);

            isIndexed = true;
        }

        private void IndexCollection<T>(List<T> items) where T : CosmeticData
        {
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                T item = items[i];
                if (item != null && !string.IsNullOrEmpty(item.Id))
                {
                    idToItemMap[item.Id] = item;
                }
            }
        }

        private void EnsureIndexed()
        {
            if (!isIndexed || idToItemMap.Count == 0)
            {
                RebuildIndex();
            }
        }

        public CosmeticData GetItemById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            EnsureIndexed();
            idToItemMap.TryGetValue(id, out CosmeticData item);
            return item;
        }

        public T GetItemById<T>(string id) where T : CosmeticData
        {
            CosmeticData data = GetItemById(id);
            return data as T;
        }

        public CreatureTypeData GetCreature(string id) => GetItemById<CreatureTypeData>(id);
        public SkinColorData GetColor(string id) => GetItemById<SkinColorData>(id);
        public PatternData GetPattern(string id) => GetItemById<PatternData>(id);
        public ClothingData GetClothing(string id) => GetItemById<ClothingData>(id);
        public HeadAccessoryData GetHat(string id) => GetItemById<HeadAccessoryData>(id);
        public FaceData GetEyes(string id) => GetItemById<FaceData>(id);
        public FaceData GetMouth(string id) => GetItemById<FaceData>(id);
        public AccessoryData GetAccessory(string id) => GetItemById<AccessoryData>(id);
        public CosmeticEffectData GetEffect(string id) => GetItemById<CosmeticEffectData>(id);

        public IReadOnlyList<CosmeticData> GetItemsByCategory(CosmeticCategory category)
        {
            return category switch
            {
                CosmeticCategory.Creature => creatures,
                CosmeticCategory.Color => skinColors,
                CosmeticCategory.Pattern => patterns,
                CosmeticCategory.Clothing => clothing,
                CosmeticCategory.Hat => hats,
                CosmeticCategory.Eyes => eyes,
                CosmeticCategory.Mouth => mouths,
                CosmeticCategory.Accessory => accessories,
                CosmeticCategory.Effect => effects,
                _ => new List<CosmeticData>()
            };
        }
    }
}
