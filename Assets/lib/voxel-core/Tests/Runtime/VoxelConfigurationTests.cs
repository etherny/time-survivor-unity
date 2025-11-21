using NUnit.Framework;
using UnityEngine;

namespace TimeSurvivor.Voxel.Core.Tests
{
    /// <summary>
    /// Unit tests for VoxelConfiguration ScriptableObject.
    /// Tests computed properties and configuration validation.
    /// </summary>
    [TestFixture]
    public class VoxelConfigurationTests
    {
        private const float EPSILON = 0.0001f;

        #region Computed Properties Tests

        [Test]
        public void Test_ChunkVolume_ReturnsCorrectVolume()
        {
            // Arrange
            var config = VoxelTestUtilities.CreateTestConfig(chunkSize: 16);

            // Act
            int volume = config.ChunkVolume;

            // Assert
            Assert.That(volume, Is.EqualTo(4096),
                "ChunkVolume for 16x16x16 should be 4096 (16^3)");
        }

        [Test]
        public void Test_ChunkVolume_DifferentSizes_ReturnsCorrectVolumes()
        {
            // Arrange
            var config8 = VoxelTestUtilities.CreateTestConfig(chunkSize: 8);
            var config16 = VoxelTestUtilities.CreateTestConfig(chunkSize: 16);
            var config32 = VoxelTestUtilities.CreateTestConfig(chunkSize: 32);

            // Act
            int volume8 = config8.ChunkVolume;
            int volume16 = config16.ChunkVolume;
            int volume32 = config32.ChunkVolume;

            // Assert
            Assert.That(volume8, Is.EqualTo(512), "8^3 should be 512");
            Assert.That(volume16, Is.EqualTo(4096), "16^3 should be 4096");
            Assert.That(volume32, Is.EqualTo(32768), "32^3 should be 32768");
        }

        [Test]
        public void Test_MacroChunkWorldSize_ReturnsCorrectSize()
        {
            // Arrange
            var config = VoxelTestUtilities.CreateTestConfig(
                chunkSize: 16,
                macroVoxelSize: 0.2f
            );

            // Act
            float worldSize = config.MacroChunkWorldSize;

            // Assert
            float expected = 16 * 0.2f; // 3.2
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(worldSize, expected, EPSILON), Is.True,
                "MacroChunkWorldSize should be ChunkSize * MacroVoxelSize = 3.2");
        }

