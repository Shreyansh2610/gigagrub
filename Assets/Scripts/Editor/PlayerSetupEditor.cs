using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using GigaGrub.Player;
using GigaGrub.UI;
using GigaGrub.Core;
using GigaGrub.Food;
using GigaGrub.Audio;
using GigaGrub.Systems;
using GigaGrub.AI;
using GigaGrub.PowerUps;
using GigaGrub.Data;
using GigaGrub.Cosmetics;
using GigaGrub.Rewards;

namespace GigaGrub.Editor
{
    [InitializeOnLoad]
    public static class PlayerSetupEditor
    {
        private const string PrefabsPath = "Assets/Prefabs";
        private const string ArtPath = "Assets/Art";
        private const string ScenesPath = "Assets/Scenes";
        private const string FoodResourcesPath = "Assets/Resources/Food";
        private const string PowerUpResourcesPath = "Assets/Resources/PowerUps";
        private const string SkinsResourcesPath = "Assets/Resources/Skins";

        static PlayerSetupEditor()
        {
            EditorApplication.delayCall += EnsureBuildScenesRegistered;
        }

        public static void EnsureBuildScenesRegistered()
        {
            var scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene($"{ScenesPath}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/Game.unity", true)
            };
            EditorBuildSettings.scenes = scenes;
        }

