namespace TimeSurvivor.Voxel.Streaming
{
    /// <summary>
    /// Tracks cache performance statistics for monitoring and optimization.
    /// Provides metrics like hit rate, miss rate, and total operations.
    /// </summary>
    public struct CacheStatistics
    {
        /// <summary>
        /// Total number of successful cache hits (item found in cache).
        /// </summary>
        public int Hits;

        /// <summary>
        /// Total number of cache misses (item not found in cache).
        /// </summary>
        public int Misses;

        /// <summary>
        /// Total number of items evicted from the cache.
        /// </summary>
        public int Evictions;

        /// <summary>
        /// Gets the cache hit rate as a value between 0.0 and 1.0.
        /// Returns 0 if no operations have been performed yet.
        /// </summary>
        public float HitRate
        {
            get
            {
                int total = Hits + Misses;
                if (total == 0) return 0f;
                return (float)Hits / total;
            }
        }

        /// <summary>
        /// Gets the total number of cache access operations (hits + misses).
        /// </summary>
        public int TotalAccesses => Hits + Misses;

        /// <summary>
        /// Resets all statistics to zero.
        /// </summary>
        public void Reset()
        {
            Hits = 0;
            Misses = 0;
            Evictions = 0;
        }

        /// <summary>
        /// Returns a formatted string representation of the cache statistics.
        /// </summary>
        public override string ToString()
        {
            return $"CacheStatistics [Hits: {Hits}, Misses: {Misses}, Evictions: {Evictions}, Hit Rate: {HitRate:P2}]";
        }
    }
}
