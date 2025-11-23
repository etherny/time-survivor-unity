using UnityEngine;

namespace TimeSurvivor.Demos.TerrainCollision
{
    /// <summary>
    /// Displays demo metrics and controls in the top-left corner.
    /// Shows FPS, player position, grounded status, collision stats, and controls help.
    /// </summary>
    public class DemoUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SimpleCharacterController playerController;
        [SerializeField] private PhysicsObjectSpawner objectSpawner;
        [SerializeField] private CollisionVisualizer visualizer;
        [SerializeField] private CollisionDemoController demoController;

        [Header("UI Settings")]
        [SerializeField] private bool showUI = true;
        [SerializeField] private KeyCode toggleUIKey = KeyCode.H;

        // FPS calculation
        private float deltaTime = 0f;
        private float updateInterval = 0.5f;
        private float accumulatedTime = 0f;
        private int frames = 0;
        private float fps = 0f;

        private void Awake()
        {
            // Find references if not assigned
            if (playerController == null)
            {
                playerController = FindObjectOfType<SimpleCharacterController>();
            }

            if (objectSpawner == null)
            {
                objectSpawner = FindObjectOfType<PhysicsObjectSpawner>();
            }

            if (visualizer == null)
            {
                visualizer = FindObjectOfType<CollisionVisualizer>();
            }

            if (demoController == null)
            {
                demoController = FindObjectOfType<CollisionDemoController>();
            }
        }

        private void Update()
        {
            // Validate input manager
            if (DemoInputManager.Instance != null)
            {
                // Toggle UI with H key
                if (DemoInputManager.Instance.ToggleUIPressed)
                {
                    showUI = !showUI;
                }
            }

            // Calculate FPS
            deltaTime = Time.unscaledDeltaTime;
            accumulatedTime += deltaTime;
            frames++;

            if (accumulatedTime >= updateInterval)
            {
                fps = frames / accumulatedTime;
                accumulatedTime = 0f;
                frames = 0;
            }
        }

        private void OnGUI()
        {
            if (!showUI) return;

            // Setup GUI style
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
                padding = new RectOffset(10, 10, 10, 10)
            };

            GUIStyle headerStyle = new GUIStyle(style)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.yellow }
            };

            // Background box
            GUI.Box(new Rect(10, 10, 350, 280), "", GUI.skin.box);

            int yPos = 20;
            int lineHeight = 20;

            // Header
            GUI.Label(new Rect(20, yPos, 300, 30), "Terrain Collision Demo", headerStyle);
            yPos += 35;

            // FPS
            Color fpsColor = fps >= 60 ? Color.green : (fps >= 30 ? Color.yellow : Color.red);
            GUIStyle fpsStyle = new GUIStyle(style) { normal = { textColor = fpsColor } };
            GUI.Label(new Rect(20, yPos, 300, lineHeight), $"FPS: {fps:F1}", fpsStyle);
            yPos += lineHeight;

            // Player Position
            if (playerController != null)
            {
                Vector3 pos = playerController.transform.position;
                GUI.Label(new Rect(20, yPos, 300, lineHeight),
                    $"Position: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})", style);
                yPos += lineHeight;

                // Grounded Status
                string groundedText = playerController.IsGrounded ? "Yes" : "No";
                Color groundedColor = playerController.IsGrounded ? Color.green : Color.red;
                GUIStyle groundedStyle = new GUIStyle(style) { normal = { textColor = groundedColor } };
                GUI.Label(new Rect(20, yPos, 300, lineHeight), $"Grounded: {groundedText}", groundedStyle);
                yPos += lineHeight;
            }

            // Collision Stats
            if (demoController != null)
            {
                GUI.Label(new Rect(20, yPos, 300, lineHeight),
                    $"Collision Chunks: {demoController.CollisionChunkCount}", style);
                yPos += lineHeight;

                GUI.Label(new Rect(20, yPos, 300, lineHeight),
                    $"Baking Queue: {demoController.BakingQueueCount}", style);
                yPos += lineHeight;
            }

            // Physics Objects
            if (objectSpawner != null)
            {
                GUI.Label(new Rect(20, yPos, 300, lineHeight),
                    $"Physics Objects: {objectSpawner.ActiveCount}/{objectSpawner.TotalSpawned}", style);
                yPos += lineHeight;
            }

            // Separator
            yPos += 5;

            // Controls
            GUI.Label(new Rect(20, yPos, 300, lineHeight), "--- Controls ---", headerStyle);
            yPos += lineHeight + 5;

            GUI.Label(new Rect(20, yPos, 300, lineHeight), "WASD: Move", style);
            yPos += lineHeight;
            GUI.Label(new Rect(20, yPos, 300, lineHeight), "Mouse: Look Around", style);
            yPos += lineHeight;
            GUI.Label(new Rect(20, yPos, 300, lineHeight), "Space: Jump", style);
            yPos += lineHeight;
            GUI.Label(new Rect(20, yPos, 300, lineHeight), "O: Spawn Physics Object", style);
            yPos += lineHeight;
            GUI.Label(new Rect(20, yPos, 300, lineHeight), "V: Toggle Collider Visualization", style);
            yPos += lineHeight;
            GUI.Label(new Rect(20, yPos, 300, lineHeight), "H: Toggle This UI", style);
            yPos += lineHeight;
            GUI.Label(new Rect(20, yPos, 300, lineHeight), "ESC: Unlock Cursor", style);
        }
    }
}
