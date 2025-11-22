using Unity.Collections;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain
{
    /// <summary>
    /// Generates flat terrain with a checkerboard pattern of Grass and Dirt voxels.
    /// Creates a simple 8-voxel high ground with alternating 8x8 tiles.
    /// </summary>
    public class FlatCheckerboardGenerator : IVoxelGenerator
    {
        private const int GROUND_HEIGHT = 8;
        private const int TILE_SIZE = 8; // Increased from 4 to make checkerboard pattern more visible
        private int chunkSize;

        /// <summary>
        /// Generates voxel data for a chunk with flat checkerboard terrain.
        /// </summary>
        /// <param name="coord">Chunk coordinate</param>
        /// <param name="chunkSize">Size of chunk (typically 64)</param>
        /// <param name="allocator">Memory allocator for NativeArray</param>
        /// <returns>NativeArray of VoxelTypes representing the chunk</returns>
        public NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator)
        {
            this.chunkSize = chunkSize;

            int totalVoxels = chunkSize * chunkSize * chunkSize;
            NativeArray<VoxelType> voxels = new NativeArray<VoxelType>(totalVoxels, allocator);

            // Generate flat terrain with checkerboard pattern
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        // Use VoxelMath's XYZ ordering: x + y*size + z*size*size
                        int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);

                        if (y < GROUND_HEIGHT)
                        {
                            // Below ground height: use checkerboard pattern
                            voxels[index] = CalculateCheckerboardPattern(x, z, coord.X, coord.Z);
                        }
                        else
                        {
                            // Above ground height: air
                            voxels[index] = VoxelType.Air;
                        }
                    }
                }
            }

            return voxels;
        }

        /// <summary>
        /// Get a single voxel at world position without generating a full chunk.
        /// Useful for raycasting and point queries.
        /// </summary>
        /// <param name="worldX">World X coordinate</param>
        /// <param name="worldY">World Y coordinate</param>
        /// <param name="worldZ">World Z coordinate</param>
        /// <returns>Voxel type at that position</returns>
        public VoxelType GetVoxelAt(int worldX, int worldY, int worldZ)
        {
            if (worldY < GROUND_HEIGHT)
            {
                // Below ground height: calculate checkerboard pattern
                bool isEvenTileX = ((worldX / TILE_SIZE) % 2) == 0;
                bool isEvenTileZ = ((worldZ / TILE_SIZE) % 2) == 0;
                return (isEvenTileX == isEvenTileZ) ? VoxelType.Grass : VoxelType.Dirt;
            }
            else
            {
                // Above ground height: air
                return VoxelType.Air;
            }
        }

        /// <summary>
        /// Calculates checkerboard pattern (Grass/Dirt) based on world coordinates.
        /// Uses 4x4 voxel tiles in XOR pattern.
        /// </summary>
        private VoxelType CalculateCheckerboardPattern(int localX, int localZ, int chunkX, int chunkZ)
        {
            // Convert to world coordinates
            int worldX = chunkX * chunkSize + localX;
            int worldZ = chunkZ * chunkSize + localZ;

            // Calculate tile indices
            bool isEvenTileX = ((worldX / TILE_SIZE) % 2) == 0;
            bool isEvenTileZ = ((worldZ / TILE_SIZE) % 2) == 0;

            // XOR pattern for checkerboard
            return (isEvenTileX == isEvenTileZ) ? VoxelType.Grass : VoxelType.Dirt;
        }
    }
}
