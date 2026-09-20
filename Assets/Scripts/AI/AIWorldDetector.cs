using UnityEngine;
using GigaGrub.Core;
using GigaGrub.Food;
using GigaGrub.Player;

namespace GigaGrub.AI
{
    public class AIWorldDetector : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Vision radius within which AI can perceive food")]
        [SerializeField] private float foodVisionRadius = 12f;

        [Tooltip("Detection radius for sensing other creature segments and heads")]
        [SerializeField] private float creatureDangerRadius = 3.5f;

        [Tooltip("Lookahead distance for detecting arena boundary walls")]
        [SerializeField] private float boundaryDangerDistance = 6f;

        [Tooltip("Layer mask for detecting creature colliders")]
        [SerializeField] private LayerMask creatureLayerMask = ~0;

        private readonly Collider2D[] hitBuffer = new Collider2D[24];
        private PlayerBody ownerBody;
        private Transform ownerTransform;

        private void Awake()
        {
            EnsureReferences();
        }

        private void EnsureReferences()
        {
            if (ownerTransform == null)
            {
                ownerTransform = transform;
            }

            if (ownerBody == null)
            {
                ownerBody = GetComponent<PlayerBody>();
                if (ownerBody == null)
                {
                    ownerBody = GetComponentInParent<PlayerBody>();
                }
            }
        }

        public void SetDetectionSettings(float foodRadius, float creatureRadius, float boundaryDistance)
        {
            foodVisionRadius = Mathf.Max(1f, foodRadius);
            creatureDangerRadius = Mathf.Max(0.5f, creatureRadius);
            boundaryDangerDistance = Mathf.Max(1f, boundaryDistance);
        }

        public bool DetectBoundaryDanger(Vector2 currentPosition, Vector2 forwardDir, out Vector2 avoidanceDirection)
        {
            avoidanceDirection = Vector2.zero;

            Vector2 arenaHalf = Vector2.one * 45f;
            if (ArenaManager.Instance != null)
            {
                arenaHalf = ArenaManager.Instance.HalfSize;
            }

            float safeMarginX = arenaHalf.x - boundaryDangerDistance;
            float safeMarginY = arenaHalf.y - boundaryDangerDistance;

            bool isNearWall = false;
            Vector2 normal = Vector2.zero;

            // Check X boundaries
            if (currentPosition.x > safeMarginX)
            {
                normal += Vector2.left * (currentPosition.x - safeMarginX);
                isNearWall = true;
            }
            else if (currentPosition.x < -safeMarginX)
            {
                normal += Vector2.right * (-safeMarginX - currentPosition.x);
                isNearWall = true;
            }

            // Check Y boundaries
            if (currentPosition.y > safeMarginY)
            {
                normal += Vector2.down * (currentPosition.y - safeMarginY);
                isNearWall = true;
            }
            else if (currentPosition.y < -safeMarginY)
            {
                normal += Vector2.up * (-safeMarginY - currentPosition.y);
                isNearWall = true;
            }

            if (isNearWall)
            {
                // Bias avoidance vector towards arena center
                Vector2 toCenter = -currentPosition.normalized;
                avoidanceDirection = (normal.normalized + toCenter * 0.5f).normalized;
                return true;
            }

            return false;
        }

        public bool DetectNearbyCreatures(Vector2 currentPosition, Vector2 forwardDir, out Vector2 avoidanceDirection)
        {
            EnsureReferences();
            avoidanceDirection = Vector2.zero;

            ContactFilter2D filter = new ContactFilter2D();
            filter.SetLayerMask(creatureLayerMask);
            filter.useTriggers = true;

            int hitCount = Physics2D.OverlapCircle(
                currentPosition,
                creatureDangerRadius,
                filter,
                hitBuffer
            );

            if (hitCount == 0) return false;

            Vector2 totalRepulsion = Vector2.zero;
            int dangerCount = 0;

            for (int i = 0; i < hitCount; i++)
            {
                Collider2D col = hitBuffer[i];
                if (col == null) continue;

                // Ignore own head collider
                if (ownerTransform != null && (col.transform == ownerTransform || col.transform.IsChildOf(ownerTransform)))
                {
                    continue;
                }

                // Ignore own body segments
                if (ownerBody != null && ownerBody.ActiveSegments != null)
                {
                    bool isOwnSegment = false;
                    for (int s = 0; s < ownerBody.ActiveSegments.Count; s++)
                    {
                        var seg = ownerBody.ActiveSegments[s];
                        if (seg != null && seg.transform == col.transform)
                        {
                            isOwnSegment = true;
                            break;
                        }
                    }
                    if (isOwnSegment) continue;
                }

                // Calculate repulsion vector inversely proportional to distance
                Vector2 obstaclePos = col.transform.position;
                Vector2 diff = currentPosition - obstaclePos;
                float sqrDist = diff.sqrMagnitude;

                if (sqrDist > 0.0001f)
                {
                    float dist = Mathf.Sqrt(sqrDist);
                    float weight = (creatureDangerRadius - dist) / creatureDangerRadius;
                    totalRepulsion += (diff / dist) * Mathf.Max(0.1f, weight);
                    dangerCount++;
                }
            }

            if (dangerCount > 0 && totalRepulsion.sqrMagnitude > 0.001f)
            {
                avoidanceDirection = totalRepulsion.normalized;
                return true;
            }

            return false;
        }

        public bool FindBestNearbyFood(Vector2 currentPosition, out Vector2 foodPosition)
        {
            foodPosition = Vector2.zero;

            if (FoodSpawner.Instance == null || FoodSpawner.Instance.ActiveFoods == null)
            {
                return false;
            }

            var activeFoods = FoodSpawner.Instance.ActiveFoods;
            int count = activeFoods.Count;
            if (count == 0) return false;

            float maxVisionSqr = foodVisionRadius * foodVisionRadius;
            float bestScore = float.MinValue;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                Food.Food food = activeFoods[i];
                if (food == null || !food.gameObject.activeSelf || food.IsConsumed) continue;

                Vector2 pos = food.transform.position;
                float sqrDist = (pos - currentPosition).sqrMagnitude;

                if (sqrDist <= maxVisionSqr)
                {
                    int foodVal = food.Data != null ? food.Data.ScoreValue : 10;
                    float distance = Mathf.Max(0.5f, Mathf.Sqrt(sqrDist));
                    float utility = (float)foodVal / distance;

                    if (utility > bestScore)
                    {
                        bestScore = utility;
                        foodPosition = pos;
                        found = true;
                    }
                }
            }

            return found;
        }
    }
}
