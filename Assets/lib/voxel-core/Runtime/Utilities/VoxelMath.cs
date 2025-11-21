using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Core
{
    /// <summary>
    /// Mathematical utilities for voxel and chunk coordinate conversions.
    /// All methods are static for use in Unity Jobs with Burst compilation.
    /// </summary>
    public static class VoxelMath
    {
        /// <summary>
        /// Convert world position to chunk coordinate.
        /// </summary>
        /// <param name="worldPosition">Position in Unity world space</param>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <param name="voxelSize">Size of a single voxel in Unity units</param>
        /// <returns>Chunk coordinate containing this world position</returns>
        public static ChunkCoord WorldToChunkCoord(float3 worldPosition, int chunkSize, float voxelSize)
        {
            float chunkWorldSize = chunkSize * voxelSize;
            int3 chunkCoord = (int3)math.floor(worldPosition / chunkWorldSize);
            return new ChunkCoord(chunkCoord);
        }

        /// <summary>
        /// Convert world position to voxel coordinate (global voxel grid).
        /// </summary>
        /// <param name="worldPosition">Position in Unity world space</param>
        /// <param name="voxelSize">Size of a single voxel in Unity units</param>
        /// <returns>Global voxel coordinate</returns>
        public static int3 WorldToVoxelCoord(float3 worldPosition, float voxelSize)
        {
            return (int3)math.floor(worldPosition / voxelSize);
        }

        /// <summary>
        /// Convert chunk coordinate to world position (bottom-left corner of chunk).
        /// </summary>
        /// <param name="chunkCoord">Chunk coordinate</param>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <param name="voxelSize">Size of a single voxel in Unity units</param>
        /// <returns>World position of chunk origin</returns>
        public static float3 ChunkCoordToWorld(ChunkCoord chunkCoord, int chunkSize, float voxelSize)
        {
            return (float3)chunkCoord.Value * (chunkSize * voxelSize);
        }

        /// <summary>
        /// Convert voxel coordinate to world position (center of voxel).
        /// </summary>
        /// <param name="voxelCoord">Global voxel coordinate</param>
        /// <param name="voxelSize">Size of a single voxel in Unity units</param>
        /// <returns>World position of voxel center</returns>
        public static float3 VoxelCoordToWorld(int3 voxelCoord, float voxelSize)
        {
            return ((float3)voxelCoord + 0.5f) * voxelSize;
        }

        /// <summary>
        /// Convert global voxel coordinate to local chunk-relative coordinate.
        /// </summary>
        /// <param name="voxelCoord">Global voxel coordinate</param>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <returns>Local coordinate within chunk (0 to chunkSize-1)</returns>
        public static int3 VoxelToLocalCoord(int3 voxelCoord, int chunkSize)
        {
            // Use modulo for positive wrapping, handle negative coordinates
            int3 local = voxelCoord % chunkSize;
            return math.select(local, local + chunkSize, local < 0);
        }

        /// <summary>
        /// Flatten 3D voxel coordinate to 1D array index.
        /// Uses XYZ ordering (Z changes fastest).
        /// </summary>
        /// <param name="x">X coordinate (0 to chunkSize-1)</param>
        /// <param name="y">Y coordinate (0 to chunkSize-1)</param>
        /// <param name="z">Z coordinate (0 to chunkSize-1)</param>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <returns>Flat array index</returns>
        public static int Flatten3DIndex(int x, int y, int z, int chunkSize)
        {
            return x + y * chunkSize + z * chunkSize * chunkSize;
        }

        /// <summary>
        /// Unflatten 1D array index to 3D voxel coordinate.
        /// </summary>
        /// <param name="index">Flat array index</param>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <returns>3D voxel coordinate</returns>
        public static int3 Unflatten3DIndex(int index, int chunkSize)
        {
            int z = index / (chunkSize * chunkSize);
            int remainder = index % (chunkSize * chunkSize);
            int y = remainder / chunkSize;
            int x = remainder % chunkSize;
            return new int3(x, y, z);
        }

        /// <summary>
        /// Check if a local coordinate is within chunk bounds.
        /// </summary>
        /// <param name="localCoord">Local coordinate to check</param>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <returns>True if coordinate is valid within chunk</returns>
        public static bool IsValidLocalCoord(int3 localCoord, int chunkSize)
        {
            return math.all(localCoord >= 0 & localCoord < chunkSize);
        }

        /// <summary>
        /// Calculate Manhattan distance between two chunk coordinates.
        /// Useful for chunk loading priority.
        /// </summary>
        /// <param name="a">First chunk coordinate</param>
        /// <param name="b">Second chunk coordinate</param>
        /// <returns>Manhattan distance (sum of absolute differences)</returns>
        public static int ChunkManhattanDistance(ChunkCoord a, ChunkCoord b)
        {
            int3 diff = math.abs(a.Value - b.Value);
            return diff.x + diff.y + diff.z;
        }

        /// <summary>
        /// Calculate squared Euclidean distance between two chunk coordinates.
        /// Avoids expensive sqrt, useful for distance comparisons.
        /// </summary>
        /// <param name="a">First chunk coordinate</param>
        /// <param name="b">Second chunk coordinate</param>
        /// <returns>Squared Euclidean distance</returns>
        public static int ChunkDistanceSquared(ChunkCoord a, ChunkCoord b)
        {
            int3 diff = a.Value - b.Value;
            return math.dot(diff, diff);
        }
    }
}