        [Test]
        public void Test_MicroChunkWorldSize_ReturnsCorrectSize()
        {
            // Arrange
            var config = VoxelTestUtilities.CreateTestConfig(
                chunkSize: 16,
                microVoxelSize: 0.1f
            );

            // Act
            float worldSize = config.MicroChunkWorldSize;

            // Assert
            float expected = 16 * 0.1f; // 1.6
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(worldSize, expected, EPSILON), Is.True,
                "MicroChunkWorldSize should be ChunkSize * MicroVoxelSize = 1.6");
        }

        [Test]
        public void Test_ChunkWorldSizes_MacroLargerThanMicro()
        {
            // Arrange
            var config = VoxelTestUtilities.CreateTestConfig(
                chunkSize: 16,
                macroVoxelSize: 0.2f,
                microVoxelSize: 0.1f
            );

            // Act
            float macroSize = config.MacroChunkWorldSize;
            float microSize = config.MicroChunkWorldSize;

            // Assert
            Assert.That(macroSize, Is.GreaterThan(microSize),
                "Macro chunk world size should be larger than micro chunk world size");
        }

        #endregion

        #region Default Values Tests

        [Test]
        public void Test_CreateTestConfig_DefaultValues_AreSet()
        {
            // Arrange & Act
            var config = VoxelTestUtilities.CreateTestConfig();

            // Assert
            Assert.That(config.ChunkSize, Is.EqualTo(16), "Default chunk size should be 16");
            Assert.That(config.MacroVoxelSize, Is.EqualTo(0.2f), "Default macro voxel size should be 0.2");
            Assert.That(config.MicroVoxelSize, Is.EqualTo(0.1f), "Default micro voxel size should be 0.1");
            Assert.That(config.UseBurstCompilation, Is.True, "Burst should be enabled by default");
            Assert.That(config.UseJobSystem, Is.True, "Job system should be enabled by default");
        }

        [Test]
        public void Test_CreateTestConfig_CustomValues_ArePreserved()
        {
            // Arrange
            int customChunkSize = 32;
            float customMacroSize = 0.5f;
            float customMicroSize = 0.25f;

            // Act
            var config = VoxelTestUtilities.CreateTestConfig(
                customChunkSize,
                customMacroSize,
                customMicroSize
            );

            // Assert
            Assert.That(config.ChunkSize, Is.EqualTo(customChunkSize),
                "Custom chunk size should be preserved");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(config.MacroVoxelSize, customMacroSize, EPSILON),
                Is.True, "Custom macro voxel size should be preserved");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(config.MicroVoxelSize, customMicroSize, EPSILON),
                Is.True, "Custom micro voxel size should be preserved");
        }

        #endregion

        #region Configuration Validation Tests

        [Test]
        public void Test_VoxelConfiguration_IsScriptableObject()
        {
            // Arrange & Act
            var config = ScriptableObject.CreateInstance<VoxelConfiguration>();

            // Assert
            Assert.That(config, Is.Not.Null, "Should be able to create VoxelConfiguration instance");
            Assert.That(config, Is.InstanceOf<ScriptableObject>(),
                "VoxelConfiguration should be a ScriptableObject");

            // Cleanup
            Object.DestroyImmediate(config);
        }

        [Test]
        public void Test_VoxelConfiguration_PowerOfTwoChunkSize_CalculatesCorrectly()
        {
            // Arrange - Test power-of-two sizes (recommended)
            int[] powerOfTwoSizes = { 8, 16, 32 };

            // Act & Assert
            foreach (int size in powerOfTwoSizes)
            {
                var config = VoxelTestUtilities.CreateTestConfig(chunkSize: size);
                int volume = config.ChunkVolume;
                Assert.That(volume, Is.EqualTo(size * size * size),
                    $"ChunkVolume should be {size}^3 for chunk size {size}");
            }
        }

        [Test]
        public void Test_VoxelConfiguration_DeterministicSeed_IsSet()
        {
            // Arrange & Act
            var config = VoxelTestUtilities.CreateTestConfig();

            // Assert
            Assert.That(config.Seed, Is.EqualTo(12345),
                "Test config should have deterministic seed for reproducible tests");
        }

        [Test]
        public void Test_VoxelConfiguration_RenderDistance_IsPositive()
        {
            // Arrange & Act
            var config = VoxelTestUtilities.CreateTestConfig();

            // Assert
            Assert.That(config.RenderDistance, Is.GreaterThan(0),
                "Render distance should be positive");
        }

        [Test]
        public void Test_VoxelConfiguration_MaxCachedChunks_IsPositive()
        {
            // Arrange & Act
            var config = VoxelTestUtilities.CreateTestConfig();

            // Assert
            Assert.That(config.MaxCachedChunks, Is.GreaterThan(0),
                "Max cached chunks should be positive");
        }

        #endregion

        #region Relationship Tests

        [Test]
        public void Test_VoxelSizes_MicroSmallerThanMacro()
        {
            // Arrange & Act
            var config = VoxelTestUtilities.CreateTestConfig(
                chunkSize: 16,
                macroVoxelSize: 0.2f,
                microVoxelSize: 0.1f
            );

            // Assert
            Assert.That(config.MicroVoxelSize, Is.LessThan(config.MacroVoxelSize),
                "Micro voxel size should be smaller than macro voxel size");
        }

        [Test]
        public void Test_ChunkWorldSize_ScalesWithVoxelSize()
        {
            // Arrange
            var config1 = VoxelTestUtilities.CreateTestConfig(
                chunkSize: 16,
                macroVoxelSize: 0.2f
            );
            var config2 = VoxelTestUtilities.CreateTestConfig(
                chunkSize: 16,
                macroVoxelSize: 0.4f
            );

            // Act
            float worldSize1 = config1.MacroChunkWorldSize;
            float worldSize2 = config2.MacroChunkWorldSize;

            // Assert
            Assert.That(worldSize2, Is.EqualTo(worldSize1 * 2.0f).Within(EPSILON),
                "Doubling voxel size should double chunk world size");
        }

        [Test]
        public void Test_ChunkWorldSize_ScalesWithChunkSize()
        {
            // Arrange
            var config1 = VoxelTestUtilities.CreateTestConfig(
                chunkSize: 8,
                macroVoxelSize: 0.2f
            );
            var config2 = VoxelTestUtilities.CreateTestConfig(
                chunkSize: 16,
                macroVoxelSize: 0.2f
            );

            // Act
            float worldSize1 = config1.MacroChunkWorldSize;
            float worldSize2 = config2.MacroChunkWorldSize;

            // Assert
            Assert.That(worldSize2, Is.EqualTo(worldSize1 * 2.0f).Within(EPSILON),
                "Doubling chunk size should double chunk world size");
        }

        #endregion
    }
}
