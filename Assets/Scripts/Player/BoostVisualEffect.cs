using UnityEngine;

namespace GigaGrub.Player
{
    public class BoostVisualEffect : MonoBehaviour
    {
        [Header("Particle References")]
        [SerializeField] private ParticleSystem boostParticles;
        [SerializeField] private TrailRenderer boostTrail;

        [Header("Visual Feedback Settings")]
        [SerializeField] private Color boostGlowColor = new Color(0.22f, 0.74f, 0.97f, 0.9f); // Sky Cyan #38BDF8
        [SerializeField] private float headScalePulse = 1.08f;
        [SerializeField] private float pulseSpeed = 14f;

        private BoostSystem boostSystem;
        private Transform headTransform;
        private SpriteRenderer headSpriteRenderer;
        private Vector3 originalHeadScale = Vector3.one;
        private Color originalHeadColor = Color.white;
        private bool isBoosting;
        private ParticleSystem.EmissionModule emissionModule;

        private void Awake()
        {
            headTransform = transform;
            headSpriteRenderer = GetComponent<SpriteRenderer>();
            if (headSpriteRenderer != null)
            {
                originalHeadColor = headSpriteRenderer.color;
            }
            originalHeadScale = headTransform.localScale;

            boostSystem = GetComponent<BoostSystem>();
            if (boostSystem != null)
            {
                boostSystem.OnBoostStateChanged += HandleBoostStateChanged;
            }

            SetupParticlesIfMissing();
        }

        private void OnDestroy()
        {
            if (boostSystem != null)
            {
                boostSystem.OnBoostStateChanged -= HandleBoostStateChanged;
            }
        }

        private void Update()
        {
            if (isBoosting)
            {
                // Dynamic subtle pulsing while boosting
                float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * (headScalePulse - 1f);
                headTransform.localScale = originalHeadScale * pulse;
            }
            else if (headTransform.localScale != originalHeadScale)
            {
                headTransform.localScale = Vector3.Lerp(headTransform.localScale, originalHeadScale, Time.deltaTime * 10f);
            }
        }

        public void BindBoostSystem(BoostSystem system)
        {
            if (boostSystem != null)
            {
                boostSystem.OnBoostStateChanged -= HandleBoostStateChanged;
            }

            boostSystem = system;
            if (boostSystem != null)
            {
                boostSystem.OnBoostStateChanged += HandleBoostStateChanged;
            }
        }

        public void HandleBoostStateChanged(bool boosting)
        {
            isBoosting = boosting;

            if (boostParticles != null)
            {
                if (boosting)
                {
                    if (!boostParticles.isPlaying) boostParticles.Play();
                    emissionModule.enabled = true;
                }
                else
                {
                    emissionModule.enabled = false;
                }
            }

            if (boostTrail != null)
            {
                boostTrail.emitting = boosting;
            }

            if (!boosting)
            {
                headTransform.localScale = originalHeadScale;
            }
        }

        private void SetupParticlesIfMissing()
        {
            if (boostParticles == null)
            {
                GameObject particleGo = new GameObject("BoostParticles");
                particleGo.transform.SetParent(transform, false);
                particleGo.transform.localPosition = new Vector3(0f, -0.3f, 0f);

                boostParticles = particleGo.AddComponent<ParticleSystem>();
                var main = boostParticles.main;
                main.startLifetime = 0.25f;
                main.startSpeed = 2.5f;
                main.startSize = 0.28f;
                main.startColor = boostGlowColor;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.playOnAwake = false;
                main.loop = true;

                emissionModule = boostParticles.emission;
                emissionModule.rateOverTime = 30f;
                emissionModule.enabled = false;

                var shape = boostParticles.shape;
                shape.shapeType = ParticleSystemShapeType.Circle;
                shape.radius = 0.2f;

                var colorOverLifetime = boostParticles.colorOverLifetime;
                colorOverLifetime.enabled = true;
                Gradient grad = new Gradient();
                grad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(boostGlowColor, 0f), new GradientColorKey(Color.white, 1f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) }
                );
                colorOverLifetime.color = grad;

                var renderer = particleGo.GetComponent<ParticleSystemRenderer>();
                renderer.sortingOrder = 95;
            }
            else
            {
                emissionModule = boostParticles.emission;
            }
        }
    }
}
