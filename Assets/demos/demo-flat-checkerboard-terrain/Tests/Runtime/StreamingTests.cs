using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Linq;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;
using UnityEngine.TestTools.Logging;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain.Tests
{
    /// <summary>
    /// Tests for dynamic terrain streaming functionality.
    /// Validates chunk loading/unloading behavior when player moves across the terrain.
    /// </summary>
    [TestFixture]
    public class StreamingTests
    {
        private GameObject parentObject;
        private VoxelConfiguration config;
        private ChunkManager chunkManager;
        private FlatCheckerboardGenerator generator;
        private Material testMaterial;

        [SetUp]
        public void Setup()
        {
            // Create parent GameObject for chunks
            parentObject = new GameObject("TestStreamingParent");

            // Create VoxelConfiguration with streaming-friendly settings
            config = ScriptableObject.CreateInstance<VoxelConfiguration>();
            config.ChunkSize = 32;
            config.MacroVoxelSize = 0.2f;
            config.MaxChunksLoadedPerFrame = 10;
            config.UseAmortizedMeshing = false;

            // Create generator and test material
            generator = new FlatCheckerboardGenerator();
            testMaterial = new Material(Shader.Find("Standard"));

            // Create ChunkManager with parent GameObject
            chunkManager = new ChunkManager(config, parentObject.transform, testMaterial, generator);
        }

        [TearDown]
        public void TearDown()
        {
            // Manually destroy GameObjects FIRST (ChunkManager.Dispose() will fail in Edit Mode)
            if (parentObject != null)
            {
                var chunks = parentObject.GetComponentsInChildren<Transform>();
                foreach (var chunkTransform in chunks)
                {
                    if (chunkTransform != null && chunkTransform.gameObject != parentObject)
                    {
                        Object.DestroyImmediate(chunkTransform.gameObject);
                    }
                }
                Object.DestroyImmediate(parentObject);
            }

            // Dispose ChunkManager resources (this will try to Destroy GameObjects that are already destroyed)
            // Suppress the expected error log since TerrainChunk.Dispose() uses Object.Destroy() in Edit Mode
            if (chunkManager != null)
            {
                chunkManager.Dispose();
            }

            // Cleanup ScriptableObjects
            if (config != null) Object.DestroyImmediate(config);
            if (testMaterial != null) Object.DestroyImmediate(testMaterial);
        }

        /// <summary>
        /// Test 1: Verify that new chunks are loaded when the player moves to a new area.
        /// Simulates player starting at origin, then moving to a new position offset by 5 chunks.
        /// </summary>
        [UnityTest]
        public IEnumerator Streaming_LoadsNewChunks_WhenPlayerMoves()
        {
            // Arrange: Start at origin
            Vector3 startPos = new Vector3(0, 2, 0);
            int loadRadius = 2;

            // Load initial chunks around starting position
            ChunkCoord startChunk = GetChunkCoordFromPosition(startPos);
            LoadChunksInRadius(startChunk, loadRadius);

            // Process generation queue to generate all chunks
            for (int i = 0; i < 20; i++)
            {
                chunkManager.ProcessGenerationQueue();
                chunkManager.ProcessMeshingQueue(Time.deltaTime);
                yield return null;
            }

            int initialChunkCount = chunkManager.ActiveChunkCount;

            // Act: Move player to new position (shift by ~5 chunks in X direction)
            // Chunk world size = 32 voxels * 0.2 = 6.4 units
            // Move by 32 units = ~5 chunks
            Vector3 newPos = new Vector3(32f, 2, 0);
            ChunkCoord newChunk = GetChunkCoordFromPosition(newPos);
            LoadChunksInRadius(newChunk, loadRadius);

            // Process generation for new chunks
            for (int i = 0; i < 20; i++)
            {
                chunkManager.ProcessGenerationQueue();
                chunkManager.ProcessMeshingQueue(Time.deltaTime);
                yield return null;
            }

            int finalChunkCount = chunkManager.ActiveChunkCount;

            // Assert: New chunks should have been loaded
            Assert.Greater(finalChunkCount, initialChunkCount,
                "New chunks should be loaded when player moves to new area");
        }

        /// <summary>
        /// Test 2: Verify that old chunks are unloaded when the player moves far away.
        /// Tests that chunks outside the streaming radius are properly cleaned up.
        /// </summary>
        [UnityTest]
        public IEnumerator Streaming_UnloadsOldChunks_WhenPlayerMovesAway()
        {
            // Arrange: Load chunks at origin
            ChunkCoord origin = new ChunkCoord(0, 0, 0);
            LoadChunksInRadius(origin, 2);

            // Process generation
            for (int i = 0; i < 20; i++)
            {
                chunkManager.ProcessGenerationQueue();
                chunkManager.ProcessMeshingQueue(Time.deltaTime);
                yield return null;
            }

            // Verify origin chunk is loaded
            Assert.IsTrue(chunkManager.HasChunk(origin),
                "Origin chunk should be loaded initially");

            // Act: Move far away and unload old chunks
            ChunkCoord farChunk = new ChunkCoord(10, 0, 10);

            // Expect error logs from Object.Destroy() calls in Edit Mode (multiple chunks will be unloaded)
            LogAssert.ignoreFailingMessages = true;
            UnloadChunksOutsideRadius(farChunk, 3);
            LogAssert.ignoreFailingMessages = false;

            // Process any pending operations
            for (int i = 0; i < 5; i++)
            {
                chunkManager.ProcessGenerationQueue();
                chunkManager.ProcessMeshingQueue(Time.deltaTime);
                yield return null;
            }

            // Assert: Origin chunk should be unloaded
            Assert.IsFalse(chunkManager.HasChunk(origin),
                "Old chunks should be unloaded when player moves far away");
        }

        /// <summary>
        /// Test 3: Verify that streaming maintains a reasonable chunk count during movement.
        /// Simulates player moving across multiple positions and validates chunk count stays bounded.
        /// </summary>
        [UnityTest]
        public IEnumerator Streaming_MaintainsChunkCount_WithinExpectedRange()
        {
            // Arrange: Define streaming parameters
            int loadRadius = 2;
            int maxExpectedChunks = (2 * loadRadius + 1) * (2 * loadRadius + 1); // 5x5 = 25 chunks

            // Act: Simulate player movement across 5 steps
            for (int moveStep = 0; moveStep < 5; moveStep++)
            {
                // Move player progressively in X direction
                Vector3 playerPos = new Vector3(moveStep * 20f, 2, 0);
                ChunkCoord playerChunk = GetChunkCoordFromPosition(playerPos);

                // Load new chunks around player
                LoadChunksInRadius(playerChunk, loadRadius);

                // Unload chunks far from player
                // Ignore error logs from Object.Destroy() calls in Edit Mode
                LogAssert.ignoreFailingMessages = true;
                UnloadChunksOutsideRadius(playerChunk, 3);
                LogAssert.ignoreFailingMessages = false;

                // Process generation/meshing
                for (int i = 0; i < 10; i++)
                {
                    chunkManager.ProcessGenerationQueue();
                    chunkManager.ProcessMeshingQueue(Time.deltaTime);
                    yield return null;
                }
            }

            // Assert: Chunk count should be within reasonable bounds
            int activeChunks = chunkManager.ActiveChunkCount;
            Assert.LessOrEqual(activeChunks, maxExpectedChunks + 5, // +5 buffer for delayed unload
                "Active chunk count should not exceed expected maximum during streaming");
        }

        // ===== Helper Methods =====

        /// <summary>
        /// Converts a world position to chunk coordinates.
        /// Same logic as FlatTerrainDemoController.GetChunkCoordFromPosition().
        /// </summary>
        private ChunkCoord GetChunkCoordFromPosition(Vector3 worldPosition)
        {
            int chunkSize = config.ChunkSize;
            float voxelSize = config.MacroVoxelSize;
            float chunkWorldSize = chunkSize * voxelSize;

            int chunkX = Mathf.FloorToInt(worldPosition.x / chunkWorldSize);
            int chunkY = 0; // Terrain is always at Y=0
            int chunkZ = Mathf.FloorToInt(worldPosition.z / chunkWorldSize);

            return new ChunkCoord(chunkX, chunkY, chunkZ);
        }

        /// <summary>
        /// Loads all chunks within a given radius of a center chunk.
        /// Same logic as FlatTerrainDemoController.LoadChunksInRadius().
        /// </summary>
        private void LoadChunksInRadius(ChunkCoord center, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    ChunkCoord coord = new ChunkCoord(center.X + x, 0, center.Z + z);
                    if (!chunkManager.HasChunk(coord))
                    {
                        chunkManager.LoadChunk(coord);
                    }
                }
            }
        }

        /// <summary>
        /// Unloads all chunks outside a given radius from a center chunk.
        /// Same logic as FlatTerrainDemoController.UnloadChunksOutsideRadius().
        /// </summary>
        private void UnloadChunksOutsideRadius(ChunkCoord center, int radius)
        {
            var allChunks = chunkManager.GetAllChunks();
            foreach (var chunk in allChunks.ToArray())
            {
                int dx = Mathf.Abs(chunk.Coord.X - center.X);
                int dz = Mathf.Abs(chunk.Coord.Z - center.Z);

                if (dx > radius || dz > radius)
                {
                    chunkManager.UnloadChunk(chunk.Coord);
                }
            }
        }
    }
}
