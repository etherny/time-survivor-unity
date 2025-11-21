using NUnit.Framework;
using Unity.Collections;
using UnityEngine;

namespace TimeSurvivor.Voxel.Terrain.Tests
{
    /// <summary>
    /// Unit tests for MinecraftHeightmapGenerator.
    /// Tests determinism, correctness, performance, and memory cleanup.
    /// </summary>
    [TestFixture]
    public class MinecraftHeightmapGeneratorTests
    {
        private MinecraftHeightmapGenerator _generator;

        [TearDown]
        public void TearDown()
        {
            // Ensure cleanup after each test
            _generator?.Dispose();
            _generator = null;
        }

        // ========== Test: Determinism ==========

        [Test]
        public void Test_GenerateHeightmap_IsDeterministic()
        {
            // ARRANGE: Create two generators with same parameters
            const int seed = 12345;
            var generator1 = new MinecraftHeightmapGenerator(
                worldSizeX: 5, worldSizeZ: 5, chunkSize: 16,
                baseTerrainHeight: 64, terrainVariation: 32,
                heightmapFrequency: 0.02f, heightmapOctaves: 3, seed: seed);

            var generator2 = new MinecraftHeightmapGenerator(
                worldSizeX: 5, worldSizeZ: 5, chunkSize: 16,
                baseTerrainHeight: 64, terrainVariation: 32,
                heightmapFrequency: 0.02f, heightmapOctaves: 3, seed: seed);

            // ACT: Generate both heightmaps
            generator1.GenerateHeightmap();
            generator2.GenerateHeightmap();

            // ASSERT: Heightmaps should be identical
            Assert.AreEqual(generator1.HeightmapSize, generator2.HeightmapSize, "Heightmap sizes should match");

            var heightmap1 = generator1.Heightmap;
            var heightmap2 = generator2.Heightmap;

            for (int i = 0; i < heightmap1.Length; i++)
            {
                Assert.AreEqual(heightmap1[i], heightmap2[i], 0.0001f,
                    $"Height values at index {i} should match for same seed");
            }

            // CLEANUP
            generator1.Dispose();
            generator2.Dispose();
        }

        // ========== Test: GetHeightAt Correctness ==========

        [Test]
        public void Test_GetHeightAt_ReturnsCorrectHeight()
        {
            // ARRANGE: Small heightmap for easy verification
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 2, worldSizeZ: 2, chunkSize: 8,
                baseTerrainHeight: 50, terrainVariation: 20,
                heightmapFrequency: 0.05f, heightmapOctaves: 2, seed: 999);

            _generator.GenerateHeightmap();

            // ACT & ASSERT: Sample multiple positions
            int width = _generator.HeightmapWidth; // 2×8 = 16
            int height = _generator.HeightmapHeight; // 2×8 = 16

            // Test center
            float centerHeight = _generator.GetHeightAt(width / 2, height / 2);
            Assert.IsTrue(centerHeight >= 30 && centerHeight <= 70,
                $"Height {centerHeight} should be within [baseHeight±variation] = [30, 70]");

            // Test corners
            float corner1 = _generator.GetHeightAt(0, 0);
            float corner2 = _generator.GetHeightAt(width - 1, 0);
            float corner3 = _generator.GetHeightAt(0, height - 1);
            float corner4 = _generator.GetHeightAt(width - 1, height - 1);

            Assert.IsTrue(corner1 >= 30 && corner1 <= 70, "Corner1 height out of range");
            Assert.IsTrue(corner2 >= 30 && corner2 <= 70, "Corner2 height out of range");
            Assert.IsTrue(corner3 >= 30 && corner3 <= 70, "Corner3 height out of range");
            Assert.IsTrue(corner4 >= 30 && corner4 <= 70, "Corner4 height out of range");
        }

        [Test]
        public void Test_GetHeightAt_ClampsOutOfBoundsCoordinates()
        {
            // ARRANGE
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 2, worldSizeZ: 2, chunkSize: 8,
                baseTerrainHeight: 50, terrainVariation: 20,
                heightmapFrequency: 0.05f, heightmapOctaves: 2, seed: 123);

            _generator.GenerateHeightmap();

            int width = _generator.HeightmapWidth;
            int height = _generator.HeightmapHeight;

            // ACT: Query out-of-bounds coordinates (should be clamped, not throw)
            float height1 = _generator.GetHeightAt(-10, 5);        // X negative
            float height2 = _generator.GetHeightAt(5, -10);        // Z negative
            float height3 = _generator.GetHeightAt(width + 100, 5); // X too large
            float height4 = _generator.GetHeightAt(5, height + 100); // Z too large

