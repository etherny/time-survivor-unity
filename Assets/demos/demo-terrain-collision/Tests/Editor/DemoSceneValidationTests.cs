using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Demos.TerrainCollision.Tests.Editor
{
    /// <summary>
    /// Edit Mode tests to validate the Terrain Collision demo scene setup.
    /// These tests ensure that all GameObjects, components, and references are properly configured.
    /// </summary>
    public class DemoSceneValidationTests
    {
        private const string SCENE_PATH = "Assets/demos/demo-terrain-collision/Scenes/TerrainCollisionDemo.unity";

        private Scene testScene;

        [SetUp]
        public void SetUp()
        {
            // Check if scene exists
            if (!System.IO.File.Exists(SCENE_PATH))
            {
                Assert.Inconclusive($"Demo scene not found at {SCENE_PATH}. Run 'Tools > Terrain Collision Demo > Create Demo Assets' first.");
            }

            // Open the demo scene
            testScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
            Assert.IsTrue(testScene.IsValid(), "Failed to load demo scene");
        }

        [TearDown]
        public void TearDown()
        {
            // Don't save changes made during tests
            if (testScene.IsValid())
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }
        }

        #region Scene Hierarchy Tests

        [Test]
        public void PlayerGameObject_Exists()
        {
            var player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Player GameObject not found in scene");
        }

        [Test]
        public void TerrainManagerGameObject_Exists()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found in scene");
        }

        [Test]
        public void InputManagerGameObject_Exists()
        {
            var inputManager = GameObject.Find("InputManager");
            Assert.IsNotNull(inputManager, "InputManager GameObject not found in scene");
        }

        [Test]
        public void DirectionalLightGameObject_Exists()
        {
            var light = GameObject.Find("Directional Light");
            Assert.IsNotNull(light, "Directional Light GameObject not found in scene");
        }

        [Test]
        public void CameraGameObject_Exists_AsChildOfPlayer()
        {
            var player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Player GameObject not found");

            var camera = player.transform.Find("Camera");
            Assert.IsNotNull(camera, "Camera not found as child of Player");
        }

        #endregion

        #region Player Component Tests

        [Test]
        public void Player_HasCharacterController()
        {
            var player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Player GameObject not found");

            var charController = player.GetComponent<CharacterController>();
            Assert.IsNotNull(charController, "Player does not have CharacterController component");
        }

        [Test]
        public void Player_HasSimpleCharacterController()
        {
            var player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Player GameObject not found");

            var playerController = player.GetComponent<SimpleCharacterController>();
            Assert.IsNotNull(playerController, "Player does not have SimpleCharacterController component");
        }

        [Test]
        public void Camera_HasCameraComponent()
        {
            var player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Player GameObject not found");

            var camera = player.transform.Find("Camera");
            Assert.IsNotNull(camera, "Camera not found");

            var cameraComponent = camera.GetComponent<Camera>();
            Assert.IsNotNull(cameraComponent, "Camera does not have Camera component");
        }

        [Test]
        public void Camera_HasAudioListener()
        {
            var player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Player GameObject not found");

            var camera = player.transform.Find("Camera");
            Assert.IsNotNull(camera, "Camera not found");

            var audioListener = camera.GetComponent<AudioListener>();
            Assert.IsNotNull(audioListener, "Camera does not have AudioListener component");
        }

        [Test]
        public void Camera_IsMainCamera()
        {
            var player = GameObject.Find("Player");
            Assert.IsNotNull(player, "Player GameObject not found");

            var camera = player.transform.Find("Camera");
            Assert.IsNotNull(camera, "Camera not found");

            Assert.AreEqual("MainCamera", camera.tag, "Camera is not tagged as MainCamera");
        }

        #endregion

        #region TerrainManager Component Tests

        [Test]
        public void TerrainManager_HasCollisionDemoController()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoController = terrainManager.GetComponent<CollisionDemoController>();
            Assert.IsNotNull(demoController, "TerrainManager does not have CollisionDemoController component");
        }

        [Test]
        public void TerrainManager_HasPhysicsObjectSpawner()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var spawner = terrainManager.GetComponent<PhysicsObjectSpawner>();
            Assert.IsNotNull(spawner, "TerrainManager does not have PhysicsObjectSpawner component");
        }

        [Test]
        public void TerrainManager_HasCollisionVisualizer()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var visualizer = terrainManager.GetComponent<CollisionVisualizer>();
            Assert.IsNotNull(visualizer, "TerrainManager does not have CollisionVisualizer component");
        }

        [Test]
        public void TerrainManager_HasDemoUI()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoUI = terrainManager.GetComponent<DemoUI>();
            Assert.IsNotNull(demoUI, "TerrainManager does not have DemoUI component");
        }

        #endregion

        #region Reference Assignment Tests

        [Test]
        public void CollisionDemoController_ConfigIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoController = terrainManager.GetComponent<CollisionDemoController>();
            Assert.IsNotNull(demoController, "CollisionDemoController component not found");

            var so = new SerializedObject(demoController);
            var configProp = so.FindProperty("config");
            Assert.IsNotNull(configProp.objectReferenceValue, "CollisionDemoController.config is not assigned");
            Assert.IsInstanceOf<VoxelConfiguration>(configProp.objectReferenceValue, "config is not a VoxelConfiguration");
        }

        [Test]
        public void CollisionDemoController_TerrainMaterialIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoController = terrainManager.GetComponent<CollisionDemoController>();
            Assert.IsNotNull(demoController, "CollisionDemoController component not found");

            var so = new SerializedObject(demoController);
            var materialProp = so.FindProperty("terrainMaterial");
            Assert.IsNotNull(materialProp.objectReferenceValue, "CollisionDemoController.terrainMaterial is not assigned");
            Assert.IsInstanceOf<Material>(materialProp.objectReferenceValue, "terrainMaterial is not a Material");
        }

        [Test]
        public void CollisionDemoController_PlayerTransformIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoController = terrainManager.GetComponent<CollisionDemoController>();
            Assert.IsNotNull(demoController, "CollisionDemoController component not found");

            var so = new SerializedObject(demoController);
            var playerTransformProp = so.FindProperty("playerTransform");
            Assert.IsNotNull(playerTransformProp.objectReferenceValue, "CollisionDemoController.playerTransform is not assigned");
        }

        [Test]
        public void PhysicsObjectSpawner_SpherePrefabIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var spawner = terrainManager.GetComponent<PhysicsObjectSpawner>();
            Assert.IsNotNull(spawner, "PhysicsObjectSpawner component not found");

            var so = new SerializedObject(spawner);
            var spherePrefabProp = so.FindProperty("spherePrefab");
            Assert.IsNotNull(spherePrefabProp.objectReferenceValue, "PhysicsObjectSpawner.spherePrefab is not assigned");
        }

        [Test]
        public void PhysicsObjectSpawner_CubePrefabIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var spawner = terrainManager.GetComponent<PhysicsObjectSpawner>();
            Assert.IsNotNull(spawner, "PhysicsObjectSpawner component not found");

            var so = new SerializedObject(spawner);
            var cubePrefabProp = so.FindProperty("cubePrefab");
            Assert.IsNotNull(cubePrefabProp.objectReferenceValue, "PhysicsObjectSpawner.cubePrefab is not assigned");
        }

        [Test]
        public void PhysicsObjectSpawner_PlayerTransformIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var spawner = terrainManager.GetComponent<PhysicsObjectSpawner>();
            Assert.IsNotNull(spawner, "PhysicsObjectSpawner component not found");

            var so = new SerializedObject(spawner);
            var playerTransformProp = so.FindProperty("playerTransform");
            Assert.IsNotNull(playerTransformProp.objectReferenceValue, "PhysicsObjectSpawner.playerTransform is not assigned");
        }

        [Test]
        public void DemoUI_PlayerControllerIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoUI = terrainManager.GetComponent<DemoUI>();
            Assert.IsNotNull(demoUI, "DemoUI component not found");

            var so = new SerializedObject(demoUI);
            var playerControllerProp = so.FindProperty("playerController");
            Assert.IsNotNull(playerControllerProp.objectReferenceValue, "DemoUI.playerController is not assigned");
        }

        [Test]
        public void DemoUI_ObjectSpawnerIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoUI = terrainManager.GetComponent<DemoUI>();
            Assert.IsNotNull(demoUI, "DemoUI component not found");

            var so = new SerializedObject(demoUI);
            var objectSpawnerProp = so.FindProperty("objectSpawner");
            Assert.IsNotNull(objectSpawnerProp.objectReferenceValue, "DemoUI.objectSpawner is not assigned");
        }

        [Test]
        public void DemoUI_VisualizerIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoUI = terrainManager.GetComponent<DemoUI>();
            Assert.IsNotNull(demoUI, "DemoUI component not found");

            var so = new SerializedObject(demoUI);
            var visualizerProp = so.FindProperty("visualizer");
            Assert.IsNotNull(visualizerProp.objectReferenceValue, "DemoUI.visualizer is not assigned");
        }

        [Test]
        public void DemoUI_DemoControllerIsAssigned()
        {
            var terrainManager = GameObject.Find("TerrainManager");
            Assert.IsNotNull(terrainManager, "TerrainManager GameObject not found");

            var demoUI = terrainManager.GetComponent<DemoUI>();
            Assert.IsNotNull(demoUI, "DemoUI component not found");

            var so = new SerializedObject(demoUI);
            var demoControllerProp = so.FindProperty("demoController");
            Assert.IsNotNull(demoControllerProp.objectReferenceValue, "DemoUI.demoController is not assigned");
        }

        #endregion

        #region InputManager Component Tests

        [Test]
        public void InputManager_HasDemoInputManager()
        {
            var inputManager = GameObject.Find("InputManager");
            Assert.IsNotNull(inputManager, "InputManager GameObject not found");

            var demoInputManager = inputManager.GetComponent<DemoInputManager>();
            Assert.IsNotNull(demoInputManager, "InputManager does not have DemoInputManager component");
        }

        [Test]
        public void DemoInputManager_InputActionsIsAssigned()
        {
            var inputManager = GameObject.Find("InputManager");
            Assert.IsNotNull(inputManager, "InputManager GameObject not found");

            var demoInputManager = inputManager.GetComponent<DemoInputManager>();
            Assert.IsNotNull(demoInputManager, "DemoInputManager component not found");

            var so = new SerializedObject(demoInputManager);
            var inputActionsProp = so.FindProperty("inputActions");
            Assert.IsNotNull(inputActionsProp.objectReferenceValue, "DemoInputManager.inputActions is not assigned");
        }

        #endregion

        #region Layer Tests

        [Test]
        public void TerrainStaticLayer_Exists()
        {
            int layerIndex = LayerMask.NameToLayer("TerrainStatic");
            Assert.GreaterOrEqual(layerIndex, 0, "TerrainStatic layer does not exist. Run 'Tools > Terrain Collision Demo > Create TerrainStatic Layer' first.");
        }

        #endregion

        #region Asset Existence Tests

        [Test]
        public void VoxelConfiguration_AssetExists()
        {
            var config = AssetDatabase.LoadAssetAtPath<VoxelConfiguration>("Assets/demos/demo-terrain-collision/Config/TerrainCollisionDemoConfig.asset");
            Assert.IsNotNull(config, "VoxelConfiguration asset not found at expected path");
        }

        [Test]
        public void TerrainMaterial_AssetExists()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>("Assets/demos/demo-terrain-collision/Materials/TerrainMaterial.mat");
            Assert.IsNotNull(material, "TerrainMaterial asset not found at expected path");
        }

        [Test]
        public void PhysicsSphere_PrefabExists()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/demos/demo-terrain-collision/Prefabs/PhysicsSphere.prefab");
            Assert.IsNotNull(prefab, "PhysicsSphere prefab not found at expected path");
        }

        [Test]
        public void PhysicsCube_PrefabExists()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/demos/demo-terrain-collision/Prefabs/PhysicsCube.prefab");
            Assert.IsNotNull(prefab, "PhysicsCube prefab not found at expected path");
        }

        [Test]
        public void DemoCamera_PrefabExists()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/demos/demo-terrain-collision/Prefabs/DemoCamera.prefab");
            Assert.IsNotNull(prefab, "DemoCamera prefab not found at expected path");
        }

        [Test]
        public void DemoInputActions_AssetExists()
        {
            var inputActions = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>("Assets/demos/demo-terrain-collision/Input/DemoInputActions.inputactions");
            Assert.IsNotNull(inputActions, "DemoInputActions asset not found at expected path");
        }

        #endregion
    }
}
