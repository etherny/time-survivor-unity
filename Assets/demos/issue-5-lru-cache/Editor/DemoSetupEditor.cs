using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

namespace TimeSurvivor.Demos.LRUCache.Editor
{
    /// <summary>
    /// Editor tool for automatically setting up the LRU Cache Demo scene.
    /// Creates the complete UI hierarchy and assigns all references to the DemoController.
    /// </summary>
    public static class DemoSetupEditor
    {
        private const string MenuPath = "Tools/LRU Cache Demo/Setup Demo Scene";
        private const int MenuPriority = 1;
        private const string PrefabPath = "Assets/demos/issue-5-lru-cache/Prefabs/ChunkCacheItem.prefab";

        [MenuItem(MenuPath, false, MenuPriority)]
        public static void SetupDemoScene()
        {
            if (!ValidateScene())
            {
                return;
            }

            Debug.Log("[Demo Setup] Starting LRU Cache Demo scene setup...");

            try
            {
                // Find or create DemoController
                DemoController controller = FindOrCreateDemoController();
                if (controller == null)
                {
                    EditorUtility.DisplayDialog("Setup Failed",
                        "Could not find or create DemoController in the scene.", "OK");
                    return;
                }

                // Check if already set up
                if (IsAlreadySetup(controller))
                {
                    bool proceed = EditorUtility.DisplayDialog("Scene Already Setup",
                        "The demo scene appears to already have UI setup. Do you want to recreate it?\n\nThis will destroy the existing UI.",
                        "Recreate", "Cancel");

                    if (!proceed)
                    {
                        Debug.Log("[Demo Setup] Setup cancelled by user.");
                        return;
                    }

                    // Clean up existing UI
                    CleanupExistingUI();
                }

                // Create complete UI hierarchy
                Canvas canvas = CreateCanvas();
                GameObject statsPanel = CreateStatsPanel(canvas.transform);
                GameObject controlPanel = CreateControlPanel(canvas.transform);
                GameObject visualizationPanel = CreateCacheVisualization(canvas.transform);

                // Assign all references to DemoController
                AssignReferences(controller, statsPanel, controlPanel, visualizationPanel);

                // Mark scene dirty and save
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                EditorSceneManager.SaveOpenScenes();

                Debug.Log("[Demo Setup] LRU Cache Demo scene setup completed successfully!");
                EditorUtility.DisplayDialog("Setup Complete",
                    "Demo scene UI has been created and all references assigned.\n\n" +
                    "You can now press Play to test the demo!", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Demo Setup] Error during setup: {ex.Message}\n{ex.StackTrace}");
                EditorUtility.DisplayDialog("Setup Failed",
                    $"An error occurred during setup:\n{ex.Message}", "OK");
            }
        }

        #region Validation

        private static bool ValidateScene()
        {
            if (!Application.isPlaying)
            {
                return true;
            }

            EditorUtility.DisplayDialog("Cannot Setup While Playing",
                "Please stop Play mode before setting up the demo scene.", "OK");
            return false;
        }

        private static bool IsAlreadySetup(DemoController controller)
        {
            // Check if any UI references are already assigned
            var so = new SerializedObject(controller);
            return so.FindProperty("countText").objectReferenceValue != null ||
                   so.FindProperty("capacitySlider").objectReferenceValue != null;
        }

        private static void CleanupExistingUI()
        {
            // Find and destroy existing Canvas
            Canvas[] canvases = Object.FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                if (canvas.name == "DemoCanvas")
                {
                    Object.DestroyImmediate(canvas.gameObject);
                    Debug.Log("[Demo Setup] Removed existing UI Canvas.");
                }
            }
        }

        #endregion

        #region DemoController Setup

        private static DemoController FindOrCreateDemoController()
        {
            // Try to find existing DemoController
            DemoController controller = Object.FindObjectOfType<DemoController>();

            if (controller != null)
            {
                Debug.Log($"[Demo Setup] Found existing DemoController on '{controller.gameObject.name}'");
                return controller;
            }

            // Create new GameObject with DemoController
            GameObject controllerObj = new GameObject("DemoController");
            controller = controllerObj.AddComponent<DemoController>();
            Debug.Log("[Demo Setup] Created new DemoController GameObject.");

            return controller;
        }

        #endregion

        #region Canvas Creation

        private static Canvas CreateCanvas()
        {
            GameObject canvasObj = new GameObject("DemoCanvas");
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.AddComponent<GraphicRaycaster>();

            Debug.Log("[Demo Setup] Created Canvas with ScreenSpace-Overlay mode.");
            return canvas;
        }

        #endregion

        #region Stats Panel

