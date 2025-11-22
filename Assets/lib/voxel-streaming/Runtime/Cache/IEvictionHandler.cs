namespace TimeSurvivor.Voxel.Streaming
{
    /// <summary>
    /// Interface for handling cache eviction callbacks.
    /// Implement this interface to perform cleanup or persistence when items are evicted from cache.
    /// </summary>
    /// <typeparam name="TKey">The type of cache keys</typeparam>
    /// <typeparam name="TValue">The type of cache values</typeparam>
    public interface IEvictionHandler<TKey, TValue>
    {
        /// <summary>
        /// Called when an item is about to be evicted from the cache.
        /// This is the last opportunity to save state, cleanup resources, or perform other operations
        /// before the item is removed from memory.
        /// </summary>
        /// <param name="key">The key of the item being evicted</param>
        /// <param name="value">The value of the item being evicted</param>
        void OnEvict(TKey key, TValue value);
    }
}
