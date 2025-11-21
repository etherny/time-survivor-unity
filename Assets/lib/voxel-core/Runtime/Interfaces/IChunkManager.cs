using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Core
{
    /// <summary>
    /// Interface for chunk management systems.
    /// Handles loading, unloading, and lifecycle management of chunks.
    /// </summary>
    /// <typeparam name="TChunk">Type of chunk managed by this system (must be reference type)</typeparam>
    public interface IChunkManager<TChunk> where TChunk : class
    {
        /// <summary>
        /// Load a chunk at the specified coordinate.
        /// If chunk is already loaded, this is a no-op.
        /// </summary>
        /// <param name="coord">Chunk coordinate to load</param>
        void LoadChunk(ChunkCoord coord);

        /// <summary>
        /// Unload a chunk at the specified coordinate.
        /// Disposes of allocated resources and removes from memory.
        /// </summary>
        /// <param name="coord">Chunk coordinate to unload</param>
        void UnloadChunk(ChunkCoord coord);

        /// <summary>
        /// Check if a chunk is currently loaded in memory.
        /// </summary>
        /// <param name="coord">Chunk coordinate to check</param>
        /// <returns>True if chunk is loaded, false otherwise</returns>
        bool IsChunkLoaded(ChunkCoord coord);

        /// <summary>
        /// Mark a chunk as dirty (needs remeshing).
        /// Used when voxel data changes and mesh needs to be regenerated.
        /// </summary>
        /// <param name="coord">Chunk coordinate to mark dirty</param>
        void MarkDirty(ChunkCoord coord);

        /// <summary>
        /// Get the chunk at the specified coordinate.
        /// Returns null if chunk is not loaded.
        /// </summary>
        /// <param name="coord">Chunk coordinate to retrieve</param>
        /// <returns>Chunk object or null if not loaded</returns>
        TChunk GetChunk(ChunkCoord coord);
    }
}
