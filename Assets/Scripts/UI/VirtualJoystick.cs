using UnityEngine;
using UnityEngine.EventSystems;

namespace GigaGrub.UI
{
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private RectTransform joystickBackground;
        [SerializeField] private RectTransform joystickKnob;

        [Header("Settings")]
        [SerializeField] private float handleLimit = 100f;
        [SerializeField] private float deadZone = 0.05f;

        public Vector2 InputDirection { get; private set; }

        private void Awake()
        {
            if (joystickBackground == null)
            {
                joystickBackground = GetComponent<RectTransform>();
            }

            if (joystickKnob == null && transform.childCount > 0)
            {
                joystickKnob = transform.GetChild(0).GetComponent<RectTransform>();
            }

            ResetKnob();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (joystickBackground == null || joystickKnob == null) return;

            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                joystickBackground,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
            {
                float radius = handleLimit > 0 ? handleLimit : joystickBackground.rect.width * 0.5f;
                Vector2 clampedPosition = Vector2.ClampMagnitude(localPoint, radius);
                joystickKnob.anchoredPosition = clampedPosition;

                Vector2 rawDirection = clampedPosition / radius;
                if (rawDirection.magnitude < deadZone)
                {
                    InputDirection = Vector2.zero;
                }
                else
                {
                    InputDirection = rawDirection;
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            ResetKnob();
        }

        private void ResetKnob()
        {
            InputDirection = Vector2.zero;
            if (joystickKnob != null)
            {
                joystickKnob.anchoredPosition = Vector2.zero;
            }
        }
    }
}
