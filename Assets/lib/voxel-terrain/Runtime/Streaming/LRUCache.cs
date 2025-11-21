using System.Collections.Generic;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Least Recently Used (LRU) cache for managing limited chunk memory.
    /// When cache is full, removes least recently accessed chunk.
    /// </summary>
    /// <typeparam name="TKey">Key type (typically ChunkCoord)</typeparam>
    /// <typeparam name="TValue">Value type (typically TerrainChunk)</typeparam>
    public class LRUCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly Dictionary<TKey, LinkedListNode<CacheItem>> _cache;
        private readonly LinkedList<CacheItem> _lruList;

        public LRUCache(int capacity)
        {
            _capacity = capacity;
            _cache = new Dictionary<TKey, LinkedListNode<CacheItem>>(capacity);
            _lruList = new LinkedList<CacheItem>();
        }

        /// <summary>
        /// Get value from cache if it exists.
        /// Marks the item as recently used.
        /// </summary>
        public bool TryGet(TKey key, out TValue value)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                // Move to front (most recently used)
                _lruList.Remove(node);
                _lruList.AddFirst(node);

                value = node.Value.Value;
                return true;
            }

            value = default;
            return false;
        }

        /// <summary>
        /// Add or update value in cache.
        /// If cache is full, removes least recently used item.
        /// </summary>
        public TValue Put(TKey key, TValue value)
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
                var lruNode = _lruList.Last;
                evictedValue = lruNode.Value.Value;

                _lruList.RemoveLast();
                _cache.Remove(lruNode.Value.Key);
            }

            // Add new item to front
            var newItem = new CacheItem { Key = key, Value = value };
            var newNode = new LinkedListNode<CacheItem>(newItem);

            _lruList.AddFirst(newNode);
            _cache[key] = newNode;

            return evictedValue;
        }

        /// <summary>
        /// Check if key exists in cache without marking as used.
        /// </summary>
        public bool Contains(TKey key)
        {
            return _cache.ContainsKey(key);
        }

        /// <summary>
        /// Remove item from cache.
        /// Returns true if item was found and removed.
        /// </summary>
        public bool Remove(TKey key)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                _lruList.Remove(node);
                _cache.Remove(key);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Clear all items from cache.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
            _lruList.Clear();
        }

        /// <summary>
        /// Get current number of items in cache.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Get cache capacity (max items).
        /// </summary>
        public int Capacity => _capacity;

        /// <summary>
        /// Get all keys currently in cache.
        /// </summary>
        public IEnumerable<TKey> Keys => _cache.Keys;

        /// <summary>
        /// Get all values currently in cache (ordered by recency).
        /// </summary>
        public IEnumerable<TValue> Values
        {
            get
            {
                foreach (var node in _lruList)
                {
                    yield return node.Value;
                }
            }
        }

        private struct CacheItem
        {
            public TKey Key;
            public TValue Value;
        }
    }
}
