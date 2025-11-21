using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Custom voxel generator for Minecraft-style terrain using pre-generated 2D heightmap.
    /// Implements IVoxelGenerator interface for use with ChunkManager.
    ///
    /// Architecture:
    /// - Uses MinecraftHeightmapGenerator for 2D heightmap (generated once at startup)
    /// - Applies heightmap + horizontal layering (Grass > Dirt > Stone) per chunk
    /// - Supports water generation in valleys below water level
    /// - Thread-safe for use in Unity Jobs (uses ProceduralTerrainGenerationJob)
    ///
    /// Usage:
    /// 1. Create MinecraftHeightmapGenerator and call GenerateHeightmap()
    /// 2. Pass generator to constructor of MinecraftTerrainCustomGenerator
    /// 3. Use with ChunkManager(customGenerator: generator)
    /// </summary>
    public class MinecraftTerrainCustomGenerator : IVoxelGenerator
    {
        private readonly MinecraftTerrainConfiguration _minecraftConfig;
        private readonly VoxelConfiguration _voxelConfig;
        private readonly MinecraftHeightmapGenerator _heightmapGenerator;
        private readonly int _seed;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="minecraftConfig">Minecraft terrain configuration (world size, layers, water)</param>
        /// <param name="voxelConfig">Voxel engine configuration (chunk size, voxel size)</param>
        /// <param name="heightmapGenerator">Pre-generated heightmap generator (must call GenerateHeightmap() first)</param>
        /// <param name="seed">Random seed for deterministic generation</param>
        public MinecraftTerrainCustomGenerator(
            MinecraftTerrainConfiguration minecraftConfig,
            VoxelConfiguration voxelConfig,
            MinecraftHeightmapGenerator heightmapGenerator,
            int seed)
        {
            _minecraftConfig = minecraftConfig;
            _voxelConfig = voxelConfig;
            _heightmapGenerator = heightmapGenerator;
            _seed = seed;

            // Validate heightmap was generated
            if (!_heightmapGenerator.IsGenerated)
            {
                throw new System.InvalidOperationException(
                    "MinecraftHeightmapGenerator must call GenerateHeightmap() before use. " +
                    "Heightmap not generated.");
            }
        }

        /// <summary>
        /// Generate voxel data for a chunk using heightmap + horizontal layering.
        /// Implements IVoxelGenerator.Generate().
        /// </summary>
        /// <param name="coord">Chunk coordinate to generate</param>
        /// <param name="chunkSize">Size of chunk in voxels (e.g., 64)</param>
        /// <param name="allocator">NativeArray allocator type</param>
        /// <returns>Flat array of voxel types (length = chunkSize³)</returns>
        public NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator)
        {
            // Allocate output voxel data
            // NOTE: If allocator is Temp, we must use TempJob instead for Unity Jobs compatibility
            int totalVoxels = chunkSize * chunkSize * chunkSize;
            Allocator jobAllocator = (allocator == Allocator.Temp) ? Allocator.TempJob : allocator;
            var voxelData = new NativeArray<VoxelType>(totalVoxels, jobAllocator);

            // Calculate chunk offset in world voxel coordinates
            int3 chunkOffsetVoxels = new int3(
                coord.X * chunkSize,
                coord.Y * chunkSize,
                coord.Z * chunkSize
            );

            // Copy heightmap to temporary NativeArray for job (read-only)
            var heightmap = _heightmapGenerator.Heightmap;

            // Create ProceduralTerrainGenerationJob in heightmap mode
            var job = new ProceduralTerrainGenerationJob
            {
                ChunkCoord = coord,
                ChunkSize = chunkSize,
                VoxelSize = _voxelConfig.MacroVoxelSize,
                Seed = _seed,
                VoxelData = voxelData,

                // Heightmap mode parameters
                Heightmap = heightmap,
                HeightmapWidth = _heightmapGenerator.HeightmapWidth,
                HeightmapHeight = _heightmapGenerator.HeightmapHeight,
                ChunkOffsetVoxels = chunkOffsetVoxels,
                GrassLayerThickness = _minecraftConfig.GrassLayerThickness,
                DirtLayerThickness = _minecraftConfig.DirtLayerThickness,
                WaterLevel = _minecraftConfig.GenerateWater ? _minecraftConfig.WaterLevelVoxels : 0
            };

            // Schedule parallel job and wait for completion
            var handle = job.Schedule(totalVoxels, 64);
            handle.Complete();

            return voxelData;
        }

        /// <summary>
        /// Get a single voxel at a world position without generating a full chunk.
        /// Useful for raycasting and point queries.
        /// Implements IVoxelGenerator.GetVoxelAt().
        /// </summary>
        /// <param name="worldX">World X coordinate in voxels</param>
        /// <param name="worldY">World Y coordinate in voxels</param>
        /// <param name="worldZ">World Z coordinate in voxels</param>
        /// <returns>Voxel type at that position</returns>
        public VoxelType GetVoxelAt(int worldX, int worldY, int worldZ)
        {
            // Lookup terrain height from heightmap
            float terrainHeight = _heightmapGenerator.GetHeightAt(worldX, worldZ);

            // Determine voxel type based on position relative to terrain surface
            if (worldY > terrainHeight)
            {
                // Above terrain surface
                if (_minecraftConfig.GenerateWater && worldY <= _minecraftConfig.WaterLevelVoxels)
                {
                    // Water fills valleys below water level
                    return VoxelType.Water;
                }
                else
                {
                    // Sky/air above water level
                    return VoxelType.Air;
                }
            }
            else
            {
                // Inside terrain - apply horizontal layering
                float depthBelowSurface = terrainHeight - worldY;

                // Grass layer (surface)
                if (depthBelowSurface < _minecraftConfig.GrassLayerThickness)
                    return VoxelType.Grass;

                // Dirt layer (subsurface)
                float dirtLayerEnd = _minecraftConfig.GrassLayerThickness + _minecraftConfig.DirtLayerThickness;
                if (depthBelowSurface < dirtLayerEnd)
                    return VoxelType.Dirt;

                // Stone (deep underground)
                return VoxelType.Stone;
            }
        }
    }
}
