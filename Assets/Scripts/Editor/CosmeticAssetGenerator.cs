using System.IO;
using UnityEngine;
using UnityEditor;

namespace GigaGrub.Editor
{
    public static class CosmeticAssetGenerator
    {
        private const string ArtCosmeticsPath = "Assets/Art/Cosmetics";

        public static void GenerateAllCosmeticSprites()
        {
            EnsureCosmeticDirectories();

            GenerateCreatureSprites();
            GeneratePatternSprites();
            GenerateClothingSprites();
            GenerateHatSprites();
            GenerateFaceSprites();
            GenerateAccessorySprites();
            GenerateEffectSprites();

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporters();
        }

        public static void EnsureCosmeticDirectories()
        {
            EnsureFolder("Assets", "Art");
            EnsureFolder("Assets/Art", "Cosmetics");
            EnsureFolder(ArtCosmeticsPath, "Creatures");
            EnsureFolder(ArtCosmeticsPath, "Patterns");
            EnsureFolder(ArtCosmeticsPath, "Clothing");
            EnsureFolder(ArtCosmeticsPath, "Hats");
            EnsureFolder(ArtCosmeticsPath, "Eyes");
            EnsureFolder(ArtCosmeticsPath, "Mouths");
            EnsureFolder(ArtCosmeticsPath, "Accessories");
            EnsureFolder(ArtCosmeticsPath, "Effects");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string full = $"{parent}/{name}";
            if (!AssetDatabase.IsValidFolder(full))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static void SaveTexture(string path, Texture2D tex)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            Object.DestroyImmediate(tex);
        }

        // =========================================================================
        // 1. CREATURE TYPE SPRITES (6 Unique Creatures)
        // =========================================================================
        private static void GenerateCreatureSprites()
        {
            const int size = 256;
            string p = $"{ArtCosmeticsPath}/Creatures";

            // 1. Grub
            SaveTexture($"{p}/Creature_Grub_Head.png", CreateCircleCreature(size, new Color(0.12f, 0.95f, 0.72f), "Grub"));
            SaveTexture($"{p}/Creature_Grub_Seg.png", CreateSegmentWithRings(size, new Color(0.12f, 0.95f, 0.72f), new Color(0.04f, 0.65f, 0.45f)));

            // 2. Slime
            SaveTexture($"{p}/Creature_Slime_Head.png", CreateBlobCreature(size, new Color(0.2f, 0.85f, 1f)));
            SaveTexture($"{p}/Creature_Slime_Seg.png", CreateBlobSegment(size, new Color(0.2f, 0.85f, 1f)));

            // 3. Dragon
            SaveTexture($"{p}/Creature_Dragon_Head.png", CreateDragonHead(size, new Color(1f, 0.35f, 0.15f)));
            SaveTexture($"{p}/Creature_Dragon_Seg.png", CreateScaleSegment(size, new Color(1f, 0.35f, 0.15f), new Color(0.7f, 0.15f, 0.05f)));

            // 4. Alien
            SaveTexture($"{p}/Creature_Alien_Head.png", CreateAlienHead(size, new Color(0.85f, 0.25f, 0.95f)));
            SaveTexture($"{p}/Creature_Alien_Seg.png", CreateTechSegment(size, new Color(0.85f, 0.25f, 0.95f)));

            // 5. Lizard
            SaveTexture($"{p}/Creature_Lizard_Head.png", CreateLizardHead(size, new Color(0.4f, 0.9f, 0.2f)));
            SaveTexture($"{p}/Creature_Lizard_Seg.png", CreateStripedSegment(size, new Color(0.4f, 0.9f, 0.2f), new Color(0.2f, 0.6f, 0.05f)));

            // 6. Monster
            SaveTexture($"{p}/Creature_Monster_Head.png", CreateMonsterHead(size, new Color(0.95f, 0.2f, 0.4f)));
            SaveTexture($"{p}/Creature_Monster_Seg.png", CreateSpikedSegment(size, new Color(0.95f, 0.2f, 0.4f)));
        }

