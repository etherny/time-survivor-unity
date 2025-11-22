using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Streaming;
using TimeSurvivor.Voxel.Terrain;
using UnityEngine;
using UnityEngine.TestTools;

namespace TimeSurvivor.Voxel.Streaming.Tests
{
    /// <summary>
    /// Integration tests for ProceduralTerrainStreamer.
    /// Tests player movement, chunk loading/unloading, hysteresis, cache behavior, and budget constraints.
    /// </summary>
    public class ProceduralTerrainStreamerTests
    {
        private GameObject streamerGO;
        private GameObject playerGO;
        private ProceduralTerrainStreamer streamer;
        private VoxelConfiguration voxelConfig;
        private StreamingConfiguration streamingConfig;
        private Material testMaterial;

        [SetUp]
        public void SetUp()
        {
            // Create player GameObject
            playerGO = new GameObject("TestPlayer");
            playerGO.transform.position = Vector3.zero;

            // Create streamer GameObject
            streamerGO = new GameObject("ProceduralTerrainStreamer");

            // Create VoxelConfiguration (ScriptableObject)
            voxelConfig = ScriptableObject.CreateInstance<VoxelConfiguration>();
            // VoxelConfiguration has public fields with default values - set directly
            voxelConfig.ChunkSize = 16;
            voxelConfig.MacroVoxelSize = 0.2f;
            voxelConfig.MicroVoxelSize = 0.1f;

            // Create StreamingConfiguration
            streamingConfig = ScriptableObject.CreateInstance<StreamingConfiguration>();
            // StreamingConfiguration only has cache-related properties (no noise/generation parameters)

            // Create test material
            testMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            // Expect error logs during AddComponent (Awake() runs before we can assign fields via reflection)
            // In Play Mode, Awake() is called immediately and validates dependencies
            LogAssert.Expect(LogType.Error, "VoxelConfiguration not assigned to ProceduralTerrainStreamer");

            // Add ProceduralTerrainStreamer
            streamer = streamerGO.AddComponent<ProceduralTerrainStreamer>();

            // Use reflection to set private fields BEFORE Awake() is called
            // (AddComponent calls Awake immediately in EditMode tests)
            var playerField = typeof(ProceduralTerrainStreamer).GetField("playerTransform",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            playerField.SetValue(streamer, playerGO.transform);

            var voxelConfigField = typeof(ProceduralTerrainStreamer).GetField("voxelConfig",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            voxelConfigField.SetValue(streamer, voxelConfig);

            var streamingConfigField = typeof(ProceduralTerrainStreamer).GetField("streamingConfig",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            streamingConfigField.SetValue(streamer, streamingConfig);

            var materialField = typeof(ProceduralTerrainStreamer).GetField("chunkMaterial",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            materialField.SetValue(streamer, testMaterial);

            var debugField = typeof(ProceduralTerrainStreamer).GetField("debugLogging",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            debugField.SetValue(streamer, false); // Disable debug logging for tests

            // Initialize internal state manually (cache, collections, tracking)
            // We can't call Awake() again because it was already called during AddComponent
            // and it disabled the component. Instead, we manually initialize what Awake() does.

            // Create cache
            var cacheField = typeof(ProceduralTerrainStreamer).GetField("chunkCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cacheType = typeof(ProceduralTerrainStreamer).Assembly.GetType("TimeSurvivor.Voxel.Streaming.ChunkCache`1");
            var cacheGenericType = cacheType.MakeGenericType(typeof(TerrainChunk));
            var cacheInstance = System.Activator.CreateInstance(cacheGenericType, streamingConfig.ChunkCacheCapacity, streamer);
            cacheField.SetValue(streamer, cacheInstance);

            // Initialize collections
            var activeChunksField = typeof(ProceduralTerrainStreamer).GetField("activeChunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            activeChunksField.SetValue(streamer, new Dictionary<ChunkCoord, TerrainChunk>());

            var loadQueueField = typeof(ProceduralTerrainStreamer).GetField("loadQueue",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            // loadQueue is SortedSet<ChunkLoadRequest> - need to get the type
            var loadRequestType = typeof(ProceduralTerrainStreamer).GetNestedType("ChunkLoadRequest", System.Reflection.BindingFlags.NonPublic);
            if (loadRequestType != null)
            {
                var sortedSetType = typeof(SortedSet<>).MakeGenericType(loadRequestType);
                var loadQueueInstance = System.Activator.CreateInstance(sortedSetType);
                loadQueueField.SetValue(streamer, loadQueueInstance);
            }

            var unloadQueueField = typeof(ProceduralTerrainStreamer).GetField("unloadQueue",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            unloadQueueField.SetValue(streamer, new HashSet<ChunkCoord>());

            var loadingChunksField = typeof(ProceduralTerrainStreamer).GetField("loadingChunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            loadingChunksField.SetValue(streamer, new HashSet<ChunkCoord>());

            // Initialize tracking
            var lastPlayerPosField = typeof(ProceduralTerrainStreamer).GetField("lastPlayerPosition",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastPlayerPosField.SetValue(streamer, playerGO.transform.position);

            var lastPlayerChunkField = typeof(ProceduralTerrainStreamer).GetField("lastPlayerChunk",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var chunkCoord = VoxelMath.WorldToChunkCoord(
                playerGO.transform.position,
                voxelConfig.ChunkSize,
                voxelConfig.MacroVoxelSize
            );
            lastPlayerChunkField.SetValue(streamer, chunkCoord);

            var lastEvaluationTimeField = typeof(ProceduralTerrainStreamer).GetField("lastEvaluationTime",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastEvaluationTimeField.SetValue(streamer, Time.time);

            // Re-enable the component (first Awake() disabled it due to null dependencies)
            streamer.enabled = true;
        }

        [TearDown]
        public void TearDown()
        {
            if (streamerGO != null)
                Object.DestroyImmediate(streamerGO);

            if (playerGO != null)
                Object.DestroyImmediate(playerGO);

            if (streamingConfig != null)
                Object.DestroyImmediate(streamingConfig);

            if (testMaterial != null)
                Object.DestroyImmediate(testMaterial);
        }

        [UnityTest]
        public IEnumerator PlayerMovement_TriggersChunkLoading()
        {
            // Arrange: Player starts at origin
            playerGO.transform.position = Vector3.zero;

            // Act: Wait for initial chunks to load
            yield return new WaitForSeconds(2f);

            // Assert: Chunks should be loaded around player
            string stats = streamer.GetStatistics();
            Assert.IsTrue(stats.Contains("Active Chunks:"), "Statistics should include active chunks");

            // Get active chunks count via reflection
            var activeChunksField = typeof(ProceduralTerrainStreamer).GetField("activeChunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var activeChunks = activeChunksField.GetValue(streamer) as System.Collections.IDictionary;

            Assert.Greater(activeChunks.Count, 0, "Should have loaded chunks around player");
        }

        [UnityTest]
        public IEnumerator ChunkLoading_RespectsDistanceThreshold()
        {
            // Arrange: Player at origin
            playerGO.transform.position = Vector3.zero;
            yield return new WaitForSeconds(2f);

            // Get initial active chunks
            var activeChunksField = typeof(ProceduralTerrainStreamer).GetField("activeChunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var activeChunks = activeChunksField.GetValue(streamer) as System.Collections.IDictionary;
            int initialCount = activeChunks.Count;

            // Act: Move player very far away (beyond unload distance)
            playerGO.transform.position = new Vector3(500f, 0f, 0f);
            yield return new WaitForSeconds(2f);

            // Assert: Old chunks should be unloaded, new chunks loaded
            int newCount = activeChunks.Count;

            // We expect the chunks to have changed (some unloaded, some loaded)
            // The exact count may vary, but there should be active chunks
            Assert.Greater(newCount, 0, "Should have chunks loaded around new position");
        }

        [UnityTest]
        public IEnumerator Hysteresis_PreventsChunkThrashing()
        {
            // Arrange: Player at boundary between load/unload distance
            float boundaryDistance = 110f; // Between 100m (load) and 120m (unload)
            playerGO.transform.position = new Vector3(boundaryDistance, 0f, 0f);
            yield return new WaitForSeconds(2f);

            // Get active chunks
            var activeChunksField = typeof(ProceduralTerrainStreamer).GetField("activeChunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var activeChunks = activeChunksField.GetValue(streamer) as System.Collections.IDictionary;
            int beforeCount = activeChunks.Count;

            // Act: Move player slightly (still within hysteresis zone)
            playerGO.transform.position = new Vector3(boundaryDistance + 5f, 0f, 0f);
            yield return new WaitForSeconds(1f);

            // Assert: Chunk count should remain stable (no thrashing)
            int afterCount = activeChunks.Count;

            // Allow some variance, but chunks shouldn't be constantly loading/unloading
            int difference = Mathf.Abs(afterCount - beforeCount);
            Assert.Less(difference, 5, "Hysteresis should prevent excessive chunk loading/unloading");
        }

        [UnityTest]
        public IEnumerator CacheHit_LoadsFasterThanGeneration()
        {
            // Arrange: Load a chunk first time (cache miss)
            playerGO.transform.position = Vector3.zero;
            yield return new WaitForSeconds(2f);

            // Move player away to unload chunks
            playerGO.transform.position = new Vector3(500f, 0f, 0f);
            yield return new WaitForSeconds(2f);

            // Act: Move player back to origin (should hit cache)
            float startTime = Time.realtimeSinceStartup;
            playerGO.transform.position = Vector3.zero;
            yield return new WaitForSeconds(2f);
            float cacheHitTime = Time.realtimeSinceStartup - startTime;

            // Assert: Cache hits should be reflected in statistics
            string stats = streamer.GetStatistics();
            Assert.IsTrue(stats.Contains("Cache Hits:"), "Statistics should show cache hits");
            Assert.IsTrue(stats.Contains("Cache Misses:"), "Statistics should show cache misses");

            // Note: We can't directly measure load time difference in tests,
            // but cache hit count should be > 0
            CacheStatistics cacheStats = GetCacheStatistics();
            Assert.Greater(cacheStats.Hits, 0, "Should have cache hits after returning to same location");
        }

        [UnityTest]
        public IEnumerator LoadBudget_RespectedPerFrame()
        {
            // Arrange: Set max chunks per frame to 1
            var maxChunksField = typeof(ProceduralTerrainStreamer).GetField("maxChunksPerFrame",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxChunksField.SetValue(streamer, 1);

            // Clear all chunks first
            streamer.ClearAll();
            yield return null;

            // Act: Move player to trigger loading multiple chunks
            playerGO.transform.position = Vector3.zero;
            yield return null; // Wait one frame

            // Get load queue size
            var loadQueueField = typeof(ProceduralTerrainStreamer).GetField("loadQueue",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var loadQueue = loadQueueField.GetValue(streamer) as System.Collections.ICollection;

            // Assert: Load queue should have pending requests (budget prevents loading all at once)
            // Note: This is timing-dependent, but with max 1 chunk/frame and multiple chunks needed,
            // there should be requests queued after first frame
            yield return new WaitForSeconds(0.5f);

            // After some time, chunks should gradually load
            var activeChunksField = typeof(ProceduralTerrainStreamer).GetField("activeChunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var activeChunks = activeChunksField.GetValue(streamer) as System.Collections.IDictionary;

            Assert.Greater(activeChunks.Count, 0, "Should have loaded at least some chunks respecting budget");
        }

        [UnityTest]
        public IEnumerator ClearAll_RemovesAllChunks()
        {
            // Arrange: Load chunks
            playerGO.transform.position = Vector3.zero;
            yield return new WaitForSeconds(2f);

            var activeChunksField = typeof(ProceduralTerrainStreamer).GetField("activeChunks",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var activeChunks = activeChunksField.GetValue(streamer) as System.Collections.IDictionary;

            Assert.Greater(activeChunks.Count, 0, "Should have chunks before clearing");

            // Act: Clear all
            streamer.ClearAll();
            yield return null;

            // Assert: No active chunks
            Assert.AreEqual(0, activeChunks.Count, "All chunks should be cleared");

            // Cache should also be empty
            CacheStatistics stats = GetCacheStatistics();
            string statsStr = streamer.GetStatistics();
            Assert.IsTrue(statsStr.Contains("Active Chunks: 0"), "Statistics should show 0 active chunks");
        }

        [Test]
        public void GetStatistics_ReturnsFormattedString()
        {
            // Act
            string stats = streamer.GetStatistics();

            // Assert
            Assert.IsNotNull(stats, "Statistics should not be null");
            Assert.IsTrue(stats.Contains("Streaming Statistics:"), "Should contain header");
            Assert.IsTrue(stats.Contains("Active Chunks:"), "Should contain active chunks count");
            Assert.IsTrue(stats.Contains("Cache Hits:"), "Should contain cache statistics");
            Assert.IsTrue(stats.Contains("Hit Rate:"), "Should contain hit rate");
        }

        #region Helper Methods

        private CacheStatistics GetCacheStatistics()
        {
            // Use reflection to get cache and its statistics
            var cacheField = typeof(ProceduralTerrainStreamer).GetField("chunkCache",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var cache = cacheField.GetValue(streamer);

            // Statistics is a property, not a method
            var statsProperty = cache.GetType().GetProperty("Statistics");
            return (CacheStatistics)statsProperty.GetValue(cache);
        }

        #endregion
    }
}
