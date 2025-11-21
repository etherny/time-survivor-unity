using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

namespace TimeSurvivor.Demos.ProceduralTerrain.Editor
{
    /// <summary>
    /// Editor utility to automatically create the ProceduralTerrainGenerationJob demo scene.
    /// Menu: Tools > Voxel Demos > Setup Procedural Terrain Demo Scene
    /// </summary>
    public static class DemoSceneSetup
    {
        private const string SCENE_PATH = "Assets/demos/demo-procedural-terrain-job/Scenes/DemoScene.unity";
        private const string MATERIAL_PATH = "Assets/demos/demo-procedural-terrain-job/Materials/VoxelTerrain.mat";

        [MenuItem("Tools/Voxel Demos/Setup Procedural Terrain Demo Scene")]
        public static void SetupScene()
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Create scene structure
            CreateLighting();
            GameObject camera = CreateCamera();
            GameObject terrainContainer = CreateTerrainContainer();
            GameObject demoController = CreateDemoController(terrainContainer.transform, camera);
            GameObject uiCanvas = CreateUI(demoController);

            // Save scene
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SCENE_PATH));
            EditorSceneManager.SaveScene(newScene, SCENE_PATH);

            Debug.Log($"[DemoSceneSetup] Demo scene created successfully at: {SCENE_PATH}");
            Debug.Log("[DemoSceneSetup] Next steps:\n" +
                      "1. Assign UI references in DemoController Inspector\n" +
                      "2. Create VoxelTerrain material with vertex color support\n" +
                      "3. Press Play to test the demo");

            // Select DemoController for easy configuration
            Selection.activeGameObject = demoController;
            EditorGUIUtility.PingObject(demoController);
        }

        // ========== Scene Creation Helpers ==========

        private static void CreateLighting()
        {
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
        }

        private static GameObject CreateCamera()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                GameObject cameraObj = new GameObject("Main Camera");
                mainCamera = cameraObj.AddComponent<Camera>();
                cameraObj.tag = "MainCamera";
            }

            // Position camera
            mainCamera.transform.position = new Vector3(32f, 48f, -48f);
            mainCamera.transform.LookAt(new Vector3(32f, 32f, 32f));

            // Add orbit controller
            CameraOrbitController orbitController = mainCamera.GetComponent<CameraOrbitController>();
            if (orbitController == null)
            {
                orbitController = mainCamera.gameObject.AddComponent<CameraOrbitController>();
            }

            return mainCamera.gameObject;
        }

        private static GameObject CreateTerrainContainer()
        {
            GameObject container = new GameObject("Terrain Container");
            container.transform.position = Vector3.zero;
            return container;
        }

        private static GameObject CreateDemoController(Transform terrainContainer, GameObject camera)
        {
            GameObject controllerObj = new GameObject("Demo Controller");
            DemoController controller = controllerObj.AddComponent<DemoController>();

            // Use reflection to set private serialized fields
            var controllerType = typeof(DemoController);
            var terrainContainerField = controllerType.GetField("terrainContainer",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (terrainContainerField != null)
            {
                terrainContainerField.SetValue(controller, terrainContainer);
            }

            Debug.Log("[DemoSceneSetup] DemoController created. Please assign UI references and material in Inspector.");

            return controllerObj;
        }

        private static GameObject CreateUI(GameObject demoController)
        {
            // Create Canvas
            GameObject canvasObj = new GameObject("UI Canvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // Create EventSystem if not exists
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // Create UI panels
            CreateControlPanel(canvas.transform);
            CreateStatsPanel(canvas.transform);

            Debug.Log("[DemoSceneSetup] UI Canvas created. Manually assign UI elements to DemoController.");

            return canvasObj;
        }

        private static void CreateControlPanel(Transform canvasTransform)
        {
            // Create panel
            GameObject panelObj = new GameObject("Panel - Controls");
            panelObj.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(0f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(20f, 0f);
            panelRect.sizeDelta = new Vector2(300f, 400f);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            // Title
            CreateText(panelObj.transform, "Title", "Procedural Terrain Generation",
                new Vector2(0f, 180f), new Vector2(280f, 30f), 18, TextAlignmentOptions.Center);

            // Sliders (placeholder - manual setup required)
            CreateText(panelObj.transform, "Seed Label", "Seed: (Add Slider)",
                new Vector2(0f, 140f), new Vector2(280f, 30f), 14, TextAlignmentOptions.Left);
            CreateText(panelObj.transform, "Frequency Label", "Frequency: (Add Slider)",
                new Vector2(0f, 100f), new Vector2(280f, 30f), 14, TextAlignmentOptions.Left);
            CreateText(panelObj.transform, "Amplitude Label", "Amplitude: (Add Slider)",
                new Vector2(0f, 60f), new Vector2(280f, 30f), 14, TextAlignmentOptions.Left);
            CreateText(panelObj.transform, "OffsetY Label", "Offset Y: (Add Slider)",
                new Vector2(0f, 20f), new Vector2(280f, 30f), 14, TextAlignmentOptions.Left);

            // Buttons (placeholder - manual setup required)
            CreateText(panelObj.transform, "Generate Label", "(Add Generate Button)",
                new Vector2(0f, -40f), new Vector2(280f, 30f), 14, TextAlignmentOptions.Center);
            CreateText(panelObj.transform, "Randomize Label", "(Add Randomize Button)",
                new Vector2(0f, -80f), new Vector2(280f, 30f), 14, TextAlignmentOptions.Center);

            Debug.Log("[DemoSceneSetup] Control Panel created. Add sliders and buttons manually via UI Builder.");
        }

        private static void CreateStatsPanel(Transform canvasTransform)
        {
            // Create panel
            GameObject panelObj = new GameObject("Panel - Stats");
            panelObj.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-20f, -20f);
            panelRect.sizeDelta = new Vector2(350f, 300f);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            // Stats texts
            CreateText(panelObj.transform, "Stats Title", "Performance Stats",
                new Vector2(-175f, -20f), new Vector2(330f, 30f), 18, TextAlignmentOptions.Center);
            CreateText(panelObj.transform, "Generation Time", "Generation Time: -- ms",
                new Vector2(-175f, -60f), new Vector2(330f, 25f), 14, TextAlignmentOptions.Left);
            CreateText(panelObj.transform, "Voxel Count", "Solid Voxels: --",
                new Vector2(-175f, -90f), new Vector2(330f, 25f), 14, TextAlignmentOptions.Left);
            CreateText(panelObj.transform, "Distribution", "Distribution:\n(Generating...)",
                new Vector2(-175f, -160f), new Vector2(330f, 100f), 14, TextAlignmentOptions.TopLeft);
            CreateText(panelObj.transform, "FPS", "FPS: --",
                new Vector2(-175f, -270f), new Vector2(330f, 25f), 14, TextAlignmentOptions.Left);
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
