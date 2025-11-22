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
        private bool _isInitialized = false;

        /// <summary>
        /// Programmatically initialize the terrain streamer with all parameters.
        /// This method allows runtime configuration without using Inspector assignment.
        /// The streamer will create its own ChunkManager internally.
        /// </summary>
        /// <param name="config">VoxelConfiguration defining chunk size, render distance, etc.</param>
        /// <param name="chunkMaterial">Material to apply to chunk meshes</param>
        /// <param name="streamingTarget">Transform to stream chunks around (e.g., player)</param>
        /// <param name="useMainCamera">If true, uses Camera.main as streaming target (overrides streamingTarget parameter)</param>
        /// <param name="showDebugInfo">If true, displays debug UI with streaming statistics</param>
        public void Initialize(
            VoxelConfiguration config,
            Material chunkMaterial,
            Transform streamingTarget,
            bool useMainCamera = false,
            bool showDebugInfo = false)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ProceduralTerrainStreamer] Already initialized. Ignoring duplicate Initialize() call.");
                return;
            }

            // Assign parameters
            _config = config;
            _chunkMaterial = chunkMaterial;
            _streamingTarget = streamingTarget;
            _useMainCamera = useMainCamera;
            _showDebugInfo = showDebugInfo;

            // Perform initialization
            ValidateAndInitialize();
        }

        /// <summary>
        /// Programmatically initialize the terrain streamer with an existing ChunkManager.
        /// This allows sharing a ChunkManager instance between multiple systems (e.g., custom terrain generator).
        /// Use this overload when you need fine-grained control over chunk generation.
        /// </summary>
        /// <param name="config">VoxelConfiguration defining chunk size, render distance, etc.</param>
        /// <param name="chunkManager">Pre-configured ChunkManager instance to use for chunk operations</param>
        /// <param name="streamingTarget">Transform to stream chunks around (e.g., player)</param>
        /// <param name="useMainCamera">If true, uses Camera.main as streaming target (overrides streamingTarget parameter)</param>
        /// <param name="showDebugInfo">If true, displays debug UI with streaming statistics</param>
        public void Initialize(
            VoxelConfiguration config,
            ChunkManager chunkManager,
            Transform streamingTarget,
            bool useMainCamera = false,
            bool showDebugInfo = false)
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ProceduralTerrainStreamer] Already initialized. Ignoring duplicate Initialize() call.");
                return;
            }

            // Assign parameters
            _config = config;
            _chunkManager = chunkManager;
            _streamingTarget = streamingTarget;
            _useMainCamera = useMainCamera;
            _showDebugInfo = showDebugInfo;

            // Perform initialization (skip ChunkManager creation since it was provided)
            ValidateAndInitialize();
        }

        private void Awake()
        {
            // Only initialize from Inspector if not already initialized programmatically
            if (!_isInitialized)
            {
                ValidateAndInitialize();
            }
        }

        private void Start()
        {
            // Final validation check before streaming begins
            if (!_isInitialized)
            {
                Debug.LogError("[ProceduralTerrainStreamer] Failed to initialize. Disabling component.");
                enabled = false;
            }
        }

        /// <summary>
        /// Validates configuration and initializes internal systems.
        /// </summary>
        private void ValidateAndInitialize()
        {
            // Validation
            if (_config == null)
            {
                Debug.LogError("[ProceduralTerrainStreamer] VoxelConfiguration is not assigned!");
                enabled = false;
                return;
            }

            if (_chunkMaterial == null && _chunkManager == null)
            {
                Debug.LogWarning("[ProceduralTerrainStreamer] Chunk material not assigned. Creating default material.");
                _chunkMaterial = new Material(Shader.Find("Standard"));
            }

            // Initialize ChunkManager if not provided
            if (_chunkManager == null)
            {
                _chunkManager = new ChunkManager(_config, transform, _chunkMaterial);
            }

            // Initialize LRU cache
            if (_chunkCache == null)
            {
                _chunkCache = new LRUCache<ChunkCoord, TerrainChunk>(_config.MaxCachedChunks);
            }

            // Validate and set streaming target
            if (!ValidateStreamingTarget())
            {
                enabled = false;
                return;
            }

            _lastPlayerChunk = new ChunkCoord(int.MaxValue, int.MaxValue, int.MaxValue);
            _isInitialized = true;

            Debug.Log("[ProceduralTerrainStreamer] Initialized successfully.");
        }

        /// <summary>
        /// Validates and sets the streaming target transform.
        /// </summary>
        /// <returns>True if streaming target is valid, false otherwise</returns>
        private bool ValidateStreamingTarget()
        {
            if (_useMainCamera)
            {
                _streamingTarget = Camera.main?.transform;
                if (_streamingTarget == null)
                {
                    Debug.LogError("[ProceduralTerrainStreamer] MainCamera not found!");
                    return false;
                }
            }
            else if (_streamingTarget == null)
            {
                Debug.LogError("[ProceduralTerrainStreamer] Streaming target not assigned!");
                return false;
            }

            return true;
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

            // Process chunk generation, meshing, and collision
            _chunkManager.ProcessGenerationQueue();
            _chunkManager.ProcessMeshingQueue(Time.deltaTime);
            _chunkManager.ProcessCollisionQueue(Time.deltaTime);
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
            GUILayout.Label($"Collision Queue: {_chunkManager.CollisionQueueCount}");

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
