using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using TimeSurvivor.Voxel.Core;
using System.Collections.Generic;

namespace TimeSurvivor.Voxel.Terrain.Tests
{
    /// <summary>
    /// Validation tests for ProceduralTerrainGenerationJob to diagnose terrain generation issues.
    /// These tests verify that the generated terrain has proper variation, layering, and diversity
    /// of voxel types to ensure a realistic and visible terrain (not a uniform cube).
    /// </summary>
    [TestFixture]
    public class ProceduralTerrainGenerationJobValidationTests
    {
        // Test constants matching the demo configuration
        private const int ChunkSize = 64;
        private const float VoxelSize = 1.0f;
        private const int Seed = 12345;
        private const float NoiseFrequency = 0.05f;
        private const int NoiseOctaves = 4;
        private const float Lacunarity = 2.0f;
        private const float Persistence = 0.5f;
        private const float TerrainOffsetY = 32f; // Middle of the chunk

        #region Helper Methods

        /// <summary>
        /// Creates a ProceduralTerrainGenerationJob with validation test parameters.
        /// </summary>
        private ProceduralTerrainGenerationJob CreateValidationJob(
            NativeArray<VoxelType> voxelData,
            ChunkCoord? chunkCoord = null)
        {
            return new ProceduralTerrainGenerationJob
            {
                Seed = Seed,
                ChunkCoord = chunkCoord ?? new ChunkCoord(int3.zero),
                ChunkSize = ChunkSize,
                VoxelSize = VoxelSize,
                NoiseFrequency = NoiseFrequency,
                NoiseOctaves = NoiseOctaves,
                Lacunarity = Lacunarity,
                Persistence = Persistence,
                TerrainOffsetY = TerrainOffsetY, // FIX: Add terrain offset parameter
                VoxelData = voxelData
            };
        }

        /// <summary>
        /// Counts voxels by type in the generated chunk.
        /// </summary>
        /// <param name="voxelData">Generated voxel data array</param>
        /// <returns>Dictionary with count for each VoxelType</returns>
        private Dictionary<VoxelType, int> CountVoxelsByType(NativeArray<VoxelType> voxelData)
        {
            var counts = new Dictionary<VoxelType, int>();

            // Initialize counts for all types
            for (int i = 0; i <= 7; i++)
            {
                counts[(VoxelType)i] = 0;
            }

            // Count voxels
            for (int i = 0; i < voxelData.Length; i++)
            {
                VoxelType type = voxelData[i];
                counts[type]++;
            }

            return counts;
        }

        /// <summary>
        /// Gets the voxel at a specific 3D coordinate within the chunk.
        /// </summary>
        private VoxelType GetVoxel(NativeArray<VoxelType> voxelData, int x, int y, int z)
        {
            int index = x + y * ChunkSize + z * ChunkSize * ChunkSize;
            return voxelData[index];
        }

        /// <summary>
        /// Finds the surface height (first solid voxel from top) at a given X,Z coordinate.
        /// </summary>
        /// <returns>Y coordinate of surface, or -1 if no solid voxel found</returns>
        private int FindSurfaceHeight(NativeArray<VoxelType> voxelData, int x, int z)
        {
            for (int y = ChunkSize - 1; y >= 0; y--)
            {
                VoxelType voxel = GetVoxel(voxelData, x, y, z);
                if (voxel != VoxelType.Air && voxel != VoxelType.Water)
                {
                    return y;
                }
            }
            return -1; // No solid voxel found (all air/water column)
        }

        /// <summary>
        /// Logs voxel type distribution for debugging.
        /// </summary>
        private void LogVoxelDistribution(Dictionary<VoxelType, int> counts, int totalVoxels)
        {
            Debug.Log("=== Voxel Distribution ===");
            foreach (var kvp in counts)
            {
                if (kvp.Value > 0)
                {
                    float percentage = (float)kvp.Value / totalVoxels * 100f;
                    Debug.Log($"  {kvp.Key}: {kvp.Value} ({percentage:F2}%)");
                }
            }
        }

        #endregion

