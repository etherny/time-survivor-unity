using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Voxel.Terrain.Tests
{
    /// <summary>
    /// Unit tests for TerrainChunk collision-related functionality.
    /// Tests the new collision system API: SetCollisionMesh, RemoveCollision, MarkCollisionPending.
    /// </summary>
    [TestFixture]
    public class TerrainChunkCollisionTests
    {
        private GameObject _testParent;
        private Material _testMaterial;
        private const int TestChunkSize = 16;

        [SetUp]
        public void SetUp()
        {
            _testParent = new GameObject("TestChunkParent");
            _testMaterial = new Material(Shader.Find("Standard"));
        }

        [TearDown]
        public void TearDown()
        {
            if (_testParent != null)
            {
                Object.DestroyImmediate(_testParent);
            }
            if (_testMaterial != null)
            {
                Object.DestroyImmediate(_testMaterial);
            }
        }

        [Test]
        public void Constructor_ShouldInitializeWithNoCollision()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);

            // Act
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);

            // Assert
            Assert.IsFalse(chunk.HasCollision, "Chunk should not have collision initially");
            Assert.IsFalse(chunk.IsCollisionPending, "Chunk should not have pending collision initially");

            var meshCollider = chunk.GameObject.GetComponent<MeshCollider>();
            Assert.IsNull(meshCollider, "MeshCollider should not be created by default");

            // Cleanup
            chunk.Dispose();
        }

        [Test]
        public void SetCollisionMesh_WithValidMesh_ShouldCreateMeshColliderAndSetFlags()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);
            var collisionMesh = CreateTestCubeMesh();
            string layerName = "Default"; // Use existing layer for testing

            // Act
            chunk.SetCollisionMesh(collisionMesh, layerName);

            // Assert
            Assert.IsTrue(chunk.HasCollision, "HasCollision should be true after setting mesh");
            Assert.IsFalse(chunk.IsCollisionPending, "IsCollisionPending should be false after setting mesh");

            var meshCollider = chunk.GameObject.GetComponent<MeshCollider>();
            Assert.IsNotNull(meshCollider, "MeshCollider should be created");
            Assert.AreEqual(collisionMesh, meshCollider.sharedMesh, "Collision mesh should be assigned");
            Assert.IsFalse(meshCollider.convex, "Terrain collision should be non-convex");

            // Cleanup
            chunk.Dispose();
            Object.DestroyImmediate(collisionMesh);
        }

        [Test]
        public void SetCollisionMesh_WithNullMesh_ShouldLogWarningAndNotSetFlags()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);
            string layerName = "Default";

            // Act
            LogAssert.Expect(LogType.Warning, $"[TerrainChunk] Attempted to set null collision mesh on chunk {coord}");
            chunk.SetCollisionMesh(null, layerName);

            // Assert
            Assert.IsFalse(chunk.HasCollision, "HasCollision should remain false");
            var meshCollider = chunk.GameObject.GetComponent<MeshCollider>();
            Assert.IsNull(meshCollider, "MeshCollider should not be created");

            // Cleanup
            chunk.Dispose();
        }

        [Test]
        public void SetCollisionMesh_CalledTwice_ShouldReuseExistingMeshCollider()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);
            var mesh1 = CreateTestCubeMesh();
            var mesh2 = CreateTestCubeMesh();
            string layerName = "Default";

            // Act
            chunk.SetCollisionMesh(mesh1, layerName);
            var firstCollider = chunk.GameObject.GetComponent<MeshCollider>();
            chunk.SetCollisionMesh(mesh2, layerName);
            var secondCollider = chunk.GameObject.GetComponent<MeshCollider>();

            // Assert
            Assert.AreSame(firstCollider, secondCollider, "Should reuse existing MeshCollider");
            Assert.AreEqual(mesh2, secondCollider.sharedMesh, "Second mesh should be assigned");

            var allColliders = chunk.GameObject.GetComponents<MeshCollider>();
            Assert.AreEqual(1, allColliders.Length, "Should only have one MeshCollider");

            // Cleanup
            chunk.Dispose();
            Object.DestroyImmediate(mesh1);
            Object.DestroyImmediate(mesh2);
        }

        [Test]
        public void SetCollisionMesh_WithInvalidLayer_ShouldLogWarningButStillSetMesh()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);
            var collisionMesh = CreateTestCubeMesh();
            string invalidLayerName = "NonExistentLayer_12345";

            // Act
            LogAssert.Expect(LogType.Warning, $"[TerrainChunk] Layer '{invalidLayerName}' not found. Using default layer.");
            chunk.SetCollisionMesh(collisionMesh, invalidLayerName);

            // Assert
            Assert.IsTrue(chunk.HasCollision, "HasCollision should still be true");
            var meshCollider = chunk.GameObject.GetComponent<MeshCollider>();
            Assert.IsNotNull(meshCollider, "MeshCollider should still be created");

            // Cleanup
            chunk.Dispose();
            Object.DestroyImmediate(collisionMesh);
        }

        [Test]
        public void RemoveCollision_WithExistingCollider_ShouldDestroyColliderAndClearFlags()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);
            var collisionMesh = CreateTestCubeMesh();
            chunk.SetCollisionMesh(collisionMesh, "Default");

            // Act
            chunk.RemoveCollision();

            // Ensure Unity processes the destroy immediately for test
            Object.DestroyImmediate(chunk.GameObject.GetComponent<MeshCollider>());

            // Assert
            Assert.IsFalse(chunk.HasCollision, "HasCollision should be false");
            Assert.IsFalse(chunk.IsCollisionPending, "IsCollisionPending should be false");

            var meshCollider = chunk.GameObject.GetComponent<MeshCollider>();
            Assert.IsNull(meshCollider, "MeshCollider should be destroyed");

            // Cleanup
            chunk.Dispose();
            Object.DestroyImmediate(collisionMesh);
        }

        [Test]
        public void RemoveCollision_WithoutCollider_ShouldHandleGracefully()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => chunk.RemoveCollision());
            Assert.IsFalse(chunk.HasCollision);

            // Cleanup
            chunk.Dispose();
        }

        [Test]
        public void MarkCollisionPending_ShouldSetPendingFlag()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);

            // Act
            chunk.MarkCollisionPending();

            // Assert
            Assert.IsTrue(chunk.IsCollisionPending, "IsCollisionPending should be true");
            Assert.IsFalse(chunk.HasCollision, "HasCollision should remain false");

            // Cleanup
            chunk.Dispose();
        }

        [Test]
        public void SetCollisionMesh_AfterMarkPending_ShouldClearPendingFlag()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);
            var collisionMesh = CreateTestCubeMesh();
            chunk.MarkCollisionPending();

            // Act
            chunk.SetCollisionMesh(collisionMesh, "Default");

            // Assert
            Assert.IsTrue(chunk.HasCollision, "HasCollision should be true");
            Assert.IsFalse(chunk.IsCollisionPending, "IsCollisionPending should be cleared");

            // Cleanup
            chunk.Dispose();
            Object.DestroyImmediate(collisionMesh);
        }

        [Test]
        public void RemoveCollision_AfterMarkPending_ShouldClearPendingFlag()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);
            chunk.MarkCollisionPending();

            // Act
            chunk.RemoveCollision();

            // Assert
            Assert.IsFalse(chunk.HasCollision);
            Assert.IsFalse(chunk.IsCollisionPending, "IsCollisionPending should be cleared");

            // Cleanup
            chunk.Dispose();
        }

        [Test]
        public void SetMesh_ShouldNotAffectCollision()
        {
            // Arrange
            var coord = new ChunkCoord(0, 0, 0);
            var chunk = new TerrainChunk(coord, _testParent.transform, _testMaterial);
            var renderMesh = CreateTestCubeMesh();

            // Act
            chunk.SetMesh(renderMesh);

            // Assert
            Assert.IsFalse(chunk.HasCollision, "SetMesh should not affect HasCollision");
            Assert.IsFalse(chunk.IsCollisionPending, "SetMesh should not affect IsCollisionPending");

            var meshCollider = chunk.GameObject.GetComponent<MeshCollider>();
            Assert.IsNull(meshCollider, "SetMesh should not create MeshCollider");

            // Cleanup
            chunk.Dispose();
            Object.DestroyImmediate(renderMesh);
        }

        /// <summary>
        /// Helper method to create a simple cube mesh for testing.
        /// </summary>
        private Mesh CreateTestCubeMesh()
        {
            var mesh = new Mesh
            {
                name = "TestCubeMesh",
                vertices = new Vector3[]
                {
                    new Vector3(0, 0, 0),
                    new Vector3(1, 0, 0),
                    new Vector3(1, 1, 0),
                    new Vector3(0, 1, 0),
                    new Vector3(0, 0, 1),
                    new Vector3(1, 0, 1),
                    new Vector3(1, 1, 1),
                    new Vector3(0, 1, 1)
                },
                triangles = new int[]
                {
                    0, 2, 1, 0, 3, 2, // Front
                    1, 6, 5, 1, 2, 6, // Right
                    5, 7, 4, 5, 6, 7, // Back
                    4, 3, 0, 4, 7, 3, // Left
                    3, 6, 2, 3, 7, 6, // Top
                    4, 1, 5, 4, 0, 1  // Bottom
                }
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
