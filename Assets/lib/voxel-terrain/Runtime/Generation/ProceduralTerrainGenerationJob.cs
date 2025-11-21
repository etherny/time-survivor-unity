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
        /// Determines the voxel type at a given world position using multi-octave noise.
        /// Applies altitude-based thresholds for natural terrain layering.
        /// </summary>
        /// <param name="worldPos">World position of voxel to generate</param>
        /// <returns>VoxelType for this position (Air, Grass, Dirt, Stone, Water)</returns>
        private VoxelType GenerateVoxelAt(float3 worldPos)
        {
            // Sample 3D noise at this position
            float density = SimplexNoise3D.MultiOctave(
                worldPos.x, worldPos.y, worldPos.z,
                Seed,
                NoiseFrequency,
                NoiseOctaves,
                Lacunarity,
                Persistence);

            // Calculate altitude-based threshold: decreases with height for natural terrain profile
            // Lower Y (underground) = easier to be solid, Higher Y (sky) = easier to be air
            // Using a base threshold of -0.2 to allow for 40% air at sea level
            // Apply TerrainOffsetY to normalize altitude relative to terrain base
            float relativeAltitude = worldPos.y - TerrainOffsetY;
            float threshold = -0.2f - relativeAltitude * 0.02f;

            // Determine if this voxel is solid or empty
            if (density > threshold)
            {
                // Solid voxel: determine type based on relative altitude
                return DetermineSolidVoxelType(relativeAltitude, density);
            }
            else
            {
                // Empty voxel: Air or Water based on relative altitude
                if (relativeAltitude < -2f)
                    return VoxelType.Water; // Underground water caverns
                else
                    return VoxelType.Air; // Open air
            }
        }

        /// <summary>
        /// Determines the solid voxel type based on altitude and surface detection.
        /// Creates natural terrain layers: Grass surface, Dirt underground, Stone deep/mountains.
        /// </summary>
        /// <param name="altitude">Relative altitude from terrain base (adjusted with TerrainOffsetY)</param>
        /// <param name="density">Noise density value at this position</param>
        /// <returns>Solid VoxelType (Grass, Dirt, or Stone)</returns>
        private VoxelType DetermineSolidVoxelType(float altitude, float density)
        {

            // Use a combination of altitude and density for deterministic type selection
            // This ensures adjacent chunks generate identical voxels at borders

            // Deep underground or very dense areas → Stone
            if (altitude < -8f || density > 0.5f)
            {
                return VoxelType.Stone;
            }
            // Mountains (high altitude) → Stone
            else if (altitude > 8f)
            {
                return VoxelType.Stone;
            }
            // Medium density or mid-altitude → Dirt
            else if (density > 0.1f || altitude < -2f)
            {
                return VoxelType.Dirt;
            }
            // Low density at positive altitude → Grass (surface)
            else
            {
                return VoxelType.Grass;
            }
        }

    }
}