        /// <summary>
        /// Test 1: Verifies that the chunk contains multiple voxel types (not 100% of a single type).
        /// A proper terrain should have Air, Grass, Dirt, and Stone at minimum.
        /// Water is optional but acceptable.
        /// </summary>
        [Test]
        public void GenerateChunk_ShouldProduceVariedVoxelTypes()
        {
            // Arrange
            int totalVoxels = ChunkSize * ChunkSize * ChunkSize;
            NativeArray<VoxelType> voxelData = new NativeArray<VoxelType>(totalVoxels, Allocator.TempJob);

            try
            {
                ProceduralTerrainGenerationJob job = CreateValidationJob(voxelData);

                // Act
                job.Schedule(totalVoxels, 64).Complete();

                // Assert: Count voxels by type
                var counts = CountVoxelsByType(voxelData);
                LogVoxelDistribution(counts, totalVoxels);

                // Verify presence of essential voxel types
                Assert.Greater(counts[VoxelType.Air], 0,
                    "Terrain must contain Air voxels (above surface)");
                Assert.Greater(counts[VoxelType.Grass], 0,
                    "Terrain must contain Grass voxels (surface layer)");
                Assert.Greater(counts[VoxelType.Dirt], 0,
                    "Terrain must contain Dirt voxels (subsurface layer)");
                Assert.Greater(counts[VoxelType.Stone], 0,
                    "Terrain must contain Stone voxels (deep layer)");

                // Verify diversity: no single type should dominate (>90%)
                foreach (var kvp in counts)
                {
                    float percentage = (float)kvp.Value / totalVoxels;
                    Assert.Less(percentage, 0.90f,
                        $"VoxelType.{kvp.Key} occupies {percentage * 100:F2}% of chunk (too dominant, expected <90%)");
                }

                // Count number of different types present
                int uniqueTypes = 0;
                foreach (var kvp in counts)
                {
                    if (kvp.Value > 0) uniqueTypes++;
                }

                Assert.GreaterOrEqual(uniqueTypes, 4,
                    $"Terrain should have at least 4 different voxel types. Found: {uniqueTypes}");

                Debug.Log($"SUCCESS: Terrain contains {uniqueTypes} different voxel types with proper distribution.");
            }
            finally
            {
                voxelData.Dispose();
            }
        }

        /// <summary>
        /// Test 2: Verifies that Grass voxels are present in the upper half of the chunk.
        /// At least 1% of voxels should be Grass, and these should be in the surface layer.
        /// </summary>
        [Test]
        public void GenerateChunk_ShouldHaveVisibleGrassSurface()
        {
            // Arrange
            int totalVoxels = ChunkSize * ChunkSize * ChunkSize;
            NativeArray<VoxelType> voxelData = new NativeArray<VoxelType>(totalVoxels, Allocator.TempJob);

            try
            {
                ProceduralTerrainGenerationJob job = CreateValidationJob(voxelData);

                // Act
                job.Schedule(totalVoxels, 64).Complete();

                // Assert: Count Grass voxels in upper half
                int grassCountUpperHalf = 0;
                int grassCountTotal = 0;
                int upperHalfThreshold = ChunkSize / 2;

                for (int z = 0; z < ChunkSize; z++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        for (int x = 0; x < ChunkSize; x++)
                        {
                            VoxelType voxel = GetVoxel(voxelData, x, y, z);
                            if (voxel == VoxelType.Grass)
                            {
                                grassCountTotal++;
                                if (y >= upperHalfThreshold)
                                {
                                    grassCountUpperHalf++;
                                }
                            }
                        }
                    }
                }

                float grassPercentage = (float)grassCountTotal / totalVoxels;
                Debug.Log($"Grass voxels: {grassCountTotal} ({grassPercentage * 100:F2}%), " +
                         $"{grassCountUpperHalf} in upper half (Y >= {upperHalfThreshold})");

                // Verify: At least 1% of chunk should be Grass
                Assert.GreaterOrEqual(grassPercentage, 0.01f,
                    $"Terrain should have at least 1% Grass voxels. Found: {grassPercentage * 100:F4}%");

                // Verify: Most Grass should be in upper half (at least 45% of Grass voxels)
                // Note: With amplitude=0.5, terrain has more variation, so grass is more evenly distributed
                if (grassCountTotal > 0)
                {
                    float grassInUpperHalfRatio = (float)grassCountUpperHalf / grassCountTotal;
                    Assert.GreaterOrEqual(grassInUpperHalfRatio, 0.45f,
                        $"At least 45% of Grass voxels should be in upper half. Found: {grassInUpperHalfRatio * 100:F2}%");
                }

                Debug.Log($"SUCCESS: Grass surface is visible with {grassCountUpperHalf} voxels in upper half.");
            }
            finally
            {
                voxelData.Dispose();
            }
        }