        private static GameObject CreateStatsPanel(Transform canvasTransform)
        {
            // Create panel
            GameObject panel = CreateUIPanel("StatsPanel", canvasTransform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 1);
            panelRect.anchorMax = new Vector2(0, 1);
            panelRect.pivot = new Vector2(0, 1);
            panelRect.anchoredPosition = new Vector2(10, -10);
            panelRect.sizeDelta = new Vector2(320, 280);

            Image panelBg = panel.GetComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            // Add VerticalLayoutGroup
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Create title
            CreateText("Title", panel.transform, "LRU CACHE DEMO - ISSUE #5", 18, FontStyle.Bold, TextAnchor.MiddleCenter);

            // Create stats texts
            CreateText("CountText", panel.transform, "Count: 0 / 20", 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            CreateText("HitRateText", panel.transform, "Hit Rate: 0%", 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            CreateText("HitsText", panel.transform, "Hits: 0", 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            CreateText("MissesText", panel.transform, "Misses: 0", 14, FontStyle.Normal, TextAnchor.MiddleLeft);
            CreateText("EvictionsText", panel.transform, "Evictions: 0", 14, FontStyle.Normal, TextAnchor.MiddleLeft);

            // Create hit rate bar
            CreateSlider("HitRateBar", panel.transform, 0f, 1f, 0f, false, 30);

            Debug.Log("[Demo Setup] Created Stats Panel.");
            return panel;
        }

        #endregion

        #region Control Panel

        private static GameObject CreateControlPanel(Transform canvasTransform)
        {
            // Create panel
            GameObject panel = CreateUIPanel("ControlPanel", canvasTransform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(0, 0);
            panelRect.pivot = new Vector2(0, 0);
            panelRect.anchoredPosition = new Vector2(10, 10);
            panelRect.sizeDelta = new Vector2(320, 320);

            Image panelBg = panel.GetComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            // Add VerticalLayoutGroup
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Capacity Section
            CreateText("CapacityLabel", panel.transform, "Cache Capacity", 14, FontStyle.Bold, TextAnchor.MiddleLeft);
            GameObject capacityGroup = CreateHorizontalGroup("CapacityGroup", panel.transform, 10);
            CreateSlider("CapacitySlider", capacityGroup.transform, 5, 50, 20, true, 30);
            CreateText("CapacityValue", capacityGroup.transform, "20", 14, FontStyle.Normal, TextAnchor.MiddleCenter, 60);

            // Buttons
            CreateButton("SimulateButton", panel.transform, "Simulate Random Access", 40);
            CreateButton("ClearButton", panel.transform, "Clear Cache", 40);
            CreateButton("ResetStatsButton", panel.transform, "Reset Statistics", 40);

            // Auto-Simulate Section
            CreateText("AutoSimLabel", panel.transform, "Auto-Simulate", 14, FontStyle.Bold, TextAnchor.MiddleLeft);
            GameObject autoGroup = CreateHorizontalGroup("AutoGroup", panel.transform, 10);
            CreateToggle("AutoSimulateToggle", autoGroup.transform, "Enable", true);

            // Auto-Simulate Speed
            CreateText("SpeedLabel", panel.transform, "Speed (ops/sec)", 12, FontStyle.Normal, TextAnchor.MiddleLeft);
            GameObject speedGroup = CreateHorizontalGroup("SpeedGroup", panel.transform, 10);
            CreateSlider("AutoSimulateSpeedSlider", speedGroup.transform, 0.1f, 2f, 0.5f, true, 30);
            CreateText("AutoSimulateSpeedText", speedGroup.transform, "2.0 ops/sec", 12, FontStyle.Normal, TextAnchor.MiddleCenter, 100);

            Debug.Log("[Demo Setup] Created Control Panel.");
            return panel;
        }

        #endregion

        #region Cache Visualization

        private static GameObject CreateCacheVisualization(Transform canvasTransform)
        {
            // Create panel
            GameObject panel = CreateUIPanel("CacheVisualization", canvasTransform);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 0.5f);
            panelRect.anchorMax = new Vector2(1, 0.5f);
            panelRect.pivot = new Vector2(1, 0.5f);
            panelRect.anchoredPosition = new Vector2(-10, 0);
            panelRect.sizeDelta = new Vector2(400, 600);

            Image panelBg = panel.GetComponent<Image>();
            panelBg.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

            // Add VerticalLayoutGroup
            VerticalLayoutGroup layout = panel.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title
            CreateText("VisualizationTitle", panel.transform, "CACHE STATE (LRU ORDER)", 16, FontStyle.Bold, TextAnchor.MiddleCenter);

            // ScrollView
            GameObject scrollView = CreateScrollView("CacheScrollView", panel.transform);

            // Legend
            CreateText("Legend", panel.transform, "Top = Most Recent | Bottom = Least Recent", 11, FontStyle.Italic, TextAnchor.MiddleCenter);

            Debug.Log("[Demo Setup] Created Cache Visualization Panel.");
            return panel;
        }

        private static GameObject CreateScrollView(string name, Transform parent)
        {
            GameObject scrollViewObj = new GameObject(name);
            scrollViewObj.transform.SetParent(parent, false);

            RectTransform scrollRect = scrollViewObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = new Vector2(0, -120); // Account for title and legend

            Image scrollBg = scrollViewObj.AddComponent<Image>();
            scrollBg.color = new Color(0.05f, 0.05f, 0.05f, 0.5f);

            ScrollRect scroll = scrollViewObj.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            // Viewport
            GameObject viewport = new GameObject("Viewport");
            viewport.transform.SetParent(scrollViewObj.transform, false);
            RectTransform viewportRect = viewport.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewportRect.pivot = new Vector2(0.5f, 1f);

            Image viewportMask = viewport.AddComponent<Image>();
            viewportMask.color = Color.white;
            Mask mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            scroll.viewport = viewportRect;

            // Content
            GameObject content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);

            VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 5;
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRect;

            return scrollViewObj;
        }

        #endregion

        #region Reference Assignment

        private static void AssignReferences(DemoController controller, GameObject statsPanel, GameObject controlPanel, GameObject visualizationPanel)
        {
            SerializedObject so = new SerializedObject(controller);

            // Stats Panel References
            AssignTextReference(so, "countText", statsPanel, "CountText");
            AssignTextReference(so, "hitRateText", statsPanel, "HitRateText");
            AssignTextReference(so, "hitsText", statsPanel, "HitsText");
            AssignTextReference(so, "missesText", statsPanel, "MissesText");
            AssignTextReference(so, "evictionsText", statsPanel, "EvictionsText");
            AssignSliderReference(so, "hitRateBar", statsPanel, "HitRateBar");

            // Control Panel References
            AssignSliderReference(so, "capacitySlider", controlPanel, "CapacityGroup/CapacitySlider");
            AssignTextReference(so, "capacityValueText", controlPanel, "CapacityGroup/CapacityValue");
            AssignButtonReference(so, "simulateButton", controlPanel, "SimulateButton");
            AssignButtonReference(so, "clearButton", controlPanel, "ClearButton");
            AssignButtonReference(so, "resetStatsButton", controlPanel, "ResetStatsButton");
            AssignToggleReference(so, "autoSimulateToggle", controlPanel, "AutoGroup/AutoSimulateToggle");
            AssignSliderReference(so, "autoSimulateSpeedSlider", controlPanel, "SpeedGroup/AutoSimulateSpeedSlider");
            AssignTextReference(so, "autoSimulateSpeedText", controlPanel, "SpeedGroup/AutoSimulateSpeedText");

            // Visualization Panel References
            Transform content = visualizationPanel.transform.Find("CacheScrollView/Viewport/Content");
            if (content != null)
            {
                so.FindProperty("cacheContentParent").objectReferenceValue = content;
            }

            // Assign prefab reference
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab != null)
            {
                so.FindProperty("chunkCacheItemPrefab").objectReferenceValue = prefab;
                Debug.Log("[Demo Setup] Assigned ChunkCacheItem prefab.");
            }
            else
            {
                Debug.LogWarning($"[Demo Setup] Could not find prefab at: {PrefabPath}");
            }

            so.ApplyModifiedProperties();
            Debug.Log("[Demo Setup] All references assigned to DemoController.");
        }

        private static void AssignTextReference(SerializedObject so, string propertyName, GameObject parent, string childPath)
        {
            Transform child = parent.transform.Find(childPath);
            if (child != null)
            {
                Text text = child.GetComponent<Text>();
                if (text != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = text;
                }
            }
        }

        private static void AssignSliderReference(SerializedObject so, string propertyName, GameObject parent, string childPath)
        {
            Transform child = parent.transform.Find(childPath);
            if (child != null)
            {
                Slider slider = child.GetComponent<Slider>();
                if (slider != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = slider;
                }
            }
        }

        private static void AssignButtonReference(SerializedObject so, string propertyName, GameObject parent, string childPath)
        {
            Transform child = parent.transform.Find(childPath);
            if (child != null)
            {
                Button button = child.GetComponent<Button>();
                if (button != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = button;
                }
            }
        }

        private static void AssignToggleReference(SerializedObject so, string propertyName, GameObject parent, string childPath)
        {
            Transform child = parent.transform.Find(childPath);
            if (child != null)
            {
                Toggle toggle = child.GetComponent<Toggle>();
                if (toggle != null)
                {
                    so.FindProperty(propertyName).objectReferenceValue = toggle;
                }
            }
        }

        #endregion

        #region UI Creation Helpers

        private static GameObject CreateUIPanel(string name, Transform parent)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.AddComponent<RectTransform>();
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            return panel;
        }

