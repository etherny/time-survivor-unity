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

            // Step 2: Convert chunk coordinate to world origin
            float3 chunkOrigin = VoxelMath.ChunkCoordToWorld(ChunkCoord, ChunkSize, VoxelSize);

            // Step 3: Convert local 3D coordinate to world position
            float3 worldPos = chunkOrigin + new float3(x, y, z) * VoxelSize;

            // Step 4: Generate voxel type at this world position using noise
            VoxelType voxelType = GenerateVoxelAt(worldPos);

            // Step 5: Write result to output array
            VoxelData[index] = voxelType;
        }

        // ========== Private Helper Methods ==========

        /// <summary>
        /// Determines the voxel type at a given world position using 2D heightmap approach (Minecraft-style).
        /// Generates flat terrain with natural layers: Grass surface, Dirt below, Stone deep underground.
        /// Water fills valleys below TerrainOffsetY.
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

            // BUG FIX #1: Reduce amplitude from 30% to 25% for better terrain visibility
            // Amplitude controls hill height variation (±25% = ±16 blocks for ChunkSize=64)
            // Example: noise=0  → surfaceHeight = TerrainOffsetY
            //          noise=1  → surfaceHeight = TerrainOffsetY + 16 blocks
            //          noise=-1 → surfaceHeight = TerrainOffsetY - 16 blocks
            float heightAmplitude = ChunkSize * 0.25f; // ±25% of chunk height
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
