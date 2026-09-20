using UnityEngine;
using GigaGrub.Core;
using GigaGrub.Player;

namespace GigaGrub.AI
{
    public class AIController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [Tooltip("Forward movement speed in units per second")]
        [SerializeField] private float moveSpeed = 4.8f;

        [Tooltip("Steering turn speed in degrees per second")]
        [SerializeField] private float turnSpeed = 280f;

        [Tooltip("Radius of the creature head for boundary collision")]
        [SerializeField] private float headRadius = 0.5f;

        [Header("AI Decision Timings")]
        [Tooltip("Time interval between AI sensor queries and state machine evaluation in seconds")]
        [SerializeField] private float decisionInterval = 0.15f;

        [Header("References")]
        [SerializeField] private AIStateMachine stateMachine;
        [SerializeField] private AIWorldDetector worldDetector;
        [SerializeField] private PlayerBody creatureBody;

        private float decisionTimer = 0f;
        private float currentSpeed;

        public float MoveSpeed => moveSpeed;
        public float TurnSpeed => turnSpeed;
        public float DecisionInterval => decisionInterval;
        public AIStateMachine StateMachine => stateMachine;
        public PlayerBody CreatureBody => creatureBody;
        public int CurrentScore => creatureBody != null ? creatureBody.CurrentScore : 0;
        public int CurrentLength => creatureBody != null ? creatureBody.CurrentLength : 0;

        private void Awake()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<AIStateMachine>();
                if (stateMachine == null)
                {
                    stateMachine = gameObject.AddComponent<AIStateMachine>();
                }
            }

            if (worldDetector == null)
            {
                worldDetector = GetComponent<AIWorldDetector>();
                if (worldDetector == null)
                {
                    worldDetector = gameObject.AddComponent<AIWorldDetector>();
                }
            }

            if (creatureBody == null)
            {
                creatureBody = GetComponent<PlayerBody>();
            }

            currentSpeed = moveSpeed;

            // Stagger decision timers across AI instances to eliminate CPU spikes
            decisionTimer = UnityEngine.Random.Range(0f, decisionInterval);
        }

        private void Update()
        {
            UpdateDecisionTimer();
            HandleSteering();
            HandleMovement();
            ApplyBoundaryConstraint();
        }

        private void UpdateDecisionTimer()
        {
            decisionTimer += Time.deltaTime;
            if (decisionTimer >= decisionInterval)
            {
                decisionTimer = 0f;
                if (stateMachine != null)
                {
                    stateMachine.EvaluateState(transform.position, transform.up);
                }
            }
        }

        private void HandleSteering()
        {
            if (stateMachine == null) return;

            Vector2 desiredDir = stateMachine.DesiredDirection;
            if (desiredDir.sqrMagnitude > 0.001f)
            {
                float targetAngle = Mathf.Atan2(-desiredDir.x, desiredDir.y) * Mathf.Rad2Deg;
                Quaternion targetRotation = Quaternion.Euler(0f, 0f, targetAngle);

                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRotation,
                    turnSpeed * Time.deltaTime
                );
            }
        }

        private void HandleMovement()
        {
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
            moveSpeed = Mathf.Max(1f, newSpeed);
            currentSpeed = moveSpeed;
        }

        public void SetDecisionInterval(float interval)
        {
            decisionInterval = Mathf.Max(0.02f, interval);
        }

        public void SetTurnSpeed(float newTurnSpeed)
        {
            turnSpeed = Mathf.Max(30f, newTurnSpeed);
        }

        public void StepDecision()
        {
            if (stateMachine != null)
            {
                stateMachine.EvaluateState(transform.position, transform.up);
            }
        }
    }
}