        // =========================================================================
        // 2. BODY PATTERNS (8 Patterns)
        // =========================================================================
        private static void GeneratePatternSprites()
        {
            const int size = 128;
            string p = $"{ArtCosmeticsPath}/Patterns";

            SaveTexture($"{p}/Pattern_01_Solid.png", CreateSolidPattern(size));
            SaveTexture($"{p}/Pattern_02_Stripes.png", CreateStripesPattern(size));
            SaveTexture($"{p}/Pattern_03_Spots.png", CreateSpotsPattern(size));
            SaveTexture($"{p}/Pattern_04_Gradient.png", CreateGradientPattern(size));
            SaveTexture($"{p}/Pattern_05_Checker.png", CreateCheckerPattern(size));
            SaveTexture($"{p}/Pattern_06_Glow.png", CreateGlowPattern(size));
            SaveTexture($"{p}/Pattern_07_Metallic.png", CreateMetallicPattern(size));
            SaveTexture($"{p}/Pattern_08_Neon.png", CreateNeonPattern(size));
        }

        // =========================================================================
        // 3. CLOTHING ITEMS (15 Items)
        // =========================================================================
        private static void GenerateClothingSprites()
        {
            const int size = 128;
            string p = $"{ArtCosmeticsPath}/Clothing";

            string[] names = new string[]
            {
                "TShirt", "Hoodie", "Jacket", "Suit", "Armor",
                "Jersey", "Pirate", "SpaceSuit", "Royal", "Winter",
                "Ninja", "LabCoat", "Robe", "CyberVest", "Tuxedo"
            };

            for (int i = 0; i < names.Length; i++)
            {
                Color clr = ColorFromIndex(i);
                SaveTexture($"{p}/Clothing_{i + 1:D2}_{names[i]}_Collar.png", CreateCollarSprite(size, clr));
                SaveTexture($"{p}/Clothing_{i + 1:D2}_{names[i]}_Seg.png", CreateClothingSegmentOverlay(size, clr, i));
            }
        }

        // =========================================================================
        // 4. HEAD ACCESSORIES / HATS (15 Hats)
        // =========================================================================
        private static void GenerateHatSprites()
        {
            const int size = 128;
            string p = $"{ArtCosmeticsPath}/Hats";

            string[] hats = new string[]
            {
                "Cap", "Crown", "Helmet", "Cowboy", "Wizard",
                "Party", "Headphones", "Sunglasses", "Halo", "Horns",
                "CatEars", "Antenna", "Viking", "Chef", "TopHat"
            };

            for (int i = 0; i < hats.Length; i++)
            {
                Color tint = ColorFromIndex(i + 2);
                SaveTexture($"{p}/Hat_{i + 1:D2}_{hats[i]}.png", CreateHatSprite(size, hats[i], tint));
            }
        }

        // =========================================================================
        // 5. FACE FEATURES (8 Eyes & 8 Mouths)
        // =========================================================================
        private static void GenerateFaceSprites()
        {
            const int size = 128;
            string pEyes = $"{ArtCosmeticsPath}/Eyes";
            string pMouths = $"{ArtCosmeticsPath}/Mouths";

            string[] eyeTypes = new string[] { "Normal", "Angry", "Happy", "Crazy", "Robot", "Sunglasses", "BigEyes", "CuteSparkle" };
            string[] mouthTypes = new string[] { "Smile", "Open", "Tongue", "Angry", "Vampire", "Grin", "Whistle", "Mustache" };

            for (int i = 0; i < eyeTypes.Length; i++)
            {
                SaveTexture($"{pEyes}/Eyes_{i + 1:D2}_{eyeTypes[i]}.png", CreateEyesSprite(size, eyeTypes[i]));
            }

            for (int i = 0; i < mouthTypes.Length; i++)
            {
                SaveTexture($"{pMouths}/Mouth_{i + 1:D2}_{mouthTypes[i]}.png", CreateMouthSprite(size, mouthTypes[i]));
            }
        }

