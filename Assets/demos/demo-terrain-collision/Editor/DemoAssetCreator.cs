using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Demos.TerrainCollision;

namespace TimeSurvivor.Demos.TerrainCollision.Editor
{
    /// <summary>
    /// Editor utility to create all necessary assets for the Terrain Collision demo.
    /// Run from menu: Tools/Terrain Collision Demo/Create Demo Assets
    /// </summary>
    public static class DemoAssetCreator
    {
        private const string DEMO_PATH = "Assets/demos/demo-terrain-collision";
        private const string CONFIG_PATH = DEMO_PATH + "/Config";
        private const string MATERIALS_PATH = DEMO_PATH + "/Materials";
        private const string PREFABS_PATH = DEMO_PATH + "/Prefabs";
        private const string SCENES_PATH = DEMO_PATH + "/Scenes";

        [MenuItem("Tools/Terrain Collision Demo/Create Demo Assets")]
        public static void CreateDemoAssets()
        {
            Debug.Log("[DemoAssetCreator] Creating demo assets...");

            // Ensure directories exist
            EnsureDirectoriesExist();

            // Create assets
            CreateVoxelConfiguration();
            CreateMaterials();
            CreatePrefabs();
            CreateDemoScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[DemoAssetCreator] Demo assets created successfully!");
        }

        private static void EnsureDirectoriesExist()
        {
            if (!AssetDatabase.IsValidFolder(CONFIG_PATH))
                AssetDatabase.CreateFolder(DEMO_PATH, "Config");

            if (!AssetDatabase.IsValidFolder(MATERIALS_PATH))
                AssetDatabase.CreateFolder(DEMO_PATH, "Materials");

            if (!AssetDatabase.IsValidFolder(PREFABS_PATH))
                AssetDatabase.CreateFolder(DEMO_PATH, "Prefabs");

            if (!AssetDatabase.IsValidFolder(SCENES_PATH))
                AssetDatabase.CreateFolder(DEMO_PATH, "Scenes");
        }

        private static void CreateVoxelConfiguration()
        {
            string path = CONFIG_PATH + "/TerrainCollisionDemoConfig.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<VoxelConfiguration>(path);
            if (existing != null)
            {
                Debug.Log($"[DemoAssetCreator] VoxelConfiguration already exists at {path}");
                return;
            }

            var config = ScriptableObject.CreateInstance<VoxelConfiguration>();

            // Configure settings for collision demo
            config.ChunkSize = 16;
            config.MacroVoxelSize = 1f;
            config.MicroVoxelSize = 0.25f;
            config.RenderDistance = 2;
            config.MaxCachedChunks = 50;

            // Collision settings
            config.EnableCollision = true;
            config.CollisionResolutionDivider = 2; // Half resolution
            config.UseAsyncCollisionBaking = true;
            config.MaxCollisionBakingTimePerFrameMs = 0.5f;
            config.TerrainLayerName = "TerrainStatic";

            // Meshing settings
            config.UseAmortizedMeshing = true;
            config.MaxMeshingTimePerFrameMs = 3f;

            AssetDatabase.CreateAsset(config, path);
            Debug.Log($"[DemoAssetCreator] Created VoxelConfiguration at {path}");
        }

        private static void CreateMaterials()
        {
            // Terrain Material (URP Lit)
            CreateMaterial("TerrainMaterial", "Universal Render Pipeline/Lit", mat =>
            {
                mat.color = Color.white;
                mat.SetFloat("_Smoothness", 0.2f);
            });

            // Physics Object Material
            CreateMaterial("PhysicsObjectMaterial", "Universal Render Pipeline/Lit", mat =>
            {
                mat.color = new Color(0.2f, 0.6f, 1f); // Light blue
                mat.SetFloat("_Smoothness", 0.5f);
                mat.SetFloat("_Metallic", 0.3f);
            });

            // Ground Indicator Material
            CreateMaterial("GroundIndicator", "Universal Render Pipeline/Lit", mat =>
            {
                mat.color = new Color(0.2f, 1f, 0.2f, 0.5f); // Semi-transparent green
                mat.SetFloat("_Smoothness", 0.8f);
                mat.SetFloat("_Surface", 1); // Transparent
                mat.SetFloat("_Blend", 0); // Alpha blending
            });
        }

        private static void CreateMaterial(string name, string shaderName, System.Action<Material> configure)
        {
            string path = $"{MATERIALS_PATH}/{name}.mat";

            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                Debug.Log($"[DemoAssetCreator] Material {name} already exists");
                return;
            }

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError($"[DemoAssetCreator] Shader {shaderName} not found!");
                return;
            }

            var material = new Material(shader) { name = name };
            configure?.Invoke(material);

