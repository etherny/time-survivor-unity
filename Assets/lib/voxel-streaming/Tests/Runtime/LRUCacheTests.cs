using NUnit.Framework;
using System.Threading.Tasks;
using TimeSurvivor.Voxel.Core;
using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Streaming.Tests
{
    /// <summary>
    /// Comprehensive test suite for LRUCache implementation.
    /// Tests cover O(1) performance, thread safety, statistics, and eviction handling.
    /// </summary>
    [TestFixture]
    public class LRUCacheTests
    {
        private class TestEvictionHandler : IEvictionHandler<int, string>
        {
            public int EvictionCount { get; private set; }
            public int LastEvictedKey { get; private set; }
            public string LastEvictedValue { get; private set; }

            public void OnEvict(int key, string value)
            {
                EvictionCount++;
                LastEvictedKey = key;
                LastEvictedValue = value;
            }

            public void Reset()
            {
                EvictionCount = 0;
                LastEvictedKey = 0;
                LastEvictedValue = null;
            }
        }

        /// <summary>
        /// Test 1: Verify cache capacity enforcement and LRU eviction behavior.
        /// When cache is full, least recently used item should be evicted.
        /// </summary>
        [Test]
        public void LRUCache_WhenAtCapacity_EvictsLeastRecentlyUsedItem()
        {
            // Arrange
            var cache = new LRUCache<int, string>(3);

            // Act
            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            // Access key 1 to make it recently used
            cache.TryGet(1, out _);

            // Add key 4, should evict key 2 (least recently used)
            cache.Put(4, "four");

            // Assert
            Assert.AreEqual(3, cache.Count);
            Assert.IsTrue(cache.Contains(1), "Key 1 should still be in cache (recently accessed)");
            Assert.IsFalse(cache.Contains(2), "Key 2 should be evicted (least recently used)");
            Assert.IsTrue(cache.Contains(3), "Key 3 should still be in cache");
            Assert.IsTrue(cache.Contains(4), "Key 4 should be in cache (just added)");
        }

        /// <summary>
        /// Test 2: Verify O(1) time complexity for all operations.
        /// Get, Put, and Contains operations should complete in constant time.
        /// </summary>
        [Test]
        public void LRUCache_Operations_CompleteInConstantTime()
        {
            // Arrange
            var cache = new LRUCache<int, string>(1000);

            // Populate cache with 1000 items
            for (int i = 0; i < 1000; i++)
            {
                cache.Put(i, $"value_{i}");
            }

            // Act & Assert - measure time for operations
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Get operation
            cache.TryGet(500, out _);
            watch.Stop();
            Assert.Less(watch.Elapsed.TotalMilliseconds, 0.05, "Get operation should be < 0.05ms (O(1))");

            // Put operation
            watch.Restart();
            cache.Put(1001, "new_value");
            watch.Stop();
            Assert.Less(watch.Elapsed.TotalMilliseconds, 0.1, "Put operation should be < 0.1ms (O(1))");

            // Contains operation
            watch.Restart();
            cache.Contains(750);
            watch.Stop();
            Assert.Less(watch.Elapsed.TotalMilliseconds, 0.05, "Contains operation should be < 0.05ms (O(1))");
        }

        /// <summary>
        /// Test 3: Verify cache hit rate meets performance targets (≥80%).
        /// Simulates realistic access patterns with temporal locality.
        /// </summary>
        [Test]
        public void LRUCache_WithRealisticAccessPattern_AchievesTargetHitRate()
        {
            // Arrange
            var cache = new LRUCache<int, string>(50); // Cache can hold 50 items
            int totalAccesses = 1000;

            // Act - Simulate realistic access pattern with temporal locality
            // 80% of accesses target a "hot set" of 40 items
            // 20% of accesses target a "cold set" of 100 items
            var random = new System.Random(42); // Fixed seed for reproducibility

            for (int i = 0; i < totalAccesses; i++)
            {
                int key;
                if (random.NextDouble() < 0.8) // 80% hot set
                {
                    key = random.Next(0, 40);
                }
                else // 20% cold set
                {
                    key = random.Next(0, 100);
                }

                if (!cache.TryGet(key, out _))
                {
                    cache.Put(key, $"value_{key}");
                }
            }

            // Assert
            var stats = cache.Statistics;
            Assert.GreaterOrEqual(stats.HitRate, 0.8f, $"Hit rate should be ≥80%, got {stats.HitRate:P2}");
            Assert.AreEqual(totalAccesses, stats.TotalAccesses, "Total accesses should match");
        }

        /// <summary>
        /// Test 4: Verify thread safety of cache operations.
        /// Multiple threads should be able to access cache concurrently without errors.
        /// </summary>
        [Test]
        public void LRUCache_ConcurrentAccess_IsThreadSafe()
        {
            // Arrange
            var cache = new LRUCache<int, string>(100);
            const int numThreads = 10;
            const int operationsPerThread = 100;
            var tasks = new Task[numThreads];

            // Act - Create multiple threads performing concurrent operations
            for (int t = 0; t < numThreads; t++)
            {
                int threadId = t;
                tasks[t] = Task.Run(() =>
                {
                    for (int i = 0; i < operationsPerThread; i++)
                    {
                        int key = threadId * 100 + i;

                        // Mix of operations
                        cache.Put(key, $"thread_{threadId}_value_{i}");
                        cache.TryGet(key, out _);
                        cache.Contains(key);

                        if (i % 10 == 0)
                        {
                            cache.Remove(key);
                        }
                    }
                });
            }

            // Wait for all threads to complete
            Task.WaitAll(tasks);

            // Assert - No exceptions thrown, cache is still functional
            Assert.DoesNotThrow(() => cache.Put(9999, "test"));
            Assert.DoesNotThrow(() => cache.TryGet(9999, out _));
            Assert.LessOrEqual(cache.Count, 100, "Cache should respect capacity even with concurrent access");
        }

        /// <summary>
        /// Test 5: Verify eviction handler is called correctly when items are evicted.
        /// Handler should receive correct key-value pairs for evicted items.
        /// </summary>
        [Test]
        public void LRUCache_EvictionHandler_IsCalledOnEviction()
        {
            // Arrange
            var evictionHandler = new TestEvictionHandler();
            var cache = new LRUCache<int, string>(3, evictionHandler);

            // Act
            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            // Assert - No evictions yet
            Assert.AreEqual(0, evictionHandler.EvictionCount);

            // Act - Add fourth item, should trigger eviction
            cache.Put(4, "four");

            // Assert
            Assert.AreEqual(1, evictionHandler.EvictionCount, "Eviction handler should be called once");
            Assert.AreEqual(1, evictionHandler.LastEvictedKey, "Key 1 should be evicted (LRU)");
            Assert.AreEqual("one", evictionHandler.LastEvictedValue, "Value 'one' should be evicted");

            // Act - Add more items
            cache.Put(5, "five");
            cache.Put(6, "six");

            // Assert
            Assert.AreEqual(3, evictionHandler.EvictionCount, "Eviction handler should be called three times total");
        }

        /// <summary>
        /// Test 6: Verify cache statistics are tracked correctly.
        /// Statistics should accurately reflect hits, misses, and evictions.
        /// </summary>
        [Test]
        public void LRUCache_Statistics_TrackCorrectly()
        {
            // Arrange
            var cache = new LRUCache<int, string>(3);

            // Act - Populate cache
            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            // Act - Mix of hits and misses
            cache.TryGet(1, out _); // Hit
            cache.TryGet(2, out _); // Hit
            cache.TryGet(999, out _); // Miss
            cache.TryGet(998, out _); // Miss

            // Act - Trigger eviction
            cache.Put(4, "four"); // Evicts key 3

            // Assert
            var stats = cache.Statistics;
            Assert.AreEqual(2, stats.Hits, "Should have 2 cache hits");
            Assert.AreEqual(2, stats.Misses, "Should have 2 cache misses");
            Assert.AreEqual(1, stats.Evictions, "Should have 1 eviction");
            Assert.AreEqual(0.5f, stats.HitRate, "Hit rate should be 50% (2 hits / 4 total)");
            Assert.AreEqual(4, stats.TotalAccesses, "Total accesses should be 4");
        }

        /// <summary>
        /// Test 7: Verify statistics reset functionality.
        /// Reset should clear all counters to zero.
        /// </summary>
        [Test]
        public void LRUCache_ResetStatistics_ClearsAllCounters()
        {
            // Arrange
            var cache = new LRUCache<int, string>(2);
            cache.Put(1, "one");
            cache.TryGet(1, out _); // Hit
            cache.TryGet(999, out _); // Miss
            cache.Put(2, "two");
            cache.Put(3, "three"); // Eviction

            // Verify statistics are non-zero
            var statsBeforeReset = cache.Statistics;
            Assert.Greater(statsBeforeReset.Hits, 0);
            Assert.Greater(statsBeforeReset.Misses, 0);
            Assert.Greater(statsBeforeReset.Evictions, 0);

            // Act
            cache.ResetStatistics();

            // Assert
            var statsAfterReset = cache.Statistics;
            Assert.AreEqual(0, statsAfterReset.Hits);
            Assert.AreEqual(0, statsAfterReset.Misses);
            Assert.AreEqual(0, statsAfterReset.Evictions);
            Assert.AreEqual(0f, statsAfterReset.HitRate);
        }

        /// <summary>
        /// Test 8: Verify ChunkCache wrapper works correctly with ChunkCoord keys.
        /// Tests domain-specific API for chunk management.
        /// </summary>
        [Test]
        public void ChunkCache_WithChunkCoord_WorksCorrectly()
        {
            // Arrange
            var cache = new ChunkCache<string>(3);
            var coord1 = new ChunkCoord(0, 0, 0);
            var coord2 = new ChunkCoord(1, 0, 0);
            var coord3 = new ChunkCoord(0, 1, 0);
            var coord4 = new ChunkCoord(0, 0, 1);

            // Act
            cache.PutChunk(coord1, "chunk_1");
            cache.PutChunk(coord2, "chunk_2");
            cache.PutChunk(coord3, "chunk_3");

            // Assert
            Assert.IsTrue(cache.TryGetChunk(coord1, out var chunk1));
            Assert.AreEqual("chunk_1", chunk1);
            Assert.IsTrue(cache.ContainsChunk(coord2));
            Assert.AreEqual(3, cache.Count);

            // Act - Add fourth chunk, should evict LRU
            cache.PutChunk(coord4, "chunk_4");

            // Assert
            Assert.AreEqual(3, cache.Count);
            Assert.IsFalse(cache.ContainsChunk(coord2), "Chunk at coord2 should be evicted (LRU)");
        }

        /// <summary>
        /// Test 9: Verify Remove operation works correctly.
        /// </summary>
        [Test]
        public void LRUCache_Remove_RemovesItemCorrectly()
        {
            // Arrange
            var cache = new LRUCache<int, string>(5);
            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            // Act
            bool removed = cache.Remove(2);

            // Assert
            Assert.IsTrue(removed, "Remove should return true for existing key");
            Assert.AreEqual(2, cache.Count);
            Assert.IsFalse(cache.Contains(2));
            Assert.IsTrue(cache.Contains(1));
            Assert.IsTrue(cache.Contains(3));

            // Act - Try to remove non-existent key
            bool removedNonExistent = cache.Remove(999);

            // Assert
            Assert.IsFalse(removedNonExistent, "Remove should return false for non-existent key");
            Assert.AreEqual(2, cache.Count);
        }

        /// <summary>
        /// Test 10: Verify Clear operation removes all items.
        /// </summary>
        [Test]
        public void LRUCache_Clear_RemovesAllItems()
        {
            // Arrange
            var cache = new LRUCache<int, string>(5);
            cache.Put(1, "one");
            cache.Put(2, "two");
            cache.Put(3, "three");

            Assert.AreEqual(3, cache.Count);

            // Act
            cache.Clear();

            // Assert
            Assert.AreEqual(0, cache.Count);
            Assert.IsFalse(cache.Contains(1));
            Assert.IsFalse(cache.Contains(2));
            Assert.IsFalse(cache.Contains(3));
        }
    }
}
