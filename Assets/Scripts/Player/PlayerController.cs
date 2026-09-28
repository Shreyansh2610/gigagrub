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

        [Tooltip("Reference to the on-screen mobile hold boost button")]
        public HoldButton boostButton;

        [Tooltip("Reference to the creature boost energy system")]
        [SerializeField] private BoostSystem boostSystem;

        [Tooltip("Reference to active power-up manager")]
        [SerializeField] private PowerUps.PowerUpManager powerUpManager;

        [Header("Audio")]
        [SerializeField] private AudioSource boostAudioSource;
        [SerializeField] private float maxBoostAudioVolume = 0.55f;

        private float currentSpeed;
        private float targetSpeed;
        private Quaternion targetRotation;

        public float CurrentSpeed => currentSpeed;
        public float BaseSpeed => moveSpeed;
        public BoostSystem Boost => boostSystem;
        public PowerUps.PowerUpManager PowerUps => powerUpManager;
        public bool IsBoosting => boostSystem != null && boostSystem.IsBoosting;

        private void Awake()
        {
            // Optimize mobile framerate for smooth responsiveness
            Application.targetFrameRate = 60;
            currentSpeed = moveSpeed;
            targetSpeed = moveSpeed;
            targetRotation = transform.rotation;

            if (boostSystem == null)
            {
                boostSystem = GetComponent<BoostSystem>();
            }

            if (powerUpManager == null)
            {
                powerUpManager = GetComponent<PowerUps.PowerUpManager>();
            }

            SetupBoostAudio();
        }

        private void SetupBoostAudio()
        {
            if (boostAudioSource == null)
            {
                boostAudioSource = gameObject.AddComponent<AudioSource>();
                boostAudioSource.playOnAwake = false;
                boostAudioSource.loop = true;
                boostAudioSource.spatialBlend = 0f;
                boostAudioSource.clip = Audio.SoundEffectGenerator.GetOrCreateBoostLoopClip();
                boostAudioSource.volume = 0f;
            }
        }

        private void Update()
        {
            HandleBoostInput();
            HandleSteering();
            HandleMovement();
            HandleBoostAudio();
            ApplyBoundaryConstraint();
        }

        private void HandleBoostInput()
        {
            bool wantsBoost = false;

            if (boostButton != null && boostButton.IsPressed)
            {
                wantsBoost = true;
            }
            else if (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.LeftShift) || Input.GetMouseButton(1))
            {
                wantsBoost = true;
            }

            float baseTarget = moveSpeed;
            if (boostSystem != null)
            {
                boostSystem.SetBoostIntent(wantsBoost);
                baseTarget = boostSystem.TargetSpeed;
            }

            float bonusSpeed = powerUpManager != null ? powerUpManager.SpeedBonus : 0f;
            targetSpeed = baseTarget + bonusSpeed;
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
            float accelRate = IsBoosting ? 20f : 12f;
            currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Time.deltaTime * accelRate);

            // Continuous forward movement in the direction the creature is facing
            transform.position += transform.up * (currentSpeed * Time.deltaTime);
        }

        private void HandleBoostAudio()
        {
            if (boostAudioSource == null) return;

            float sfxVol = Systems.SaveManager.Instance != null && Systems.SaveManager.Instance.Statistics != null
                ? Systems.SaveManager.Instance.Statistics.SFXVolume
                : 1f;

            float targetVol = IsBoosting ? (maxBoostAudioVolume * sfxVol) : 0f;

            if (IsBoosting)
            {
                if (!boostAudioSource.isPlaying)
                {
                    boostAudioSource.Play();
                }
                boostAudioSource.volume = Mathf.MoveTowards(boostAudioSource.volume, targetVol, Time.deltaTime * 6f);
                boostAudioSource.pitch = Mathf.Lerp(boostAudioSource.pitch, 1.15f, Time.deltaTime * 5f);
            }
            else
            {
                boostAudioSource.volume = Mathf.MoveTowards(boostAudioSource.volume, 0f, Time.deltaTime * 8f);
                boostAudioSource.pitch = Mathf.Lerp(boostAudioSource.pitch, 1.0f, Time.deltaTime * 5f);
                if (boostAudioSource.volume <= 0.001f && boostAudioSource.isPlaying)
                {
                    boostAudioSource.Stop();
                }
            }
        }

        private void ApplyBoundaryConstraint()
        {
            if (ArenaManager.Instance != null)
            {
                Vector2 clamped = ArenaManager.Instance.ClampPosition(transform.position, headRadius);
                transform.position = new Vector3(clamped.x, clamped.y, transform.position.z);
            }
        }

        public void BindBoostButton(HoldButton button)
        {
            boostButton = button;
        }

        public void SetSpeed(float newSpeed)
        {
            targetSpeed = Mathf.Max(0f, newSpeed);
        }

        public void ResetSpeed()
        {
            targetSpeed = boostSystem != null ? boostSystem.NormalSpeed : moveSpeed;
        }
    }
}
