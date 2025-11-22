using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;
using Unity.Collections;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain.Tests
{
    /// <summary>
    /// Integration tests for FlatCheckerboardGenerator with ChunkManager.
    /// Tests end-to-end terrain generation, chunk streaming, and mesh generation.
    /// </summary>
    [TestFixture]
    public class FlatTerrainIntegrationTests
    {
        private GameObject parentObject;
        private ChunkManager chunkManager;
        private VoxelConfiguration config;
        private FlatCheckerboardGenerator generator;
        private Material testMaterial;

        [SetUp]
        public void SetUp()
        {
            // Create parent GameObject for chunks
            parentObject = new GameObject("TestChunkParent");

            // Create VoxelConfiguration
            config = ScriptableObject.CreateInstance<VoxelConfiguration>();
            config.ChunkSize = 16;
            config.MacroVoxelSize = 1.0f;

            // Create test material
            testMaterial = new Material(Shader.Find("Standard"));

            // Create generator
            generator = new FlatCheckerboardGenerator();

            // Create ChunkManager (it's a regular class, not MonoBehaviour)
            chunkManager = new ChunkManager(config, parentObject.transform, testMaterial, generator);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup GameObjects first
            if (parentObject != null)
            {
                // Manually clean up all chunk GameObjects to avoid Destroy() error in Edit Mode
                var chunks = parentObject.GetComponentsInChildren<Transform>();
                foreach (var chunk in chunks)
                {
                    if (chunk != null && chunk.gameObject != parentObject)
                    {
                        Object.DestroyImmediate(chunk.gameObject);
                    }
                }
                Object.DestroyImmediate(parentObject);
            }

            // Dispose ChunkManager resources (this will try to Destroy GameObjects that are already destroyed)
            if (chunkManager != null)
            {
                chunkManager.Dispose();
            }

            // Cleanup ScriptableObjects
            if (config != null)
            {
                Object.DestroyImmediate(config);
            }

            if (testMaterial != null)
            {
                Object.DestroyImmediate(testMaterial);
            }
        }

        /// <summary>
        /// Test 1: Verify ChunkManager uses FlatCheckerboardGenerator for chunk generation.
        /// </summary>
        [Test]
        public void ChunkManager_UsesCustomGenerator_ForChunkGeneration()
        {
            // Arrange
            ChunkCoord testCoord = new ChunkCoord(0, 0, 0);

            // Act
            chunkManager.LoadChunk(testCoord);
            chunkManager.ProcessGenerationQueue();

            TerrainChunk chunk = chunkManager.GetChunk(testCoord);

            // Assert
            Assert.IsNotNull(chunk, "ChunkManager should create a chunk");
            Assert.AreEqual(testCoord, chunk.Coord, "Chunk should have correct coordinate");

            // Verify chunk has been generated
            Assert.IsTrue(chunk.IsGenerated, "Chunk should be marked as generated");

            // The chunk should exist and be tracked by ChunkManager
            Assert.IsTrue(chunkManager.IsChunkLoaded(testCoord), "ChunkManager should track the created chunk");
        }

        /// <summary>
        /// Test 2: Verify ChunkManager generates 9 chunks in demo configuration.
        /// Demo creates a 3x3 grid of chunks (X: -1 to 1, Z: -1 to 1, Y: 0).
        /// </summary>
        [Test]
        public void ChunkManager_Generates9Chunks_InDemoConfiguration()
        {
            // Arrange
            int expectedChunkCount = 9; // 3x3 grid

            // Act - Generate chunks in 3x3 grid (simulating demo setup)
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    ChunkCoord coord = new ChunkCoord(x, 0, z);
                    chunkManager.LoadChunk(coord);
                }
            }

            // Assert
            int actualChunkCount = 0;
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    ChunkCoord coord = new ChunkCoord(x, 0, z);
                    if (chunkManager.IsChunkLoaded(coord))
                    {
                        actualChunkCount++;
                    }
                }
            }

            Assert.AreEqual(expectedChunkCount, actualChunkCount,
                "ChunkManager should generate exactly 9 chunks in 3x3 grid");
        }

        /// <summary>
        /// Test 3: Verify checkerboard pattern is consistent across adjacent chunks.
        /// Tests that terrain patterns align correctly at chunk boundaries.
        /// </summary>
        [Test]
        public void ChunkManager_CheckerboardPatternIsConsistent_AcrossAdjacentChunks()
        {
            // Arrange
            ChunkCoord coord0 = new ChunkCoord(0, 0, 0);
            ChunkCoord coord1 = new ChunkCoord(1, 0, 0);

            // Act
            chunkManager.LoadChunk(coord0);
            chunkManager.LoadChunk(coord1);
            chunkManager.ProcessGenerationQueue();

            TerrainChunk chunk0 = chunkManager.GetChunk(coord0);
            TerrainChunk chunk1 = chunkManager.GetChunk(coord1);

            // Assert - Chunks should exist
            Assert.IsNotNull(chunk0, "Chunk at (0,0,0) should exist");
            Assert.IsNotNull(chunk1, "Chunk at (1,0,0) should exist");

            // Verify boundary consistency using generator's GetVoxelAt()
            // Last voxel of chunk 0 at X=15, world coordinate = 0*16 + 15 = 15
            // First voxel of chunk 1 at X=0, world coordinate = 1*16 + 0 = 16

            VoxelType voxel_chunk0_boundary = generator.GetVoxelAt(15, 0, 0);
            VoxelType voxel_chunk1_boundary = generator.GetVoxelAt(16, 0, 0);

            // Pattern should change at tile boundary (15 is in tile 1, 16 is in tile 2)
            Assert.AreNotEqual(voxel_chunk0_boundary, voxel_chunk1_boundary,
                "Pattern should be consistent but different across tile boundaries");

            // Verify specific values
            Assert.AreEqual(VoxelType.Dirt, voxel_chunk0_boundary,
                "World position (15,0,0) should be Dirt");
            Assert.AreEqual(VoxelType.Grass, voxel_chunk1_boundary,
                "World position (16,0,0) should be Grass");
        }

        /// <summary>
        /// Test 4: Verify ChunkManager generates valid meshes for all chunks.
        /// UnityTest to allow yielding for mesh generation (if async).
        /// </summary>
        [UnityTest]
        public IEnumerator ChunkManager_GeneratesValidMeshes_ForAllChunks()
        {
            // Arrange & Act - Generate chunks
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    ChunkCoord coord = new ChunkCoord(x, 0, z);
                    chunkManager.LoadChunk(coord);
                }
            }

            // Process generation and meshing queues
            chunkManager.ProcessGenerationQueue();
            chunkManager.ProcessMeshingQueue(Time.deltaTime);

            // Wait a frame for mesh application
            yield return null;

            // Assert - Check all chunks have valid meshes
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    ChunkCoord coord = new ChunkCoord(x, 0, z);
                    TerrainChunk chunk = chunkManager.GetChunk(coord);

                    Assert.IsNotNull(chunk, $"Chunk at {coord} should exist");
                    Assert.IsNotNull(chunk.GameObject, $"Chunk at {coord} should have GameObject");

                    // Check for MeshFilter and MeshRenderer components
                    MeshFilter meshFilter = chunk.GameObject.GetComponent<MeshFilter>();
                    MeshRenderer meshRenderer = chunk.GameObject.GetComponent<MeshRenderer>();

                    Assert.IsNotNull(meshFilter, $"Chunk at {coord} should have MeshFilter");
                    Assert.IsNotNull(meshRenderer, $"Chunk at {coord} should have MeshRenderer");

                    // Check mesh validity
                    if (meshFilter.sharedMesh != null)
                    {
                        Mesh mesh = meshFilter.sharedMesh;
                        Assert.Greater(mesh.vertexCount, 0,
                            $"Chunk at {coord} should have vertices in mesh");
                        Assert.Greater(mesh.triangles.Length, 0,
                            $"Chunk at {coord} should have triangles in mesh");
                    }
                }
            }
        }

        /// <summary>
        /// Test 5: Verify ChunkManager sets correct world positions for chunks.
        /// </summary>
        [Test]
        public void ChunkManager_SetsCorrectWorldPositions_ForChunks()
        {
            // Arrange
            float chunkWorldSize = config.ChunkSize * config.MacroVoxelSize;

            // Act & Assert - Generate chunks and verify positions
            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    ChunkCoord coord = new ChunkCoord(x, 0, z);
                    chunkManager.LoadChunk(coord);

                    TerrainChunk chunk = chunkManager.GetChunk(coord);

                    Vector3 expectedPosition = new Vector3(
                        x * chunkWorldSize,
                        0 * chunkWorldSize,
                        z * chunkWorldSize
                    );

                    Vector3 actualPosition = chunk.GameObject.transform.position;

                    Assert.AreEqual(expectedPosition.x, actualPosition.x, 0.01f,
                        $"Chunk at {coord} should have correct X world position");
                    Assert.AreEqual(expectedPosition.y, actualPosition.y, 0.01f,
                        $"Chunk at {coord} should have correct Y world position");
                    Assert.AreEqual(expectedPosition.z, actualPosition.z, 0.01f,
                        $"Chunk at {coord} should have correct Z world position");
                }
            }
        }
    }
}
