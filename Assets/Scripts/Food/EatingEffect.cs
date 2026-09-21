using System.Collections.Generic;
using UnityEngine;

namespace GigaGrub.Food
{
    public class EatingEffect : MonoBehaviour
    {
        private static readonly Queue<EatingEffect> pool = new Queue<EatingEffect>();
        private static Transform poolParent;
        private static GameObject effectPrefabRef;

        [Header("Components")]
        [SerializeField] private SpriteRenderer spriteRenderer;

        [Header("Particle Bursts")]
        [SerializeField] private SpriteRenderer[] particleRenderers;

        [Header("Animation")]
        [SerializeField] private float duration = 0.28f;
        [SerializeField] private float startScale = 0.3f;
        [SerializeField] private float endScale = 1.7f;
        [SerializeField] private float particleSpread = 1.2f;

        private float timer = 0f;
        private bool isPlaying = false;
        private Color initialColor;
        private float baseScaleMultiplier = 1f;
        private static readonly Vector2[] particleDirections = new Vector2[]
        {
            new Vector2( 0.707f,  0.707f),
            new Vector2(-0.707f,  0.707f),
            new Vector2(-0.707f, -0.707f),
            new Vector2( 0.707f, -0.707f),
            new Vector2( 1.0f,    0.0f),
            new Vector2(-1.0f,    0.0f)
        };

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            EnsureParticles();
        }

        private void EnsureParticles()
        {
            if (particleRenderers == null || particleRenderers.Length == 0)
            {
                particleRenderers = new SpriteRenderer[particleDirections.Length];
                for (int i = 0; i < particleDirections.Length; i++)
                {
                    Transform existing = transform.Find($"Particle_{i}");
                    GameObject pGo = existing != null ? existing.gameObject : new GameObject($"Particle_{i}");
                    pGo.transform.SetParent(transform, false);

                    SpriteRenderer sr = pGo.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = pGo.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 46;
                    if (spriteRenderer != null && spriteRenderer.sprite != null)
                    {
                        sr.sprite = spriteRenderer.sprite;
                    }
                    particleRenderers[i] = sr;
                }
            }
        }

        private void Update()
        {
            if (!isPlaying) return;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // Ring scale expansion with ease-out
            float easeOut = 1f - (1f - t) * (1f - t);
            float scale = Mathf.Lerp(startScale, endScale, easeOut) * baseScaleMultiplier;
            transform.localScale = Vector3.one * scale;

            // Alpha fadeout
            Color c = initialColor;
            c.a = Mathf.Lerp(initialColor.a, 0f, t * t);

            if (spriteRenderer != null)
            {
                spriteRenderer.color = c;
            }

            // Scatter child sparkle particles outward
            if (particleRenderers != null)
            {
                float spreadDist = easeOut * particleSpread * baseScaleMultiplier;
                float partScale = Mathf.Lerp(0.35f, 0f, t) * baseScaleMultiplier;

                for (int i = 0; i < particleRenderers.Length; i++)
                {
                    if (particleRenderers[i] != null)
                    {
                        particleRenderers[i].color = c;
                        particleRenderers[i].transform.localPosition = (Vector3)(particleDirections[i] * spreadDist);
                        particleRenderers[i].transform.localScale = Vector3.one * partScale;
                    }
                }
            }

            if (t >= 1f)
            {
                StopAndReturn();
            }
        }

        public void Play(Vector3 position, Color color, float scaleMultiplier = 1f)
        {
            transform.position = position;
            initialColor = color;
            baseScaleMultiplier = scaleMultiplier;
            timer = 0f;
            isPlaying = true;

            EnsureParticles();

            if (spriteRenderer != null)
            {
                spriteRenderer.color = initialColor;
            }

            if (particleRenderers != null)
            {
                for (int i = 0; i < particleRenderers.Length; i++)
                {
                    if (particleRenderers[i] != null)
                    {
                        particleRenderers[i].color = initialColor;
                        particleRenderers[i].transform.localPosition = Vector3.zero;
                    }
                }
            }

            transform.localScale = Vector3.one * (startScale * baseScaleMultiplier);
            gameObject.SetActive(true);
        }

        private void StopAndReturn()
        {
            isPlaying = false;
            gameObject.SetActive(false);
            pool.Enqueue(this);
        }

        public static void SetPrefabReference(GameObject prefab)
        {
            effectPrefabRef = prefab;
        }

        public static EatingEffect Spawn(Vector3 position, Color color, float scaleMultiplier = 1f)
        {
            EatingEffect effect = null;

            while (pool.Count > 0 && effect == null)
            {
                effect = pool.Dequeue();
            }

            if (effect == null)
            {
                if (poolParent == null)
                {
                    GameObject parentGo = new GameObject("EatingEffectsPool");
                    poolParent = parentGo.transform;
                }

                if (effectPrefabRef != null)
                {
                    GameObject go = Instantiate(effectPrefabRef, position, Quaternion.identity, poolParent);
                    effect = go.GetComponent<EatingEffect>();
                }
                else
                {
                    GameObject go = new GameObject("EatingEffect");
                    go.transform.SetParent(poolParent, false);
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 45;
                    effect = go.AddComponent<EatingEffect>();
                }
            }

            effect.Play(position, color, scaleMultiplier);
            return effect;
        }
    }
}
