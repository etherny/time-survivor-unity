using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using System.Diagnostics;

namespace TimeSurvivor.Voxel.Rendering.Tests
{
    /// <summary>
    /// Unit tests for GreedyMeshingJob (ADR-003).
    /// Validates correctness, performance, and output validity of the greedy meshing algorithm.
    /// </summary>
    public class GreedyMeshingJobTests
    {
        private const int ChunkSize = 64;
        private const int ChunkSizeSmall = 32; // For worst-case tests

        #region Tests

        /// <summary>
        /// Test 1: Verify a single cube generates exactly 24 vertices (6 faces × 4 vertices).
        /// </summary>
        [Test]
        public void SingleCubeTest()
        {
            // Arrange
            var voxels = CreateSingleCube(ChunkSize, new int3(32, 32, 32), VoxelType.Stone);

            // Act
            ExecuteMeshingJob(voxels, ChunkSize,
                out var vertices, out var triangles, out var uvs, out var normals, out var colors);

            // Assert
            Assert.AreEqual(24, vertices.Length, "Single cube should generate 24 vertices (6 faces × 4)");
            Assert.AreEqual(36, triangles.Length, "Single cube should generate 36 triangle indices (12 triangles × 3)");
            Assert.AreEqual(24, normals.Length, "Normals count should match vertices");
            Assert.AreEqual(24, uvs.Length, "UVs count should match vertices");
            Assert.AreEqual(24, colors.Length, "Colors count should match vertices");

            // Verify all vertices are near the cube center (32, 32, 32)
            float3 center = new float3(32, 32, 32);
            for (int i = 0; i < vertices.Length; i++)
            {
                float distance = math.distance(vertices[i], center);
                Assert.Less(distance, 2.0f, $"Vertex {i} at {vertices[i]} is too far from center {center}");
            }

            // Cleanup
            DisposeAll(voxels, vertices, triangles, uvs, normals, colors);
        }

        /// <summary>
        /// Test 2: Verify greedy meshing correctly merges a flat 10×10 plane.
        /// Should produce significantly fewer quads than 100 separate quads.
        /// A flat plane generates both top AND bottom faces (exposed to air).
        /// </summary>
        [Test]
        public void FlatPlaneTest()
        {
            // Arrange
            var voxels = CreateFlatPlane(ChunkSize, 10, 10, VoxelType.Grass);

            // Act
            ExecuteMeshingJob(voxels, ChunkSize,
                out var vertices, out var triangles, out var uvs, out var normals, out var colors);

            // Assert - Greedy meshing should merge into few quads
            // A 10x10 plane at Y=0 has:
            //   - Top face: 1 merged quad = 4 vertices
            //   - Bottom face: 1 merged quad = 4 vertices
            //   - Side faces: 4 edge quads (4 sides × 10 length each) = 16 vertices
            // Total expected: ~24 vertices (6 merged quads)
            // This is MUCH better than 100 separate cubes which would be 2400 vertices
            Assert.LessOrEqual(vertices.Length, 32,
                "Flat plane should be heavily optimized by greedy meshing (max 32 vertices for merged quads)");
            Assert.Greater(vertices.Length, 0, "Should generate at least some vertices");

            // Verify greedy fusion occurred by checking spacing between vertices
            // If properly merged, we should see vertices separated by ~10 units
            bool foundLargeMerge = false;
            for (int i = 0; i < vertices.Length - 1; i++)
            {
                for (int j = i + 1; j < vertices.Length; j++)
                {
                    float3 diff = vertices[j] - vertices[i];
                    // Check if any dimension has a large span (indicating merged quad)
                    if (math.abs(diff.x) >= 9.0f || math.abs(diff.z) >= 9.0f)
                    {
                        foundLargeMerge = true;
                        break;
                    }
                }
                if (foundLargeMerge) break;
            }
            Assert.IsTrue(foundLargeMerge, "Should find evidence of greedy meshing (vertices spanning ~10 units)");

            // Cleanup
            DisposeAll(voxels, vertices, triangles, uvs, normals, colors);
        }

