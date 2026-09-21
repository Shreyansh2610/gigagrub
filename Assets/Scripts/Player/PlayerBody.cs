using System.Collections.Generic;
using UnityEngine;
using GigaGrub.AI;
using GigaGrub.Audio;
using GigaGrub.Core;
using GigaGrub.Food;
using GigaGrub.Systems;

namespace GigaGrub.Player
{
    public class PlayerBody : MonoBehaviour
    {
        [Header("Entity Type")]
        [Tooltip("True if controlled by human player, false if controlled by AI")]
        [SerializeField] private bool isPlayer = true;

        [Header("Length & Growth Settings")]
        [Tooltip("Starting number of body segments")]
        [SerializeField] private int startingLength = 10;

        [Tooltip("Maximum allowed body length")]
        [SerializeField] private int maxLength = 600;

        [Tooltip("Distance between consecutive body segments")]
        [SerializeField] private float segmentSpacing = 0.45f;

        [Tooltip("Minimum distance head must move before recording a new trail node")]
        [SerializeField] private float stepDistance = 0.05f;

        [Tooltip("Global multiplier applied to food growth values")]
        [SerializeField] private int growthMultiplier = 1;

        [Tooltip("Speed at which newly added segments scale in")]
        [SerializeField] private float smoothGrowthSpeed = 8f;

        [Header("Visuals")]
        [SerializeField] private bool enableTaper = true;
        [SerializeField] private float minTailScale = 0.65f;
        [SerializeField] private int headSortingOrder = 100;
        [SerializeField] private Color headTintColor = Color.white;
        [SerializeField] private Color bodyTintColor = Color.white;

        [Header("Feedback Effects")]
        [SerializeField] private bool enableAudioFeedback = true;
        [SerializeField] private bool enableVisualPunch = true;
        [SerializeField] private float headPunchScale = 1.16f;

        [Header("References")]
        [SerializeField] private GameObject segmentPrefab;
        [SerializeField] private Transform headTransform;
        [SerializeField] private Transform segmentsParent;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GrowthSystem growthSystem;
        [SerializeField] private CreatureCollision creatureCollision;
        [SerializeField] private CreatureDeath creatureDeath;
        [SerializeField] private SpriteRenderer headSpriteRenderer;

        private readonly List<PlayerSegment> activeSegments = new List<PlayerSegment>();
        private readonly Queue<PlayerSegment> segmentPool = new Queue<PlayerSegment>();

        private struct TrailPoint
        {
            public Vector3 Position;
            public Quaternion Rotation;

            public TrailPoint(Vector3 pos, Quaternion rot)
            {
                Position = pos;
                Rotation = rot;
            }
        }

        private TrailPoint[] trailBuffer = new TrailPoint[8192];
        private int trailHead = 0;
        private int trailCount = 0;
        private Vector3 lastRecordedPosition;

        private int currentScore = 0;
        private Vector3 originalHeadScale = Vector3.one;
        private bool isHeadPunching = false;
        private bool isInitialized = false;

        public bool IsPlayer => isPlayer;
        public int CurrentLength => activeSegments.Count;
        public int MaxLength => maxLength;
        public float SegmentSpacing => segmentSpacing;
        public int GrowthMultiplier => growthMultiplier;
        public IReadOnlyList<PlayerSegment> ActiveSegments => activeSegments;
        public int CurrentScore => isPlayer && ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : currentScore;
        public GrowthSystem GrowthSystem => growthSystem;
        public CreatureCollision Collision => creatureCollision;
        public CreatureDeath Death => creatureDeath;

        public event System.Action<int, int> OnScoreChanged;
        public event System.Action<FoodData> OnFoodEaten;

        private void Awake()
        {
            InitializeRuntime();
        }

        private void Start()
        {
            if (!isInitialized)
            {
                InitializeRuntime();
            }
        }

        public void InitializeRuntime()
        {
            if (headTransform == null)
            {
                headTransform = transform;
            }

            if (headSpriteRenderer == null)
            {
                headSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (headSpriteRenderer != null && headTintColor != Color.white)
            {
                headSpriteRenderer.color = headTintColor;
            }

            originalHeadScale = headTransform.localScale;

            if (segmentsParent == null)
            {
                GameObject parentGo = new GameObject($"{gameObject.name}_Segments");
                segmentsParent = parentGo.transform;
            }

            if (audioSource == null && isPlayer)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null && enableAudioFeedback)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 0f; // 2D Sound
                }
            }

