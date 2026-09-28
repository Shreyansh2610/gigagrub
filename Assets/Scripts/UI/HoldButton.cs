using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GigaGrub.UI
{
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [Header("Visual Feedback")]
        [SerializeField] private Image targetGraphic;
        [SerializeField] private Color normalColor = new Color(0.95f, 0.35f, 0.25f, 0.85f);
        [SerializeField] private Color pressedColor = new Color(1f, 0.55f, 0.35f, 1f);
        [SerializeField] private Color disabledColor = new Color(0.4f, 0.4f, 0.45f, 0.6f);
        [SerializeField] private float pressedScale = 0.92f;
        [SerializeField] private float transitionSpeed = 16f;

        private bool isPressed;
        private bool isInteractable = true;
        private Vector3 originalScale = Vector3.one;
        private Vector3 targetScale = Vector3.one;

        public bool IsPressed => isPressed && isInteractable;
        public bool IsInteractable => isInteractable;

        public event Action<bool> OnPressedChanged;

        private void Awake()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Image>();
            }

            originalScale = transform.localScale;
            targetScale = originalScale;
            UpdateVisualInstant();
        }

        private void OnDisable()
        {
            if (isPressed)
            {
                isPressed = false;
                targetScale = originalScale;
                UpdateVisualInstant();
                OnPressedChanged?.Invoke(false);
            }
        }

        private void Update()
        {
            if (transform.localScale != targetScale)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.unscaledDeltaTime * transitionSpeed);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInteractable) return;

            isPressed = true;
            targetScale = originalScale * pressedScale;
            ApplyColor(pressedColor);
            OnPressedChanged?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!isPressed) return;

            isPressed = false;
            targetScale = originalScale;
            ApplyColor(isInteractable ? normalColor : disabledColor);
            OnPressedChanged?.Invoke(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!isPressed) return;

            isPressed = false;
            targetScale = originalScale;
            ApplyColor(isInteractable ? normalColor : disabledColor);
            OnPressedChanged?.Invoke(false);
        }

        public void SetInteractable(bool interactable)
        {
            if (isInteractable == interactable) return;

            isInteractable = interactable;
            if (!isInteractable && isPressed)
            {
                isPressed = false;
                targetScale = originalScale;
                OnPressedChanged?.Invoke(false);
            }

            ApplyColor(isInteractable ? (isPressed ? pressedColor : normalColor) : disabledColor);
        }

        private void ApplyColor(Color col)
        {
            if (targetGraphic != null)
            {
                targetGraphic.color = col;
            }
        }

        private void UpdateVisualInstant()
        {
            transform.localScale = targetScale;
            ApplyColor(isInteractable ? (isPressed ? pressedColor : normalColor) : disabledColor);
        }
    }
}