        // =========================================================================
        // 6. ACCESSORIES (15 Items)
        // =========================================================================
        private static void GenerateAccessorySprites()
        {
            const int size = 128;
            string p = $"{ArtCosmeticsPath}/Accessories";

            string[] accs = new string[]
            {
                "Backpack", "Wings", "Cape", "Scarf", "Necklace",
                "Shield", "PetDrone", "FloatingStars", "TailSpikes", "TailRibbon",
                "FairyWings", "SpikedCollar", "Bowtie", "Jetpack", "TailFlame"
            };

            for (int i = 0; i < accs.Length; i++)
            {
                Color c = ColorFromIndex(i + 4);
                SaveTexture($"{p}/Accessory_{i + 1:D2}_{accs[i]}.png", CreateAccessorySprite(size, accs[i], c));
            }
        }

        // =========================================================================
        // 7. COSMETIC EFFECTS (10 Particles)
        // =========================================================================
        private static void GenerateEffectSprites()
        {
            const int size = 64;
            string p = $"{ArtCosmeticsPath}/Effects";

            string[] effs = new string[]
            {
                "FireTrail", "Lightning", "RainbowTrail", "Smoke", "Sparkles",
                "Hearts", "Stars", "NeonGlow", "ShadowAura", "BubbleStream"
            };

            for (int i = 0; i < effs.Length; i++)
            {
                Color c = ColorFromIndex(i);
                SaveTexture($"{p}/Effect_{i + 1:D2}_{effs[i]}.png", CreateEffectParticleSprite(size, effs[i], c));
            }
        }

        // Helper procedural texture builders
        private static Color ColorFromIndex(int idx)
        {
            Color[] colors = new Color[]
            {
                new Color(0.12f, 0.95f, 0.72f), // Mint
                new Color(0.95f, 0.35f, 0.2f),  // Coral
                new Color(0.2f, 0.75f, 1f),    // Cyan
                new Color(1f, 0.85f, 0.15f),   // Gold
                new Color(0.85f, 0.25f, 0.95f),// Violet
                new Color(0.95f, 0.2f, 0.45f), // Ruby
                new Color(0.35f, 0.95f, 0.2f), // Lime
                new Color(1f, 0.5f, 0.1f),     // Amber
                new Color(0.45f, 0.85f, 1f),   // Sky
                new Color(0.95f, 0.95f, 0.95f) // Silver
            };
            return colors[idx % colors.Length];
        }

