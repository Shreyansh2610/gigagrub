using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GigaGrub.Cosmetics;

namespace GigaGrub.Editor
{
    public static class CosmeticDatabaseBuilder
    {
        private const string CosmeticsResourcesPath = "Assets/Resources/Cosmetics";
        private const string ArtCosmeticsPath = "Assets/Art/Cosmetics";

        public static CosmeticDatabase BuildAndSaveAllCosmetics()
        {
            EnsureCosmeticResourceFolders();
            CosmeticAssetGenerator.GenerateAllCosmeticSprites();

            var creatures = BuildCreatureTypeData();
            var skinColors = BuildSkinColorData();
            var patterns = BuildPatternData();
            var clothing = BuildClothingData();
            var hats = BuildHatData();
            var eyes = BuildEyesData();
            var mouths = BuildMouthData();
            var accessories = BuildAccessoryData();
            var effects = BuildEffectData();

            string dbPath = $"{CosmeticsResourcesPath}/CosmeticDatabase.asset";
            CosmeticDatabase db = AssetDatabase.LoadAssetAtPath<CosmeticDatabase>(dbPath);
            if (db == null)
            {
                db = ScriptableObject.CreateInstance<CosmeticDatabase>();
                AssetDatabase.CreateAsset(db, dbPath);
            }

            db.SetCollections(creatures, skinColors, patterns, clothing, hats, eyes, mouths, accessories, effects);
            EditorUtility.SetDirty(db);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[GigaGrub] Built CosmeticDatabase with {creatures.Count} creatures, {skinColors.Count} colors, {patterns.Count} patterns, {clothing.Count} clothes, {hats.Count} hats, {eyes.Count} eyes, {mouths.Count} mouths, {accessories.Count} accessories, {effects.Count} effects!");
            return db;
        }

