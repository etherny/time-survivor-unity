using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Collections;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Physics;

namespace TimeSurvivor.Voxel.Physics.Tests
{
    /// <summary>
    /// Unit tests for VoxelCollisionBaker and CollisionBakingJob.
    /// Tests collision mesh generation with reduced resolution.
    /// </summary>
    [TestFixture]
    public class VoxelCollisionBakerTests
    {
        private const int TestChunkSize = 16;

        [TearDown]
        public void TearDown()
        {
            // Cleanup any leaked resources
        }

        [Test]
        public void BakeCollisionSync_WithEmptyChunk_ShouldReturnEmptyMesh()
        {
            // Arrange
            var voxels = CreateEmptyChunk(TestChunkSize);

            // Act
            var mesh = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);

            // Assert
            Assert.IsNotNull(mesh);
            Assert.AreEqual(0, mesh.vertexCount, "Empty chunk should produce empty mesh");
            Assert.AreEqual(0, mesh.triangles.Length, "Empty chunk should have no triangles");

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BakeCollisionSync_WithFullChunk_ShouldGenerateCollisionMesh()
        {
            // Arrange
            var voxels = CreateFullChunk(TestChunkSize, VoxelType.Stone);

            // Act
            var mesh = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);

            // Assert
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "Full chunk should produce vertices");
            Assert.Greater(mesh.triangles.Length, 0, "Full chunk should produce triangles");
            Assert.AreEqual(0, mesh.triangles.Length % 3, "Triangle count should be multiple of 3");

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BakeCollisionSync_WithSingleVoxel_ShouldGenerateCube()
        {
            // Arrange
            var voxels = CreateEmptyChunk(TestChunkSize);
            SetVoxel(voxels, 8, 8, 8, VoxelType.Stone, TestChunkSize); // Center voxel

            // Act
            var mesh = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 1);

