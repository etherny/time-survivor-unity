using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using TMPro;
using TimeSurvivor.Demos.GreedyMeshing;

namespace TimeSurvivor.Demos.GreedyMeshing.Editor
{
    /// <summary>
    /// Editor script to automatically create and configure the GreedyMeshing demo scene.
    /// This ensures all references are correctly assigned without manual GUID manipulation.
    /// </summary>
    public class SetupDemoScene : EditorWindow
    {
        [MenuItem("Tools/Demos/Setup Greedy Meshing Demo Scene")]
        public static void CreateDemoScene()
        {
            // Create new scene
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // === CAMERA ===
            GameObject cameraObj = GameObject.Find("Main Camera");
            if (cameraObj == null)
            {
                cameraObj = new GameObject("Main Camera");
                cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }

            // Position camera
            cameraObj.transform.position = new Vector3(40, 30, 40);
            cameraObj.transform.LookAt(new Vector3(0, 16, 0));

            // Add URP Additional Camera Data component
            var camera = cameraObj.GetComponent<Camera>();
            if (camera != null)
            {
                var urpCameraData = cameraObj.GetComponent<UniversalAdditionalCameraData>();
                if (urpCameraData == null)
                {
                    cameraObj.AddComponent<UniversalAdditionalCameraData>();
                }
            }

            // Add CameraController
            var cameraController = cameraObj.GetComponent<CameraController>();
            if (cameraController == null)
                cameraController = cameraObj.AddComponent<CameraController>();

            // === LIGHTING ===
            GameObject lightObj = GameObject.Find("Directional Light");
            if (lightObj == null)
            {
                lightObj = new GameObject("Directional Light");
                Light light = lightObj.AddComponent<Light>();
                light.type = LightType.Directional;
            }
            lightObj.transform.rotation = Quaternion.Euler(50, -30, 0);

            // === CHUNK RENDERER ===
            GameObject chunkRenderer = new GameObject("ChunkRenderer");
            chunkRenderer.transform.position = Vector3.zero;

            MeshFilter meshFilter = chunkRenderer.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunkRenderer.AddComponent<MeshRenderer>();

            // Create material
            Material material = CreateVoxelMaterial();
            meshRenderer.material = material;

            GreedyMeshingDemo demo = chunkRenderer.AddComponent<GreedyMeshingDemo>();
            demo.ChunkSize = 32;
            demo.MeshFilter = meshFilter;
            demo.MeshRenderer = meshRenderer;

            // === UI CANVAS ===
            GameObject canvasObj = new GameObject("UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            // Create UI elements
            CreateUIElements(canvasObj, demo);

            // Set CameraController target
            cameraController.Target = chunkRenderer.transform;
            cameraController.Distance = 40f;
            cameraController.OrbitSpeed = 100f;
            cameraController.ZoomSpeed = 10f;
            cameraController.MinDistance = 10f;
            cameraController.MaxDistance = 100f;

            // Save scene
            string scenePath = "Assets/demos/demo-greedy-meshing/Scenes/DemoScene.unity";
            System.IO.Directory.CreateDirectory("Assets/demos/demo-greedy-meshing/Scenes");
            EditorSceneManager.SaveScene(scene, scenePath);

            Debug.Log($"[SetupDemoScene] Demo scene created successfully at {scenePath}");
            Debug.Log("[SetupDemoScene] You can test the demo using the menu: Tools > Demos > Generate Pattern");
        }

        private static Material CreateVoxelMaterial()
        {
            // Find the shader
            Shader shader = Shader.Find("Demo/VoxelVertexColorURP");
            if (shader == null)
            {
                Debug.LogError("[SetupDemoScene] Shader 'Demo/VoxelVertexColorURP' not found! Make sure the shader file exists.");
                shader = Shader.Find("Universal Render Pipeline/Lit"); // Fallback
            }

            Material material = new Material(shader);
            material.name = "VoxelVertexColor";

            // Save material asset
            string materialPath = "Assets/demos/demo-greedy-meshing/Materials/VoxelVertexColor.mat";
            System.IO.Directory.CreateDirectory("Assets/demos/demo-greedy-meshing/Materials");
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[SetupDemoScene] Material created at {materialPath}");

            return material;
        }

        private static void CreateUIElements(GameObject canvas, GreedyMeshingDemo demo)
        {
            // Header
            GameObject headerObj = CreateTextObject(canvas, "Header", "Greedy Meshing Demo - Issue #4",
                new Vector2(20, -20), new Vector2(400, 40), TextAlignmentOptions.TopLeft, 24);

            // Stats Panel
            GameObject statsPanel = CreatePanel(canvas, "StatsPanel", new Vector2(20, -70), new Vector2(300, 120));

            GameObject fpsText = CreateTextObject(statsPanel, "FPSText", "FPS: 0",
                new Vector2(10, -10), new Vector2(280, 25), TextAlignmentOptions.TopLeft, 16);
            demo.FpsText = fpsText.GetComponent<TextMeshProUGUI>();

            GameObject vertexCountText = CreateTextObject(statsPanel, "VertexCountText", "Vertices: 0",
                new Vector2(10, -35), new Vector2(280, 25), TextAlignmentOptions.TopLeft, 16);
            demo.VertexCountText = vertexCountText.GetComponent<TextMeshProUGUI>();

            GameObject meshingTimeText = CreateTextObject(statsPanel, "MeshingTimeText", "Meshing Time: 0ms",
                new Vector2(10, -60), new Vector2(280, 25), TextAlignmentOptions.TopLeft, 16);
            demo.MeshingTimeText = meshingTimeText.GetComponent<TextMeshProUGUI>();

            GameObject patternText = CreateTextObject(statsPanel, "PatternText", "Pattern: None",
                new Vector2(10, -85), new Vector2(280, 25), TextAlignmentOptions.TopLeft, 16);
            demo.PatternText = patternText.GetComponent<TextMeshProUGUI>();

            // Instructions
            GameObject instructions = CreateTextObject(canvas, "Instructions",
                "Instructions:\n- Right-click + Drag: Orbit camera\n- Mouse Wheel: Zoom\n- Use menu: Tools > Demos > Generate Pattern",
                new Vector2(20, -300), new Vector2(400, 100), TextAlignmentOptions.TopLeft, 14);

            Debug.Log("[SetupDemoScene] UI elements created successfully.");
        }

        private static GameObject CreatePanel(GameObject parent, string name, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);

            return panel;
        }

