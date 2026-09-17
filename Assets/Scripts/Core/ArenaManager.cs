using UnityEngine;

namespace GigaGrub.Core
{
    [ExecuteAlways]
    public class ArenaManager : MonoBehaviour
    {
        public static ArenaManager Instance { get; private set; }

        [Header("Arena Dimensions")]
        [Tooltip("Total width and height of the playable arena in world units")]
        [SerializeField] private Vector2 arenaSize = new Vector2(100f, 100f);

        [Header("Visuals")]
        [SerializeField] private Color boundaryColor = new Color(0.1f, 0.8f, 1f, 0.9f);
        [SerializeField] private float boundaryWidth = 0.3f;
        [SerializeField] private Material boundaryMaterial;

        [Header("Colliders")]
        [SerializeField] private bool generateColliders = true;
        [SerializeField] private float wallThickness = 2f;

        private LineRenderer lineRenderer;
        private Transform wallsParent;

        public Vector2 ArenaSize => arenaSize;
        public Vector2 HalfSize => arenaSize * 0.5f;

        public Rect Bounds => new Rect(-arenaSize.x * 0.5f, -arenaSize.y * 0.5f, arenaSize.x, arenaSize.y);

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupBoundaryVisuals();
            if (generateColliders)
            {
                SetupBoundaryColliders();
            }
        }

        private void OnValidate()
        {
            if (arenaSize.x < 10f) arenaSize.x = 10f;
            if (arenaSize.y < 10f) arenaSize.y = 10f;

            if (Application.isEditor && !Application.isPlaying)
            {
                SetupBoundaryVisuals();
            }
        }

        public void SetupBoundaryVisuals()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
                if (lineRenderer == null)
                {
                    lineRenderer = gameObject.AddComponent<LineRenderer>();
                }
            }

            lineRenderer.useWorldSpace = true;
            lineRenderer.loop = true;
            lineRenderer.positionCount = 4;
            lineRenderer.startWidth = boundaryWidth;
            lineRenderer.endWidth = boundaryWidth;
            lineRenderer.startColor = boundaryColor;
            lineRenderer.endColor = boundaryColor;
            lineRenderer.sortingOrder = 50;

            if (boundaryMaterial != null)
            {
                lineRenderer.sharedMaterial = boundaryMaterial;
            }

            Vector2 half = HalfSize;
            Vector3[] corners = new Vector3[4]
            {
                new Vector3(-half.x, -half.y, 0f),
                new Vector3( half.x, -half.y, 0f),
                new Vector3( half.x,  half.y, 0f),
                new Vector3(-half.x,  half.y, 0f)
            };

            lineRenderer.SetPositions(corners);
        }

        public void SetupBoundaryColliders()
        {
            if (wallsParent != null)
            {
                DestroyImmediate(wallsParent.gameObject);
            }

            GameObject wallsGo = new GameObject("BoundaryWalls");
            wallsGo.transform.SetParent(transform, false);
            wallsParent = wallsGo.transform;

            Vector2 half = HalfSize;

            // Top Wall
            CreateWall("TopWall", new Vector2(0f, half.y + wallThickness * 0.5f), new Vector2(arenaSize.x + wallThickness * 2f, wallThickness));
            // Bottom Wall
            CreateWall("BottomWall", new Vector2(0f, -half.y - wallThickness * 0.5f), new Vector2(arenaSize.x + wallThickness * 2f, wallThickness));
            // Left Wall
            CreateWall("LeftWall", new Vector2(-half.x - wallThickness * 0.5f, 0f), new Vector2(wallThickness, arenaSize.y));
            // Right Wall
            CreateWall("RightWall", new Vector2(half.x + wallThickness * 0.5f, 0f), new Vector2(wallThickness, arenaSize.y));
        }

        private void CreateWall(string name, Vector2 localPos, Vector2 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(wallsParent, false);
            wall.transform.localPosition = localPos;

            BoxCollider2D col = wall.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        public Vector2 ClampPosition(Vector2 position, float margin = 0.5f)
        {
            Vector2 half = HalfSize;
            float clampedX = Mathf.Clamp(position.x, -half.x + margin, half.x - margin);
            float clampedY = Mathf.Clamp(position.y, -half.y + margin, half.y - margin);
            return new Vector2(clampedX, clampedY);
        }

        public bool IsInside(Vector2 position, float margin = 0f)
        {
            Vector2 half = HalfSize;
            return position.x >= -half.x + margin && position.x <= half.x - margin &&
                   position.y >= -half.y + margin && position.y <= half.y - margin;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, new Vector3(arenaSize.x, arenaSize.y, 0.1f));
        }
    }
}
