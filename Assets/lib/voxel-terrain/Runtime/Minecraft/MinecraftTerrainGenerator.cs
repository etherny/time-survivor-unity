using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// MonoBehaviour for generating complete Minecraft-style terrain worlds.
    /// Orchestrates heightmap generation, chunk creation, and meshing across multiple frames.
    ///
    /// Features:
    /// - Configurable world size (X×Y×Z in chunks)
    /// - 2D heightmap-based terrain with horizontal layering (Grass > Dirt > Stone)
    /// - Optional water generation in valleys
    /// - Batch processing to avoid frame spikes (_chunksPerFrame)
    /// - Events for progress tracking (OnGenerationStarted, OnGenerationProgress, OnGenerationCompleted)
    /// - Memory validation and error handling
    ///
    /// Usage:
    /// 1. Attach to GameObject
    /// 2. Assign VoxelConfiguration and MinecraftTerrainConfiguration ScriptableObjects
    /// 3. Assign Material for voxel rendering
    /// 4. Call GenerateTerrain() or enable _autoGenerate
    /// </summary>
    public class MinecraftTerrainGenerator : MonoBehaviour
    {
        [Header("Configuration")]
        [Tooltip("Voxel engine configuration (chunk size, voxel size, etc.)")]
        [SerializeField] private VoxelConfiguration _voxelConfiguration;

        [Tooltip("Minecraft terrain configuration (world size, height, layers, water)")]
        [SerializeField] private MinecraftTerrainConfiguration _minecraftConfiguration;

        [Header("Rendering")]
        [Tooltip("Material for rendering voxel chunks")]
        [SerializeField] private Material _chunkMaterial;

        [Header("Performance")]
        [Tooltip("Number of chunks to process per frame (lower = smoother, slower generation)")]
        [Range(1, 10)]
        [SerializeField] private int _chunksPerFrame = 5;

        [Header("Auto-Generation")]
        [Tooltip("Generate terrain automatically on Start()")]
        [SerializeField] private bool _autoGenerate = true;

        [Header("Events")]
        public UnityEvent OnGenerationStarted = new UnityEvent();
        public UnityEvent<int, int> OnGenerationProgress = new UnityEvent<int, int>(); // (current, total)
        public UnityEvent<float> OnGenerationCompleted = new UnityEvent<float>(); // elapsed time in ms
        public UnityEvent<string> OnGenerationFailed = new UnityEvent<string>(); // error message

        // Private state
        private ChunkManager _chunkManager;
        private MinecraftHeightmapGenerator _heightmapGenerator;
        private MinecraftTerrainCustomGenerator _customGenerator;
        private bool _isGenerating;
        private int _seed;

        /// <summary>
        /// Check if terrain is currently being generated.
        /// </summary>
        public bool IsGenerating => _isGenerating;

        /// <summary>
        /// Get the chunk manager (null if not generated yet).
        /// </summary>
        public ChunkManager ChunkManager => _chunkManager;

        private void Start()
        {
            if (_autoGenerate)
            {
                GenerateTerrain();
            }
        }

        /// <summary>
        /// Generate the complete Minecraft-style terrain world.
        /// Asynchronous - runs as coroutine across multiple frames.
        /// </summary>
        public void GenerateTerrain()
        {
            if (_isGenerating)
            {
                Debug.LogWarning("[MinecraftTerrainGenerator] Generation already in progress. Ignoring request.");
                return;
            }

            StartCoroutine(GenerateTerrainCoroutine());
        }

        /// <summary>
        /// Clear the entire terrain and cleanup resources.
        /// </summary>
        public void ClearTerrain()
        {
            if (_isGenerating)
            {
                Debug.LogWarning("[MinecraftTerrainGenerator] Cannot clear terrain during generation.");
                return;
            }

            // Dispose chunk manager
            if (_chunkManager != null)
            {
                _chunkManager.Dispose();
                _chunkManager = null;
            }

            // Dispose heightmap generator
            if (_heightmapGenerator != null)
            {
                _heightmapGenerator.Dispose();
                _heightmapGenerator = null;
            }

            _customGenerator = null;

            Debug.Log("[MinecraftTerrainGenerator] Terrain cleared successfully.");
        }

        private IEnumerator GenerateTerrainCoroutine()
        {
            _isGenerating = true;
            var startTime = System.Diagnostics.Stopwatch.StartNew();

            // ========== STEP 1: Validation ==========
            Debug.Log("[MinecraftTerrainGenerator] Starting terrain generation...");

            if (!ValidateConfiguration())
            {
                OnGenerationFailed?.Invoke("Configuration validation failed");
                _isGenerating = false;
                yield break;
            }

            OnGenerationStarted?.Invoke();

            // ========== STEP 2: Generate Heightmap ==========
            Debug.Log("[MinecraftTerrainGenerator] Generating 2D heightmap...");

            _seed = _voxelConfiguration.Seed == 0 ? UnityEngine.Random.Range(1, 1000000) : _voxelConfiguration.Seed;

            _heightmapGenerator = new MinecraftHeightmapGenerator(
                worldSizeX: _minecraftConfiguration.WorldSizeX,
                worldSizeZ: _minecraftConfiguration.WorldSizeZ,
                chunkSize: _voxelConfiguration.ChunkSize,
                baseTerrainHeight: _minecraftConfiguration.BaseTerrainHeightVoxels,
                terrainVariation: _minecraftConfiguration.TerrainVariationVoxels,
                heightmapFrequency: _minecraftConfiguration.HeightmapFrequency,
                heightmapOctaves: _minecraftConfiguration.HeightmapOctaves,
                seed: _seed
            );

            _heightmapGenerator.GenerateHeightmap();
            Debug.Log($"[MinecraftTerrainGenerator] Heightmap generated: {_heightmapGenerator.HeightmapWidth}×{_heightmapGenerator.HeightmapHeight} elements");

            // ========== STEP 3: Create ChunkManager with Custom Generator ==========
            _customGenerator = new MinecraftTerrainCustomGenerator(
                minecraftConfig: _minecraftConfiguration,
                voxelConfig: _voxelConfiguration,
                heightmapGenerator: _heightmapGenerator,
                seed: _seed
            );

            _chunkManager = new ChunkManager(
                config: _voxelConfiguration,
                chunkParent: transform,
                chunkMaterial: _chunkMaterial,
                customGenerator: _customGenerator
            );

            Debug.Log("[MinecraftTerrainGenerator] ChunkManager created with MinecraftTerrainCustomGenerator");

            // ========== STEP 4: Generate All Chunks (Batch Processing) ==========
            int totalChunks = _minecraftConfiguration.TotalChunks;
            int chunksGenerated = 0;

            Debug.Log($"[MinecraftTerrainGenerator] Generating {totalChunks} chunks ({_minecraftConfiguration.WorldSizeX}×{_minecraftConfiguration.WorldSizeY}×{_minecraftConfiguration.WorldSizeZ})...");

            // Iterate over all chunk coordinates (X×Y×Z)
            for (int y = 0; y < _minecraftConfiguration.WorldSizeY; y++)
            {
                for (int z = 0; z < _minecraftConfiguration.WorldSizeZ; z++)
                {
                    for (int x = 0; x < _minecraftConfiguration.WorldSizeX; x++)
                    {
                        ChunkCoord coord = new ChunkCoord(x, y, z);
                        _chunkManager.LoadChunk(coord);
                        chunksGenerated++;

                        // Batch processing: yield after _chunksPerFrame chunks
                        if (chunksGenerated % _chunksPerFrame == 0)
                        {
                            // Process queues
                            _chunkManager.ProcessGenerationQueue();
                            _chunkManager.ProcessMeshingQueue(Time.deltaTime);

                            // Fire progress event
                            OnGenerationProgress?.Invoke(chunksGenerated, totalChunks);

                            // Log progress every 50 chunks
                            if (chunksGenerated % 50 == 0)
                            {
                                float progressPercent = (float)chunksGenerated / totalChunks * 100f;
                                Debug.Log($"[MinecraftTerrainGenerator] Progress: {chunksGenerated}/{totalChunks} chunks ({progressPercent:F1}%)");
                            }

                            yield return null; // Wait for next frame
                        }
                    }
                }
            }

            // ========== STEP 5: Flush Remaining Queues ==========
            Debug.Log("[MinecraftTerrainGenerator] Flushing remaining generation and meshing queues...");

            while (_chunkManager.GenerationQueueCount > 0 || _chunkManager.MeshingQueueCount > 0)
            {
                _chunkManager.ProcessGenerationQueue();
                _chunkManager.ProcessMeshingQueue(Time.deltaTime);
                yield return null;
            }

            // ========== STEP 6: Complete ==========
            startTime.Stop();
            float elapsedMs = startTime.ElapsedMilliseconds;
            float avgMsPerChunk = elapsedMs / totalChunks;

            Debug.Log($"[MinecraftTerrainGenerator] ✅ Generation completed!");
            Debug.Log($"  - Total Time: {elapsedMs:F0}ms ({elapsedMs / 1000f:F2}s)");
            Debug.Log($"  - Chunks: {totalChunks}");
            Debug.Log($"  - Avg Time/Chunk: {avgMsPerChunk:F2}ms");
            Debug.Log($"  - World Size: {_minecraftConfiguration.WorldSizeX}×{_minecraftConfiguration.WorldSizeY}×{_minecraftConfiguration.WorldSizeZ} chunks");
            Debug.Log($"  - Voxels: {_minecraftConfiguration.WorldSizeVoxelsX}×{_minecraftConfiguration.WorldSizeVoxelsY}×{_minecraftConfiguration.WorldSizeVoxelsZ}");
            Debug.Log($"  - Memory: {_minecraftConfiguration.EstimatedMemoryMB:F1}MB (voxel data only)");

            OnGenerationCompleted?.Invoke(elapsedMs);

            _isGenerating = false;
        }

        /// <summary>
        /// Validate configuration before generation.
        /// </summary>
        private bool ValidateConfiguration()
        {
            if (_voxelConfiguration == null)
            {
                Debug.LogError("[MinecraftTerrainGenerator] VoxelConfiguration is null. Assign in Inspector.");
                return false;
            }

            if (_minecraftConfiguration == null)
            {
                Debug.LogError("[MinecraftTerrainGenerator] MinecraftTerrainConfiguration is null. Assign in Inspector.");
                return false;
            }

            if (_chunkMaterial == null)
            {
                Debug.LogError("[MinecraftTerrainGenerator] ChunkMaterial is null. Assign in Inspector.");
                return false;
            }

            // Validate memory budget
            if (_minecraftConfiguration.EstimatedMemoryMB > 2000f)
            {
                Debug.LogWarning($"[MinecraftTerrainGenerator] Estimated memory usage is HIGH: {_minecraftConfiguration.EstimatedMemoryMB:F1}MB. " +
                                 "Consider reducing world size.");
            }

            // Validate total chunks
            if (_minecraftConfiguration.TotalChunks > 5000)
            {
                Debug.LogWarning($"[MinecraftTerrainGenerator] Total chunks is VERY HIGH: {_minecraftConfiguration.TotalChunks}. " +
                                 "Generation may take several minutes.");
            }

            // Validate terrain height doesn't exceed world size
            int maxTerrainHeight = _minecraftConfiguration.BaseTerrainHeight + _minecraftConfiguration.TerrainVariation;
            if (maxTerrainHeight > _minecraftConfiguration.WorldSizeY)
            {
                Debug.LogError($"[MinecraftTerrainGenerator] MaxTerrainHeight ({maxTerrainHeight}) exceeds WorldSizeY ({_minecraftConfiguration.WorldSizeY}). " +
                               "Reduce BaseTerrainHeight or TerrainVariation, or increase WorldSizeY.");
                return false;
            }

            return true;
        }

        private void OnDestroy()
        {
            // Cleanup resources on destroy
            ClearTerrain();
        }
    }
}
