using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Demos.TerrainCollision;

namespace TimeSurvivor.Demos.TerrainCollision.Editor.Tests
{
    /// <summary>
    /// Tests for validating the demo asset creation workflow.
    /// Ensures that CreateDemoAssets() properly creates all required assets.
    /// </summary>
    public class DemoAssetCreationTests
    {
        private const string DEMO_PATH = "Assets/demos/demo-terrain-collision";
        private const string CONFIG_PATH = DEMO_PATH + "/Config";
        private const string MATERIALS_PATH = DEMO_PATH + "/Materials";
        private const string PREFABS_PATH = DEMO_PATH + "/Prefabs";
        private const string INPUT_PATH = DEMO_PATH + "/Input";
        private const string SCENES_PATH = DEMO_PATH + "/Scenes";

        [SetUp]
        public void Setup()
        {
            // Clean up any existing test assets before each test
            CleanupTestAssets();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up after each test
            CleanupTestAssets();
        }

        private void CleanupTestAssets()
        {
            // Delete scene first (it references other assets)
            if (System.IO.File.Exists($"{SCENES_PATH}/TerrainCollisionDemo.unity"))
            {
                AssetDatabase.DeleteAsset($"{SCENES_PATH}/TerrainCollisionDemo.unity");
            }

            // Delete all created assets
            if (AssetDatabase.IsValidFolder(CONFIG_PATH))
            {
                var configAssets = AssetDatabase.FindAssets("t:VoxelConfiguration", new[] { CONFIG_PATH });
                foreach (var guid in configAssets)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            if (AssetDatabase.IsValidFolder(MATERIALS_PATH))
            {
                var materials = AssetDatabase.FindAssets("t:Material", new[] { MATERIALS_PATH });
                foreach (var guid in materials)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            if (AssetDatabase.IsValidFolder(PREFABS_PATH))
            {
                var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { PREFABS_PATH });
                foreach (var guid in prefabs)
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));
                }
            }

            if (System.IO.File.Exists($"{INPUT_PATH}/DemoInputActions.inputactions"))
            {
                AssetDatabase.DeleteAsset($"{INPUT_PATH}/DemoInputActions.inputactions");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        [Test]
        public void CreateDemoAssets_CreatesVoxelConfiguration()
        {
            // Arrange
            string path = $"{CONFIG_PATH}/TerrainCollisionDemoConfig.asset";

            // Expect DisplayDialog assertions (Unity calls these in batch mode)
            LogAssert.Expect(LogType.Assert, "Cancelling DisplayDialog: Success Demo assets created and validated successfully!\nThis should not be called in batch mode.");

            // Act
            DemoAssetCreator.CreateDemoAssets();

            // Assert
            var config = AssetDatabase.LoadAssetAtPath<VoxelConfiguration>(path);
            Assert.IsNotNull(config, "VoxelConfiguration should be created");
            Assert.AreEqual(16, config.ChunkSize, "ChunkSize should be 16");
            Assert.AreEqual(1f, config.MacroVoxelSize, "MacroVoxelSize should be 1");
            Assert.AreEqual(0.25f, config.MicroVoxelSize, "MicroVoxelSize should be 0.25");
            Assert.AreEqual(2, config.RenderDistance, "RenderDistance should be 2");
            Assert.IsTrue(config.EnableCollision, "Collision should be enabled");
        }

        [Test]
        public void CreateDemoAssets_CreatesAllMaterials()
        {
            // Expect DisplayDialog assertions
            LogAssert.Expect(LogType.Assert, "Cancelling DisplayDialog: Success Demo assets created and validated successfully!\nThis should not be called in batch mode.");

            // Act
            DemoAssetCreator.CreateDemoAssets();

            // Assert
            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIALS_PATH}/TerrainMaterial.mat");
            Assert.IsNotNull(terrainMat, "TerrainMaterial should be created");

            var physicsMat = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIALS_PATH}/PhysicsObjectMaterial.mat");
            Assert.IsNotNull(physicsMat, "PhysicsObjectMaterial should be created");

