using Unity.Collections;

namespace TimeSurvivor.Voxel.Core
{
    /// <summary>
    /// Interface for voxel data generation (procedural terrain, structures, etc.).
    /// Implementations should be thread-safe for use in Unity Jobs.
    /// </summary>
    public interface IVoxelGenerator
    {
        /// <summary>
        /// Generate voxel data for a chunk at the specified coordinate.
        /// </summary>
        /// <param name="coord">Chunk coordinate to generate</param>
        /// <param name="chunkSize">Size of the chunk (voxels per axis)</param>
        /// <param name="allocator">NativeArray allocator type</param>
        /// <returns>Flat array of voxel types (length = chunkSize³)</returns>
        NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator);

        /// <summary>
        /// Get a single voxel at a world position without generating a full chunk.
        /// Useful for raycasting and point queries.
        /// </summary>
        /// <param name="worldX">World X coordinate</param>
        /// <param name="worldY">World Y coordinate</param>
        /// <param name="worldZ">World Z coordinate</param>
        /// <returns>Voxel type at that position</returns>
        VoxelType GetVoxelAt(int worldX, int worldY, int worldZ);
    }
}
