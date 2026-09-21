using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GigaGrub.Systems
{
    public class SceneTransitionManager : MonoBehaviour
    {
        public static SceneTransitionManager Instance { get; private set; }

        [Header("Transition Settings")]
        [Tooltip("Duration in seconds for fade out and fade in")]
        [SerializeField] private float defaultFadeDuration = 0.35f;

        [SerializeField] private Canvas transitionCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image fadeImage;

        private bool isTransitioning = false;

        public bool IsTransitioning => isTransitioning;
        public float DefaultFadeDuration => defaultFadeDuration;

        public event Action<string> OnTransitionStarted;
        public event Action<string> OnTransitionCompleted;

        public static void SetInstanceForTest(SceneTransitionManager manager)
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
            DontDestroyOnLoad(gameObject);

            EnsureTransitionVisuals();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnsureTransitionVisuals()
        {
            if (transitionCanvas == null)
            {
                transitionCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (transitionCanvas == null)
            {
                GameObject canvasGo = new GameObject("TransitionCanvas");
                canvasGo.transform.SetParent(transform, false);

                transitionCanvas = canvasGo.AddComponent<Canvas>();
                transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                transitionCanvas.sortingOrder = 9999; // Above all game and UI elements

                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();

                GameObject imgGo = new GameObject("FadeOverlay");
                imgGo.transform.SetParent(canvasGo.transform, false);

                RectTransform rect = imgGo.AddComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                fadeImage = imgGo.AddComponent<Image>();
                fadeImage.color = new Color(0.04f, 0.06f, 0.09f, 1f); // Sleek dark slate tint

                canvasGroup = imgGo.AddComponent<CanvasGroup>();
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                if (canvasGroup == null)
                {
                    canvasGroup = transitionCanvas.GetComponentInChildren<CanvasGroup>(true);
                }
                if (fadeImage == null)
                {
                    fadeImage = transitionCanvas.GetComponentInChildren<Image>(true);
                }
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void TransitionToScene(string sceneName, float fadeDuration = -1f)
        {
            if (isTransitioning) return;

            float duration = fadeDuration > 0f ? fadeDuration : defaultFadeDuration;
            StartCoroutine(TransitionRoutine(sceneName, duration));
        }

        private IEnumerator TransitionRoutine(string sceneName, float duration)
        {
            isTransitioning = true;
            EnsureTransitionVisuals();
            OnTransitionStarted?.Invoke(sceneName);

            // 1. Fade to dark overlay
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(elapsed / duration);
                    yield return null;
                }
                canvasGroup.alpha = 1f;
            }

            // 2. Load target scene asynchronously
            AsyncOperation loadOp = SceneManager.LoadSceneAsync(sceneName);
            if (loadOp != null)
            {
                while (!loadOp.isDone)
                {
                    yield return null;
                }
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }

            // Brief delay for scene initialization
            yield return new WaitForSecondsRealtime(0.05f);

            // 3. Fade in from dark overlay
            if (canvasGroup != null)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    canvasGroup.alpha = Mathf.Clamp01(1f - (elapsed / duration));
                    yield return null;
                }
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            isTransitioning = false;
            OnTransitionCompleted?.Invoke(sceneName);
        }
    }
}
