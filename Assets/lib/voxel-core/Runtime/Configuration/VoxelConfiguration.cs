using UnityEngine;

namespace TimeSurvivor.Voxel.Core
{
    /// <summary>
    /// ScriptableObject configuration for voxel engine settings.
    /// Create via: Assets > Create > TimeSurvivor > Voxel Configuration
    /// </summary>
    [CreateAssetMenu(fileName = "VoxelConfig", menuName = "TimeSurvivor/Voxel Configuration")]
    public class VoxelConfiguration : ScriptableObject
    {
        [Header("Chunk Settings")]
        [Tooltip("Size of a chunk in voxels (16x16x16 recommended by ADR-004)")]
        [Range(8, 32)]
        public int ChunkSize = 16;

        [Header("Voxel Scale")]
        [Tooltip("Macro voxel size in Unity units (terrain layer)")]
        [Range(0.1f, 1.0f)]
        public float MacroVoxelSize = 0.2f;

        [Tooltip("Micro voxel size in Unity units (destructible props layer)")]
        [Range(0.05f, 0.5f)]
        public float MicroVoxelSize = 0.1f;

        [Header("Streaming & LOD")]
        [Tooltip("Maximum number of chunks to keep in memory (LRU cache)")]
        [Range(100, 1000)]
        public int MaxCachedChunks = 300;

        [Tooltip("Render distance in chunks (from player position)")]
        [Range(4, 16)]
        public int RenderDistance = 8;

        [Tooltip("Maximum chunks to load per frame (prevents frame spikes)")]
        [Range(1, 10)]
        public int MaxChunksLoadedPerFrame = 2;

        [Header("Mesh Generation")]
        [Tooltip("Use amortized meshing to spread work across frames (ADR-005)")]
        public bool UseAmortizedMeshing = true;

        [Tooltip("Maximum milliseconds per frame for meshing (if amortized enabled)")]
        [Range(1f, 10f)]
        public float MaxMeshingTimePerFrameMs = 3f;

        [Header("Procedural Generation")]
        [Tooltip("Seed for procedural terrain generation (0 = random)")]
        public int Seed = 0;

        [Tooltip("Frequency of noise for terrain generation")]
        [Range(0.001f, 0.1f)]
        public float NoiseFrequency = 0.02f;

        [Tooltip("Number of octaves for noise (more = more detail)")]
        [Range(1, 8)]
        public int NoiseOctaves = 4;

        [Header("Performance")]
        [Tooltip("Enable Burst compilation for Jobs (ADR-002)")]
        public bool UseBurstCompilation = true;

        [Tooltip("Use Unity Job System for parallel processing")]
        public bool UseJobSystem = true;

        /// <summary>
        /// Get the total volume of a chunk in voxels
        /// </summary>
        public int ChunkVolume => ChunkSize * ChunkSize * ChunkSize;

        /// <summary>
        /// Get the world size of a macro-scale chunk in Unity units
        /// </summary>
        public float MacroChunkWorldSize => ChunkSize * MacroVoxelSize;

        /// <summary>
        /// Get the world size of a micro-scale chunk in Unity units
        /// </summary>
        public float MicroChunkWorldSize => ChunkSize * MicroVoxelSize;

        private void OnValidate()
        {
            // Ensure chunk size is power of 2 for optimization
            if (!IsPowerOfTwo(ChunkSize))
            {
                Debug.LogWarning($"[VoxelConfiguration] ChunkSize {ChunkSize} is not a power of 2. " +
                                 "Consider using 8, 16, or 32 for better performance.");
            }

            // Ensure micro voxel size is smaller than macro
            if (MicroVoxelSize >= MacroVoxelSize)
            {
                Debug.LogWarning($"[VoxelConfiguration] MicroVoxelSize ({MicroVoxelSize}) should be smaller than " +
                                 $"MacroVoxelSize ({MacroVoxelSize}).");
            }

            // Warn about extreme cache sizes
            int estimatedMemoryMB = (MaxCachedChunks * ChunkVolume * sizeof(byte)) / (1024 * 1024);
            if (estimatedMemoryMB > 500)
            {
                Debug.LogWarning($"[VoxelConfiguration] Estimated memory usage: {estimatedMemoryMB} MB. " +
                                 "Consider reducing MaxCachedChunks or ChunkSize.");
            }
        }

        private bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
