using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Demos.ColorfulCubes
{
    /// <summary>
    /// Pattern types for colorful voxel generation.
    /// </summary>
    public enum ColorPattern
    {
        /// <summary>Horizontal layers cycling through voxel types based on Y</summary>
        RainbowLayers,

        /// <summary>3D checkerboard pattern based on (x+y+z) % 2</summary>
        Checkerboard3D,

        /// <summary>Vertical columns with different colors based on XZ position</summary>
        RainbowGrid,

        /// <summary>Radial gradient from center based on distance</summary>
        GradientSphere,

        /// <summary>Controlled random distribution of colors</summary>
        RandomColors
    }

    /// <summary>
    /// IVoxelGenerator implementation that creates colorful patterns.
    /// Thread-safe for use in Unity Jobs.
    /// </summary>
    public class ColorfulTerrainGenerator : IVoxelGenerator
    {
        private readonly ColorPattern _pattern;
        private readonly int _seed;
        private readonly int3 _centerPoint;

        // Voxel types used for rainbow patterns
        private static readonly VoxelType[] RainbowTypes =
        {
            VoxelType.Grass,   // Green
            VoxelType.Dirt,    // Brown
            VoxelType.Stone,   // Gray
            VoxelType.Sand,    // Yellow
            VoxelType.Wood,    // Dark brown
            VoxelType.Leaves   // Light green
        };

        /// <summary>
        /// Creates a new ColorfulTerrainGenerator.
        /// </summary>
        /// <param name="pattern">The color pattern to generate</param>
        /// <param name="seed">Random seed for RandomColors pattern</param>
        /// <param name="centerPoint">Center point for GradientSphere pattern</param>
        public ColorfulTerrainGenerator(ColorPattern pattern, int seed = 12345, int3 centerPoint = default)
        {
            _pattern = pattern;
            _seed = seed;
            _centerPoint = centerPoint;
        }

        /// <summary>
        /// Generate voxel data for a chunk at the specified coordinate.
        /// Uses NativeArray for thread-safe operations in Unity Jobs.
        /// </summary>
        public NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator)
        {
            var voxelData = new NativeArray<VoxelType>(chunkSize * chunkSize * chunkSize, allocator);

            // Calculate world offset for this chunk
            int worldOffsetX = coord.X * chunkSize;
            int worldOffsetY = coord.Y * chunkSize;
            int worldOffsetZ = coord.Z * chunkSize;

            // Fill voxel data using pattern
            int index = 0;
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        int worldX = worldOffsetX + x;
                        int worldY = worldOffsetY + y;
                        int worldZ = worldOffsetZ + z;

                        voxelData[index] = GetVoxelAt(worldX, worldY, worldZ);
                        index++;
                    }
                }
            }

            return voxelData;
        }

        /// <summary>
        /// Get a single voxel at a world position.
        /// Pattern logic is centralized here for consistency.
        /// </summary>
        public VoxelType GetVoxelAt(int worldX, int worldY, int worldZ)
        {
            switch (_pattern)
            {
                case ColorPattern.RainbowLayers:
                    return GetRainbowLayersVoxel(worldY);

                case ColorPattern.Checkerboard3D:
                    return GetCheckerboard3DVoxel(worldX, worldY, worldZ);

                case ColorPattern.RainbowGrid:
                    return GetRainbowGridVoxel(worldX, worldZ);

                case ColorPattern.GradientSphere:
                    return GetGradientSphereVoxel(worldX, worldY, worldZ);

                case ColorPattern.RandomColors:
                    return GetRandomColorVoxel(worldX, worldY, worldZ);

                default:
                    return VoxelType.Stone;
            }
        }

        #region Pattern Implementations

        /// <summary>
        /// Rainbow Layers: Horizontal layers cycling through voxel types based on Y.
        /// Creates horizontal stripes of different colors.
        /// </summary>
        private VoxelType GetRainbowLayersVoxel(int worldY)
        {
            // Use absolute value to handle negative Y coordinates
            int index = math.abs(worldY) % RainbowTypes.Length;
            return RainbowTypes[index];
        }

        /// <summary>
        /// Checkerboard 3D: 3D checkerboard pattern based on (x+y+z) % 2.
        /// Alternates between two colors in all three dimensions.
        /// </summary>
        private VoxelType GetCheckerboard3DVoxel(int worldX, int worldY, int worldZ)
        {
            bool isEven = ((worldX + worldY + worldZ) % 2) == 0;
            return isEven ? VoxelType.Stone : VoxelType.Sand;
        }

        /// <summary>
        /// Rainbow Grid: Vertical columns with different colors based on XZ position.
        /// Creates a grid of colored columns when viewed from above.
        /// </summary>
        private VoxelType GetRainbowGridVoxel(int worldX, int worldZ)
        {
            // Use absolute values and combine X and Z to create grid pattern
            int columnIndex = (math.abs(worldX) + math.abs(worldZ)) % RainbowTypes.Length;
            return RainbowTypes[columnIndex];
        }

        /// <summary>
        /// Gradient Sphere: Radial gradient from center based on distance.
        /// Creates concentric spherical layers of different colors.
        /// </summary>
        private VoxelType GetGradientSphereVoxel(int worldX, int worldY, int worldZ)
        {
            // Calculate distance from center point
            int dx = worldX - _centerPoint.x;
            int dy = worldY - _centerPoint.y;
            int dz = worldZ - _centerPoint.z;

            // Use integer approximation of distance to avoid floating point
            int distanceSquared = dx * dx + dy * dy + dz * dz;
            int distance = (int)math.sqrt(distanceSquared);

            // Map distance to color index
            int colorIndex = distance % RainbowTypes.Length;
            return RainbowTypes[colorIndex];
        }

        /// <summary>
        /// Random Colors: Controlled random distribution of colors.
        /// Uses a deterministic hash function for consistent randomness.
        /// </summary>
        private VoxelType GetRandomColorVoxel(int worldX, int worldY, int worldZ)
        {
            // Use a hash function to generate deterministic "random" values
            // This ensures the same position always returns the same voxel type
            int hash = HashPosition(worldX, worldY, worldZ, _seed);
            int colorIndex = math.abs(hash) % RainbowTypes.Length;
            return RainbowTypes[colorIndex];
        }

        /// <summary>
        /// Simple hash function for deterministic randomness.
        /// Based on FNV-1a hash algorithm for good distribution.
        /// </summary>
        private int HashPosition(int x, int y, int z, int seed)
        {
            unchecked
            {
                int hash = seed;
                hash = hash * 31 + x;
                hash = hash * 31 + y;
                hash = hash * 31 + z;

                // Additional mixing for better distribution
                hash ^= hash >> 16;
                hash *= (int)0x85ebca6b;
                hash ^= hash >> 13;
                hash *= (int)0xc2b2ae35;
                hash ^= hash >> 16;

                return hash;
            }
        }

        #endregion
    }
}