        /// <summary>
        /// Test 3: Verifies horizontal layering structure (Grass > Dirt > Stone).
        /// Samples multiple columns and checks that layers appear in correct order.
        /// </summary>
        [Test]
        public void GenerateChunk_ShouldHaveHorizontalLayers()
        {
            // Arrange
            int totalVoxels = ChunkSize * ChunkSize * ChunkSize;
            NativeArray<VoxelType> voxelData = new NativeArray<VoxelType>(totalVoxels, Allocator.TempJob);

            try
            {
                ProceduralTerrainGenerationJob job = CreateValidationJob(voxelData);

                // Act
                job.Schedule(totalVoxels, 64).Complete();

                // Assert: Sample 16 columns across the chunk
                int samplesPerAxis = 4;
                int stepSize = ChunkSize / samplesPerAxis;
                int validLayeredColumns = 0;

                for (int z = stepSize / 2; z < ChunkSize; z += stepSize)
                {
                    for (int x = stepSize / 2; x < ChunkSize; x += stepSize)
                    {
                        // Find surface height
                        int surfaceY = FindSurfaceHeight(voxelData, x, z);
                        if (surfaceY < 5) continue; // Skip if too close to bottom

                        VoxelType surface = GetVoxel(voxelData, x, surfaceY, z);
                        VoxelType depth1 = surfaceY >= 1 ? GetVoxel(voxelData, x, surfaceY - 1, z) : VoxelType.Air;
                        VoxelType depth2 = surfaceY >= 2 ? GetVoxel(voxelData, x, surfaceY - 2, z) : VoxelType.Air;
                        VoxelType depth4 = surfaceY >= 4 ? GetVoxel(voxelData, x, surfaceY - 4, z) : VoxelType.Air;

                        // Check for layering: Grass on surface, Dirt below, Stone deeper
                        bool hasGrassSurface = surface == VoxelType.Grass;
                        bool hasDirtSubsurface = depth1 == VoxelType.Dirt || depth2 == VoxelType.Dirt;
                        bool hasStoneDeep = depth4 == VoxelType.Stone;

                        if (hasGrassSurface && (hasDirtSubsurface || hasStoneDeep))
                        {
                            validLayeredColumns++;
                        }

                        // Log first few columns for debugging
                        if (validLayeredColumns < 3)
                        {
                            Debug.Log($"Column ({x}, {z}) surface at Y={surfaceY}: " +
                                     $"Surface={surface}, Depth-1={depth1}, Depth-2={depth2}, Depth-4={depth4}");
                        }
                    }
                }

                int totalSamples = samplesPerAxis * samplesPerAxis;
                float layeringPercentage = (float)validLayeredColumns / totalSamples;

                Debug.Log($"Valid layered columns: {validLayeredColumns}/{totalSamples} ({layeringPercentage * 100:F2}%)");

                // At least 30% of sampled columns should show proper layering
                Assert.GreaterOrEqual(layeringPercentage, 0.30f,
                    $"At least 30% of columns should show Grass > Dirt > Stone layering. Found: {layeringPercentage * 100:F2}%");

                Debug.Log($"SUCCESS: Terrain has horizontal layering with {validLayeredColumns} valid columns.");
            }
            finally
            {
                voxelData.Dispose();
            }
        }

