using UnityEngine;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// ScriptableObject configuration for Minecraft-style terrain generation.
    /// Defines world size, terrain height parameters, and generation settings.
    /// Create via: Assets > Create > TimeSurvivor > Minecraft Terrain Configuration
    /// </summary>
    [CreateAssetMenu(fileName = "MinecraftTerrainConfig", menuName = "TimeSurvivor/Minecraft Terrain Configuration")]
    public class MinecraftTerrainConfiguration : ScriptableObject
    {
        [Header("World Dimensions (in Chunks)")]
        [Tooltip("World size in X direction (chunks). 10 = 10×64 = 640 voxels wide")]
        [Range(1, 100)]
        public int WorldSizeX = 10;

        [Tooltip("World size in Y direction (chunks). Height limit. 8 = 8×64 = 512 voxels tall")]
        [Range(1, 16)]
        public int WorldSizeY = 8;

        [Tooltip("World size in Z direction (chunks). 10 = 10×64 = 640 voxels deep")]
        [Range(1, 100)]
        public int WorldSizeZ = 10;

        [Header("Terrain Height")]
        [Tooltip("Base terrain height in chunks. Example: 3 = terrain at Y=3 chunks (192 voxels at ChunkSize=64)")]
        [Range(0, 15)]
        public int BaseTerrainHeight = 3;

        [Tooltip("Terrain height variation ± in chunks. Example: 2 = terrain can vary ±128 voxels")]
        [Range(0, 8)]
        public int TerrainVariation = 2;

        [Header("Heightmap Generation")]
        [Tooltip("Frequency of 2D heightmap noise. Lower = larger features (hills/valleys). Default: 0.01f")]
        [Range(0.001f, 0.1f)]
        public float HeightmapFrequency = 0.01f;

        [Tooltip("Number of noise octaves for heightmap detail. More octaves = more fine detail. Default: 4")]
        [Range(1, 6)]
        public int HeightmapOctaves = 4;

        [Header("Terrain Layers")]
        [Tooltip("Thickness of grass layer on surface (voxels). Default: 1")]
        [Range(1, 4)]
        public int GrassLayerThickness = 1;

        [Tooltip("Thickness of dirt layer below grass (voxels). Default: 3")]
        [Range(2, 10)]
        public int DirtLayerThickness = 3;

        [Header("Water Generation")]
        [Tooltip("Generate water in valleys below water level")]
        public bool GenerateWater = true;

        [Tooltip("Water level in chunks. Water fills valleys below this height. Default: BaseTerrainHeight")]
        [Range(0, 15)]
        public int WaterLevel = 3;

        // ========== Derived Properties ==========

        /// <summary>
        /// Total number of chunks to generate (WorldSizeX × WorldSizeY × WorldSizeZ).
        /// </summary>
        public int TotalChunks => WorldSizeX * WorldSizeY * WorldSizeZ;

        /// <summary>
        /// Estimated memory usage in megabytes for voxel data only (ChunkSize=64, VoxelType=1 byte).
        /// Does NOT include mesh data.
        /// </summary>
        public float EstimatedMemoryMB
        {
            get
            {
                const int chunkSize = 64; // Standard chunk size
                const int bytesPerVoxel = 1; // VoxelType = byte
                int voxelsPerChunk = chunkSize * chunkSize * chunkSize; // 262,144
                long totalBytes = (long)TotalChunks * voxelsPerChunk * bytesPerVoxel;
                return totalBytes / (1024f * 1024f);
            }
        }

        /// <summary>
        /// World size in voxels (X axis). Assumes ChunkSize=64.
        /// </summary>
        public int WorldSizeVoxelsX => WorldSizeX * 64;

        /// <summary>
        /// World size in voxels (Y axis). Assumes ChunkSize=64.
        /// </summary>
        public int WorldSizeVoxelsY => WorldSizeY * 64;

        /// <summary>
        /// World size in voxels (Z axis). Assumes ChunkSize=64.
        /// </summary>
        public int WorldSizeVoxelsZ => WorldSizeZ * 64;

        /// <summary>
        /// Base terrain height in voxels. Assumes ChunkSize=64.
        /// </summary>
        public int BaseTerrainHeightVoxels => BaseTerrainHeight * 64;

        /// <summary>
        /// Terrain variation in voxels. Assumes ChunkSize=64.
        /// </summary>
        public int TerrainVariationVoxels => TerrainVariation * 64;

        /// <summary>
        /// Water level in voxels. Assumes ChunkSize=64.
        /// </summary>
        public int WaterLevelVoxels => WaterLevel * 64;

        /// <summary>
        /// Minimum terrain height in voxels (BaseTerrainHeight - TerrainVariation).
        /// </summary>
        public int MinTerrainHeightVoxels => Mathf.Max(0, BaseTerrainHeightVoxels - TerrainVariationVoxels);

        /// <summary>
        /// Maximum terrain height in voxels (BaseTerrainHeight + TerrainVariation).
        /// </summary>
        public int MaxTerrainHeightVoxels => BaseTerrainHeightVoxels + TerrainVariationVoxels;

        // ========== Validation ==========

        private void OnValidate()
        {
            // Ensure WaterLevel defaults to BaseTerrainHeight
            if (WaterLevel == 0)
            {
                WaterLevel = BaseTerrainHeight;
            }

            // Validate BaseTerrainHeight + TerrainVariation doesn't exceed WorldSizeY
            int maxPossibleHeight = BaseTerrainHeight + TerrainVariation;
            if (maxPossibleHeight > WorldSizeY)
            {
                Debug.LogWarning($"[MinecraftTerrainConfiguration] MaxTerrainHeight ({maxPossibleHeight} chunks) " +
                                 $"exceeds WorldSizeY ({WorldSizeY} chunks). " +
                                 $"Reduce BaseTerrainHeight or TerrainVariation.");
            }

            // Validate BaseTerrainHeight - TerrainVariation is non-negative
            int minPossibleHeight = BaseTerrainHeight - TerrainVariation;
            if (minPossibleHeight < 0)
            {
                Debug.LogWarning($"[MinecraftTerrainConfiguration] MinTerrainHeight ({minPossibleHeight} chunks) " +
                                 $"is negative. Increase BaseTerrainHeight or reduce TerrainVariation.");
            }

            // Validate WaterLevel is within reasonable bounds
            if (WaterLevel > BaseTerrainHeight)
            {
                Debug.LogWarning($"[MinecraftTerrainConfiguration] WaterLevel ({WaterLevel}) is higher than " +
                                 $"BaseTerrainHeight ({BaseTerrainHeight}). Water will flood most terrain.");
            }

            // Warn about memory usage
            if (EstimatedMemoryMB > 500f)
            {
                Debug.LogWarning($"[MinecraftTerrainConfiguration] Estimated memory usage: {EstimatedMemoryMB:F1} MB. " +
                                 $"Large worlds may cause performance issues.");
            }

            // Warn about large chunk counts
            if (TotalChunks > 1000)
            {
                Debug.LogWarning($"[MinecraftTerrainConfiguration] Total chunks: {TotalChunks}. " +
                                 $"Generation may take several minutes. Consider smaller world size for testing.");
            }

            // Validate layer thicknesses
            int totalLayerThickness = GrassLayerThickness + DirtLayerThickness;
            if (totalLayerThickness > 64)
            {
                Debug.LogWarning($"[MinecraftTerrainConfiguration] Total layer thickness ({totalLayerThickness} voxels) " +
                                 $"exceeds chunk size. Reduce GrassLayerThickness or DirtLayerThickness.");
            }
        }
    }
}