            // ASSERT: Should return valid heights (clamped to bounds)
            Assert.IsTrue(height1 >= 30 && height1 <= 70, "Negative X should be clamped");
            Assert.IsTrue(height2 >= 30 && height2 <= 70, "Negative Z should be clamped");
            Assert.IsTrue(height3 >= 30 && height3 <= 70, "Large X should be clamped");
            Assert.IsTrue(height4 >= 30 && height4 <= 70, "Large Z should be clamped");
        }

        // ========== Test: Performance ==========

        [Test]
        public void Test_GenerateHeightmap_PerformanceUnder100ms()
        {
            // ARRANGE: Medium-sized world (20×20 chunks = 1280×1280 heightmap = 1.6M elements)
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 20, worldSizeZ: 20, chunkSize: 64,
                baseTerrainHeight: 192, terrainVariation: 128,
                heightmapFrequency: 0.01f, heightmapOctaves: 4, seed: 7777);

            // ACT: Time generation
            var startTime = System.Diagnostics.Stopwatch.StartNew();
            _generator.GenerateHeightmap();
            startTime.Stop();

            // ASSERT: Should generate in under 100ms (target: ~50ms)
            long elapsedMs = startTime.ElapsedMilliseconds;
            Debug.Log($"Heightmap generation (20×20 chunks, 1.6M elements): {elapsedMs}ms");
            Assert.Less(elapsedMs, 100, "Heightmap generation should complete in under 100ms");
        }

        // ========== Test: Memory Cleanup ==========

        [Test]
        public void Test_Dispose_FreesMemory()
        {
            // ARRANGE
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 5, worldSizeZ: 5, chunkSize: 16,
                baseTerrainHeight: 64, terrainVariation: 32,
                heightmapFrequency: 0.02f, heightmapOctaves: 3, seed: 555);

            _generator.GenerateHeightmap();
            var heightmap = _generator.Heightmap;

            // ACT: Dispose
            _generator.Dispose();

            // ASSERT: Heightmap should no longer be created
            Assert.IsFalse(heightmap.IsCreated, "Heightmap NativeArray should be disposed");
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                var h = _generator.Heightmap; // Should throw
            }, "Accessing Heightmap after Dispose should throw ObjectDisposedException");
        }

        [Test]
        public void Test_Dispose_CanBeCalledMultipleTimes()
        {
            // ARRANGE
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 2, worldSizeZ: 2, chunkSize: 8,
                baseTerrainHeight: 50, terrainVariation: 20,
                heightmapFrequency: 0.05f, heightmapOctaves: 2, seed: 999);

            _generator.GenerateHeightmap();

            // ACT: Dispose multiple times (should not throw)
            _generator.Dispose();
            _generator.Dispose();
            _generator.Dispose();

            // ASSERT: No exceptions thrown
            Assert.Pass("Dispose() can be called multiple times safely");
        }

        // ========== Test: Error Handling ==========

        [Test]
        public void Test_GetHeightAt_ThrowsIfNotGenerated()
        {
            // ARRANGE: Generator without calling GenerateHeightmap()
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 2, worldSizeZ: 2, chunkSize: 8,
                baseTerrainHeight: 50, terrainVariation: 20,
                heightmapFrequency: 0.05f, heightmapOctaves: 2, seed: 123);

            // ACT & ASSERT: Should throw InvalidOperationException
            Assert.Throws<System.InvalidOperationException>(() =>
            {
                _generator.GetHeightAt(5, 5);
            }, "GetHeightAt() before GenerateHeightmap() should throw");
        }

        [Test]
        public void Test_Heightmap_ThrowsIfDisposed()
        {
            // ARRANGE
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 2, worldSizeZ: 2, chunkSize: 8,
                baseTerrainHeight: 50, terrainVariation: 20,
                heightmapFrequency: 0.05f, heightmapOctaves: 2, seed: 123);

            _generator.GenerateHeightmap();
            _generator.Dispose();

            // ACT & ASSERT: Should throw ObjectDisposedException
            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                var h = _generator.Heightmap;
            }, "Accessing Heightmap after Dispose should throw");

            Assert.Throws<System.ObjectDisposedException>(() =>
            {
                _generator.GetHeightAt(5, 5);
            }, "GetHeightAt() after Dispose should throw");
        }

        // ========== Test: Dimensions ==========

        [Test]
        public void Test_HeightmapDimensions_AreCorrect()
        {
            // ARRANGE
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 10, worldSizeZ: 8, chunkSize: 64,
                baseTerrainHeight: 192, terrainVariation: 128,
                heightmapFrequency: 0.01f, heightmapOctaves: 4, seed: 123);

            // ACT
            _generator.GenerateHeightmap();

            // ASSERT
            int expectedWidth = 10 * 64; // 640
            int expectedHeight = 8 * 64; // 512
            int expectedSize = expectedWidth * expectedHeight; // 327,680

            Assert.AreEqual(expectedWidth, _generator.HeightmapWidth, "Width should match WorldSizeX × ChunkSize");
            Assert.AreEqual(expectedHeight, _generator.HeightmapHeight, "Height should match WorldSizeZ × ChunkSize");
            Assert.AreEqual(expectedSize, _generator.HeightmapSize, "Size should match Width × Height");
            Assert.AreEqual(expectedSize, _generator.Heightmap.Length, "NativeArray length should match size");
        }

        // ========== Test: Regeneration ==========

        [Test]
        public void Test_GenerateHeightmap_CanBeCalledMultipleTimes()
        {
            // ARRANGE
            _generator = new MinecraftHeightmapGenerator(
                worldSizeX: 3, worldSizeZ: 3, chunkSize: 16,
                baseTerrainHeight: 64, terrainVariation: 32,
                heightmapFrequency: 0.02f, heightmapOctaves: 3, seed: 456);

            // ACT: Generate twice
            _generator.GenerateHeightmap();
            var firstHeightmap = new float[_generator.HeightmapSize];
            _generator.Heightmap.CopyTo(firstHeightmap);

            _generator.GenerateHeightmap();
            var secondHeightmap = new float[_generator.HeightmapSize];
            _generator.Heightmap.CopyTo(secondHeightmap);

            // ASSERT: Both heightmaps should be identical (deterministic)
            for (int i = 0; i < firstHeightmap.Length; i++)
            {
                Assert.AreEqual(firstHeightmap[i], secondHeightmap[i], 0.0001f,
                    $"Regenerated heightmap should match original at index {i}");
            }
        }
    }
}
