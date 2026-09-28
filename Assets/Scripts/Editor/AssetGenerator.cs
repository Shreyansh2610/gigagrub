using System.IO;
using UnityEngine;
using UnityEditor;

namespace GigaGrub.Editor
{
    public static class AssetGenerator
    {
        private const string ArtRoot = "Assets/Art";
        private const string CreaturesPath = "Assets/Art/Creatures";
        private const string FoodPath = "Assets/Art/Food";
        private const string PowerUpsPath = "Assets/Art/PowerUps";
        private const string ArenaPath = "Assets/Art/Arena";
        private const string UIPath = "Assets/Art/UI";

        [MenuItem("GigaGrub/0. Generate All Original Art Assets")]
        public static void GenerateAllArtAssets()
        {
            EnsureArtDirectories();

            Debug.Log("[GigaGrub] Generating original Bioluminescent Cute Grub art assets...");

            // 1. Generate 10 Unique Creature Heads & Segments
            GenerateCreatureSprites();

            // 2. Generate 4 Celestial Food Sprites
            GenerateFoodSprites();

            // 3. Generate 3 Power-Up Badges
            GeneratePowerUpSprites();

            // 4. Generate Arena Tiles & Boundary Visuals
            GenerateArenaSprites();

            // 5. Generate UI Assets & Logo
            GenerateUISprites();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            // Configure texture import settings
            ConfigureAllTextureImporters();

            Debug.Log("[GigaGrub] All original art assets generated and imported successfully!");
        }

        private static void EnsureArtDirectories()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder(ArtRoot, "Creatures");
            EnsureFolder(ArtRoot, "Food");
            EnsureFolder(ArtRoot, "PowerUps");
            EnsureFolder(ArtRoot, "Arena");
            EnsureFolder(ArtRoot, "UI");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string full = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        // =========================================================================
        // 1. CREATURE ASSET GENERATION (10 Distinct Heads + 10 Matching Segments)
        // =========================================================================
        private static void GenerateCreatureSprites()
        {
            const int size = 256;

            // 1. Player (Giga Grub: Mint/Emerald)
            SaveTexture($"{CreaturesPath}/Head_01_Player.png", CreateHead_Player(size));
            SaveTexture($"{CreaturesPath}/Segment_01_Player.png", CreateSegment_Ringed(size, new Color(0.10f, 0.90f, 0.65f), new Color(0.04f, 0.55f, 0.38f), new Color(0.55f, 1f, 0.85f)));

            // 2. Bot Sprout (Leaf Lime)
            SaveTexture($"{CreaturesPath}/Head_02_Sprout.png", CreateHead_Sprout(size));
            SaveTexture($"{CreaturesPath}/Segment_02_Sprout.png", CreateSegment_Organic(size, new Color(0.48f, 0.95f, 0.12f), new Color(0.28f, 0.65f, 0.05f), new Color(0.78f, 1f, 0.45f)));

            // 3. Bot Spark (Electric Cyan)
            SaveTexture($"{CreaturesPath}/Head_03_Spark.png", CreateHead_Spark(size));
            SaveTexture($"{CreaturesPath}/Segment_03_Spark.png", CreateSegment_Tech(size, new Color(0.12f, 0.88f, 1f), new Color(0.02f, 0.48f, 0.75f), new Color(0.65f, 0.95f, 1f)));

            // 4. Bot Ruby (Crimson Coral)
            SaveTexture($"{CreaturesPath}/Head_04_Ruby.png", CreateHead_Ruby(size));
            SaveTexture($"{CreaturesPath}/Segment_04_Ruby.png", CreateSegment_Heart(size, new Color(1f, 0.22f, 0.38f), new Color(0.68f, 0.08f, 0.20f), new Color(1f, 0.65f, 0.75f)));

            // 5. Bot Sunny (Radiant Gold)
            SaveTexture($"{CreaturesPath}/Head_05_Sunny.png", CreateHead_Sunny(size));
            SaveTexture($"{CreaturesPath}/Segment_05_Sunny.png", CreateSegment_Sun(size, new Color(1f, 0.85f, 0.12f), new Color(0.75f, 0.50f, 0.02f), new Color(1f, 0.95f, 0.55f)));

            // 6. Bot Violet (Astral Orchid)
            SaveTexture($"{CreaturesPath}/Head_06_Violet.png", CreateHead_Violet(size));
            SaveTexture($"{CreaturesPath}/Segment_06_Violet.png", CreateSegment_Swirl(size, new Color(0.82f, 0.22f, 0.98f), new Color(0.45f, 0.05f, 0.68f), new Color(0.95f, 0.65f, 1f)));

            // 7. Bot Bubble (Bubblegum Pink)
            SaveTexture($"{CreaturesPath}/Head_07_Bubble.png", CreateHead_Bubble(size));
            SaveTexture($"{CreaturesPath}/Segment_07_Bubble.png", CreateSegment_Bubble(size, new Color(1f, 0.35f, 0.72f), new Color(0.72f, 0.12f, 0.45f), new Color(1f, 0.75f, 0.90f)));

            // 8. Bot Frost (Glacier Blue)
            SaveTexture($"{CreaturesPath}/Head_08_Frost.png", CreateHead_Frost(size));
            SaveTexture($"{CreaturesPath}/Segment_08_Frost.png", CreateSegment_Crystal(size, new Color(0.45f, 0.85f, 1f), new Color(0.15f, 0.45f, 0.75f), new Color(0.85f, 0.95f, 1f)));

            // 9. Bot Flame (Tangerine Amber)
            SaveTexture($"{CreaturesPath}/Head_09_Flame.png", CreateHead_Flame(size));
            SaveTexture($"{CreaturesPath}/Segment_09_Flame.png", CreateSegment_Flame(size, new Color(1f, 0.48f, 0.10f), new Color(0.72f, 0.22f, 0.02f), new Color(1f, 0.80f, 0.40f)));

            // 10. Bot Shadow (Midnight Indigo)
            SaveTexture($"{CreaturesPath}/Head_10_Shadow.png", CreateHead_Shadow(size));
            SaveTexture($"{CreaturesPath}/Segment_10_Shadow.png", CreateSegment_Cosmic(size, new Color(0.48f, 0.28f, 0.98f), new Color(0.20f, 0.08f, 0.55f), new Color(0.80f, 0.65f, 1f)));

            // Also replace legacy root HeadSprite and SegmentSprite with Player theme for instant fallback
            SaveTexture($"{ArtRoot}/HeadSprite.png", CreateHead_Player(size));
            SaveTexture($"{ArtRoot}/SegmentSprite.png", CreateSegment_Ringed(size, new Color(0.10f, 0.90f, 0.65f), new Color(0.04f, 0.55f, 0.38f), new Color(0.55f, 1f, 0.85f)));
        }

