using UnityEngine;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Demos.Checkerboard
{
    /// <summary>
    /// Simple demonstration of a flat checkerboard terrain with alternating green colors.
    /// Generates a 50×50 (or custom size) terrain with 2 shades of green in a checkerboard pattern.
    ///
    /// This demo showcases:
    /// - Custom IVoxelGenerator implementation (SimpleCheckerboardGenerator)
    /// - Basic ChunkManager usage for terrain generation
    /// - Minimal configuration for simple terrain visualization
    /// </summary>
    public class CheckerboardTerrainDemo : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Voxel engine configuration (chunk size, voxel size, etc.)")]
        [SerializeField] private VoxelConfiguration _voxelConfig;

        [Tooltip("Material to use for terrain rendering")]
        [SerializeField] private Material _terrainMaterial;

        [Header("Terrain Size")]
        [Tooltip("Width of the terrain in voxels")]
        [Range(10, 200)]
        [SerializeField] private int _sizeX = 50;

        [Tooltip("Depth of the terrain in voxels")]
        [Range(10, 200)]
        [SerializeField] private int _sizeZ = 50;

        [Header("Debug")]
        [Tooltip("Enable detailed debug logs")]
        [SerializeField] private bool _enableDebugLogs = true;

        private ChunkManager _chunkManager;
        private SimpleCheckerboardGenerator _generator;

        private void Start()
        {
            ValidateConfiguration();
            GenerateTerrain();
        }

        /// <summary>
        /// Validate that all required references are assigned.
        /// </summary>
        private void ValidateConfiguration()
        {
            if (_voxelConfig == null)
            {
                Debug.LogError("[CheckerboardTerrainDemo] VoxelConfiguration is not assigned! Please assign it in the Inspector.");
                enabled = false;
                return;
            }

            if (_terrainMaterial == null)
            {
                Debug.LogError("[CheckerboardTerrainDemo] Terrain Material is not assigned! Please assign it in the Inspector.");
                enabled = false;
                return;
            }

            if (_sizeX < 10 || _sizeZ < 10)
            {
                Debug.LogWarning($"[CheckerboardTerrainDemo] Terrain size is very small ({_sizeX}×{_sizeZ}). Consider increasing it.");
            }
        }

        /// <summary>
        /// Generate the checkerboard terrain using the existing voxel engine.
        /// </summary>
        private void GenerateTerrain()
        {
            if (_enableDebugLogs)
                Debug.Log($"[CheckerboardTerrainDemo] Starting terrain generation: {_sizeX}×{_sizeZ} voxels");

            float startTime = Time.realtimeSinceStartup;

            // Create the checkerboard generator
            _generator = new SimpleCheckerboardGenerator(_sizeX, _sizeZ);

            // Create the ChunkManager with custom generator
            _chunkManager = new ChunkManager(
                config: _voxelConfig,
                chunkParent: transform,
                chunkMaterial: _terrainMaterial,
                customGenerator: _generator
            );

            // Calculate number of chunks needed
            int chunkSize = _voxelConfig.ChunkSize;
            int chunksX = Mathf.CeilToInt((float)_sizeX / chunkSize);
            int chunksZ = Mathf.CeilToInt((float)_sizeZ / chunkSize);
            int totalChunks = chunksX * chunksZ;

            if (_enableDebugLogs)
                Debug.Log($"[CheckerboardTerrainDemo] Generating {totalChunks} chunks ({chunksX}×{chunksZ}) with chunk size {chunkSize}");

            // Generate chunks at Y=0 only (single layer terrain)
            for (int z = 0; z < chunksZ; z++)
            {
                for (int x = 0; x < chunksX; x++)
                {
                    ChunkCoord coord = new ChunkCoord(x, 0, z);
                    _chunkManager.LoadChunk(coord);
                }
            }

            // Process all generation and meshing queues
            _chunkManager.ProcessGenerationQueue();
            _chunkManager.ProcessMeshingQueue(Time.deltaTime);

            float elapsedTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            if (_enableDebugLogs)
            {
                Debug.Log($"[CheckerboardTerrainDemo] Terrain generated successfully!");
                Debug.Log($"[CheckerboardTerrainDemo] Total chunks: {totalChunks}");
                Debug.Log($"[CheckerboardTerrainDemo] Generation time: {elapsedTime:F2}ms");
                Debug.Log($"[CheckerboardTerrainDemo] Terrain dimensions: {_sizeX}×1×{_sizeZ} voxels");
            }
        }

        /// <summary>
        /// Regenerate terrain with current settings.
        /// Useful for testing different sizes in Play mode.
        /// </summary>
        [ContextMenu("Regenerate Terrain")]
        public void RegenerateTerrain()
        {
            if (_chunkManager != null)
            {
                _chunkManager.Dispose();
                _chunkManager = null;
            }

            GenerateTerrain();
        }

        private void OnDestroy()
        {
            // Cleanup resources
            if (_chunkManager != null)
            {
                _chunkManager.Dispose();
                _chunkManager = null;
            }
        }

        private void OnValidate()
        {
            // Clamp values to valid ranges
            _sizeX = Mathf.Max(10, _sizeX);
            _sizeZ = Mathf.Max(10, _sizeZ);
        }
    }
}