        private static GameObject CreateText(string name, Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, float width = -1)
        {
            GameObject textObj = new GameObject(name);
            textObj.transform.SetParent(parent, false);

            RectTransform rect = textObj.AddComponent<RectTransform>();
            if (width > 0)
            {
                rect.sizeDelta = new Vector2(width, 25);
            }
            else
            {
                rect.sizeDelta = new Vector2(0, 25);
            }

            Text textComponent = textObj.AddComponent<Text>();
            textComponent.text = text;
            textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComponent.fontSize = fontSize;
            textComponent.fontStyle = fontStyle;
            textComponent.alignment = alignment;
            textComponent.color = Color.white;

            LayoutElement layout = textObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 25;
            if (width > 0)
            {
                layout.preferredWidth = width;
                layout.flexibleWidth = 0;
            }

            return textObj;
        }

        private static GameObject CreateButton(string name, Transform parent, string label, float height)
        {
            GameObject buttonObj = new GameObject(name);
            buttonObj.transform.SetParent(parent, false);

            RectTransform rect = buttonObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            Image image = buttonObj.AddComponent<Image>();
            image.color = new Color(0.3f, 0.5f, 0.7f, 1f);

            Button button = buttonObj.AddComponent<Button>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.5f, 0.7f, 1f);
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.8f, 1f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.6f, 1f);
            button.colors = colors;

            // Button text
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;

            Text text = textObj.AddComponent<Text>();
            text.text = label;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;

            LayoutElement layout = buttonObj.AddComponent<LayoutElement>();
            layout.preferredHeight = height;

            return buttonObj;
        }

        private static GameObject CreateSlider(string name, Transform parent, float minValue, float maxValue, float value, bool wholeNumbers, float height)
        {
            GameObject sliderObj = new GameObject(name);
            sliderObj.transform.SetParent(parent, false);

            RectTransform rect = sliderObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, height);

            Slider slider = sliderObj.AddComponent<Slider>();
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = value;
            slider.wholeNumbers = wholeNumbers;

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(sliderObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Fill Area
            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObj.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.sizeDelta = new Vector2(-10, 0);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.sizeDelta = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = new Color(0.3f, 0.7f, 0.3f, 1f);

            slider.fillRect = fillRect;

            // Handle Area
            GameObject handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(sliderObj.transform, false);
            RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.sizeDelta = new Vector2(-10, 0);

            GameObject handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            RectTransform handleRect = handle.AddComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 0);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.handleRect = handleRect;
            slider.targetGraphic = handleImage;

            LayoutElement layout = sliderObj.AddComponent<LayoutElement>();
            layout.preferredHeight = height;

            return sliderObj;
        }

        private static GameObject CreateToggle(string name, Transform parent, string label, bool isOn)
        {
            GameObject toggleObj = new GameObject(name);
            toggleObj.transform.SetParent(parent, false);

            RectTransform rect = toggleObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 25);

            Toggle toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = isOn;

            // Background
            GameObject bg = new GameObject("Background");
            bg.transform.SetParent(toggleObj.transform, false);
            RectTransform bgRect = bg.AddComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.5f);
            bgRect.anchorMax = new Vector2(0, 0.5f);
            bgRect.pivot = new Vector2(0, 0.5f);
            bgRect.sizeDelta = new Vector2(20, 20);
            bgRect.anchoredPosition = Vector2.zero;
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);

            // Checkmark
            GameObject checkmark = new GameObject("Checkmark");
            checkmark.transform.SetParent(bg.transform, false);
            RectTransform checkRect = checkmark.AddComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = Vector2.zero;
            Image checkImage = checkmark.AddComponent<Image>();
            checkImage.color = new Color(0.3f, 0.7f, 0.3f, 1f);

            toggle.graphic = checkImage;

            // Label
            GameObject labelObj = new GameObject("Label");
            labelObj.transform.SetParent(toggleObj.transform, false);
            RectTransform labelRect = labelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.offsetMin = new Vector2(25, 0);
            labelRect.offsetMax = Vector2.zero;

            Text labelText = labelObj.AddComponent<Text>();
            labelText.text = label;
            labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            labelText.fontSize = 12;
            labelText.alignment = TextAnchor.MiddleLeft;
            labelText.color = Color.white;

            LayoutElement layout = toggleObj.AddComponent<LayoutElement>();
            layout.preferredWidth = 120;
            layout.preferredHeight = 25;

            return toggleObj;
        }

        private static GameObject CreateHorizontalGroup(string name, Transform parent, float spacing)
        {
            GameObject group = new GameObject(name);
            group.transform.SetParent(parent, false);

            RectTransform rect = group.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0, 30);

            HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            LayoutElement layoutElement = group.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 30;

            return group;
        }

        #endregion
    }
}