        // =========================================================================
        // 2. FOOD SPRITES (4 Celestial Food Items)
        // =========================================================================
        private static void GenerateFoodSprites()
        {
            const int size = 128;
            SaveTexture($"{FoodPath}/Food_StarBerry.png", CreateFood_StarBerry(size));
            SaveTexture($"{FoodPath}/Food_JellyDrop.png", CreateFood_JellyDrop(size));
            SaveTexture($"{FoodPath}/Food_AstralCore.png", CreateFood_AstralCore(size));
            SaveTexture($"{FoodPath}/Food_SporePod.png", CreateFood_SporePod(size));
        }

        // =========================================================================
        // 3. POWER-UP BADGES (3 Badges)
        // =========================================================================
        private static void GeneratePowerUpSprites()
        {
            const int size = 128;
            SaveTexture($"{PowerUpsPath}/PowerUp_SpeedSurge.png", CreatePowerUp_Speed(size));
            SaveTexture($"{PowerUpsPath}/PowerUp_FoodMagnet.png", CreatePowerUp_Magnet(size));
            SaveTexture($"{PowerUpsPath}/PowerUp_2xScore.png", CreatePowerUp_Multiplier(size));
        }

        // =========================================================================
        // 4. ARENA SPRITES (Floor Tile & Boundary Wall)
        // =========================================================================
        private static void GenerateArenaSprites()
        {
            SaveTexture($"{ArenaPath}/Arena_HexTile.png", CreateArena_HexTile(256));
            SaveTexture($"{ArenaPath}/Arena_WallGlow.png", CreateArena_WallGlow(128));
            SaveTexture($"{ArenaPath}/Arena_SporeDecor.png", CreateArena_SporeDecor(128));
        }

        // =========================================================================
        // 5. UI SPRITES (Logo, Joystick, Badges)
        // =========================================================================
        private static void GenerateUISprites()
        {
            SaveTexture($"{UIPath}/Logo_GigaGrub.png", CreateUI_Logo(512, 160));
            SaveTexture($"{UIPath}/Joystick_Base.png", CreateUI_JoystickBase(256));
            SaveTexture($"{UIPath}/Joystick_Knob.png", CreateUI_JoystickKnob(128));
            SaveTexture($"{UIPath}/UI_Panel_Card.png", CreateUI_Card(128));

            // Legacy overrides
            SaveTexture($"{ArtRoot}/JoystickBG.png", CreateUI_JoystickBase(256));
            SaveTexture($"{ArtRoot}/JoystickKnob.png", CreateUI_JoystickKnob(128));
        }