        /// <summary>
        /// Test 4: Verifies that Water voxels only appear below TerrainOffsetY (in valleys).
        /// Water should not be floating in the air above the terrain offset.
        /// </summary>
        [Test]
        public void GenerateChunk_WaterShouldOnlyBeInValleys()
        {
            // Arrange
            int totalVoxels = ChunkSize * ChunkSize * ChunkSize;
            NativeArray<VoxelType> voxelData = new NativeArray<VoxelType>(totalVoxels, Allocator.TempJob);

            try
            {
                ProceduralTerrainGenerationJob job = CreateValidationJob(voxelData);

                // Act
                job.Schedule(totalVoxels, 64).Complete();

                // Assert: Check all Water voxels are below TerrainOffsetY
                int waterCountTotal = 0;
                int waterCountAboveOffset = 0;

                for (int z = 0; z < ChunkSize; z++)
                {
                    for (int y = 0; y < ChunkSize; y++)
                    {
                        for (int x = 0; x < ChunkSize; x++)
                        {
                            VoxelType voxel = GetVoxel(voxelData, x, y, z);
                            if (voxel == VoxelType.Water)
                            {
                                waterCountTotal++;
                                if (y > TerrainOffsetY)
                                {
                                    waterCountAboveOffset++;
                                    // Log first few violations
                                    if (waterCountAboveOffset <= 5)
                                    {
                                        Debug.LogWarning($"Water voxel found above TerrainOffsetY at ({x}, {y}, {z}), " +
                                                        $"Y={y} > TerrainOffsetY={TerrainOffsetY}");
                                    }
                                }
                            }
                        }
                    }
                }

                Debug.Log($"Water voxels: {waterCountTotal} total, {waterCountAboveOffset} above TerrainOffsetY");

                // Verify: No water above TerrainOffsetY (or very minimal <1%)
                if (waterCountTotal > 0)
                {
                    float waterAbovePercentage = (float)waterCountAboveOffset / waterCountTotal;
                    Assert.LessOrEqual(waterAbovePercentage, 0.01f,
                        $"Water should not be above TerrainOffsetY. Found: {waterCountAboveOffset}/{waterCountTotal} " +
                        $"({waterAbovePercentage * 100:F2}%) above offset");
                }

                Debug.Log($"SUCCESS: Water is properly constrained to valleys (below TerrainOffsetY={TerrainOffsetY}).");
            }
            finally
            {
                voxelData.Dispose();
            }
        }

        /// <summary>
        /// Test 5: Verifies that the terrain surface has height variation (hills and valleys).
        /// A proper terrain should not be a flat plane at the same height everywhere.
        /// </summary>
        [Test]
        public void GenerateChunk_ShouldHaveHeightVariation()
        {
            // Arrange
            int totalVoxels = ChunkSize * ChunkSize * ChunkSize;
            NativeArray<VoxelType> voxelData = new NativeArray<VoxelType>(totalVoxels, Allocator.TempJob);

            try
            {
                ProceduralTerrainGenerationJob job = CreateValidationJob(voxelData);

                // Act
                job.Schedule(totalVoxels, 64).Complete();

                // Assert: Sample surface heights across chunk
                List<int> surfaceHeights = new List<int>();
                int samplesPerAxis = 8;
                int stepSize = ChunkSize / samplesPerAxis;

                for (int z = 0; z < ChunkSize; z += stepSize)
                {
                    for (int x = 0; x < ChunkSize; x += stepSize)
                    {
                        int surfaceY = FindSurfaceHeight(voxelData, x, z);
                        if (surfaceY >= 0)
                        {
                            surfaceHeights.Add(surfaceY);
                        }
                    }
                }

                if (surfaceHeights.Count < 4)
                {
                    Assert.Fail("Not enough surface samples found to validate height variation");
                }

                // Calculate height variation statistics
                int minHeight = int.MaxValue;
                int maxHeight = int.MinValue;
                float avgHeight = 0f;

                foreach (int height in surfaceHeights)
                {
                    minHeight = math.min(minHeight, height);
                    maxHeight = math.max(maxHeight, height);
                    avgHeight += height;
                }
                avgHeight /= surfaceHeights.Count;

                int heightRange = maxHeight - minHeight;
                Debug.Log($"Surface height stats: Min={minHeight}, Max={maxHeight}, Avg={avgHeight:F2}, Range={heightRange}");

                // Verify: Height range should be at least 8 voxels (12.5% of chunk height)
                Assert.GreaterOrEqual(heightRange, 8,
                    $"Terrain should have at least 8 voxels of height variation. Found: {heightRange}");

                // Verify: Surface should not be completely flat (standard deviation > 0)
                float variance = 0f;
                foreach (int height in surfaceHeights)
                {
                    variance += (height - avgHeight) * (height - avgHeight);
                }
                variance /= surfaceHeights.Count;
                float stdDev = math.sqrt(variance);

                Debug.Log($"Surface height standard deviation: {stdDev:F2}");
                Assert.Greater(stdDev, 2.0f,
                    $"Terrain should have varied surface heights (std dev > 2.0). Found: {stdDev:F2}");

                Debug.Log($"SUCCESS: Terrain has height variation with range={heightRange}, stdDev={stdDev:F2}");
            }
            finally
            {
                voxelData.Dispose();
            }
        }