            var indicatorMat = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIALS_PATH}/GroundIndicator.mat");
            Assert.IsNotNull(indicatorMat, "GroundIndicator material should be created");
        }

        [Test]
        public void CreateDemoAssets_CreatesAllPrefabs()
        {
            // Expect DisplayDialog assertions
            LogAssert.Expect(LogType.Assert, "Cancelling DisplayDialog: Success Demo assets created and validated successfully!\nThis should not be called in batch mode.");

            // Act
            DemoAssetCreator.CreateDemoAssets();

            // Assert
            var spherePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/PhysicsSphere.prefab");
            Assert.IsNotNull(spherePrefab, "PhysicsSphere prefab should be created");
            Assert.IsNotNull(spherePrefab.GetComponent<Rigidbody>(), "PhysicsSphere should have Rigidbody");

            var cubePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/PhysicsCube.prefab");
            Assert.IsNotNull(cubePrefab, "PhysicsCube prefab should be created");
            Assert.IsNotNull(cubePrefab.GetComponent<Rigidbody>(), "PhysicsCube should have Rigidbody");

            var cameraPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/DemoCamera.prefab");
            Assert.IsNotNull(cameraPrefab, "DemoCamera prefab should be created");
            Assert.IsNotNull(cameraPrefab.GetComponent<SimpleCharacterController>(), "DemoCamera should have SimpleCharacterController");
        }

        [Test]
        public void CreateDemoAssets_CreatesInputActions()
        {
            // Arrange
            string path = $"{INPUT_PATH}/DemoInputActions.inputactions";

            // Expect DisplayDialog assertions
            LogAssert.Expect(LogType.Assert, "Cancelling DisplayDialog: Success Demo assets created and validated successfully!\nThis should not be called in batch mode.");

            // Act
            DemoAssetCreator.CreateDemoAssets();

            // Assert
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            Assert.IsNotNull(inputActions, "InputActionAsset should be created");

            // Verify action map exists
            var playerMap = inputActions.FindActionMap("Player");
            Assert.IsNotNull(playerMap, "Player action map should exist");

            // Verify all required actions exist
            Assert.IsNotNull(playerMap.FindAction("Move"), "Move action should exist");
            Assert.IsNotNull(playerMap.FindAction("Look"), "Look action should exist");
            Assert.IsNotNull(playerMap.FindAction("Jump"), "Jump action should exist");
            Assert.IsNotNull(playerMap.FindAction("SpawnObject"), "SpawnObject action should exist");
            Assert.IsNotNull(playerMap.FindAction("ToggleVisualization"), "ToggleVisualization action should exist");
            Assert.IsNotNull(playerMap.FindAction("ToggleUI"), "ToggleUI action should exist");
            Assert.IsNotNull(playerMap.FindAction("UnlockCursor"), "UnlockCursor action should exist");
        }

        [Test]
        public void CreateDemoAssets_InputActionsHaveCorrectBindings()
        {
            // Expect DisplayDialog assertions
            LogAssert.Expect(LogType.Assert, "Cancelling DisplayDialog: Success Demo assets created and validated successfully!\nThis should not be called in batch mode.");

            // Act
            DemoAssetCreator.CreateDemoAssets();

            // Arrange
            string path = $"{INPUT_PATH}/DemoInputActions.inputactions";
            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            var playerMap = inputActions.FindActionMap("Player");

            // Assert Move action has 2D Vector composite
            var moveAction = playerMap.FindAction("Move");
            Assert.AreEqual(InputActionType.Value, moveAction.type, "Move should be Value type");
            Assert.Greater(moveAction.bindings.Count, 0, "Move should have bindings");

            // Assert Look action has mouse binding
            var lookAction = playerMap.FindAction("Look");
            Assert.AreEqual(InputActionType.Value, lookAction.type, "Look should be Value type");
            Assert.Greater(lookAction.bindings.Count, 0, "Look should have bindings");

            // Assert Jump action is button
            var jumpAction = playerMap.FindAction("Jump");
            Assert.AreEqual(InputActionType.Button, jumpAction.type, "Jump should be Button type");
            Assert.Greater(jumpAction.bindings.Count, 0, "Jump should have bindings");
        }

        [Test]
        public void CreateDemoAssets_AssetsAreLoadableBeforeSceneCreation()
        {
            // This test validates that assets are saved and refreshed BEFORE scene creation
            // This ensures CreateDemoScene() can successfully load all assets

            // Expect DisplayDialog assertions
            LogAssert.Expect(LogType.Assert, "Cancelling DisplayDialog: Success Demo assets created and validated successfully!\nThis should not be called in batch mode.");

            // Act
            DemoAssetCreator.CreateDemoAssets();

            // Assert - All assets should be loadable
            var config = AssetDatabase.LoadAssetAtPath<VoxelConfiguration>($"{CONFIG_PATH}/TerrainCollisionDemoConfig.asset");
            Assert.IsNotNull(config, "Config should be loadable");

            var terrainMat = AssetDatabase.LoadAssetAtPath<Material>($"{MATERIALS_PATH}/TerrainMaterial.mat");
            Assert.IsNotNull(terrainMat, "TerrainMaterial should be loadable");

            var inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>($"{INPUT_PATH}/DemoInputActions.inputactions");
            Assert.IsNotNull(inputActions, "InputActions should be loadable");

            var spherePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PREFABS_PATH}/PhysicsSphere.prefab");
            Assert.IsNotNull(spherePrefab, "PhysicsSphere should be loadable");
        }

        [Test]
        public void CreateInputActions_CreatesAssetWithCorrectStructure()
        {
            // This test directly validates the CreateInputActions() implementation
            // by ensuring the asset structure is correct

            // Expect DisplayDialog assertions
            LogAssert.Expect(LogType.Assert, "Cancelling DisplayDialog: Success Demo assets created and validated successfully!\nThis should not be called in batch mode.");

            // Act
            DemoAssetCreator.CreateDemoAssets();

            // Arrange
            string path = $"{INPUT_PATH}/DemoInputActions.inputactions";
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);

            // Assert
            Assert.IsNotNull(asset, "Asset should exist");
            Assert.AreEqual("DemoInputActions", asset.name, "Asset name should be correct");

            var actionMaps = asset.actionMaps;
            Assert.AreEqual(1, actionMaps.Count, "Should have exactly 1 action map");
            Assert.AreEqual("Player", actionMaps[0].name, "Action map should be named 'Player'");

            var actions = actionMaps[0].actions;
            Assert.AreEqual(7, actions.Count, "Should have exactly 7 actions");
        }
    }
}
