using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Burst-compiled parallel job for procedural voxel terrain generation using 3D Simplex noise.
    /// Implements IJobParallelFor for maximum performance, processing multiple voxels concurrently.
    ///
    /// Performance: Targets less than 0.3ms per 64^3 chunk (262,144 voxels).
    /// Algorithm: Multi-octave fractal noise with altitude-based thresholds for natural terrain.
    ///
    /// Supports two generation modes:
    /// 1. 3D Noise Mode (default): Uses 3D Simplex noise for terrain generation (legacy behavior)
    /// 2. Heightmap Mode (Minecraft-style): Uses pre-generated 2D heightmap for flat terrain with layers
    ///
    /// Conforms to ADR-007: Procedural Terrain Generation Specifications.
    /// </summary>
    [BurstCompile(CompileSynchronously = true, FloatMode = FloatMode.Fast)]
    public struct ProceduralTerrainGenerationJob : IJobParallelFor
    {
        // ========== Input Parameters (Read-Only) ==========
        // NOTE: Property names use PascalCase for compatibility with ChunkManager

        /// <summary>Chunk coordinate in chunk grid space.</summary>
        [ReadOnly] public ChunkCoord ChunkCoord;

        /// <summary>Chunk dimension in voxels (e.g., 64 for 64x64x64 chunk).</summary>
        [ReadOnly] public int ChunkSize;

        /// <summary>Size of a single voxel in Unity world units (e.g., 0.2f).</summary>
        [ReadOnly] public float VoxelSize;

        /// <summary>Random seed for deterministic procedural generation (same seed = same terrain).</summary>
        [ReadOnly] public int Seed;

        /// <summary>Base frequency of noise (scale of terrain features). Default: 0.02f.</summary>
        [ReadOnly] public float NoiseFrequency;

        /// <summary>Number of noise octaves to combine. Default: 4 (ADR-007).</summary>
        [ReadOnly] public int NoiseOctaves;

        /// <summary>Frequency multiplier per octave. Default: 2.0f (ADR-007).</summary>
        [ReadOnly] public float Lacunarity;

        /// <summary>Amplitude multiplier per octave. Default: 0.5f (ADR-007).</summary>
        [ReadOnly] public float Persistence;

        /// <summary>Vertical offset for terrain generation (shifts terrain up/down). Default: 0f.</summary>
        [ReadOnly] public float TerrainOffsetY;

        // ========== Heightmap Mode Parameters (Optional) ==========

        /// <summary>
        /// Optional 2D heightmap for Minecraft-style terrain generation.
        /// If Length > 0, uses heightmap mode. If Length == 0, uses 3D noise mode (legacy).
        /// Format: Row-major (X changes fastest), size = HeightmapWidth × HeightmapHeight.
        /// </summary>
        [ReadOnly] public NativeArray<float> Heightmap;

        /// <summary>Width of heightmap in voxels (world X dimension).</summary>
        [ReadOnly] public int HeightmapWidth;

        /// <summary>Height of heightmap in voxels (world Z dimension).</summary>
        [ReadOnly] public int HeightmapHeight;

        /// <summary>
        /// Chunk offset in world voxel coordinates (ChunkCoord × ChunkSize).
        /// Used to map chunk-local coordinates to world heightmap coordinates.
        /// Example: Chunk (2, 0, 3) with ChunkSize 64 → Offset (128, 0, 192).
        /// </summary>
        [ReadOnly] public int3 ChunkOffsetVoxels;

        /// <summary>Thickness of grass layer on surface (voxels). Default: 1.</summary>
        [ReadOnly] public int GrassLayerThickness;

        /// <summary>Thickness of dirt layer below grass (voxels). Default: 3.</summary>
        [ReadOnly] public int DirtLayerThickness;

        /// <summary>Water level in world voxels. Water fills air below this level. Default: 0 (disabled).</summary>
        [ReadOnly] public int WaterLevel;

        // ========== Output (Write-Only) ==========

        /// <summary>
        /// Output voxel data as VoxelType array.
        /// Size: ChunkSize^3 (e.g., 262,144 elements for 64^3 chunk).
        /// Index layout: X-Y-Z order (X changes fastest, then Y, then Z).
        /// </summary>
        [WriteOnly] public NativeArray<VoxelType> VoxelData;

        // ========== IJobParallelFor Implementation ==========

        /// <summary>
        /// Execute method called in parallel for each voxel in the chunk.
        /// Converts flat index to 3D coordinate, calculates world position, and generates voxel type.
        /// Supports both 3D noise mode (legacy) and heightmap mode (Minecraft-style).
        /// </summary>
        /// <param name="index">Flat array index in range [0, chunkSize^3 - 1]</param>
        public void Execute(int index)
        {
            // Step 1: Convert flat index to 3D voxel coordinate (X-Y-Z ordering)
            // Formula: z = index / (ChunkSize^2), y = (index % ChunkSize^2) / ChunkSize, x = index % ChunkSize
            int chunkSizeSquared = ChunkSize * ChunkSize;
            int z = index / chunkSizeSquared;
            int remainder = index % chunkSizeSquared;
            int y = remainder / ChunkSize;
            int x = remainder % ChunkSize;

            // Step 2: Check generation mode (heightmap vs 3D noise)
            VoxelType voxelType;

            if (Heightmap.IsCreated && Heightmap.Length > 0)
            {
                // HEIGHTMAP MODE: Use pre-generated 2D heightmap
                voxelType = GenerateVoxelFromHeightmap(x, y, z);
            }
            else
            {
                // 3D NOISE MODE (LEGACY): Use 3D Simplex noise
                float3 chunkOrigin = VoxelMath.ChunkCoordToWorld(ChunkCoord, ChunkSize, VoxelSize);
                float3 worldPos = chunkOrigin + new float3(x, y, z) * VoxelSize;
                voxelType = GenerateVoxelAt(worldPos);
            }

            // Step 3: Write result to output array
            VoxelData[index] = voxelType;
        }

        // ========== Private Helper Methods ==========

        /// <summary>
        /// Generate voxel type using pre-generated 2D heightmap (Minecraft-style).
        /// Uses heightmap to determine terrain surface, then applies horizontal layering:
        /// - Grass (surface, configurable thickness)
        /// - Dirt (subsurface, configurable thickness)
        /// - Stone (deep underground)
        /// - Water (fills air below water level)
        /// </summary>
        /// <param name="localX">Local X coordinate within chunk (0 to ChunkSize-1)</param>
        /// <param name="localY">Local Y coordinate within chunk (0 to ChunkSize-1)</param>
        /// <param name="localZ">Local Z coordinate within chunk (0 to ChunkSize-1)</param>
        /// <returns>VoxelType for this position</returns>
        private VoxelType GenerateVoxelFromHeightmap(int localX, int localY, int localZ)
        {
            // STEP 1: Convert local chunk coordinates to world voxel coordinates
            int worldVoxelX = ChunkOffsetVoxels.x + localX;
            int worldVoxelY = ChunkOffsetVoxels.y + localY;
            int worldVoxelZ = ChunkOffsetVoxels.z + localZ;

            // STEP 2: Lookup terrain height from heightmap (clamp to bounds)
            int hmX = math.clamp(worldVoxelX, 0, HeightmapWidth - 1);
            int hmZ = math.clamp(worldVoxelZ, 0, HeightmapHeight - 1);
            int heightmapIndex = hmZ * HeightmapWidth + hmX;
            float terrainHeight = Heightmap[heightmapIndex];

            // STEP 3: Determine voxel type based on position relative to terrain surface
            if (worldVoxelY > terrainHeight)
            {
                // Above terrain surface
                if (WaterLevel > 0 && worldVoxelY <= WaterLevel)
                {
                    // Water fills air below water level (lakes in valleys)
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
                // Inside terrain - apply horizontal layering (Minecraft-style)
                float depthBelowSurface = terrainHeight - worldVoxelY;

                // Grass layer (surface)
                if (depthBelowSurface < GrassLayerThickness)
                    return VoxelType.Grass;

                // Dirt layer (subsurface)
                float dirtLayerEnd = GrassLayerThickness + DirtLayerThickness;
                if (depthBelowSurface < dirtLayerEnd)
                    return VoxelType.Dirt;

                // Stone (deep underground)
                return VoxelType.Stone;
            }
        }

        /// <summary>
        /// Determines the voxel type at a given world position using 2D heightmap approach (Minecraft-style).
        /// Generates flat terrain with natural layers: Grass surface, Dirt below, Stone deep underground.
        /// Water fills valleys below TerrainOffsetY.
        ///
        /// NOTE: This method is used in legacy 3D noise mode. For heightmap mode, use GenerateVoxelFromHeightmap.
        /// </summary>
        /// <param name="worldPos">World position of voxel to generate</param>
        /// <returns>VoxelType for this position (Air, Grass, Dirt, Stone, Water)</returns>
        private VoxelType GenerateVoxelAt(float3 worldPos)
        {
            // STEP 1: Generate 2D heightmap using noise on X-Z plane (ignore Y for surface height)
            // This creates a terrain surface with hills and valleys
            float heightNoise = SimplexNoise3D.MultiOctave(
                worldPos.x, 0f, worldPos.z, // Use Y=0 for 2D heightmap
                Seed,
                NoiseFrequency,
                NoiseOctaves,
                Lacunarity,
                Persistence);

            // AMPLITUDE FIX: Increase amplitude to 50% for better stone distribution
            // Amplitude controls hill height variation (±50% = ±6.4 world units for ChunkSize=64, VoxelSize=0.2)
            // Example: noise=0  → surfaceHeight = TerrainOffsetY
            //          noise=1  → surfaceHeight = TerrainOffsetY + 6.4 world units (32 voxels)
            //          noise=-1 → surfaceHeight = TerrainOffsetY - 6.4 world units (32 voxels)
            float heightAmplitude = ChunkSize * VoxelSize * 0.5f; // ±50% in world units (reduced stone from 44% to ~28%)
            float surfaceHeight = TerrainOffsetY + (heightNoise * heightAmplitude);

            // STEP 2: Define water level at base terrain height (fills valleys only)
            float waterLevel = TerrainOffsetY;
            float voxelY = worldPos.y;

            // STEP 3: Determine voxel type based on position relative to surface
            if (voxelY > surfaceHeight)
            {
                // Above terrain surface
                // BUG FIX #2: Water only in valleys (voxelY <= waterLevel), not floating in air
                if (voxelY <= waterLevel)
                    return VoxelType.Water; // Lakes in valleys below TerrainOffsetY
                else
                    return VoxelType.Air; // Sky above water level
            }
            else
            {
                // Inside terrain - apply horizontal layering (Minecraft-style)
                float depthBelowSurface = surfaceHeight - voxelY;

                // BUG FIX #3: Increase grass layer from 0.5 blocks to 1.0 block for visibility
                if (depthBelowSurface < 1.0f)
                    return VoxelType.Grass; // Surface layer: 1 full block of grass
                else if (depthBelowSurface < 4.0f)
                    return VoxelType.Dirt; // Subsurface: 3 blocks of dirt
                else
                    return VoxelType.Stone; // Deep underground: stone
            }
        }

    }
}
