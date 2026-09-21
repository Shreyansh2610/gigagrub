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
        [SerializeField] private Color backgroundColor = new Color(0.06f, 0.08f, 0.12f, 1f);

        [Header("Colliders")]
        [SerializeField] private bool generateColliders = true;
        [SerializeField] private float wallThickness = 2f;

        private LineRenderer lineRenderer;
        private Transform wallsParent;
        private Transform backgroundTransform;

        public Vector2 ArenaSize => arenaSize;
        public Vector2 HalfSize => arenaSize * 0.5f;

        public Rect Bounds => new Rect(-arenaSize.x * 0.5f, -arenaSize.y * 0.5f, arenaSize.x, arenaSize.y);

        private Transform decorationsParent;
        private static Sprite solidSquareSprite;
        private static Sprite arenaGridSprite;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetupBackground();
            SetupBoundaryVisuals();
            SetupArenaDecorations();
            if (generateColliders)
            {
                SetupBoundaryColliders();
            }
        }

        private void OnValidate()
        {
            if (arenaSize.x < 10f) arenaSize.x = 10f;
            if (arenaSize.y < 10f) arenaSize.y = 10f;
        }

        public void SetupBackground()
        {
            // Clean up any old duplicate BackgroundGrid objects to ensure no circular/vignetted sprites exist
            Transform[] children = GetComponentsInChildren<Transform>(true);
            Transform primaryBg = null;
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null || child == transform || child.parent != transform) continue;
                if (child.name.StartsWith("BackgroundGrid"))
                {
                    if (primaryBg == null)
                    {
                        primaryBg = child;
                    }
                    else
                    {
                        if (Application.isPlaying)
                            Destroy(child.gameObject);
                        else
                            DestroyImmediate(child.gameObject);
                    }
                }
            }

            if (primaryBg == null)
            {
                GameObject bgGo = new GameObject("BackgroundGrid");
                bgGo.transform.SetParent(transform, false);
                primaryBg = bgGo.transform;
            }

            backgroundTransform = primaryBg;
            backgroundTransform.name = "BackgroundGrid";
            backgroundTransform.localPosition = Vector3.zero;

            // Massive background coverage (500x500 units) so camera view can never see outside void at any aspect ratio or zoom
            float bgSizeX = Mathf.Max(arenaSize.x * 5f, 500f);
            float bgSizeY = Mathf.Max(arenaSize.y * 5f, 500f);
            backgroundTransform.localScale = new Vector3(bgSizeX, bgSizeY, 1f);

            SpriteRenderer sr = backgroundTransform.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                sr = backgroundTransform.gameObject.AddComponent<SpriteRenderer>();
            }

            // Create a guaranteed 100% solid square sprite with zero alpha corner falloff
            if (solidSquareSprite == null)
            {
                Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Point;
                Color[] pixels = new Color[] { Color.white, Color.white, Color.white, Color.white };
                tex.SetPixels(pixels);
                tex.Apply();
                solidSquareSprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
            }

            sr.sprite = solidSquareSprite;
            sr.color = backgroundColor;
            sr.sortingOrder = -100;

            if (Camera.main != null)
            {
                Camera.main.backgroundColor = backgroundColor;
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

        public void SetupArenaDecorations()
        {
            if (decorationsParent != null)
            {
                if (Application.isPlaying)
                    Destroy(decorationsParent.gameObject);
                else
                    DestroyImmediate(decorationsParent.gameObject);
            }

            GameObject decGo = new GameObject("ArenaDecorations");
            decGo.transform.SetParent(transform, false);
            decorationsParent = decGo.transform;

            Vector2 half = HalfSize;
            float cornerSize = 3.5f;

            // Create 4 sleek L-shaped corner accent lines
            Vector2[] cornerPositions = new Vector2[]
            {
                new Vector2(-half.x + cornerSize * 0.5f, -half.y + cornerSize * 0.5f),
                new Vector2( half.x - cornerSize * 0.5f, -half.y + cornerSize * 0.5f),
                new Vector2( half.x - cornerSize * 0.5f,  half.y - cornerSize * 0.5f),
                new Vector2(-half.x + cornerSize * 0.5f,  half.y - cornerSize * 0.5f)
            };

            for (int i = 0; i < 4; i++)
            {
                GameObject cornerMarker = new GameObject($"Corner_{i}");
                cornerMarker.transform.SetParent(decorationsParent, false);
                cornerMarker.transform.localPosition = cornerPositions[i];

                LineRenderer clr = cornerMarker.AddComponent<LineRenderer>();
                clr.useWorldSpace = false;
                clr.positionCount = 3;
                clr.startWidth = 0.15f;
                clr.endWidth = 0.15f;
                Color accent = new Color(boundaryColor.r, boundaryColor.g, boundaryColor.b, 0.45f);
                clr.startColor = accent;
                clr.endColor = accent;
                clr.sortingOrder = -90;

                float signX = (i == 0 || i == 3) ? 1f : -1f;
                float signY = (i == 0 || i == 1) ? 1f : -1f;

                clr.SetPositions(new Vector3[]
                {
                    new Vector3(0f, -signY * cornerSize * 0.5f, 0f),
                    new Vector3(-signX * cornerSize * 0.5f, -signY * cornerSize * 0.5f, 0f),
                    new Vector3(-signX * cornerSize * 0.5f, 0f, 0f)
                });
            }
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
