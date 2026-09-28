using UnityEngine;

namespace GigaGrub.Cosmetics
{
    public class CosmeticEffectInstance : MonoBehaviour
    {
        [SerializeField] private ParticleSystem particleSys;

        private CosmeticEffectData currentEffectData;
        private static Material defaultParticleMat;

        public void EnsureParticleSystem()
        {
            if (particleSys == null)
            {
                particleSys = GetComponent<ParticleSystem>();
                if (particleSys == null)
                {
                    particleSys = gameObject.AddComponent<ParticleSystem>();
                }
            }
        }

        public void ApplyEffect(CosmeticEffectData effectData)
        {
            currentEffectData = effectData;
            EnsureParticleSystem();

            if (effectData == null)
            {
                if (particleSys != null)
                {
                    particleSys.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            // 1. Ensure Particle Material
            var renderer = particleSys.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 95;
                if (renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null)
                {
                    if (defaultParticleMat == null)
                    {
                        Shader s = Shader.Find("Sprites/Default");
                        if (s == null) s = Shader.Find("Mobile/Particles/Alpha Blended");
                        if (s == null) s = Shader.Find("UI/Default");
                        if (s != null)
                        {
                            defaultParticleMat = new Material(s);
                        }
                    }
                    if (defaultParticleMat != null)
                    {
                        renderer.material = new Material(defaultParticleMat);
                    }
                }

                if (renderer.material != null && effectData.ParticleSprite != null && effectData.ParticleSprite.texture != null)
                {
                    renderer.material.mainTexture = effectData.ParticleSprite.texture;
                }
            }

            // 2. Configure Main Module
            var main = particleSys.main;
            main.playOnAwake = false;
            main.loop = true;
            main.startLifetime = Mathf.Max(0.5f, effectData.ParticleLifetime);
            main.startSpeed = Mathf.Max(0.5f, effectData.Speed);
            main.startSize = Mathf.Max(0.45f, effectData.ParticleSize * 1.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = new ParticleSystem.MinMaxGradient(effectData.PrimaryEffectColor, effectData.SecondaryEffectColor);

            // 3. Configure Emission
            var emission = particleSys.emission;
            emission.enabled = true;
            emission.rateOverTime = Mathf.Max(15f, effectData.EmissionRate * 1.5f);
            emission.rateOverDistance = 3.5f;

            // 4. Configure Shape
            var shape = particleSys.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f;

            // 5. Configure Color Over Lifetime
            var colorOverLifetime = particleSys.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(effectData.PrimaryEffectColor, 0f), new GradientColorKey(effectData.SecondaryEffectColor, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0.95f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) }
            );
            colorOverLifetime.color = grad;

            // 6. Configure Size Over Lifetime
            var sizeOverLifetime = particleSys.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            AnimationCurve curve = new AnimationCurve();
            curve.AddKey(0f, 1f);
            curve.AddKey(1f, 0.15f);
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

            // 7. Configure Texture Sheet Animation if custom sprite is present
            if (effectData.ParticleSprite != null)
            {
                var texSheet = particleSys.textureSheetAnimation;
                texSheet.enabled = true;
                texSheet.mode = ParticleSystemAnimationMode.Sprites;
                if (texSheet.spriteCount == 0 || texSheet.GetSprite(0) != effectData.ParticleSprite)
                {
                    texSheet.SetSprite(0, effectData.ParticleSprite);
                }
            }
            else
            {
                var texSheet = particleSys.textureSheetAnimation;
                texSheet.enabled = false;
            }

            particleSys.Clear();
            particleSys.Play();
        }

        public void Stop()
        {
            if (particleSys != null)
            {
                particleSys.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }
}
