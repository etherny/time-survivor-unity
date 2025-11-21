using Unity.Collections;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Simple flat checkerboard terrain generator for testing/demo purposes.
    /// Generates a 1-block high flat terrain with alternating green colors in a checkerboard pattern.
    ///
    /// This generator is intentionally trivial and optimized for simplicity over performance.
    /// Use for demos, testing, and educational purposes.
    /// </summary>
    public class SimpleCheckerboardGenerator : IVoxelGenerator
    {
        private readonly int _sizeX;
        private readonly int _sizeZ;

        /// <summary>
        /// Creates a new checkerboard generator with specified terrain dimensions.
        /// </summary>
        /// <param name="sizeX">Width of the terrain in voxels</param>
        /// <param name="sizeZ">Depth of the terrain in voxels</param>
        public SimpleCheckerboardGenerator(int sizeX, int sizeZ)
        {
            _sizeX = sizeX;
            _sizeZ = sizeZ;
        }

        /// <summary>
        /// Generate voxel data for a chunk at the specified coordinate.
        /// Only generates terrain at Y=0 (single layer).
        /// </summary>
        public NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator)
        {
            int totalVoxels = chunkSize * chunkSize * chunkSize;
            var voxelData = new NativeArray<VoxelType>(totalVoxels, allocator);

            // Iterate through all voxels in the chunk
            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        // Calculate global world coordinates
                        int globalX = coord.X * chunkSize + x;
                        int globalY = coord.Y * chunkSize + y;
                        int globalZ = coord.Z * chunkSize + z;

                        // Calculate flat array index
                        int index = x + y * chunkSize + z * chunkSize * chunkSize;

                        // Check if within terrain bounds
                        if (globalX >= _sizeX || globalZ >= _sizeZ)
                        {
                            voxelData[index] = VoxelType.Air;
                            continue;
                        }

                        // Only generate at Y=0 (1 block high terrain)
                        if (globalY == 0)
                        {
                            // Checkerboard pattern: alternate Grass and Leaves
                            bool isLightGreen = (globalX + globalZ) % 2 == 0;
                            voxelData[index] = isLightGreen ? VoxelType.Grass : VoxelType.Leaves;
                        }
                        else
                        {
                            voxelData[index] = VoxelType.Air;
                        }
                    }
                }
            }

            return voxelData;
        }

        /// <summary>
        /// Get a single voxel at a world position without generating a full chunk.
        /// Useful for raycasting and point queries.
        /// </summary>
        public VoxelType GetVoxelAt(int worldX, int worldY, int worldZ)
        {
            // Check bounds
            if (worldX < 0 || worldX >= _sizeX || worldZ < 0 || worldZ >= _sizeZ)
                return VoxelType.Air;

            // Only terrain at Y=0
            if (worldY == 0)
            {
                bool isLightGreen = (worldX + worldZ) % 2 == 0;
                return isLightGreen ? VoxelType.Grass : VoxelType.Leaves;
            }

            return VoxelType.Air;
        }
    }
}
