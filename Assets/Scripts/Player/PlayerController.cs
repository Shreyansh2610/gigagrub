using UnityEngine;
using GigaGrub.UI;
using GigaGrub.Core;

namespace GigaGrub.Player
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Forward movement speed in units per second")]
        [SerializeField] private float moveSpeed = 5f;

        [Tooltip("Steering angular rotation speed in degrees per second")]
        [SerializeField] private float turnSpeed = 360f;

        [Tooltip("Smoothness factor for angular turn damping")]
        [SerializeField] private float turnDamping = 16f;

        [Tooltip("Radius of the creature head for boundary collision")]
        [SerializeField] private float headRadius = 0.5f;

        [Header("References")]
        [Tooltip("Reference to the on-screen virtual joystick")]
        public VirtualJoystick joystick;

        private float currentSpeed;
        private float targetSpeed;
        private Quaternion targetRotation;

        public float CurrentSpeed => currentSpeed;
        public float BaseSpeed => moveSpeed;

        private void Awake()
        {
            // Optimize mobile framerate for smooth responsiveness
            Application.targetFrameRate = 60;
            currentSpeed = moveSpeed;
            targetSpeed = moveSpeed;
            targetRotation = transform.rotation;
        }

        private void Update()
        {
            HandleSteering();
            HandleMovement();
            ApplyBoundaryConstraint();
        }

        private void HandleSteering()
        {
            Vector2 input = Vector2.zero;

            if (joystick != null && joystick.InputDirection.sqrMagnitude > 0.001f)
            {
                input = joystick.InputDirection;
            }
            else
            {
                // Fallback keyboard input for testing in Unity Editor
                float h = Input.GetAxisRaw("Horizontal");
                float v = Input.GetAxisRaw("Vertical");
                if (Mathf.Abs(h) > 0.01f || Mathf.Abs(v) > 0.01f)
                {
                    input = new Vector2(h, v).normalized;
                }
            }

            if (input.sqrMagnitude > 0.001f)
            {
                // Calculate target angle (0 degrees = up/forward)
                float targetAngle = Mathf.Atan2(-input.x, input.y) * Mathf.Rad2Deg;
                targetRotation = Quaternion.Euler(0f, 0f, targetAngle);
            }

            // Dual interpolation: Angular speed limit + exponential smoothing for ultra-smooth fluid steering
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                turnSpeed * Time.deltaTime
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                turnDamping * Time.deltaTime
            );
        }

        private void HandleMovement()
        {
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * 12f);

            // Continuous forward movement in the direction the creature is facing
            transform.position += transform.up * (currentSpeed * Time.deltaTime);
        }

        private void ApplyBoundaryConstraint()
        {
            if (ArenaManager.Instance != null)
            {
                Vector2 clamped = ArenaManager.Instance.ClampPosition(transform.position, headRadius);
                transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
            }
        }

        public void SetSpeed(float newSpeed)
        {
            targetSpeed = Mathf.Max(0f, newSpeed);
        }

        public void ResetSpeed()
        {
            targetSpeed = moveSpeed;
        }
    }
}