            AssetDatabase.CreateAsset(material, path);
            Debug.Log($"[DemoAssetCreator] Created material {name} at {path}");
        }

        private static void CreatePrefabs()
        {
            CreatePhysicsSphere();
            CreatePhysicsCube();
            CreateDemoCamera();
        }

        private static void CreatePhysicsSphere()
        {
            string path = $"{PREFABS_PATH}/PhysicsSphere.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log("[DemoAssetCreator] PhysicsSphere prefab already exists");
                return;
            }

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "PhysicsSphere";
            sphere.transform.localScale = Vector3.one * 0.5f;

            // Add Rigidbody
            var rb = sphere.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.drag = 0.1f;
            rb.angularDrag = 0.05f;

            // Assign material
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIALS_PATH}/PhysicsObjectMaterial.mat");
            if (material != null)
            {
                sphere.GetComponent<Renderer>().sharedMaterial = material;
            }

            PrefabUtility.SaveAsPrefabAsset(sphere, path);
            Object.DestroyImmediate(sphere);
            Debug.Log($"[DemoAssetCreator] Created PhysicsSphere prefab at {path}");
        }

        private static void CreatePhysicsCube()
        {
            string path = $"{PREFABS_PATH}/PhysicsCube.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log("[DemoAssetCreator] PhysicsCube prefab already exists");
                return;
            }

            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "PhysicsCube";
            cube.transform.localScale = Vector3.one * 0.5f;

            // Add Rigidbody
            var rb = cube.AddComponent<Rigidbody>();
            rb.mass = 1f;
            rb.drag = 0.1f;
            rb.angularDrag = 0.05f;

            // Assign material
            var material = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIALS_PATH}/PhysicsObjectMaterial.mat");
            if (material != null)
            {
                cube.GetComponent<Renderer>().sharedMaterial = material;
            }

            PrefabUtility.SaveAsPrefabAsset(cube, path);
            Object.DestroyImmediate(cube);
            Debug.Log($"[DemoAssetCreator] Created PhysicsCube prefab at {path}");
        }

        private static void CreateDemoCamera()
        {
            string path = $"{PREFABS_PATH}/DemoCamera.prefab";

            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
            {
                Debug.Log("[DemoAssetCreator] DemoCamera prefab already exists");
                return;
            }

            // Create player GameObject
            var player = new GameObject("Player");

            // Add CharacterController
            var charController = player.AddComponent<CharacterController>();
            charController.radius = 0.5f;
            charController.height = 1.8f;
            charController.center = new Vector3(0, 0.9f, 0);

            // Add SimpleCharacterController
            var playerController = player.AddComponent<SimpleCharacterController>();

            // Create camera as child
            var cameraObj = new GameObject("Camera");
            cameraObj.transform.SetParent(player.transform);
            cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0); // Eye height

            var camera = cameraObj.AddComponent<Camera>();
            camera.fieldOfView = 75f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 500f;

            // Tag as MainCamera
            cameraObj.tag = "MainCamera";

            // Add AudioListener
            cameraObj.AddComponent<AudioListener>();

            PrefabUtility.SaveAsPrefabAsset(player, path);
            Object.DestroyImmediate(player);
            Debug.Log($"[DemoAssetCreator] Created DemoCamera prefab at {path}");
        }

        private static void CreateDemoScene()
        {
            string scenePath = $"{SCENES_PATH}/TerrainCollisionDemo.unity";

            // Check if scene already exists
            if (System.IO.File.Exists(scenePath))
            {
                Debug.Log($"[DemoAssetCreator] Scene already exists at {scenePath}");
                return;
            }

            // Create new scene
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add directional light
            var light = new GameObject("Directional Light");
            var lightComp = light.AddComponent<Light>();
            lightComp.type = LightType.Directional;
            lightComp.intensity = 1f;
            lightComp.shadows = LightShadows.Soft;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Load and instantiate player prefab
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/DemoCamera.prefab");
            if (playerPrefab != null)
            {
                var player = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
                player.transform.position = new Vector3(32, 10, 32); // Center of terrain, elevated
            }

            // Create TerrainManager GameObject
            var terrainManager = new GameObject("TerrainManager");
            var demoController = terrainManager.AddComponent<CollisionDemoController>();

            // Assign configuration
            var config = AssetDatabase.LoadAssetAtPath<VoxelConfiguration>($"{CONFIG_PATH}/TerrainCollisionDemoConfig.asset");
            if (config != null)
            {
                var configField = typeof(CollisionDemoController).GetField("config", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                configField?.SetValue(demoController, config);
            }

            // Assign terrain material
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIALS_PATH}/TerrainMaterial.mat");
            if (terrainMat != null)
            {
                var matField = typeof(CollisionDemoController).GetField("terrainMaterial", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                matField?.SetValue(demoController, terrainMat);
            }

            // Add PhysicsObjectSpawner
            var spawner = terrainManager.AddComponent<PhysicsObjectSpawner>();
            var spherePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/PhysicsSphere.prefab");
            var cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/PhysicsCube.prefab");

            if (spherePrefab != null && cubePrefab != null)
            {
                var sphereField = typeof(PhysicsObjectSpawner).GetField("spherePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var cubeField = typeof(PhysicsObjectSpawner).GetField("cubePrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                sphereField?.SetValue(spawner, spherePrefab);
                cubeField?.SetValue(spawner, cubePrefab);
            }

            // Add CollisionVisualizer
            terrainManager.AddComponent<CollisionVisualizer>();

            // Add DemoUI
            terrainManager.AddComponent<DemoUI>();

            // Save scene
            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[DemoAssetCreator] Created demo scene at {scenePath}");
        }

        [MenuItem("Tools/Terrain Collision Demo/Create TerrainStatic Layer")]
        public static void CreateTerrainStaticLayer()
        {
            // Check if layer exists
            int layerIndex = LayerMask.NameToLayer("TerrainStatic");
            if (layerIndex >= 0)
            {
                Debug.Log("[DemoAssetCreator] TerrainStatic layer already exists");
                return;
            }

            // Get TagManager asset
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layersProp = tagManager.FindProperty("layers");

            // Find empty layer slot
            for (int i = 8; i < layersProp.arraySize; i++)
            {
                SerializedProperty layerSP = layersProp.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(layerSP.stringValue))
                {
                    layerSP.stringValue = "TerrainStatic";
                    tagManager.ApplyModifiedProperties();
                    Debug.Log($"[DemoAssetCreator] Created TerrainStatic layer at index {i}");
                    return;
                }
            }

            Debug.LogWarning("[DemoAssetCreator] No empty layer slots available!");
        }
    }
}
