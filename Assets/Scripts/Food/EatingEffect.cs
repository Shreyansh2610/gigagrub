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

        [Header("Animation")]
        [SerializeField] private float duration = 0.28f;
        [SerializeField] private float startScale = 0.4f;
        [SerializeField] private float endScale = 1.6f;

        private float timer = 0f;
        private bool isPlaying = false;
        private Color initialColor;
        private float baseScaleMultiplier = 1f;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void Update()
        {
            if (!isPlaying) return;

            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / duration);

            // Scale expansion
            float scale = Mathf.Lerp(startScale, endScale, t) * baseScaleMultiplier;
            transform.localScale = Vector3.one * scale;

            // Alpha fadeout
            if (spriteRenderer != null)
            {
                Color c = initialColor;
                c.a = Mathf.Lerp(initialColor.a, 0f, t * t);
                spriteRenderer.color = c;
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

            if (spriteRenderer != null)
            {
                spriteRenderer.color = initialColor;
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