            // Assert
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "Single voxel should produce vertices");
            Assert.AreEqual(8, mesh.vertexCount, "Single exposed cube should have 8 vertices");
            Assert.AreEqual(36, mesh.triangles.Length, "Single cube should have 36 triangle indices (6 faces × 2 triangles × 3 vertices)");

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BakeCollisionSync_WithHalfResolution_ShouldReduceComplexity()
        {
            // Arrange
            var voxels = CreateFullChunk(TestChunkSize, VoxelType.Stone);

            // Act
            var meshFull = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 1);
            var meshHalf = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);

            // Assert
            Assert.IsNotNull(meshFull);
            Assert.IsNotNull(meshHalf);
            Assert.Less(meshHalf.vertexCount, meshFull.vertexCount, "Half resolution should have fewer vertices");
            Assert.Less(meshHalf.triangles.Length, meshFull.triangles.Length, "Half resolution should have fewer triangles");

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(meshFull);
            Object.DestroyImmediate(meshHalf);
        }

        [Test]
        public void BakeCollisionSync_WithQuarterResolution_ShouldReduceComplexityFurther()
        {
            // Arrange
            var voxels = CreateFullChunk(TestChunkSize, VoxelType.Stone);

            // Act
            var meshHalf = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);
            var meshQuarter = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 4);

            // Assert
            Assert.IsNotNull(meshHalf);
            Assert.IsNotNull(meshQuarter);
            Assert.Less(meshQuarter.vertexCount, meshHalf.vertexCount, "Quarter resolution should have fewer vertices than half");
            Assert.Less(meshQuarter.triangles.Length, meshHalf.triangles.Length, "Quarter resolution should have fewer triangles than half");

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(meshHalf);
            Object.DestroyImmediate(meshQuarter);
        }

        [Test]
        public void BakeCollisionAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            var voxels = CreateFullChunk(TestChunkSize, VoxelType.Stone);

            // Act
            var handle = VoxelCollisionBaker.BakeCollisionAsync(voxels, TestChunkSize, resolutionDivider: 2);

            // Assert
            Assert.IsNotNull(handle);
            Assert.IsNotNull(handle.Vertices);
            Assert.IsNotNull(handle.Triangles);

            // Wait for completion
            var mesh = handle.Complete();
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);

            // Cleanup
            handle.Dispose();
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void BakeCollisionAsync_IsCompleted_ShouldBeTrueAfterComplete()
        {
            // Arrange
            var voxels = CreateEmptyChunk(TestChunkSize);

            // Act
            var handle = VoxelCollisionBaker.BakeCollisionAsync(voxels, TestChunkSize, resolutionDivider: 2);
            var mesh = handle.Complete();

            // Assert
            Assert.IsTrue(handle.IsCompleted, "Job should be completed after Complete() call");

            // Cleanup
            handle.Dispose();
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void ApplyCollisionMesh_WithValidMesh_ShouldCreateMeshCollider()
        {
            // Arrange
            var voxels = CreateFullChunk(TestChunkSize, VoxelType.Stone);
            var mesh = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);
            var target = new GameObject("TestCollisionTarget");

            // Act
            var meshCollider = VoxelCollisionBaker.ApplyCollisionMesh(mesh, target, "Default");

            // Assert
            Assert.IsNotNull(meshCollider);
            Assert.AreEqual(mesh, meshCollider.sharedMesh);
            Assert.IsFalse(meshCollider.convex, "Terrain collision should be non-convex");

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void ApplyCollisionMesh_WithNullMesh_ShouldLogWarningAndReturnNull()
        {
            // Arrange
            var target = new GameObject("TestCollisionTarget");

            // Act
            LogAssert.Expect(LogType.Warning, "[VoxelCollisionBaker] Cannot apply null collision mesh");
            var meshCollider = VoxelCollisionBaker.ApplyCollisionMesh(null, target, "Default");

            // Assert
            Assert.IsNull(meshCollider);

            // Cleanup
            Object.DestroyImmediate(target);
        }

        [Test]
        public void ApplyCollisionMesh_WithNullTarget_ShouldLogWarningAndReturnNull()
        {
            // Arrange
            var voxels = CreateFullChunk(TestChunkSize, VoxelType.Stone);
            var mesh = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);

            // Act
            LogAssert.Expect(LogType.Warning, "[VoxelCollisionBaker] Cannot apply collision to null GameObject");
            var meshCollider = VoxelCollisionBaker.ApplyCollisionMesh(mesh, null, "Default");

            // Assert
            Assert.IsNull(meshCollider);

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void ApplyCollisionMesh_CalledTwice_ShouldReuseExistingCollider()
        {
            // Arrange
            var voxels = CreateFullChunk(TestChunkSize, VoxelType.Stone);
            var mesh1 = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);
            var mesh2 = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);
            var target = new GameObject("TestCollisionTarget");

            // Act
            var collider1 = VoxelCollisionBaker.ApplyCollisionMesh(mesh1, target, "Default");
            var collider2 = VoxelCollisionBaker.ApplyCollisionMesh(mesh2, target, "Default");

            // Assert
            Assert.AreSame(collider1, collider2, "Should reuse existing MeshCollider");
            var allColliders = target.GetComponents<MeshCollider>();
            Assert.AreEqual(1, allColliders.Length, "Should only have one MeshCollider");

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh1);
            Object.DestroyImmediate(mesh2);
            Object.DestroyImmediate(target);
        }

        [Test]
        public void CalculateCollisionMemoryUsage_WithValidMesh_ShouldReturnCorrectSize()
        {
            // Arrange
            var voxels = CreateFullChunk(TestChunkSize, VoxelType.Stone);
            var mesh = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);

            // Act
            int memoryUsage = VoxelCollisionBaker.CalculateCollisionMemoryUsage(mesh);

            // Assert
            int expectedVerticesSize = mesh.vertexCount * sizeof(float) * 3;
            int expectedTrianglesSize = mesh.triangles.Length * sizeof(int);
            int expectedTotal = expectedVerticesSize + expectedTrianglesSize;

            Assert.AreEqual(expectedTotal, memoryUsage);
            Assert.Greater(memoryUsage, 0);

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void CalculateCollisionMemoryUsage_WithNullMesh_ShouldReturnZero()
        {
            // Act
            int memoryUsage = VoxelCollisionBaker.CalculateCollisionMemoryUsage(null);

            // Assert
            Assert.AreEqual(0, memoryUsage);
        }

        [Test]
        public void CollisionBakingJob_WithCheckerboardPattern_ShouldGenerateCorrectMesh()
        {
            // Arrange
            var voxels = CreateCheckerboardChunk(TestChunkSize);

            // Act
            var mesh = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 1);

            // Assert
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0, "Checkerboard should produce vertices");
            Assert.Greater(mesh.triangles.Length, 0, "Checkerboard should produce triangles");

            // Verify mesh integrity
            Assert.AreEqual(0, mesh.triangles.Length % 3, "Triangles should be valid");
            Assert.IsNotNull(mesh.bounds);
            Assert.Greater(mesh.bounds.size.magnitude, 0, "Mesh should have valid bounds");

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void CollisionBakingJob_WithLayeredTerrain_ShouldGenerateValidMesh()
        {
            // Arrange - Create terrain with layers (bottom half solid)
            var voxels = CreateEmptyChunk(TestChunkSize);
            for (int y = 0; y < TestChunkSize / 2; y++)
            {
                for (int z = 0; z < TestChunkSize; z++)
                {
                    for (int x = 0; x < TestChunkSize; x++)
                    {
                        SetVoxel(voxels, x, y, z, VoxelType.Stone, TestChunkSize);
                    }
                }
            }

            // Act
            var mesh = VoxelCollisionBaker.BakeCollisionSync(voxels, TestChunkSize, resolutionDivider: 2);

            // Assert
            Assert.IsNotNull(mesh);
            Assert.Greater(mesh.vertexCount, 0);
            Assert.Greater(mesh.triangles.Length, 0);

            // Cleanup
            voxels.Dispose();
            Object.DestroyImmediate(mesh);
        }

        /// <summary>
        /// Helper: Create empty chunk (all air).
        /// </summary>
        private NativeArray<VoxelType> CreateEmptyChunk(int size)
        {
            int volume = size * size * size;
            var voxels = new NativeArray<VoxelType>(volume, Allocator.TempJob);
            for (int i = 0; i < volume; i++)
            {
                voxels[i] = VoxelType.Air;
            }
            return voxels;
        }

        /// <summary>
        /// Helper: Create full chunk (all solid).
        /// </summary>
        private NativeArray<VoxelType> CreateFullChunk(int size, VoxelType type)
        {
            int volume = size * size * size;
            var voxels = new NativeArray<VoxelType>(volume, Allocator.TempJob);
            for (int i = 0; i < volume; i++)
            {
                voxels[i] = type;
            }
            return voxels;
        }

        /// <summary>
        /// Helper: Create checkerboard pattern chunk.
        /// </summary>
        private NativeArray<VoxelType> CreateCheckerboardChunk(int size)
        {
            int volume = size * size * size;
            var voxels = new NativeArray<VoxelType>(volume, Allocator.TempJob);

            for (int y = 0; y < size; y++)
            {
                for (int z = 0; z < size; z++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool isSolid = ((x + y + z) % 2) == 0;
                        int index = x + (z * size) + (y * size * size);
                        voxels[index] = isSolid ? VoxelType.Stone : VoxelType.Air;
                    }
                }
            }

            return voxels;
        }

        /// <summary>
        /// Helper: Set a single voxel.
        /// </summary>
        private void SetVoxel(NativeArray<VoxelType> voxels, int x, int y, int z, VoxelType type, int size)
        {
            int index = x + (z * size) + (y * size * size);
            if (index >= 0 && index < voxels.Length)
            {
                voxels[index] = type;
            }
        }
    }
}