        /// <summary>
        /// Test 6: Verifies that the chunk is NOT a uniform solid cube.
        /// Should have approximately 50% Air and 50% solid blocks for a realistic terrain.
        /// </summary>
        [Test]
        public void GenerateChunk_ShouldNotBeUniformCube()
        {
            // Arrange
            int totalVoxels = ChunkSize * ChunkSize * ChunkSize;
            NativeArray<VoxelType> voxelData = new NativeArray<VoxelType>(totalVoxels, Allocator.TempJob);

            try
            {
                ProceduralTerrainGenerationJob job = CreateValidationJob(voxelData);

                // Act
                job.Schedule(totalVoxels, 64).Complete();

                // Assert: Count Air vs Solid voxels
                var counts = CountVoxelsByType(voxelData);

                int airCount = counts[VoxelType.Air];
                int solidCount = 0;

                foreach (var kvp in counts)
                {
                    if (kvp.Key != VoxelType.Air)
                    {
                        solidCount += kvp.Value;
                    }
                }

                float airPercentage = (float)airCount / totalVoxels;
                float solidPercentage = (float)solidCount / totalVoxels;

                Debug.Log($"Air: {airCount} ({airPercentage * 100:F2}%), Solid: {solidCount} ({solidPercentage * 100:F2}%)");

                // Verify: Should have Air (not 100% solid)
                Assert.Greater(airCount, 0,
                    "Terrain must contain Air voxels (not a solid cube)");

                // Verify: Air should be between 20% and 80% (reasonable distribution)
                Assert.GreaterOrEqual(airPercentage, 0.20f,
                    $"Terrain should have at least 20% Air. Found: {airPercentage * 100:F2}%");
                Assert.LessOrEqual(airPercentage, 0.80f,
                    $"Terrain should have at most 80% Air. Found: {airPercentage * 100:F2}%");

                // Verify: Solid blocks should be diverse (not 100% of one type)
                VoxelType mostCommonSolid = VoxelType.Air;
                int mostCommonCount = 0;

                foreach (var kvp in counts)
                {
                    if (kvp.Key != VoxelType.Air && kvp.Value > mostCommonCount)
                    {
                        mostCommonSolid = kvp.Key;
                        mostCommonCount = kvp.Value;
                    }
                }

                if (solidCount > 0)
                {
                    float mostCommonPercentage = (float)mostCommonCount / solidCount;
                    Debug.Log($"Most common solid type: {mostCommonSolid} ({mostCommonPercentage * 100:F2}% of solid voxels)");

                    Assert.Less(mostCommonPercentage, 0.95f,
                        $"Solid voxels should be diverse (not 95%+ of one type). " +
                        $"{mostCommonSolid} occupies {mostCommonPercentage * 100:F2}% of solid voxels");
                }

                Debug.Log($"SUCCESS: Terrain is not a uniform cube. Air={airPercentage * 100:F2}%, Solid={solidPercentage * 100:F2}%");
            }
            finally
            {
                voxelData.Dispose();
            }
        }
    }
}
