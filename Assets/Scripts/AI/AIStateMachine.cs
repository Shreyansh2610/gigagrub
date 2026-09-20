using System;
using UnityEngine;

namespace GigaGrub.AI
{
    public class AIStateMachine : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AIWorldDetector detector;

        [Header("Wander Settings")]
        [Tooltip("Frequency of direction adjustments during Explore state")]
        [SerializeField] private float wanderFrequency = 1.5f;

        [Tooltip("Maximum steering angle variation in degrees during Explore state")]
        [SerializeField] private float wanderAngleJitter = 45f;

        private AIStateType currentState = AIStateType.Explore;
        private Vector2 desiredDirection = Vector2.up;
        private float wanderTimer = 0f;
        private float currentWanderAngle = 0f;

        public AIStateType CurrentState => currentState;
        public Vector2 DesiredDirection => desiredDirection;

        public event Action<AIStateType, AIStateType> OnStateChanged;

        private void Awake()
        {
            if (detector == null)
            {
                detector = GetComponent<AIWorldDetector>();
            }

            currentWanderAngle = UnityEngine.Random.Range(0f, 360f);
            desiredDirection = new Vector2(Mathf.Cos(currentWanderAngle * Mathf.Deg2Rad), Mathf.Sin(currentWanderAngle * Mathf.Deg2Rad));
        }

        public void BindDetector(AIWorldDetector worldDetector)
        {
            detector = worldDetector;
        }

        public void EvaluateState(Vector2 position, Vector2 forwardDirection)
        {
            if (detector == null) return;

            AIStateType previousState = currentState;

            // 1. High Priority: Avoid Boundary
            if (detector.DetectBoundaryDanger(position, forwardDirection, out Vector2 boundaryAvoidance))
            {
                currentState = AIStateType.AvoidBoundary;
                desiredDirection = boundaryAvoidance;
            }
            // 2. High Priority: Avoid other Creatures
            else if (detector.DetectNearbyCreatures(position, forwardDirection, out Vector2 creatureAvoidance))
            {
                currentState = AIStateType.AvoidCreature;
                desiredDirection = creatureAvoidance;
            }
            // 3. Medium Priority: Seek Food
            else if (detector.FindBestNearbyFood(position, out Vector2 targetFoodPos))
            {
                currentState = AIStateType.SeekFood;
                desiredDirection = (targetFoodPos - position).normalized;
            }
            // 4. Default: Explore / Wander
            else
            {
                currentState = AIStateType.Explore;
                UpdateWanderDirection(position, forwardDirection);
            }

            if (currentState != previousState)
            {
                OnStateChanged?.Invoke(currentState, previousState);
            }
        }

        private void UpdateWanderDirection(Vector2 position, Vector2 forwardDirection)
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= (1f / Mathf.Max(0.1f, wanderFrequency)))
            {
                wanderTimer = 0f;
                float angleDelta = UnityEngine.Random.Range(-wanderAngleJitter, wanderAngleJitter);
                currentWanderAngle = Mathf.Repeat(currentWanderAngle + angleDelta, 360f);

                Vector2 wanderDir = new Vector2(
                    Mathf.Cos(currentWanderAngle * Mathf.Deg2Rad),
                    Mathf.Sin(currentWanderAngle * Mathf.Deg2Rad)
                );

                // Subtle center-seeking bias when wandering far from arena origin
                if (position.sqrMagnitude > 400f) // > 20 units from center
                {
                    Vector2 toCenter = -position.normalized;
                    wanderDir = Vector2.Lerp(wanderDir, toCenter, 0.4f).normalized;
                }

                desiredDirection = wanderDir;
            }
        }

        public void ForceState(AIStateType state, Vector2 direction)
        {
            AIStateType oldState = currentState;
            currentState = state;
            desiredDirection = direction;

            if (currentState != oldState)
            {
                OnStateChanged?.Invoke(currentState, oldState);
            }
        }
    }
}
