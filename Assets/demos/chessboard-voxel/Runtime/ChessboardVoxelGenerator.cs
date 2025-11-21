using Unity.Collections;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Demos.ChessboardVoxel
{
    /// <summary>
    /// IVoxelGenerator implementation that creates a chessboard pattern.
    /// Generates a flat 16x16 chessboard (single layer Y=0) using Stone and Sand voxels.
    /// Thread-safe for use in Unity Jobs.
    /// </summary>
    public class ChessboardVoxelGenerator : IVoxelGenerator
    {
        private readonly int _boardSize;
        private readonly int _baseHeight;

        /// <summary>
        /// Creates a new ChessboardVoxelGenerator.
        /// </summary>
        /// <param name="boardSize">Size of the chessboard (default: 16x16)</param>
        /// <param name="baseHeight">Y coordinate for the board layer (default: 0)</param>
        public ChessboardVoxelGenerator(int boardSize = 16, int baseHeight = 0)
        {
            _boardSize = boardSize;
            _baseHeight = baseHeight;
        }

        /// <summary>
        /// Generate voxel data for a chunk at the specified coordinate.
        /// Creates chessboard pattern at Y=baseHeight, air everywhere else.
        /// </summary>
        public NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator)
        {
            var voxelData = new NativeArray<VoxelType>(chunkSize * chunkSize * chunkSize, allocator);

            // Calculate world offset for this chunk
            int worldOffsetX = coord.X * chunkSize;
            int worldOffsetY = coord.Y * chunkSize;
            int worldOffsetZ = coord.Z * chunkSize;

            // Fill voxel data
            // IMPORTANT: Loop order must match Flatten3DIndex (x + y*size + z*size²)
            // So we loop Z, then Y, then X to fill the array in the correct order
            for (int z = 0; z < chunkSize; z++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        int worldX = worldOffsetX + x;
                        int worldY = worldOffsetY + y;
                        int worldZ = worldOffsetZ + z;

                        int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);
                        voxelData[index] = GetVoxelAt(worldX, worldY, worldZ);
                    }
                }
            }

            return voxelData;
        }

        /// <summary>
        /// Get a single voxel at a world position.
        /// Returns Stone or Sand for chessboard pattern at Y=baseHeight, Air otherwise.
        /// </summary>
        public VoxelType GetVoxelAt(int worldX, int worldY, int worldZ)
        {
            // Only place voxels at the base height
            if (worldY != _baseHeight)
                return VoxelType.Air;

            // Only place voxels within the board bounds
            if (worldX < 0 || worldX >= _boardSize || worldZ < 0 || worldZ >= _boardSize)
                return VoxelType.Air;

            // Chessboard pattern: (x + z) % 2 determines color
            bool isEvenSquare = ((worldX + worldZ) % 2) == 0;
            return isEvenSquare ? VoxelType.Stone : VoxelType.Sand;
        }
    }
}
