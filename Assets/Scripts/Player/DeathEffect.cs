using System.Collections.Generic;
using UnityEngine;

namespace GigaGrub.Player
{
    public class DeathEffect : MonoBehaviour
    {
        private static readonly Queue<DeathEffect> pool = new Queue<DeathEffect>();
        private static Transform poolParent;
        private static GameObject effectPrefabRef;

        [Header("Components")]
        [SerializeField] private SpriteRenderer shockwaveRenderer;
        [SerializeField] private SpriteRenderer[] debrisRenderers;

        [Header("Animation Settings")]
        [SerializeField] private float duration = 0.5f;
        [SerializeField] private float startScale = 0.5f;
        [SerializeField] private float endScale = 3.2f;
        [SerializeField] private float debrisSpread = 2.8f;

        private float timer = 0f;
        private bool isPlaying = false;
        private Color mainColor;
        private float scaleMultiplier = 1f;

        private static readonly Vector2[] debrisDirections = new Vector2[]
        {
            new Vector2( 1.0f,  0.0f),
            new Vector2(-1.0f,  0.0f),
            new Vector2( 0.0f,  1.0f),
            new Vector2( 0.0f, -1.0f),
            new Vector2( 0.707f,  0.707f),
            new Vector2(-0.707f,  0.707f),
            new Vector2(-0.707f, -0.707f),
            new Vector2( 0.707f, -0.707f)
        };

        private void Awake()
        {
            if (shockwaveRenderer == null)
            {
                shockwaveRenderer = GetComponent<SpriteRenderer>();
            }

            EnsureDebris();
        }

        private void EnsureDebris()
        {
            if (debrisRenderers == null || debrisRenderers.Length == 0)
            {
                debrisRenderers = new SpriteRenderer[debrisDirections.Length];
                for (int i = 0; i < debrisDirections.Length; i++)
                {
                    Transform existing = transform.Find($"Debris_{i}");
                    GameObject dGo = existing != null ? existing.gameObject : new GameObject($"Debris_{i}");
                    dGo.transform.SetParent(transform, false);

                    SpriteRenderer sr = dGo.GetComponent<SpriteRenderer>();
                    if (sr == null) sr = dGo.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 60;
                    if (shockwaveRenderer != null && shockwaveRenderer.sprite != null)
                    {
                        sr.sprite = shockwaveRenderer.sprite;
                    }
                    debrisRenderers[i] = sr;
                }
            }
        }

        private void Update()
        {
            if (!isPlaying) return;

            timer += Time.deltaTime;
            float progress = Mathf.Clamp01(timer / duration);
            float easeOut = 1f - Mathf.Pow(1f - progress, 3f);

            // Shockwave expansion
            float currentScale = Mathf.Lerp(startScale, endScale, easeOut) * scaleMultiplier;
            transform.localScale = Vector3.one * currentScale;

            // Fade alpha
            Color c = mainColor;
            c.a = Mathf.Lerp(mainColor.a, 0f, progress * progress);

            if (shockwaveRenderer != null)
            {
                shockwaveRenderer.color = c;
            }

            // Scatter debris outward
            if (debrisRenderers != null)
            {
                float spread = easeOut * debrisSpread * scaleMultiplier;
                float dScale = Mathf.Lerp(0.5f, 0.05f, progress) * scaleMultiplier;

                for (int i = 0; i < debrisRenderers.Length; i++)
                {
                    if (debrisRenderers[i] != null)
                    {
                        debrisRenderers[i].color = c;
                        debrisRenderers[i].transform.localPosition = (Vector3)(debrisDirections[i] * spread);
                        debrisRenderers[i].transform.localScale = Vector3.one * dScale;
                    }
                }
            }

            if (progress >= 1f)
            {
                StopAndReturn();
            }
        }

        public void Play(Vector3 position, Color color, float scale = 1f)
        {
            transform.position = position;
            mainColor = color;
            scaleMultiplier = scale;
            timer = 0f;
            isPlaying = true;

            EnsureDebris();

            if (shockwaveRenderer != null)
            {
                shockwaveRenderer.color = mainColor;
            }

            if (debrisRenderers != null)
            {
                for (int i = 0; i < debrisRenderers.Length; i++)
                {
                    if (debrisRenderers[i] != null)
                    {
                        debrisRenderers[i].color = mainColor;
                        debrisRenderers[i].transform.localPosition = Vector3.zero;
                    }
                }
            }

            transform.localScale = Vector3.one * (startScale * scaleMultiplier);
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

        public static DeathEffect Spawn(Vector3 position, Color color, float scale = 1.2f)
        {
            DeathEffect effect = null;

            while (pool.Count > 0 && effect == null)
            {
                effect = pool.Dequeue();
            }

            if (effect == null)
            {
                if (poolParent == null)
                {
                    GameObject parentGo = new GameObject("DeathEffectsPool");
                    poolParent = parentGo.transform;
                }

                if (effectPrefabRef != null)
                {
                    GameObject go = Instantiate(effectPrefabRef, position, Quaternion.identity, poolParent);
                    effect = go.GetComponent<DeathEffect>();
                }
                else
                {
                    GameObject go = new GameObject("DeathEffect");
                    go.transform.SetParent(poolParent, false);
                    SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 55;
                    effect = go.AddComponent<DeathEffect>();
                }
            }

            effect.Play(position, color, scale);
            return effect;
        }
    }
}