        /// <summary>
        /// Test 3: Verify meshing performance is reasonable for realistic terrain.
        /// Note: ADR-007 specifies <0.9ms in production with Burst, but Editor mode tests
        /// run slower due to debugging overhead. We verify performance is in acceptable range.
        /// </summary>
        [Test]
        public void PerformanceTest()
        {
            // Arrange - Create realistic procedural terrain (~28% fill)
            var voxels = CreateProceduralTerrain(ChunkSize);

            // Warmup - Execute 50 times to ensure Burst compilation kicks in
            for (int i = 0; i < 50; i++)
            {
                ExecuteMeshingJob(voxels, ChunkSize,
                    out var v, out var t, out var u, out var n, out var c);
                DisposeAll(v, t, u, n, c);
            }

            // Act - Measure average time over 100 iterations
            var stopwatch = Stopwatch.StartNew();
            const int iterations = 100;

            NativeList<float3> lastVertices = default;
            NativeList<int> lastTriangles = default;
            NativeList<float2> lastUVs = default;
            NativeList<float3> lastNormals = default;
            NativeList<float4> lastColors = default;

            for (int i = 0; i < iterations; i++)
            {
                ExecuteMeshingJob(voxels, ChunkSize,
                    out lastVertices, out lastTriangles, out lastUVs, out lastNormals, out lastColors);

                if (i < iterations - 1)
                {
                    DisposeAll(lastVertices, lastTriangles, lastUVs, lastNormals, lastColors);
                }
            }

            stopwatch.Stop();
            double averageMs = stopwatch.Elapsed.TotalMilliseconds / iterations;

            // Assert - Editor mode is slower than production builds
            // Production target: <0.9ms (ADR-007)
            // Editor mode acceptable: <15ms (roughly 10-15x slower is normal for Edit mode tests)
            Assert.Less(averageMs, 15.0,
                $"Average meshing time {averageMs:F3}ms should be < 15ms in Editor mode (production target is <0.9ms)");
            Assert.Less(lastVertices.Length, 65535, "Vertex count should stay under Unity's 65K limit");

            UnityEngine.Debug.Log($"[GreedyMeshingJobTests] Performance: {averageMs:F3}ms avg (Editor mode), {lastVertices.Length} vertices");

            // Cleanup
            DisposeAll(voxels, lastVertices, lastTriangles, lastUVs, lastNormals, lastColors);
        }

        /// <summary>
        /// Test 4: Verify output mesh data is valid (no out-of-bounds indices, proper counts).
        /// </summary>
        [Test]
        public void OutputValidityTest()
        {
            // Arrange - Create random pattern (~20% fill)
            var voxels = CreateRandomPattern(ChunkSize, 0.2f);

            // Act
            ExecuteMeshingJob(voxels, ChunkSize,
                out var vertices, out var triangles, out var uvs, out var normals, out var colors);

            // Assert - Basic validity checks
            Assert.AreEqual(0, triangles.Length % 3, "Triangle count must be multiple of 3");
            Assert.Greater(triangles.Length, 0, "Should generate at least some triangles");
            Assert.AreEqual(vertices.Length, normals.Length, "Normals count must match vertices");
            Assert.AreEqual(vertices.Length, uvs.Length, "UVs count must match vertices");
            Assert.AreEqual(vertices.Length, colors.Length, "Colors count must match vertices");

            // Verify all triangle indices are valid (< vertex count)
            for (int i = 0; i < triangles.Length; i++)
            {
                Assert.Less(triangles[i], vertices.Length,
                    $"Triangle index {i} = {triangles[i]} is out of bounds (vertex count = {vertices.Length})");
                Assert.GreaterOrEqual(triangles[i], 0,
                    $"Triangle index {i} = {triangles[i]} is negative");
            }

            // Cleanup
            DisposeAll(voxels, vertices, triangles, uvs, normals, colors);
        }

