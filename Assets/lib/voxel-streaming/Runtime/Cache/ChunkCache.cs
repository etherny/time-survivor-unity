using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Streaming
{
    /// <summary>
    /// Specialized LRU cache wrapper for managing terrain chunks.
    /// Provides a domain-specific API for chunk caching with ChunkCoord keys.
    /// Automatically handles chunk eviction and cleanup through the eviction handler.
    /// </summary>
    /// <typeparam name="TChunk">The chunk type to cache (e.g., TerrainChunk)</typeparam>
    public class ChunkCache<TChunk> where TChunk : class
    {
        private readonly LRUCache<ChunkCoord, TChunk> _cache;

        /// <summary>
        /// Creates a new chunk cache with the specified capacity.
        /// </summary>
        /// <param name="capacity">Maximum number of chunks to keep in memory</param>
        /// <param name="evictionHandler">Optional callback handler for chunk eviction events</param>
        public ChunkCache(int capacity, IEvictionHandler<ChunkCoord, TChunk> evictionHandler = null)
        {
            _cache = new LRUCache<ChunkCoord, TChunk>(capacity, evictionHandler);
        }

        /// <summary>
        /// Attempts to retrieve a chunk from the cache by its coordinates.
        /// If found, marks the chunk as recently used.
        /// </summary>
        /// <param name="coord">The chunk coordinate to lookup</param>
        /// <param name="chunk">The retrieved chunk if found</param>
        /// <returns>True if the chunk was found in cache, false otherwise</returns>
        public bool TryGetChunk(ChunkCoord coord, out TChunk chunk)
        {
            return _cache.TryGet(coord, out chunk);
        }

        /// <summary>
        /// Adds or updates a chunk in the cache.
        /// If the cache is at capacity, evicts the least recently used chunk.
        /// </summary>
        /// <param name="coord">The chunk coordinate</param>
        /// <param name="chunk">The chunk to cache</param>
        /// <returns>The evicted chunk if an eviction occurred, null otherwise</returns>
        public TChunk PutChunk(ChunkCoord coord, TChunk chunk)
        {
            return _cache.Put(coord, chunk);
        }

        /// <summary>
        /// Checks if a chunk exists in the cache without marking it as recently used.
        /// </summary>
        /// <param name="coord">The chunk coordinate to check</param>
        /// <returns>True if the chunk exists in cache</returns>
        public bool ContainsChunk(ChunkCoord coord)
        {
            return _cache.Contains(coord);
        }

        /// <summary>
        /// Removes a specific chunk from the cache.
        /// </summary>
        /// <param name="coord">The chunk coordinate to remove</param>
        /// <returns>True if the chunk was found and removed</returns>
        public bool RemoveChunk(ChunkCoord coord)
        {
            return _cache.Remove(coord);
        }

        /// <summary>
        /// Removes all chunks from the cache.
        /// </summary>
        public void Clear()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Gets the current number of chunks in the cache.
        /// </summary>
        public int Count => _cache.Count;

        /// <summary>
        /// Gets the maximum capacity of the cache.
        /// </summary>
        public int Capacity => _cache.Capacity;

        /// <summary>
        /// Gets a copy of the current cache statistics.
        /// </summary>
        public CacheStatistics Statistics => _cache.Statistics;

        /// <summary>
        /// Resets the cache statistics to zero.
        /// </summary>
        public void ResetStatistics()
        {
            _cache.ResetStatistics();
        }

        /// <summary>
        /// Gets all chunk coordinates currently in the cache, ordered by recency (most recent first).
        /// Returns a snapshot of coordinates at the time of the call.
        /// Useful for cache visualization and monitoring.
        /// </summary>
        public System.Collections.Generic.IEnumerable<ChunkCoord> OrderedKeys => _cache.OrderedKeys;
    }
}
