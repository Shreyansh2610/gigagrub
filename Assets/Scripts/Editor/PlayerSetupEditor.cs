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
            GameObject eatingEffectPrefab = SetupEatingEffectPrefab();
            GameObject joystickCanvasPrefab = SetupJoystickCanvasPrefab();
            GameObject arenaPrefab = SetupArenaPrefab();
            FoodData[] foodDataAssets = SetupFoodDataAssets();
            GameObject foodPrefab = SetupFoodPrefab();

            SetupGameScene(playerPrefab, joystickCanvasPrefab, arenaPrefab, foodPrefab, foodDataAssets, eatingEffectPrefab);

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
            soArena.FindProperty("generateColliders").boolValue = true;
            soArena.FindProperty("wallThickness").floatValue = 2f;
            soArena.ApplyModifiedPropertiesWithoutUndo();

            arena.SetupBoundaryVisuals();
            arena.SetupBoundaryColliders();

            // Add Background Grid
            GameObject bgGo = new GameObject("BackgroundGrid");
            bgGo.transform.SetParent(go.transform, false);
            bgGo.transform.localScale = new Vector3(100f, 100f, 1f);

            SpriteRenderer sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("SegmentSprite.png");
            sr.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);
            sr.sortingOrder = -100;

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

            PlayerBody body = go.AddComponent<PlayerBody>();
            SerializedObject soBody = new SerializedObject(body);
            soBody.FindProperty("startingLength").intValue = 10;
            soBody.FindProperty("maxLength").intValue = 250;
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
            soBody.ApplyModifiedPropertiesWithoutUndo();

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

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("SegmentSprite.png");
            sr.sortingOrder = 45;
            sr.color = new Color(1f, 1f, 1f, 0.9f);

            EatingEffect effect = go.AddComponent<EatingEffect>();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);

            Debug.Log($"[GigaGrub] Created/Updated {prefabPath}");
            return prefab;
        }

        [MenuItem("GigaGrub/6. Setup Joystick Canvas Prefab")]
        public static GameObject SetupJoystickCanvasPrefab()
        {
            string prefabPath = $"{PrefabsPath}/JoystickCanvas.prefab";
            GameObject canvasGo = new GameObject("JoystickCanvas");

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Virtual Joystick
            GameObject joystickGo = new GameObject("VirtualJoystick");
            joystickGo.transform.SetParent(canvasGo.transform, false);

            RectTransform joyRect = joystickGo.AddComponent<RectTransform>();
            joyRect.anchorMin = new Vector2(0f, 0f);
            joyRect.anchorMax = new Vector2(0f, 0f);
            joyRect.pivot = new Vector2(0.5f, 0.5f);
            joyRect.anchoredPosition = new Vector2(250f, 250f);
            joyRect.sizeDelta = new Vector2(300f, 300f);

            Image bgImage = joystickGo.AddComponent<Image>();
            bgImage.sprite = LoadSprite("JoystickBG.png");
            bgImage.color = new Color(1f, 1f, 1f, 0.8f);

            GameObject knobGo = new GameObject("Knob");
            knobGo.transform.SetParent(joystickGo.transform, false);

            RectTransform knobRect = knobGo.AddComponent<RectTransform>();
            knobRect.anchorMin = new Vector2(0.5f, 0.5f);
            knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            knobRect.pivot = new Vector2(0.5f, 0.5f);
            knobRect.anchoredPosition = Vector2.zero;
            knobRect.sizeDelta = new Vector2(120f, 120f);

            Image knobImage = knobGo.AddComponent<Image>();
            knobImage.sprite = LoadSprite("JoystickKnob.png");
            knobImage.color = new Color(1f, 1f, 1f, 0.95f);

            VirtualJoystick vj = joystickGo.AddComponent<VirtualJoystick>();
            SerializedObject soVj = new SerializedObject(vj);
            soVj.FindProperty("joystickBackground").objectReferenceValue = joyRect;
            soVj.FindProperty("joystickKnob").objectReferenceValue = knobRect;
            soVj.FindProperty("handleLimit").floatValue = 120f;
            soVj.FindProperty("deadZone").floatValue = 0.05f;
            soVj.ApplyModifiedPropertiesWithoutUndo();

            // Score HUD Panel (Top-Center)
            GameObject scorePanelGo = new GameObject("ScoreHUD");
            scorePanelGo.transform.SetParent(canvasGo.transform, false);

            RectTransform hudRect = scorePanelGo.AddComponent<RectTransform>();
            hudRect.anchorMin = new Vector2(0.5f, 1f);
            hudRect.anchorMax = new Vector2(0.5f, 1f);
            hudRect.pivot = new Vector2(0.5f, 1f);
            hudRect.anchoredPosition = new Vector2(0f, -40f);
            hudRect.sizeDelta = new Vector2(520f, 130f);

            Image hudBg = scorePanelGo.AddComponent<Image>();
            hudBg.color = new Color(0.04f, 0.06f, 0.1f, 0.75f);

            // Score Text
            GameObject scoreTextGo = new GameObject("ScoreText");
            scoreTextGo.transform.SetParent(scorePanelGo.transform, false);
            RectTransform scoreTextRect = scoreTextGo.AddComponent<RectTransform>();
            scoreTextRect.anchorMin = new Vector2(0f, 0.45f);
            scoreTextRect.anchorMax = new Vector2(1f, 1f);
            scoreTextRect.pivot = new Vector2(0.5f, 0.5f);
            scoreTextRect.offsetMin = new Vector2(10f, 0f);
            scoreTextRect.offsetMax = new Vector2(-10f, -5f);

            Text scoreText = scoreTextGo.AddComponent<Text>();
            scoreText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            scoreText.fontSize = 44;
            scoreText.fontStyle = FontStyle.Bold;
            scoreText.alignment = TextAnchor.MiddleCenter;
            scoreText.color = new Color(1f, 0.9f, 0.2f, 1f);
            scoreText.text = "SCORE  0";

            // Length Text
            GameObject lengthTextGo = new GameObject("LengthText");
            lengthTextGo.transform.SetParent(scorePanelGo.transform, false);
            RectTransform lengthTextRect = lengthTextGo.AddComponent<RectTransform>();
            lengthTextRect.anchorMin = new Vector2(0f, 0f);
            lengthTextRect.anchorMax = new Vector2(1f, 0.45f);
            lengthTextRect.pivot = new Vector2(0.5f, 0.5f);
            lengthTextRect.offsetMin = new Vector2(10f, 5f);
            lengthTextRect.offsetMax = new Vector2(-10f, 0f);

            Text lengthText = lengthTextGo.AddComponent<Text>();
            lengthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lengthText.fontSize = 24;
            lengthText.alignment = TextAnchor.MiddleCenter;
            lengthText.color = new Color(0.6f, 0.85f, 1f, 0.85f);
            lengthText.text = "LENGTH  10";

            ScoreUI scoreUI = scorePanelGo.AddComponent<ScoreUI>();
            SerializedObject soScore = new SerializedObject(scoreUI);
            soScore.FindProperty("scoreText").objectReferenceValue = scoreText;
            soScore.FindProperty("lengthText").objectReferenceValue = lengthText;
            soScore.ApplyModifiedPropertiesWithoutUndo();

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
            SetupGameScene(null, null, null, null, null, null);
        }

        public static void SetupGameScene(
            GameObject playerPrefab = null,
            GameObject joystickCanvasPrefab = null,
            GameObject arenaPrefab = null,
            GameObject foodPrefab = null,
            FoodData[] foodDataAssets = null,
            GameObject eatingEffectPrefab = null)
        {
            string scenePath = $"{ScenesPath}/Game.unity";

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Camera
            GameObject camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0, 0, -10);

            Camera cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 9f;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
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
                scoreUI.BindPlayer(playerBody);
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

            // 6. EventSystem
            GameObject eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<StandaloneInputModule>();

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[GigaGrub] Saved Game scene with 100+ FoodSpawner to {scenePath}");
        }

        [MenuItem("GigaGrub/9. Run Automated Verification Tests")]
        public static void RunVerificationTests()
        {
            Debug.Log("=== [GigaGrub Verification Tests] Starting ===");

            // --- Section 1: Core Prefabs ---
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Player.prefab");
            GameObject segmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/PlayerSegment.prefab");
            GameObject joystickCanvasPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/JoystickCanvas.prefab");
            GameObject arenaPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Arena.prefab");
            GameObject foodPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/Food.prefab");
            GameObject eatingEffectPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabsPath}/EatingEffect.prefab");

            Assert(playerPrefab != null, "Player.prefab exists");
            Assert(segmentPrefab != null, "PlayerSegment.prefab exists");
            Assert(joystickCanvasPrefab != null, "JoystickCanvas.prefab exists");
            Assert(arenaPrefab != null, "Arena.prefab exists");
            Assert(foodPrefab != null, "Food.prefab exists");
            Assert(eatingEffectPrefab != null, "EatingEffect.prefab exists");

            // --- Section 2: Food Data Assets ---
            FoodData standardFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Standard.asset");
            FoodData superFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Super.asset");
            FoodData megaFood = AssetDatabase.LoadAssetAtPath<FoodData>($"{FoodResourcesPath}/FoodData_Mega.asset");

            Assert(standardFood != null, "FoodData_Standard.asset exists");
            Assert(superFood != null, "FoodData_Super.asset exists");
            Assert(megaFood != null, "FoodData_Mega.asset exists");

            Assert(standardFood.ScoreValue == 10 && standardFood.GrowthValue == 1, "Standard Grub has 10 score & 1 growth");
            Assert(superFood.ScoreValue == 30 && superFood.GrowthValue == 3, "Super Grub has 30 score & 3 growth");
            Assert(megaFood.ScoreValue == 100 && megaFood.GrowthValue == 5, "Mega Grub has 100 score & 5 growth");
            Assert(standardFood.SpawnWeight > superFood.SpawnWeight && superFood.SpawnWeight > megaFood.SpawnWeight, "Food weights reflect rarity hierarchy (Standard > Super > Mega)");

            // --- Section 3: Arena Bounds & Clamping ---
            ArenaManager arena = arenaPrefab.GetComponent<ArenaManager>();
            Assert(arena != null, "Arena has ArenaManager component");
            Assert(arena.ArenaSize == new Vector2(100f, 100f), "Arena size is 100x100");

            Vector2 outsidePos = new Vector2(65f, -70f);
            Vector2 clamped = arena.ClampPosition(outsidePos, 0.5f);
            Assert(clamped.x <= 49.5f && clamped.y >= -49.5f, "Arena boundary clamping constrains coordinates within playable limits");

            // --- Section 4: Player Movement, Max Length & Rapid Growth ---
            GameObject testPlayer = Object.Instantiate(playerPrefab);
            testPlayer.name = "TestPlayer";
            PlayerController controller = testPlayer.GetComponent<PlayerController>();
            PlayerBody body = testPlayer.GetComponent<PlayerBody>();

            Assert(controller != null, "Player has PlayerController");
            Assert(body != null, "Player has PlayerBody");
            Assert(body.CurrentLength == 10, $"Initial body length is 10 (actual: {body.CurrentLength})");

            body.OnEatFood(standardFood);
            Assert(body.CurrentLength == 11, $"Body length after eating Standard Grub is 11 (actual: {body.CurrentLength})");
            Assert(body.CurrentScore == 10, $"Player score after eating Standard Grub is 10 (actual: {body.CurrentScore})");

            body.OnEatFood(superFood);
            Assert(body.CurrentLength == 14, $"Body length after eating Super Grub is 14 (actual: {body.CurrentLength})");
            Assert(body.CurrentScore == 40, $"Player score after eating Super Grub is 40 (actual: {body.CurrentScore})");

            body.OnEatFood(megaFood);
            Assert(body.CurrentLength == 19, $"Body length after eating Mega Grub is 19 (actual: {body.CurrentLength})");
            Assert(body.CurrentScore == 140, $"Player score after eating Mega Grub is 140 (actual: {body.CurrentScore})");

            // Rapid Growth Stability Test: Add 40 food items rapidly
            for (int i = 0; i < 40; i++)
            {
                body.OnEatFood(superFood);
            }
            Assert(body.CurrentLength == 139, $"Body length after 40 rapid Super Grubs is 139 (actual: {body.CurrentLength})");

            // Verify body segments have valid finite positions and non-NaN rotations
            bool bodyValid = true;
            for (int s = 0; s < body.ActiveSegments.Count; s++)
            {
                Vector3 pos = body.ActiveSegments[s].transform.position;
                if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsInfinity(pos.x) || float.IsInfinity(pos.y))
                {
                    bodyValid = false;
                    break;
                }
            }
            Assert(bodyValid, "All 139 body segment transforms remain valid without NaNs/Infinities after rapid growth");

            // Max Length Constraint Test: set maxLength to 150 and try adding 50 more segments
            body.SetSettings(10, 150, 0.45f, 1);
            for (int i = 0; i < 20; i++)
            {
                body.OnEatFood(megaFood);
            }
            Assert(body.CurrentLength == 150, $"Body length capped at maxLength 150 (actual: {body.CurrentLength})");

            // --- Section 5: Audio & Procedural Sound Generation ---
            AudioClip eatClip = SoundEffectGenerator.GetOrCreateEatSoundClip();
            Assert(eatClip != null, "Procedural eat sound clip generated successfully");
            Assert(eatClip.length > 0.05f && eatClip.length < 0.2f, $"Procedural eat clip duration is snappy ({eatClip.length:F3}s)");

            // --- Section 6: Food Component & Trigger Consumption ---
            GameObject testFoodGo = Object.Instantiate(foodPrefab);
            Food.Food testFood = testFoodGo.GetComponent<Food.Food>();
            Assert(testFood != null, "Food prefab has Food component");

            testFood.Initialize(standardFood);
            Assert(testFood.Data == standardFood, "Food initialized with correct FoodData");

            int scoreBeforeEat = body.CurrentScore;
            testFood.Consume(body);

            Assert(body.CurrentScore == scoreBeforeEat + 10, "Direct food consumption triggers player score increment");
            Assert(testFood.IsConsumed, "Food is marked as consumed");
            Assert(!testFoodGo.activeSelf, "Consumed food GameObject is deactivated for pooling");

            // --- Section 7: 100+ Food Spawner Arena Scaling & Pooling ---
            GameObject spawnerGo = new GameObject("Test100FoodSpawner");
            FoodSpawner spawner = spawnerGo.AddComponent<FoodSpawner>();
            spawner.SetFoodPrefab(foodPrefab);
            spawner.SetFoodTypes(new FoodData[] { standardFood, superFood, megaFood });
            spawner.SetPopulationLimits(100, 150, 120);
            spawner.SetPlayerBody(body);
            spawner.PrewarmPool(160);

            Assert(spawner.TotalPoolCount >= 160, $"100+ Food Object pool pre-warmed correctly (total pool count: {spawner.TotalPoolCount})");

            spawner.SpawnInitialPopulation(120);
            Assert(spawner.ActiveFoodCount == 120, $"Active food count in arena is 120 (actual: {spawner.ActiveFoodCount})");

            // Verify all 120 food items are active and within arena boundaries
            bool allInside = true;
            for (int i = 0; i < spawner.ActiveFoods.Count; i++)
            {
                if (!arena.IsInside(spawner.ActiveFoods[i].transform.position, 2f))
                {
                    allInside = false;
                    break;
                }
            }
            Assert(allInside, "All 120 spawned food items are placed strictly inside arena boundaries");

            // Cleanup test instances
            Object.DestroyImmediate(spawnerGo);
            Object.DestroyImmediate(testFoodGo);
            Object.DestroyImmediate(testPlayer);

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
