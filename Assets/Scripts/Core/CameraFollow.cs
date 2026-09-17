using UnityEngine;

namespace GigaGrub.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Transform of the player to follow")]
        public Transform target;

        [Tooltip("Speed of camera tracking interpolation")]
        [SerializeField] private float smoothSpeed = 8f;

        [Tooltip("Position offset from target")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        [Header("Zoom Settings")]
        [Tooltip("Default orthographic size")]
        [SerializeField] private float defaultZoom = 9f;

        [Tooltip("Minimum allowable camera orthographic size")]
        [SerializeField] private float minZoom = 6f;

        [Tooltip("Maximum allowable camera orthographic size")]
        [SerializeField] private float maxZoom = 18f;

        [Tooltip("Speed of camera zoom interpolation")]
        [SerializeField] private float zoomSpeed = 4f;

        [Header("Boundary Clamping")]
        [Tooltip("Whether camera should strictly clamp within ArenaManager bounds")]
        [SerializeField] private bool clampToArena = true;

        private Camera cam;
        private float targetZoom;
        private Vector3 currentVelocity;

        public float CurrentZoom => cam != null ? cam.orthographicSize : defaultZoom;
        public float TargetZoom => targetZoom;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = Camera.main;
            }

            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = defaultZoom;
            }

            targetZoom = defaultZoom;
        }

        private void LateUpdate()
        {
            if (cam == null) return;

            UpdateZoom();
            UpdatePosition();
        }

        private void UpdateZoom()
        {
            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }

        private void UpdatePosition()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;

            if (clampToArena && ArenaManager.Instance != null)
            {
                desiredPosition = ClampCameraPosition(desiredPosition);
            }

            // Smooth position interpolation
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        }

        public Vector3 ClampCameraPosition(Vector3 rawPosition)
        {
            if (cam == null || ArenaManager.Instance == null) return rawPosition;

            float camHalfHeight = cam.orthographicSize;
            float camHalfWidth = camHalfHeight * cam.aspect;

            Vector2 arenaHalf = ArenaManager.Instance.HalfSize;

            float minX = -arenaHalf.x + camHalfWidth;
            float maxX = arenaHalf.x - camHalfWidth;
            float minY = -arenaHalf.y + camHalfHeight;
            float maxY = arenaHalf.y - camHalfHeight;

            float clampedX;
            if (minX > maxX)
            {
                // If screen is wider than arena, center horizontally
                clampedX = 0f;
            }
            else
            {
                clampedX = Mathf.Clamp(rawPosition.x, minX, maxX);
            }

            float clampedY;
            if (minY > maxY)
            {
                // If screen is taller than arena, center vertically
                clampedY = 0f;
            }
            else
            {
                clampedY = Mathf.Clamp(rawPosition.y, minY, maxY);
            }

            return new Vector3(clampedX, clampedY, offset.z);
        }

        public void SetZoom(float newZoom)
        {
            targetZoom = Mathf.Clamp(newZoom, minZoom, maxZoom);
        }

        public void ZoomBy(float delta)
        {
            SetZoom(targetZoom + delta);
        }

        public void ResetZoom()
        {
            SetZoom(defaultZoom);
        }
    }
}
