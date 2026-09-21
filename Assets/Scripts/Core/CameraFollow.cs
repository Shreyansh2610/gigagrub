using UnityEngine;

namespace GigaGrub.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target Tracking")]
        [Tooltip("Transform of the player to follow")]
        public Transform target;

        [Tooltip("Forward lookahead distance based on creature orientation")]
        [SerializeField] private float lookaheadDistance = 1.2f;

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

        [Tooltip("Enable dynamic zoom scaling as creature body grows longer")]
        [SerializeField] private bool autoScaleZoomWithLength = true;

        [Header("Boundary Clamping")]
        [Tooltip("Whether camera should strictly clamp within ArenaManager bounds")]
        [SerializeField] private bool clampToArena = true;

        private Camera cam;
        private float targetZoom;
        private Vector3 currentVelocity;
        private Player.PlayerBody targetBody;

        // Screen Shake state
        private float shakeIntensity = 0f;
        private float shakeDuration = 0f;
        private float shakeTimer = 0f;
        private Vector3 shakeOffset = Vector3.zero;

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
            ResolveTargetBody();
        }

        private void ResolveTargetBody()
        {
            if (target != null)
            {
                targetBody = target.GetComponent<Player.PlayerBody>();
            }
        }

        private void LateUpdate()
        {
            if (cam == null) return;

            UpdateShake();
            UpdateZoom();
            UpdatePosition();
        }

        private void UpdateShake()
        {
            if (shakeTimer < shakeDuration)
            {
                shakeTimer += Time.deltaTime;
                float damp = 1f - Mathf.Clamp01(shakeTimer / Mathf.Max(0.001f, shakeDuration));
                float currentMag = shakeIntensity * damp;
                shakeOffset = new Vector3(
                    Random.Range(-currentMag, currentMag),
                    Random.Range(-currentMag, currentMag),
                    0f
                );
            }
            else
            {
                shakeOffset = Vector3.zero;
            }
        }

        private void UpdateZoom()
        {
            if (autoScaleZoomWithLength)
            {
                if (targetBody == null && target != null)
                {
                    ResolveTargetBody();
                }

                if (targetBody != null)
                {
                    // As length grows from 10 to 100+, scale zoom from 9.0 to ~13.5
                    int length = targetBody.CurrentLength;
                    float growthT = Mathf.Clamp01((length - 10) / 120f);
                    targetZoom = Mathf.Lerp(defaultZoom, defaultZoom + 4.5f, growthT);
                }
            }

            targetZoom = Mathf.Clamp(targetZoom, minZoom, maxZoom);
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
        }

        private void UpdatePosition()
        {
            if (target == null) return;

            Vector3 forwardLead = target.up * lookaheadDistance;
            Vector3 desiredPosition = target.position + forwardLead + offset + shakeOffset;

            if (clampToArena && ArenaManager.Instance != null)
            {
                desiredPosition = ClampCameraPosition(desiredPosition);
            }

            // Smooth position interpolation
            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

            // Ensure interpolated camera position stays strictly within arena bounds on every frame
            if (clampToArena && ArenaManager.Instance != null)
            {
                transform.position = ClampCameraPosition(transform.position);
            }
        }

        public void TriggerShake(float intensity = 0.18f, float duration = 0.15f)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = 0f;
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

        public void SnapToTarget()
        {
            if (target == null) return;

            Vector3 desiredPosition = target.position + offset;
            if (clampToArena && ArenaManager.Instance != null)
            {
                desiredPosition = ClampCameraPosition(desiredPosition);
            }
            transform.position = desiredPosition;
            ResetZoom();
        }
    }
}
