using System;
using UnityEngine;

namespace GigaGrub.PowerUps
{
    [Serializable]
    public class ActivePowerUp
    {
        public PowerUpData Data { get; private set; }
        public float RemainingTime { get; private set; }
        public float TotalDuration { get; private set; }
        public float EffectStrength => Data != null ? Data.EffectStrength : 1.0f;
        public PowerUpType Type => Data != null ? Data.Type : PowerUpType.SpeedBoost;
        public string Id => Data != null ? Data.Id : string.Empty;
        public string Name => Data != null ? Data.PowerUpName : string.Empty;
        public Color ThemeColor => Data != null ? Data.ThemeColor : Color.white;
        public Sprite IconSprite => Data != null ? Data.IconSprite : null;

        public float NormalizedProgress => TotalDuration > 0f ? Mathf.Clamp01(RemainingTime / TotalDuration) : 0f;
        public bool IsExpired => RemainingTime <= 0f;

        public ActivePowerUp(PowerUpData data)
        {
            Data = data;
            TotalDuration = data != null ? data.Duration : 5.0f;
            RemainingTime = TotalDuration;
        }

        public void Tick(float deltaTime)
        {
            RemainingTime = Mathf.Max(0f, RemainingTime - deltaTime);
        }

        public void Update(float deltaTime) => Tick(deltaTime);

        public void RefreshDuration()
        {
            TotalDuration = Data != null ? Data.Duration : TotalDuration;
            RemainingTime = TotalDuration;
        }

        public void ResetDuration() => RefreshDuration();

        public void SetRemainingTime(float time)
        {
            RemainingTime = Mathf.Clamp(time, 0f, TotalDuration);
        }
    }
}
