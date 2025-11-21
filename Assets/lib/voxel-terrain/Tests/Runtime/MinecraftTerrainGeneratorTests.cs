using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain.Tests
{
    /// <summary>
    /// Integration tests for MinecraftTerrainGenerator.
    /// Tests full terrain generation workflow, validation, events, and cleanup.
    /// </summary>
    [TestFixture]
    public class MinecraftTerrainGeneratorTests
    {
        private GameObject _testObject;
        private MinecraftTerrainGenerator _generator;
        private VoxelConfiguration _voxelConfig;
        private MinecraftTerrainConfiguration _minecraftConfig;
        private Material _testMaterial;

        [SetUp]
        public void SetUp()
        {
            // Create test GameObject with generator
            _testObject = new GameObject("TestMinecraftTerrainGenerator");
            _generator = _testObject.AddComponent<MinecraftTerrainGenerator>();

            // Create test configurations
            _voxelConfig = ScriptableObject.CreateInstance<VoxelConfiguration>();
            _voxelConfig.ChunkSize = 16; // Small for fast tests
            _voxelConfig.MacroVoxelSize = 0.2f;
            _voxelConfig.Seed = 12345;
            _voxelConfig.MaxChunksLoadedPerFrame = 10;

            _minecraftConfig = ScriptableObject.CreateInstance<MinecraftTerrainConfiguration>();
            _minecraftConfig.WorldSizeX = 2; // 2×2×2 = 8 chunks (small for tests)
            _minecraftConfig.WorldSizeY = 2;
            _minecraftConfig.WorldSizeZ = 2;
            _minecraftConfig.BaseTerrainHeight = 1; // 1 chunk = 16 voxels
            _minecraftConfig.TerrainVariation = 0; // Flat terrain for predictability
            _minecraftConfig.HeightmapFrequency = 0.05f;
            _minecraftConfig.HeightmapOctaves = 2;
            _minecraftConfig.GrassLayerThickness = 1;
            _minecraftConfig.DirtLayerThickness = 2;
            _minecraftConfig.GenerateWater = false;

            // Create test material
            _testMaterial = new Material(Shader.Find("Standard"));

            // Assign via reflection (SerializeField)
            var voxelConfigField = typeof(MinecraftTerrainGenerator).GetField("_voxelConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            voxelConfigField.SetValue(_generator, _voxelConfig);

            var minecraftConfigField = typeof(MinecraftTerrainGenerator).GetField("_minecraftConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            minecraftConfigField.SetValue(_generator, _minecraftConfig);

            var materialField = typeof(MinecraftTerrainGenerator).GetField("_chunkMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            materialField.SetValue(_generator, _testMaterial);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup
            if (_generator != null)
            {
                _generator.ClearTerrain();
            }

            if (_testObject != null)
            {
                Object.DestroyImmediate(_testObject);
            }

            if (_voxelConfig != null)
            {
                Object.DestroyImmediate(_voxelConfig);
            }

            if (_minecraftConfig != null)
            {
                Object.DestroyImmediate(_minecraftConfig);
            }

            if (_testMaterial != null)
            {
                Object.DestroyImmediate(_testMaterial);
            }
        }

        // ========== Test: Validation ==========

        [Test]
        public void Test_Validation_RejectsNullConfiguration()
        {
            // Arrange: Set configuration to null
            var configField = typeof(MinecraftTerrainGenerator).GetField("_voxelConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            configField.SetValue(_generator, null);

            // Act: Call private ValidateConfiguration method via reflection
            var validateMethod = typeof(MinecraftTerrainGenerator).GetMethod("ValidateConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool result = (bool)validateMethod.Invoke(_generator, null);

            // Assert: Validation should fail
            Assert.IsFalse(result, "Validation should fail when VoxelConfiguration is null");
        }

        [Test]
        public void Test_Validation_RejectsInvalidTerrainHeight()
        {
            // Arrange: Set terrain height exceeding world size
            _minecraftConfig.WorldSizeY = 2; // 32 voxels total height
            _minecraftConfig.BaseTerrainHeight = 1; // 16 voxels
            _minecraftConfig.TerrainVariation = 2; // ±32 voxels -> max = 48 voxels (exceeds 32)

            // Act: Call ValidateConfiguration
            var validateMethod = typeof(MinecraftTerrainGenerator).GetMethod("ValidateConfiguration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            bool result = (bool)validateMethod.Invoke(_generator, null);

            // Assert: Validation should fail
            Assert.IsFalse(result, "Validation should fail when max terrain height exceeds WorldSizeY");
        }

        // ========== Test: Generation ==========

        [UnityTest]
        public IEnumerator Test_GenerateTerrain_CreatesCorrectNumberOfChunks()
        {
            // Arrange
            int expectedChunks = _minecraftConfig.TotalChunks; // 2×2×2 = 8
            int progressEventCount = 0;
            bool completedFired = false;

            _generator.OnGenerationProgress.AddListener((current, total) => progressEventCount++);
            _generator.OnGenerationCompleted.AddListener((elapsedMs) => completedFired = true);

            // Act: Generate terrain
            _generator.GenerateTerrain();

            // Wait for generation to complete (max 5 seconds)
            float timeout = 5f;
            float elapsed = 0f;
            while (_generator.IsGenerating && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert: Generation completed
            Assert.IsFalse(_generator.IsGenerating, "Generation should complete");
            Assert.IsTrue(completedFired, "OnGenerationCompleted event should fire");

            // Assert: ChunkManager exists and has chunks
            Assert.IsNotNull(_generator.ChunkManager, "ChunkManager should be created");

            int loadedChunks = 0;
            foreach (var chunk in _generator.ChunkManager.GetAllChunks())
            {
                loadedChunks++;
            }

            Assert.AreEqual(expectedChunks, loadedChunks,
                $"Should create {expectedChunks} chunks. Found: {loadedChunks}");

            Debug.Log($"SUCCESS: Generated {loadedChunks} chunks with {progressEventCount} progress updates");
        }

        [UnityTest]
        public IEnumerator Test_GenerateTerrain_EventsAreFired()
        {
            // Arrange
            bool startedFired = false;
            bool progressFired = false;
            bool completedFired = false;
            float completedElapsedMs = -1f;

            _generator.OnGenerationStarted.AddListener(() => startedFired = true);
            _generator.OnGenerationProgress.AddListener((current, total) => progressFired = true);
            _generator.OnGenerationCompleted.AddListener((elapsedMs) =>
            {
                completedFired = true;
                completedElapsedMs = elapsedMs;
            });

            // Act: Generate terrain
            _generator.GenerateTerrain();

            // Wait for completion
            float timeout = 5f;
            float elapsed = 0f;
            while (_generator.IsGenerating && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert: All events fired
            Assert.IsTrue(startedFired, "OnGenerationStarted should fire");
            Assert.IsTrue(progressFired, "OnGenerationProgress should fire at least once");
            Assert.IsTrue(completedFired, "OnGenerationCompleted should fire");
            Assert.Greater(completedElapsedMs, 0f, "Completed event should provide elapsed time");

            Debug.Log($"SUCCESS: All events fired. Elapsed time: {completedElapsedMs}ms");
        }

        // ========== Test: Cleanup ==========

        [UnityTest]
        public IEnumerator Test_ClearTerrain_CleansUpResources()
        {
            // Arrange: Generate terrain first
            _generator.GenerateTerrain();

            float timeout = 5f;
            float elapsed = 0f;
            while (_generator.IsGenerating && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsNotNull(_generator.ChunkManager, "ChunkManager should exist after generation");

            // Act: Clear terrain
            _generator.ClearTerrain();

            // Assert: Resources cleaned up
            Assert.IsNull(_generator.ChunkManager, "ChunkManager should be null after clear");
            Assert.IsFalse(_generator.IsGenerating, "Should not be generating after clear");

            Debug.Log("SUCCESS: Terrain cleared and resources cleaned up");
        }

        [UnityTest]
        public IEnumerator Test_ClearTerrain_CanRegenerateAfterClear()
        {
            // Arrange: Generate, clear, then regenerate
            _generator.GenerateTerrain();

            float timeout = 5f;
            float elapsed = 0f;
            while (_generator.IsGenerating && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            _generator.ClearTerrain();

            // Act: Regenerate
            _generator.GenerateTerrain();

            elapsed = 0f;
            while (_generator.IsGenerating && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert: Should successfully regenerate
            Assert.IsNotNull(_generator.ChunkManager, "ChunkManager should exist after regeneration");

            int loadedChunks = 0;
            foreach (var chunk in _generator.ChunkManager.GetAllChunks())
            {
                loadedChunks++;
            }

            Assert.AreEqual(_minecraftConfig.TotalChunks, loadedChunks,
                $"Should regenerate {_minecraftConfig.TotalChunks} chunks");

            Debug.Log("SUCCESS: Terrain can be regenerated after clear");
        }

        // ========== Test: Error Handling ==========

        [UnityTest]
        public IEnumerator Test_GenerateTerrain_IgnoresDuplicateCalls()
        {
            // Arrange
            _generator.GenerateTerrain();

            yield return null; // Wait one frame

            // Act: Try to generate again while first generation is in progress
            _generator.GenerateTerrain(); // Should log warning and ignore

            // Wait for generation to complete
            float timeout = 5f;
            float elapsed = 0f;
            while (_generator.IsGenerating && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            // Assert: Should still complete successfully (second call ignored)
            Assert.IsNotNull(_generator.ChunkManager, "ChunkManager should exist");

            Debug.Log("SUCCESS: Duplicate generation calls are ignored");
        }
    }
}
