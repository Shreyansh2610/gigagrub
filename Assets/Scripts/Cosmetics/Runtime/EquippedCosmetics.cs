using System;
using UnityEngine;

namespace GigaGrub.Cosmetics
{
    [Serializable]
    public class EquippedCosmetics
    {
        [SerializeField] public string CreatureId = "creature_01_grub";
        [SerializeField] public string ColorId = "color_01_emerald";
        [SerializeField] public string PatternId = "pattern_01_solid";
        [SerializeField] public string ClothingId = "";
        [SerializeField] public string HatId = "";
        [SerializeField] public string EyesId = "eyes_01_normal";
        [SerializeField] public string MouthId = "mouth_01_smile";
        [SerializeField] public string BackAccessoryId = "";
        [SerializeField] public string NeckAccessoryId = "";
        [SerializeField] public string TailAccessoryId = "";
        [SerializeField] public string EffectId = "";

        // Custom color overrides if player fine-tunes colors
        [SerializeField] public bool UseCustomColors = false;
        [SerializeField] public Color CustomPrimary = Color.white;
        [SerializeField] public Color CustomSecondary = Color.white;
        [SerializeField] public Color CustomAccent = Color.cyan;

        public static EquippedCosmetics CreateDefault()
        {
            return new EquippedCosmetics
            {
                CreatureId = "creature_01_grub",
                ColorId = "color_01_emerald",
                PatternId = "pattern_01_solid",
                ClothingId = "",
                HatId = "",
                EyesId = "eyes_01_normal",
                MouthId = "mouth_01_smile",
                BackAccessoryId = "",
                NeckAccessoryId = "",
                TailAccessoryId = "",
                EffectId = "",
                UseCustomColors = false,
                CustomPrimary = new Color(0.12f, 0.95f, 0.72f),
                CustomSecondary = new Color(0.04f, 0.65f, 0.45f),
                CustomAccent = new Color(0.45f, 1f, 0.85f)
            };
        }

        public EquippedCosmetics Clone()
        {
            return new EquippedCosmetics
            {
                CreatureId = this.CreatureId,
                ColorId = this.ColorId,
                PatternId = this.PatternId,
                ClothingId = this.ClothingId,
                HatId = this.HatId,
                EyesId = this.EyesId,
                MouthId = this.MouthId,
                BackAccessoryId = this.BackAccessoryId,
                NeckAccessoryId = this.NeckAccessoryId,
                TailAccessoryId = this.TailAccessoryId,
                EffectId = this.EffectId,
                UseCustomColors = this.UseCustomColors,
                CustomPrimary = this.CustomPrimary,
                CustomSecondary = this.CustomSecondary,
                CustomAccent = this.CustomAccent
            };
        }
    }
}