            if (growthSystem == null)
            {
                growthSystem = GetComponent<GrowthSystem>();
                if (growthSystem == null)
                {
                    growthSystem = gameObject.AddComponent<GrowthSystem>();
                }
            }
            growthSystem.BindPlayerBody(this);

            if (creatureDeath == null)
            {
                creatureDeath = GetComponent<CreatureDeath>();
                if (creatureDeath == null)
                {
                    creatureDeath = gameObject.AddComponent<CreatureDeath>();
                }
            }

            if (creatureCollision == null)
            {
                creatureCollision = GetComponent<CreatureCollision>();
                if (creatureCollision == null)
                {
                    creatureCollision = gameObject.AddComponent<CreatureCollision>();
                }
            }
            creatureCollision.BindComponents(this, creatureDeath);

            PrewarmPool(startingLength + 50);
            InitializeTrail();

            if (activeSegments.Count == 0)
            {
                // Spawn initial segments (instant scale without animation)
                for (int i = 0; i < startingLength; i++)
                {
                    AddSegmentInternal(false);
                }
            }

            UpdateSegments();
            isInitialized = true;

            if (RankingManager.Instance != null)
            {
                RankingManager.Instance.RegisterCreature(this);
            }
        }

        public void SetIsPlayer(bool player)
        {
            isPlayer = player;
        }

        public void SetColor(Color headColor, Color bodyColor)
        {
            headTintColor = headColor;
            bodyTintColor = bodyColor;

            if (headSpriteRenderer == null)
            {
                headSpriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (headSpriteRenderer != null)
            {
                headSpriteRenderer.color = headTintColor;
            }

            for (int i = 0; i < activeSegments.Count; i++)
            {
                if (activeSegments[i] != null)
                {
                    activeSegments[i].SetColor(bodyTintColor);
                }
            }
        }

        private void LateUpdate()
        {
            RecordHeadPosition();
            UpdateSegments();
            UpdateHeadPunch();
        }

        public void PrewarmPool(int count)
        {
            if (segmentPrefab == null) return;

            for (int i = 0; i < count; i++)
            {
                CreatePooledSegment();
            }
        }

        private PlayerSegment CreatePooledSegment()
        {
            if (segmentPrefab == null) return null;

            GameObject go = Instantiate(segmentPrefab, Vector3.zero, Quaternion.identity, segmentsParent);
            go.SetActive(false);

            PlayerSegment segment = go.GetComponent<PlayerSegment>();
            if (segment == null)
            {
                segment = go.AddComponent<PlayerSegment>();
            }

            segment.SetOwner(this);
            if (bodyTintColor != Color.white)
            {
                segment.SetColor(bodyTintColor);
            }

            segmentPool.Enqueue(segment);
            return segment;
        }

        private void InitializeTrail()
        {
            trailCount = 0;
            trailHead = 0;

            Vector3 startPos = headTransform != null ? headTransform.position : transform.position;
            Quaternion startRot = headTransform != null ? headTransform.rotation : transform.rotation;
            Vector3 backwardDir = -(startRot * Vector3.up);

            // Pre-fill trail backwards so segments have a natural layout on start
            int prefillSteps = Mathf.CeilToInt((startingLength + 10) * segmentSpacing / stepDistance) + 20;
            if (prefillSteps > trailBuffer.Length)
            {
                ResizeBuffer(prefillSteps + 512);
            }

            for (int i = prefillSteps - 1; i >= 0; i--)
            {
                Vector3 pos = startPos + backwardDir * (i * stepDistance);
                PushTrailPoint(pos, startRot);
            }

            lastRecordedPosition = startPos;
        }

        private void PushTrailPoint(Vector3 pos, Quaternion rot)
        {
            if (trailCount >= trailBuffer.Length)
            {
                ResizeBuffer(trailBuffer.Length * 2);
            }

            trailHead = (trailHead + 1) % trailBuffer.Length;
            trailBuffer[trailHead] = new TrailPoint(pos, rot);
            if (trailCount < trailBuffer.Length)
            {
                trailCount++;
            }
        }

        private void ResizeBuffer(int newCapacity)
        {
            TrailPoint[] newBuffer = new TrailPoint[newCapacity];
            for (int i = 0; i < trailCount; i++)
            {
                int oldIdx = (trailHead - (trailCount - 1 - i) + trailBuffer.Length) % trailBuffer.Length;
                newBuffer[i] = trailBuffer[oldIdx];
            }

            trailBuffer = newBuffer;
            trailHead = trailCount > 0 ? trailCount - 1 : 0;
        }

        private void RecordHeadPosition()
        {
            if (headTransform == null) return;
            Vector3 currentPos = headTransform.position;
            float dist = Vector3.Distance(currentPos, lastRecordedPosition);

            if (dist >= stepDistance)
            {
                int steps = Mathf.Min(Mathf.FloorToInt(dist / stepDistance), 50);
                Vector3 stepVector = (currentPos - lastRecordedPosition).normalized * stepDistance;
                Quaternion currentRot = headTransform.rotation;

                for (int s = 1; s <= steps; s++)
                {
                    Vector3 interpPos = lastRecordedPosition + stepVector * s;
                    PushTrailPoint(interpPos, currentRot);
                }

                lastRecordedPosition = lastRecordedPosition + stepVector * steps;
            }
        }

        private void UpdateSegments()
        {
            int segmentCount = activeSegments.Count;
            if (segmentCount == 0 || trailCount == 0 || headTransform == null) return;

            Vector3 currentHeadPos = headTransform.position;
            Quaternion currentHeadRot = headTransform.rotation;

            float accumulatedDistance = 0f;
            Vector3 prevPos = currentHeadPos;
            Quaternion prevRot = currentHeadRot;
            int trailIndex = 0;

            // Pre-calculate stable extrapolation fallback direction
            Vector3 tailBackwardDir = -(currentHeadRot * Vector3.up);
            if (trailCount >= 2)
            {
                int lastIdx = (trailHead - (trailCount - 1) + trailBuffer.Length) % trailBuffer.Length;
                int prevIdx = (trailHead - (trailCount - 2) + trailBuffer.Length) % trailBuffer.Length;
                Vector3 diff = trailBuffer[lastIdx].Position - trailBuffer[prevIdx].Position;
                if (diff.sqrMagnitude > 0.0001f)
                {
                    tailBackwardDir = diff.normalized;
                }
            }

            for (int i = 0; i < segmentCount; i++)
            {
                float targetDistance = (i + 1) * segmentSpacing;
                Vector3 targetPos;
                Quaternion targetRot;
                bool found = false;

                while (trailIndex < trailCount)
                {
                    int bufferIdx = (trailHead - trailIndex + trailBuffer.Length) % trailBuffer.Length;
                    TrailPoint point = trailBuffer[bufferIdx];

                    float segDist = Vector3.Distance(prevPos, point.Position);
                    if (accumulatedDistance + segDist >= targetDistance)
                    {
                        float remaining = targetDistance - accumulatedDistance;
                        float t = segDist > 0.0001f ? remaining / segDist : 0f;

                        targetPos = Vector3.Lerp(prevPos, point.Position, t);
                        targetRot = Quaternion.Slerp(prevRot, point.Rotation, t);
                        found = true;

                        PlayerSegment segment = activeSegments[i];
                        if (segment != null)
                        {
                            segment.transform.position = targetPos;
                            segment.transform.rotation = targetRot;
                        }
                        break;
                    }

                    accumulatedDistance += segDist;
                    prevPos = point.Position;
                    prevRot = point.Rotation;
                    trailIndex++;
                }

                if (!found)
                {
                    float remainingExtrapDist = targetDistance - accumulatedDistance;
                    targetPos = prevPos + tailBackwardDir * remainingExtrapDist;
                    targetRot = prevRot;

                    PlayerSegment segment = activeSegments[i];
                    if (segment != null)
                    {
                        segment.transform.position = targetPos;
                        segment.transform.rotation = targetRot;
                    }
                }
            }
        }

        private void GetPointAtDistance(Vector3 headPos, Quaternion headRot, float targetDistance, out Vector3 resultPos, out Quaternion resultRot)
        {
            float accumulatedDistance = 0f;
            Vector3 prevPos = headPos;
            Quaternion prevRot = headRot;

            for (int k = 0; k < trailCount; k++)
            {
                int bufferIdx = (trailHead - k + trailBuffer.Length) % trailBuffer.Length;
                TrailPoint point = trailBuffer[bufferIdx];

                float segDist = Vector3.Distance(prevPos, point.Position);
                if (accumulatedDistance + segDist >= targetDistance)
                {
                    float remaining = targetDistance - accumulatedDistance;
                    float t = segDist > 0.0001f ? remaining / segDist : 0f;

                    resultPos = Vector3.Lerp(prevPos, point.Position, t);
                    resultRot = Quaternion.Slerp(prevRot, point.Rotation, t);
                    return;
                }

                accumulatedDistance += segDist;
                prevPos = point.Position;
                prevRot = point.Rotation;
            }

            // Stable extrapolation
            Vector3 tailBackwardDir = -(headRot * Vector3.up);
            if (trailCount >= 2)
            {
                int lastIdx = (trailHead - (trailCount - 1) + trailBuffer.Length) % trailBuffer.Length;
                int prevIdx = (trailHead - (trailCount - 2) + trailBuffer.Length) % trailBuffer.Length;
                Vector3 diff = trailBuffer[lastIdx].Position - trailBuffer[prevIdx].Position;
                if (diff.sqrMagnitude > 0.0001f)
                {
                    tailBackwardDir = diff.normalized;
                }
            }

            float remainingExtrapDist = targetDistance - accumulatedDistance;
            resultPos = prevPos + tailBackwardDir * remainingExtrapDist;
            resultRot = prevRot;
        }

        public void SetSettings(int startLen, int maxLen, float spacing, int growthMult)
        {
            startingLength = Mathf.Max(1, startLen);
            maxLength = Mathf.Max(startingLength, maxLen);
            segmentSpacing = Mathf.Max(0.1f, spacing);
            growthMultiplier = Mathf.Max(1, growthMult);
        }

        public void AddSegment()
        {
            AddSegmentInternal(true);
        }

        public void AddSegmentInternal(bool smoothScaleIn)
        {
            if (activeSegments.Count >= maxLength) return;

            if (segmentPool.Count == 0)
            {
                CreatePooledSegment();
            }

            PlayerSegment newSegment = segmentPool.Dequeue();
            int segmentIndex = activeSegments.Count;

            int order = headSortingOrder - (segmentIndex + 1);
            float scaleMultiplier = CalculateSegmentScale(segmentIndex, activeSegments.Count + 1);

            Vector3 spawnPos = headTransform != null ? headTransform.position : transform.position;
            Quaternion spawnRot = headTransform != null ? headTransform.rotation : transform.rotation;

            if (activeSegments.Count > 0 && activeSegments[activeSegments.Count - 1] != null)
            {
                spawnPos = activeSegments[activeSegments.Count - 1].transform.position;
                spawnRot = activeSegments[activeSegments.Count - 1].transform.rotation;
            }

            newSegment.OnSpawnFromPool(this, spawnPos, spawnRot, order, scaleMultiplier, smoothScaleIn, smoothGrowthSpeed);
            if (bodyTintColor != Color.white)
            {
                newSegment.SetColor(bodyTintColor);
            }
            activeSegments.Add(newSegment);

            // Re-taper existing segments smoothly if needed
            if (enableTaper && smoothScaleIn)
            {
                RefreshSegmentTaper();
            }

            // Ensure trail has enough pre-buffered capacity for new length
            int requiredTrailSteps = Mathf.CeilToInt((activeSegments.Count + 10) * segmentSpacing / stepDistance);
            if (requiredTrailSteps > trailBuffer.Length)
            {
                ResizeBuffer(requiredTrailSteps + 512);
            }
        }

        private float CalculateSegmentScale(int segmentIndex, int totalCount)
        {
            if (!enableTaper || totalCount <= 1) return 1f;
            float t = Mathf.Clamp01((float)segmentIndex / Mathf.Max(1, totalCount - 1));
            return Mathf.Lerp(1f, minTailScale, t);
        }

        private void RefreshSegmentTaper()
        {
            int count = activeSegments.Count;
            for (int i = 0; i < count; i++)
            {
                if (activeSegments[i] != null)
                {
                    float targetScale = CalculateSegmentScale(i, count);
                    activeSegments[i].SetScaleMultiplier(targetScale, false, smoothGrowthSpeed);
                }
            }
        }

        public void AddSegments(int count)
        {
            int toAdd = Mathf.Min(count, maxLength - activeSegments.Count);
            for (int i = 0; i < toAdd; i++)
            {
                AddSegmentInternal(true);
            }
        }

        public void RemoveSegment()
        {
            if (activeSegments.Count == 0) return;

            int lastIdx = activeSegments.Count - 1;
            PlayerSegment segment = activeSegments[lastIdx];
            activeSegments.RemoveAt(lastIdx);

            if (segment != null)
            {
                segment.OnReturnToPool();
                segmentPool.Enqueue(segment);
            }
        }

        public void ClearAllSegmentsToPool()
        {
            for (int i = activeSegments.Count - 1; i >= 0; i--)
            {
                PlayerSegment seg = activeSegments[i];
                if (seg != null)
                {
                    seg.OnReturnToPool();
                    segmentPool.Enqueue(seg);
                }
            }
            activeSegments.Clear();
        }

        public void OnEatFood(FoodData foodData)
        {
            if (foodData == null) return;

            // 1. Scoring update (individual score for AI, centralized for player)
            if (isPlayer && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.AddScore(foodData.ScoreValue);
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.RecordFoodCollected();
                }
            }
            else
            {
                currentScore += foodData.ScoreValue;
                OnScoreChanged?.Invoke(currentScore, foodData.ScoreValue);
            }

            OnFoodEaten?.Invoke(foodData);

            // 2. Growth system update
            int segmentsToAdd = foodData.GrowthValue * growthMultiplier;
            if (growthSystem != null)
            {
                growthSystem.Grow(segmentsToAdd);
            }
            else if (segmentsToAdd > 0)
            {
                AddSegments(segmentsToAdd);
            }

            // 3. Audio feedback (player only to avoid chaotic overlapping sound loops)
            if (isPlayer && enableAudioFeedback && audioSource != null)
            {
                SoundEffectGenerator.PlayEatSound(audioSource, 0.7f, 0.15f);
            }

            // 4. Visual head punch feedback
            if (enableVisualPunch && headTransform != null)
            {
                headTransform.localScale = originalHeadScale * headPunchScale;
                isHeadPunching = true;
            }

            // 5. Visual particle burst feedback
            EatingEffect.Spawn(transform.position, foodData.FoodColor, foodData.ScaleMultiplier);

            // 6. Screen micro-feedback impulse (player only)
            if (isPlayer && Camera.main != null)
            {
                CameraFollow cam = Camera.main.GetComponent<CameraFollow>();
                if (cam != null)
                {
                    float shakeMag = foodData.FoodType == FoodType.Mega ? 0.25f : (foodData.FoodType == FoodType.Super ? 0.15f : 0.06f);
                    cam.TriggerShake(shakeMag, 0.12f);
                }
            }
        }

        private void UpdateHeadPunch()
        {
            if (isHeadPunching && headTransform != null)
            {
                headTransform.localScale = Vector3.Lerp(headTransform.localScale, originalHeadScale, Time.deltaTime * 10f);
                if (Vector3.Distance(headTransform.localScale, originalHeadScale) < 0.01f)
                {
                    headTransform.localScale = originalHeadScale;
                    isHeadPunching = false;
                }
            }
        }

        public void ResetScore()
        {
            currentScore = 0;
            if (isPlayer && ScoreManager.Instance != null)
            {
                ScoreManager.Instance.ResetScore();
            }
            else
            {
                OnScoreChanged?.Invoke(currentScore, 0);
            }
        }

        public void ResetCreature(Vector2 spawnPosition)
        {
            // 1. Activate GameObject and set position & rotation
            gameObject.SetActive(true);
            transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);
            transform.rotation = Quaternion.identity;

            // 2. Reset CreatureDeath and colliders
            if (creatureDeath != null)
            {
                creatureDeath.ResetState();
            }

            // 3. Reset Controller
            PlayerController controller = GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.enabled = true;
                controller.ResetSpeed();
            }

            AIController ai = GetComponent<AIController>();
            if (ai != null)
            {
                ai.enabled = true;
            }

            // 4. Clear existing segments back to pool
            ClearAllSegmentsToPool();

            // 5. Reset trail history buffer to spawn position
            InitializeTrail();

            // 6. Spawn initial starting length segments
            for (int i = 0; i < startingLength; i++)
            {
                AddSegmentInternal(false);
            }
            UpdateSegments();

            // 7. Reset GrowthSystem
            if (growthSystem != null)
            {
                growthSystem.ResetGrowth(startingLength);
            }

            // 8. Reset Score
            ResetScore();

            // 9. Re-register in RankingManager
            if (RankingManager.Instance != null)
            {
                RankingManager.Instance.RegisterCreature(this);
            }
        }

        private void OnDestroy()
        {
            if (segmentsParent != null)
            {
                Destroy(segmentsParent.gameObject);
            }
        }
    }
}
