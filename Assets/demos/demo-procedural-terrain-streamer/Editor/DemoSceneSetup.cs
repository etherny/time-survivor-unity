using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;
using System.Reflection;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Demos.ProceduralTerrainStreamer.Editor
{
    /// <summary>
    /// Editor utility to automatically create the ProceduralTerrainStreamer demo scene.
    /// Menu: Tools > Voxel Demos > Setup ProceduralTerrainStreamer Demo
    ///
    /// Creates a complete, working demo scene with:
    /// - Lighting (Directional Light)
    /// - Main Camera with URP Additional Camera Data
    /// - Player with CharacterController + PlayerController + visual representation
    /// - Terrain System with ProceduralTerrainStreamer
    /// - Demo Controller with DemoController
    /// - UI Canvas with stats and instructions
    /// - EventSystem with InputSystemUIInputModule (new Input System)
    ///
    /// All references are automatically assigned - no manual setup required!
    /// Uses direct assignment for public fields and reflection only when necessary for Unity internal components.
    /// </summary>
    public static class DemoSceneSetup
    {
        private const string SCENE_PATH = "Assets/demos/demo-procedural-terrain-streamer/Scenes/DemoScene.unity";
        private const string TERRAIN_MATERIAL_PATH = "Assets/demos/demo-procedural-terrain-streamer/Materials/TerrainMaterial.mat";
        private const string PLAYER_MATERIAL_PATH = "Assets/demos/demo-procedural-terrain-streamer/Materials/PlayerMaterial.mat";
        private const string VOXEL_CONFIG_PATH = "Assets/Resources/VoxelConfiguration.asset";
        private const string STREAMING_CONFIG_PATH = "Assets/Resources/StreamingConfiguration.asset";

        // UI Layout Constants
        private const float UI_PADDING = 20f;
        private const float STATS_PANEL_WIDTH = 450f;
        private const float STATS_PANEL_HEIGHT = 350f;
        private const float STATS_PANEL_CONTENT_WIDTH = 430f;
        private const float STATS_PANEL_CONTENT_HEIGHT = 330f;
        private const float INSTRUCTIONS_PANEL_WIDTH = 400f;
        private const float INSTRUCTIONS_PANEL_HEIGHT = 250f;
        private const float INSTRUCTIONS_PANEL_CONTENT_WIDTH = 380f;
        private const float INSTRUCTIONS_PANEL_CONTENT_HEIGHT = 230f;
        private const float STATS_TITLE_FONT_SIZE = 18f;
        private const float STATS_TEXT_FONT_SIZE = 14f;
        private const float INSTRUCTIONS_TEXT_FONT_SIZE = 14f;

        [MenuItem("Tools/Voxel Demos/Setup ProceduralTerrainStreamer Demo")]
        public static void SetupScene()
        {
            Debug.Log("[DemoSceneSetup] Starting scene creation...");

            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create scene structure
            CreateLighting();
            GameObject camera = CreateCamera();
            GameObject player = CreatePlayer();
            GameObject terrainSystem = CreateTerrainSystem(player.transform);
            GameObject demoController = CreateDemoController(terrainSystem, player);
            GameObject uiCanvas = CreateUI(demoController);

            // Save scene
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SCENE_PATH));
            EditorSceneManager.SaveScene(newScene, SCENE_PATH);

            Debug.Log($"[DemoSceneSetup] ✅ Demo scene created successfully at: {SCENE_PATH}");
            Debug.Log("[DemoSceneSetup] ✅ All references automatically assigned!");
            Debug.Log("[DemoSceneSetup] ✅ Player movement configured with new Input System (WASD + Shift)");
            Debug.Log("[DemoSceneSetup] ✅ Scene is ready to play immediately!");

            // Select DemoController for visibility
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

        private static GameObject CreateCamera()
        {
            Debug.Log("[DemoSceneSetup] Creating camera...");

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }

            // Position camera to view player
            mainCamera.transform.position = new Vector3(0f, 5f, -10f);
            mainCamera.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            // Add URP Additional Camera Data (silences warning)
            // NOTE: Reflection is required here because UniversalAdditionalCameraData is a Unity internal component
            // that cannot be directly referenced without heavy URP assembly dependencies in the editor script.
            var urpCameraDataType = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
            if (urpCameraDataType != null && mainCamera.GetComponent(urpCameraDataType) == null)
            {
                mainCamera.gameObject.AddComponent(urpCameraDataType);
            }

            Debug.Log("[DemoSceneSetup] ✅ Main Camera configured with URP");
            return mainCamera.gameObject;
        }

        private static GameObject CreatePlayer()
        {
            Debug.Log("[DemoSceneSetup] Creating player...");

            // Create player GameObject
            GameObject playerObj = new GameObject("Player");
            playerObj.transform.position = new Vector3(0f, 2f, 0f);

            // Add CharacterController
            CharacterController characterController = playerObj.AddComponent<CharacterController>();
            characterController.radius = 0.5f;
            characterController.height = 2f;
            characterController.center = new Vector3(0f, 1f, 0f);

            // Add PlayerController
            PlayerController playerController = playerObj.AddComponent<PlayerController>();

            // Configure player movement settings
            playerController.moveSpeed = 10f;
            playerController.sprintMultiplier = 2f;
            playerController.gravity = 9.81f;

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

            Debug.Log("[DemoSceneSetup] ✅ Player created with CharacterController and PlayerController");
            return playerObj;
        }

        private static GameObject CreateTerrainSystem(Transform player)
        {
            Debug.Log("[DemoSceneSetup] Creating terrain system...");

            // Create terrain system GameObject
            GameObject terrainObj = new GameObject("Terrain System");
            terrainObj.transform.position = Vector3.zero;

            // Add ProceduralTerrainStreamer
            TimeSurvivor.Voxel.Streaming.ProceduralTerrainStreamer streamer =
                terrainObj.AddComponent<TimeSurvivor.Voxel.Streaming.ProceduralTerrainStreamer>();

            // Load or create StreamingConfiguration
            TimeSurvivor.Voxel.Streaming.StreamingConfiguration streamingConfig =
                AssetDatabase.LoadAssetAtPath<TimeSurvivor.Voxel.Streaming.StreamingConfiguration>(STREAMING_CONFIG_PATH);
            if (streamingConfig == null)
            {
                Debug.LogWarning($"[DemoSceneSetup] StreamingConfiguration not found at {STREAMING_CONFIG_PATH}. Creating default...");
                streamingConfig = TimeSurvivor.Voxel.Streaming.StreamingConfiguration.CreateDefault();
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(STREAMING_CONFIG_PATH));
                AssetDatabase.CreateAsset(streamingConfig, STREAMING_CONFIG_PATH);
                AssetDatabase.SaveAssets();
                Debug.Log($"[DemoSceneSetup] ✅ StreamingConfiguration created at {STREAMING_CONFIG_PATH}");
            }

            // Load VoxelConfiguration
            VoxelConfiguration voxelConfig = AssetDatabase.LoadAssetAtPath<VoxelConfiguration>(VOXEL_CONFIG_PATH);
            if (voxelConfig == null)
            {
                Debug.LogError($"[DemoSceneSetup] VoxelConfiguration not found at {VOXEL_CONFIG_PATH}. Please create it first.");
            }

            // Load terrain material
            Material terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>(TERRAIN_MATERIAL_PATH);
            if (terrainMaterial == null)
            {
                Debug.LogWarning($"[DemoSceneSetup] Terrain material not found at {TERRAIN_MATERIAL_PATH}");
            }

            // Configure terrain streamer
            streamer.playerTransform = player;
            streamer.chunkMaterial = terrainMaterial;
            streamer.voxelConfig = voxelConfig;
            streamer.streamingConfig = streamingConfig;
            streamer.maxChunksPerFrame = 1;

            Debug.Log("[DemoSceneSetup] ✅ ProceduralTerrainStreamer configured");
            return terrainObj;
        }

        private static GameObject CreateDemoController(GameObject terrainSystem, GameObject player)
        {
            Debug.Log("[DemoSceneSetup] Creating demo controller...");

            // Create demo controller GameObject
            GameObject controllerObj = new GameObject("Demo Controller");

            // Add DemoController
            DemoController controller = controllerObj.AddComponent<DemoController>();

            // Configure demo controller
            TimeSurvivor.Voxel.Streaming.ProceduralTerrainStreamer streamer =
                terrainSystem.GetComponent<TimeSurvivor.Voxel.Streaming.ProceduralTerrainStreamer>();

            // Use reflection only for private fields that cannot be made public (Unity internal types)
            AssignFieldViaReflection(controller, "terrainStreamer", streamer);
            AssignFieldViaReflection(controller, "player", player.transform);
            AssignFieldViaReflection(controller, "fpsUpdateInterval", 1f);

            // Direct assignment for public fields
            controller.showGizmos = true;
            controller.loadRadiusColor = Color.green;
            controller.unloadRadiusColor = Color.red;

            Debug.Log("[DemoSceneSetup] ✅ DemoController created (UI references will be assigned next)");
            return controllerObj;
        }

        private static GameObject CreateUI(GameObject demoController)
        {
            Debug.Log("[DemoSceneSetup] Creating UI...");

            // Create Canvas
            GameObject canvasObj = new GameObject("UI Canvas");
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

            // Assign to DemoController via Reflection
            var controller = demoController.GetComponent<DemoController>();
            AssignFieldViaReflection(controller, "statsText", statsText);

            Debug.Log("[DemoSceneSetup] ✅ Stats panel created and assigned to DemoController");
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

            // Assign to DemoController via Reflection
            var controller = demoController.GetComponent<DemoController>();
            AssignFieldViaReflection(controller, "instructionsText", instructionsText);

            Debug.Log("[DemoSceneSetup] ✅ Instructions panel created and assigned to DemoController");
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

        // ========== Reflection Utilities ==========

        private static void AssignFieldViaReflection(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(target, value);
                Debug.Log($"[DemoSceneSetup] ✅ Assigned {fieldName} = {value}");
            }
            else
            {
                Debug.LogWarning($"[DemoSceneSetup] ⚠️ Field '{fieldName}' not found on {target.GetType().Name}");
            }
        }
    }
}