        private static GameObject CreateTextObject(GameObject parent, string name, string text,
            Vector2 anchoredPosition, Vector2 sizeDelta, TextAlignmentOptions alignment, int fontSize)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent.transform, false);

            RectTransform rectTransform = textObj.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            TextMeshProUGUI textMesh = textObj.AddComponent<TextMeshProUGUI>();
            textMesh.text = text;
            textMesh.fontSize = fontSize;
            textMesh.color = Color.white;
            textMesh.alignment = alignment;

            return textObj;
        }

        // Menu items for testing patterns
        [MenuItem("Tools/Demos/Generate Pattern/Single Cube")]
        public static void GenerateSingleCube()
        {
            GreedyMeshingDemo demo = Object.FindObjectOfType<GreedyMeshingDemo>();
            if (demo != null) demo.GenerateSingleCube();
            else Debug.LogWarning("GreedyMeshingDemo not found in scene! Please run 'Tools > Demos > Setup Greedy Meshing Demo Scene' first.");
        }

        [MenuItem("Tools/Demos/Generate Pattern/Flat Plane")]
        public static void GenerateFlatPlane()
        {
            GreedyMeshingDemo demo = Object.FindObjectOfType<GreedyMeshingDemo>();
            if (demo != null) demo.GenerateFlatPlane();
            else Debug.LogWarning("GreedyMeshingDemo not found in scene! Please run 'Tools > Demos > Setup Greedy Meshing Demo Scene' first.");
        }

        [MenuItem("Tools/Demos/Generate Pattern/Terrain")]
        public static void GenerateTerrain()
        {
            GreedyMeshingDemo demo = Object.FindObjectOfType<GreedyMeshingDemo>();
            if (demo != null) demo.GenerateTerrain();
            else Debug.LogWarning("GreedyMeshingDemo not found in scene! Please run 'Tools > Demos > Setup Greedy Meshing Demo Scene' first.");
        }

        [MenuItem("Tools/Demos/Generate Pattern/Checkerboard")]
        public static void GenerateCheckerboard()
        {
            GreedyMeshingDemo demo = Object.FindObjectOfType<GreedyMeshingDemo>();
            if (demo != null) demo.GenerateCheckerboard();
            else Debug.LogWarning("GreedyMeshingDemo not found in scene! Please run 'Tools > Demos > Setup Greedy Meshing Demo Scene' first.");
        }

        [MenuItem("Tools/Demos/Generate Pattern/Random")]
        public static void GenerateRandom()
        {
            GreedyMeshingDemo demo = Object.FindObjectOfType<GreedyMeshingDemo>();
            if (demo != null) demo.GenerateRandom();
            else Debug.LogWarning("GreedyMeshingDemo not found in scene! Please run 'Tools > Demos > Setup Greedy Meshing Demo Scene' first.");
        }
    }
}
