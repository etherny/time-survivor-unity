using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Burst-compiled job for procedural terrain generation (ADR-007).
    /// Generates voxel data using 3D Simplex noise for natural-looking terrain.
    /// </summary>
    [BurstCompile]
    public struct ProceduralTerrainGenerationJob : IJob
    {
        [ReadOnly] public ChunkCoord ChunkCoord;
        [ReadOnly] public int ChunkSize;
        [ReadOnly] public float VoxelSize;
        [ReadOnly] public int Seed;
        [ReadOnly] public float NoiseFrequency;
        [ReadOnly] public int NoiseOctaves;

        [WriteOnly] public NativeArray<VoxelType> VoxelData;

        public void Execute()
        {
            var noise = new SimplexNoise3D(Seed, NoiseFrequency, NoiseOctaves);

            // Calculate world position of chunk origin
            float3 chunkWorldOrigin = VoxelMath.ChunkCoordToWorld(ChunkCoord, ChunkSize, VoxelSize);

            for (int y = 0; y < ChunkSize; y++)
            {
                for (int z = 0; z < ChunkSize; z++)
                {
                    for (int x = 0; x < ChunkSize; x++)
                    {
                        int3 localCoord = new int3(x, y, z);
                        int index = VoxelMath.Flatten3DIndex(x, y, z, ChunkSize);

                        // Calculate world position of this voxel
                        float3 worldPos = chunkWorldOrigin + (float3)localCoord * VoxelSize;

                        // Generate voxel type based on noise
                        VoxelData[index] = GenerateVoxelAt(worldPos, noise);
                    }
                }
            }
        }

        private VoxelType GenerateVoxelAt(float3 worldPos, SimplexNoise3D noise)
        {
            // Get 3D noise value for this position
            float density = noise.GetNoise(worldPos.x, worldPos.y, worldPos.z);

            // Simple terrain generation: higher Y = less likely to be solid
            float heightFactor = worldPos.y * 0.05f; // Scale factor for height influence
            float threshold = 0.3f - heightFactor; // Threshold decreases with height

            if (density > threshold)
            {
                // Determine voxel type based on height
                if (worldPos.y > 10f)
                {
                    return VoxelType.Stone; // Mountains
                }
                else if (worldPos.y > 0f)
                {
                    // Surface layer
                    float surfaceNoise = noise.GetNoise(worldPos.x * 0.1f, 0, worldPos.z * 0.1f);
                    if (surfaceNoise > 0.2f)
                        return VoxelType.Grass;
                    else
                        return VoxelType.Dirt;
                }
                else if (worldPos.y > -5f)
                {
                    return VoxelType.Dirt; // Underground
                }
                else
                {
                    return VoxelType.Stone; // Deep underground
                }
            }
            else
            {
                // Empty space or water
                if (worldPos.y < -2f)
                    return VoxelType.Water; // Underground water
                else
                    return VoxelType.Air;
            }
        }
    }
}
