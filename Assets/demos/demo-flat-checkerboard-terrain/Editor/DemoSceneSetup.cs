using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain.Editor
{
    /// <summary>
    /// Editor utility to automatically create the Flat Checkerboard Terrain demo scene.
    /// Menu: Tools > Voxel Demos > Setup Flat Checkerboard Terrain Demo
    ///
    /// Creates a complete, working demo scene with:
    /// - Lighting (Directional Light)
    /// - Main Camera with SimpleCameraFollow
    /// - Player with CharacterController + FlatTerrainPlayerController + visual representation
    /// - Demo Controller with FlatTerrainDemoController
    /// - UI Canvas with stats and instructions
    /// - EventSystem with InputSystemUIInputModule (new Input System)
    ///
    /// All references are automatically assigned via direct field assignment - no reflection needed!
    /// </summary>
    public static class DemoSceneSetup
    {
        private const string SCENE_PATH = "Assets/demos/demo-flat-checkerboard-terrain/Scenes/DemoScene.unity";
        private const string VOXEL_MATERIAL_PATH = "Assets/demos/demo-procedural-terrain-streamer/Materials/TerrainMaterial.mat";
        private const string PLAYER_MATERIAL_PATH = "Assets/demos/demo-procedural-terrain-streamer/Materials/PlayerMaterial.mat";
        private const string VOXEL_CONFIG_PATH = "Assets/Resources/VoxelConfiguration.asset";

        // UI Layout Constants
        private const float UI_PADDING = 20f;
        private const float STATS_PANEL_WIDTH = 400f;
        private const float STATS_PANEL_HEIGHT = 200f;
        private const float STATS_PANEL_CONTENT_WIDTH = 380f;
        private const float STATS_PANEL_CONTENT_HEIGHT = 180f;
        private const float INSTRUCTIONS_PANEL_WIDTH = 350f;
        private const float INSTRUCTIONS_PANEL_HEIGHT = 200f;
        private const float INSTRUCTIONS_PANEL_CONTENT_WIDTH = 330f;
        private const float INSTRUCTIONS_PANEL_CONTENT_HEIGHT = 180f;
        private const float STATS_TEXT_FONT_SIZE = 14f;
        private const float INSTRUCTIONS_TEXT_FONT_SIZE = 14f;

        [MenuItem("Tools/Voxel Demos/Setup Flat Checkerboard Terrain Demo")]
        public static void SetupScene()
        {
            Debug.Log("[DemoSceneSetup] Starting Flat Checkerboard Terrain demo scene creation...");

            Scene newScene = CreateNewScene();
            GameObject demoController = SetupSceneObjects(newScene);
            SaveAndSelectScene(newScene, demoController);
        }

        private static Scene CreateNewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        private static GameObject SetupSceneObjects(Scene scene)
        {
            CreateLighting();
            GameObject player = CreatePlayer();
            GameObject camera = CreateCamera(player);
            GameObject demoController = CreateDemoController(player);
            GameObject uiCanvas = CreateUI(demoController);

            return demoController;
        }

        private static void SaveAndSelectScene(Scene scene, GameObject demoController)
        {
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SCENE_PATH));
            EditorSceneManager.SaveScene(scene, SCENE_PATH);

            Debug.Log($"[DemoSceneSetup] ✅ Demo scene created successfully at: {SCENE_PATH}");
            Debug.Log("[DemoSceneSetup] ✅ All references automatically assigned!");
            Debug.Log("[DemoSceneSetup] ✅ Player movement configured with new Input System (WASD + Shift)");
            Debug.Log("[DemoSceneSetup] ✅ Scene is ready to play immediately!");

            Selection.activeGameObject = demoController;
            EditorGUIUtility.PingObject(demoController);
        }

        // ========== Scene Creation Helpers ==========

        private static void CreateLighting()
        {
            Debug.Log("[DemoSceneSetup] Creating lighting...");

            // Find existing directional light or create one
            Light[] lights = Object.FindObjectsOfType<Light>();
            Light directionalLight = null;

            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    directionalLight = light;
                    break;
                }
            }

            if (directionalLight == null)
            {
                GameObject lightObj = new GameObject("Directional Light");
                directionalLight = lightObj.AddComponent<Light>();
                directionalLight.type = LightType.Directional;
            }

            // Configure lighting
            directionalLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            directionalLight.intensity = 1f;
            directionalLight.color = Color.white;

            Debug.Log("[DemoSceneSetup] ✅ Directional Light configured");
        }

        private static GameObject CreatePlayer()
        {
            Debug.Log("[DemoSceneSetup] Creating player...");

            // Create player GameObject
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(0f, 2f, 0f); // Just above terrain (terrain height = 1.6)

            // Add CharacterController
            CharacterController characterController = playerObj.AddComponent<CharacterController>();
            characterController.radius = 0.5f;
            characterController.height = 2f;
            characterController.center = new Vector3(0f, 1f, 0f);

            // Add FlatTerrainPlayerController (no gravity)
            FlatTerrainPlayerController playerController = playerObj.AddComponent<FlatTerrainPlayerController>();

            // Configure player movement settings (direct assignment of public fields)
            playerController.moveSpeed = 10f;
            playerController.sprintMultiplier = 2f;

            // Create visual representation (capsule)
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            visual.name = "Visual";
            visual.transform.SetParent(playerObj.transform, false);
            visual.transform.localPosition = new Vector3(0f, 1f, 0f);
            visual.transform.localScale = new Vector3(1f, 1f, 1f);

            // Remove collider from visual (CharacterController handles collision)
            Object.DestroyImmediate(visual.GetComponent<Collider>());

            // Apply material
            Material playerMaterial = AssetDatabase.LoadAssetAtPath<Material>(PLAYER_MATERIAL_PATH);
            if (playerMaterial != null)
            {
                visual.GetComponent<Renderer>().material = playerMaterial;
                Debug.Log("[DemoSceneSetup] ✅ Player material assigned");
            }

            Debug.Log("[DemoSceneSetup] ✅ Player created with CharacterController and FlatTerrainPlayerController");
            return playerObj;
        }

        private static GameObject CreateCamera(GameObject player)
        {
            Debug.Log("[DemoSceneSetup] Creating camera with SimpleCameraFollow...");

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }

            // Position camera (initial position before follow starts)
            mainCamera.transform.position = new Vector3(0f, 12f, -12f);
            mainCamera.transform.LookAt(player.transform);

            // Add SimpleCameraFollow component
            SimpleCameraFollow cameraFollow = mainCamera.gameObject.AddComponent<SimpleCameraFollow>();

            // Configure camera follow (direct assignment of public fields)
            cameraFollow.target = player.transform;
            cameraFollow.offset = new Vector3(0, 12, -12); // Closer to terrain for better pattern visibility
            cameraFollow.smoothFollow = true;
            cameraFollow.smoothSpeed = 5f;

            // Add URP Additional Camera Data (silences warning)
            var urpCameraDataType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpCameraDataType != null && mainCamera.GetComponent(urpCameraDataType) == null)
            {
                mainCamera.gameObject.AddComponent(urpCameraDataType);
            }

            Debug.Log("[DemoSceneSetup] ✅ Main Camera configured with SimpleCameraFollow and URP");
            return mainCamera.gameObject;
        }

        private static GameObject CreateDemoController(GameObject player)
        {
            Debug.Log("[DemoSceneSetup] Creating demo controller...");

            // Create demo controller GameObject
            GameObject controllerObj = new GameObject("Terrain Manager");

            // Add FlatTerrainDemoController
            FlatTerrainDemoController controller = controllerObj.AddComponent<FlatTerrainDemoController>();

            // Load VoxelConfiguration
            VoxelConfiguration voxelConfig = AssetDatabase.LoadAssetAtPath<VoxelConfiguration>(VOXEL_CONFIG_PATH);
            if (voxelConfig == null)
            {
                Debug.LogError($"[DemoSceneSetup] VoxelConfiguration not found at {VOXEL_CONFIG_PATH}. Please create it first.");
            }

            // Load voxel material
            Material voxelMaterial = AssetDatabase.LoadAssetAtPath<Material>(VOXEL_MATERIAL_PATH);
            if (voxelMaterial == null)
            {
                Debug.LogWarning($"[DemoSceneSetup] Voxel material not found at {VOXEL_MATERIAL_PATH}");
            }

            // Configure demo controller (direct assignment of public fields)
            controller.voxelConfig = voxelConfig;
            controller.chunkMaterial = voxelMaterial;
            controller.player = player.transform;
            controller.fpsUpdateInterval = 1f;

            Debug.Log("[DemoSceneSetup] ✅ FlatTerrainDemoController created (UI references will be assigned next)");
            return controllerObj;
        }

        private static GameObject CreateUI(GameObject demoController)
        {
            Debug.Log("[DemoSceneSetup] Creating UI...");

            // Create Canvas
            GameObject canvasObj = new GameObject("Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create EventSystem with new Input System module
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<InputSystemUIInputModule>(); // New Input System
                Debug.Log("[DemoSceneSetup] ✅ EventSystem created with InputSystemUIInputModule (new Input System)");
            }

            // Create UI panels
            CreateStatsPanel(canvas.transform, demoController);
            CreateInstructionsPanel(canvas.transform, demoController);

            Debug.Log("[DemoSceneSetup] ✅ UI Canvas created with all panels");
            return canvasObj;
        }

        private static void CreateStatsPanel(Transform canvasTransform, GameObject demoController)
        {
            Debug.Log("[DemoSceneSetup] Creating stats panel...");

            // Create panel
            GameObject panelObj = new GameObject("Panel - Stats");
            panelObj.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-UI_PADDING, -UI_PADDING);
            panelRect.sizeDelta = new Vector2(STATS_PANEL_WIDTH, STATS_PANEL_HEIGHT);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            // Create stats text
            GameObject statsTextObj = CreateText(panelObj.transform, "Stats Text", "Initializing...",
                new Vector2(-STATS_PANEL_WIDTH / 2f, -STATS_PANEL_HEIGHT / 2f),
                new Vector2(STATS_PANEL_CONTENT_WIDTH, STATS_PANEL_CONTENT_HEIGHT),
                (int)STATS_TEXT_FONT_SIZE, TextAlignmentOptions.TopLeft);

            TextMeshProUGUI statsText = statsTextObj.GetComponent<TextMeshProUGUI>();

            // Assign to DemoController (direct assignment to public field)
            var controller = demoController.GetComponent<FlatTerrainDemoController>();
            controller.statsText = statsText;

            Debug.Log("[DemoSceneSetup] ✅ Stats panel created and assigned to FlatTerrainDemoController");
        }

        private static void CreateInstructionsPanel(Transform canvasTransform, GameObject demoController)
        {
            Debug.Log("[DemoSceneSetup] Creating instructions panel...");

            // Create panel
            GameObject panelObj = new GameObject("Panel - Instructions");
            panelObj.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f);
            panelRect.anchorMax = new Vector2(0f, 0f);
            panelRect.pivot = new Vector2(0f, 0f);
            panelRect.anchoredPosition = new Vector2(UI_PADDING, UI_PADDING);
            panelRect.sizeDelta = new Vector2(INSTRUCTIONS_PANEL_WIDTH, INSTRUCTIONS_PANEL_HEIGHT);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            // Create instructions text
            GameObject instructionsTextObj = CreateText(panelObj.transform, "Instructions Text", "Initializing...",
                new Vector2(INSTRUCTIONS_PANEL_WIDTH / 2f, INSTRUCTIONS_PANEL_HEIGHT / 2f),
                new Vector2(INSTRUCTIONS_PANEL_CONTENT_WIDTH, INSTRUCTIONS_PANEL_CONTENT_HEIGHT),
                (int)INSTRUCTIONS_TEXT_FONT_SIZE, TextAlignmentOptions.TopLeft);

            TextMeshProUGUI instructionsText = instructionsTextObj.GetComponent<TextMeshProUGUI>();

            // Assign to DemoController (direct assignment to public field)
            var controller = demoController.GetComponent<FlatTerrainDemoController>();
            controller.instructionsText = instructionsText;

            Debug.Log("[DemoSceneSetup] ✅ Instructions panel created and assigned to FlatTerrainDemoController");
        }

        private static GameObject CreateText(Transform parent, string name, string content,
            Vector2 position, Vector2 size, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchoredPosition = position;
            textRect.sizeDelta = size;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;

            return textObj;
        }
    }
}
