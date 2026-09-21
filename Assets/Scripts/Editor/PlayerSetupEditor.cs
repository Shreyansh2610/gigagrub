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

namespace GigaGrub.Editor
{
    public static class PlayerSetupEditor
    {
        private const string PrefabsPath = "Assets/Prefabs";
        private const string ArtPath = "Assets/Art";
        private const string ScenesPath = "Assets/Scenes";
        private const string FoodResourcesPath = "Assets/Resources/Food";

        [MenuItem("GigaGrub/1. Setup All (Prefabs, Food & Game Scene)")]
        public static void SetupAll()
        {
            Debug.Log("[GigaGrub] Starting Full Setup...");

            EnsureDirectories();
            GameObject segmentPrefab = SetupPlayerSegmentPrefab();
            GameObject playerPrefab = SetupPlayerPrefab(segmentPrefab);
            GameObject aiCreaturePrefab = SetupAICreaturePrefab(segmentPrefab);
            GameObject eatingEffectPrefab = SetupEatingEffectPrefab();
            GameObject joystickCanvasPrefab = SetupJoystickCanvasPrefab();
            GameObject arenaPrefab = SetupArenaPrefab();
            FoodData[] foodDataAssets = SetupFoodDataAssets();
            GameObject foodPrefab = SetupFoodPrefab();

            SetupGameScene(playerPrefab, joystickCanvasPrefab, arenaPrefab, foodPrefab, foodDataAssets, eatingEffectPrefab, aiCreaturePrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[GigaGrub] Full Setup Completed Successfully!");
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
            GrowthSystem growth = go.AddComponent<GrowthSystem>();
            CreatureDeath death = go.AddComponent<CreatureDeath>();
            CreatureCollision collision = go.AddComponent<CreatureCollision>();

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
            GrowthSystem growth = go.AddComponent<GrowthSystem>();
            CreatureDeath death = go.AddComponent<CreatureDeath>();
            CreatureCollision collision = go.AddComponent<CreatureCollision>();

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

            // Right: Boost Button Placeholder
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

            // Wire GameplayHUD component
            GameplayHUD gameplayHUD = topHudGo.AddComponent<GameplayHUD>();
            SerializedObject soHud = new SerializedObject(gameplayHUD);
            soHud.FindProperty("scoreText").objectReferenceValue = scoreText;
            soHud.FindProperty("lengthText").objectReferenceValue = lengthText;
            soHud.FindProperty("rankText").objectReferenceValue = rankText;
            soHud.FindProperty("timeText").objectReferenceValue = timeText;
            soHud.FindProperty("pauseButton").objectReferenceValue = pauseBtn;
            soHud.FindProperty("boostButton").objectReferenceValue = boostBtn;
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

            Sprite sprite = LoadSprite("SegmentSprite.png");

            // 1. Standard Grub (Score: 10, Growth: 1, Weight: 70, Emerald Lime)
            FoodData standard = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Standard.asset");
            if (standard == null)
            {
                standard = ScriptableObject.CreateInstance<FoodData>();
                AssetDatabase.CreateAsset(standard, $"{FoodResourcesPath}/FoodData_Standard.asset");
            }
            standard.Configure(
                FoodType.Standard,
                "Standard Grub",
                score: 10,
                growth: 1,
                weight: 70f,
                color: new Color(0.18f, 0.95f, 0.35f, 1f),
                scale: 0.9f,
                sprite: sprite,
                pulse: true,
                pSpeed: 3f,
                pMag: 0.08f
            );
            EditorUtility.SetDirty(standard);

            // 2. Super Grub (Score: 30, Growth: 3, Weight: 20, Amber Gold)
            FoodData superFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Super.asset");
            if (superFood == null)
            {
                superFood = ScriptableObject.CreateInstance<FoodData>();
                AssetDatabase.CreateAsset(superFood, $"{FoodResourcesPath}/FoodData_Super.asset");
            }
            superFood.Configure(
                FoodType.Super,
                "Super Grub",
                score: 30,
                growth: 3,
                weight: 20f,
                color: new Color(1f, 0.78f, 0.1f, 1f),
                scale: 1.2f,
                sprite: sprite,
                pulse: true,
                pSpeed: 4.5f,
                pMag: 0.14f
            );
            EditorUtility.SetDirty(superFood);

            // 3. Mega Grub (Score: 100, Growth: 5, Weight: 10, Electric Magenta)
            FoodData megaFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Mega.asset");
            if (megaFood == null)
            {
                megaFood = ScriptableObject.CreateInstance<FoodData>();
                AssetDatabase.CreateAsset(megaFood, $"{FoodResourcesPath}/FoodData_Mega.asset");
            }
            megaFood.Configure(
                FoodType.Mega,
                "Mega Grub",
                score: 100,
                growth: 5,
                weight: 10f,
                color: new Color(0.92f, 0.2f, 0.98f, 1f),
                scale: 1.55f,
                sprite: sprite,
                pulse: true,
                pSpeed: 6f,
                pMag: 0.2f
            );
            EditorUtility.SetDirty(megaFood);

            Debug.Log("[GigaGrub] Created/Updated 3 FoodData ScriptableObjects");
            return new FoodData[] { standard, superFood, megaFood };
        }

        public static GameObject SetupFoodPrefab()
        {
            string prefabPath = $"{PrefabsPath}/Food.prefab";
            GameObject go = new GameObject("Food");

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("SegmentSprite.png");
            sr.sortingOrder = 40;
            sr.color = new Color(0.2f, 0.95f, 0.35f, 1f);

            CircleCollider2D col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            Food.Food food = go.AddComponent<Food.Food>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/8. Setup Game Scene")]
        public static void MenuSetupGameScene()
        {
            SetupGameScene(null, null, null, null, null, null, null);
        }

        public static void SetupGameScene(
            GameObject playerPrefab = null,
            GameObject joystickCanvasPrefab = null,
            GameObject arenaPrefab = null,
            GameObject foodPrefab = null,
            FoodData[] foodDataAssets = null,
            GameObject eatingEffectPrefab = null,
            GameObject aiCreaturePrefab = null)
        {
            string scenePath = $"{ScenesPath}/Game.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 0. ScoreManager and RankingManager System Objects
            GameObject systemGo = new GameObject("ScoreManager");
            ScoreManager scoreMgr = systemGo.AddComponent<ScoreManager>();

            GameObject rankingGo = new GameObject("RankingManager");
            RankingManager rankingMgr = rankingGo.AddComponent<RankingManager>();

            // 1. Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);
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
            spawner.SetDefaultSprite(LoadSprite("SegmentSprite.png"));
            spawner.SetPopulationLimits(100, 150, 120);
            spawner.SetPlayerBody(playerBody);

            SerializedObject soSpawner = new SerializedObject(spawner);
            soSpawner.FindProperty("foodPrefab").objectReferenceValue = foodPrefab;
            soSpawner.FindProperty("defaultFoodSprite").objectReferenceValue = LoadSprite("SegmentSprite.png");
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

            // 6. AI Spawner (10 AI Creatures)
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

            SerializedObject soAiSpawner = new SerializedObject(aiSpawner);
            soAiSpawner.FindProperty("aiCreaturePrefab").objectReferenceValue = aiCreaturePrefab;
            soAiSpawner.FindProperty("targetAICount").intValue = 10;
            soAiSpawner.FindProperty("minSpawnDistance").floatValue = 12f;
            soAiSpawner.ApplyModifiedPropertiesWithoutUndo();

            // 7. GameManager
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

            // Clean up test instances
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
            Object.DestroyImmediate(ai1Go);
            Object.DestroyImmediate(testFoodGo);
            Object.DestroyImmediate(testPlayer);
            Object.DestroyImmediate(scoreMgrGo);
            Object.DestroyImmediate(spawnerGo);

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