        private static Texture2D CreateCircleCreature(int size, Color mainColor, string label)
        {
            Texture2D tex = NewTex(size);
            float rad = size * 0.44f;
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= rad)
                    {
                        float t = d / rad;
                        Color c = Color.Lerp(Color.white, mainColor, t * 0.85f);
                        if (d >= rad - 6f) c = Color.Lerp(c, Color.black, 0.5f);
                        tex.SetPixel(x, y, c);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBlobCreature(int size, Color c)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float angle = Mathf.Atan2(y - center.y, x - center.x);
                    float rad = size * 0.42f + Mathf.Sin(angle * 5f) * (size * 0.04f);
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= rad)
                    {
                        float t = d / rad;
                        Color pixel = Color.Lerp(Color.white, c, t * 0.8f);
                        tex.SetPixel(x, y, pixel);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateDragonHead(int size, Color c)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.48f);
            float rad = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    // Horns at top
                    bool isHorn = (y > size * 0.65f) && (Mathf.Abs(x - size * 0.5f) > size * 0.22f && Mathf.Abs(x - size * 0.5f) < size * 0.38f);
                    if (d <= rad || isHorn)
                    {
                        Color pixel = isHorn ? new Color(1f, 0.85f, 0.3f) : Color.Lerp(Color.white, c, d / rad);
                        tex.SetPixel(x, y, pixel);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateAlienHead(int size, Color c)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.52f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Tapered alien oval shape
                    float nx = (x - center.x) / (size * 0.38f);
                    float ny = (y - center.y) / (size * 0.44f);
                    if (ny < 0) nx *= (1f - ny * 0.3f);
                    if (nx * nx + ny * ny <= 1f)
                    {
                        tex.SetPixel(x, y, Color.Lerp(Color.white, c, (nx * nx + ny * ny)));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateLizardHead(int size, Color c)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= rad)
                    {
                        Color pixel = Color.Lerp(Color.white, c, d / rad);
                        // Scale ridge
                        if (Mathf.Abs(x - size * 0.5f) < size * 0.08f) pixel = Color.Lerp(pixel, Color.yellow, 0.6f);
                        tex.SetPixel(x, y, pixel);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMonsterHead(int size, Color c)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.43f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= rad)
                    {
                        tex.SetPixel(x, y, Color.Lerp(Color.white, c, d / rad));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSegmentWithRings(int size, Color c1, Color c2)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= rad)
                    {
                        Color col = (Mathf.Sin(d * 0.25f) > 0f) ? c1 : c2;
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateBlobSegment(int size, Color c)
        {
            return CreateBlobCreature(size, c);
        }

        private static Texture2D CreateScaleSegment(int size, Color c1, Color c2)
        {
            return CreateSegmentWithRings(size, c1, c2);
        }

        private static Texture2D CreateTechSegment(int size, Color c)
        {
            return CreateCircleCreature(size, c, "Tech");
        }

        private static Texture2D CreateStripedSegment(int size, Color c1, Color c2)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.42f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= rad)
                    {
                        Color col = (x % 16 < 8) ? c1 : c2;
                        tex.SetPixel(x, y, col);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSpikedSegment(int size, Color c)
        {
            return CreateCircleCreature(size, c, "Spike");
        }

        private static Texture2D CreateSolidPattern(int size)
        {
            Texture2D tex = NewTex(size);
            FillCircle(tex, size * 0.5f, size * 0.5f, size * 0.45f, Color.white);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateStripesPattern(int size)
        {
            Texture2D tex = NewTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.45f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), c) <= rad)
                    {
                        tex.SetPixel(x, y, (x % 16 < 8) ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateSpotsPattern(int size)
        {
            Texture2D tex = NewTex(size);
            FillCircle(tex, size * 0.5f, size * 0.5f, size * 0.45f, Color.white);
            FillCircle(tex, size * 0.35f, size * 0.4f, size * 0.12f, new Color(0.5f, 0.5f, 0.5f));
            FillCircle(tex, size * 0.65f, size * 0.6f, size * 0.14f, new Color(0.5f, 0.5f, 0.5f));
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGradientPattern(int size)
        {
            Texture2D tex = NewTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.45f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), c) <= rad)
                    {
                        tex.SetPixel(x, y, Color.Lerp(Color.white, new Color(0.3f, 0.3f, 0.3f), (float)y / size));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCheckerPattern(int size)
        {
            Texture2D tex = NewTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.45f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), c) <= rad)
                    {
                        bool chk = ((x / 12) + (y / 12)) % 2 == 0;
                        tex.SetPixel(x, y, chk ? Color.white : new Color(0.6f, 0.6f, 0.6f));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateGlowPattern(int size)
        {
            Texture2D tex = NewTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.45f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    if (d <= rad)
                    {
                        float t = 1f - (d / rad);
                        tex.SetPixel(x, y, Color.Lerp(new Color(0.4f, 0.4f, 0.4f), Color.white, t * t));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMetallicPattern(int size)
        {
            Texture2D tex = NewTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.45f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), c) <= rad)
                    {
                        float spec = Mathf.Abs(Mathf.Sin((x + y) * 0.08f));
                        tex.SetPixel(x, y, Color.Lerp(new Color(0.5f, 0.55f, 0.6f), Color.white, spec));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateNeonPattern(int size)
        {
            Texture2D tex = NewTex(size);
            Vector2 c = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.45f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    if (d <= rad)
                    {
                        bool ring = Mathf.Abs(d - rad * 0.6f) < 4f;
                        tex.SetPixel(x, y, ring ? Color.cyan : Color.white);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateCollarSprite(int size, Color c)
        {
            Texture2D tex = NewTex(size);
            // Draw a neat curved shirt collar
            for (int y = (int)(size * 0.1f); y < (int)(size * 0.35f); y++)
            {
                for (int x = (int)(size * 0.25f); x < (int)(size * 0.75f); x++)
                {
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateClothingSegmentOverlay(int size, Color c, int variant)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.43f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= rad)
                    {
                        // Outer clothing vest / armor belt
                        if (y > size * 0.25f && y < size * 0.75f)
                        {
                            tex.SetPixel(x, y, c);
                        }
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateHatSprite(int size, string hatName, Color tint)
        {
            Texture2D tex = NewTex(size);
            // Draw stylized hat graphic centered
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = (int)(size * 0.3f); y < (int)(size * 0.8f); y++)
            {
                for (int x = (int)(size * 0.2f); x < (int)(size * 0.8f); x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d < size * 0.35f)
                    {
                        tex.SetPixel(x, y, tint);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateEyesSprite(int size, string eyeType)
        {
            Texture2D tex = NewTex(size);
            // Draw pair of cute eyes
            FillCircle(tex, size * 0.32f, size * 0.55f, size * 0.16f, Color.black);
            FillCircle(tex, size * 0.68f, size * 0.55f, size * 0.16f, Color.black);
            FillCircle(tex, size * 0.36f, size * 0.60f, size * 0.06f, Color.white);
            FillCircle(tex, size * 0.72f, size * 0.60f, size * 0.06f, Color.white);
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateMouthSprite(int size, string mouthType)
        {
            Texture2D tex = NewTex(size);
            // Draw cute smiling arc mouth
            Vector2 c = new Vector2(size * 0.5f, size * 0.45f);
            for (int y = (int)(size * 0.25f); y < (int)(size * 0.55f); y++)
            {
                for (int x = (int)(size * 0.3f); x < (int)(size * 0.7f); x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), c);
                    if (Mathf.Abs(d - size * 0.15f) < 4f && y < size * 0.45f)
                    {
                        tex.SetPixel(x, y, new Color(0.2f, 0.1f, 0.1f, 1f));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateAccessorySprite(int size, string accName, Color tint)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            for (int y = (int)(size * 0.2f); y < (int)(size * 0.8f); y++)
            {
                for (int x = (int)(size * 0.2f); x < (int)(size * 0.8f); x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), center) < size * 0.32f)
                    {
                        tex.SetPixel(x, y, tint);
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D CreateEffectParticleSprite(int size, string effName, Color c)
        {
            Texture2D tex = NewTex(size);
            Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
            float rad = size * 0.45f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), center);
                    if (d <= rad)
                    {
                        float alpha = Mathf.Clamp01(1f - (d / rad));
                        tex.SetPixel(x, y, new Color(c.r, c.g, c.b, alpha));
                    }
                }
            }
            tex.Apply();
            return tex;
        }

        private static void FillCircle(Texture2D tex, float cx, float cy, float r, Color c)
        {
            int minX = Mathf.Max(0, (int)(cx - r));
            int maxX = Mathf.Min(tex.width, (int)(cx + r + 1));
            int minY = Mathf.Max(0, (int)(cy - r));
            int maxY = Mathf.Min(tex.height, (int)(cy + r + 1));

            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    if (Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy)) <= r)
                    {
                        tex.SetPixel(x, y, c);
                    }
                }
            }
        }

        private static Texture2D NewTex(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] clear = new Color[size * size];
            for (int i = 0; i < clear.Length; i++) clear[i] = Color.clear;
            tex.SetPixels(clear);
            return tex;
        }

        public static void ConfigureTextureImporters()
        {
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new string[] { ArtCosmeticsPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
            }
        }
    }
}