        private static void EnsureCosmeticResourceFolders()
        {
            EnsureFolder("Assets", "Resources");
            EnsureFolder("Assets/Resources", "Cosmetics");
            EnsureFolder(CosmeticsResourcesPath, "Creatures");
            EnsureFolder(CosmeticsResourcesPath, "Colors");
            EnsureFolder(CosmeticsResourcesPath, "Patterns");
            EnsureFolder(CosmeticsResourcesPath, "Clothing");
            EnsureFolder(CosmeticsResourcesPath, "Hats");
            EnsureFolder(CosmeticsResourcesPath, "Eyes");
            EnsureFolder(CosmeticsResourcesPath, "Mouths");
            EnsureFolder(CosmeticsResourcesPath, "Accessories");
            EnsureFolder(CosmeticsResourcesPath, "Effects");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string full = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static Sprite LoadSprite(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static List<CreatureTypeData> BuildCreatureTypeData()
        {
            string artP = $"{ArtCosmeticsPath}/Creatures";
            string resP = $"{CosmeticsResourcesPath}/Creatures";

            var configs = new (string id, string name, CosmeticRarity rarity, string headF, string segF, Color p, Color s, Color a, UnlockType unlock, int price, string req, bool def)[]
            {
                ("creature_01_grub", "Grub", CosmeticRarity.Common, "Creature_Grub_Head.png", "Creature_Grub_Seg.png", new Color(0.12f, 0.95f, 0.72f), new Color(0.04f, 0.65f, 0.45f), new Color(0.45f, 1f, 0.85f), UnlockType.DefaultUnlocked, 0, "Default", true),
                ("creature_02_slime", "Slime", CosmeticRarity.Uncommon, "Creature_Slime_Head.png", "Creature_Slime_Seg.png", new Color(0.2f, 0.85f, 1f), new Color(0.05f, 0.55f, 0.8f), new Color(0.7f, 0.95f, 1f), UnlockType.Coins, 300, "300 Coins", false),
                ("creature_03_dragon", "Dragon", CosmeticRarity.Legendary, "Creature_Dragon_Head.png", "Creature_Dragon_Seg.png", new Color(1f, 0.35f, 0.15f), new Color(0.7f, 0.15f, 0.05f), new Color(1f, 0.85f, 0.2f), UnlockType.Coins, 1200, "1,200 Coins", false),
                ("creature_04_alien", "Alien", CosmeticRarity.Epic, "Creature_Alien_Head.png", "Creature_Alien_Seg.png", new Color(0.85f, 0.25f, 0.95f), new Color(0.45f, 0.08f, 0.65f), new Color(0.95f, 0.75f, 1f), UnlockType.Coins, 800, "800 Coins", false),
                ("creature_05_lizard", "Lizard", CosmeticRarity.Rare, "Creature_Lizard_Head.png", "Creature_Lizard_Seg.png", new Color(0.4f, 0.9f, 0.2f), new Color(0.2f, 0.6f, 0.05f), new Color(0.85f, 1f, 0.4f), UnlockType.Coins, 500, "500 Coins", false),
                ("creature_06_monster", "Monster", CosmeticRarity.Mythic, "Creature_Monster_Head.png", "Creature_Monster_Seg.png", new Color(0.95f, 0.2f, 0.4f), new Color(0.6f, 0.05f, 0.2f), new Color(1f, 0.65f, 0.8f), UnlockType.Coins, 2000, "2,000 Coins", false),
            };

            List<CreatureTypeData> list = new List<CreatureTypeData>();
            for (int i = 0; i < configs.Length; i++)
            {
                var cfg = configs[i];
                string assetPath = $"{resP}/{cfg.id}.asset";
                CreatureTypeData data = AssetDatabase.LoadAssetAtPath<CreatureTypeData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<CreatureTypeData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                Sprite head = LoadSprite($"{artP}/{cfg.headF}");
                Sprite seg = LoadSprite($"{artP}/{cfg.segF}");
                data.ConfigureCreature(cfg.id, cfg.name, cfg.rarity, head, head, seg, null, null, cfg.p, cfg.s, cfg.a, cfg.unlock, cfg.price, cfg.req, cfg.def);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static List<SkinColorData> BuildSkinColorData()
        {
            string resP = $"{CosmeticsResourcesPath}/Colors";

            var configs = new (string id, string name, CosmeticRarity rarity, Color p, Color s, Color a, bool grad, Color[] grads, UnlockType unlock, int price, string req, bool def)[]
            {
                ("color_01_emerald", "Emerald Mint", CosmeticRarity.Common, new Color(0.12f, 0.95f, 0.72f), new Color(0.04f, 0.65f, 0.45f), new Color(0.45f, 1f, 0.85f), false, null, UnlockType.DefaultUnlocked, 0, "Default", true),
                ("color_02_crimson", "Crimson Red", CosmeticRarity.Common, new Color(0.95f, 0.25f, 0.25f), new Color(0.65f, 0.08f, 0.08f), new Color(1f, 0.65f, 0.65f), false, null, UnlockType.Coins, 100, "100 Coins", false),
                ("color_03_ocean", "Ocean Blue", CosmeticRarity.Common, new Color(0.2f, 0.65f, 1f), new Color(0.05f, 0.35f, 0.75f), new Color(0.7f, 0.9f, 1f), false, null, UnlockType.Coins, 100, "100 Coins", false),
                ("color_04_gold", "Radiant Gold", CosmeticRarity.Uncommon, new Color(1f, 0.85f, 0.15f), new Color(0.75f, 0.5f, 0.02f), new Color(1f, 0.95f, 0.55f), false, null, UnlockType.Coins, 200, "200 Coins", false),
                ("color_05_violet", "Astral Violet", CosmeticRarity.Uncommon, new Color(0.85f, 0.25f, 0.95f), new Color(0.45f, 0.08f, 0.65f), new Color(0.95f, 0.75f, 1f), false, null, UnlockType.Coins, 200, "200 Coins", false),
                ("color_06_coral", "Coral Pink", CosmeticRarity.Common, new Color(1f, 0.45f, 0.65f), new Color(0.75f, 0.15f, 0.35f), new Color(1f, 0.8f, 0.9f), false, null, UnlockType.Coins, 100, "100 Coins", false),
                ("color_07_orange", "Neon Orange", CosmeticRarity.Common, new Color(1f, 0.55f, 0.1f), new Color(0.75f, 0.25f, 0.02f), new Color(1f, 0.85f, 0.4f), false, null, UnlockType.Coins, 100, "100 Coins", false),
                ("color_08_cyan", "Electric Cyan", CosmeticRarity.Common, new Color(0.15f, 0.9f, 1f), new Color(0.02f, 0.5f, 0.75f), new Color(0.65f, 0.95f, 1f), false, null, UnlockType.Coins, 100, "100 Coins", false),
                ("color_09_lime", "Sprout Lime", CosmeticRarity.Common, new Color(0.45f, 0.95f, 0.2f), new Color(0.25f, 0.65f, 0.05f), new Color(0.8f, 1f, 0.5f), false, null, UnlockType.Coins, 100, "100 Coins", false),
                ("color_10_white", "Pure Pearl", CosmeticRarity.Rare, new Color(0.95f, 0.95f, 0.98f), new Color(0.75f, 0.78f, 0.85f), Color.white, false, null, UnlockType.Coins, 400, "400 Coins", false),
                ("color_11_black", "Obsidian Dark", CosmeticRarity.Rare, new Color(0.15f, 0.16f, 0.2f), new Color(0.08f, 0.08f, 0.12f), new Color(0.4f, 0.45f, 0.55f), false, null, UnlockType.Coins, 400, "400 Coins", false),
                ("color_12_rainbow", "Prismatic Rainbow", CosmeticRarity.Legendary, Color.magenta, Color.cyan, Color.yellow, true, new Color[] { Color.red, new Color(1f, 0.5f, 0f), Color.yellow, Color.green, Color.cyan, Color.blue, Color.magenta }, UnlockType.Coins, 1500, "1,500 Coins", false),
                ("color_13_sunset", "Sunset Gradient", CosmeticRarity.Epic, new Color(1f, 0.35f, 0.3f), new Color(0.6f, 0.1f, 0.5f), Color.yellow, true, new Color[] { new Color(1f, 0.85f, 0.2f), new Color(1f, 0.4f, 0.2f), new Color(0.85f, 0.15f, 0.55f), new Color(0.4f, 0.1f, 0.6f) }, UnlockType.Coins, 800, "800 Coins", false),
                ("color_14_celestial", "Celestial Aura", CosmeticRarity.Mythic, Color.cyan, Color.magenta, Color.white, true, new Color[] { Color.white, Color.cyan, Color.magenta, new Color(0.3f, 0.1f, 0.8f) }, UnlockType.Coins, 2500, "2,500 Coins", false),
            };

            List<SkinColorData> list = new List<SkinColorData>();
            for (int i = 0; i < configs.Length; i++)
            {
                var cfg = configs[i];
                string assetPath = $"{resP}/{cfg.id}.asset";
                SkinColorData data = AssetDatabase.LoadAssetAtPath<SkinColorData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<SkinColorData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                data.ConfigureColor(cfg.id, cfg.name, cfg.rarity, null, cfg.p, cfg.s, cfg.a, cfg.grad, cfg.grads, cfg.unlock, cfg.price, cfg.req, cfg.def);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static List<PatternData> BuildPatternData()
        {
            string artP = $"{ArtCosmeticsPath}/Patterns";
            string resP = $"{CosmeticsResourcesPath}/Patterns";

            var configs = new (string id, string name, PatternType type, CosmeticRarity rarity, string file, float glow, bool sec, bool alt, UnlockType unlock, int price, string req, bool def)[]
            {
                ("pattern_01_solid", "Solid Smooth", PatternType.Solid, CosmeticRarity.Common, "Pattern_01_Solid.png", 1f, true, true, UnlockType.DefaultUnlocked, 0, "Default", true),
                ("pattern_02_stripes", "Racer Stripes", PatternType.Stripes, CosmeticRarity.Common, "Pattern_02_Stripes.png", 1f, true, true, UnlockType.Coins, 150, "150 Coins", false),
                ("pattern_03_spots", "Polka Spots", PatternType.Spots, CosmeticRarity.Common, "Pattern_03_Spots.png", 1f, true, true, UnlockType.Coins, 150, "150 Coins", false),
                ("pattern_04_gradient", "Smooth Gradient", PatternType.Gradient, CosmeticRarity.Uncommon, "Pattern_04_Gradient.png", 1f, true, true, UnlockType.Coins, 250, "250 Coins", false),
                ("pattern_05_checker", "Checkered Grid", PatternType.Checker, CosmeticRarity.Rare, "Pattern_05_Checker.png", 1f, true, true, UnlockType.Coins, 400, "400 Coins", false),
                ("pattern_06_glow", "Bioluminescent Glow", PatternType.Glow, CosmeticRarity.Epic, "Pattern_06_Glow.png", 1.8f, true, true, UnlockType.Coins, 750, "750 Coins", false),
                ("pattern_07_metallic", "Metallic Sheen", PatternType.Metallic, CosmeticRarity.Epic, "Pattern_07_Metallic.png", 1.2f, true, true, UnlockType.Coins, 900, "900 Coins", false),
                ("pattern_08_neon", "Cyber Neon", PatternType.Neon, CosmeticRarity.Legendary, "Pattern_08_Neon.png", 2.2f, true, true, UnlockType.Coins, 1400, "1,400 Coins", false),
            };

            List<PatternData> list = new List<PatternData>();
            for (int i = 0; i < configs.Length; i++)
            {
                var cfg = configs[i];
                string assetPath = $"{resP}/{cfg.id}.asset";
                PatternData data = AssetDatabase.LoadAssetAtPath<PatternData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<PatternData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                Sprite spr = LoadSprite($"{artP}/{cfg.file}");
                data.ConfigurePattern(cfg.id, cfg.name, cfg.rarity, spr, cfg.type, spr, cfg.glow, cfg.sec, cfg.alt, cfg.unlock, cfg.price, cfg.req, cfg.def);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static List<ClothingData> BuildClothingData()
        {
            string artP = $"{ArtCosmeticsPath}/Clothing";
            string resP = $"{CosmeticsResourcesPath}/Clothing";

            string[] names = new string[]
            {
                "TShirt", "Hoodie", "Jacket", "Suit", "Armor",
                "Jersey", "Pirate", "SpaceSuit", "Royal", "Winter",
                "Ninja", "LabCoat", "Robe", "CyberVest", "Tuxedo"
            };

            CosmeticRarity[] rarities = new CosmeticRarity[]
            {
                CosmeticRarity.Common, CosmeticRarity.Common, CosmeticRarity.Uncommon, CosmeticRarity.Rare, CosmeticRarity.Epic,
                CosmeticRarity.Common, CosmeticRarity.Uncommon, CosmeticRarity.Legendary, CosmeticRarity.Epic, CosmeticRarity.Rare,
                CosmeticRarity.Rare, CosmeticRarity.Uncommon, CosmeticRarity.Epic, CosmeticRarity.Legendary, CosmeticRarity.Mythic
            };

            int[] prices = new int[] { 100, 150, 250, 450, 750, 120, 300, 1200, 850, 400, 500, 350, 800, 1400, 2000 };

            List<ClothingData> list = new List<ClothingData>();
            for (int i = 0; i < names.Length; i++)
            {
                string id = $"clothing_{i + 1:D2}_{names[i].ToLower()}";
                string assetPath = $"{resP}/{id}.asset";
                ClothingData data = AssetDatabase.LoadAssetAtPath<ClothingData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<ClothingData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                Sprite collar = LoadSprite($"{artP}/Clothing_{i + 1:D2}_{names[i]}_Collar.png");
                Sprite seg = LoadSprite($"{artP}/Clothing_{i + 1:D2}_{names[i]}_Seg.png");

                int coverage = (i == 3 || i == 7 || i == 14) ? -1 : 8; // Full suits vs vests
                UnlockType unl = (i == 0) ? UnlockType.DefaultUnlocked : UnlockType.Coins;
                bool def = (i == 0);

                data.ConfigureClothing(id, names[i], rarities[i], seg, collar, seg, coverage, true, Color.white, unl, prices[i], $"{prices[i]} Coins", def);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static List<HeadAccessoryData> BuildHatData()
        {
            string artP = $"{ArtCosmeticsPath}/Hats";
            string resP = $"{CosmeticsResourcesPath}/Hats";

            string[] hats = new string[]
            {
                "Cap", "Crown", "Helmet", "Cowboy", "Wizard",
                "Party", "Headphones", "Sunglasses", "Halo", "Horns",
                "CatEars", "Antenna", "Viking", "Chef", "TopHat"
            };

            CosmeticRarity[] rarities = new CosmeticRarity[]
            {
                CosmeticRarity.Common, CosmeticRarity.Epic, CosmeticRarity.Rare, CosmeticRarity.Uncommon, CosmeticRarity.Epic,
                CosmeticRarity.Common, CosmeticRarity.Rare, CosmeticRarity.Uncommon, CosmeticRarity.Legendary, CosmeticRarity.Rare,
                CosmeticRarity.Uncommon, CosmeticRarity.Common, CosmeticRarity.Epic, CosmeticRarity.Common, CosmeticRarity.Legendary
            };

            int[] prices = new int[] { 100, 800, 450, 250, 750, 100, 500, 300, 1200, 400, 350, 150, 850, 200, 1500 };

            List<HeadAccessoryData> list = new List<HeadAccessoryData>();
            for (int i = 0; i < hats.Length; i++)
            {
                string id = $"hat_{i + 1:D2}_{hats[i].ToLower()}";
                string assetPath = $"{resP}/{id}.asset";
                HeadAccessoryData data = AssetDatabase.LoadAssetAtPath<HeadAccessoryData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<HeadAccessoryData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                Sprite spr = LoadSprite($"{artP}/Hat_{i + 1:D2}_{hats[i]}.png");
                Vector2 offset = (hats[i] == "Sunglasses") ? new Vector2(0f, 0.05f) : new Vector2(0f, 0.42f);
                UnlockType unl = (i == 0) ? UnlockType.DefaultUnlocked : UnlockType.Coins;

                data.ConfigureHat(id, hats[i], rarities[i], spr, spr, offset, Vector2.one, Color.white, unl, prices[i], $"{prices[i]} Coins", i == 0);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static List<FaceData> BuildEyesData()
        {
            string artP = $"{ArtCosmeticsPath}/Eyes";
            string resP = $"{CosmeticsResourcesPath}/Eyes";

            string[] eyeTypes = new string[] { "Normal", "Angry", "Happy", "Crazy", "Robot", "Sunglasses", "BigEyes", "CuteSparkle" };
            CosmeticRarity[] rarities = new CosmeticRarity[]
            {
                CosmeticRarity.Common, CosmeticRarity.Common, CosmeticRarity.Common, CosmeticRarity.Uncommon,
                CosmeticRarity.Rare, CosmeticRarity.Rare, CosmeticRarity.Epic, CosmeticRarity.Legendary
            };
            int[] prices = new int[] { 0, 80, 80, 150, 300, 350, 600, 1000 };

            List<FaceData> list = new List<FaceData>();
            for (int i = 0; i < eyeTypes.Length; i++)
            {
                string id = $"eyes_{i + 1:D2}_{eyeTypes[i].ToLower()}";
                string assetPath = $"{resP}/{id}.asset";
                FaceData data = AssetDatabase.LoadAssetAtPath<FaceData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<FaceData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                Sprite spr = LoadSprite($"{artP}/Eyes_{i + 1:D2}_{eyeTypes[i]}.png");
                data.ConfigureFace(id, eyeTypes[i], CosmeticCategory.Eyes, FaceFeatureType.Eyes, rarities[i], spr, spr, new Vector2(0f, 0.05f), Vector2.one, Color.white, (i == 0 ? UnlockType.DefaultUnlocked : UnlockType.Coins), prices[i], $"{prices[i]} Coins", i == 0);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static List<FaceData> BuildMouthData()
        {
            string artP = $"{ArtCosmeticsPath}/Mouths";
            string resP = $"{CosmeticsResourcesPath}/Mouths";

            string[] mouthTypes = new string[] { "Smile", "Open", "Tongue", "Angry", "Vampire", "Grin", "Whistle", "Mustache" };
            CosmeticRarity[] rarities = new CosmeticRarity[]
            {
                CosmeticRarity.Common, CosmeticRarity.Common, CosmeticRarity.Uncommon, CosmeticRarity.Common,
                CosmeticRarity.Rare, CosmeticRarity.Uncommon, CosmeticRarity.Rare, CosmeticRarity.Epic
            };
            int[] prices = new int[] { 0, 80, 150, 80, 350, 200, 300, 700 };

            List<FaceData> list = new List<FaceData>();
            for (int i = 0; i < mouthTypes.Length; i++)
            {
                string id = $"mouth_{i + 1:D2}_{mouthTypes[i].ToLower()}";
                string assetPath = $"{resP}/{id}.asset";
                FaceData data = AssetDatabase.LoadAssetAtPath<FaceData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<FaceData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                Sprite spr = LoadSprite($"{artP}/Mouth_{i + 1:D2}_{mouthTypes[i]}.png");
                data.ConfigureFace(id, mouthTypes[i], CosmeticCategory.Mouth, FaceFeatureType.Mouth, rarities[i], spr, spr, new Vector2(0f, -0.2f), Vector2.one, Color.white, (i == 0 ? UnlockType.DefaultUnlocked : UnlockType.Coins), prices[i], $"{prices[i]} Coins", i == 0);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static List<AccessoryData> BuildAccessoryData()
        {
            string artP = $"{ArtCosmeticsPath}/Accessories";
            string resP = $"{CosmeticsResourcesPath}/Accessories";

            string[] accs = new string[]
            {
                "Backpack", "Wings", "Cape", "Scarf", "Necklace",
                "Shield", "PetDrone", "FloatingStars", "TailSpikes", "TailRibbon",
                "FairyWings", "SpikedCollar", "Bowtie", "Jetpack", "TailFlame"
            };

            AccessorySlot[] slots = new AccessorySlot[]
            {
                AccessorySlot.Back, AccessorySlot.Back, AccessorySlot.Back, AccessorySlot.Neck, AccessorySlot.Neck,
                AccessorySlot.Back, AccessorySlot.Back, AccessorySlot.Head, AccessorySlot.Tail, AccessorySlot.Tail,
                AccessorySlot.Back, AccessorySlot.Neck, AccessorySlot.Neck, AccessorySlot.Back, AccessorySlot.Tail
            };

            CosmeticRarity[] rarities = new CosmeticRarity[]
            {
                CosmeticRarity.Common, CosmeticRarity.Epic, CosmeticRarity.Rare, CosmeticRarity.Common, CosmeticRarity.Uncommon,
                CosmeticRarity.Rare, CosmeticRarity.Legendary, CosmeticRarity.Epic, CosmeticRarity.Rare, CosmeticRarity.Uncommon,
                CosmeticRarity.Legendary, CosmeticRarity.Rare, CosmeticRarity.Common, CosmeticRarity.Mythic, CosmeticRarity.Epic
            };

            int[] prices = new int[] { 100, 800, 450, 150, 250, 400, 1200, 750, 500, 300, 1400, 450, 120, 2200, 850 };

            List<AccessoryData> list = new List<AccessoryData>();
            for (int i = 0; i < accs.Length; i++)
            {
                string id = $"accessory_{i + 1:D2}_{accs[i].ToLower()}";
                string assetPath = $"{resP}/{id}.asset";
                AccessoryData data = AssetDatabase.LoadAssetAtPath<AccessoryData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<AccessoryData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                Sprite spr = LoadSprite($"{artP}/Accessory_{i + 1:D2}_{accs[i]}.png");
                bool isTail = slots[i] == AccessorySlot.Tail;
                int sort = (slots[i] == AccessorySlot.Back) ? -5 : 5;

                data.ConfigureAccessory(id, accs[i], slots[i], rarities[i], spr, spr, Vector2.zero, Vector2.one, sort, Color.white, isTail, UnlockType.Coins, prices[i], $"{prices[i]} Coins", false);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }

        private static List<CosmeticEffectData> BuildEffectData()
        {
            string artP = $"{ArtCosmeticsPath}/Effects";
            string resP = $"{CosmeticsResourcesPath}/Effects";

            string[] effs = new string[]
            {
                "FireTrail", "Lightning", "RainbowTrail", "Smoke", "Sparkles",
                "Hearts", "Stars", "NeonGlow", "ShadowAura", "BubbleStream"
            };

            EffectType[] types = new EffectType[]
            {
                EffectType.FireTrail, EffectType.Lightning, EffectType.RainbowTrail, EffectType.Smoke, EffectType.Sparkles,
                EffectType.Hearts, EffectType.Stars, EffectType.NeonGlow, EffectType.ShadowAura, EffectType.BubbleStream
            };

            CosmeticRarity[] rarities = new CosmeticRarity[]
            {
                CosmeticRarity.Rare, CosmeticRarity.Epic, CosmeticRarity.Legendary, CosmeticRarity.Common, CosmeticRarity.Uncommon,
                CosmeticRarity.Rare, CosmeticRarity.Epic, CosmeticRarity.Epic, CosmeticRarity.Legendary, CosmeticRarity.Common
            };

            int[] prices = new int[] { 450, 750, 1500, 150, 300, 500, 800, 850, 1600, 200 };

            List<CosmeticEffectData> list = new List<CosmeticEffectData>();
            for (int i = 0; i < effs.Length; i++)
            {
                string id = $"effect_{i + 1:D2}_{effs[i].ToLower()}";
                string assetPath = $"{resP}/{id}.asset";
                CosmeticEffectData data = AssetDatabase.LoadAssetAtPath<CosmeticEffectData>(assetPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<CosmeticEffectData>();
                    AssetDatabase.CreateAsset(data, assetPath);
                }

                Sprite spr = LoadSprite($"{artP}/Effect_{i + 1:D2}_{effs[i]}.png");
                Color pCol = (types[i] == EffectType.FireTrail) ? new Color(1f, 0.4f, 0.1f) :
                             (types[i] == EffectType.Hearts) ? new Color(1f, 0.3f, 0.6f) :
                             (types[i] == EffectType.Lightning) ? Color.cyan : Color.white;

                data.ConfigureEffect(id, effs[i], rarities[i], spr, types[i], pCol, Color.yellow, spr, 12f, 0.35f, 0.6f, UnlockType.Coins, prices[i], $"{prices[i]} Coins", false);
                EditorUtility.SetDirty(data);
                list.Add(data);
            }
            return list;
        }
    }
}
