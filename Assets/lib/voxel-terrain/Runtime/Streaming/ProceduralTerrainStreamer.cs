using UnityEngine;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// MonoBehaviour managing infinite terrain streaming around a player/camera.
    /// Loads/unloads chunks based on distance using LRU cache.
    /// </summary>
    public class ProceduralTerrainStreamer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VoxelConfiguration _config;
        [SerializeField] private Material _chunkMaterial;

        [Header("Streaming Target")]
        [SerializeField] private Transform _streamingTarget;
        [Tooltip("If true, uses MainCamera as streaming target")]
        [SerializeField] private bool _useMainCamera = true;

        [Header("Debug")]
        [SerializeField] private bool _showDebugInfo = true;

        private ChunkManager _chunkManager;
        private LRUCache<ChunkCoord, TerrainChunk> _chunkCache;
        private ChunkCoord _lastPlayerChunk;

        private void Awake()
        {
            // Validation
            if (_config == null)
            {
                Debug.LogError("[ProceduralTerrainStreamer] VoxelConfiguration is not assigned!");
                enabled = false;
                return;
            }

            if (_chunkMaterial == null)
            {
                Debug.LogWarning("[ProceduralTerrainStreamer] Chunk material not assigned. Creating default material.");
                _chunkMaterial = new Material(Shader.Find("Standard"));
            }

            // Initialize systems
            _chunkManager = new ChunkManager(_config, transform, _chunkMaterial);
            _chunkCache = new LRUCache<ChunkCoord, TerrainChunk>(_config.MaxCachedChunks);

            // Find streaming target
            if (_useMainCamera)
            {
                _streamingTarget = Camera.main?.transform;
                if (_streamingTarget == null)
                {
                    Debug.LogError("[ProceduralTerrainStreamer] MainCamera not found!");
                    enabled = false;
                    return;
                }
            }
            else if (_streamingTarget == null)
            {
                Debug.LogError("[ProceduralTerrainStreamer] Streaming target not assigned!");
                enabled = false;
                return;
            }

            _lastPlayerChunk = new ChunkCoord(int.MaxValue, int.MaxValue, int.MaxValue);
        }

        private void Update()
        {
            if (_streamingTarget == null) return;

            // Get current player chunk
            float3 playerPos = _streamingTarget.position;
            ChunkCoord currentChunk = VoxelMath.WorldToChunkCoord(
                playerPos,
                _config.ChunkSize,
                _config.MacroVoxelSize
            );

            // Only update if player moved to a new chunk
            if (currentChunk != _lastPlayerChunk)
            {
                UpdateChunks(currentChunk);
                _lastPlayerChunk = currentChunk;
            }

            // Process chunk generation and meshing
            _chunkManager.ProcessGenerationQueue();
            _chunkManager.ProcessMeshingQueue(Time.deltaTime);
        }

        /// <summary>
        /// Update which chunks are loaded based on player position.
        /// </summary>
        private void UpdateChunks(ChunkCoord playerChunk)
        {
            int renderDistance = _config.RenderDistance;

            // Determine which chunks should be loaded
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    for (int x = -renderDistance; x <= renderDistance; x++)
                    {
                        ChunkCoord coord = playerChunk + new int3(x, y, z);

                        // Check if within render distance (spherical)
                        int distSq = VoxelMath.ChunkDistanceSquared(coord, playerChunk);
                        int maxDistSq = renderDistance * renderDistance;

                        if (distSq <= maxDistSq)
                        {
                            // Load chunk if not already loaded
                            if (!_chunkManager.IsChunkLoaded(coord))
                            {
                                // Load the chunk first
                                _chunkManager.LoadChunk(coord);

                                // Then add to cache (evicting LRU if needed)
                                var chunkToCache = _chunkManager.GetTerrainChunk(coord);
                                if (chunkToCache != null)
                                {
                                    var evicted = _chunkCache.Put(coord, chunkToCache);
                                    if (evicted != null)
                                    {
                                        _chunkManager.UnloadChunk(evicted.Coord);
                                    }
                                }
                            }
                            else
                            {
                                // Mark as recently used in cache
                                if (_chunkCache.TryGet(coord, out var chunk))
                                {
                                    // Just accessing it updates LRU order
                                }
                            }
                        }
                    }
                }
            }

            // Unload chunks outside render distance
            UnloadDistantChunks(playerChunk, renderDistance);
        }

        /// <summary>
        /// Unload chunks that are outside render distance.
        /// </summary>
        private void UnloadDistantChunks(ChunkCoord playerChunk, int renderDistance)
        {
            var chunksToUnload = new System.Collections.Generic.List<ChunkCoord>();

            foreach (var chunk in _chunkManager.GetAllChunks())
            {
                int distSq = VoxelMath.ChunkDistanceSquared(chunk.Coord, playerChunk);
                int maxDistSq = (renderDistance + 1) * (renderDistance + 1); // +1 for hysteresis

                if (distSq > maxDistSq)
                {
                    chunksToUnload.Add(chunk.Coord);
                }
            }

            foreach (var coord in chunksToUnload)
            {
                _chunkManager.UnloadChunk(coord);
                _chunkCache.Remove(coord);
            }
        }

        private void OnGUI()
        {
            if (!_showDebugInfo) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Box("Terrain Streaming Debug");

            GUILayout.Label($"Player Chunk: {_lastPlayerChunk}");
            GUILayout.Label($"Loaded Chunks: {_chunkCache.Count}/{_config.MaxCachedChunks}");
            GUILayout.Label($"Generation Queue: {_chunkManager.GenerationQueueCount}");
            GUILayout.Label($"Meshing Queue: {_chunkManager.MeshingQueueCount}");

            GUILayout.EndArea();
        }

        private void OnDestroy()
        {
            _chunkManager?.Dispose();
        }

        private void OnValidate()
        {
            if (_config != null && _config.RenderDistance <= 0)
            {
                Debug.LogWarning("[ProceduralTerrainStreamer] RenderDistance must be greater than 0!");
            }
        }
    }
}
