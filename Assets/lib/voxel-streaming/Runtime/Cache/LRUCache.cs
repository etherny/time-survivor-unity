using System.Collections.Generic;
using Unity.Profiling;

namespace TimeSurvivor.Voxel.Streaming
{
    /// <summary>
    /// Thread-safe Least Recently Used (LRU) cache with O(1) operations.
    /// When cache reaches capacity, automatically evicts the least recently accessed item.
    /// Includes performance profiling, statistics tracking, and eviction callbacks.
    /// </summary>
    /// <typeparam name="TKey">Key type (must have proper Equals/GetHashCode implementation)</typeparam>
    /// <typeparam name="TValue">Value type to cache</typeparam>
    public class LRUCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly object _lock = new object();
        private readonly IEvictionHandler<TKey, TValue> _evictionHandler;
        private CacheStatistics _statistics;

        // Profiler markers for performance monitoring
        private static readonly ProfilerMarker s_getMarker = new ProfilerMarker("LRUCache.Get");
        private static readonly ProfilerMarker s_putMarker = new ProfilerMarker("LRUCache.Put");
        private static readonly ProfilerMarker s_evictMarker = new ProfilerMarker("LRUCache.Evict");

        /// <summary>
        /// Creates a new LRU cache with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of items the cache can hold</param>
        /// <param name="evictionHandler">Optional callback handler for eviction events</param>
        public LRUCache(int capacity, IEvictionHandler<TKey, TValue> evictionHandler = null)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
            _evictionHandler = evictionHandler;
            _statistics = new CacheStatistics();
        }

        /// <summary>
        /// Attempts to retrieve a value from the cache.
        /// If found, marks the item as recently used and returns true.
        /// Thread-safe operation with O(1) time complexity.
        /// </summary>
        /// <param name="key">The key to lookup</param>
        /// <param name="value">The retrieved value if found</param>
        /// <returns>True if the key was found in cache, false otherwise</returns>
        public bool TryGet(TKey key, out TValue value)
        {
            using (s_getMarker.Auto())
            {
                lock (_lock)
                {
                    if (_cache.TryGetValue(key, out var node))
                    {
                        // Move to front (most recently used)
                        _lruList.Remove(node);
                        _lruList.AddFirst(node);

                        value = node.Value.Value;
                        _statistics.Hits++;
                        return true;
                    }

                    value = default;
                    _statistics.Misses++;
                    return false;
                }
            }
        }

        /// <summary>
        /// Adds or updates a value in the cache.
        /// If the cache is at capacity, evicts the least recently used item.
        /// If an eviction occurs, calls the eviction handler (if set) before removing the item.
        /// Thread-safe operation with O(1) time complexity.
        /// </summary>
        /// <param name="key">The key to add/update</param>
        /// <param name="value">The value to cache</param>
        /// <returns>The evicted value if an eviction occurred, default(TValue) otherwise</returns>
        public TValue Put(TKey key, TValue value)
        {
            using (s_putMarker.Auto())
            {
                lock (_lock)
                {
                    TValue evictedValue = default;

                    // If key exists, update it
                    if (_cache.TryGetValue(key, out var existingNode))
                    {
                        _lruList.Remove(existingNode);
                        _cache.Remove(key);
                    }
                    // If cache is full, evict LRU item
                    else if (_cache.Count >= _capacity)
                    {
                        evictedValue = EvictLRU();
                    }

                    // Add new item to front (most recently used)
                    var newItem = new CacheItem { Key = key, Value = value };
                    var newNode = new LinkedListNode<CacheItem>(newItem);

                    _lruList.AddFirst(newNode);
                    _cache[key] = newNode;

                    return evictedValue;
                }
            }
        }

        /// <summary>
        /// Removes the least recently used item from the cache.
        /// Calls eviction handler before removal if configured.
        /// Thread-safe operation with O(1) time complexity.
        /// </summary>
        /// <returns>The evicted value</returns>
        private TValue EvictLRU()
        {
            using (s_evictMarker.Auto())
            {
                var lruNode = _lruList.Last;
                var evictedKey = lruNode.Value.Key;
                var evictedValue = lruNode.Value.Value;

                // Call eviction handler before removing
                _evictionHandler?.OnEvict(evictedKey, evictedValue);

                _lruList.RemoveLast();
                _cache.Remove(evictedKey);
                _statistics.Evictions++;

                return evictedValue;
            }
        }

        /// <summary>
        /// Checks if a key exists in the cache without marking it as recently used.
        /// Thread-safe operation with O(1) time complexity.
        /// </summary>
        /// <param name="key">The key to check</param>
        /// <returns>True if the key exists in cache</returns>
        public bool Contains(TKey key)
        {
            lock (_lock)
            {
                return _cache.ContainsKey(key);
            }
        }

        /// <summary>
        /// Removes a specific item from the cache.
        /// Thread-safe operation with O(1) time complexity.
        /// </summary>
        /// <param name="key">The key to remove</param>
        /// <returns>True if the item was found and removed</returns>
        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (_cache.TryGetValue(key, out var node))
                {
                    _lruList.Remove(node);
                    _cache.Remove(key);
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Removes all items from the cache.
        /// Thread-safe operation.
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _lruList.Clear();
            }
        }

        /// <summary>
        /// Gets the current number of items in the cache.
        /// Thread-safe operation.
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _cache.Count;
                }
            }
        }

        /// <summary>
        /// Gets the maximum capacity of the cache.
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Gets a copy of the current cache statistics.
        /// Thread-safe operation.
        /// </summary>
        public CacheStatistics Statistics
        {
            get
            {
                lock (_lock)
                {
                    return _statistics;
                }
            }
        }

        /// <summary>
        /// Resets the cache statistics to zero.
        /// Thread-safe operation.
        /// </summary>
        public void ResetStatistics()
        {
            lock (_lock)
            {
                _statistics.Reset();
            }
        }

        /// <summary>
        /// Gets all keys currently in the cache.
        /// Returns a snapshot of keys at the time of the call.
        /// Thread-safe operation.
        /// </summary>
        public IEnumerable<TKey> Keys
        {
            get
            {
                lock (_lock)
                {
                    // Return a copy to avoid threading issues
                    return new List<TKey>(_cache.Keys);
                }
            }
        }

        /// <summary>
        /// Gets all values currently in the cache, ordered by recency (most recent first).
        /// Returns a snapshot of values at the time of the call.
        /// Thread-safe operation.
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                lock (_lock)
                {
                    var values = new List<TValue>(_lruList.Count);
                    foreach (var node in _lruList)
                    {
                        values.Add(node.Value);
                    }
                    return values;
                }
            }
        }

        /// <summary>
        /// Gets all keys currently in the cache, ordered by recency (most recent first).
        /// Returns a snapshot of keys at the time of the call.
        /// Thread-safe operation. Useful for cache visualization and monitoring.
        /// </summary>
        public IEnumerable<TKey> OrderedKeys
        {
            get
            {
                lock (_lock)
                {
                    var keys = new List<TKey>(_lruList.Count);
                    foreach (var node in _lruList)
                    {
                        keys.Add(node.Key);
                    }
                    return keys;
                }
            }
        }

        /// <summary>
        /// Internal cache item structure.
        /// </summary>
        private struct CacheItem
        {
            public TKey Key;
            public TValue Value;
        }
    }
}