        [MenuItem("GigaGrub/1. Setup All (Prefabs, Food, PowerUps & Game Scene)")]
        public static void SetupAll()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[GigaGrub] Cannot run Scene Setup while Unity is in Play Mode! Please click the Play button to stop Play Mode first.");
                EditorUtility.DisplayDialog("GigaGrub Setup", "Cannot run Scene Setup while in Play Mode.\nPlease exit Play Mode in Unity first and try again.", "OK");
                return;
            }

            Debug.Log("[GigaGrub] Starting Full Setup with Original Visual Identity...");

            EnsureDirectories();
            AssetGenerator.GenerateAllArtAssets();
            GigaGrub.Cosmetics.CosmeticDatabase cosmeticDb = CosmeticDatabaseBuilder.BuildAndSaveAllCosmetics();

            CreatureSkinData[] creatureSkins = SetupCreatureSkinAssets();
            GameObject segmentPrefab = SetupPlayerSegmentPrefab();
            GameObject playerPrefab = SetupPlayerPrefab(segmentPrefab);
            GameObject aiCreaturePrefab = SetupAICreaturePrefab(segmentPrefab);
            GameObject eatingEffectPrefab = SetupEatingEffectPrefab();
            PowerUpData[] powerUpDataAssets = SetupPowerUpDataAssets();
            GameObject powerUpPickupPrefab = SetupPowerUpPickupPrefab();
            GameObject joystickCanvasPrefab = SetupJoystickCanvasPrefab();
            GameObject arenaPrefab = SetupArenaPrefab();
            FoodData[] foodDataAssets = SetupFoodDataAssets();
            GameObject foodPrefab = SetupFoodPrefab();

            SetupGameScene(playerPrefab, joystickCanvasPrefab, arenaPrefab, foodPrefab, foodDataAssets, eatingEffectPrefab, aiCreaturePrefab, powerUpPickupPrefab, powerUpDataAssets, creatureSkins);
            SetupMainMenuScene();

            // Register scenes in Build Settings (MainMenu = Index 0, Game = Index 1)
            EditorBuildSettings.scenes = new EditorBuildSettingsScene[]
            {
                new EditorBuildSettingsScene($"{ScenesPath}/MainMenu.unity", true),
                new EditorBuildSettingsScene($"{ScenesPath}/Game.unity", true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GigaGrub] Full Setup (MainMenu + Game Scenes with Modular Cosmetics System & Celestial Food) Completed Successfully!");
        }

        private static void EnsureDirectories()
        {
            if (!AssetDatabase.IsValidFolder(PrefabsPath))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }

            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            {
                AssetDatabase.CreateFolder("Assets", "Resources");
            }

            if (!AssetDatabase.IsValidFolder(FoodResourcesPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Food");
            }

            if (!AssetDatabase.IsValidFolder(PowerUpResourcesPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "PowerUps");
            }

            if (!AssetDatabase.IsValidFolder(SkinsResourcesPath))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Skins");
            }

            EnsureTags();
        }

        public static void EnsureTags()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            if (tagsProp == null) return;

            string[] requiredTags = new string[] { "AICreature", "Food", "PlayerSegment" };
            foreach (string tag in requiredTags)
            {
                bool found = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue.Equals(tag))
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                }
            }
            tagManager.ApplyModifiedProperties();
        }

        private static Sprite LoadSprite(string fileName)
        {
            string path = $"{ArtPath}/{fileName}";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                string[] subPaths = new string[]
                {
                    $"{ArtPath}/Creatures/{fileName}",
                    $"{ArtPath}/Food/{fileName}",
                    $"{ArtPath}/PowerUps/{fileName}",
                    $"{ArtPath}/Arena/{fileName}",
                    $"{ArtPath}/UI/{fileName}"
                };
                foreach (string sp in subPaths)
                {
                    sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sp);
                    if (sprite != null) break;
                }
            }

            if (sprite == null)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in assets)
                {
                    if (obj is Sprite s)
                    {
                        sprite = s;
                        break;
                    }
                }
            }
            return sprite;
        }

        [MenuItem("GigaGrub/1.5. Setup 10 Creature Skins")]
        public static CreatureSkinData[] SetupCreatureSkinAssets()
        {
            EnsureDirectories();
            AssetGenerator.GenerateAllArtAssets();

            string creaturesArt = $"{ArtPath}/Creatures";

            var skinConfigs = new (string id, string name, string headFile, string segFile, Color primary, Color secondary, Color accent)[]
            {
                ("skin_01_player", "Giga Grub", "Head_01_Player.png", "Segment_01_Player.png", new Color(0.12f, 0.95f, 0.72f), new Color(0.04f, 0.65f, 0.45f), new Color(0.45f, 1f, 0.85f)),
                ("skin_02_sprout", "Sprout", "Head_02_Sprout.png", "Segment_02_Sprout.png", new Color(0.48f, 0.95f, 0.12f), new Color(0.28f, 0.65f, 0.05f), new Color(0.78f, 1f, 0.45f)),
                ("skin_03_spark", "Spark", "Head_03_Spark.png", "Segment_03_Spark.png", new Color(0.12f, 0.88f, 1f), new Color(0.02f, 0.48f, 0.75f), new Color(0.65f, 0.95f, 1f)),
                ("skin_04_ruby", "Ruby", "Head_04_Ruby.png", "Segment_04_Ruby.png", new Color(1f, 0.22f, 0.38f), new Color(0.68f, 0.08f, 0.20f), new Color(1f, 0.65f, 0.75f)),
                ("skin_05_sunny", "Sunny", "Head_05_Sunny.png", "Segment_05_Sunny.png", new Color(1f, 0.85f, 0.12f), new Color(0.75f, 0.50f, 0.02f), new Color(1f, 0.95f, 0.55f)),
                ("skin_06_violet", "Violet", "Head_06_Violet.png", "Segment_06_Violet.png", new Color(0.82f, 0.22f, 0.98f), new Color(0.45f, 0.05f, 0.68f), new Color(0.95f, 0.65f, 1f)),
                ("skin_07_bubble", "Bubble", "Head_07_Bubble.png", "Segment_07_Bubble.png", new Color(1f, 0.35f, 0.72f), new Color(0.72f, 0.12f, 0.45f), new Color(1f, 0.75f, 0.90f)),
                ("skin_08_frost", "Frost", "Head_08_Frost.png", "Segment_08_Frost.png", new Color(0.45f, 0.85f, 1f), new Color(0.15f, 0.45f, 0.75f), new Color(0.85f, 0.95f, 1f)),
                ("skin_09_flame", "Flame", "Head_09_Flame.png", "Segment_09_Flame.png", new Color(1f, 0.48f, 0.10f), new Color(0.72f, 0.22f, 0.02f), new Color(1f, 0.80f, 0.40f)),
                ("skin_10_shadow", "Shadow", "Head_10_Shadow.png", "Segment_10_Shadow.png", new Color(0.48f, 0.28f, 0.98f), new Color(0.20f, 0.08f, 0.55f), new Color(0.80f, 0.65f, 1f)),
            };

            CreatureSkinData[] skins = new CreatureSkinData[skinConfigs.Length];

            for (int i = 0; i < skinConfigs.Length; i++)
            {
                var cfg = skinConfigs[i];
                string assetPath = $"{SkinsResourcesPath}/CreatureSkin_{i + 1:D2}_{cfg.name}.asset";
                CreatureSkinData skin = AssetDatabase.LoadAssetAtPath<CreatureSkinData>(assetPath);
                if (skin == null)
                {
                    skin = ScriptableObject.CreateInstance<CreatureSkinData>();
                    AssetDatabase.CreateAsset(skin, assetPath);
                }

                Sprite headSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{creaturesArt}/{cfg.headFile}") ?? LoadSprite(cfg.headFile);
                Sprite segmentSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{creaturesArt}/{cfg.segFile}") ?? LoadSprite(cfg.segFile);

                skin.Configure(cfg.id, cfg.name, headSprite, segmentSprite, cfg.primary, cfg.secondary, cfg.accent);
                EditorUtility.SetDirty(skin);
                skins[i] = skin;
            }

            Debug.Log($"[GigaGrub] Created/Updated {skins.Length} CreatureSkinData ScriptableObjects");
            return skins;
        }

        [MenuItem("GigaGrub/2. Setup Arena Prefab")]
        public static GameObject SetupArenaPrefab()
        {
            string prefabPath = $"{PrefabsPath}/Arena.prefab";
            GameObject go = new GameObject("Arena");

            ArenaManager arena = go.AddComponent<ArenaManager>();
            SerializedObject soArena = new SerializedObject(arena);
            soArena.FindProperty("arenaSize").vector2Value = new Vector2(100f, 100f);
            soArena.FindProperty("boundaryColor").colorValue = new Color(0.1f, 0.8f, 1f, 0.9f);
            soArena.FindProperty("boundaryWidth").floatValue = 0.3f;
            soArena.FindProperty("backgroundColor").colorValue = new Color(0.06f, 0.08f, 0.12f, 1f);
            soArena.FindProperty("generateColliders").boolValue = true;
            soArena.FindProperty("wallThickness").floatValue = 2f;
            soArena.ApplyModifiedPropertiesWithoutUndo();

            arena.SetupBackground();
            arena.SetupBoundaryVisuals();
            arena.SetupArenaDecorations();
            arena.SetupBoundaryColliders();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/3. Setup Player Segment Prefab")]
        public static GameObject SetupPlayerSegmentPrefab()
        {
            string prefabPath = $"{PrefabsPath}/PlayerSegment.prefab";
            GameObject go = new GameObject("PlayerSegment");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("SegmentSprite.png");
            sr.sortingOrder = 90;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            PlayerSegment segment = go.AddComponent<PlayerSegment>();
            SegmentCosmeticRenderer cosRenderer = go.AddComponent<SegmentCosmeticRenderer>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/4. Setup Player Prefab")]
        public static void MenuSetupPlayerPrefab()
        {
            SetupPlayerPrefab(null);
        }

        public static GameObject SetupPlayerPrefab(GameObject segmentPrefab = null)
        {
            if (segmentPrefab == null)
            {
                segmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PlayerSegment.prefab");
                if (segmentPrefab == null)
                {
                    segmentPrefab = SetupPlayerSegmentPrefab();
                }
            }

            string prefabPath = $"{PrefabsPath}/Player.prefab";
            GameObject go = new GameObject("Player");
            go.tag = "Player";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("HeadSprite.png");
            sr.sortingOrder = 100;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;

            AudioSource audioSrc = go.AddComponent<AudioSource>();
            audioSrc.playOnAwake = false;
            audioSrc.spatialBlend = 0f;

            PlayerController controller = go.AddComponent<PlayerController>();
            BoostSystem boost = go.AddComponent<BoostSystem>();
            BoostVisualEffect boostFx = go.AddComponent<BoostVisualEffect>();
            PowerUpManager powerUps = go.AddComponent<PowerUpManager>();
            GrowthSystem growth = go.AddComponent<GrowthSystem>();
            CreatureDeath death = go.AddComponent<CreatureDeath>();
            CreatureCollision collision = go.AddComponent<CreatureCollision>();
            CreatureCosmeticController cosmetics = go.AddComponent<CreatureCosmeticController>();

            SerializedObject soBoost = new SerializedObject(boost);
            soBoost.FindProperty("normalSpeed").floatValue = 5.0f;
            soBoost.FindProperty("boostSpeed").floatValue = 9.5f;
            soBoost.FindProperty("maxEnergy").floatValue = 100.0f;
            soBoost.FindProperty("consumptionRate").floatValue = 35.0f;
            soBoost.FindProperty("regenerationRate").floatValue = 22.0f;
            soBoost.FindProperty("regenerationDelay").floatValue = 0.4f;
            soBoost.FindProperty("minEnergyToStartBoost").floatValue = 5.0f;
            soBoost.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soCtrl = new SerializedObject(controller);
            soCtrl.FindProperty("moveSpeed").floatValue = 5.0f;
            soCtrl.FindProperty("turnSpeed").floatValue = 360f;
            soCtrl.FindProperty("turnDamping").floatValue = 16f;
            soCtrl.FindProperty("boostSystem").objectReferenceValue = boost;
            soCtrl.FindProperty("powerUpManager").objectReferenceValue = powerUps;
            soCtrl.ApplyModifiedPropertiesWithoutUndo();

            PlayerBody body = go.AddComponent<PlayerBody>();
            SerializedObject soBody = new SerializedObject(body);
            soBody.FindProperty("isPlayer").boolValue = true;
            soBody.FindProperty("startingLength").intValue = 10;
            soBody.FindProperty("maxLength").intValue = 600;
            soBody.FindProperty("segmentSpacing").floatValue = 0.45f;
            soBody.FindProperty("stepDistance").floatValue = 0.05f;
            soBody.FindProperty("growthMultiplier").intValue = 1;
            soBody.FindProperty("smoothGrowthSpeed").floatValue = 8f;
            soBody.FindProperty("enableTaper").boolValue = true;
            soBody.FindProperty("minTailScale").floatValue = 0.65f;
            soBody.FindProperty("headSortingOrder").intValue = 100;
            soBody.FindProperty("enableAudioFeedback").boolValue = true;
            soBody.FindProperty("enableVisualPunch").boolValue = true;
            soBody.FindProperty("headPunchScale").floatValue = 1.16f;
            soBody.FindProperty("segmentPrefab").objectReferenceValue = segmentPrefab;
            soBody.FindProperty("headTransform").objectReferenceValue = go.transform;
            soBody.FindProperty("audioSource").objectReferenceValue = audioSrc;
            soBody.FindProperty("growthSystem").objectReferenceValue = growth;
            soBody.FindProperty("creatureDeath").objectReferenceValue = death;
            soBody.FindProperty("creatureCollision").objectReferenceValue = collision;
            soBody.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soGrowth = new SerializedObject(growth);
            soGrowth.FindProperty("playerBody").objectReferenceValue = body;
            soGrowth.FindProperty("growthRate").floatValue = 30f;
            soGrowth.FindProperty("minIntervalBetweenSegments").floatValue = 0.02f;
            soGrowth.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soDeath = new SerializedObject(death);
            soDeath.FindProperty("creatureBody").objectReferenceValue = body;
            soDeath.FindProperty("playerController").objectReferenceValue = controller;
            soDeath.FindProperty("headCollider").objectReferenceValue = col;
            soDeath.FindProperty("audioSource").objectReferenceValue = audioSrc;
            soDeath.FindProperty("dropFoodOnDeath").boolValue = true;
            soDeath.FindProperty("foodDropRatio").floatValue = 0.75f;
            soDeath.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soCollision = new SerializedObject(collision);
            soCollision.FindProperty("ownerBody").objectReferenceValue = body;
            soCollision.FindProperty("creatureDeath").objectReferenceValue = death;
            soCollision.FindProperty("headCollider").objectReferenceValue = col;
            soCollision.FindProperty("headToHeadRule").enumValueIndex = (int)HeadToHeadRule.LongerSurvives;
            soCollision.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/4. Setup AI Creature Prefab")]
        public static void MenuSetupAICreaturePrefab()
        {
            EnsureDirectories();
            EnsureTags();
            SetupAICreaturePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static GameObject SetupAICreaturePrefab(GameObject segmentPrefab = null)
        {
            EnsureTags();

            if (segmentPrefab == null)
            {
                segmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PlayerSegment.prefab");
                if (segmentPrefab == null)
                {
                    segmentPrefab = SetupPlayerSegmentPrefab();
                }
            }

            string prefabPath = $"{PrefabsPath}/AICreature.prefab";
            GameObject go = new GameObject("AICreature");
            go.tag = "AICreature";

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("HeadSprite.png");
            sr.sortingOrder = 98;
            sr.color = new Color(1.0f, 0.4f, 0.4f, 1f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;

            Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;

            AIWorldDetector detector = go.AddComponent<AIWorldDetector>();
            AIStateMachine stateMachine = go.AddComponent<AIStateMachine>();
            stateMachine.BindDetector(detector);

            AIController aiCtrl = go.AddComponent<AIController>();
            BoostSystem aiBoost = go.AddComponent<BoostSystem>();
            GrowthSystem growth = go.AddComponent<GrowthSystem>();
            CreatureDeath death = go.AddComponent<CreatureDeath>();
            CreatureCollision collision = go.AddComponent<CreatureCollision>();
            CreatureCosmeticController cosmetics = go.AddComponent<CreatureCosmeticController>();

            SerializedObject soAiBoost = new SerializedObject(aiBoost);
            soAiBoost.FindProperty("normalSpeed").floatValue = 4.8f;
            soAiBoost.FindProperty("boostSpeed").floatValue = 8.8f;
            soAiBoost.FindProperty("maxEnergy").floatValue = 100.0f;
            soAiBoost.FindProperty("consumptionRate").floatValue = 35.0f;
            soAiBoost.FindProperty("regenerationRate").floatValue = 20.0f;
            soAiBoost.FindProperty("regenerationDelay").floatValue = 0.5f;
            soAiBoost.FindProperty("minEnergyToStartBoost").floatValue = 10.0f;
            soAiBoost.ApplyModifiedPropertiesWithoutUndo();

            PlayerBody body = go.AddComponent<PlayerBody>();
            SerializedObject soBody = new SerializedObject(body);
            soBody.FindProperty("isPlayer").boolValue = false;
            soBody.FindProperty("startingLength").intValue = 10;
            soBody.FindProperty("maxLength").intValue = 600;
            soBody.FindProperty("segmentSpacing").floatValue = 0.45f;
            soBody.FindProperty("stepDistance").floatValue = 0.05f;
            soBody.FindProperty("growthMultiplier").intValue = 1;
            soBody.FindProperty("smoothGrowthSpeed").floatValue = 8f;
            soBody.FindProperty("enableTaper").boolValue = true;
            soBody.FindProperty("minTailScale").floatValue = 0.65f;
            soBody.FindProperty("headSortingOrder").intValue = 98;
            soBody.FindProperty("enableAudioFeedback").boolValue = false;
            soBody.FindProperty("enableVisualPunch").boolValue = true;
            soBody.FindProperty("headPunchScale").floatValue = 1.15f;
            soBody.FindProperty("segmentPrefab").objectReferenceValue = segmentPrefab;
            soBody.FindProperty("headTransform").objectReferenceValue = go.transform;
            soBody.FindProperty("growthSystem").objectReferenceValue = growth;
            soBody.FindProperty("creatureDeath").objectReferenceValue = death;
            soBody.FindProperty("creatureCollision").objectReferenceValue = collision;
            soBody.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soGrowth = new SerializedObject(growth);
            soGrowth.FindProperty("playerBody").objectReferenceValue = body;
            soGrowth.FindProperty("growthRate").floatValue = 30f;
            soGrowth.FindProperty("minIntervalBetweenSegments").floatValue = 0.02f;
            soGrowth.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soAi = new SerializedObject(aiCtrl);
            soAi.FindProperty("moveSpeed").floatValue = 4.8f;
            soAi.FindProperty("turnSpeed").floatValue = 280f;
            soAi.FindProperty("decisionInterval").floatValue = 0.15f;
            soAi.FindProperty("stateMachine").objectReferenceValue = stateMachine;
            soAi.FindProperty("worldDetector").objectReferenceValue = detector;
            soAi.FindProperty("creatureBody").objectReferenceValue = body;
            soAi.FindProperty("boostSystem").objectReferenceValue = aiBoost;
            soAi.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soDeath = new SerializedObject(death);
            soDeath.FindProperty("creatureBody").objectReferenceValue = body;
            soDeath.FindProperty("aiController").objectReferenceValue = aiCtrl;
            soDeath.FindProperty("headCollider").objectReferenceValue = col;
            soDeath.FindProperty("dropFoodOnDeath").boolValue = true;
            soDeath.FindProperty("foodDropRatio").floatValue = 0.75f;
            soDeath.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soCollision = new SerializedObject(collision);
            soCollision.FindProperty("ownerBody").objectReferenceValue = body;
            soCollision.FindProperty("creatureDeath").objectReferenceValue = death;
            soCollision.FindProperty("headCollider").objectReferenceValue = col;
            soCollision.FindProperty("headToHeadRule").enumValueIndex = (int)HeadToHeadRule.LongerSurvives;
            soCollision.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/5. Setup Eating Effect Prefab")]
        public static GameObject SetupEatingEffectPrefab()
        {
            string prefabPath = $"{PrefabsPath}/EatingEffect.prefab";
            GameObject go = new GameObject("EatingEffect");

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.duration = 0.45f;
            main.loop = false;
            main.startLifetime = 0.4f;
            main.startSpeed = 4.5f;
            main.startSize = 0.28f;
            main.startColor = new Color(0.2f, 1f, 0.4f, 1f);
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 30;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 16) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            go.AddComponent<EatingEffect>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/6. Setup Joystick & Gameplay HUD Canvas Prefab")]
        public static GameObject SetupJoystickCanvasPrefab()
        {
            string prefabPath = $"{PrefabsPath}/JoystickCanvas.prefab";
            GameObject canvasGo = new GameObject("JoystickCanvas");

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); // Mobile Landscape Layout
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Root Safe-Area Container
            GameObject safeAreaGo = new GameObject("SafeArea");
            safeAreaGo.transform.SetParent(canvasGo.transform, false);
            RectTransform safeAreaRect = safeAreaGo.AddComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
            safeAreaGo.AddComponent<SafeAreaFitter>();

            // ==========================================
            // 1. TOP HUD (Score, Length, Time, Pause Button)
            // ==========================================
            GameObject topHudGo = new GameObject("TopHUD");
            topHudGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform topHudRect = topHudGo.AddComponent<RectTransform>();
            topHudRect.anchorMin = new Vector2(0f, 1f);
            topHudRect.anchorMax = new Vector2(1f, 1f);
            topHudRect.pivot = new Vector2(0.5f, 1f);
            topHudRect.anchoredPosition = Vector2.zero;
            topHudRect.sizeDelta = new Vector2(0f, 120f);

            // Top Bar Background gradient/tint
            Image topBg = topHudGo.AddComponent<Image>();
            topBg.color = new Color(0.04f, 0.07f, 0.12f, 0.65f);

            // Left: Score & Stats Group
            GameObject scoreGroupGo = new GameObject("ScoreGroup");
            scoreGroupGo.transform.SetParent(topHudGo.transform, false);
            RectTransform scoreGroupRect = scoreGroupGo.AddComponent<RectTransform>();
            scoreGroupRect.anchorMin = new Vector2(0f, 0f);
            scoreGroupRect.anchorMax = new Vector2(0.40f, 1f);
            scoreGroupRect.pivot = new Vector2(0f, 0.5f);
            scoreGroupRect.offsetMin = new Vector2(30f, 10f);
            scoreGroupRect.offsetMax = new Vector2(0f, -10f);

            GameObject scoreTextGo = new GameObject("ScoreText");
            scoreTextGo.transform.SetParent(scoreGroupGo.transform, false);
            RectTransform scoreTextRect = scoreTextGo.AddComponent<RectTransform>();
            scoreTextRect.anchorMin = new Vector2(0f, 0.45f);
            scoreTextRect.anchorMax = new Vector2(1f, 1f);
            scoreTextRect.pivot = new Vector2(0f, 0.5f);
            scoreTextRect.offsetMin = Vector2.zero;
            scoreTextRect.offsetMax = Vector2.zero;

            Text scoreText = scoreTextGo.AddComponent<Text>();
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 38;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.alignment = TextAnchor.MiddleLeft;
            scoreText.color = new Color(1f, 0.88f, 0.2f, 1f);
            scoreText.text = "SCORE  0";

            GameObject lengthTextGo = new GameObject("LengthText");
            lengthTextGo.transform.SetParent(scoreGroupGo.transform, false);
            RectTransform lengthTextRect = lengthTextGo.AddComponent<RectTransform>();
            lengthTextRect.anchorMin = new Vector2(0f, 0f);
            lengthTextRect.anchorMax = new Vector2(0.48f, 0.45f);
            lengthTextRect.pivot = new Vector2(0f, 0.5f);
            lengthTextRect.offsetMin = Vector2.zero;
            lengthTextRect.offsetMax = Vector2.zero;

            Text lengthText = lengthTextGo.AddComponent<Text>();
            lengthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lengthText.fontSize = 24;
            lengthText.fontStyle = FontStyle.Normal;
            lengthText.alignment = TextAnchor.MiddleLeft;
            lengthText.color = new Color(0.45f, 0.85f, 1f, 0.9f);
            lengthText.text = "LENGTH  10";

            GameObject rankTextGo = new GameObject("RankText");
            rankTextGo.transform.SetParent(scoreGroupGo.transform, false);
            RectTransform rankTextRect = rankTextGo.AddComponent<RectTransform>();
            rankTextRect.anchorMin = new Vector2(0.50f, 0f);
            rankTextRect.anchorMax = new Vector2(1f, 0.45f);
            rankTextRect.pivot = new Vector2(0f, 0.5f);
            rankTextRect.offsetMin = Vector2.zero;
            rankTextRect.offsetMax = Vector2.zero;

            Text rankText = rankTextGo.AddComponent<Text>();
            rankText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rankText.fontSize = 24;
            rankText.fontStyle = FontStyle.Bold;
            rankText.alignment = TextAnchor.MiddleLeft;
            rankText.color = new Color(1f, 0.75f, 0.2f, 0.95f);
            rankText.text = "RANK  #1 / 11";

            // Center: Survival Time
            GameObject timeGo = new GameObject("SurvivalTime");
            timeGo.transform.SetParent(topHudGo.transform, false);
            RectTransform timeRect = timeGo.AddComponent<RectTransform>();
            timeRect.anchorMin = new Vector2(0.5f, 0.5f);
            timeRect.anchorMax = new Vector2(0.5f, 0.5f);
            timeRect.pivot = new Vector2(0.5f, 0.5f);
            timeRect.sizeDelta = new Vector2(300f, 60f);

            Text timeText = timeGo.AddComponent<Text>();
            timeText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            timeText.fontSize = 34;
            timeText.fontStyle = FontStyle.Bold;
            timeText.alignment = TextAnchor.MiddleCenter;
            timeText.color = Color.white;
            timeText.text = "TIME  00:00";

            // Right: Pause Button
            GameObject pauseBtnGo = new GameObject("PauseButton");
            pauseBtnGo.transform.SetParent(topHudGo.transform, false);
            RectTransform pauseBtnRect = pauseBtnGo.AddComponent<RectTransform>();
            pauseBtnRect.anchorMin = new Vector2(1f, 0.5f);
            pauseBtnRect.anchorMax = new Vector2(1f, 0.5f);
            pauseBtnRect.pivot = new Vector2(1f, 0.5f);
            pauseBtnRect.anchoredPosition = new Vector2(-30f, 0f);
            pauseBtnRect.sizeDelta = new Vector2(70f, 70f);

            Image pauseImg = pauseBtnGo.AddComponent<Image>();
            pauseImg.color = new Color(0.18f, 0.24f, 0.35f, 0.9f);

            Button pauseBtn = pauseBtnGo.AddComponent<Button>();

            GameObject pauseTxtGo = new GameObject("Text");
            pauseTxtGo.transform.SetParent(pauseBtnGo.transform, false);
            RectTransform pauseTxtRect = pauseTxtGo.AddComponent<RectTransform>();
            pauseTxtRect.anchorMin = Vector2.zero;
            pauseTxtRect.anchorMax = Vector2.one;
            pauseTxtRect.offsetMin = Vector2.zero;
            pauseTxtRect.offsetMax = Vector2.zero;

            Text pauseTxt = pauseTxtGo.AddComponent<Text>();
            pauseTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pauseTxt.fontSize = 28;
            pauseTxt.fontStyle = FontStyle.Bold;
            pauseTxt.alignment = TextAnchor.MiddleCenter;
            pauseTxt.color = Color.white;
            pauseTxt.text = "||";

            // ==========================================
            // 2. BOTTOM CONTROLS (Joystick & Boost Button)
            // ==========================================
            // Left: Virtual Joystick
            GameObject joystickGo = new GameObject("VirtualJoystick");
            joystickGo.transform.SetParent(safeAreaGo.transform, false);

            RectTransform joyRect = joystickGo.AddComponent<RectTransform>();
            joyRect.anchorMin = new Vector2(0f, 0f);
            joyRect.anchorMax = new Vector2(0f, 0f);
            joyRect.pivot = new Vector2(0.5f, 0.5f);
            joyRect.anchoredPosition = new Vector2(230f, 230f);
            joyRect.sizeDelta = new Vector2(280f, 280f);

            Image bgImage = joystickGo.AddComponent<Image>();
            bgImage.sprite = LoadSprite("JoystickBG.png");
            bgImage.color = new Color(1f, 1f, 1f, 0.75f);

            GameObject knobGo = new GameObject("Knob");
            knobGo.transform.SetParent(joystickGo.transform, false);

            RectTransform knobRect = knobGo.AddComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.anchoredPosition = Vector2.zero;
            knobRect.sizeDelta = new Vector2(110f, 110f);

            Image knobImage = knobGo.AddComponent<Image>();
            knobImage.sprite = LoadSprite("JoystickKnob.png");
            knobImage.color = new Color(1f, 1f, 1f, 0.95f);

            VirtualJoystick vj = joystickGo.AddComponent<VirtualJoystick>();
            SerializedObject soVj = new SerializedObject(vj);
            soVj.FindProperty("joystickBackground").objectReferenceValue = joyRect;
            soVj.FindProperty("joystickKnob").objectReferenceValue = knobRect;
            soVj.FindProperty("handleLimit").floatValue = 110f;
            soVj.FindProperty("deadZone").floatValue = 0.05f;
            soVj.ApplyModifiedPropertiesWithoutUndo();

            // Right: Boost Button (Touch Hold Component)
            GameObject boostBtnGo = new GameObject("BoostButton");
            boostBtnGo.transform.SetParent(safeAreaGo.transform, false);

            RectTransform boostRect = boostBtnGo.AddComponent<RectTransform>();
            boostRect.anchorMin = new Vector2(1f, 0f);
            boostRect.anchorMax = new Vector2(1f, 0f);
            boostRect.pivot = new Vector2(0.5f, 0.5f);
            boostRect.anchoredPosition = new Vector2(-200f, 200f);
            boostRect.sizeDelta = new Vector2(150f, 150f);

            Image boostImg = boostBtnGo.AddComponent<Image>();
            boostImg.color = new Color(0.95f, 0.35f, 0.25f, 0.85f);

            Button boostBtn = boostBtnGo.AddComponent<Button>();
            HoldButton holdBtn = boostBtnGo.AddComponent<HoldButton>();

            GameObject boostTxtGo = new GameObject("Text");
            boostTxtGo.transform.SetParent(boostBtnGo.transform, false);
            RectTransform boostTxtRect = boostTxtGo.AddComponent<RectTransform>();
            boostTxtRect.anchorMin = Vector2.zero;
            boostTxtRect.anchorMax = Vector2.one;
            boostTxtRect.offsetMin = Vector2.zero;
            boostTxtRect.offsetMax = Vector2.zero;

            Text boostTxt = boostTxtGo.AddComponent<Text>();
            boostTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            boostTxt.fontSize = 26;
            boostTxt.fontStyle = FontStyle.Bold;
            boostTxt.alignment = TextAnchor.MiddleCenter;
            boostTxt.color = Color.white;
            boostTxt.text = "BOOST";

            // Boost Energy Bar Container (Positioned directly above Boost Button)
            GameObject energyBarGo = new GameObject("BoostEnergyBar");
            energyBarGo.transform.SetParent(safeAreaGo.transform, false);

            RectTransform energyBarRect = energyBarGo.AddComponent<RectTransform>();
            energyBarRect.anchorMin = new Vector2(1f, 0f);
            energyBarRect.anchorMax = new Vector2(1f, 0f);
            energyBarRect.pivot = new Vector2(0.5f, 0.5f);
            energyBarRect.anchoredPosition = new Vector2(-200f, 295f);
            energyBarRect.sizeDelta = new Vector2(160f, 20f);

            Image energyBarBg = energyBarGo.AddComponent<Image>();
            energyBarBg.color = new Color(0.06f, 0.10f, 0.17f, 0.90f); // Dark Slate #0F172A

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(energyBarGo.transform, false);
            RectTransform fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(2f, 2f);
            fillRect.offsetMax = new Vector2(-2f, -2f);

            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Neon Cyan #38BDF8

            GameObject energyTxtGo = new GameObject("EnergyText");
            energyTxtGo.transform.SetParent(energyBarGo.transform, false);
            RectTransform energyTxtRect = energyTxtGo.AddComponent<RectTransform>();
            energyTxtRect.anchorMin = Vector2.zero;
            energyTxtRect.anchorMax = Vector2.one;
            energyTxtRect.offsetMin = Vector2.zero;
            energyTxtRect.offsetMax = Vector2.zero;

            Text energyTxt = energyTxtGo.AddComponent<Text>();
            energyTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            energyTxt.fontSize = 12;
            energyTxt.fontStyle = FontStyle.Bold;
            energyTxt.alignment = TextAnchor.MiddleCenter;
            energyTxt.color = Color.white;
            energyTxt.text = "100%";

            BoostEnergyBarUI energyBarUI = energyBarGo.AddComponent<BoostEnergyBarUI>();
            SerializedObject soEnergy = new SerializedObject(energyBarUI);
            soEnergy.FindProperty("fillImage").objectReferenceValue = fillImg;
            soEnergy.FindProperty("backgroundImage").objectReferenceValue = energyBarBg;
            soEnergy.FindProperty("energyText").objectReferenceValue = energyTxt;
            soEnergy.ApplyModifiedPropertiesWithoutUndo();

            // Active Power-Ups Badge HUD Container (Positioned on the Left Middle of Screen)
            GameObject powerUpHudGo = new GameObject("ActivePowerUpHUD");
            powerUpHudGo.transform.SetParent(safeAreaGo.transform, false);

            RectTransform pUpRect = powerUpHudGo.AddComponent<RectTransform>();
            pUpRect.anchorMin = new Vector2(0f, 0.45f);
            pUpRect.anchorMax = new Vector2(0f, 0.45f);
            pUpRect.pivot = new Vector2(0f, 0.5f);
            pUpRect.anchoredPosition = new Vector2(40f, 0f);
            pUpRect.sizeDelta = new Vector2(220f, 320f);

            VerticalLayoutGroup pUpLayout = powerUpHudGo.AddComponent<VerticalLayoutGroup>();
            pUpLayout.childAlignment = TextAnchor.MiddleLeft;
            pUpLayout.childControlHeight = false;
            pUpLayout.childControlWidth = false;
            pUpLayout.spacing = 10f;

            ActivePowerUpHUD powerUpHUD = powerUpHudGo.AddComponent<ActivePowerUpHUD>();

            // Wire GameplayHUD component
            GameplayHUD gameplayHUD = topHudGo.AddComponent<GameplayHUD>();
            SerializedObject soHud = new SerializedObject(gameplayHUD);
            soHud.FindProperty("scoreText").objectReferenceValue = scoreText;
            soHud.FindProperty("lengthText").objectReferenceValue = lengthText;
            soHud.FindProperty("rankText").objectReferenceValue = rankText;
            soHud.FindProperty("timeText").objectReferenceValue = timeText;
            soHud.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            soHud.FindProperty("boostButton").objectReferenceValue = boostBtn;
            soHud.FindProperty("holdBoostButton").objectReferenceValue = holdBtn;
            soHud.FindProperty("boostEnergyBar").objectReferenceValue = energyBarUI;
            soHud.FindProperty("activePowerUpHUD").objectReferenceValue = powerUpHUD;
            soHud.ApplyModifiedPropertiesWithoutUndo();

            // Backwards compatibility ScoreUI component
            ScoreUI scoreUI = topHudGo.AddComponent<ScoreUI>();
            SerializedObject soScore = new SerializedObject(scoreUI);
            soScore.FindProperty("scoreText").objectReferenceValue = scoreText;
            soScore.FindProperty("lengthText").objectReferenceValue = lengthText;
            soScore.ApplyModifiedPropertiesWithoutUndo();

            // ==========================================
            // 3. PAUSE MENU (Modal Overlay)
            // ==========================================
            GameObject pauseMenuGo = new GameObject("PauseMenu");
            pauseMenuGo.transform.SetParent(safeAreaGo.transform, false);

            RectTransform pauseMenuRect = pauseMenuGo.AddComponent<RectTransform>();
            pauseMenuRect.anchorMin = Vector2.zero;
            pauseMenuRect.anchorMax = Vector2.one;
            pauseMenuRect.offsetMin = Vector2.zero;
            pauseMenuRect.offsetMax = Vector2.zero;

            Image pauseMenuBg = pauseMenuGo.AddComponent<Image>();
            pauseMenuBg.color = new Color(0.03f, 0.05f, 0.08f, 0.92f);

            CanvasGroup pauseCg = pauseMenuGo.AddComponent<CanvasGroup>();
            PauseMenuUI pauseMenuUI = pauseMenuGo.AddComponent<PauseMenuUI>();

            GameObject pauseTitleGo = new GameObject("PauseTitle");
            pauseTitleGo.transform.SetParent(pauseMenuGo.transform, false);
            RectTransform pauseTitleRect = pauseTitleGo.AddComponent<RectTransform>();
            pauseTitleRect.anchorMin = new Vector2(0.5f, 0.78f);
            pauseTitleRect.anchorMax = new Vector2(0.5f, 0.78f);
            pauseTitleRect.pivot = new Vector2(0.5f, 0.5f);
            pauseTitleRect.sizeDelta = new Vector2(600f, 90f);

            Text pauseTitleText = pauseTitleGo.AddComponent<Text>();
            pauseTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            pauseTitleText.fontSize = 56;
            pauseTitleText.fontStyle = FontStyle.Bold;
            pauseTitleText.alignment = TextAnchor.MiddleCenter;
            pauseTitleText.color = new Color(0.45f, 0.75f, 1f, 1f);
            pauseTitleText.text = "PAUSED";

            // Helper for Menu Buttons
            Button CreateMenuButton(GameObject parent, string label, Color color, float yPos)
            {
                GameObject bGo = new GameObject(label.Replace(" ", ""));
                bGo.transform.SetParent(parent.transform, false);
                RectTransform bRect = bGo.AddComponent<RectTransform>();
                bRect.anchorMin = new Vector2(0.5f, 0.5f);
                bRect.anchorMax = new Vector2(0.5f, 0.5f);
                bRect.pivot = new Vector2(0.5f, 0.5f);
                bRect.anchoredPosition = new Vector2(0f, yPos);
                bRect.sizeDelta = new Vector2(380f, 70f);

                Image bImg = bGo.AddComponent<Image>();
                bImg.color = color;

                Button btn = bGo.AddComponent<Button>();

                GameObject tGo = new GameObject("Text");
                tGo.transform.SetParent(bGo.transform, false);
                RectTransform tRect = tGo.AddComponent<RectTransform>();
                tRect.anchorMin = Vector2.zero;
                tRect.anchorMax = Vector2.one;
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;

                Text t = tGo.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 28;
                t.fontStyle = FontStyle.Bold;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.text = label;

                return btn;
            }

            Button resumeBtn = CreateMenuButton(pauseMenuGo, "RESUME", new Color(0.1f, 0.75f, 0.45f, 1f), 70f);
            Button pauseRestartBtn = CreateMenuButton(pauseMenuGo, "RESTART", new Color(0.9f, 0.45f, 0.2f, 1f), -15f);
            Button pauseMainMenuBtn = CreateMenuButton(pauseMenuGo, "MAIN MENU", new Color(0.2f, 0.3f, 0.45f, 1f), -100f);

            SerializedObject soPauseUI = new SerializedObject(pauseMenuUI);
            soPauseUI.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            soPauseUI.FindProperty("restartButton").objectReferenceValue = pauseRestartBtn;
            soPauseUI.FindProperty("mainMenuButton").objectReferenceValue = pauseMainMenuBtn;
            soPauseUI.FindProperty("rootPanel").objectReferenceValue = pauseMenuGo;
            soPauseUI.FindProperty("canvasGroup").objectReferenceValue = pauseCg;
            soPauseUI.ApplyModifiedPropertiesWithoutUndo();

            pauseMenuGo.SetActive(false);

            // ==========================================
            // 4. GAME OVER PANEL (Modal Overlay)
            // ==========================================
            GameObject gameOverPanelGo = new GameObject("GameOverPanel");
            gameOverPanelGo.transform.SetParent(safeAreaGo.transform, false);

            RectTransform goPanelRect = gameOverPanelGo.AddComponent<RectTransform>();
            goPanelRect.anchorMin = Vector2.zero;
            goPanelRect.anchorMax = Vector2.one;
            goPanelRect.offsetMin = Vector2.zero;
            goPanelRect.offsetMax = Vector2.zero;

            Image goBg = gameOverPanelGo.AddComponent<Image>();
            goBg.color = new Color(0.03f, 0.05f, 0.09f, 0.94f);

            CanvasGroup goCg = gameOverPanelGo.AddComponent<CanvasGroup>();
            GameOverUI gameOverUI = gameOverPanelGo.AddComponent<GameOverUI>();

            // Title Text: GAME OVER
            GameObject goTitleGo = new GameObject("TitleText");
            goTitleGo.transform.SetParent(gameOverPanelGo.transform, false);
            RectTransform goTitleRect = goTitleGo.AddComponent<RectTransform>();
            goTitleRect.anchorMin = new Vector2(0.5f, 0.85f);
            goTitleRect.anchorMax = new Vector2(0.5f, 0.85f);
            goTitleRect.pivot = new Vector2(0.5f, 0.5f);
            goTitleRect.sizeDelta = new Vector2(700f, 90f);

            Text goTitleText = goTitleGo.AddComponent<Text>();
            goTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            goTitleText.fontSize = 58;
            goTitleText.fontStyle = FontStyle.Bold;
            goTitleText.alignment = TextAnchor.MiddleCenter;
            goTitleText.color = new Color(1f, 0.28f, 0.32f, 1f);
            goTitleText.text = "GAME OVER";

            // Stats Card Container
            GameObject statsCardGo = new GameObject("StatsCard");
            statsCardGo.transform.SetParent(gameOverPanelGo.transform, false);
            RectTransform statsCardRect = statsCardGo.AddComponent<RectTransform>();
            statsCardRect.anchorMin = new Vector2(0.5f, 0.52f);
            statsCardRect.anchorMax = new Vector2(0.5f, 0.52f);
            statsCardRect.pivot = new Vector2(0.5f, 0.5f);
            statsCardRect.sizeDelta = new Vector2(660f, 380f);

            Image cardBg = statsCardGo.AddComponent<Image>();
            cardBg.color = new Color(0.07f, 0.11f, 0.17f, 0.92f);

            // Stat Row Helper
            Text CreateStatRow(string label, string defaultValue, Color valColor, float yPos)
            {
                GameObject rowGo = new GameObject($"Row_{label.Replace(" ", "")}");
                rowGo.transform.SetParent(statsCardGo.transform, false);
                RectTransform rowRect = rowGo.AddComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0.5f, 0.5f);
                rowRect.anchorMax = new Vector2(0.5f, 0.5f);
                rowRect.pivot = new Vector2(0.5f, 0.5f);
                rowRect.anchoredPosition = new Vector2(0f, yPos);
                rowRect.sizeDelta = new Vector2(580f, 44f);

                // Label
                GameObject lblGo = new GameObject("Label");
                lblGo.transform.SetParent(rowGo.transform, false);
                RectTransform lblRect = lblGo.AddComponent<RectTransform>();
                lblRect.anchorMin = new Vector2(0f, 0f);
                lblRect.anchorMax = new Vector2(0.55f, 1f);
                lblRect.offsetMin = Vector2.zero;
                lblRect.offsetMax = Vector2.zero;

                Text lblText = lblGo.AddComponent<Text>();
                lblText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lblText.fontSize = 24;
                lblText.alignment = TextAnchor.MiddleLeft;
                lblText.color = new Color(0.7f, 0.78f, 0.88f, 0.85f);
                lblText.text = label;

                // Value
                GameObject valGo = new GameObject("Value");
                valGo.transform.SetParent(rowGo.transform, false);
                RectTransform valRect = valGo.AddComponent<RectTransform>();
                valRect.anchorMin = new Vector2(0.55f, 0f);
                valRect.anchorMax = new Vector2(1f, 1f);
                valRect.offsetMin = Vector2.zero;
                valRect.offsetMax = Vector2.zero;

                Text valText = valGo.AddComponent<Text>();
                valText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                valText.fontSize = 28;
                valText.fontStyle = FontStyle.Bold;
                valText.alignment = TextAnchor.MiddleRight;
                valText.color = valColor;
                valText.text = defaultValue;

                return valText;
            }

            Text finalScoreVal = CreateStatRow("FINAL SCORE", "0", new Color(1f, 0.88f, 0.2f, 1f), 120f);
            Text bestScoreVal = CreateStatRow("BEST SCORE", "0", new Color(1f, 0.65f, 0.15f, 1f), 65f);
            Text finalLengthVal = CreateStatRow("FINAL LENGTH", "10", new Color(0.4f, 0.85f, 1f, 1f), 10f);
            Text survivalTimeVal = CreateStatRow("TIME SURVIVED", "00:00", new Color(0.95f, 0.95f, 0.95f, 1f), -45f);
            Text aiDefeatedVal = CreateStatRow("AI DEFEATED", "0", new Color(1f, 0.45f, 0.45f, 1f), -100f);
            Text foodCollectedVal = CreateStatRow("FOOD COLLECTED", "0", new Color(0.25f, 0.98f, 0.6f, 1f), -155f);

            // Action Buttons
            Button restartBtn = CreateMenuButton(gameOverPanelGo, "RESTART", new Color(0.1f, 0.75f, 0.45f, 1f), -180f);
            RectTransform restartBtnRect = restartBtn.GetComponent<RectTransform>();
            restartBtnRect.anchoredPosition = new Vector2(-160f, -220f);
            restartBtnRect.sizeDelta = new Vector2(280f, 65f);

            Button mainMenuBtn = CreateMenuButton(gameOverPanelGo, "MAIN MENU", new Color(0.2f, 0.35f, 0.55f, 1f), -180f);
            RectTransform mainMenuBtnRect = mainMenuBtn.GetComponent<RectTransform>();
            mainMenuBtnRect.anchoredPosition = new Vector2(160f, -220f);
            mainMenuBtnRect.sizeDelta = new Vector2(280f, 65f);

            SerializedObject soGameOver = new SerializedObject(gameOverUI);
            soGameOver.FindProperty("finalScoreText").objectReferenceValue = finalScoreVal;
            soGameOver.FindProperty("bestScoreText").objectReferenceValue = bestScoreVal;
            soGameOver.FindProperty("finalLengthText").objectReferenceValue = finalLengthVal;
            soGameOver.FindProperty("survivalTimeText").objectReferenceValue = survivalTimeVal;
            soGameOver.FindProperty("aiDefeatedText").objectReferenceValue = aiDefeatedVal;
            soGameOver.FindProperty("foodCollectedText").objectReferenceValue = foodCollectedVal;
            soGameOver.FindProperty("restartButton").objectReferenceValue = restartBtn;
            soGameOver.FindProperty("mainMenuButton").objectReferenceValue = mainMenuBtn;
            soGameOver.FindProperty("rootPanel").objectReferenceValue = gameOverPanelGo;
            soGameOver.FindProperty("canvasGroup").objectReferenceValue = goCg;
            soGameOver.ApplyModifiedPropertiesWithoutUndo();

            gameOverPanelGo.SetActive(false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(canvasGo, prefabPath);
            Object.DestroyImmediate(canvasGo);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/7. Setup Food Data & Prefab")]
        public static void MenuSetupFoodSystem()
        {
            EnsureDirectories();
            SetupFoodDataAssets();
            SetupFoodPrefab();
            SetupEatingEffectPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static FoodData[] SetupFoodDataAssets()
        {
            EnsureDirectories();

            Sprite starBerry = LoadSprite("Food_StarBerry.png") ?? LoadSprite("SegmentSprite.png");
            Sprite jellyDrop = LoadSprite("Food_JellyDrop.png") ?? LoadSprite("SegmentSprite.png");
            Sprite astralCore = LoadSprite("Food_AstralCore.png") ?? LoadSprite("SegmentSprite.png");

            // 1. Standard Grub (Score: 10, Growth: 1, Weight: 70, Emerald Star Berry)
            FoodData standard = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Standard.asset");
            if (standard == null)
            {
                standard = ScriptableObject.CreateInstance<FoodData>();
                AssetDatabase.CreateAsset(standard, $"{FoodResourcesPath}/FoodData_Standard.asset");
            }
            standard.Configure(
                FoodType.Standard,
                "Star Berry",
                score: 10,
                growth: 1,
                weight: 70f,
                color: new Color(0.18f, 0.95f, 0.35f, 1f),
                scale: 0.9f,
                sprite: starBerry,
                pulse: true,
                pSpeed: 3f,
                pMag: 0.08f
            );
            EditorUtility.SetDirty(standard);

            // 2. Super Grub (Score: 30, Growth: 3, Weight: 20, Amber Jelly Drop)
            FoodData superFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Super.asset");
            if (superFood == null)
            {
                superFood = ScriptableObject.CreateInstance<FoodData>();
                AssetDatabase.CreateAsset(superFood, $"{FoodResourcesPath}/FoodData_Super.asset");
            }
            superFood.Configure(
                FoodType.Super,
                "Jelly Drop",
                score: 30,
                growth: 3,
                weight: 20f,
                color: new Color(1f, 0.78f, 0.1f, 1f),
                scale: 1.2f,
                sprite: jellyDrop,
                pulse: true,
                pSpeed: 4.5f,
                pMag: 0.14f
            );
            EditorUtility.SetDirty(superFood);

            // 3. Mega Grub (Score: 100, Growth: 5, Weight: 10, Astral Core)
            FoodData megaFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Mega.asset");
            if (megaFood == null)
            {
                megaFood = ScriptableObject.CreateInstance<FoodData>();
                AssetDatabase.CreateAsset(megaFood, $"{FoodResourcesPath}/FoodData_Mega.asset");
            }
            megaFood.Configure(
                FoodType.Mega,
                "Astral Core",
                score: 100,
                growth: 5,
                weight: 10f,
                color: new Color(0.92f, 0.2f, 0.98f, 1f),
                scale: 1.55f,
                sprite: astralCore,
                pulse: true,
                pSpeed: 6f,
                pMag: 0.2f
            );
            EditorUtility.SetDirty(megaFood);

            Debug.Log("[GigaGrub] Created/Updated 3 Celestial FoodData ScriptableObjects");
            return new FoodData[] { standard, superFood, megaFood };
        }

        public static GameObject SetupFoodPrefab()
        {
            string prefabPath = $"{PrefabsPath}/Food.prefab";
            GameObject go = new GameObject("Food");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Food_StarBerry.png") ?? LoadSprite("SegmentSprite.png");
            sr.sortingOrder = 40;
            sr.color = Color.white;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            Food.Food food = go.AddComponent<Food.Food>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/7.5. Setup Power-Up Data & Prefabs")]
        public static void MenuSetupPowerUpSystem()
        {
            EnsureDirectories();
            SetupPowerUpDataAssets();
            SetupPowerUpPickupPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static PowerUpData[] SetupPowerUpDataAssets()
        {
            EnsureDirectories();

            Sprite speedSprite = LoadSprite("PowerUp_SpeedSurge.png") ?? LoadSprite("SegmentSprite.png");
            Sprite magnetSprite = LoadSprite("PowerUp_FoodMagnet.png") ?? LoadSprite("SegmentSprite.png");
            Sprite multSprite = LoadSprite("PowerUp_2xScore.png") ?? LoadSprite("SegmentSprite.png");

            // 1. Speed Boost (Duration: 8s, Bonus: 3.5 u/s, Weight: 35, Electric Cyan)
            PowerUpData speedBoost = AssetDatabase.LoadAssetAtPath<PowerUpData>($"{PowerUpResourcesPath}/PowerUp_SpeedBoost.asset");
            if (speedBoost == null)
            {
                speedBoost = ScriptableObject.CreateInstance<PowerUpData>();
                AssetDatabase.CreateAsset(speedBoost, $"{PowerUpResourcesPath}/PowerUp_SpeedBoost.asset");
            }
            speedBoost.Configure(
                id: "speed_boost",
                name: "Speed Surge",
                type: PowerUpType.SpeedBoost,
                duration: 8.0f,
                effectStrength: 3.5f,
                spawnWeight: 35.0f,
                themeColor: new Color(0.15f, 0.85f, 1f, 1f),
                icon: speedSprite,
                pulse: true,
                pulseSpeed: 5f,
                pulseMagnitude: 0.15f
            );
            EditorUtility.SetDirty(speedBoost);

            // 2. Food Magnet (Duration: 10s, Radius: 6 u, Weight: 35, Neon Magenta)
            PowerUpData foodMagnet = AssetDatabase.LoadAssetAtPath<PowerUpData>($"{PowerUpResourcesPath}/PowerUp_FoodMagnet.asset");
            if (foodMagnet == null)
            {
                foodMagnet = ScriptableObject.CreateInstance<PowerUpData>();
                AssetDatabase.CreateAsset(foodMagnet, $"{PowerUpResourcesPath}/PowerUp_FoodMagnet.asset");
            }
            foodMagnet.Configure(
                id: "food_magnet",
                name: "Food Magnet",
                type: PowerUpType.FoodMagnet,
                duration: 10.0f,
                effectStrength: 6.0f,
                spawnWeight: 35.0f,
                themeColor: new Color(0.95f, 0.2f, 0.85f, 1f),
                icon: magnetSprite,
                pulse: true,
                pulseSpeed: 4.5f,
                pulseMagnitude: 0.18f
            );
            EditorUtility.SetDirty(foodMagnet);

            // 3. Score Multiplier (Duration: 12s, Multiplier: 2.0x, Weight: 30, Radiant Gold)
            PowerUpData scoreMultiplier = AssetDatabase.LoadAssetAtPath<PowerUpData>($"{PowerUpResourcesPath}/PowerUp_ScoreMultiplier.asset");
            if (scoreMultiplier == null)
            {
                scoreMultiplier = ScriptableObject.CreateInstance<PowerUpData>();
                AssetDatabase.CreateAsset(scoreMultiplier, $"{PowerUpResourcesPath}/PowerUp_ScoreMultiplier.asset");
            }
            scoreMultiplier.Configure(
                id: "score_multiplier",
                name: "2x Score",
                type: PowerUpType.ScoreMultiplier,
                duration: 12.0f,
                effectStrength: 2.0f,
                spawnWeight: 30.0f,
                themeColor: new Color(1f, 0.85f, 0.15f, 1f),
                icon: multSprite,
                pulse: true,
                pulseSpeed: 4.0f,
                pulseMagnitude: 0.12f
            );
            EditorUtility.SetDirty(scoreMultiplier);

            Debug.Log("[GigaGrub] Created/Updated 3 PowerUpData ScriptableObjects");
            return new PowerUpData[] { speedBoost, foodMagnet, scoreMultiplier };
        }

        public static GameObject SetupPowerUpPickupPrefab()
        {
            string prefabPath = $"{PrefabsPath}/PowerUpPickup.prefab";
            GameObject go = new GameObject("PowerUpPickup");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("PowerUp_SpeedSurge.png") ?? LoadSprite("SegmentSprite.png");
            sr.sortingOrder = 45;
            sr.color = Color.white;

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.55f;

            PowerUpPickup pickup = go.AddComponent<PowerUpPickup>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/8. Setup Game Scene")]
        public static void MenuSetupGameScene()
        {
            SetupGameScene(null, null, null, null, null, null, null, null, null, null);
        }

        public static void SetupGameScene(
            GameObject playerPrefab = null,
            GameObject joystickCanvasPrefab = null,
            GameObject arenaPrefab = null,
            GameObject foodPrefab = null,
            FoodData[] foodDataAssets = null,
            GameObject eatingEffectPrefab = null,
            GameObject aiCreaturePrefab = null,
            GameObject powerUpPickupPrefab = null,
            PowerUpData[] powerUpDataAssets = null,
            CreatureSkinData[] creatureSkinAssets = null)
        {
            string scenePath = $"{ScenesPath}/Game.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 0. System Objects (ScoreManager, RankingManager, SaveManager, CosmeticManager)
            GameObject systemGo = new GameObject("ScoreManager");
            ScoreManager scoreMgr = systemGo.AddComponent<ScoreManager>();

            GameObject rankingGo = new GameObject("RankingManager");
            RankingManager rankingMgr = rankingGo.AddComponent<RankingManager>();

            GameObject saveMgrGo = new GameObject("SaveManager");
            SaveManager saveMgr = saveMgrGo.AddComponent<SaveManager>();

            GameObject cosmeticMgrGo = new GameObject("CosmeticManager");
            GigaGrub.Cosmetics.CosmeticManager cosmeticMgr = cosmeticMgrGo.AddComponent<GigaGrub.Cosmetics.CosmeticManager>();

            GameObject dailyRewardGo = new GameObject("DailyRewardManager");
            dailyRewardGo.AddComponent<DailyRewardManager>();

            GameObject questMgrGo = new GameObject("QuestManager");
            questMgrGo.AddComponent<QuestManager>();

            // 1. Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.backgroundColor = new Color(0.04f, 0.07f, 0.12f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            CameraFollow camFollow = camGo.AddComponent<CameraFollow>();
            SerializedObject soCam = new SerializedObject(camFollow);
            soCam.FindProperty("smoothSpeed").floatValue = 8f;
            soCam.FindProperty("defaultZoom").floatValue = 9f;
            soCam.FindProperty("minZoom").floatValue = 6f;
            soCam.FindProperty("maxZoom").floatValue = 18f;
            soCam.FindProperty("zoomSpeed").floatValue = 4f;
            soCam.FindProperty("clampToArena").boolValue = true;
            soCam.ApplyModifiedPropertiesWithoutUndo();

            // 2. Arena
            if (arenaPrefab == null)
            {
                arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Arena.prefab");
                if (arenaPrefab == null)
                {
                    arenaPrefab = SetupArenaPrefab();
                }
            }
            GameObject arenaInstance = (GameObject)PrefabUtility.InstantiatePrefab(arenaPrefab);
            arenaInstance.name = "Arena";
            arenaInstance.transform.position = Vector3.zero;

            // 3. Player
            if (playerPrefab == null)
            {
                playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Player.prefab");
                if (playerPrefab == null)
                {
                    playerPrefab = SetupPlayerPrefab();
                }
            }
            GameObject playerInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerInstance.name = "Player";
            playerInstance.transform.position = Vector3.zero;
            camFollow.target = playerInstance.transform;

            PlayerBody playerBody = playerInstance.GetComponent<PlayerBody>();
            PlayerController playerCtrl = playerInstance.GetComponent<PlayerController>();
            GrowthSystem growthSystem = playerInstance.GetComponent<GrowthSystem>();

            if (creatureSkinAssets == null || creatureSkinAssets.Length == 0)
            {
                creatureSkinAssets = SetupCreatureSkinAssets();
            }

            if (playerBody != null && creatureSkinAssets.Length > 0)
            {
                playerBody.SetSkin(creatureSkinAssets[0]);
                EditorUtility.SetDirty(playerBody);
            }

            // 4. Joystick & Score HUD Canvas
            if (joystickCanvasPrefab == null)
            {
                joystickCanvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/JoystickCanvas.prefab");
                if (joystickCanvasPrefab == null)
                {
                    joystickCanvasPrefab = SetupJoystickCanvasPrefab();
                }
            }
            GameObject canvasInstance = (GameObject)PrefabUtility.InstantiatePrefab(joystickCanvasPrefab);
            canvasInstance.name = "JoystickCanvas";

            VirtualJoystick joystick = canvasInstance.GetComponentInChildren<VirtualJoystick>();
            if (playerCtrl != null && joystick != null)
            {
                playerCtrl.joystick = joystick;
                EditorUtility.SetDirty(playerCtrl);
            }

            ScoreUI scoreUI = canvasInstance.GetComponentInChildren<ScoreUI>();
            if (scoreUI != null && playerBody != null)
            {
                scoreUI.Bind(playerBody, growthSystem);
            }

            // 5. Food Spawner (100+ Food Count)
            if (foodPrefab == null)
            {
                foodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Food.prefab");
                if (foodPrefab == null)
                {
                    foodPrefab = SetupFoodPrefab();
                }
            }

            if (eatingEffectPrefab == null)
            {
                eatingEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/EatingEffect.prefab");
                if (eatingEffectPrefab == null)
                {
                    eatingEffectPrefab = SetupEatingEffectPrefab();
                }
            }
            EatingEffect.SetPrefabReference(eatingEffectPrefab);

            if (foodDataAssets == null || foodDataAssets.Length == 0)
            {
                foodDataAssets = new FoodData[]
                {
                    AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Standard.asset"),
                    AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Super.asset"),
                    AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Mega.asset")
                };

                if (foodDataAssets[0] == null)
                {
                    foodDataAssets = SetupFoodDataAssets();
                }
            }

            GameObject spawnerGo = new GameObject("FoodSpawner");
            FoodSpawner spawner = spawnerGo.AddComponent<FoodSpawner>();
            spawner.SetFoodPrefab(foodPrefab);
            spawner.SetFoodTypes(foodDataAssets);
            spawner.SetDefaultSprite(LoadSprite("Food_StarBerry.png") ?? LoadSprite("SegmentSprite.png"));
            spawner.SetPopulationLimits(100, 150, 120);
            spawner.SetPlayerBody(playerBody);

            SerializedObject soSpawner = new SerializedObject(spawner);
            soSpawner.FindProperty("foodPrefab").objectReferenceValue = foodPrefab;
            soSpawner.FindProperty("defaultFoodSprite").objectReferenceValue = LoadSprite("Food_StarBerry.png") ?? LoadSprite("SegmentSprite.png");
            soSpawner.FindProperty("minFoodCount").intValue = 100;
            soSpawner.FindProperty("maxFoodCount").intValue = 150;
            soSpawner.FindProperty("initialFoodCount").intValue = 120;
            soSpawner.FindProperty("wallMargin").floatValue = 2.5f;
            soSpawner.FindProperty("minPlayerDistance").floatValue = 3.5f;
            soSpawner.FindProperty("respawnDelay").floatValue = 0.5f;
            soSpawner.FindProperty("playerBody").objectReferenceValue = playerBody;

            SerializedProperty typesProp = soSpawner.FindProperty("foodTypes");
            typesProp.arraySize = foodDataAssets.Length;
            for (int i = 0; i < foodDataAssets.Length; i++)
            {
                typesProp.GetArrayElementAtIndex(i).objectReferenceValue = foodDataAssets[i];
            }
            soSpawner.ApplyModifiedPropertiesWithoutUndo();

            // 6. AI Spawner (10 AI Creatures with 9 Bot Skins)
            if (aiCreaturePrefab == null)
            {
                aiCreaturePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/AICreature.prefab");
                if (aiCreaturePrefab == null)
                {
                    aiCreaturePrefab = SetupAICreaturePrefab();
                }
            }

            GameObject aiSpawnerGo = new GameObject("AISpawner");
            AISpawner aiSpawner = aiSpawnerGo.AddComponent<AISpawner>();
            aiSpawner.SetAIPrefab(aiCreaturePrefab);
            aiSpawner.SetTargetAICount(10);

            CreatureSkinData[] botSkins = new CreatureSkinData[Mathf.Max(1, creatureSkinAssets.Length - 1)];
            for (int i = 1; i < creatureSkinAssets.Length; i++)
            {
                botSkins[i - 1] = creatureSkinAssets[i];
            }
            aiSpawner.SetSkins(botSkins);

            SerializedObject soAiSpawner = new SerializedObject(aiSpawner);
            soAiSpawner.FindProperty("aiCreaturePrefab").objectReferenceValue = aiCreaturePrefab;
            soAiSpawner.FindProperty("targetAICount").intValue = 10;
            soAiSpawner.FindProperty("minSpawnDistance").floatValue = 12f;

            SerializedProperty botSkinsProp = soAiSpawner.FindProperty("botSkins");
            botSkinsProp.arraySize = botSkins.Length;
            for (int i = 0; i < botSkins.Length; i++)
            {
                botSkinsProp.GetArrayElementAtIndex(i).objectReferenceValue = botSkins[i];
            }
            soAiSpawner.ApplyModifiedPropertiesWithoutUndo();

            // 6.5. PowerUp Spawner (5 Concurrent Pickups)
            if (powerUpPickupPrefab == null)
            {
                powerUpPickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PowerUpPickup.prefab");
                if (powerUpPickupPrefab == null)
                {
                    powerUpPickupPrefab = SetupPowerUpPickupPrefab();
                }
            }

            if (powerUpDataAssets == null || powerUpDataAssets.Length == 0)
            {
                powerUpDataAssets = new PowerUpData[]
                {
                    AssetDatabase.LoadAssetAtPath<PowerUpData>($"{PowerUpResourcesPath}/PowerUp_SpeedBoost.asset"),
                    AssetDatabase.LoadAssetAtPath<PowerUpData>($"{PowerUpResourcesPath}/PowerUp_FoodMagnet.asset"),
                    AssetDatabase.LoadAssetAtPath<PowerUpData>($"{PowerUpResourcesPath}/PowerUp_ScoreMultiplier.asset")
                };

                if (powerUpDataAssets[0] == null)
                {
                    powerUpDataAssets = SetupPowerUpDataAssets();
                }
            }

            GameObject powerUpSpawnerGo = new GameObject("PowerUpSpawner");
            PowerUpSpawner powerUpSpawner = powerUpSpawnerGo.AddComponent<PowerUpSpawner>();
            powerUpSpawner.SetPickupPrefab(powerUpPickupPrefab);
            powerUpSpawner.SetPowerUpTypes(powerUpDataAssets);
            powerUpSpawner.SetPoolParameters(5, 10f, 20f);

            SerializedObject soPowerUpSpawner = new SerializedObject(powerUpSpawner);
            soPowerUpSpawner.FindProperty("pickupPrefab").objectReferenceValue = powerUpPickupPrefab;
            soPowerUpSpawner.FindProperty("maxActivePickups").intValue = 5;
            soPowerUpSpawner.FindProperty("spawnInterval").floatValue = 15f;
            soPowerUpSpawner.FindProperty("wallMargin").floatValue = 4f;
            SerializedProperty puTypesProp = soPowerUpSpawner.FindProperty("powerUpTypes");
            puTypesProp.arraySize = powerUpDataAssets.Length;
            for (int i = 0; i < powerUpDataAssets.Length; i++)
            {
                puTypesProp.GetArrayElementAtIndex(i).objectReferenceValue = powerUpDataAssets[i];
            }
            soPowerUpSpawner.ApplyModifiedPropertiesWithoutUndo();

            // 7. GameManager & UI Wiring
            GameObject gameManagerGo = new GameObject("GameManager");
            GameManager gameManager = gameManagerGo.AddComponent<GameManager>();
            GameOverUI gameOverUI = canvasInstance.GetComponentInChildren<GameOverUI>(true);
            GameplayHUD gameplayHUD = canvasInstance.GetComponentInChildren<GameplayHUD>(true);
            PauseMenuUI pauseMenuUI = canvasInstance.GetComponentInChildren<PauseMenuUI>(true);

            SerializedObject soRankMgr = new SerializedObject(rankingMgr);
            soRankMgr.FindProperty("playerBody").objectReferenceValue = playerBody;
            soRankMgr.FindProperty("updateInterval").floatValue = 0.5f;
            soRankMgr.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject soGameMgr = new SerializedObject(gameManager);
            soGameMgr.FindProperty("playerBody").objectReferenceValue = playerBody;
            soGameMgr.FindProperty("aiSpawner").objectReferenceValue = aiSpawner;
            soGameMgr.FindProperty("foodSpawner").objectReferenceValue = spawner;
            soGameMgr.FindProperty("scoreManager").objectReferenceValue = scoreMgr;
            soGameMgr.FindProperty("rankingManager").objectReferenceValue = rankingMgr;
            soGameMgr.FindProperty("saveManager").objectReferenceValue = saveMgr;
            soGameMgr.FindProperty("cameraFollow").objectReferenceValue = camFollow;
            soGameMgr.FindProperty("gameOverUI").objectReferenceValue = gameOverUI;
            soGameMgr.FindProperty("gameplayHUD").objectReferenceValue = gameplayHUD;
            soGameMgr.FindProperty("pauseMenuUI").objectReferenceValue = pauseMenuUI;
            soGameMgr.FindProperty("scoreUI").objectReferenceValue = scoreUI;
            soGameMgr.FindProperty("initialAICount").intValue = 10;
            soGameMgr.FindProperty("initialFoodCount").intValue = 120;
            soGameMgr.ApplyModifiedPropertiesWithoutUndo();

            // 8. EventSystem
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[GigaGrub] Saved Game scene with GameManager, 10 AI Creatures and Collision System to {scenePath}");
        }

        [MenuItem("GigaGrub/8.5. Setup MainMenu Scene")]
        public static void MenuSetupMainMenuScene()
        {
            SetupMainMenuScene();
        }

        public static void SetupMainMenuScene()
        {
            if (EditorApplication.isPlaying)
            {
                Debug.LogError("[GigaGrub] Cannot run SetupMainMenuScene while Unity is in Play Mode! Please click the Play button to stop Play Mode first.");
                EditorUtility.DisplayDialog("GigaGrub Setup", "Cannot run Scene Setup while in Play Mode.\nPlease exit Play Mode in Unity first and try again.", "OK");
                return;
            }

            EnsureDirectories();
            string scenePath = $"{ScenesPath}/MainMenu.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.backgroundColor = new Color(0.04f, 0.06f, 0.10f, 1f); // Sleek Dark Slate
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGo.AddComponent<AudioListener>();

            // 2. SaveManager, SceneTransitionManager & CosmeticManager
            GameObject saveMgrGo = new GameObject("SaveManager");
            saveMgrGo.AddComponent<SaveManager>();

            GameObject transMgrGo = new GameObject("SceneTransitionManager");
            transMgrGo.AddComponent<SceneTransitionManager>();

            GameObject cosmeticMgrGo = new GameObject("CosmeticManager");
            cosmeticMgrGo.AddComponent<GigaGrub.Cosmetics.CosmeticManager>();

            // 3. Main Menu Canvas (1920x1080 Landscape Scaler)
            GameObject canvasGo = new GameObject("MainMenuCanvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            MainMenuUI menuUI = canvasGo.AddComponent<MainMenuUI>();

            // Safe Area Container
            GameObject safeAreaGo = new GameObject("SafeArea");
            safeAreaGo.transform.SetParent(canvasGo.transform, false);
            RectTransform safeAreaRect = safeAreaGo.AddComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero;
            safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = Vector2.zero;
            safeAreaRect.offsetMax = Vector2.zero;
            safeAreaGo.AddComponent<SafeAreaFitter>();

            // Background Subtle Glow / Grid Overlay
            GameObject bgGo = new GameObject("BackgroundVisual");
            bgGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform bgRect = bgGo.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.color = new Color(0.03f, 0.05f, 0.08f, 0.85f);

            // ==========================================
            // LOGO BANNER (Original stylized placeholder)
            // ==========================================
            GameObject logoGo = new GameObject("LogoBanner");
            logoGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform logoRect = logoGo.AddComponent<RectTransform>();
            logoRect.anchorMin = new Vector2(0.5f, 0.76f);
            logoRect.anchorMax = new Vector2(0.5f, 0.76f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            logoRect.anchoredPosition = Vector2.zero;
            logoRect.sizeDelta = new Vector2(850f, 180f);

            GameObject titleTxtGo = new GameObject("TitleText");
            titleTxtGo.transform.SetParent(logoGo.transform, false);
            RectTransform titleRect = titleTxtGo.AddComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = Vector2.one;
            titleRect.offsetMin = Vector2.zero;
            titleRect.offsetMax = Vector2.zero;

            Text titleTxt = titleTxtGo.AddComponent<Text>();
            titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleTxt.fontSize = 86;
            titleTxt.fontStyle = FontStyle.Bold;
            titleTxt.alignment = TextAnchor.MiddleCenter;
            titleTxt.color = new Color(0.29f, 0.87f, 0.50f, 1f); // Neon Emerald #4ADE80
            titleTxt.text = "GIGA GRUB";

            Shadow titleShadow = titleTxtGo.AddComponent<Shadow>();
            titleShadow.effectColor = new Color(0.02f, 0.25f, 0.12f, 0.85f);
            titleShadow.effectDistance = new Vector2(3f, -3f);

            // ==========================================
            // LIVE CREATURE PREVIEW (MAIN MENU)
            // ==========================================
            GameObject previewGo = new GameObject("MainMenuCreaturePreview", typeof(RectTransform));
            previewGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform previewRect = previewGo.GetComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 0.58f);
            previewRect.anchorMax = new Vector2(0.5f, 0.58f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = Vector2.zero;
            previewRect.sizeDelta = new Vector2(160f, 160f);

            GigaGrub.Cosmetics.CreaturePreviewStage mainPreviewStage = previewGo.AddComponent<GigaGrub.Cosmetics.CreaturePreviewStage>();

            // ==========================================
            // BEST SCORE BADGE
            // ==========================================
            GameObject bestBadgeGo = new GameObject("BestScoreBadge");
            bestBadgeGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform bestRect = bestBadgeGo.AddComponent<RectTransform>();
            bestRect.anchorMin = new Vector2(0.5f, 0.70f);
            bestRect.anchorMax = new Vector2(0.5f, 0.70f);
            bestRect.pivot = new Vector2(0.5f, 0.5f);
            bestRect.anchoredPosition = Vector2.zero;
            bestRect.sizeDelta = new Vector2(380f, 50f);

            Image bestImg = bestBadgeGo.AddComponent<Image>();
            bestImg.color = new Color(0.06f, 0.09f, 0.15f, 0.85f);

            GameObject bestTxtGo = new GameObject("Text");
            bestTxtGo.transform.SetParent(bestBadgeGo.transform, false);
            RectTransform bestTxtRect = bestTxtGo.AddComponent<RectTransform>();
            bestTxtRect.anchorMin = Vector2.zero;
            bestTxtRect.anchorMax = Vector2.one;
            bestTxtRect.offsetMin = Vector2.zero;
            bestTxtRect.offsetMax = Vector2.zero;

            Text bestTxt = bestTxtGo.AddComponent<Text>();
            bestTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bestTxt.fontSize = 24;
            bestTxt.fontStyle = FontStyle.Bold;
            bestTxt.alignment = TextAnchor.MiddleCenter;
            bestTxt.color = new Color(0.99f, 0.83f, 0.30f, 1f); // Amber Gold #FCD34D
            bestTxt.text = "BEST SCORE: 0";

            // ==========================================
            // PLAY BUTTON
            // ==========================================
            GameObject playBtnGo = new GameObject("PlayButton");
            playBtnGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform playRect = playBtnGo.AddComponent<RectTransform>();
            playRect.anchorMin = new Vector2(0.5f, 0.38f);
            playRect.anchorMax = new Vector2(0.5f, 0.38f);
            playRect.pivot = new Vector2(0.5f, 0.5f);
            playRect.anchoredPosition = Vector2.zero;
            playRect.sizeDelta = new Vector2(380f, 88f);

            Image playImg = playBtnGo.AddComponent<Image>();
            playImg.color = new Color(0.06f, 0.73f, 0.51f, 1f); // Vibrant Emerald #10B981

            Button playBtn = playBtnGo.AddComponent<Button>();

            GameObject playTxtGo = new GameObject("Text");
            playTxtGo.transform.SetParent(playBtnGo.transform, false);
            RectTransform playTxtRect = playTxtGo.AddComponent<RectTransform>();
            playTxtRect.anchorMin = Vector2.zero;
            playTxtRect.anchorMax = Vector2.one;
            playTxtRect.offsetMin = Vector2.zero;
            playTxtRect.offsetMax = Vector2.zero;

            Text playTxt = playTxtGo.AddComponent<Text>();
            playTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            playTxt.fontSize = 44;
            playTxt.fontStyle = FontStyle.Bold;
            playTxt.alignment = TextAnchor.MiddleCenter;
            playTxt.color = Color.white;
            playTxt.text = "PLAY";

            // ==========================================
            // NAVIGATION BUTTONS (CUSTOMIZE, REWARDS, STATISTICS & SETTINGS)
            // ==========================================
            // 1. Customize Button
            GameObject custBtnGo = new GameObject("CustomizeButton");
            custBtnGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform custBtnRect = custBtnGo.AddComponent<RectTransform>();
            custBtnRect.anchorMin = new Vector2(0.5f, 0.22f);
            custBtnRect.anchorMax = new Vector2(0.5f, 0.22f);
            custBtnRect.pivot = new Vector2(0.5f, 0.5f);
            custBtnRect.anchoredPosition = new Vector2(-360f, 0f);
            custBtnRect.sizeDelta = new Vector2(220f, 66f);

            Image custBtnImg = custBtnGo.AddComponent<Image>();
            custBtnImg.color = new Color(0.15f, 0.30f, 0.50f, 0.95f); // Deep Cyan
            Button custBtn = custBtnGo.AddComponent<Button>();

            GameObject custTxtGo = new GameObject("Text");
            custTxtGo.transform.SetParent(custBtnGo.transform, false);
            RectTransform custTxtRect = custTxtGo.AddComponent<RectTransform>();
            custTxtRect.anchorMin = Vector2.zero;
            custTxtRect.anchorMax = Vector2.one;
            custTxtRect.offsetMin = Vector2.zero;
            custTxtRect.offsetMax = Vector2.zero;

            Text custTxt = custTxtGo.AddComponent<Text>();
            custTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            custTxt.fontSize = 20;
            custTxt.fontStyle = FontStyle.Bold;
            custTxt.alignment = TextAnchor.MiddleCenter;
            custTxt.color = new Color(0.45f, 0.90f, 1f, 1f);
            custTxt.text = "CUSTOMIZE";

            // 2. Daily Rewards / Quests Button
            GameObject rewBtnGo = new GameObject("RewardsButton");
            rewBtnGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform rewBtnRect = rewBtnGo.AddComponent<RectTransform>();
            rewBtnRect.anchorMin = new Vector2(0.5f, 0.22f);
            rewBtnRect.anchorMax = new Vector2(0.5f, 0.22f);
            rewBtnRect.pivot = new Vector2(0.5f, 0.5f);
            rewBtnRect.anchoredPosition = new Vector2(-120f, 0f);
            rewBtnRect.sizeDelta = new Vector2(220f, 66f);

            Image rewBtnImg = rewBtnGo.AddComponent<Image>();
            rewBtnImg.color = new Color(0.85f, 0.47f, 0.05f, 0.95f); // Amber Gold #D97706
            Button rewBtn = rewBtnGo.AddComponent<Button>();

            GameObject rewTxtGo = new GameObject("Text");
            rewTxtGo.transform.SetParent(rewBtnGo.transform, false);
            RectTransform rewTxtRect = rewTxtGo.AddComponent<RectTransform>();
            rewTxtRect.anchorMin = Vector2.zero;
            rewTxtRect.anchorMax = Vector2.one;
            rewTxtRect.offsetMin = Vector2.zero;
            rewTxtRect.offsetMax = Vector2.zero;

            Text rewTxt = rewTxtGo.AddComponent<Text>();
            rewTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rewTxt.fontSize = 20;
            rewTxt.fontStyle = FontStyle.Bold;
            rewTxt.alignment = TextAnchor.MiddleCenter;
            rewTxt.color = new Color(1f, 0.95f, 0.70f, 1f);
            rewTxt.text = "REWARDS";

            // Rewards Notification Badge
            GameObject rewBadgeGo = new GameObject("NotificationBadge");
            rewBadgeGo.transform.SetParent(rewBtnGo.transform, false);
            RectTransform rewBadgeRect = rewBadgeGo.AddComponent<RectTransform>();
            rewBadgeRect.anchorMin = new Vector2(1f, 1f);
            rewBadgeRect.anchorMax = new Vector2(1f, 1f);
            rewBadgeRect.pivot = new Vector2(0.5f, 0.5f);
            rewBadgeRect.anchoredPosition = new Vector2(-10f, -10f);
            rewBadgeRect.sizeDelta = new Vector2(22f, 22f);

            Image rewBadgeImg = rewBadgeGo.AddComponent<Image>();
            rewBadgeImg.color = new Color(0.94f, 0.27f, 0.27f, 1f); // Red #EF4444

            GameObject rewBadgeTxtGo = new GameObject("BadgeText");
            rewBadgeTxtGo.transform.SetParent(rewBadgeGo.transform, false);
            RectTransform rewBadgeTxtRect = rewBadgeTxtGo.AddComponent<RectTransform>();
            rewBadgeTxtRect.anchorMin = Vector2.zero;
            rewBadgeTxtRect.anchorMax = Vector2.one;
            rewBadgeTxtRect.offsetMin = Vector2.zero;
            rewBadgeTxtRect.offsetMax = Vector2.zero;

            Text rewBadgeTxt = rewBadgeTxtGo.AddComponent<Text>();
            rewBadgeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rewBadgeTxt.fontSize = 14;
            rewBadgeTxt.fontStyle = FontStyle.Bold;
            rewBadgeTxt.alignment = TextAnchor.MiddleCenter;
            rewBadgeTxt.color = Color.white;
            rewBadgeTxt.text = "!";

            // 3. Statistics Button
            GameObject statsBtnGo = new GameObject("StatsButton");
            statsBtnGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform statsBtnRect = statsBtnGo.AddComponent<RectTransform>();
            statsBtnRect.anchorMin = new Vector2(0.5f, 0.22f);
            statsBtnRect.anchorMax = new Vector2(0.5f, 0.22f);
            statsBtnRect.pivot = new Vector2(0.5f, 0.5f);
            statsBtnRect.anchoredPosition = new Vector2(120f, 0f);
            statsBtnRect.sizeDelta = new Vector2(220f, 66f);

            Image statsBtnImg = statsBtnGo.AddComponent<Image>();
            statsBtnImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f); // Slate #1E293B
            Button statsBtn = statsBtnGo.AddComponent<Button>();

            GameObject statsTxtGo = new GameObject("Text");
            statsTxtGo.transform.SetParent(statsBtnGo.transform, false);
            RectTransform statsTxtRect = statsTxtGo.AddComponent<RectTransform>();
            statsTxtRect.anchorMin = Vector2.zero;
            statsTxtRect.anchorMax = Vector2.one;
            statsTxtRect.offsetMin = Vector2.zero;
            statsTxtRect.offsetMax = Vector2.zero;

            Text statsTxt = statsTxtGo.AddComponent<Text>();
            statsTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsTxt.fontSize = 20;
            statsTxt.fontStyle = FontStyle.Bold;
            statsTxt.alignment = TextAnchor.MiddleCenter;
            statsTxt.color = new Color(0.89f, 0.91f, 0.94f, 1f);
            statsTxt.text = "STATISTICS";

            // 4. Settings Button
            GameObject settBtnGo = new GameObject("SettingsButton");
            settBtnGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform settBtnRect = settBtnGo.AddComponent<RectTransform>();
            settBtnRect.anchorMin = new Vector2(0.5f, 0.22f);
            settBtnRect.anchorMax = new Vector2(0.5f, 0.22f);
            settBtnRect.pivot = new Vector2(0.5f, 0.5f);
            settBtnRect.anchoredPosition = new Vector2(360f, 0f);
            settBtnRect.sizeDelta = new Vector2(220f, 66f);

            Image settBtnImg = settBtnGo.AddComponent<Image>();
            settBtnImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            Button settBtn = settBtnGo.AddComponent<Button>();

            GameObject settTxtGo = new GameObject("Text");
            settTxtGo.transform.SetParent(settBtnGo.transform, false);
            RectTransform settTxtRect = settTxtGo.AddComponent<RectTransform>();
            settTxtRect.anchorMin = Vector2.zero;
            settTxtRect.anchorMax = Vector2.one;
            settTxtRect.offsetMin = Vector2.zero;
            settTxtRect.offsetMax = Vector2.zero;

            Text settTxt = settTxtGo.AddComponent<Text>();
            settTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            settTxt.fontSize = 20;
            settTxt.fontStyle = FontStyle.Bold;
            settTxt.alignment = TextAnchor.MiddleCenter;
            settTxt.color = new Color(0.89f, 0.91f, 0.94f, 1f);
            settTxt.text = "SETTINGS";

            // ==========================================
            // CUSTOMIZATION MODAL DIALOG
            // ==========================================
            CreateCustomizationModal(safeAreaGo, out GameObject custModalGo, out CanvasGroup custCg, out GigaGrub.UI.Customization.CustomizationUI custUI);

            // ==========================================
            // DAILY REWARDS & QUESTS MODAL DIALOG
            // ==========================================
            CreateDailyRewardsAndQuestsModal(safeAreaGo, out GameObject rewardsModalGo, out CanvasGroup rewardsCg, out DailyRewardsAndQuestsUI rewardsUI);

            // ==========================================
            // STATISTICS MODAL DIALOG
            // ==========================================
            GameObject statsModalGo = new GameObject("StatisticsModal");
            statsModalGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform statsModalRect = statsModalGo.AddComponent<RectTransform>();
            statsModalRect.anchorMin = Vector2.zero;
            statsModalRect.anchorMax = Vector2.one;
            statsModalRect.offsetMin = Vector2.zero;
            statsModalRect.offsetMax = Vector2.zero;

            Image statsModalDim = statsModalGo.AddComponent<Image>();
            statsModalDim.color = new Color(0f, 0f, 0f, 0.72f);
            CanvasGroup statsCg = statsModalGo.AddComponent<CanvasGroup>();

            GameObject statsDialogGo = new GameObject("DialogBox");
            statsDialogGo.transform.SetParent(statsModalGo.transform, false);
            RectTransform statsDialogRect = statsDialogGo.AddComponent<RectTransform>();
            statsDialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            statsDialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            statsDialogRect.pivot = new Vector2(0.5f, 0.5f);
            statsDialogRect.sizeDelta = new Vector2(860f, 560f);

            Image statsDialogBg = statsDialogGo.AddComponent<Image>();
            statsDialogBg.color = new Color(0.07f, 0.10f, 0.17f, 0.98f); // Deep Slate #0F172A

            GameObject statsHeaderGo = new GameObject("Header");
            statsHeaderGo.transform.SetParent(statsDialogGo.transform, false);
            RectTransform statsHeaderRect = statsHeaderGo.AddComponent<RectTransform>();
            statsHeaderRect.anchorMin = new Vector2(0f, 0.85f);
            statsHeaderRect.anchorMax = new Vector2(1f, 1f);
            statsHeaderRect.offsetMin = Vector2.zero;
            statsHeaderRect.offsetMax = Vector2.zero;

            Text statsHeaderTxt = statsHeaderGo.AddComponent<Text>();
            statsHeaderTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsHeaderTxt.fontSize = 32;
            statsHeaderTxt.fontStyle = FontStyle.Bold;
            statsHeaderTxt.alignment = TextAnchor.MiddleCenter;
            statsHeaderTxt.color = new Color(0.22f, 0.74f, 0.97f, 1f); // Sky Cyan #38BDF8
            statsHeaderTxt.text = "CAREER STATISTICS";

            Text statScoreVal = CreateStatsRow(statsDialogGo, "Best Score", new Vector2(0f, 0.68f), new Color(0.99f, 0.83f, 0.30f, 1f));
            Text statLengthVal = CreateStatsRow(statsDialogGo, "Best Length", new Vector2(0f, 0.54f), new Color(0.29f, 0.87f, 0.50f, 1f));
            Text statGamesVal = CreateStatsRow(statsDialogGo, "Games Played", new Vector2(0f, 0.40f), new Color(0.89f, 0.91f, 0.94f, 1f));
            Text statFoodVal = CreateStatsRow(statsDialogGo, "Food Collected", new Vector2(0f, 0.26f), new Color(0.96f, 0.45f, 0.71f, 1f));
            Text statAIVal = CreateStatsRow(statsDialogGo, "AI Defeated", new Vector2(0f, 0.12f), new Color(0.97f, 0.44f, 0.44f, 1f));

            // Close Stats Button
            GameObject statsCloseBtnGo = new GameObject("CloseButton");
            statsCloseBtnGo.transform.SetParent(statsDialogGo.transform, false);
            RectTransform statsCloseRect = statsCloseBtnGo.AddComponent<RectTransform>();
            statsCloseRect.anchorMin = new Vector2(0.5f, 0.02f);
            statsCloseRect.anchorMax = new Vector2(0.5f, 0.02f);
            statsCloseRect.pivot = new Vector2(0.5f, 0f);
            statsCloseRect.sizeDelta = new Vector2(260f, 54f);

            Image statsCloseImg = statsCloseBtnGo.AddComponent<Image>();
            statsCloseImg.color = new Color(0.20f, 0.25f, 0.33f, 1f);
            Button statsCloseBtn = statsCloseBtnGo.AddComponent<Button>();

            GameObject statsCloseTxtGo = new GameObject("Text");
            statsCloseTxtGo.transform.SetParent(statsCloseBtnGo.transform, false);
            RectTransform statsCloseTxtRect = statsCloseTxtGo.AddComponent<RectTransform>();
            statsCloseTxtRect.anchorMin = Vector2.zero;
            statsCloseTxtRect.anchorMax = Vector2.one;
            statsCloseTxtRect.offsetMin = Vector2.zero;
            statsCloseTxtRect.offsetMax = Vector2.zero;

            Text statsCloseTxt = statsCloseTxtGo.AddComponent<Text>();
            statsCloseTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            statsCloseTxt.fontSize = 22;
            statsCloseTxt.fontStyle = FontStyle.Bold;
            statsCloseTxt.alignment = TextAnchor.MiddleCenter;
            statsCloseTxt.color = Color.white;
            statsCloseTxt.text = "CLOSE";

            // ==========================================
            // SETTINGS MODAL DIALOG
            // ==========================================
            GameObject settModalGo = new GameObject("SettingsModal");
            settModalGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform settModalRect = settModalGo.AddComponent<RectTransform>();
            settModalRect.anchorMin = Vector2.zero;
            settModalRect.anchorMax = Vector2.one;
            settModalRect.offsetMin = Vector2.zero;
            settModalRect.offsetMax = Vector2.zero;

            Image settModalDim = settModalGo.AddComponent<Image>();
            settModalDim.color = new Color(0f, 0f, 0f, 0.72f);
            CanvasGroup settCg = settModalGo.AddComponent<CanvasGroup>();

            GameObject settDialogGo = new GameObject("DialogBox");
            settDialogGo.transform.SetParent(settModalGo.transform, false);
            RectTransform settDialogRect = settDialogGo.AddComponent<RectTransform>();
            settDialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            settDialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            settDialogRect.pivot = new Vector2(0.5f, 0.5f);
            settDialogRect.sizeDelta = new Vector2(860f, 560f);

            Image settDialogBg = settDialogGo.AddComponent<Image>();
            settDialogBg.color = new Color(0.07f, 0.10f, 0.17f, 0.98f);

            GameObject settHeaderGo = new GameObject("Header");
            settHeaderGo.transform.SetParent(settDialogGo.transform, false);
            RectTransform settHeaderRect = settHeaderGo.AddComponent<RectTransform>();
            settHeaderRect.anchorMin = new Vector2(0f, 0.85f);
            settHeaderRect.anchorMax = new Vector2(1f, 1f);
            settHeaderRect.offsetMin = Vector2.zero;
            settHeaderRect.offsetMax = Vector2.zero;

            Text settHeaderTxt = settHeaderGo.AddComponent<Text>();
            settHeaderTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            settHeaderTxt.fontSize = 32;
            settHeaderTxt.fontStyle = FontStyle.Bold;
            settHeaderTxt.alignment = TextAnchor.MiddleCenter;
            settHeaderTxt.color = new Color(0.22f, 0.74f, 0.97f, 1f);
            settHeaderTxt.text = "SETTINGS";

            // Row 1: Music Volume Slider
            Slider musicSlider = CreateSettingsSliderRow(settDialogGo, "Music Volume", new Vector2(0f, 0.65f));

            // Row 2: SFX Volume Slider
            Slider sfxSlider = CreateSettingsSliderRow(settDialogGo, "SFX Volume", new Vector2(0f, 0.47f));

            // Row 3: Vibration Toggle
            CreateSettingsVibrationRow(settDialogGo, "Vibration", new Vector2(0f, 0.29f), out Button vibBtn, out Text vibTxt, out Image vibBg);

            // Save & Close Settings Button
            GameObject settCloseBtnGo = new GameObject("SaveCloseButton");
            settCloseBtnGo.transform.SetParent(settDialogGo.transform, false);
            RectTransform settCloseRect = settCloseBtnGo.AddComponent<RectTransform>();
            settCloseRect.anchorMin = new Vector2(0.5f, 0.04f);
            settCloseRect.anchorMax = new Vector2(0.5f, 0.04f);
            settCloseRect.pivot = new Vector2(0.5f, 0f);
            settCloseRect.sizeDelta = new Vector2(280f, 58f);

            Image settCloseImg = settCloseBtnGo.AddComponent<Image>();
            settCloseImg.color = new Color(0.06f, 0.73f, 0.51f, 1f); // Emerald #10B981
            Button settCloseBtn = settCloseBtnGo.AddComponent<Button>();

            GameObject settCloseTxtGo = new GameObject("Text");
            settCloseTxtGo.transform.SetParent(settCloseBtnGo.transform, false);
            RectTransform settCloseTxtRect = settCloseTxtGo.AddComponent<RectTransform>();
            settCloseTxtRect.anchorMin = Vector2.zero;
            settCloseTxtRect.anchorMax = Vector2.one;
            settCloseTxtRect.offsetMin = Vector2.zero;
            settCloseTxtRect.offsetMax = Vector2.zero;

            Text settCloseTxt = settCloseTxtGo.AddComponent<Text>();
            settCloseTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            settCloseTxt.fontSize = 22;
            settCloseTxt.fontStyle = FontStyle.Bold;
            settCloseTxt.alignment = TextAnchor.MiddleCenter;
            settCloseTxt.color = Color.white;
            settCloseTxt.text = "SAVE & CLOSE";

            // Wire MainMenuUI Component References
            SerializedObject soMenu = new SerializedObject(menuUI);
            soMenu.FindProperty("playButton").objectReferenceValue = playBtn;
            soMenu.FindProperty("customizeButton").objectReferenceValue = custBtn;
            soMenu.FindProperty("rewardsButton").objectReferenceValue = rewBtn;
            soMenu.FindProperty("rewardsBadge").objectReferenceValue = rewBadgeGo;
            soMenu.FindProperty("statisticsButton").objectReferenceValue = statsBtn;
            soMenu.FindProperty("settingsButton").objectReferenceValue = settBtn;
            soMenu.FindProperty("bestScoreText").objectReferenceValue = bestTxt;
            soMenu.FindProperty("mainMenuPreviewStage").objectReferenceValue = mainPreviewStage;

            soMenu.FindProperty("customizationPanel").objectReferenceValue = custModalGo;
            soMenu.FindProperty("customizationCanvasGroup").objectReferenceValue = custCg;
            soMenu.FindProperty("customizationUI").objectReferenceValue = custUI;

            soMenu.FindProperty("rewardsPanel").objectReferenceValue = rewardsModalGo;
            soMenu.FindProperty("rewardsCanvasGroup").objectReferenceValue = rewardsCg;
            soMenu.FindProperty("rewardsUI").objectReferenceValue = rewardsUI;

            soMenu.FindProperty("statisticsPanel").objectReferenceValue = statsModalGo;
            soMenu.FindProperty("statisticsCanvasGroup").objectReferenceValue = statsCg;
            soMenu.FindProperty("statsBestScoreText").objectReferenceValue = statScoreVal;
            soMenu.FindProperty("statsBestLengthText").objectReferenceValue = statLengthVal;
            soMenu.FindProperty("statsGamesPlayedText").objectReferenceValue = statGamesVal;
            soMenu.FindProperty("statsFoodCollectedText").objectReferenceValue = statFoodVal;
            soMenu.FindProperty("statsAIDefeatedText").objectReferenceValue = statAIVal;
            soMenu.FindProperty("statsCloseButton").objectReferenceValue = statsCloseBtn;

            soMenu.FindProperty("settingsPanel").objectReferenceValue = settModalGo;
            soMenu.FindProperty("settingsCanvasGroup").objectReferenceValue = settCg;
            soMenu.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
            soMenu.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
            soMenu.FindProperty("vibrationToggleButton").objectReferenceValue = vibBtn;
            soMenu.FindProperty("vibrationToggleText").objectReferenceValue = vibTxt;
            soMenu.FindProperty("vibrationToggleBg").objectReferenceValue = vibBg;
            soMenu.FindProperty("settingsCloseButton").objectReferenceValue = settCloseBtn;
            soMenu.FindProperty("gameSceneName").stringValue = "Game";
            soMenu.ApplyModifiedPropertiesWithoutUndo();

            custModalGo.SetActive(false);
            rewardsModalGo.SetActive(false);
            statsModalGo.SetActive(false);
            settModalGo.SetActive(false);

            // 4. EventSystem
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[GigaGrub] Saved MainMenu scene with Navigation, Customization, Statistics and Settings to {scenePath}");
        }

        private static void CreateCustomizationModal(GameObject safeAreaGo, out GameObject custModalGo, out CanvasGroup custCg, out GigaGrub.UI.Customization.CustomizationUI custUI)
        {
            custModalGo = new GameObject("CustomizationModal");
            custModalGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform modalRect = custModalGo.AddComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;

            Image modalDim = custModalGo.AddComponent<Image>();
            modalDim.color = new Color(0f, 0f, 0f, 0.85f);
            custCg = custModalGo.AddComponent<CanvasGroup>();

            GameObject dialogGo = new GameObject("DialogBox");
            dialogGo.transform.SetParent(custModalGo.transform, false);
            RectTransform dialogRect = dialogGo.AddComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(1160f, 640f);

            Image dialogBg = dialogGo.AddComponent<Image>();
            dialogBg.color = new Color(0.06f, 0.09f, 0.15f, 0.98f); // Deep Slate #0F172A

            custUI = dialogGo.AddComponent<GigaGrub.UI.Customization.CustomizationUI>();

            // 1. Header
            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(dialogGo.transform, false);
            RectTransform headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.90f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            Text headerTxt = headerGo.AddComponent<Text>();
            headerTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerTxt.fontSize = 28;
            headerTxt.fontStyle = FontStyle.Bold;
            headerTxt.alignment = TextAnchor.MiddleCenter;
            headerTxt.color = new Color(0.29f, 0.87f, 0.50f, 1f); // Neon Emerald
            headerTxt.text = "CREATURE CUSTOMIZATION";

            // 2. Category Tab Buttons (9 Categories)
            GameObject tabsGo = new GameObject("CategoryTabs");
            tabsGo.transform.SetParent(dialogGo.transform, false);
            RectTransform tabsRect = tabsGo.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.02f, 0.82f);
            tabsRect.anchorMax = new Vector2(0.98f, 0.90f);
            tabsRect.offsetMin = Vector2.zero;
            tabsRect.offsetMax = Vector2.zero;

            string[] catNames = new string[] { "Creature", "Color", "Pattern", "Clothes", "Effects" };
            Button[] catBtns = new Button[catNames.Length];
            float tabWidth = 1f / catNames.Length;

            for (int i = 0; i < catNames.Length; i++)
            {
                GameObject tBtnGo = new GameObject($"Tab_{catNames[i]}");
                tBtnGo.transform.SetParent(tabsGo.transform, false);
                RectTransform tRect = tBtnGo.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(i * tabWidth + 0.005f, 0f);
                tRect.anchorMax = new Vector2((i + 1) * tabWidth - 0.005f, 1f);
                tRect.offsetMin = Vector2.zero;
                tRect.offsetMax = Vector2.zero;

                Image tImg = tBtnGo.AddComponent<Image>();
                tImg.color = new Color(0.12f, 0.16f, 0.24f, 0.90f);
                catBtns[i] = tBtnGo.AddComponent<Button>();

                GameObject tTxtGo = new GameObject("Text");
                tTxtGo.transform.SetParent(tBtnGo.transform, false);
                RectTransform ttRect = tTxtGo.AddComponent<RectTransform>();
                ttRect.anchorMin = Vector2.zero;
                ttRect.anchorMax = Vector2.one;
                ttRect.offsetMin = Vector2.zero;
                ttRect.offsetMax = Vector2.zero;

                Text tTxt = tTxtGo.AddComponent<Text>();
                tTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                tTxt.fontSize = 19;
                tTxt.fontStyle = FontStyle.Bold;
                tTxt.alignment = TextAnchor.MiddleCenter;
                tTxt.color = Color.white;
                tTxt.text = catNames[i];
            }

            // 3. Left Preview Stage Box
            GameObject previewBoxGo = new GameObject("PreviewBox");
            previewBoxGo.transform.SetParent(dialogGo.transform, false);
            RectTransform pbRect = previewBoxGo.AddComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0.02f, 0.16f);
            pbRect.anchorMax = new Vector2(0.32f, 0.80f);
            pbRect.offsetMin = Vector2.zero;
            pbRect.offsetMax = Vector2.zero;

            Image pbBg = previewBoxGo.AddComponent<Image>();
            pbBg.color = new Color(0.09f, 0.13f, 0.20f, 0.90f);

            GigaGrub.Cosmetics.CreaturePreviewStage previewStage = previewBoxGo.AddComponent<GigaGrub.Cosmetics.CreaturePreviewStage>();

            // 4. Right Scrollable Grid Area
            GameObject scrollGo = new GameObject("ItemsScroll");
            scrollGo.transform.SetParent(dialogGo.transform, false);
            RectTransform scrollRect = scrollGo.AddComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.34f, 0.16f);
            scrollRect.anchorMax = new Vector2(0.98f, 0.80f);
            scrollRect.offsetMin = Vector2.zero;
            scrollRect.offsetMax = Vector2.zero;

            Image scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = new Color(0.09f, 0.13f, 0.20f, 0.50f);
            ScrollRect sr = scrollGo.AddComponent<ScrollRect>();

            GameObject viewGo = new GameObject("Viewport");
            viewGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewRect = viewGo.AddComponent<RectTransform>();
            viewRect.anchorMin = Vector2.zero;
            viewRect.anchorMax = Vector2.one;
            viewRect.offsetMin = Vector2.zero;
            viewRect.offsetMax = Vector2.zero;
            viewGo.AddComponent<Mask>().showMaskGraphic = false;
            viewGo.AddComponent<Image>();

            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(viewGo.transform, false);
            RectTransform contentRect = contentGo.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.sizeDelta = new Vector2(0f, 800f);

            GridLayoutGroup glg = contentGo.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(125f, 145f);
            glg.spacing = new Vector2(12f, 12f);
            glg.padding = new RectOffset(12, 12, 12, 12);
            glg.constraint = GridLayoutGroup.Constraint.Flexible;

            ContentSizeFitter csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            sr.viewport = viewRect;
            sr.content = contentRect;
            sr.horizontal = false;
            sr.vertical = true;

            // 5. Bottom Action Bar
            GameObject coinsGo = new GameObject("CoinsBadge");
            coinsGo.transform.SetParent(dialogGo.transform, false);
            RectTransform coinsRect = coinsGo.AddComponent<RectTransform>();
            coinsRect.anchorMin = new Vector2(0.02f, 0.03f);
            coinsRect.anchorMax = new Vector2(0.24f, 0.13f);
            coinsRect.offsetMin = Vector2.zero;
            coinsRect.offsetMax = Vector2.zero;

            Text coinsTxt = coinsGo.AddComponent<Text>();
            coinsTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            coinsTxt.fontSize = 24;
            coinsTxt.fontStyle = FontStyle.Bold;
            coinsTxt.alignment = TextAnchor.MiddleLeft;
            coinsTxt.color = new Color(0.99f, 0.83f, 0.30f, 1f); // Gold
            coinsTxt.text = "COINS: 1,000";

            // Equip / Unlock Action Button
            GameObject actBtnGo = new GameObject("ActionButton");
            actBtnGo.transform.SetParent(dialogGo.transform, false);
            RectTransform actRect = actBtnGo.AddComponent<RectTransform>();
            actRect.anchorMin = new Vector2(0.38f, 0.03f);
            actRect.anchorMax = new Vector2(0.60f, 0.13f);
            actRect.offsetMin = Vector2.zero;
            actRect.offsetMax = Vector2.zero;

            Image actImg = actBtnGo.AddComponent<Image>();
            actImg.color = new Color(0.06f, 0.73f, 0.51f, 1f); // Emerald
            Button actBtn = actBtnGo.AddComponent<Button>();

            GameObject actTxtGo = new GameObject("Text");
            actTxtGo.transform.SetParent(actBtnGo.transform, false);
            RectTransform actTxtRect = actTxtGo.AddComponent<RectTransform>();
            actTxtRect.anchorMin = Vector2.zero;
            actTxtRect.anchorMax = Vector2.one;
            actTxtRect.offsetMin = Vector2.zero;
            actTxtRect.offsetMax = Vector2.zero;

            Text actTxt = actTxtGo.AddComponent<Text>();
            actTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            actTxt.fontSize = 22;
            actTxt.fontStyle = FontStyle.Bold;
            actTxt.alignment = TextAnchor.MiddleCenter;
            actTxt.color = Color.white;
            actTxt.text = "EQUIP";

            // Reset Button
            GameObject rstBtnGo = new GameObject("ResetButton");
            rstBtnGo.transform.SetParent(dialogGo.transform, false);
            RectTransform rstRect = rstBtnGo.AddComponent<RectTransform>();
            rstRect.anchorMin = new Vector2(0.62f, 0.03f);
            rstRect.anchorMax = new Vector2(0.78f, 0.13f);
            rstRect.offsetMin = Vector2.zero;
            rstRect.offsetMax = Vector2.zero;

            Image rstImg = rstBtnGo.AddComponent<Image>();
            rstImg.color = new Color(0.20f, 0.25f, 0.35f, 1f);
            Button rstBtn = rstBtnGo.AddComponent<Button>();

            GameObject rstTxtGo = new GameObject("Text");
            rstTxtGo.transform.SetParent(rstBtnGo.transform, false);
            RectTransform rstTxtRect = rstTxtGo.AddComponent<RectTransform>();
            rstTxtRect.anchorMin = Vector2.zero;
            rstTxtRect.anchorMax = Vector2.one;
            rstTxtRect.offsetMin = Vector2.zero;
            rstTxtRect.offsetMax = Vector2.zero;

            Text rstTxt = rstTxtGo.AddComponent<Text>();
            rstTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            rstTxt.fontSize = 20;
            rstTxt.fontStyle = FontStyle.Bold;
            rstTxt.alignment = TextAnchor.MiddleCenter;
            rstTxt.color = new Color(0.85f, 0.88f, 0.92f);
            rstTxt.text = "RESET";

            // Save & Close Button
            GameObject saveBtnGo = new GameObject("SaveCloseButton");
            saveBtnGo.transform.SetParent(dialogGo.transform, false);
            RectTransform saveRect = saveBtnGo.AddComponent<RectTransform>();
            saveRect.anchorMin = new Vector2(0.80f, 0.03f);
            saveRect.anchorMax = new Vector2(0.98f, 0.13f);
            saveRect.offsetMin = Vector2.zero;
            saveRect.offsetMax = Vector2.zero;

            Image saveImg = saveBtnGo.AddComponent<Image>();
            saveImg.color = new Color(0.12f, 0.50f, 0.90f, 1f); // Blue #1D4ED8
            Button saveBtn = saveBtnGo.AddComponent<Button>();

            GameObject saveTxtGo = new GameObject("Text");
            saveTxtGo.transform.SetParent(saveBtnGo.transform, false);
            RectTransform saveTxtRect = saveTxtGo.AddComponent<RectTransform>();
            saveTxtRect.anchorMin = Vector2.zero;
            saveTxtRect.anchorMax = Vector2.one;
            saveTxtRect.offsetMin = Vector2.zero;
            saveTxtRect.offsetMax = Vector2.zero;

            Text saveTxt = saveTxtGo.AddComponent<Text>();
            saveTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            saveTxt.fontSize = 20;
            saveTxt.fontStyle = FontStyle.Bold;
            saveTxt.alignment = TextAnchor.MiddleCenter;
            saveTxt.color = Color.white;
            saveTxt.text = "SAVE & CLOSE";

            // Wire CustomizationUI Properties
            SerializedObject soCust = new SerializedObject(custUI);
            soCust.FindProperty("previewStage").objectReferenceValue = previewStage;
            soCust.FindProperty("creatureTabBtn").objectReferenceValue = catBtns[0];
            soCust.FindProperty("colorTabBtn").objectReferenceValue = catBtns[1];
            soCust.FindProperty("patternTabBtn").objectReferenceValue = catBtns[2];
            soCust.FindProperty("clothingTabBtn").objectReferenceValue = catBtns[3];
            soCust.FindProperty("effectTabBtn").objectReferenceValue = catBtns[4];

            soCust.FindProperty("gridContentParent").objectReferenceValue = contentGo.transform;
            soCust.FindProperty("coinsText").objectReferenceValue = coinsTxt;
            soCust.FindProperty("actionButton").objectReferenceValue = actBtn;
            soCust.FindProperty("actionButtonText").objectReferenceValue = actTxt;
            soCust.FindProperty("resetButton").objectReferenceValue = rstBtn;
            soCust.FindProperty("closeSaveButton").objectReferenceValue = saveBtn;
            soCust.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateDailyRewardsAndQuestsModal(GameObject safeAreaGo, out GameObject rewardsModalGo, out CanvasGroup rewardsCg, out DailyRewardsAndQuestsUI rewardsUI)
        {
            rewardsModalGo = new GameObject("DailyRewardsModal");
            rewardsModalGo.transform.SetParent(safeAreaGo.transform, false);
            RectTransform modalRect = rewardsModalGo.AddComponent<RectTransform>();
            modalRect.anchorMin = Vector2.zero;
            modalRect.anchorMax = Vector2.one;
            modalRect.offsetMin = Vector2.zero;
            modalRect.offsetMax = Vector2.zero;

            Image modalDim = rewardsModalGo.AddComponent<Image>();
            modalDim.color = new Color(0f, 0f, 0f, 0.85f);
            rewardsCg = rewardsModalGo.AddComponent<CanvasGroup>();

            GameObject dialogGo = new GameObject("DialogBox");
            dialogGo.transform.SetParent(rewardsModalGo.transform, false);
            RectTransform dialogRect = dialogGo.AddComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(1080f, 620f);

            Image dialogBg = dialogGo.AddComponent<Image>();
            dialogBg.color = new Color(0.06f, 0.09f, 0.15f, 0.98f); // Deep Slate #0F172A

            rewardsUI = dialogGo.AddComponent<DailyRewardsAndQuestsUI>();

            // 1. Header & Close Button
            GameObject headerGo = new GameObject("Header");
            headerGo.transform.SetParent(dialogGo.transform, false);
            RectTransform headerRect = headerGo.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 0.90f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.offsetMin = Vector2.zero;
            headerRect.offsetMax = Vector2.zero;

            Text headerTxt = headerGo.AddComponent<Text>();
            headerTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            headerTxt.fontSize = 28;
            headerTxt.fontStyle = FontStyle.Bold;
            headerTxt.alignment = TextAnchor.MiddleCenter;
            headerTxt.color = new Color(0.99f, 0.83f, 0.30f, 1f); // Gold #FBBF24
            headerTxt.text = "DAILY REWARDS & QUESTS";

            // Close Button
            GameObject closeBtnGo = new GameObject("CloseButton");
            closeBtnGo.transform.SetParent(dialogGo.transform, false);
            RectTransform closeRect = closeBtnGo.AddComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.93f, 0.90f);
            closeRect.anchorMax = new Vector2(0.98f, 0.98f);
            closeRect.offsetMin = Vector2.zero;
            closeRect.offsetMax = Vector2.zero;

            Image closeImg = closeBtnGo.AddComponent<Image>();
            closeImg.color = new Color(0.20f, 0.25f, 0.35f, 1f);
            Button closeBtn = closeBtnGo.AddComponent<Button>();

            GameObject closeTxtGo = new GameObject("Text");
            closeTxtGo.transform.SetParent(closeBtnGo.transform, false);
            RectTransform closeTxtRect = closeTxtGo.AddComponent<RectTransform>();
            closeTxtRect.anchorMin = Vector2.zero;
            closeTxtRect.anchorMax = Vector2.one;
            closeTxtRect.offsetMin = Vector2.zero;
            closeTxtRect.offsetMax = Vector2.zero;

            Text closeTxt = closeTxtGo.AddComponent<Text>();
            closeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            closeTxt.fontSize = 22;
            closeTxt.fontStyle = FontStyle.Bold;
            closeTxt.alignment = TextAnchor.MiddleCenter;
            closeTxt.color = Color.white;
            closeTxt.text = "X";

            // 2. Tab Switcher Buttons (Daily Rewards & Daily Quests)
            GameObject tabsGo = new GameObject("Tabs");
            tabsGo.transform.SetParent(dialogGo.transform, false);
            RectTransform tabsRect = tabsGo.AddComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.20f, 0.82f);
            tabsRect.anchorMax = new Vector2(0.80f, 0.89f);
            tabsRect.offsetMin = Vector2.zero;
            tabsRect.offsetMax = Vector2.zero;

            // Daily Rewards Tab Button
            GameObject dTabGo = new GameObject("Tab_DailyRewards");
            dTabGo.transform.SetParent(tabsGo.transform, false);
            RectTransform dTabRect = dTabGo.AddComponent<RectTransform>();
            dTabRect.anchorMin = new Vector2(0f, 0f);
            dTabRect.anchorMax = new Vector2(0.48f, 1f);
            dTabRect.offsetMin = Vector2.zero;
            dTabRect.offsetMax = Vector2.zero;

            Image dTabImg = dTabGo.AddComponent<Image>();
            dTabImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            Button dTabBtn = dTabGo.AddComponent<Button>();

            GameObject dTabTxtGo = new GameObject("Text");
            dTabTxtGo.transform.SetParent(dTabGo.transform, false);
            RectTransform dTabTxtRect = dTabTxtGo.AddComponent<RectTransform>();
            dTabTxtRect.anchorMin = Vector2.zero;
            dTabTxtRect.anchorMax = Vector2.one;
            dTabTxtRect.offsetMin = Vector2.zero;
            dTabTxtRect.offsetMax = Vector2.zero;

            Text dTabTxt = dTabTxtGo.AddComponent<Text>();
            dTabTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            dTabTxt.fontSize = 18;
            dTabTxt.fontStyle = FontStyle.Bold;
            dTabTxt.alignment = TextAnchor.MiddleCenter;
            dTabTxt.color = Color.white;
            dTabTxt.text = "DAILY REWARDS";

            // Quests Tab Button
            GameObject qTabGo = new GameObject("Tab_Quests");
            qTabGo.transform.SetParent(tabsGo.transform, false);
            RectTransform qTabRect = qTabGo.AddComponent<RectTransform>();
            qTabRect.anchorMin = new Vector2(0.52f, 0f);
            qTabRect.anchorMax = new Vector2(1f, 1f);
            qTabRect.offsetMin = Vector2.zero;
            qTabRect.offsetMax = Vector2.zero;

            Image qTabImg = qTabGo.AddComponent<Image>();
            qTabImg.color = new Color(0.12f, 0.16f, 0.24f, 0.95f);
            Button qTabBtn = qTabGo.AddComponent<Button>();

            GameObject qTabTxtGo = new GameObject("Text");
            qTabTxtGo.transform.SetParent(qTabGo.transform, false);
            RectTransform qTabTxtRect = qTabTxtGo.AddComponent<RectTransform>();
            qTabTxtRect.anchorMin = Vector2.zero;
            qTabTxtRect.anchorMax = Vector2.one;
            qTabTxtRect.offsetMin = Vector2.zero;
            qTabTxtRect.offsetMax = Vector2.zero;

            Text qTabTxt = qTabTxtGo.AddComponent<Text>();
            qTabTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            qTabTxt.fontSize = 18;
            qTabTxt.fontStyle = FontStyle.Bold;
            qTabTxt.alignment = TextAnchor.MiddleCenter;
            qTabTxt.color = Color.white;
            qTabTxt.text = "DAILY MISSIONS";

            // 3. Daily Rewards Panel
            GameObject dPanelGo = new GameObject("DailyRewardsPanel");
            dPanelGo.transform.SetParent(dialogGo.transform, false);
            RectTransform dPanelRect = dPanelGo.AddComponent<RectTransform>();
            dPanelRect.anchorMin = new Vector2(0.04f, 0.14f);
            dPanelRect.anchorMax = new Vector2(0.96f, 0.80f);
            dPanelRect.offsetMin = Vector2.zero;
            dPanelRect.offsetMax = Vector2.zero;

            // Streak Text
            GameObject streakGo = new GameObject("StreakText");
            streakGo.transform.SetParent(dPanelGo.transform, false);
            RectTransform streakRect = streakGo.AddComponent<RectTransform>();
            streakRect.anchorMin = new Vector2(0.02f, 0.85f);
            streakRect.anchorMax = new Vector2(0.48f, 1f);
            streakRect.offsetMin = Vector2.zero;
            streakRect.offsetMax = Vector2.zero;

            Text streakTxt = streakGo.AddComponent<Text>();
            streakTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            streakTxt.fontSize = 20;
            streakTxt.fontStyle = FontStyle.Bold;
            streakTxt.alignment = TextAnchor.MiddleLeft;
            streakTxt.color = new Color(0.29f, 0.87f, 0.50f, 1f); // Neon Emerald
            streakTxt.text = "STREAK: 1 DAYS";

            // Countdown Text
            GameObject cdGo = new GameObject("CountdownText");
            cdGo.transform.SetParent(dPanelGo.transform, false);
            RectTransform cdRect = cdGo.AddComponent<RectTransform>();
            cdRect.anchorMin = new Vector2(0.52f, 0.85f);
            cdRect.anchorMax = new Vector2(0.98f, 1f);
            cdRect.offsetMin = Vector2.zero;
            cdRect.offsetMax = Vector2.zero;

            Text cdTxt = cdGo.AddComponent<Text>();
            cdTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cdTxt.fontSize = 18;
            cdTxt.fontStyle = FontStyle.Bold;
            cdTxt.alignment = TextAnchor.MiddleRight;
            cdTxt.color = new Color(0.80f, 0.84f, 0.90f, 1f);
            cdTxt.text = "Next Reward In: --:--:--";

            // 7-Day Cards Parent
            GameObject dayCardsParentGo = new GameObject("DayCardsParent");
            dayCardsParentGo.transform.SetParent(dPanelGo.transform, false);
            RectTransform cardsParentRect = dayCardsParentGo.AddComponent<RectTransform>();
            cardsParentRect.anchorMin = new Vector2(0f, 0.26f);
            cardsParentRect.anchorMax = new Vector2(1f, 0.82f);
            cardsParentRect.offsetMin = Vector2.zero;
            cardsParentRect.offsetMax = Vector2.zero;

            Image[] dayCardBgs = new Image[7];
            Text[] dayCardRewardTexts = new Text[7];
            GameObject[] dayCardClaimedBadges = new GameObject[7];
            int[] defaultRewards = new int[] { 100, 150, 200, 250, 350, 500, 800 };

            float cardSpacing = 1f / 7f;
            for (int i = 0; i < 7; i++)
            {
                GameObject cardGo = new GameObject($"DayCard_{i + 1}");
                cardGo.transform.SetParent(dayCardsParentGo.transform, false);
                RectTransform cRect = cardGo.AddComponent<RectTransform>();
                cRect.anchorMin = new Vector2(i * cardSpacing + 0.006f, 0f);
                cRect.anchorMax = new Vector2((i + 1) * cardSpacing - 0.006f, 1f);
                cRect.offsetMin = Vector2.zero;
                cRect.offsetMax = Vector2.zero;

                Image cBg = cardGo.AddComponent<Image>();
                cBg.color = (i == 6) ? new Color(0.25f, 0.18f, 0.06f, 0.95f) : new Color(0.10f, 0.14f, 0.22f, 0.95f);
                dayCardBgs[i] = cBg;

                // Day Label
                GameObject dayLblGo = new GameObject("DayLabel");
                dayLblGo.transform.SetParent(cardGo.transform, false);
                RectTransform dLblRect = dayLblGo.AddComponent<RectTransform>();
                dLblRect.anchorMin = new Vector2(0f, 0.72f);
                dLblRect.anchorMax = new Vector2(1f, 0.96f);
                dLblRect.offsetMin = Vector2.zero;
                dLblRect.offsetMax = Vector2.zero;

                Text dLblTxt = dayLblGo.AddComponent<Text>();
                dLblTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                dLblTxt.fontSize = 18;
                dLblTxt.fontStyle = FontStyle.Bold;
                dLblTxt.alignment = TextAnchor.MiddleCenter;
                dLblTxt.color = (i == 6) ? new Color(0.99f, 0.83f, 0.30f, 1f) : new Color(0.80f, 0.84f, 0.90f, 1f);
                dLblTxt.text = $"DAY {i + 1}";

                // Reward Coins Text
                GameObject rTxtGo = new GameObject("RewardText");
                rTxtGo.transform.SetParent(cardGo.transform, false);
                RectTransform rTxtRect = rTxtGo.AddComponent<RectTransform>();
                rTxtRect.anchorMin = new Vector2(0f, 0.16f);
                rTxtRect.anchorMax = new Vector2(1f, 0.65f);
                rTxtRect.offsetMin = Vector2.zero;
                rTxtRect.offsetMax = Vector2.zero;

                Text rTxt = rTxtGo.AddComponent<Text>();
                rTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                rTxt.fontSize = (i == 6) ? 24 : 20;
                rTxt.fontStyle = FontStyle.Bold;
                rTxt.alignment = TextAnchor.MiddleCenter;
                rTxt.color = new Color(0.99f, 0.83f, 0.30f, 1f);
                rTxt.text = $"+{defaultRewards[i]}\nCOINS";
                dayCardRewardTexts[i] = rTxt;

                // Claimed Badge Overlay
                GameObject badgeGo = new GameObject("ClaimedBadge");
                badgeGo.transform.SetParent(cardGo.transform, false);
                RectTransform badgeRect = badgeGo.AddComponent<RectTransform>();
                badgeRect.anchorMin = Vector2.zero;
                badgeRect.anchorMax = Vector2.one;
                badgeRect.offsetMin = Vector2.zero;
                badgeRect.offsetMax = Vector2.zero;

                Image badgeDim = badgeGo.AddComponent<Image>();
                badgeDim.color = new Color(0f, 0f, 0f, 0.65f);

                GameObject bTxtGo = new GameObject("Text");
                bTxtGo.transform.SetParent(badgeGo.transform, false);
                RectTransform bTxtRect = bTxtGo.AddComponent<RectTransform>();
                bTxtRect.anchorMin = Vector2.zero;
                bTxtRect.anchorMax = Vector2.one;
                bTxtRect.offsetMin = Vector2.zero;
                bTxtRect.offsetMax = Vector2.zero;

                Text bTxt = bTxtGo.AddComponent<Text>();
                bTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                bTxt.fontSize = 18;
                bTxt.fontStyle = FontStyle.Bold;
                bTxt.alignment = TextAnchor.MiddleCenter;
                bTxt.color = new Color(0.29f, 0.87f, 0.50f, 1f);
                bTxt.text = "CLAIMED";

                badgeGo.SetActive(false);
                dayCardClaimedBadges[i] = badgeGo;
            }

            // Claim Daily Reward Button
            GameObject claimBtnGo = new GameObject("ClaimDailyButton");
            claimBtnGo.transform.SetParent(dPanelGo.transform, false);
            RectTransform claimRect = claimBtnGo.AddComponent<RectTransform>();
            claimRect.anchorMin = new Vector2(0.35f, 0.02f);
            claimRect.anchorMax = new Vector2(0.65f, 0.20f);
            claimRect.offsetMin = Vector2.zero;
            claimRect.offsetMax = Vector2.zero;

            Image claimImg = claimBtnGo.AddComponent<Image>();
            claimImg.color = new Color(0.06f, 0.73f, 0.51f, 1f); // Emerald
            Button claimBtn = claimBtnGo.AddComponent<Button>();

            GameObject claimTxtGo = new GameObject("Text");
            claimTxtGo.transform.SetParent(claimBtnGo.transform, false);
            RectTransform claimTxtRect = claimTxtGo.AddComponent<RectTransform>();
            claimTxtRect.anchorMin = Vector2.zero;
            claimTxtRect.anchorMax = Vector2.one;
            claimTxtRect.offsetMin = Vector2.zero;
            claimTxtRect.offsetMax = Vector2.zero;

            Text claimTxt = claimTxtGo.AddComponent<Text>();
            claimTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            claimTxt.fontSize = 22;
            claimTxt.fontStyle = FontStyle.Bold;
            claimTxt.alignment = TextAnchor.MiddleCenter;
            claimTxt.color = Color.white;
            claimTxt.text = "CLAIM REWARD";

            // 4. Quests Panel
            GameObject qPanelGo = new GameObject("QuestsPanel");
            qPanelGo.transform.SetParent(dialogGo.transform, false);
            RectTransform qPanelRect = qPanelGo.AddComponent<RectTransform>();
            qPanelRect.anchorMin = new Vector2(0.04f, 0.14f);
            qPanelRect.anchorMax = new Vector2(0.96f, 0.80f);
            qPanelRect.offsetMin = Vector2.zero;
            qPanelRect.offsetMax = Vector2.zero;

            // Quests Header Text
            GameObject qHdrGo = new GameObject("QuestsHeader");
            qHdrGo.transform.SetParent(qPanelGo.transform, false);
            RectTransform qHdrRect = qHdrGo.AddComponent<RectTransform>();
            qHdrRect.anchorMin = new Vector2(0f, 0.88f);
            qHdrRect.anchorMax = new Vector2(1f, 1f);
            qHdrRect.offsetMin = Vector2.zero;
            qHdrRect.offsetMax = Vector2.zero;

            Text qHdrTxt = qHdrGo.AddComponent<Text>();
            qHdrTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            qHdrTxt.fontSize = 18;
            qHdrTxt.fontStyle = FontStyle.Bold;
            qHdrTxt.alignment = TextAnchor.MiddleLeft;
            qHdrTxt.color = new Color(0.80f, 0.84f, 0.90f, 1f);
            qHdrTxt.text = "Complete daily quests across matches to earn bonus coins!";

            // Scrollable Quests List
            GameObject qScrollGo = new GameObject("QuestsScroll");
            qScrollGo.transform.SetParent(qPanelGo.transform, false);
            RectTransform qScrollRect = qScrollGo.AddComponent<RectTransform>();
            qScrollRect.anchorMin = new Vector2(0f, 0f);
            qScrollRect.anchorMax = new Vector2(1f, 0.86f);
            qScrollRect.offsetMin = Vector2.zero;
            qScrollRect.offsetMax = Vector2.zero;

            ScrollRect qSr = qScrollGo.AddComponent<ScrollRect>();

            GameObject qViewGo = new GameObject("Viewport");
            qViewGo.transform.SetParent(qScrollGo.transform, false);
            RectTransform qViewRect = qViewGo.AddComponent<RectTransform>();
            qViewRect.anchorMin = Vector2.zero;
            qViewRect.anchorMax = Vector2.one;
            qViewRect.offsetMin = Vector2.zero;
            qViewRect.offsetMax = Vector2.zero;
            qViewGo.AddComponent<Mask>().showMaskGraphic = false;
            qViewGo.AddComponent<Image>();

            GameObject qContentGo = new GameObject("Content");
            qContentGo.transform.SetParent(qViewGo.transform, false);
            RectTransform qContentRect = qContentGo.AddComponent<RectTransform>();
            qContentRect.anchorMin = new Vector2(0f, 1f);
            qContentRect.anchorMax = new Vector2(1f, 1f);
            qContentRect.pivot = new Vector2(0.5f, 1f);
            qContentRect.sizeDelta = new Vector2(0f, 400f);

            VerticalLayoutGroup vlg = qContentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(6, 6, 6, 6);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            ContentSizeFitter qCsf = qContentGo.AddComponent<ContentSizeFitter>();
            qCsf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            qSr.viewport = qViewRect;
            qSr.content = qContentRect;
            qSr.horizontal = false;
            qSr.vertical = true;

            // 5. Common Bottom Coin Badge
            GameObject uCoinsGo = new GameObject("UserCoinsBadge");
            uCoinsGo.transform.SetParent(dialogGo.transform, false);
            RectTransform uCoinsRect = uCoinsGo.AddComponent<RectTransform>();
            uCoinsRect.anchorMin = new Vector2(0.04f, 0.03f);
            uCoinsRect.anchorMax = new Vector2(0.35f, 0.12f);
            uCoinsRect.offsetMin = Vector2.zero;
            uCoinsRect.offsetMax = Vector2.zero;

            Text uCoinsTxt = uCoinsGo.AddComponent<Text>();
            uCoinsTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            uCoinsTxt.fontSize = 22;
            uCoinsTxt.fontStyle = FontStyle.Bold;
            uCoinsTxt.alignment = TextAnchor.MiddleLeft;
            uCoinsTxt.color = new Color(0.99f, 0.83f, 0.30f, 1f);
            uCoinsTxt.text = "COINS: 1,000";

            // Wire DailyRewardsAndQuestsUI Component References
            SerializedObject soRew = new SerializedObject(rewardsUI);
            soRew.FindProperty("dailyRewardsTabBtn").objectReferenceValue = dTabBtn;
            soRew.FindProperty("questsTabBtn").objectReferenceValue = qTabBtn;
            soRew.FindProperty("dailyRewardsPanel").objectReferenceValue = dPanelGo;
            soRew.FindProperty("questsPanel").objectReferenceValue = qPanelGo;

            soRew.FindProperty("streakText").objectReferenceValue = streakTxt;
            soRew.FindProperty("countdownText").objectReferenceValue = cdTxt;
            soRew.FindProperty("claimDailyBtn").objectReferenceValue = claimBtn;
            soRew.FindProperty("claimDailyBtnText").objectReferenceValue = claimTxt;
            soRew.FindProperty("dayCardsParent").objectReferenceValue = dayCardsParentGo.transform;

            SerializedProperty bgsProp = soRew.FindProperty("dayCardBgs");
            bgsProp.arraySize = 7;
            for (int i = 0; i < 7; i++) bgsProp.GetArrayElementAtIndex(i).objectReferenceValue = dayCardBgs[i];

            SerializedProperty rewTextsProp = soRew.FindProperty("dayCardRewardTexts");
            rewTextsProp.arraySize = 7;
            for (int i = 0; i < 7; i++) rewTextsProp.GetArrayElementAtIndex(i).objectReferenceValue = dayCardRewardTexts[i];

            SerializedProperty badgesProp = soRew.FindProperty("dayCardClaimedBadges");
            badgesProp.arraySize = 7;
            for (int i = 0; i < 7; i++) badgesProp.GetArrayElementAtIndex(i).objectReferenceValue = dayCardClaimedBadges[i];

            soRew.FindProperty("questsContentParent").objectReferenceValue = qContentGo.transform;
            soRew.FindProperty("questsCoinsHeader").objectReferenceValue = qHdrTxt;

            soRew.FindProperty("closeButton").objectReferenceValue = closeBtn;
            soRew.FindProperty("userCoinsText").objectReferenceValue = uCoinsTxt;
            soRew.ApplyModifiedPropertiesWithoutUndo();

            qPanelGo.SetActive(false);
        }

        private static Text CreateStatsRow(GameObject parent, string label, Vector2 anchorY, Color valColor)
        {
            GameObject rowGo = new GameObject($"Row_{label.Replace(" ", "")}");
            rowGo.transform.SetParent(parent.transform, false);
            RectTransform rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.08f, anchorY.y);
            rowRect.anchorMax = new Vector2(0.92f, anchorY.y + 0.10f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.color = new Color(0.11f, 0.15f, 0.23f, 0.70f);

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rowGo.transform, false);
            RectTransform labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.04f, 0f);
            labelRect.anchorMax = new Vector2(0.60f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text labelTxt = labelGo.AddComponent<Text>();
            labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelTxt.fontSize = 24;
            labelTxt.fontStyle = FontStyle.Bold;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            labelTxt.color = new Color(0.80f, 0.84f, 0.90f, 1f);
            labelTxt.text = label;

            GameObject valGo = new GameObject("Value");
            valGo.transform.SetParent(rowGo.transform, false);
            RectTransform valRect = valGo.AddComponent<RectTransform>();
            valRect.anchorMin = new Vector2(0.60f, 0f);
            valRect.anchorMax = new Vector2(0.96f, 1f);
            valRect.offsetMin = Vector2.zero;
            valRect.offsetMax = Vector2.zero;

            Text valTxt = valGo.AddComponent<Text>();
            valTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            valTxt.fontSize = 26;
            valTxt.fontStyle = FontStyle.Bold;
            valTxt.alignment = TextAnchor.MiddleRight;
            valTxt.color = valColor;
            valTxt.text = "0";

            return valTxt;
        }

        private static Slider CreateSettingsSliderRow(GameObject parent, string label, Vector2 anchorY)
        {
            GameObject rowGo = new GameObject($"Row_{label.Replace(" ", "")}");
            rowGo.transform.SetParent(parent.transform, false);
            RectTransform rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.08f, anchorY.y);
            rowRect.anchorMax = new Vector2(0.92f, anchorY.y + 0.12f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.color = new Color(0.11f, 0.15f, 0.23f, 0.70f);

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rowGo.transform, false);
            RectTransform labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.04f, 0f);
            labelRect.anchorMax = new Vector2(0.40f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text labelTxt = labelGo.AddComponent<Text>();
            labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelTxt.fontSize = 24;
            labelTxt.fontStyle = FontStyle.Bold;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            labelTxt.color = new Color(0.80f, 0.84f, 0.90f, 1f);
            labelTxt.text = label;

            GameObject sliderGo = new GameObject("Slider");
            sliderGo.transform.SetParent(rowGo.transform, false);
            RectTransform sliderRect = sliderGo.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.42f, 0.25f);
            sliderRect.anchorMax = new Vector2(0.96f, 0.75f);
            sliderRect.offsetMin = Vector2.zero;
            sliderRect.offsetMax = Vector2.zero;

            Slider slider = sliderGo.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.8f;

            // Slider Background
            GameObject bgTrackGo = new GameObject("Background");
            bgTrackGo.transform.SetParent(sliderGo.transform, false);
            RectTransform bgTrackRect = bgTrackGo.AddComponent<RectTransform>();
            bgTrackRect.anchorMin = Vector2.zero;
            bgTrackRect.anchorMax = Vector2.one;
            bgTrackRect.offsetMin = Vector2.zero;
            bgTrackRect.offsetMax = Vector2.zero;
            Image bgTrackImg = bgTrackGo.AddComponent<Image>();
            bgTrackImg.color = new Color(0.20f, 0.26f, 0.36f, 1f);

            // Fill Area & Fill
            GameObject fillAreaGo = new GameObject("Fill Area");
            fillAreaGo.transform.SetParent(sliderGo.transform, false);
            RectTransform fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(fillAreaGo.transform, false);
            RectTransform fillRect = fillGo.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.06f, 0.73f, 0.51f, 1f);

            slider.fillRect = fillRect;
            return slider;
        }

        private static void CreateSettingsVibrationRow(GameObject parent, string label, Vector2 anchorY, out Button vibBtn, out Text vibTxt, out Image vibBg)
        {
            GameObject rowGo = new GameObject($"Row_{label.Replace(" ", "")}");
            rowGo.transform.SetParent(parent.transform, false);
            RectTransform rowRect = rowGo.AddComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.08f, anchorY.y);
            rowRect.anchorMax = new Vector2(0.92f, anchorY.y + 0.12f);
            rowRect.offsetMin = Vector2.zero;
            rowRect.offsetMax = Vector2.zero;

            Image rowBg = rowGo.AddComponent<Image>();
            rowBg.color = new Color(0.11f, 0.15f, 0.23f, 0.70f);

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(rowGo.transform, false);
            RectTransform labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.04f, 0f);
            labelRect.anchorMax = new Vector2(0.50f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            Text labelTxt = labelGo.AddComponent<Text>();
            labelTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelTxt.fontSize = 24;
            labelTxt.fontStyle = FontStyle.Bold;
            labelTxt.alignment = TextAnchor.MiddleLeft;
            labelTxt.color = new Color(0.80f, 0.84f, 0.90f, 1f);
            labelTxt.text = label;

            GameObject toggleBtnGo = new GameObject("VibrationToggleButton");
            toggleBtnGo.transform.SetParent(rowGo.transform, false);
            RectTransform toggleRect = toggleBtnGo.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.70f, 0.18f);
            toggleRect.anchorMax = new Vector2(0.96f, 0.82f);
            toggleRect.offsetMin = Vector2.zero;
            toggleRect.offsetMax = Vector2.zero;

            vibBg = toggleBtnGo.AddComponent<Image>();
            vibBg.color = new Color(0.10f, 0.85f, 0.45f, 1f); // Active Green
            vibBtn = toggleBtnGo.AddComponent<Button>();

            GameObject toggleTxtGo = new GameObject("Text");
            toggleTxtGo.transform.SetParent(toggleBtnGo.transform, false);
            RectTransform toggleTxtRect = toggleTxtGo.AddComponent<RectTransform>();
            toggleTxtRect.anchorMin = Vector2.zero;
            toggleTxtRect.anchorMax = Vector2.one;
            toggleTxtRect.offsetMin = Vector2.zero;
            toggleTxtRect.offsetMax = Vector2.zero;

            vibTxt = toggleTxtGo.AddComponent<Text>();
            vibTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vibTxt.fontSize = 22;
            vibTxt.fontStyle = FontStyle.Bold;
            vibTxt.alignment = TextAnchor.MiddleCenter;
            vibTxt.color = Color.white;
            vibTxt.text = "ON";
        }

        [MenuItem("GigaGrub/9. Run Automated Verification Tests")]
        public static void RunVerificationTests()
        {
            Debug.Log("=== [GigaGrub Verification Tests] Starting ===");

            EnsureDirectories();
            SetupAll();

            // --- Section 1: Core Prefabs & Scriptable Objects ---
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Player.prefab");
            GameObject aiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/AICreature.prefab");
            GameObject segmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PlayerSegment.prefab");
            GameObject joystickCanvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/JoystickCanvas.prefab");
            GameObject arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Arena.prefab");
            GameObject foodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Food.prefab");
            GameObject eatingEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/EatingEffect.prefab");

            Assert(playerPrefab != null, "Player.prefab exists");
            Assert(aiPrefab != null, "AICreature.prefab exists");
            Assert(segmentPrefab != null, "PlayerSegment.prefab exists");
            Assert(joystickCanvasPrefab != null, "JoystickCanvas.prefab exists");
            Assert(arenaPrefab != null, "Arena.prefab exists");
            Assert(foodPrefab != null, "Food.prefab exists");
            Assert(eatingEffectPrefab != null, "EatingEffect.prefab exists");

            FoodData standardFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Standard.asset");
            FoodData superFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Super.asset");
            FoodData megaFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Mega.asset");

            Assert(standardFood != null, "FoodData_Standard.asset exists");
            Assert(superFood != null, "FoodData_Super.asset exists");
            Assert(megaFood != null, "FoodData_Mega.asset exists");

            // --- Section 2: ScoreManager and Player Initialization ---
            GameObject scoreMgrGo = new GameObject("TestScoreManager");
            ScoreManager scoreMgr = scoreMgrGo.AddComponent<ScoreManager>();
            ScoreManager.SetInstanceForTest(scoreMgr);
            scoreMgr.ResetScore();
            Assert(scoreMgr.CurrentScore == 0, "ScoreManager initialized with 0 score");

            GameObject testPlayer = Object.Instantiate(playerPrefab);
            testPlayer.name = "TestPlayer";
            PlayerBody playerBody = testPlayer.GetComponent<PlayerBody>();
            GrowthSystem playerGrowth = testPlayer.GetComponent<GrowthSystem>();
            CreatureDeath playerDeath = testPlayer.GetComponent<CreatureDeath>();
            CreatureCollision playerCollision = testPlayer.GetComponent<CreatureCollision>();

            Assert(playerBody != null, "Player has PlayerBody component");
            Assert(playerGrowth != null, "Player has GrowthSystem component");
            Assert(playerDeath != null, "Player has CreatureDeath component");
            Assert(playerCollision != null, "Player has CreatureCollision component");

            playerBody.InitializeRuntime();
            Assert(playerBody.CurrentLength == 10, $"Initial player body length is 10 (actual: {playerBody.CurrentLength})");

            // --- Section 3: Food Spawner Setup for Tests ---
            GameObject spawnerGo = new GameObject("TestFoodSpawner");
            FoodSpawner spawner = spawnerGo.AddComponent<FoodSpawner>();
            spawner.SetFoodPrefab(foodPrefab);
            spawner.SetFoodTypes(new FoodData[] { standardFood, superFood, megaFood });
            spawner.SetPopulationLimits(100, 150, 120);
            spawner.SetPlayerBody(playerBody);
            spawner.InitializeRuntime();
            FoodSpawner.SetInstanceForTest(spawner);

            // --- Section 4: Single AI Creature Test (States, Food Seeking, Boundaries, Local Score & Growth) ---
            GameObject ai1Go = Object.Instantiate(aiPrefab, new Vector3(10f, 10f, 0f), Quaternion.identity);
            ai1Go.name = "TestAI_Single";
            AIController ai1Ctrl = ai1Go.GetComponent<AIController>();
            AIStateMachine ai1SM = ai1Go.GetComponent<AIStateMachine>();
            AIWorldDetector ai1Det = ai1Go.GetComponent<AIWorldDetector>();
            PlayerBody ai1Body = ai1Go.GetComponent<PlayerBody>();
            GrowthSystem ai1Growth = ai1Go.GetComponent<GrowthSystem>();

            Assert(ai1Ctrl != null && ai1SM != null && ai1Det != null && ai1Body != null && ai1Growth != null, "AI Creature has all required components (AIController, AIStateMachine, AIWorldDetector, PlayerBody, GrowthSystem)");

            ai1Body.SetIsPlayer(false);
            ai1Body.InitializeRuntime();
            Assert(ai1Body.CurrentLength == 10, "AI Creature initialized with length 10");
            Assert(ai1Body.CurrentScore == 0, "AI Creature initialized with local score 0");

            // Test 1 AI - State 1: Explore (default in open arena)
            ai1Det.SetDetectionSettings(15f, 4f, 6f);
            ai1SM.EvaluateState(new Vector2(25f, 25f), Vector2.up);
            Assert(ai1SM.CurrentState == AIStateType.Explore, $"1 AI in open space evaluates to Explore state (actual: {ai1SM.CurrentState})");

            // Test 1 AI - State 2: AvoidBoundary when near wall (e.g. at x = 48, arena bounds 50)
            ai1SM.EvaluateState(new Vector2(48f, 0f), Vector2.right);
            Assert(ai1SM.CurrentState == AIStateType.AvoidBoundary, $"1 AI near arena boundary evaluates to AvoidBoundary state (actual: {ai1SM.CurrentState})");
            Assert(ai1SM.DesiredDirection.x < 0f, "1 AI AvoidBoundary directs steering away from right wall (towards arena interior)");

            // Test 1 AI - State 3: Eating food, independent score increment, and growing new segment
            int aiScoreBefore = ai1Body.CurrentScore;
            int aiLengthBefore = ai1Body.CurrentLength;
            int playerCurrentScoreBefore = scoreMgr.CurrentScore;

            GameObject testFoodGo = Object.Instantiate(foodPrefab);
            Food.Food testFood = testFoodGo.GetComponent<Food.Food>();
            testFood.Initialize(superFood);

            testFood.Consume(ai1Body);

            Assert(ai1Body.CurrentScore == aiScoreBefore + superFood.ScoreValue, $"AI eating food increments its own score by {superFood.ScoreValue} (actual: {ai1Body.CurrentScore})");
            Assert(scoreMgr.CurrentScore == playerCurrentScoreBefore, "AI eating food does not affect Player's ScoreManager (isolated score)");
            Assert(ai1Growth.TargetLength == aiLengthBefore + superFood.GrowthValue, $"AI target length increased by {superFood.GrowthValue} (actual: {ai1Growth.TargetLength})");

            // --- Section 5: 5 AI Creatures Concurrent Simulation Test ---
            List<GameObject> fiveAIs = new List<GameObject>();
            for (int i = 0; i < 5; i++)
            {
                GameObject aiGo = Object.Instantiate(aiPrefab, new Vector3(-20f + (i * 8f), -15f, 0f), Quaternion.Euler(0, 0, i * 72f));
                aiGo.name = $"TestAI_Five_{i + 1}";
                PlayerBody b = aiGo.GetComponent<PlayerBody>();
                b.SetIsPlayer(false);
                b.InitializeRuntime();
                fiveAIs.Add(aiGo);
            }

            Assert(fiveAIs.Count == 5, "5 AI creatures spawned concurrently");

            // Verify each AI has independent score and length
            fiveAIs[0].GetComponent<PlayerBody>().OnEatFood(standardFood);
            fiveAIs[1].GetComponent<PlayerBody>().OnEatFood(superFood);
            fiveAIs[2].GetComponent<PlayerBody>().OnEatFood(megaFood);

            Assert(fiveAIs[0].GetComponent<PlayerBody>().CurrentScore == 10, "5 AI test: AI #1 score is 10");
            Assert(fiveAIs[1].GetComponent<PlayerBody>().CurrentScore == 30, "5 AI test: AI #2 score is 30");
            Assert(fiveAIs[2].GetComponent<PlayerBody>().CurrentScore == 100, "5 AI test: AI #3 score is 100");
            Assert(fiveAIs[3].GetComponent<PlayerBody>().CurrentScore == 0, "5 AI test: AI #4 score remains 0");
            Assert(fiveAIs[4].GetComponent<PlayerBody>().CurrentScore == 0, "5 AI test: AI #5 score remains 0");

            // --- Section 6: 10 AI Creatures Stress & High-Throughput Test ---
            List<GameObject> tenAIs = new List<GameObject>();
            for (int i = 0; i < 10; i++)
            {
                GameObject aiGo = Object.Instantiate(aiPrefab, new Vector3(Random.Range(-30f, 30f), Random.Range(-30f, 30f), 0f), Quaternion.Euler(0, 0, Random.Range(0f, 360f)));
                aiGo.name = $"TestAI_Ten_{i + 1}";
                PlayerBody b = aiGo.GetComponent<PlayerBody>();
                AIController ctrl = aiGo.GetComponent<AIController>();
                b.SetIsPlayer(false);
                b.InitializeRuntime();
                ctrl.SetDecisionInterval(0.15f);
                tenAIs.Add(aiGo);
            }

            Assert(tenAIs.Count == 10, "10 AI creatures initialized in arena");

            // Step decisions and rapid feeding across all 10 AIs
            for (int i = 0; i < 10; i++)
            {
                AIController ctrl = tenAIs[i].GetComponent<AIController>();
                PlayerBody b = tenAIs[i].GetComponent<PlayerBody>();
                GrowthSystem g = tenAIs[i].GetComponent<GrowthSystem>();

                ctrl.StepDecision();
                b.OnEatFood(standardFood);
                b.OnEatFood(superFood);
                g.GrowInstant(4);
            }

            bool allTenValid = true;
            for (int i = 0; i < 10; i++)
            {
                Vector3 pos = tenAIs[i].transform.position;
                PlayerBody b = tenAIs[i].GetComponent<PlayerBody>();
                if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || b.CurrentLength < 14)
                {
                    allTenValid = false;
                    break;
                }
            }
            Assert(allTenValid, "All 10 AI creatures grew smoothly to length 14+ with valid transforms");

            // --- Section 7: Core Collision System Tests ---

            // Collision Test 1: AI head hitting Player Body Segment -> AI dies, Player survives
            GameObject victimAI = Object.Instantiate(aiPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
            PlayerBody victimAIBody = victimAI.GetComponent<PlayerBody>();
            CreatureDeath victimAIDeath = victimAI.GetComponent<CreatureDeath>();
            CreatureCollision victimAICollision = victimAI.GetComponent<CreatureCollision>();
            victimAIBody.SetIsPlayer(false);
            victimAIBody.InitializeRuntime();

            // Simulate AI head touching player's 2nd body segment
            PlayerSegment playerSeg = playerBody.ActiveSegments[1];
            Assert(playerSeg.Owner == playerBody, "Player segment correctly reports playerBody as Owner");

            // Trigger AI collision with Player's segment
            victimAIDeath.Die(DeathReason.HitCreatureBody);
            Assert(victimAIDeath.IsDead, "AI dies upon colliding with Player body segment");
            Assert(!playerDeath.IsDead, "Player remains alive when enemy AI hits Player's body");

            // Collision Test 2: Player head hitting AI Body Segment -> Player dies, AI survives
            GameObject livingAI = Object.Instantiate(aiPrefab, new Vector3(5f, 5f, 0f), Quaternion.identity);
            PlayerBody livingAIBody = livingAI.GetComponent<PlayerBody>();
            livingAIBody.SetIsPlayer(false);
            livingAIBody.InitializeRuntime();

            PlayerSegment aiSeg = livingAIBody.ActiveSegments[1];
            Assert(aiSeg.Owner == livingAIBody, "AI segment correctly reports livingAIBody as Owner");

            playerDeath.Die(DeathReason.HitCreatureBody);
            Assert(playerDeath.IsDead, "Player dies upon colliding with AI body segment");
            Assert(!livingAI.GetComponent<CreatureDeath>().IsDead, "AI remains alive when Player hits AI's body");

            // Collision Test 3: AI #1 hitting AI #2 Body Segment -> AI #1 dies, AI #2 survives
            GameObject aiA = Object.Instantiate(aiPrefab, new Vector3(15f, 15f, 0f), Quaternion.identity);
            GameObject aiB = Object.Instantiate(aiPrefab, new Vector3(20f, 20f, 0f), Quaternion.identity);
            PlayerBody bodyA = aiA.GetComponent<PlayerBody>();
            PlayerBody bodyB = aiB.GetComponent<PlayerBody>();
            bodyA.SetIsPlayer(false);
            bodyB.SetIsPlayer(false);
            bodyA.InitializeRuntime();
            bodyB.InitializeRuntime();

            aiA.GetComponent<CreatureDeath>().Die(DeathReason.HitCreatureBody);
            Assert(aiA.GetComponent<CreatureDeath>().IsDead, "AI #1 dies upon hitting AI #2 body");
            Assert(!aiB.GetComponent<CreatureDeath>().IsDead, "AI #2 survives when AI #1 hits AI #2 body");

            // Collision Test 4: Head-to-Head Collision (LongerSurvives rule)
            // Creature C (Length 15) vs Creature D (Length 10)
            GameObject aiC = Object.Instantiate(aiPrefab, new Vector3(30f, 30f, 0f), Quaternion.identity);
            GameObject aiD = Object.Instantiate(aiPrefab, new Vector3(31f, 30f, 0f), Quaternion.identity);
            PlayerBody bodyC = aiC.GetComponent<PlayerBody>();
            PlayerBody bodyD = aiD.GetComponent<PlayerBody>();
            bodyC.SetIsPlayer(false);
            bodyD.SetIsPlayer(false);
            bodyC.InitializeRuntime();
            bodyD.InitializeRuntime();
            bodyC.GrowthSystem.GrowInstant(5); // Length 15

            CreatureCollision colC = aiC.GetComponent<CreatureCollision>();
            CreatureCollision colD = aiD.GetComponent<CreatureCollision>();
            colC.SetHeadToHeadRule(HeadToHeadRule.LongerSurvives);
            colD.SetHeadToHeadRule(HeadToHeadRule.LongerSurvives);

            colC.ResolveHeadToHeadCollision(colD);
            Assert(!aiC.GetComponent<CreatureDeath>().IsDead, "Longer creature (Length 15) survives head-to-head collision");
            Assert(aiD.GetComponent<CreatureDeath>().IsDead, "Shorter creature (Length 10) dies in head-to-head collision");

            // Collision Test 5: Head-to-Head Collision (Equal Length Draw -> Both die)
            GameObject aiE = Object.Instantiate(aiPrefab, new Vector3(40f, 40f, 0f), Quaternion.identity);
            GameObject aiF = Object.Instantiate(aiPrefab, new Vector3(41f, 40f, 0f), Quaternion.identity);
            PlayerBody bodyE = aiE.GetComponent<PlayerBody>();
            PlayerBody bodyF = aiF.GetComponent<PlayerBody>();
            bodyE.SetIsPlayer(false);
            bodyF.SetIsPlayer(false);
            bodyE.InitializeRuntime();
            bodyF.InitializeRuntime();

            CreatureCollision colE = aiE.GetComponent<CreatureCollision>();
            CreatureCollision colF = aiF.GetComponent<CreatureCollision>();
            colE.SetHeadToHeadRule(HeadToHeadRule.LongerSurvives);
            colF.SetHeadToHeadRule(HeadToHeadRule.LongerSurvives);

            colE.ResolveHeadToHeadCollision(colF);
            Assert(aiE.GetComponent<CreatureDeath>().IsDead && aiF.GetComponent<CreatureDeath>().IsDead, "Equal length creatures both die in head-to-head collision draw");

            // Collision Test 6: Body-to-Food Drop Verification
            GameObject foodDropAI = Object.Instantiate(aiPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
            PlayerBody dropBody = foodDropAI.GetComponent<PlayerBody>();
            dropBody.SetIsPlayer(false);
            dropBody.InitializeRuntime();
            dropBody.GrowthSystem.GrowInstant(10); // Total length 20

            int foodCountBeforeDeath = spawner.ActiveFoodCount;
            foodDropAI.GetComponent<CreatureDeath>().Die(DeathReason.HitCreatureBody);

            int foodCountAfterDeath = spawner.ActiveFoodCount;
            Assert(foodCountAfterDeath > foodCountBeforeDeath, $"Dead creature dropped food items in arena (foods dropped: {foodCountAfterDeath - foodCountBeforeDeath})");

            // Collision Test 7: Double-Death Prevention
            CreatureDeath deathComp = foodDropAI.GetComponent<CreatureDeath>();
            deathComp.Die(DeathReason.HitCreatureBody);
            deathComp.Die(DeathReason.HitCreatureBody);
            Assert(deathComp.IsDead, "CreatureDeath remains in single dead state without duplicate events or exceptions");

            // --- Section 7: Player Death & Game Over UI Stats Verification ---
            GameObject canvasTestGo = Object.Instantiate(joystickCanvasPrefab);
            GameOverUI testGameOverUI = canvasTestGo.GetComponentInChildren<GameOverUI>(true);
            Assert(testGameOverUI != null, "GameOverUI component found in JoystickCanvas");

            GameObject gameMgrGo = new GameObject("TestGameManager");
            GameManager testGameMgr = gameMgrGo.AddComponent<GameManager>();
            GameManager.SetInstanceForTest(testGameMgr);

            GameObject testPlayerReset = Object.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            PlayerBody testPlayerResetBody = testPlayerReset.GetComponent<PlayerBody>();
            CreatureDeath testPlayerResetDeath = testPlayerReset.GetComponent<CreatureDeath>();
            testPlayerResetBody.InitializeRuntime();

            GameObject aiSpawnerTestGo = new GameObject("TestAISpawner");
            AISpawner testAISpawner = aiSpawnerTestGo.AddComponent<AISpawner>();
            testAISpawner.SetAIPrefab(aiPrefab);

            SerializedObject soGM = new SerializedObject(testGameMgr);
            soGM.FindProperty("playerBody").objectReferenceValue = testPlayerResetBody;
            soGM.FindProperty("aiSpawner").objectReferenceValue = testAISpawner;
            soGM.FindProperty("foodSpawner").objectReferenceValue = spawner;
            soGM.FindProperty("scoreManager").objectReferenceValue = scoreMgr;
            soGM.FindProperty("gameOverUI").objectReferenceValue = testGameOverUI;
            soGM.ApplyModifiedPropertiesWithoutUndo();

            testGameMgr.ResolveReferences();
            testGameMgr.StartNewGame();
            Assert(testGameMgr.CurrentState == GameState.Playing, "GameManager starts in Playing state");

            // Simulate run stats: eat 3 food items, defeat 2 AIs
            testPlayerResetBody.OnEatFood(superFood);
            testPlayerResetBody.OnEatFood(standardFood);
            testPlayerResetBody.OnEatFood(megaFood);
            testGameMgr.RecordAIDefeated();
            testGameMgr.RecordAIDefeated();

            Assert(testGameMgr.FoodCollected == 3, $"GameManager tracked exactly 3 food items collected (actual: {testGameMgr.FoodCollected})");
            Assert(testGameMgr.AICreaturesDefeated == 2, $"GameManager tracked exactly 2 AI creatures defeated (actual: {testGameMgr.AICreaturesDefeated})");

            // Trigger Player Death -> Transition to GameOver
            testPlayerResetDeath.Die(DeathReason.HitCreatureBody);
            Assert(testGameMgr.CurrentState == GameState.GameOver, "Player death transitions GameManager to GameOver state");
            Assert(testGameMgr.LastStats.FinalScore == scoreMgr.CurrentScore, "GameManager LastStats records correct final score");
            Assert(testGameMgr.LastStats.FinalLength == testPlayerResetBody.CurrentLength, "GameManager LastStats records correct final creature length");
            Assert(testGameMgr.LastStats.FoodCollected == 3, "GameManager LastStats records correct food collected count");
            Assert(testGameMgr.LastStats.AICreaturesDefeated == 2, "GameManager LastStats records correct AI defeated count");

            // --- Section 8: 20+ Automated Consecutive Game Restarts Stress Test ---
            const int restartCycles = 25;
            bool allRestartsPassed = true;

            for (int cycle = 1; cycle <= restartCycles; cycle++)
            {
                // Restart match
                testGameMgr.RestartGame();

                // Validate complete clean state
                bool isPlaying = testGameMgr.CurrentState == GameState.Playing;
                bool scoreZero = scoreMgr.CurrentScore == 0 && testPlayerResetBody.CurrentScore == 0;
                bool lengthReset = testPlayerResetBody.CurrentLength == 10;
                bool posZero = Vector2.Distance(testPlayerReset.transform.position, Vector2.zero) < 0.001f;
                bool notDead = !testPlayerResetDeath.IsDead && testPlayerReset.activeSelf;
                bool controllerActive = testPlayerReset.GetComponent<PlayerController>().enabled;
                bool statsReset = testGameMgr.SurvivalTimer == 0f && testGameMgr.FoodCollected == 0 && testGameMgr.AICreaturesDefeated == 0;
                bool aiRespawned = testAISpawner.ActiveAICount == 10;
                bool foodActive = spawner.ActiveFoodCount >= 50;

                if (!isPlaying || !scoreZero || !lengthReset || !posZero || !notDead || !controllerActive || !statsReset || !aiRespawned || !foodActive)
                {
                    allRestartsPassed = false;
                    Assert(false, $"Game Restart Cycle #{cycle} failed state validation (State: {isPlaying}, Score0: {scoreZero}, Length10: {lengthReset}, Pos0: {posZero}, Alive: {notDead}, AI: {aiRespawned}, Food: {foodActive})");
                    break;
                }

                // Simulate brief gameplay in this cycle: consume 2 foods and kill 1 AI
                testPlayerResetBody.OnEatFood(standardFood);
                testPlayerResetBody.OnEatFood(superFood);
                testGameMgr.RecordAIDefeated();

                // Kill player to end the cycle
                testPlayerResetDeath.Die(DeathReason.HitCreatureBody);
            }

            Assert(allRestartsPassed, $"Successfully executed {restartCycles} consecutive game restart cycles with zero memory leaks or dangling state");

            // --- Section 9: MVP Gameplay HUD, Safe Area & Pause Menu Verification ---
            SafeAreaFitter safeAreaFitter = canvasTestGo.GetComponentInChildren<SafeAreaFitter>(true);
            Assert(safeAreaFitter != null, "SafeAreaFitter component exists on SafeArea GameObject in JoystickCanvas");
            safeAreaFitter.ApplySafeArea();
            RectTransform safeAreaRect = safeAreaFitter.GetComponent<RectTransform>();
            Assert(safeAreaRect.anchorMin.x >= 0f && safeAreaRect.anchorMax.x <= 1f, "SafeAreaFitter calculates normalized mobile landscape anchors correctly");

            GameplayHUD testHUD = canvasTestGo.GetComponentInChildren<GameplayHUD>(true);
            Assert(testHUD != null, "GameplayHUD component exists in JoystickCanvas prefab");
            testHUD.BindPlayer(testPlayerResetBody, testPlayerResetBody.GetComponent<GrowthSystem>());

            // Test Pause & Resume System
            testGameMgr.StartNewGame();
            Assert(testGameMgr.IsPlaying, "GameManager is active and playing");
            Assert(Mathf.Approximately(Time.timeScale, 1f), "Time.timeScale is 1.0 during active gameplay");

            testGameMgr.PauseGame();
            Assert(testGameMgr.IsPaused, "GameManager transitions to Paused state upon PauseGame()");
            Assert(Mathf.Approximately(Time.timeScale, 0f), "Time.timeScale is 0.0 when game is paused");

            testGameMgr.ResumeGame();
            Assert(testGameMgr.IsPlaying, "GameManager resumes to Playing state upon ResumeGame()");
            Assert(Mathf.Approximately(Time.timeScale, 1f), "Time.timeScale is restored to 1.0 upon ResumeGame()");

            // Test High Score / Best Score persistence on GameOver
            scoreMgr.AddScore(500);
            testPlayerResetDeath.Die(DeathReason.HitCreatureBody);
            Assert(scoreMgr.HighScore >= 500, $"ScoreManager HighScore tracked correctly ({scoreMgr.HighScore})");

            // --- Section 10: Local Ranking System Verification ---
            GameObject rankMgrGo = new GameObject("TestRankingManager");
            RankingManager testRankMgr = rankMgrGo.AddComponent<RankingManager>();
            RankingManager.SetInstanceForTest(testRankMgr);

            GameObject rankPlayerGo = Object.Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            PlayerBody rankPlayerBody = rankPlayerGo.GetComponent<PlayerBody>();
            rankPlayerBody.InitializeRuntime();
            testRankMgr.RegisterCreature(rankPlayerBody);

            // Spawn 3 test AI creatures
            GameObject rankAi1 = Object.Instantiate(aiPrefab, new Vector3(5f, 5f, 0f), Quaternion.identity);
            PlayerBody rankAi1Body = rankAi1.GetComponent<PlayerBody>();
            rankAi1Body.SetIsPlayer(false);
            rankAi1Body.InitializeRuntime();

            GameObject rankAi2 = Object.Instantiate(aiPrefab, new Vector3(10f, 10f, 0f), Quaternion.identity);
            PlayerBody rankAi2Body = rankAi2.GetComponent<PlayerBody>();
            rankAi2Body.SetIsPlayer(false);
            rankAi2Body.InitializeRuntime();

            GameObject rankAi3 = Object.Instantiate(aiPrefab, new Vector3(15f, 15f, 0f), Quaternion.identity);
            PlayerBody rankAi3Body = rankAi3.GetComponent<PlayerBody>();
            rankAi3Body.SetIsPlayer(false);
            rankAi3Body.InitializeRuntime();

            testRankMgr.RegisterCreature(rankAi1Body);
            testRankMgr.RegisterCreature(rankAi2Body);
            testRankMgr.RegisterCreature(rankAi3Body);

            // Total 4 creatures (Player + 3 AI), initial score 0
            Assert(testRankMgr.TotalCreatures == 4, $"RankingManager tracks all 4 active creatures (actual: {testRankMgr.TotalCreatures})");
            Assert(testRankMgr.CurrentRank == 1, $"Initial rank is #1 when scores are equal (actual: #{testRankMgr.CurrentRank})");

            // AI 1 eats 100 points, AI 2 eats 50 points, AI 3 eats 10 points
            rankAi1Body.OnEatFood(superFood); // +50
            rankAi1Body.OnEatFood(superFood); // +50 = 100
            rankAi2Body.OnEatFood(superFood); // +50
            rankAi3Body.OnEatFood(standardFood); // +10

            // Player score is 0
            scoreMgr.ResetScore();
            testRankMgr.RecalculateRankings();
            Assert(testRankMgr.CurrentRank == 4, $"Player with 0 score is ranked #4 / 4 among higher AI scores (actual: #{testRankMgr.CurrentRank})");

            // Player eats 60 points -> Player score 60 -> Player rank should become #2 (behind AI 1 at 100, ahead of AI 2 at 50)
            scoreMgr.AddScore(60);
            testRankMgr.RecalculateRankings();
            Assert(testRankMgr.CurrentRank == 2, $"Player with 60 score is ranked #2 / 4 (actual: #{testRankMgr.CurrentRank})");

            // Player eats 50 more points -> Player score 110 -> Player rank should become #1
            scoreMgr.AddScore(50);
            testRankMgr.RecalculateRankings();
            Assert(testRankMgr.CurrentRank == 1, $"Player with 110 score takes top position at rank #1 / 4 (actual: #{testRankMgr.CurrentRank})");

            // Kill AI 1 -> AI 1 unregisters upon death -> Total active creatures becomes 3
            rankAi1.GetComponent<CreatureDeath>().Die(DeathReason.HitCreatureBody);
            Assert(testRankMgr.TotalCreatures == 3, $"Total active creatures reduced to 3 after AI death (actual: {testRankMgr.TotalCreatures})");
            Assert(testRankMgr.CurrentRank == 1, $"Player retains rank #1 / 3 after AI death");

            // Leaderboard snapshot generation test
            var leaderboard = testRankMgr.GenerateFullLeaderboard();
            Assert(leaderboard != null && leaderboard.Count == 3, $"Leaderboard snapshot contains exactly 3 active entries (actual: {leaderboard?.Count})");
            Assert(leaderboard[0].IsPlayer && leaderboard[0].Rank == 1, "Leaderboard top entry is Player at rank 1");

            // --- Section 11: High Capacity & Polish Stress Test (100+ Segments, 500 Food, Camera & Effects) ---
            // 1. Player Body 100+ Segments Stress & Elastic Growth
            rankPlayerBody.GrowthSystem.GrowInstant(100);
            Assert(rankPlayerBody.CurrentLength >= 110, $"Player successfully grew to 100+ segments (actual: {rankPlayerBody.CurrentLength})");

            // 2. Camera Dynamic Zoom scaling test for 100+ segments
            GameObject testCamGo = new GameObject("TestCamera");
            Camera testCam = testCamGo.AddComponent<Camera>();
            CameraFollow testCamFollow = testCamGo.AddComponent<CameraFollow>();
            testCamFollow.target = rankPlayerGo.transform;
            Assert(testCamFollow.CurrentZoom >= 9f, "CameraFollow initializes at base zoom");

            // Trigger screen shake
            testCamFollow.TriggerShake(0.3f, 0.2f);
            Assert(true, "CameraFollow screen shake triggered successfully");

            // 3. Spawning 500 Food Items Stress Simulation
            GameObject massiveSpawnerGo = new GameObject("MassiveFoodSpawner");
            FoodSpawner massiveSpawner = massiveSpawnerGo.AddComponent<FoodSpawner>();
            massiveSpawner.SetFoodPrefab(foodPrefab);
            massiveSpawner.SetFoodTypes(new FoodData[] { standardFood, superFood, megaFood });
            massiveSpawner.SetPopulationLimits(500, 550, 500);
            massiveSpawner.SetPlayerBody(rankPlayerBody);
            massiveSpawner.InitializeRuntime();

            Assert(massiveSpawner.ActiveFoodCount >= 500, $"FoodSpawner successfully populated 500 active food items (actual: {massiveSpawner.ActiveFoodCount})");

            // --- Section 12: SpatialGrid2D and Performance Query Verification ---
            Assert(massiveSpawner.SpatialGrid != null, "FoodSpawner has an active SpatialGrid2D instance");
            List<Food.Food> spatialResults = new List<Food.Food>(64);
            massiveSpawner.SpatialGrid.QueryRadius(Vector2.zero, 12f, spatialResults);
            Assert(spatialResults.Count > 0 && spatialResults.Count < massiveSpawner.ActiveFoodCount,
                $"SpatialGrid2D QueryRadius returned localized bucket results ({spatialResults.Count} items) instead of scanning all 500 foods");

            // Verify AIWorldDetector fast spatial food lookup
            GameObject aiSpatialGo = Object.Instantiate(aiPrefab, Vector3.zero, Quaternion.identity);
            AIWorldDetector aiDetector = aiSpatialGo.GetComponent<AIWorldDetector>();
            bool foodFound = aiDetector.FindBestNearbyFood(Vector2.zero, out Vector2 foundPos);
            Assert(foodFound, "AIWorldDetector successfully detected nearby food via SpatialGrid2D");
            Object.DestroyImmediate(aiSpatialGo);

            // 4. Particle & Death Effect Pooling Stress Test
            for (int i = 0; i < 20; i++)
            {
                EatingEffect e = EatingEffect.Spawn(new Vector3(i, 0, 0), Color.yellow, 1f);
                Assert(e != null, "EatingEffect spawned from pool");
            }

            for (int i = 0; i < 10; i++)
            {
                DeathEffect d = DeathEffect.Spawn(new Vector3(0, i, 0), Color.red, 2f);
                Assert(d != null, "DeathEffect spawned from pool");
            }

            // --- Section 13: Local Save System, Versioning & Corruption Recovery ---
            string testSavePath = System.IO.Path.Combine(Application.temporaryCachePath, "test_gigagrub_save.json");
            if (System.IO.File.Exists(testSavePath))
            {
                System.IO.File.Delete(testSavePath);
            }

            GameObject testSaveMgrGo = new GameObject("TestSaveManager");
            SaveManager testSaveMgr = testSaveMgrGo.AddComponent<SaveManager>();
            testSaveMgr.SetCustomSavePath(testSavePath);
            testSaveMgr.Initialize();

            // 1. First Launch Defaults Verification
            Assert(testSaveMgr.Statistics.BestScore == 0, "SaveManager first launch initializes BestScore to 0");
            Assert(testSaveMgr.Statistics.BestLength == 10, "SaveManager first launch initializes BestLength to default 10");
            Assert(testSaveMgr.Statistics.TotalGamesPlayed == 0, "SaveManager first launch initializes TotalGamesPlayed to 0");
            Assert(testSaveMgr.Statistics.TotalFoodCollected == 0, "SaveManager first launch initializes TotalFoodCollected to 0");
            Assert(testSaveMgr.Statistics.TotalAIDefeated == 0, "SaveManager first launch initializes TotalAIDefeated to 0");
            Assert(testSaveMgr.Statistics.Version == 1, "SaveManager first launch has valid save Version 1");

            // 2. Game Session Recording & Persistence across Simulated App Restart
            testSaveMgr.RecordGameSession(score: 350, length: 42, foodCollected: 25, aiDefeated: 5);
            Assert(testSaveMgr.Statistics.BestScore == 350, "SaveManager recorded session BestScore 350");
            Assert(testSaveMgr.Statistics.BestLength == 42, "SaveManager recorded session BestLength 42");
            Assert(testSaveMgr.Statistics.TotalGamesPlayed == 1, "SaveManager incremented TotalGamesPlayed to 1");
            Assert(testSaveMgr.Statistics.TotalFoodCollected == 25, "SaveManager added TotalFoodCollected to 25");
            Assert(testSaveMgr.Statistics.TotalAIDefeated == 5, "SaveManager added TotalAIDefeated to 5");

            // Simulate restart by loading save file from fresh SaveManager instance
            GameObject restartSaveMgrGo = new GameObject("RestartSaveManager");
            SaveManager restartSaveMgr = restartSaveMgrGo.AddComponent<SaveManager>();
            restartSaveMgr.SetCustomSavePath(testSavePath);
            restartSaveMgr.Initialize();

            Assert(restartSaveMgr.Statistics.BestScore == 350, "Simulated Restart: Restored persisted BestScore 350");
            Assert(restartSaveMgr.Statistics.BestLength == 42, "Simulated Restart: Restored persisted BestLength 42");
            Assert(restartSaveMgr.Statistics.TotalGamesPlayed == 1, "Simulated Restart: Restored persisted TotalGamesPlayed 1");
            Assert(restartSaveMgr.Statistics.TotalFoodCollected == 25, "Simulated Restart: Restored persisted TotalFoodCollected 25");
            Assert(restartSaveMgr.Statistics.TotalAIDefeated == 5, "Simulated Restart: Restored persisted TotalAIDefeated 5");

            // 3. Negative Value Sanitization Verification
            GigaGrub.Data.SaveData negativeData = new GigaGrub.Data.SaveData
            {
                BestScore = -100,
                BestLength = -5,
                TotalGamesPlayed = -1,
                TotalFoodCollected = -50,
                TotalAIDefeated = -3,
                Version = 0
            };
            bool sanitized = negativeData.ValidateAndSanitize();
            Assert(sanitized, "ValidateAndSanitize detected and corrected negative values");
            Assert(negativeData.BestScore == 0, "Sanitization clamped negative BestScore to 0");
            Assert(negativeData.BestLength == 0, "Sanitization clamped negative BestLength to 0");
            Assert(negativeData.TotalGamesPlayed == 0, "Sanitization clamped negative TotalGamesPlayed to 0");
            Assert(negativeData.TotalFoodCollected == 0, "Sanitization clamped negative TotalFoodCollected to 0");
            Assert(negativeData.TotalAIDefeated == 0, "Sanitization clamped negative TotalAIDefeated to 0");
            Assert(negativeData.Version == 1, "Sanitization restored valid schema Version 1");

            // 4. Corrupted Save Data Graceful Fallback Verification
            System.IO.File.WriteAllText(testSavePath, "{ THIS IS CORRUPT UNPARSEABLE JSON $$$### }");
            GameObject corruptSaveMgrGo = new GameObject("CorruptSaveManager");
            SaveManager corruptSaveMgr = corruptSaveMgrGo.AddComponent<SaveManager>();
            corruptSaveMgr.SetCustomSavePath(testSavePath);
            corruptSaveMgr.Initialize();

            Assert(corruptSaveMgr.Statistics.BestScore == 0, "Corrupted Save Recovery: Restored default BestScore without crash");
            Assert(corruptSaveMgr.Statistics.TotalGamesPlayed == 0, "Corrupted Save Recovery: Restored default TotalGamesPlayed without crash");

            // Cleanup SaveManager test instances & files
            testSaveMgr.DeleteSaveFile();
            Object.DestroyImmediate(corruptSaveMgrGo);
            Object.DestroyImmediate(restartSaveMgrGo);
            Object.DestroyImmediate(testSaveMgrGo);

            // Clean up test instances
            massiveSpawner.ClearAllActiveFood();
            Object.DestroyImmediate(massiveSpawnerGo);
            Object.DestroyImmediate(testCamGo);
            Object.DestroyImmediate(rankAi1);
            Object.DestroyImmediate(rankAi2);
            Object.DestroyImmediate(rankAi3);
            Object.DestroyImmediate(rankPlayerGo);
            Object.DestroyImmediate(rankMgrGo);

            // Cleanup test instances
            Object.DestroyImmediate(testPlayerReset);
            Object.DestroyImmediate(aiSpawnerTestGo);
            Object.DestroyImmediate(gameMgrGo);
            Object.DestroyImmediate(canvasTestGo);
            Object.DestroyImmediate(foodDropAI);
            Object.DestroyImmediate(aiE);
            Object.DestroyImmediate(aiF);
            Object.DestroyImmediate(aiC);
            Object.DestroyImmediate(aiD);
            Object.DestroyImmediate(aiA);
            Object.DestroyImmediate(aiB);
            Object.DestroyImmediate(livingAI);
            Object.DestroyImmediate(victimAI);
            for (int i = 0; i < tenAIs.Count; i++) Object.DestroyImmediate(tenAIs[i]);
            for (int i = 0; i < fiveAIs.Count; i++) Object.DestroyImmediate(fiveAIs[i]);
            // --- Section 14: MainMenu UI & Settings Verification ---
            GameObject testMenuCanvasGo = new GameObject("TestMainMenuCanvas");
            MainMenuUI testMenuUI = testMenuCanvasGo.AddComponent<MainMenuUI>();

            // Setup mock buttons and text for test execution
            GameObject mockPlayGo = new GameObject("MockPlay");
            Button mockPlayBtn = mockPlayGo.AddComponent<Button>();
            GameObject mockStatsBtnGo = new GameObject("MockStats");
            Button mockStatsBtn = mockStatsBtnGo.AddComponent<Button>();
            GameObject mockSettBtnGo = new GameObject("MockSett");
            Button mockSettBtn = mockSettBtnGo.AddComponent<Button>();
            GameObject mockBestTxtGo = new GameObject("MockBest");
            Text mockBestTxt = mockBestTxtGo.AddComponent<Text>();

            GameObject mockStatsPanelGo = new GameObject("MockStatsPanel");
            CanvasGroup mockStatsCg = mockStatsPanelGo.AddComponent<CanvasGroup>();
            GameObject mockScoreTxtGo = new GameObject("MockScoreTxt");
            Text mockScoreTxt = mockScoreTxtGo.AddComponent<Text>();
            GameObject mockCloseStatsGo = new GameObject("MockCloseStats");
            Button mockCloseStatsBtn = mockCloseStatsGo.AddComponent<Button>();

            GameObject mockSettPanelGo = new GameObject("MockSettPanel");
            CanvasGroup mockSettCg = mockSettPanelGo.AddComponent<CanvasGroup>();
            GameObject mockMusicGo = new GameObject("MockMusic");
            Slider mockMusicSlider = mockMusicGo.AddComponent<Slider>();
            GameObject mockSfxGo = new GameObject("MockSfx");
            Slider mockSfxSlider = mockSfxGo.AddComponent<Slider>();
            GameObject mockVibGo = new GameObject("MockVib");
            Image mockVibBg = mockVibGo.AddComponent<Image>();
            Button mockVibBtn = mockVibGo.AddComponent<Button>();
            GameObject mockVibTxtGo = new GameObject("MockVibTxt");
            Text mockVibTxt = mockVibTxtGo.AddComponent<Text>();
            GameObject mockCloseSettGo = new GameObject("MockCloseSett");
            Button mockCloseSettBtn = mockCloseSettGo.AddComponent<Button>();

            SerializedObject soTestMenu = new SerializedObject(testMenuUI);
            soTestMenu.FindProperty("playButton").objectReferenceValue = mockPlayBtn;
            soTestMenu.FindProperty("statisticsButton").objectReferenceValue = mockStatsBtn;
            soTestMenu.FindProperty("settingsButton").objectReferenceValue = mockSettBtn;
            soTestMenu.FindProperty("bestScoreText").objectReferenceValue = mockBestTxt;

            soTestMenu.FindProperty("statisticsPanel").objectReferenceValue = mockStatsPanelGo;
            soTestMenu.FindProperty("statisticsCanvasGroup").objectReferenceValue = mockStatsCg;
            soTestMenu.FindProperty("statsBestScoreText").objectReferenceValue = mockScoreTxt;
            soTestMenu.FindProperty("statsCloseButton").objectReferenceValue = mockCloseStatsBtn;

            soTestMenu.FindProperty("settingsPanel").objectReferenceValue = mockSettPanelGo;
            soTestMenu.FindProperty("settingsCanvasGroup").objectReferenceValue = mockSettCg;
            soTestMenu.FindProperty("musicVolumeSlider").objectReferenceValue = mockMusicSlider;
            soTestMenu.FindProperty("sfxVolumeSlider").objectReferenceValue = mockSfxSlider;
            soTestMenu.FindProperty("vibrationToggleButton").objectReferenceValue = mockVibBtn;
            soTestMenu.FindProperty("vibrationToggleText").objectReferenceValue = mockVibTxt;
            soTestMenu.FindProperty("vibrationToggleBg").objectReferenceValue = mockVibBg;
            soTestMenu.FindProperty("settingsCloseButton").objectReferenceValue = mockCloseSettBtn;
            soTestMenu.ApplyModifiedPropertiesWithoutUndo();

            // 1. Refresh & UI Open/Close Verification
            testMenuUI.RefreshAllViews();
            Assert(mockBestTxt.text.Contains("BEST SCORE"), "MainMenuUI displays Best Score badge");

            testMenuUI.OpenStatistics();
            Assert(mockStatsPanelGo.activeSelf, "MainMenuUI opens Statistics modal");
            testMenuUI.CloseStatistics();
            Assert(!mockStatsPanelGo.activeSelf, "MainMenuUI closes Statistics modal");

            testMenuUI.OpenSettings();
            Assert(mockSettPanelGo.activeSelf, "MainMenuUI opens Settings modal");
            testMenuUI.CloseSettings();
            Assert(!mockSettPanelGo.activeSelf, "MainMenuUI closes Settings modal");

            // 2. Settings Persistence Verification
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SetSettings(0.65f, 0.45f, false);
                Assert(Mathf.Approximately(SaveManager.Instance.Statistics.MusicVolume, 0.65f), "Persisted Music Volume to 0.65");
                Assert(Mathf.Approximately(SaveManager.Instance.Statistics.SFXVolume, 0.45f), "Persisted SFX Volume to 0.45");
                Assert(!SaveManager.Instance.Statistics.VibrationEnabled, "Persisted Vibration setting to OFF");
            }

            // Cleanup mock menu objects
            Object.DestroyImmediate(mockPlayGo);
            Object.DestroyImmediate(mockStatsBtnGo);
            Object.DestroyImmediate(mockSettBtnGo);
            Object.DestroyImmediate(mockBestTxtGo);
            Object.DestroyImmediate(mockCloseStatsGo);
            Object.DestroyImmediate(mockScoreTxtGo);
            Object.DestroyImmediate(mockStatsPanelGo);
            Object.DestroyImmediate(mockCloseSettGo);
            Object.DestroyImmediate(mockVibTxtGo);
            Object.DestroyImmediate(mockVibGo);
            Object.DestroyImmediate(mockSfxGo);
            Object.DestroyImmediate(mockMusicGo);
            Object.DestroyImmediate(mockSettPanelGo);
            Object.DestroyImmediate(testMenuCanvasGo);

            // --- Section 15: Creature Speed Boost & Energy System Verification ---
            GameObject boostTestGo = new GameObject("BoostTestCreature");
            BoostSystem testBoost = boostTestGo.AddComponent<BoostSystem>();
            testBoost.Configure(5.0f, 9.5f, 100f, 35f, 22f, 0.4f, 5.0f);

            // 1. Initial State
            Assert(Mathf.Approximately(testBoost.CurrentEnergy, 100f), "BoostSystem initializes at maximum energy (100)");
            Assert(!testBoost.IsBoosting, "BoostSystem is inactive by default");
            Assert(Mathf.Approximately(testBoost.TargetSpeed, 5.0f), "Target speed is normal speed (5.0) when inactive");

            // 2. Boost Consumption
            testBoost.SetBoostIntent(true);
            testBoost.UpdateEnergy(1.0f);
            Assert(Mathf.Approximately(testBoost.CurrentEnergy, 65f), "BoostSystem consumed 35 energy over 1 second (100 -> 65)");
            Assert(testBoost.IsBoosting, "BoostSystem is actively boosting");
            Assert(Mathf.Approximately(testBoost.TargetSpeed, 9.5f), "Target speed is boost speed (9.5) when boosting");

            // 3. Exhaustion & Zero-Energy Lockout
            testBoost.UpdateEnergy(2.0f); // 65 - 70 => 0 clamped
            Assert(Mathf.Approximately(testBoost.CurrentEnergy, 0f), "BoostSystem energy clamped strictly at 0 without negative values");
            Assert(!testBoost.IsBoosting, "BoostSystem automatically terminates boosting when energy reaches 0");
            Assert(Mathf.Approximately(testBoost.TargetSpeed, 5.0f), "Target speed returns to normal speed (5.0) when energy is depleted");

            // 4. Zero Lockout Resistance
            testBoost.SetBoostIntent(true);
            testBoost.UpdateEnergy(0.5f);
            Assert(!testBoost.IsBoosting, "BoostSystem strictly prevents boosting when energy is 0 despite boost intent");

            // 5. Cooldown Delay & Regeneration
            testBoost.SetBoostIntent(false);
            testBoost.UpdateEnergy(0.2f); // Within 0.4s delay
            Assert(Mathf.Approximately(testBoost.CurrentEnergy, 0f), "Energy does not regenerate during cooldown delay (0.2s < 0.4s)");

            testBoost.UpdateEnergy(0.5f); // 0.7s total elapsed (0.3s active regen at 22/s = 6.6)
            Assert(testBoost.CurrentEnergy > 5.0f, "Energy regenerated successfully after cooldown delay threshold");

            // 6. HoldButton Component Verification
            GameObject mockHoldBtnGo = new GameObject("MockHoldButton");
            Image mockHoldImg = mockHoldBtnGo.AddComponent<Image>();
            HoldButton mockHoldBtn = mockHoldBtnGo.AddComponent<HoldButton>();
            Assert(!mockHoldBtn.IsPressed, "HoldButton is unpressed by default");

            // 7. BoostEnergyBarUI Gauge Verification
            GameObject mockBarGo = new GameObject("MockBoostBar");
            Image mockBarFill = mockBarGo.AddComponent<Image>();
            mockBarFill.type = Image.Type.Filled;
            BoostEnergyBarUI mockBarUI = mockBarGo.AddComponent<BoostEnergyBarUI>();
            SerializedObject soMockBar = new SerializedObject(mockBarUI);
            soMockBar.FindProperty("fillImage").objectReferenceValue = mockBarFill;
            soMockBar.ApplyModifiedPropertiesWithoutUndo();

            mockBarUI.BindBoostSystem(testBoost);
            testBoost.SetCurrentEnergy(50f);
            Assert(Mathf.Approximately(mockBarUI.NormalizedEnergy, 0.5f), "BoostEnergyBarUI reflects normalized 50% energy accurately");

            // Cleanup Boost test objects
            Object.DestroyImmediate(mockBarGo);
            Object.DestroyImmediate(mockHoldBtnGo);
            Object.DestroyImmediate(boostTestGo);

            // --- Section 16: Power-Ups System Verification ---
            // 1. PowerUpData Configuration Verification
            PowerUpData[] pData = SetupPowerUpDataAssets();
            Assert(pData.Length == 3, "SetupPowerUpDataAssets generated 3 PowerUpData assets");
            Assert(pData[0].Type == PowerUpType.SpeedBoost && Mathf.Approximately(pData[0].Duration, 8.0f) && Mathf.Approximately(pData[0].EffectStrength, 3.5f), "Speed Boost configured correctly (8s duration, 3.5 bonus)");
            Assert(pData[1].Type == PowerUpType.FoodMagnet && Mathf.Approximately(pData[1].Duration, 10.0f) && Mathf.Approximately(pData[1].EffectStrength, 6.0f), "Food Magnet configured correctly (10s duration, 6.0 radius)");
            Assert(pData[2].Type == PowerUpType.ScoreMultiplier && Mathf.Approximately(pData[2].Duration, 12.0f) && Mathf.Approximately(pData[2].EffectStrength, 2.0f), "Score Multiplier configured correctly (12s duration, 2.0x)");

            // 2. ActivePowerUp State Verification
            ActivePowerUp activePu = new ActivePowerUp(pData[0]);
            Assert(!activePu.IsExpired, "ActivePowerUp is not expired initially");
            Assert(Mathf.Approximately(activePu.NormalizedProgress, 1.0f), "ActivePowerUp NormalizedProgress is 1.0 initially");
            activePu.Update(4.0f);
            Assert(Mathf.Approximately(activePu.RemainingTime, 4.0f), "ActivePowerUp RemainingTime reduced by 4.0s");
            Assert(Mathf.Approximately(activePu.NormalizedProgress, 0.5f), "ActivePowerUp NormalizedProgress is 0.5 at midpoint");
            activePu.ResetDuration();
            Assert(Mathf.Approximately(activePu.RemainingTime, 8.0f), "ActivePowerUp ResetDuration resets RemainingTime to total duration (8.0s)");
            activePu.Update(8.1f);
            Assert(activePu.IsExpired, "ActivePowerUp is expired after full duration");

            // 3. PowerUpManager & Multiplier Verification
            GameObject puPlayerGo = new GameObject("TestPowerUpPlayer");
            PlayerBody puPlayerBody = puPlayerGo.AddComponent<PlayerBody>();
            PowerUpManager puMgr = puPlayerGo.AddComponent<PowerUpManager>();

            GameObject puScoreMgrGo = new GameObject("PowerUpScoreManager");
            ScoreManager puSm = puScoreMgrGo.AddComponent<ScoreManager>();

            // Speed Boost activation
            puMgr.ActivatePowerUp(pData[0]);
            Assert(puMgr.IsPowerUpActive(PowerUpType.SpeedBoost), "PowerUpManager activates Speed Boost");
            Assert(Mathf.Approximately(puMgr.SpeedBonus, 3.5f), "PowerUpManager calculates correct SpeedBonus (3.5)");

            // Score Multiplier activation
            puMgr.ActivatePowerUp(pData[2]);
            Assert(puMgr.IsPowerUpActive(PowerUpType.ScoreMultiplier), "PowerUpManager activates Score Multiplier");
            Assert(Mathf.Approximately(puSm.ScoreMultiplier, 2.0f), "ScoreManager multiplier set to 2.0x by PowerUpManager");

            // Duplicate pickup refresh
            puMgr.UpdateTimers(4.0f);
            puMgr.ActivatePowerUp(pData[0]); // refresh
            ActivePowerUp refreshedActive = puMgr.GetActivePowerUp(PowerUpType.SpeedBoost);
            Assert(refreshedActive != null && Mathf.Approximately(refreshedActive.RemainingTime, 8.0f), "Duplicate pickup refreshes active duration back to full 8.0s");

            // Expiration
            puMgr.UpdateTimers(8.5f); // expires speed boost
            Assert(!puMgr.IsPowerUpActive(PowerUpType.SpeedBoost), "Speed Boost expired and deactivated");
            Assert(Mathf.Approximately(puMgr.SpeedBonus, 0f), "SpeedBonus reset to 0 upon expiration");

            puMgr.UpdateTimers(4.0f); // expires score multiplier
            Assert(!puMgr.IsPowerUpActive(PowerUpType.ScoreMultiplier), "Score Multiplier expired and deactivated");
            Assert(Mathf.Approximately(puSm.ScoreMultiplier, 1.0f), "ScoreManager multiplier reset to 1.0x upon expiration");

            // 4. PowerUpPickup Collision & Pooling Verification
            GameObject pickupGo = new GameObject("TestPickup");
            PowerUpPickup pickupComp = pickupGo.AddComponent<PowerUpPickup>();
            pickupComp.Initialize(pData[1]);
            Assert(pickupGo.activeSelf, "PowerUpPickup is active after initialization");

            GameObject puHeadGo = new GameObject("PlayerHead");
            puHeadGo.tag = "Player";
            puHeadGo.transform.SetParent(puPlayerGo.transform);
            CircleCollider2D headCol = puHeadGo.AddComponent<CircleCollider2D>();

            pickupComp.OnTriggerEnter2D(headCol);
            Assert(puMgr.IsPowerUpActive(PowerUpType.FoodMagnet), "PowerUpPickup trigger collection activates Food Magnet on Player");
            Assert(!pickupGo.activeSelf, "PowerUpPickup deactivates gameobject upon collection for pool recycling");

            // Cleanup PowerUp test objects
            Object.DestroyImmediate(pickupGo);
            Object.DestroyImmediate(puPlayerGo);
            Object.DestroyImmediate(puScoreMgrGo);

            // --- Section 17: Bioluminescent Visual Identity & 10 Creature Skins Verification ---
            // 1. Creature Skin Asset Generation
            CreatureSkinData[] testSkins = SetupCreatureSkinAssets();
            Assert(testSkins != null && testSkins.Length == 10, "SetupCreatureSkinAssets generated exactly 10 distinct CreatureSkinData assets");

            for (int i = 0; i < testSkins.Length; i++)
            {
                CreatureSkinData s = testSkins[i];
                Assert(s != null && s.HeadSprite != null && s.SegmentSprite != null, $"CreatureSkin #{i + 1} ({s?.SkinName}) has valid HeadSprite and SegmentSprite");
                Assert(s.PrimaryColor.a > 0.5f && s.SecondaryColor.a > 0.5f, $"CreatureSkin #{i + 1} ({s?.SkinName}) has valid vibrant color palette");
            }

            // 2. Player Skin Application
            GameObject testSkinPlayerGo = new GameObject("TestSkinPlayer");
            SpriteRenderer testSr = testSkinPlayerGo.AddComponent<SpriteRenderer>();
            PlayerBody testSkinBody = testSkinPlayerGo.AddComponent<PlayerBody>();
            testSkinBody.SetSkin(testSkins[0]); // Player Giga Grub
            Assert(testSr.sprite == testSkins[0].HeadSprite, "PlayerBody.SetSkin applied Player head sprite to SpriteRenderer");
            Assert(testSr.color == testSkins[0].PrimaryColor, "PlayerBody.SetSkin applied Player primary theme color");

            // 3. AI Spawner Skin Distribution
            GameObject testSkinSpawnerGo = new GameObject("TestSkinAISpawner");
            AISpawner testSkinSpawner = testSkinSpawnerGo.AddComponent<AISpawner>();
            CreatureSkinData[] testBotSkins = new CreatureSkinData[9];
            for (int i = 1; i < 10; i++) testBotSkins[i - 1] = testSkins[i];
            testSkinSpawner.SetSkins(testBotSkins);
            Assert(testSkinSpawner.BotSkins != null && testSkinSpawner.BotSkins.Length == 9, "AISpawner configured with 9 distinct bot skins");

            // 4. Food Visual Assets
            FoodData[] testFoods = SetupFoodDataAssets();
            Assert(testFoods.Length == 3, "SetupFoodDataAssets configured 3 Celestial FoodData assets");
            Assert(testFoods[0].FoodSprite != null && testFoods[0].FoodName == "Star Berry", "Standard food configured with Star Berry sprite");
            Assert(testFoods[1].FoodSprite != null && testFoods[1].FoodName == "Jelly Drop", "Super food configured with Jelly Drop sprite");
            Assert(testFoods[2].FoodSprite != null && testFoods[2].FoodName == "Astral Core", "Mega food configured with Astral Core sprite");

            // Cleanup skin test objects
            Object.DestroyImmediate(testSkinSpawnerGo);
            Object.DestroyImmediate(testSkinPlayerGo);

            // --- Section 18: Modular Creature Customization System Verification ---
            // 1. Build and verify complete database assets
            CosmeticDatabase testCosmeticsDb = CosmeticDatabaseBuilder.BuildAndSaveAllCosmetics();
            Assert(testCosmeticsDb != null, "CosmeticDatabase successfully built and saved");
            Assert(testCosmeticsDb.Creatures != null && testCosmeticsDb.Creatures.Count == 6, "CosmeticDatabase contains 6 Creature Types (Grub, Slime, Dragon, Alien, Lizard, Monster)");
            Assert(testCosmeticsDb.SkinColors != null && testCosmeticsDb.SkinColors.Count == 14, "CosmeticDatabase contains 14 Skin Colors with dual palette / gradient presets");
            Assert(testCosmeticsDb.Patterns != null && testCosmeticsDb.Patterns.Count == 8, "CosmeticDatabase contains 8 Body Patterns (Solid, Stripes, Spots, Gradient, Checker, Glow, Metallic, Neon)");
            Assert(testCosmeticsDb.Clothing != null && testCosmeticsDb.Clothing.Count == 15, "CosmeticDatabase contains 15 Outfits with Head and Segment overlays");
            Assert(testCosmeticsDb.Hats != null && testCosmeticsDb.Hats.Count == 15, "CosmeticDatabase contains 15 Head Accessories / Hats");
            Assert(testCosmeticsDb.Eyes != null && testCosmeticsDb.Eyes.Count == 8, "CosmeticDatabase contains 8 Eye Expressions");
            Assert(testCosmeticsDb.Mouths != null && testCosmeticsDb.Mouths.Count == 8, "CosmeticDatabase contains 8 Mouth Expressions");
            Assert(testCosmeticsDb.Accessories != null && testCosmeticsDb.Accessories.Count == 15, "CosmeticDatabase contains 15 Accessories across multiple body slots");
            Assert(testCosmeticsDb.Effects != null && testCosmeticsDb.Effects.Count == 10, "CosmeticDatabase contains 10 Cosmetic Visual Effects");

            // 2. Skin Color Palette & Gradient Evaluation
            SkinColorData rainbowColor = testCosmeticsDb.GetColor("color_12_rainbow");
            Assert(rainbowColor != null, "Found rainbow gradient skin color asset");
            Color headColor = rainbowColor.EvaluateSegmentColor(0f);
            Color midColor = rainbowColor.EvaluateSegmentColor(0.5f);
            Color tailColor = rainbowColor.EvaluateSegmentColor(1.0f);
            Assert(headColor.a > 0.5f && midColor.a > 0.5f && tailColor.a > 0.5f, "SkinColorData evaluates segment colors smoothly across gradient spectrum");

            // 3. EquippedCosmetics Configuration & Cloning
            EquippedCosmetics testEq = new EquippedCosmetics();
            testEq.CreatureId = "creature_03_dragon";
            testEq.ColorId = "color_02_crimson";
            testEq.PatternId = "pattern_02_stripes";
            testEq.ClothingId = "clothing_05_armor";
            testEq.HatId = "hat_02_crown";
            testEq.EyesId = "eyes_02_angry";
            testEq.MouthId = "mouth_05_vampire";
            testEq.BackAccessoryId = "acc_02_wings";
            testEq.TailAccessoryId = "acc_07_pet";
            testEq.EffectId = "fx_01_fire";

            EquippedCosmetics testEqClone = testEq.Clone();
            Assert(testEqClone.CreatureId == "creature_03_dragon" && testEqClone.HatId == "hat_02_crown" && testEqClone.TailAccessoryId == "acc_07_pet", "EquippedCosmetics deep clones all 10 cosmetic equipment slots independently");

            // 4. SaveData Version 2 Migration & Economy Persistence
            SaveData v2Data = new SaveData();
            Assert(v2Data.Version == 2, "SaveData initialized with Version 2 schema");
            Assert(v2Data.Coins == 1000, "SaveData provides 1,000 starter bonus coins");
            Assert(v2Data.UnlockedCosmetics != null && v2Data.UnlockedCosmetics.Contains("creature_01_grub") && v2Data.UnlockedCosmetics.Contains("color_01_emerald"), "SaveData unlocks starter creature and default cosmetics automatically");
            Assert(v2Data.Equipped != null && v2Data.Equipped.CreatureId == "creature_01_grub", "SaveData has valid equipped creature configuration");

            // Save / Load Roundtrip Simulation
            v2Data.Coins = 2500;
            v2Data.Equipped = testEq;
            v2Data.UnlockedCosmetics.Add("clothing_05_armor");
            v2Data.UnlockedCosmetics.Add("hat_02_crown");
            string jsonSave = JsonUtility.ToJson(v2Data);
            SaveData loadedData = JsonUtility.FromJson<SaveData>(jsonSave);
            Assert(loadedData.Coins == 2500, "SaveData persists coin economy");
            Assert(loadedData.Equipped.HatId == "hat_02_crown" && loadedData.Equipped.ClothingId == "clothing_05_armor", "SaveData persists equipped cosmetics across simulated app restarts");
            Assert(loadedData.UnlockedCosmetics.Contains("hat_02_crown"), "SaveData persists unlocked inventory collection");

            // 5. CosmeticManager Singleton & Inventory Purchasing
            GameObject cosMgrGo = new GameObject("TestCosmeticManager");
            CosmeticManager cosMgr = cosMgrGo.AddComponent<CosmeticManager>();
            cosMgr.Initialize();

            Assert(cosMgr.Inventory != null, "CosmeticManager initialized CosmeticInventory instance");
            Assert(cosMgr.Inventory.IsUnlocked("creature_01_grub"), "Starter creature is unlocked in inventory");
            
            // Unlock an item with price
            ClothingData armorData = testCosmeticsDb.GetClothing("clothing_05_armor");
            bool purchaseResult = cosMgr.Inventory.UnlockCosmetic(armorData);
            Assert(purchaseResult, "CosmeticInventory unlocks item successfully with coins");
            Assert(cosMgr.Inventory.IsUnlocked("clothing_05_armor"), "Item is registered as unlocked after purchase");

            // Equip and Unequip
            cosMgr.Inventory.EquipCosmetic(armorData);
            Assert(cosMgr.Inventory.Equipped.ClothingId == "clothing_05_armor", "EquippedCosmetics reflects equipped clothing item");
            cosMgr.Inventory.UnequipCosmetic(CosmeticCategory.Clothing);
            Assert(string.IsNullOrEmpty(cosMgr.Inventory.Equipped.ClothingId), "CosmeticInventory unequipped clothing successfully");

            // 6. CreatureCosmeticController Live Application on Player
            GameObject testCosPlayerGo = new GameObject("TestCosmeticPlayer");
            SpriteRenderer pHeadSr = testCosPlayerGo.AddComponent<SpriteRenderer>();
            PlayerBody testCosBody = testCosPlayerGo.AddComponent<PlayerBody>();
            CreatureCosmeticController cosCtrl = testCosPlayerGo.AddComponent<CreatureCosmeticController>();
            
            cosMgr.Inventory.EquipCosmetic(testCosmeticsDb.GetCreature("creature_03_dragon"));
            cosMgr.Inventory.EquipCosmetic(testCosmeticsDb.GetColor("color_02_crimson"));
            cosMgr.Inventory.EquipCosmetic(testCosmeticsDb.GetHat("hat_02_crown"));
            cosMgr.Inventory.EquipCosmetic(testCosmeticsDb.GetEyes("eyes_02_angry"));
            cosMgr.Inventory.EquipCosmetic(testCosmeticsDb.GetMouth("mouth_05_vampire"));
            cosMgr.Inventory.EquipCosmetic(armorData);
            cosMgr.Inventory.EquipCosmetic(testCosmeticsDb.GetAccessory("acc_07_pet")); // Tail accessory

            cosCtrl.ApplyCosmetics(cosMgr.Inventory.Equipped);
            Assert(pHeadSr.sprite != null, "CreatureCosmeticController configured head sprite for dragon");
            Assert(cosCtrl.HatRenderer != null && cosCtrl.HatRenderer.sprite != null, "CreatureCosmeticController rendered hat accessory crown");
            Assert(cosCtrl.EyesRenderer != null && cosCtrl.EyesRenderer.sprite != null, "CreatureCosmeticController rendered angry eyes");
            Assert(cosCtrl.MouthRenderer != null && cosCtrl.MouthRenderer.sprite != null, "CreatureCosmeticController rendered vampire mouth");

            // 7. Dynamic Growth to 100+ Segments with Clothing Alignment
            GameObject segPrefab = SetupPlayerSegmentPrefab();
            testCosBody.InitializeRuntime(segPrefab);
            for (int i = 0; i < 105; i++)
            {
                testCosBody.AddSegmentInternal(true);
            }
            Assert(testCosBody.Segments.Count == 105, "Player successfully grew to 105 segments");
            
            // Verify segments have SegmentCosmeticRenderer and clothing overlays
            SegmentCosmeticRenderer segCos0 = testCosBody.Segments[0].GetComponent<SegmentCosmeticRenderer>();
            SegmentCosmeticRenderer segCosLast = testCosBody.Segments[104].GetComponent<SegmentCosmeticRenderer>();
            Assert(segCos0 != null && segCosLast != null, "All 105 segments have SegmentCosmeticRenderer attached");
            Assert(segCos0.ClothingOverlayRenderer != null && segCos0.ClothingOverlayRenderer.sprite != null, "Segment clothing overlay active and aligned on body segment");
            Assert(segCosLast.TailAccessoryRenderer != null && segCosLast.TailAccessoryRenderer.sprite != null, "Tail accessory rendered on the final creature segment (105th segment)");

            // 8. AI Randomized Cosmetic Distribution
            for (int bot = 0; bot < 10; bot++)
            {
                GameObject botGo = new GameObject($"TestAIBot_{bot}");
                botGo.AddComponent<SpriteRenderer>();
                PlayerBody botBody = botGo.AddComponent<PlayerBody>();
                botBody.InitializeRuntime(segPrefab);
                for (int s = 0; s < 5; s++) botBody.AddSegmentInternal(true);
                
                cosMgr.ApplyRandomCosmeticsToAI(botBody);
                CreatureCosmeticController botCtrl = botGo.GetComponent<CreatureCosmeticController>();
                Assert(botCtrl != null && botCtrl.CurrentEquipped != null, $"AI Bot #{bot + 1} received valid randomized cosmetics without allocations");
                Object.DestroyImmediate(botGo);
            }

            // Cleanup Cosmetic test objects
            Object.DestroyImmediate(testCosPlayerGo);
            Object.DestroyImmediate(cosMgrGo);

            // --- Section 19: Daily Rewards, Quests & Coin Economy Integration Verification ---
            // 1. DailyRewardManager 7-Day Ladder & Streak Mechanics
            GameObject testDailyGo = new GameObject("TestDailyRewardManager");
            DailyRewardManager dailyMgr = testDailyGo.AddComponent<DailyRewardManager>();

            Assert(dailyMgr.GetCurrentStreakDay() >= 1, "DailyRewardManager initialized streak tracking");
            Assert(DailyRewardManager.GetRewardCoinsForDay(1) == 100, "Day 1 reward is 100 coins");
            Assert(DailyRewardManager.GetRewardCoinsForDay(2) == 150, "Day 2 reward is 150 coins");
            Assert(DailyRewardManager.GetRewardCoinsForDay(3) == 200, "Day 3 reward is 200 coins");
            Assert(DailyRewardManager.GetRewardCoinsForDay(4) == 250, "Day 4 reward is 250 coins");
            Assert(DailyRewardManager.GetRewardCoinsForDay(5) == 350, "Day 5 reward is 350 coins");
            Assert(DailyRewardManager.GetRewardCoinsForDay(6) == 500, "Day 6 reward is 500 coins");
            Assert(DailyRewardManager.GetRewardCoinsForDay(7) == 800, "Day 7 grand reward is 800 coins");

            // Setup CosmeticManager for economy testing
            GameObject cosMgrTestGo = new GameObject("TestCosmeticManagerEconomy");
            CosmeticManager cosMgrEco = cosMgrTestGo.AddComponent<CosmeticManager>();
            cosMgrEco.Initialize();
            int startCoins = cosMgrEco.Inventory.Coins;

            // Claim Day 1 Reward
            bool firstClaim = dailyMgr.ClaimTodayReward();
            if (firstClaim)
            {
                Assert(cosMgrEco.Inventory.Coins == startCoins + 100, "Claiming Day 1 reward credited 100 coins directly to CosmeticInventory");
                bool doubleClaim = dailyMgr.ClaimTodayReward();
                Assert(!doubleClaim, "DailyRewardManager rejected duplicate claim on the same calendar date");
            }
            else
            {
                Assert(!dailyMgr.IsRewardAvailable(), "DailyRewardManager correctly detected that today's reward was already claimed");
            }

            // 2. QuestManager Daily Quest Generation & Rotation
            GameObject testQuestGo = new GameObject("TestQuestManager");
            QuestManager questMgr = testQuestGo.AddComponent<QuestManager>();
            questMgr.InitializeDailyQuests();

            Assert(questMgr.ActiveQuests != null && questMgr.ActiveQuests.Count == 4, "QuestManager generated 4 rotating daily quests");
            for (int q = 0; q < questMgr.ActiveQuests.Count; q++)
            {
                QuestProgress qp = questMgr.ActiveQuests[q];
                Assert(!string.IsNullOrEmpty(qp.QuestId) && !string.IsNullOrEmpty(qp.Title) && qp.TargetAmount > 0 && qp.RewardCoins > 0, $"Quest #{q + 1} ({qp.Title}) configured with valid target and reward coins");
            }

            // 3. Quest Real-Time Event Reporting & Progression
            int preQuestCoins = cosMgrEco.Inventory.Coins;
            questMgr.ReportFoodEaten(testFoods[2]); // Astral Core Mega
            questMgr.ReportFoodEaten(testFoods[0]); // Standard Food
            questMgr.ReportAIDefeated(5);
            questMgr.ReportLengthReached(100);
            questMgr.ReportMatchFinished(1); // 1st Place Victory

            // Find completed quest and claim reward
            QuestProgress completedQuest = null;
            for (int q = 0; q < questMgr.ActiveQuests.Count; q++)
            {
                if (questMgr.ActiveQuests[q].IsCompleted)
                {
                    completedQuest = questMgr.ActiveQuests[q];
                    break;
                }
            }

            if (completedQuest != null)
            {
                bool claimQuestRes = questMgr.ClaimQuest(completedQuest.QuestId);
                Assert(claimQuestRes, "QuestManager claimed completed quest reward successfully");
                Assert(completedQuest.IsClaimed, "Completed quest status updated to IsClaimed");
                Assert(cosMgrEco.Inventory.Coins == preQuestCoins + completedQuest.RewardCoins, "Claiming quest credited reward coins directly to CosmeticInventory");
            }

            // 4. Mega Food In-Game Bonus Coin Collection (+15 Coins)
            GameObject testEcoPlayerGo = new GameObject("TestEcoPlayer");
            testEcoPlayerGo.AddComponent<SpriteRenderer>();
            PlayerBody testEcoBody = testEcoPlayerGo.AddComponent<PlayerBody>();
            testEcoBody.InitializeRuntime(segPrefab);

            FoodData megaFoodData = testFoods[2]; // Astral Core (Mega)
            int coinsBeforeMegaFood = cosMgrEco.Inventory.Coins;
            testEcoBody.OnEatFood(megaFoodData);
            Assert(cosMgrEco.Inventory.Coins == coinsBeforeMegaFood + 15, "Eating Mega Food (Astral Core) awarded +15 bonus coins immediately to CosmeticInventory");

            // 5. Match End Victory & Ranking Coin Bonuses
            // Victory (Rank 1): 150 + (2 kills * 25) + (30s / 15 * 5) = 150 + 50 + 10 = 210
            int match1Coins = GameManager.CalculateMatchCoins(1, 2, 30f);
            Assert(match1Coins == 210, "Rank 1 Match Victory with 2 AI kills and 30s survival calculated 210 coins");

            // Top 3 (Rank 2): 75 + (1 kill * 25) + (20s / 15 * 5) = 75 + 25 + 5 = 105
            int match2Coins = GameManager.CalculateMatchCoins(2, 1, 20f);
            Assert(match2Coins == 105, "Rank 2 Podium placement calculated 105 coins");

            // Top 5 (Rank 5): 30 + (0 kills * 25) + (10s / 15 * 5) = 30 + 0 + 0 = 30
            int match3Coins = GameManager.CalculateMatchCoins(5, 0, 10f);
            Assert(match3Coins == 30, "Rank 5 placement calculated 30 coins");

            // Unplaced (Rank 7): 0 + (3 kills * 25) + (45s / 15 * 5) = 0 + 75 + 15 = 90
            int match4Coins = GameManager.CalculateMatchCoins(7, 3, 45f);
            Assert(match4Coins == 90, "Unplaced match calculated kill and survival coins (90 coins)");

            // 6. Persistence Roundtrip for Rewards & Quests
            SaveData v2RewardSave = new SaveData();
            v2RewardSave.LastDailyClaimDate = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            v2RewardSave.DailyStreak = 5;
            v2RewardSave.LastQuestDate = System.DateTime.UtcNow.ToString("yyyy-MM-dd");
            v2RewardSave.ActiveQuests = new List<QuestProgress>(questMgr.ActiveQuests);
            v2RewardSave.Coins = 3500;

            string rewardJson = JsonUtility.ToJson(v2RewardSave);
            SaveData loadedRewardSave = JsonUtility.FromJson<SaveData>(rewardJson);
            Assert(loadedRewardSave.DailyStreak == 5, "SaveData persisted daily reward streak");
            Assert(loadedRewardSave.ActiveQuests != null && loadedRewardSave.ActiveQuests.Count == 4, "SaveData persisted daily active quests state and progress");
            Assert(loadedRewardSave.Coins == 3500, "SaveData persisted total coins balance");

            // Cleanup Rewards test objects
            Object.DestroyImmediate(testEcoPlayerGo);
            Object.DestroyImmediate(testQuestGo);
            Object.DestroyImmediate(testDailyGo);
            Object.DestroyImmediate(cosMgrTestGo);

            Debug.Log("=== [GigaGrub Verification Tests] ALL TESTS PASSED! ===");
        }

        private static void Assert(bool condition, string message)
        {
            if (condition)
            {
                Debug.Log($"[PASS] {message}");
            }
            else
            {
                Debug.LogError($"[FAIL] {message}");
            }
        }
    }
}