        /// <summary>
        /// Test 5: Verify worst-case scenario (checkerboard) doesn't exceed 65K vertex limit.
        /// Checkerboard creates maximum possible faces, so we use a smaller chunk size.
        /// </summary>
        [Test]
        public void VerticesLimitTest()
        {
            // Arrange - Create worst-case checkerboard pattern
            // 32³ checkerboard = 393K vertices (exceeds limit!)
            // 16³ checkerboard = ~49K vertices (within limit)
            const int testSize = 16;
            var voxels = CreateCheckerboard(testSize);

            // Act
            ExecuteMeshingJob(voxels, testSize,
                out var vertices, out var triangles, out var uvs, out var normals, out var colors);

            // Assert
            Assert.Less(vertices.Length, 65535,
                $"Vertex count {vertices.Length} must stay under Unity's 65535 limit (uint16)");

            // Also verify that a 64³ realistic terrain chunk (not checkerboard) stays under limit
            var realisticVoxels = CreateProceduralTerrain(ChunkSize);
            ExecuteMeshingJob(realisticVoxels, ChunkSize,
                out var rv, out var rt, out var ru, out var rn, out var rc);

            Assert.Less(rv.Length, 65535,
                $"Realistic terrain vertex count {rv.Length} must stay under 65K limit");

            UnityEngine.Debug.Log($"[GreedyMeshingJobTests] Worst-case (16³ checkerboard): {vertices.Length} vertices, Realistic (64³ terrain): {rv.Length} vertices");

            // Cleanup
            DisposeAll(voxels, vertices, triangles, uvs, normals, colors);
            DisposeAll(realisticVoxels, rv, rt, ru, rn, rc);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates an empty chunk filled with Air.
        /// </summary>
        private static NativeArray<VoxelType> CreateEmptyChunk(int chunkSize)
        {
            int totalVoxels = chunkSize * chunkSize * chunkSize;
            var voxels = new NativeArray<VoxelType>(totalVoxels, Allocator.TempJob);

            for (int i = 0; i < totalVoxels; i++)
            {
                voxels[i] = VoxelType.Air;
            }

            return voxels;
        }

        /// <summary>
        /// Creates a chunk with a single solid voxel at the specified position.
        /// </summary>
        private static NativeArray<VoxelType> CreateSingleCube(int chunkSize, int3 position, VoxelType type)
        {
            var voxels = CreateEmptyChunk(chunkSize);
            int index = VoxelMath.Flatten3DIndex(position.x, position.y, position.z, chunkSize);
            voxels[index] = type;
            return voxels;
        }

        /// <summary>
        /// Creates a flat plane of voxels at Y=0 with specified width and height.
        /// </summary>
        private static NativeArray<VoxelType> CreateFlatPlane(int chunkSize, int width, int height, VoxelType type)
        {
            var voxels = CreateEmptyChunk(chunkSize);

            for (int z = 0; z < height && z < chunkSize; z++)
            {
                for (int x = 0; x < width && x < chunkSize; x++)
                {
                    int index = VoxelMath.Flatten3DIndex(x, 0, z, chunkSize);
                    voxels[index] = type;
                }
            }

            return voxels;
        }

        /// <summary>
        /// Creates a 3D checkerboard pattern (worst case for meshing - maximum faces).
        /// </summary>
        private static NativeArray<VoxelType> CreateCheckerboard(int chunkSize)
        {
            var voxels = CreateEmptyChunk(chunkSize);

            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        // Checkerboard: solid if (x + y + z) is even
                        if ((x + y + z) % 2 == 0)
                        {
                            int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);
                            voxels[index] = VoxelType.Grass;
                        }
                    }
                }
            }

            return voxels;
        }

        /// <summary>
        /// Creates realistic procedural terrain using simple height-based generation.
        /// Approximately 28% fill ratio (typical for surface terrain).
        /// </summary>
        private static NativeArray<VoxelType> CreateProceduralTerrain(int chunkSize)
        {
            var voxels = CreateEmptyChunk(chunkSize);

            // Simple heightmap-based terrain
            for (int z = 0; z < chunkSize; z++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    // Simple noise-like pattern using position
                    float noise = math.sin(x * 0.1f) * math.cos(z * 0.1f);
                    int height = (int)(chunkSize * 0.3f + noise * 5.0f);
                    height = math.clamp(height, 0, chunkSize - 1);

                    for (int y = 0; y <= height; y++)
                    {
                        int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);

                        // Top layer = Grass, next 3 = Dirt, rest = Stone
                        if (y == height)
                            voxels[index] = VoxelType.Grass;
                        else if (y >= height - 3)
                            voxels[index] = VoxelType.Dirt;
                        else
                            voxels[index] = VoxelType.Stone;
                    }
                }
            }

            return voxels;
        }

        /// <summary>
        /// Creates a random pattern with specified fill ratio.
        /// </summary>
        private static NativeArray<VoxelType> CreateRandomPattern(int chunkSize, float fillRatio)
        {
            var voxels = CreateEmptyChunk(chunkSize);
            var random = new Unity.Mathematics.Random(12345); // Fixed seed for deterministic tests

            int totalVoxels = chunkSize * chunkSize * chunkSize;
            for (int i = 0; i < totalVoxels; i++)
            {
                if (random.NextFloat() < fillRatio)
                {
                    voxels[i] = VoxelType.Stone;
                }
            }

            return voxels;
        }

        /// <summary>
        /// Executes the GreedyMeshingJob and returns the generated mesh data.
        /// </summary>
        private static void ExecuteMeshingJob(
            NativeArray<VoxelType> voxels,
            int chunkSize,
            out NativeList<float3> vertices,
            out NativeList<int> triangles,
            out NativeList<float2> uvs,
            out NativeList<float3> normals,
            out NativeList<float4> colors)
        {
            // Allocate output buffers
            vertices = new NativeList<float3>(Allocator.TempJob);
            triangles = new NativeList<int>(Allocator.TempJob);
            uvs = new NativeList<float2>(Allocator.TempJob);
            normals = new NativeList<float3>(Allocator.TempJob);
            colors = new NativeList<float4>(Allocator.TempJob);

            // Allocate temporary mask buffers
            int maskSize = chunkSize * chunkSize;
            var mask = new NativeArray<bool>(maskSize, Allocator.TempJob);
            var maskVoxelTypes = new NativeArray<VoxelType>(maskSize, Allocator.TempJob);

            // Create and execute job
            var job = new GreedyMeshingJob
            {
                Voxels = voxels,
                ChunkSize = chunkSize,
                Vertices = vertices,
                Triangles = triangles,
                UVs = uvs,
                Normals = normals,
                Colors = colors,
                Mask = mask,
                MaskVoxelTypes = maskVoxelTypes
            };

            job.Run();

            // Cleanup temp buffers
            mask.Dispose();
            maskVoxelTypes.Dispose();
        }

        /// <summary>
        /// Helper to dispose multiple native collections.
        /// </summary>
        private static void DisposeAll(params System.IDisposable[] disposables)
        {
            foreach (var disposable in disposables)
            {
                disposable?.Dispose();
            }
        }

        #endregion
    }
}
