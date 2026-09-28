using System;
using UnityEngine;
using GigaGrub.Data;
using GigaGrub.Player;
using GigaGrub.Systems;

namespace GigaGrub.Cosmetics
{
    public class CosmeticManager : MonoBehaviour
    {
        public static CosmeticManager Instance { get; private set; }

        private CosmeticInventory inventory;
        private bool isInitialized = false;

        public CosmeticInventory Inventory
        {
            get
            {
                if (inventory == null)
                {
                    inventory = new CosmeticInventory();
                }
                return inventory;
            }
        }

        public bool IsInitialized => isInitialized;

        public static void SetInstanceForTest(CosmeticManager manager)
        {
            Instance = manager;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            Initialize();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Initialize()
        {
            if (inventory == null)
            {
                inventory = new CosmeticInventory();
            }

            LoadFromSave();
            isInitialized = true;
        }

        public void LoadFromSave()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Statistics != null)
            {
                SaveData snapshot = SaveManager.Instance.Statistics.GetDataSnapshot();
                inventory.InitializeFromSave(snapshot.Coins, snapshot.UnlockedCosmetics, snapshot.EquippedCosmetics);
            }
            else
            {
                inventory.InitializeFromSave(1000, new System.Collections.Generic.List<string>(), EquippedCosmetics.CreateDefault());
            }
        }

        public void SaveToSaveManager()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.Statistics != null)
            {
                SaveManager.Instance.Statistics.SetCosmetics(
                    inventory.Coins,
                    inventory.GetUnlockedList(),
                    inventory.Equipped
                );
                SaveManager.Instance.Save();
            }
        }

        public void ApplyToPlayer(PlayerBody playerBody)
        {
            if (playerBody == null) return;

            CreatureCosmeticController cosmeticCtrl = playerBody.GetComponent<CreatureCosmeticController>();
            if (cosmeticCtrl == null)
            {
                cosmeticCtrl = playerBody.gameObject.AddComponent<CreatureCosmeticController>();
            }

            cosmeticCtrl.ApplyEquippedCosmetics(Inventory.Equipped);
        }

        public void ApplyRandomCosmeticsToAI(PlayerBody aiBody)
        {
            if (aiBody == null) return;

            CosmeticDatabase db = CosmeticDatabase.Instance;
            if (db == null) return;

            CreatureCosmeticController cosmeticCtrl = aiBody.GetComponent<CreatureCosmeticController>();
            if (cosmeticCtrl == null)
            {
                cosmeticCtrl = aiBody.gameObject.AddComponent<CreatureCosmeticController>();
            }

            var creatures = db.Creatures;
            var colors = db.SkinColors;
            var patterns = db.Patterns;
            var clothing = db.Clothing;
            var hats = db.Hats;
            var eyes = db.Eyes;
            var mouths = db.Mouths;
            var accessories = db.Accessories;

            EquippedCosmetics aiConfig = new EquippedCosmetics
            {
                CreatureId = creatures != null && creatures.Count > 0 ? creatures[UnityEngine.Random.Range(0, creatures.Count)].Id : "creature_01_grub",
                ColorId = colors != null && colors.Count > 0 ? colors[UnityEngine.Random.Range(0, colors.Count)].Id : "color_01_emerald",
                PatternId = patterns != null && patterns.Count > 0 ? patterns[UnityEngine.Random.Range(0, patterns.Count)].Id : "pattern_01_solid",
                ClothingId = (clothing != null && clothing.Count > 0 && UnityEngine.Random.value > 0.4f) ? clothing[UnityEngine.Random.Range(0, clothing.Count)].Id : "",
                HatId = (hats != null && hats.Count > 0 && UnityEngine.Random.value > 0.3f) ? hats[UnityEngine.Random.Range(0, hats.Count)].Id : "",
                EyesId = eyes != null && eyes.Count > 0 ? eyes[UnityEngine.Random.Range(0, eyes.Count)].Id : "eyes_01_normal",
                MouthId = mouths != null && mouths.Count > 0 ? mouths[UnityEngine.Random.Range(0, mouths.Count)].Id : "mouth_01_smile",
                BackAccessoryId = (accessories != null && accessories.Count > 0 && UnityEngine.Random.value > 0.6f) ? accessories[UnityEngine.Random.Range(0, accessories.Count)].Id : "",
                NeckAccessoryId = "",
                TailAccessoryId = "",
                EffectId = "",
                UseCustomColors = false
            };

            cosmeticCtrl.ApplyEquippedCosmetics(aiConfig);
        }
    }
}