        // =========================================================================
        // PROCEDURAL DRAWING PRIMITIVES & HELPERS
        // =========================================================================
        private static Texture2D CreateEmptyTexture(int width, int height)
        {
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            Color[] clear = new Color[width * height];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        private static void BlendPixel(Texture2D tex, int x, int y, Color color)
        {
            if (x < 0 || x >= tex.width || y < 0 || y >= tex.height || color.a <= 0f) return;
            Color dst = tex.GetPixel(x, y);
            float outA = color.a + dst.a * (1f - color.a);
            if (outA <= 0.001f) return;
            Color outCol = (color * color.a + dst * dst.a * (1f - color.a)) / outA;
            outCol.a = outA;
            tex.SetPixel(x, y, outCol);
        }

        private static void DrawFilledCircle(Texture2D tex, float cx, float cy, float radius, Color color, float feather = 1.2f)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius - feather));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + radius + feather));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius - feather));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + radius + feather));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist <= radius - feather)
                    {
                        BlendPixel(tex, x, y, color);
                    }
                    else if (dist < radius + feather)
                    {
                        float alpha = Mathf.Clamp01((radius + feather - dist) / (feather * 2f)) * color.a;
                        BlendPixel(tex, x, y, new Color(color.r, color.g, color.b, alpha));
                    }
                }
            }
        }

        private static void DrawRing(Texture2D tex, float cx, float cy, float innerR, float outerR, Color color, float feather = 1.2f)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - outerR - feather));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + outerR + feather));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - outerR - feather));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + outerR + feather));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist >= innerR + feather && dist <= outerR - feather)
                    {
                        BlendPixel(tex, x, y, color);
                    }
                    else if (dist > innerR - feather && dist < innerR + feather)
                    {
                        float alpha = Mathf.Clamp01((dist - (innerR - feather)) / (feather * 2f)) * color.a;
                        BlendPixel(tex, x, y, new Color(color.r, color.g, color.b, alpha));
                    }
                    else if (dist > outerR - feather && dist < outerR + feather)
                    {
                        float alpha = Mathf.Clamp01(((outerR + feather) - dist) / (feather * 2f)) * color.a;
                        BlendPixel(tex, x, y, new Color(color.r, color.g, color.b, alpha));
                    }
                }
            }
        }

        private static void DrawFilledEllipse(Texture2D tex, float cx, float cy, float rx, float ry, Color color, float rotationDeg = 0f, float feather = 1.2f)
        {
            float maxR = Mathf.Max(rx, ry) + feather;
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - maxR));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + maxR));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - maxR));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + maxR));

            float rad = rotationDeg * Mathf.Deg2Rad;
            float cos = Mathf.Cos(rad);
            float sin = Mathf.Sin(rad);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float rotX = dx * cos + dy * sin;
                    float rotY = -dx * sin + dy * cos;

                    float normDist = Mathf.Sqrt((rotX * rotX) / (rx * rx) + (rotY * rotY) / (ry * ry));
                    if (normDist <= 1f - (feather / Mathf.Min(rx, ry)))
                    {
                        BlendPixel(tex, x, y, color);
                    }
                    else if (normDist <= 1f + (feather / Mathf.Min(rx, ry)))
                    {
                        float alpha = Mathf.Clamp01((1f + (feather / Mathf.Min(rx, ry)) - normDist) / (2f * feather / Mathf.Min(rx, ry))) * color.a;
                        BlendPixel(tex, x, y, new Color(color.r, color.g, color.b, alpha));
                    }
                }
            }
        }

        private static void DrawRadialGradient(Texture2D tex, float cx, float cy, float radius, Color innerCol, Color outerCol)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(cx - radius));
            int maxX = Mathf.Min(tex.width - 1, Mathf.CeilToInt(cx + radius));
            int minY = Mathf.Max(0, Mathf.FloorToInt(cy - radius));
            int maxY = Mathf.Min(tex.height - 1, Mathf.CeilToInt(cy + radius));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                    if (dist <= radius)
                    {
                        float t = dist / radius;
                        Color col = Color.Lerp(innerCol, outerCol, t);
                        BlendPixel(tex, x, y, col);
                    }
                }
            }
        }

        // =========================================================================
        // HEAD DRAWING SUITE (10 Cute Heads)
        // =========================================================================
        private static void DrawKawaiiEyes(Texture2D tex, float cx, float cy, float eyeSpacing, float eyeSize, Color blushColor, Color eyeGlowColor)
        {
            float leftEyeX = cx - eyeSpacing;
            float rightEyeX = cx + eyeSpacing;
            float eyeY = cy + 6f;

            // Blush cheeks
            DrawFilledEllipse(tex, leftEyeX - 12f, eyeY - 26f, 18f, 11f, blushColor);
            DrawFilledEllipse(tex, rightEyeX + 12f, eyeY - 26f, 18f, 11f, blushColor);

            // Eye outer soft shadow/glow
            DrawFilledCircle(tex, leftEyeX, eyeY, eyeSize + 2f, new Color(0f, 0f, 0f, 0.45f));
            DrawFilledCircle(tex, rightEyeX, eyeY, eyeSize + 2f, new Color(0f, 0f, 0f, 0.45f));

            // Eye Main Dark Iris
            DrawFilledCircle(tex, leftEyeX, eyeY, eyeSize, new Color(0.06f, 0.07f, 0.12f, 0.98f));
            DrawFilledCircle(tex, rightEyeX, eyeY, eyeSize, new Color(0.06f, 0.07f, 0.12f, 0.98f));

            // Eye Inner Color Rim / Glow
            DrawRing(tex, leftEyeX, eyeY - 3f, eyeSize * 0.45f, eyeSize * 0.85f, new Color(eyeGlowColor.r, eyeGlowColor.g, eyeGlowColor.b, 0.5f));
            DrawRing(tex, rightEyeX, eyeY - 3f, eyeSize * 0.45f, eyeSize * 0.85f, new Color(eyeGlowColor.r, eyeGlowColor.g, eyeGlowColor.b, 0.5f));

            // Primary Sparkle Highlight (Top Right)
            DrawFilledCircle(tex, leftEyeX + eyeSize * 0.35f, eyeY + eyeSize * 0.35f, eyeSize * 0.38f, Color.white);
            DrawFilledCircle(tex, rightEyeX + eyeSize * 0.35f, eyeY + eyeSize * 0.35f, eyeSize * 0.38f, Color.white);

            // Secondary Sparkle Highlight (Bottom Left)
            DrawFilledCircle(tex, leftEyeX - eyeSize * 0.32f, eyeY - eyeSize * 0.30f, eyeSize * 0.18f, new Color(1f, 1f, 1f, 0.85f));
            DrawFilledCircle(tex, rightEyeX - eyeSize * 0.32f, eyeY - eyeSize * 0.30f, eyeSize * 0.18f, new Color(1f, 1f, 1f, 0.85f));

            // Cute gentle smile
            DrawRing(tex, cx, eyeY - 26f, 9f, 12f, new Color(0.08f, 0.08f, 0.15f, 0.75f));
        }

        // 1. Player (Giga Grub: Emerald Mint + Curled Antennae)
        private static Texture2D CreateHead_Player(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            // Outer Bioluminescent Aura Glow
            DrawRadialGradient(tex, cx, cy, r + 18f, new Color(0.15f, 0.95f, 0.75f, 0.45f), Color.clear);

            // Antennae stalks & glowing bulbs
            DrawFilledEllipse(tex, cx - 44f, cy + 85f, 9f, 32f, new Color(0.08f, 0.65f, 0.45f), -20f);
            DrawFilledEllipse(tex, cx + 44f, cy + 85f, 9f, 32f, new Color(0.08f, 0.65f, 0.45f), 20f);
            DrawRadialGradient(tex, cx - 58f, cy + 115f, 22f, new Color(0.35f, 1f, 0.90f, 0.9f), Color.clear);
            DrawFilledCircle(tex, cx - 58f, cy + 115f, 14f, new Color(0.40f, 1f, 0.88f));
            DrawRadialGradient(tex, cx + 58f, cy + 115f, 22f, new Color(0.35f, 1f, 0.90f, 0.9f), Color.clear);
            DrawFilledCircle(tex, cx + 58f, cy + 115f, 14f, new Color(0.40f, 1f, 0.88f));

            // Head Spherical Body
            DrawRadialGradient(tex, cx, cy, r, new Color(0.45f, 1f, 0.82f), new Color(0.06f, 0.72f, 0.50f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(0.75f, 1f, 0.92f, 0.85f));

            // Kawaii Eyes & Blush
            DrawKawaiiEyes(tex, cx, cy, 38f, 24f, new Color(1f, 0.35f, 0.60f, 0.55f), new Color(0.15f, 0.95f, 0.85f));
            tex.Apply();
            return tex;
        }

        // 2. Bot Sprout (Leaf Lime + Leaf Horn)
        private static Texture2D CreateHead_Sprout(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 16f, new Color(0.55f, 0.98f, 0.20f, 0.4f), Color.clear);

            // Leaf horn atop head
            DrawFilledEllipse(tex, cx - 18f, cy + 96f, 12f, 30f, new Color(0.35f, 0.85f, 0.08f), -35f);
            DrawFilledEllipse(tex, cx + 18f, cy + 96f, 12f, 30f, new Color(0.55f, 0.98f, 0.15f), 35f);
            DrawFilledCircle(tex, cx, cy + 78f, 10f, new Color(0.75f, 1f, 0.35f));

            // Head Body
            DrawRadialGradient(tex, cx, cy, r, new Color(0.70f, 1f, 0.25f), new Color(0.28f, 0.68f, 0.05f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(0.85f, 1f, 0.55f, 0.8f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 24f, new Color(1f, 0.45f, 0.55f, 0.5f), new Color(0.65f, 1f, 0.25f));
            tex.Apply();
            return tex;
        }

        // 3. Bot Spark (Electric Cyan + Lightning Antennas)
        private static Texture2D CreateHead_Spark(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 18f, new Color(0.15f, 0.88f, 1f, 0.45f), Color.clear);

            // Lightning horns
            DrawFilledEllipse(tex, cx - 45f, cy + 85f, 10f, 28f, new Color(0.05f, 0.65f, 0.95f), -28f);
            DrawFilledEllipse(tex, cx + 45f, cy + 85f, 10f, 28f, new Color(0.05f, 0.65f, 0.95f), 28f);
            DrawFilledEllipse(tex, cx - 58f, cy + 110f, 8f, 20f, new Color(0.65f, 0.95f, 1f), 35f);
            DrawFilledEllipse(tex, cx + 58f, cy + 110f, 8f, 20f, new Color(0.65f, 0.95f, 1f), -35f);

            // Head Body
            DrawRadialGradient(tex, cx, cy, r, new Color(0.45f, 0.92f, 1f), new Color(0.04f, 0.52f, 0.85f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(0.75f, 0.98f, 1f, 0.9f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 25f, new Color(0.15f, 0.85f, 1f, 0.5f), new Color(0.25f, 0.95f, 1f));
            tex.Apply();
            return tex;
        }

        // 4. Bot Ruby (Crimson Coral + Heart Horns)
        private static Texture2D CreateHead_Ruby(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 18f, new Color(1f, 0.20f, 0.40f, 0.4f), Color.clear);

            // Heart Horns
            DrawFilledCircle(tex, cx - 48f, cy + 96f, 16f, new Color(1f, 0.35f, 0.50f));
            DrawFilledCircle(tex, cx - 30f, cy + 96f, 16f, new Color(1f, 0.35f, 0.50f));
            DrawFilledCircle(tex, cx + 30f, cy + 96f, 16f, new Color(1f, 0.35f, 0.50f));
            DrawFilledCircle(tex, cx + 48f, cy + 96f, 16f, new Color(1f, 0.35f, 0.50f));

            DrawRadialGradient(tex, cx, cy, r, new Color(1f, 0.45f, 0.58f), new Color(0.75f, 0.08f, 0.22f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(1f, 0.78f, 0.85f, 0.85f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 24f, new Color(1f, 0.15f, 0.35f, 0.6f), new Color(1f, 0.45f, 0.65f));
            tex.Apply();
            return tex;
        }

        // 5. Bot Sunny (Radiant Gold + Star Crown)
        private static Texture2D CreateHead_Sunny(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 20f, new Color(1f, 0.85f, 0.15f, 0.45f), Color.clear);

            // Star Crown Tuft
            DrawFilledEllipse(tex, cx - 35f, cy + 90f, 10f, 28f, new Color(1f, 0.75f, 0.05f), -30f);
            DrawFilledEllipse(tex, cx + 35f, cy + 90f, 10f, 28f, new Color(1f, 0.75f, 0.05f), 30f);
            DrawFilledEllipse(tex, cx, cy + 98f, 12f, 32f, new Color(1f, 0.95f, 0.25f), 0f);

            DrawRadialGradient(tex, cx, cy, r, new Color(1f, 0.92f, 0.35f), new Color(0.85f, 0.52f, 0.02f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(1f, 0.98f, 0.75f, 0.9f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 24f, new Color(1f, 0.55f, 0.15f, 0.55f), new Color(1f, 0.85f, 0.15f));
            tex.Apply();
            return tex;
        }

        // 6. Bot Violet (Astral Orchid + Teardrop Antenna)
        private static Texture2D CreateHead_Violet(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 18f, new Color(0.85f, 0.25f, 1f, 0.45f), Color.clear);

            // Teardrop central antenna
            DrawFilledEllipse(tex, cx, cy + 85f, 10f, 30f, new Color(0.65f, 0.10f, 0.85f), 0f);
            DrawFilledCircle(tex, cx, cy + 115f, 16f, new Color(0.95f, 0.45f, 1f));

            DrawRadialGradient(tex, cx, cy, r, new Color(0.92f, 0.48f, 1f), new Color(0.52f, 0.05f, 0.75f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(0.98f, 0.78f, 1f, 0.85f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 24f, new Color(0.95f, 0.25f, 0.85f, 0.55f), new Color(0.85f, 0.45f, 1f));
            tex.Apply();
            return tex;
        }

        // 7. Bot Bubble (Bubblegum Pink + Spherical Bobbles)
        private static Texture2D CreateHead_Bubble(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 18f, new Color(1f, 0.35f, 0.75f, 0.4f), Color.clear);

            // Spherical twin bobbles
            DrawFilledEllipse(tex, cx - 45f, cy + 85f, 8f, 28f, new Color(0.85f, 0.15f, 0.55f), -25f);
            DrawFilledEllipse(tex, cx + 45f, cy + 85f, 8f, 28f, new Color(0.85f, 0.15f, 0.55f), 25f);
            DrawFilledCircle(tex, cx - 60f, cy + 110f, 16f, new Color(1f, 0.65f, 0.88f));
            DrawFilledCircle(tex, cx + 60f, cy + 110f, 16f, new Color(1f, 0.65f, 0.88f));

            DrawRadialGradient(tex, cx, cy, r, new Color(1f, 0.58f, 0.82f), new Color(0.82f, 0.15f, 0.50f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(1f, 0.88f, 0.95f, 0.85f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 25f, new Color(1f, 0.25f, 0.55f, 0.55f), new Color(1f, 0.55f, 0.85f));
            tex.Apply();
            return tex;
        }

        // 8. Bot Frost (Glacier Ice Blue + Crystal Shard)
        private static Texture2D CreateHead_Frost(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 18f, new Color(0.45f, 0.85f, 1f, 0.45f), Color.clear);

            // Crystal Horn
            DrawFilledEllipse(tex, cx, cy + 96f, 16f, 35f, new Color(0.75f, 0.95f, 1f), 0f);
            DrawFilledEllipse(tex, cx - 35f, cy + 85f, 10f, 25f, new Color(0.35f, 0.75f, 1f), -25f);
            DrawFilledEllipse(tex, cx + 35f, cy + 85f, 10f, 25f, new Color(0.35f, 0.75f, 1f), 25f);

            DrawRadialGradient(tex, cx, cy, r, new Color(0.68f, 0.92f, 1f), new Color(0.18f, 0.52f, 0.85f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(0.90f, 0.98f, 1f, 0.9f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 24f, new Color(0.25f, 0.75f, 1f, 0.5f), new Color(0.55f, 0.95f, 1f));
            tex.Apply();
            return tex;
        }

        // 9. Bot Flame (Tangerine Amber + Ember Tuft)
        private static Texture2D CreateHead_Flame(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 18f, new Color(1f, 0.55f, 0.12f, 0.45f), Color.clear);

            // Flame Tuft Crest
            DrawFilledEllipse(tex, cx - 25f, cy + 90f, 12f, 32f, new Color(1f, 0.42f, 0.05f), -25f);
            DrawFilledEllipse(tex, cx + 25f, cy + 90f, 12f, 32f, new Color(1f, 0.42f, 0.05f), 25f);
            DrawFilledEllipse(tex, cx, cy + 102f, 14f, 36f, new Color(1f, 0.85f, 0.25f), 0f);

            DrawRadialGradient(tex, cx, cy, r, new Color(1f, 0.75f, 0.28f), new Color(0.85f, 0.28f, 0.02f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(1f, 0.92f, 0.65f, 0.85f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 24f, new Color(1f, 0.35f, 0.15f, 0.55f), new Color(1f, 0.65f, 0.15f));
            tex.Apply();
            return tex;
        }

        // 10. Bot Shadow (Midnight Indigo + Crescent Moon)
        private static Texture2D CreateHead_Shadow(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float cx = s * 0.5f;
            float cy = s * 0.46f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, cx, cy, r + 18f, new Color(0.65f, 0.35f, 1f, 0.45f), Color.clear);

            // Crescent Moon Antenna
            DrawFilledCircle(tex, cx, cy + 100f, 26f, new Color(0.85f, 0.65f, 1f));
            DrawFilledCircle(tex, cx + 8f, cy + 104f, 22f, Color.clear);

            DrawRadialGradient(tex, cx, cy, r, new Color(0.68f, 0.48f, 1f), new Color(0.22f, 0.08f, 0.62f));
            DrawRing(tex, cx, cy, r - 6f, r, new Color(0.88f, 0.78f, 1f, 0.85f));

            DrawKawaiiEyes(tex, cx, cy, 38f, 24f, new Color(0.75f, 0.35f, 1f, 0.55f), new Color(0.45f, 0.85f, 1f));
            tex.Apply();
            return tex;
        }

        // =========================================================================
        // BODY SEGMENT DRAWING ROUTINES (10 Matching Segment Textures)
        // =========================================================================
        private static Texture2D CreateSegment_Ringed(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawRing(tex, c, c, r * 0.55f, r * 0.75f, new Color(rim.r, rim.g, rim.b, 0.45f));
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Organic(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            // Leaf vein motif
            DrawFilledEllipse(tex, c, c, r * 0.35f, r * 0.70f, new Color(rim.r, rim.g, rim.b, 0.55f), 0f);
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Tech(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawRing(tex, c, c, r * 0.35f, r * 0.50f, rim);
            DrawRing(tex, c, c, r * 0.65f, r * 0.75f, new Color(rim.r, rim.g, rim.b, 0.5f));
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Heart(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawFilledCircle(tex, c - 18f, c + 12f, 22f, new Color(rim.r, rim.g, rim.b, 0.5f));
            DrawFilledCircle(tex, c + 18f, c + 12f, 22f, new Color(rim.r, rim.g, rim.b, 0.5f));
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Sun(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawFilledCircle(tex, c, c, r * 0.45f, new Color(rim.r, rim.g, rim.b, 0.7f));
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Swirl(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawRing(tex, c, c, r * 0.40f, r * 0.55f, new Color(rim.r, rim.g, rim.b, 0.6f));
            DrawRing(tex, c, c, r * 0.70f, r * 0.82f, new Color(rim.r, rim.g, rim.b, 0.4f));
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Bubble(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawFilledCircle(tex, c - 20f, c + 15f, 18f, new Color(rim.r, rim.g, rim.b, 0.6f));
            DrawFilledCircle(tex, c + 22f, c - 12f, 14f, new Color(rim.r, rim.g, rim.b, 0.45f));
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Crystal(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawFilledEllipse(tex, c, c, r * 0.65f, r * 0.65f, new Color(rim.r, rim.g, rim.b, 0.65f), 45f);
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Flame(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawFilledEllipse(tex, c, c, r * 0.35f, r * 0.60f, new Color(rim.r, rim.g, rim.b, 0.7f), 0f);
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegment_Cosmic(int s, Color bright, Color dark, Color rim)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(bright.r, bright.g, bright.b, 0.35f), Color.clear);
            DrawRadialGradient(tex, c, c, r, bright, dark);
            DrawFilledCircle(tex, c, c, 12f, Color.white);
            DrawRing(tex, c, c, r * 0.45f, r * 0.65f, new Color(rim.r, rim.g, rim.b, 0.5f));
            DrawRing(tex, c, c, r - 5f, r, rim);
            tex.Apply();
            return tex;
        }

        // =========================================================================
        // FOOD SPRITES (Star Berry, Jelly Drop, Astral Core, Spore Pod)
        // =========================================================================
        private static Texture2D CreateFood_StarBerry(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.38f;

            // Outer Glow
            DrawRadialGradient(tex, c, c, r + 12f, new Color(0.20f, 1f, 0.40f, 0.5f), Color.clear);
            // Berry Body
            DrawRadialGradient(tex, c, c, r, new Color(0.65f, 1f, 0.35f), new Color(0.08f, 0.75f, 0.25f));
            // Star Core
            DrawFilledEllipse(tex, c, c, 6f, 22f, Color.white, 0f);
            DrawFilledEllipse(tex, c, c, 6f, 22f, Color.white, 90f);
            DrawFilledCircle(tex, c, c, 9f, Color.white);
            // Gloss Sparkle
            DrawFilledCircle(tex, c + 14f, c + 14f, 7f, Color.white);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateFood_JellyDrop(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.38f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(1f, 0.85f, 0.15f, 0.5f), Color.clear);
            DrawRadialGradient(tex, c, c, r, new Color(1f, 0.95f, 0.45f), new Color(0.92f, 0.55f, 0.05f));
            DrawFilledCircle(tex, c - 10f, c + 10f, 12f, new Color(1f, 1f, 1f, 0.7f));
            DrawFilledCircle(tex, c + 12f, c - 10f, 8f, new Color(1f, 1f, 1f, 0.5f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateFood_AstralCore(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.38f;

            DrawRadialGradient(tex, c, c, r + 14f, new Color(0.95f, 0.25f, 1f, 0.55f), Color.clear);
            DrawRadialGradient(tex, c, c, r, new Color(1f, 0.65f, 1f), new Color(0.65f, 0.05f, 0.85f));
            // Crystal diamond facets
            DrawFilledEllipse(tex, c, c, 8f, 28f, Color.white, 45f);
            DrawFilledEllipse(tex, c, c, 8f, 28f, Color.white, -45f);
            DrawFilledCircle(tex, c, c, 11f, Color.white);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateFood_SporePod(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.38f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(0.15f, 0.95f, 1f, 0.5f), Color.clear);
            DrawRadialGradient(tex, c, c, r, new Color(0.65f, 1f, 1f), new Color(0.05f, 0.65f, 0.90f));
            DrawFilledCircle(tex, c + 10f, c + 10f, 8f, Color.white);
            tex.Apply();
            return tex;
        }

        // =========================================================================
        // POWER-UP BADGES
        // =========================================================================
        private static Texture2D CreatePowerUp_Speed(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(0.15f, 0.85f, 1f, 0.5f), Color.clear);
            DrawRadialGradient(tex, c, c, r, new Color(0.20f, 0.90f, 1f), new Color(0.04f, 0.45f, 0.75f));
            // Wing speed streak
            DrawFilledEllipse(tex, c, c, 8f, 32f, Color.white, 45f);
            DrawFilledEllipse(tex, c + 12f, c - 12f, 6f, 22f, Color.white, 45f);
            DrawFilledCircle(tex, c - 12f, c + 12f, 9f, Color.white);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreatePowerUp_Magnet(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(0.95f, 0.20f, 0.85f, 0.5f), Color.clear);
            DrawRadialGradient(tex, c, c, r, new Color(1f, 0.45f, 0.90f), new Color(0.65f, 0.05f, 0.60f));
            // Magnetic U-curve
            DrawRing(tex, c, c - 4f, 14f, 26f, Color.white);
            DrawFilledCircle(tex, c - 20f, c + 16f, 8f, new Color(0.40f, 0.85f, 1f));
            DrawFilledCircle(tex, c + 20f, c + 16f, 8f, new Color(1f, 0.35f, 0.45f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreatePowerUp_Multiplier(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.40f;

            DrawRadialGradient(tex, c, c, r + 12f, new Color(1f, 0.85f, 0.15f, 0.5f), Color.clear);
            DrawRadialGradient(tex, c, c, r, new Color(1f, 0.95f, 0.35f), new Color(0.85f, 0.55f, 0.02f));
            // Star Double Crest
            DrawFilledEllipse(tex, c, c, 8f, 30f, Color.white, 0f);
            DrawFilledEllipse(tex, c, c, 8f, 30f, Color.white, 90f);
            DrawFilledCircle(tex, c, c, 12f, Color.white);
            tex.Apply();
            return tex;
        }

        // =========================================================================
        // ARENA & UI ASSETS
        // =========================================================================
        private static Texture2D CreateArena_HexTile(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            Color bg = new Color(0.04f, 0.07f, 0.12f, 1f);
            Color grid = new Color(0.12f, 0.28f, 0.45f, 0.35f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    tex.SetPixel(x, y, bg);
                }
            }

            // Grid border lines
            for (int i = 0; i < s; i++)
            {
                tex.SetPixel(i, 0, grid);
                tex.SetPixel(i, s - 1, grid);
                tex.SetPixel(0, i, grid);
                tex.SetPixel(s - 1, i, grid);
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateArena_WallGlow(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float distY = Mathf.Abs(y - c);
                    float alpha = Mathf.Clamp01(1f - (distY / c));
                    tex.SetPixel(x, y, new Color(0.15f, 0.85f, 1f, alpha * 0.85f));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateArena_SporeDecor(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            DrawRadialGradient(tex, c, c, s * 0.45f, new Color(0.25f, 1f, 0.85f, 0.65f), Color.clear);
            DrawFilledCircle(tex, c, c, s * 0.22f, new Color(0.55f, 1f, 0.90f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateUI_Logo(int w, int h)
        {
            Texture2D tex = CreateEmptyTexture(w, h);
            float cx = w * 0.5f;
            float cy = h * 0.5f;

            // Background glow banner
            DrawRadialGradient(tex, cx, cy, h * 0.7f, new Color(0.12f, 0.85f, 0.70f, 0.5f), Color.clear);
            // Grub crest head on left
            DrawFilledCircle(tex, 70f, cy, 45f, new Color(0.20f, 0.95f, 0.75f));
            DrawKawaiiEyes(tex, 70f, cy, 14f, 10f, new Color(1f, 0.35f, 0.60f, 0.6f), new Color(0.40f, 1f, 0.90f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateUI_JoystickBase(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.45f;
            DrawRadialGradient(tex, c, c, r, new Color(0.08f, 0.16f, 0.26f, 0.65f), new Color(0.02f, 0.05f, 0.10f, 0.85f));
            DrawRing(tex, c, c, r - 4f, r, new Color(0.25f, 0.85f, 1f, 0.55f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateUI_JoystickKnob(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            float c = s * 0.5f;
            float r = s * 0.42f;
            DrawRadialGradient(tex, c, c, r + 6f, new Color(0.20f, 0.90f, 1f, 0.4f), Color.clear);
            DrawRadialGradient(tex, c, c, r, new Color(0.35f, 0.95f, 1f), new Color(0.08f, 0.55f, 0.85f));
            DrawFilledCircle(tex, c, c, r * 0.45f, new Color(1f, 1f, 1f, 0.7f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateUI_Card(int s)
        {
            Texture2D tex = CreateEmptyTexture(s, s);
            Color bg = new Color(0.06f, 0.10f, 0.16f, 0.92f);
            Color border = new Color(0.25f, 0.85f, 1f, 0.45f);

            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    bool isBorder = (x < 2 || x >= s - 2 || y < 2 || y >= s - 2);
                    tex.SetPixel(x, y, isBorder ? border : bg);
                }
            }
            tex.Apply();
            return tex;
        }

        private static void SaveTexture(string path, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }

        public static void ConfigureAllTextureImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { ArtRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
