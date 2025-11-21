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

            Debug.Log($"[DemoSceneSetup] ✅ Demo scene created successfully at: {SCENE_PATH}");
            Debug.Log("[DemoSceneSetup] ✅ All UI elements created and automatically assigned to DemoController!");
            Debug.Log("[DemoSceneSetup] Next steps:\n" +
                      "1. Run: Tools > Voxel Demos > Create Voxel Terrain Shader (to create material)\n" +
                      "2. Assign VoxelTerrain.mat to DemoController > Voxel Material in Inspector\n" +
                      "3. Press Play to test the demo!");

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
            CreateControlPanel(canvas.transform, demoController);
            CreateStatsPanel(canvas.transform, demoController);

            Debug.Log("[DemoSceneSetup] UI Canvas created with all sliders, buttons, and automatic DemoController assignments!");

            return canvasObj;
        }

        private static void CreateControlPanel(Transform canvasTransform, GameObject demoController)
        {
            // Create panel
            GameObject panelObj = new GameObject("Panel - Controls");
            panelObj.transform.SetParent(canvasTransform, false);
            RectTransform panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0.5f);
            panelRect.anchorMax = new Vector2(0f, 0.5f);
            panelRect.pivot = new Vector2(0f, 0.5f);
            panelRect.anchoredPosition = new Vector2(20f, 0f);
            panelRect.sizeDelta = new Vector2(300f, 450f);

            Image panelImage = panelObj.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.7f);

            // Title
            CreateText(panelObj.transform, "Title", "Procedural Terrain Generation",
                new Vector2(150f, -20f), new Vector2(280f, 30f), 18, TextAlignmentOptions.Center);

            // Create sliders
            Slider seedSlider = CreateSlider(panelObj.transform, "Slider - Seed", "Seed",
                new Vector2(150f, -70f), 0f, 999999f, 12345f, true);
            Slider freqSlider = CreateSlider(panelObj.transform, "Slider - Frequency", "Frequency",
                new Vector2(150f, -130f), 0.01f, 0.2f, 0.05f, false);
            Slider ampSlider = CreateSlider(panelObj.transform, "Slider - Amplitude", "Amplitude",
                new Vector2(150f, -190f), 5f, 50f, 20f, true);
            Slider offsetSlider = CreateSlider(panelObj.transform, "Slider - OffsetY", "Offset Y",
                new Vector2(150f, -250f), 0f, 64f, 32f, true);

            // Create buttons
            Button generateBtn = CreateButton(panelObj.transform, "Button - Generate", "Generate Chunk",
                new Vector2(150f, -310f), new Color(0.13f, 0.59f, 0.95f)); // Blue
            Button randomizeBtn = CreateButton(panelObj.transform, "Button - Randomize", "Randomize Seed",
                new Vector2(150f, -370f), new Color(0.30f, 0.69f, 0.31f)); // Green

            // Assign references to DemoController using reflection
            var controller = demoController.GetComponent<DemoController>();
            var controllerType = typeof(DemoController);

            AssignField(controllerType, controller, "seedSlider", seedSlider);
            AssignField(controllerType, controller, "frequencySlider", freqSlider);
            AssignField(controllerType, controller, "amplitudeSlider", ampSlider);
            AssignField(controllerType, controller, "offsetYSlider", offsetSlider);
            AssignField(controllerType, controller, "generateButton", generateBtn);
            AssignField(controllerType, controller, "randomizeButton", randomizeBtn);

            Debug.Log("[DemoSceneSetup] Control Panel created with 4 sliders and 2 buttons - automatically assigned to DemoController!");
        }

        private static void CreateStatsPanel(Transform canvasTransform, GameObject demoController)
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
            TextMeshProUGUI genTimeText = CreateText(panelObj.transform, "Text - Generation Time", "Generation Time: -- ms",
                new Vector2(-175f, -60f), new Vector2(330f, 25f), 14, TextAlignmentOptions.Left).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI voxelCountText = CreateText(panelObj.transform, "Text - Voxel Count", "Solid Voxels: --",
                new Vector2(-175f, -90f), new Vector2(330f, 25f), 14, TextAlignmentOptions.Left).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI distributionText = CreateText(panelObj.transform, "Text - Distribution", "Distribution:\n(Generating...)",
                new Vector2(-175f, -160f), new Vector2(330f, 100f), 14, TextAlignmentOptions.TopLeft).GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI fpsText = CreateText(panelObj.transform, "Text - FPS", "FPS: --",
                new Vector2(-175f, -270f), new Vector2(330f, 25f), 14, TextAlignmentOptions.Left).GetComponent<TextMeshProUGUI>();

            // Assign references to DemoController using reflection
            var controller = demoController.GetComponent<DemoController>();
            var controllerType = typeof(DemoController);

            AssignField(controllerType, controller, "generationTimeText", genTimeText);
            AssignField(controllerType, controller, "voxelCountText", voxelCountText);
            AssignField(controllerType, controller, "distributionText", distributionText);
            AssignField(controllerType, controller, "fpsText", fpsText);

            Debug.Log("[DemoSceneSetup] Stats Panel created with 4 text fields - automatically assigned to DemoController!");
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

        private static Slider CreateSlider(Transform parent, string name, string label,
            Vector2 position, float minValue, float maxValue, float defaultValue, bool wholeNumbers)
        {
            // Create slider GameObject
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchoredPosition = position;
            sliderRect.sizeDelta = new Vector2(280f, 30f);

            // Add Slider component
            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = defaultValue;
            slider.wholeNumbers = wholeNumbers;

            // Create Background
            GameObject bgObj = new GameObject("Background");
            bgObj.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bgObj.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Create Fill Area
            GameObject fillAreaObj = new GameObject("Fill Area");
            fillAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.sizeDelta = new Vector2(-20f, 0f);

            // Create Fill
            GameObject fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            RectTransform fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fillObj.AddComponent<Image>();
            fillImage.color = new Color(0.13f, 0.59f, 0.95f, 1f); // Blue

            // Create Handle Slide Area
            GameObject handleAreaObj = new GameObject("Handle Slide Area");
            handleAreaObj.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleAreaObj.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = new Vector2(-20f, 0f);

            // Create Handle
            GameObject handleObj = new GameObject("Handle");
            handleObj.transform.SetParent(handleAreaObj.transform, false);
            RectTransform handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20f, 0f);
            Image handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;

            // Assign slider references
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            // Create label above slider
            string valueStr = wholeNumbers ? defaultValue.ToString("F0") : defaultValue.ToString("F2");
            GameObject labelObj = CreateText(sliderObj.transform, "Label", $"{label}: {valueStr}",
                new Vector2(0f, 20f), new Vector2(280f, 20f), 14, TextAlignmentOptions.Left);

            return slider;
        }

        private static Button CreateButton(Transform parent, string name, string buttonText,
            Vector2 position, Color normalColor)
        {
            // Create button GameObject
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);
            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.anchoredPosition = position;
            buttonRect.sizeDelta = new Vector2(280f, 40f);

            // Add Image
            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = normalColor;

            // Add Button component
            Button button = buttonObj.AddComponent<Button>();
            button.targetGraphic = buttonImage;

            // Set color tint transition
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = normalColor * 1.2f;
            colors.pressedColor = normalColor * 0.8f;
            button.colors = colors;

            // Create text child
            GameObject textObj = new GameObject("Text (TMP)");
            textObj.transform.SetParent(buttonObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = buttonText;
            text.fontSize = 18;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;

            return button;
        }

        private static void AssignField(System.Type type, object instance, string fieldName, object value)
        {
            var field = type.GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                field.SetValue(instance, value);
                Debug.Log($"[DemoSceneSetup] Assigned {fieldName} to DemoController");
            }
            else
            {
                Debug.LogWarning($"[DemoSceneSetup] Field '{fieldName}' not found in DemoController");
            }
        }
    }
}
