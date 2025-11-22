using System.Collections;
using System.Collections.Generic;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Rendering;
using TimeSurvivor.Voxel.Terrain;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace TimeSurvivor.Voxel.Streaming
{
    /// <summary>
    /// Manages streaming of procedurally generated voxel terrain around a player.
    /// Handles chunk loading, unloading, caching, and mesh generation with budget constraints.
    /// Implements hysteresis (load at 100m, unload at 120m) to prevent thrashing.
    /// </summary>
    public class ProceduralTerrainStreamer : MonoBehaviour, IEvictionHandler<ChunkCoord, TerrainChunk>
    {
        #region Serialized Fields

        [Header("Player Tracking")]
        [SerializeField]
        [Tooltip("Player transform to track for streaming")]
        public Transform playerTransform;

        [Header("Configuration")]
        [SerializeField]
        [Tooltip("Voxel configuration asset")]
        public VoxelConfiguration voxelConfig;

        [SerializeField]
        [Tooltip("Streaming configuration asset")]
        public StreamingConfiguration streamingConfig;

        [Header("Rendering")]
        [SerializeField]
        [Tooltip("Material for chunk rendering (URP/Lit)")]
        public Material chunkMaterial;

        [SerializeField]
        [Tooltip("Enable mesh colliders for physics")]
        private bool enableCollision = false;

        [Header("Performance Budget")]
        [SerializeField]
        [Tooltip("Maximum chunks to load per frame")]
        public int maxChunksPerFrame = 1;

        [SerializeField]
        [Tooltip("Maximum time budget per frame (milliseconds)")]
        private float timeBudgetMs = 1.5f;

        [Header("Debug")]
        [SerializeField]
        [Tooltip("Enable debug logging")]
        private bool debugLogging = false;

        #endregion

        #region Private Fields

        private ChunkCache<TerrainChunk> chunkCache;
        private Dictionary<ChunkCoord, TerrainChunk> activeChunks;
        private SortedSet<ChunkLoadRequest> loadQueue;
        private HashSet<ChunkCoord> unloadQueue;
        private HashSet<ChunkCoord> loadingChunks;

        private Vector3 lastPlayerPosition;
        private ChunkCoord lastPlayerChunk;
        private float lastEvaluationTime;

        private const float LOAD_DISTANCE = 100f;
        private const float UNLOAD_DISTANCE = 120f;
        private const float EVALUATION_INTERVAL = 0.5f;

        // Generation constants
        private const int GENERATION_JOB_BATCH_SIZE = 64;
        private const float NOISE_LACUNARITY = 2.0f;
        private const float NOISE_PERSISTENCE = 0.5f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Validate voxel config
            if (voxelConfig == null)
            {
                Debug.LogError("VoxelConfiguration not assigned to ProceduralTerrainStreamer");
                enabled = false;
                return;
            }

            // Validate streaming config
            if (streamingConfig == null)
            {
                Debug.LogError("StreamingConfiguration not assigned to ProceduralTerrainStreamer");
                enabled = false;
                return;
            }

            // Validate player transform
            if (playerTransform == null)
            {
                Debug.LogError("Player transform not assigned to ProceduralTerrainStreamer");
                enabled = false;
                return;
            }

            // Validate material
            if (chunkMaterial == null)
            {
                Debug.LogWarning("Chunk material not assigned to ProceduralTerrainStreamer - chunks may not render correctly");
            }

            // Initialize cache
            chunkCache = new ChunkCache<TerrainChunk>(
                streamingConfig.ChunkCacheCapacity,
                this
            );

            // Initialize collections
            activeChunks = new Dictionary<ChunkCoord, TerrainChunk>();
            loadQueue = new SortedSet<ChunkLoadRequest>();
            unloadQueue = new HashSet<ChunkCoord>();
            loadingChunks = new HashSet<ChunkCoord>();

            // Initialize tracking
            lastPlayerPosition = playerTransform.position;
            lastPlayerChunk = VoxelMath.WorldToChunkCoord(
                lastPlayerPosition,
                voxelConfig.ChunkSize,
                voxelConfig.MacroVoxelSize
            );
            lastEvaluationTime = Time.time;

            if (debugLogging)
            {
                Debug.Log($"ProceduralTerrainStreamer initialized with cache capacity: {streamingConfig.ChunkCacheCapacity}");
            }
        }

        private void Update()
        {
            float startTime = Time.realtimeSinceStartup;

            // Track player movement
            TrackPlayerMovement();

            // Evaluate streaming needs periodically
            if (Time.time - lastEvaluationTime >= EVALUATION_INTERVAL)
            {
                EvaluateStreamingNeeds();
                lastEvaluationTime = Time.time;
            }

            // Process unload queue first (frees resources)
            ProcessUnloadQueue();

            // Process load queue with budget constraints
            ProcessLoadQueue(startTime);

            // Log performance if debugging
            if (debugLogging)
            {
                float frameTime = (Time.realtimeSinceStartup - startTime) * 1000f;
                if (frameTime > timeBudgetMs)
                {
                    Debug.LogWarning($"ProceduralTerrainStreamer exceeded time budget: {frameTime:F2}ms / {timeBudgetMs}ms");
                }
            }
        }

        private void OnDestroy()
        {
            // Cleanup all active chunks
            foreach (var chunk in activeChunks.Values)
            {
                chunk.Dispose();
            }
            activeChunks.Clear();

            // Clear cache (eviction handler will be called for each entry)
            chunkCache?.Clear();

            // Clear queues
            loadQueue?.Clear();
            unloadQueue?.Clear();
            loadingChunks?.Clear();
        }

        #endregion

        #region Player Tracking

        private void TrackPlayerMovement()
        {
            Vector3 currentPosition = playerTransform.position;

            // Check if player moved to a different chunk
            ChunkCoord currentChunk = VoxelMath.WorldToChunkCoord(
                currentPosition,
                voxelConfig.ChunkSize,
                voxelConfig.MacroVoxelSize
            );

            if (!currentChunk.Equals(lastPlayerChunk))
            {
                if (debugLogging)
                {
                    Debug.Log($"Player moved to new chunk: {currentChunk}");
                }

                lastPlayerChunk = currentChunk;
            }

            lastPlayerPosition = currentPosition;
        }

        #endregion

        #region Streaming Evaluation

        private void EvaluateStreamingNeeds()
        {
            Vector3 playerPos = lastPlayerPosition;

            // Determine chunks that should be loaded
            HashSet<ChunkCoord> desiredChunks = GetChunksInRadius(playerPos, LOAD_DISTANCE);

            // Find chunks to load (desired but not active)
            foreach (ChunkCoord coord in desiredChunks)
            {
                if (!activeChunks.ContainsKey(coord) && !loadingChunks.Contains(coord))
                {
                    float3 chunkWorldPos = VoxelMath.ChunkCoordToWorld(
                        coord,
                        voxelConfig.ChunkSize,
                        voxelConfig.MacroVoxelSize
                    );
                    float distance = Vector3.Distance(playerPos, chunkWorldPos);
                    ChunkLoadRequest request = new ChunkLoadRequest(coord, distance);

                    loadQueue.Add(request);
                    loadingChunks.Add(coord);

                    if (debugLogging)
                    {
                        Debug.Log($"Queued chunk for loading: {coord} at distance {distance:F1}m");
                    }
                }
            }

            // Find chunks to unload (active but outside unload distance)
            List<ChunkCoord> chunksToUnload = new List<ChunkCoord>();

            foreach (var kvp in activeChunks)
            {
                ChunkCoord coord = kvp.Key;
                float3 chunkWorldPos = VoxelMath.ChunkCoordToWorld(
                    coord,
                    voxelConfig.ChunkSize,
                    voxelConfig.MacroVoxelSize
                );
                float halfChunkSize = voxelConfig.ChunkSize * voxelConfig.MacroVoxelSize * 0.5f;
                Vector3 chunkCenter = (Vector3)chunkWorldPos + new Vector3(
                    halfChunkSize,
                    halfChunkSize,
                    halfChunkSize
                );

                float distance = Vector3.Distance(playerPos, chunkCenter);

                if (distance > UNLOAD_DISTANCE)
                {
                    chunksToUnload.Add(coord);
                }
            }

            // Add to unload queue
            foreach (ChunkCoord coord in chunksToUnload)
            {
                unloadQueue.Add(coord);

                if (debugLogging)
                {
                    Debug.Log($"Queued chunk for unloading: {coord}");
                }
            }
        }

        private HashSet<ChunkCoord> GetChunksInRadius(Vector3 centerPos, float radius)
        {
            HashSet<ChunkCoord> chunks = new HashSet<ChunkCoord>();

            // Convert radius to chunk units
            float chunkWorldSize = voxelConfig.ChunkSize * voxelConfig.MacroVoxelSize;
            int chunkRadius = Mathf.CeilToInt(radius / chunkWorldSize);

            ChunkCoord centerChunk = VoxelMath.WorldToChunkCoord(
                centerPos,
                voxelConfig.ChunkSize,
                voxelConfig.MacroVoxelSize
            );

            // Iterate over chunk grid in radius
            for (int x = -chunkRadius; x <= chunkRadius; x++)
            {
                for (int y = -chunkRadius; y <= chunkRadius; y++)
                {
                    for (int z = -chunkRadius; z <= chunkRadius; z++)
                    {
                        ChunkCoord coord = new ChunkCoord(
                            centerChunk.X + x,
                            centerChunk.Y + y,
                            centerChunk.Z + z
                        );

                        float3 chunkWorldPos = VoxelMath.ChunkCoordToWorld(
                            coord,
                            voxelConfig.ChunkSize,
                            voxelConfig.MacroVoxelSize
                        );
                        float halfChunkWorldSize = chunkWorldSize * 0.5f;
                        Vector3 chunkCenter = (Vector3)chunkWorldPos + new Vector3(halfChunkWorldSize, halfChunkWorldSize, halfChunkWorldSize);

                        float distance = Vector3.Distance(centerPos, chunkCenter);

                        if (distance <= radius)
                        {
                            chunks.Add(coord);
                        }
                    }
                }
            }

            return chunks;
        }

        #endregion

        #region Load Queue Processing

        private void ProcessLoadQueue(float startTime)
        {
            int chunksLoadedThisFrame = 0;

            while (loadQueue.Count > 0 && chunksLoadedThisFrame < maxChunksPerFrame)
            {
                // Check time budget
                float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
                if (elapsed >= timeBudgetMs)
                {
                    if (debugLogging)
                    {
                        Debug.Log($"Load queue processing stopped: time budget reached ({elapsed:F2}ms)");
                    }
                    break;
                }

                // Get highest priority request
                ChunkLoadRequest request = loadQueue.Min;
                loadQueue.Remove(request);

                // Start loading coroutine
                StartCoroutine(LoadChunkAsync(request.Coordinate));

                chunksLoadedThisFrame++;
            }
        }

        /// <summary>
        /// Load chunk asynchronously (cache check → generation → meshing → instantiation).
        /// Respects per-frame budget.
        /// </summary>
        private IEnumerator LoadChunkAsync(ChunkCoord coord)
        {
            float startTime = Time.realtimeSinceStartup;

            // STEP 1: Try cache
            if (TryLoadFromCache(coord, startTime))
                yield break;

            // STEP 2: Create new chunk
            TerrainChunk chunk = CreateNewChunk(coord);

            // STEP 3: Generate voxel data
            yield return GenerateVoxelDataAsync(chunk);

            // STEP 4: Generate mesh
            yield return GenerateMeshAsync(chunk);

            // STEP 5: Cache and activate
            CacheAndActivateChunk(coord, chunk, startTime);
        }

        /// <summary>
        /// Try to load chunk from cache. Returns true if cache hit, false if miss.
        /// </summary>
        private bool TryLoadFromCache(ChunkCoord coord, float startTime)
        {
            if (chunkCache.TryGetChunk(coord, out TerrainChunk cachedChunk))
            {
                // Cache hit - activate directly
                cachedChunk.SetActive(true);
                activeChunks[coord] = cachedChunk;

                float loadTime = (Time.realtimeSinceStartup - startTime) * 1000f;

                if (debugLogging)
                    Debug.Log($"Chunk {coord} loaded from cache in {loadTime:F2}ms");

                loadingChunks.Remove(coord);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Create new TerrainChunk for procedural generation.
        /// </summary>
        private TerrainChunk CreateNewChunk(ChunkCoord coord)
        {
            TerrainChunk chunk = new TerrainChunk(
                coord,
                transform,
                chunkMaterial,
                voxelConfig.MacroVoxelSize
            );
            chunk.AllocateVoxelData(voxelConfig.ChunkSize);

            float3 worldPos = VoxelMath.ChunkCoordToWorld(
                coord,
                voxelConfig.ChunkSize,
                voxelConfig.MacroVoxelSize
            );
            chunk.SetWorldPosition(worldPos);

            return chunk;
        }

        /// <summary>
        /// Generate voxel data using ProceduralTerrainGenerationJob.
        /// </summary>
        private IEnumerator GenerateVoxelDataAsync(TerrainChunk chunk)
        {
            ProceduralTerrainGenerationJob genJob = new ProceduralTerrainGenerationJob
            {
                ChunkCoord = chunk.Coord,
                ChunkSize = voxelConfig.ChunkSize,
                VoxelSize = voxelConfig.MacroVoxelSize,
                Seed = voxelConfig.Seed,
                NoiseFrequency = voxelConfig.NoiseFrequency,
                NoiseOctaves = voxelConfig.NoiseOctaves,
                Lacunarity = NOISE_LACUNARITY,
                Persistence = NOISE_PERSISTENCE,
                TerrainOffsetY = 0f,
                Heightmap = new NativeArray<float>(),
                VoxelData = chunk.VoxelData
            };

            JobHandle genHandle = genJob.Schedule(chunk.VoxelData.Length, GENERATION_JOB_BATCH_SIZE);

            yield return new WaitUntil(() => genHandle.IsCompleted);

            genHandle.Complete();
            chunk.MarkGenerated();
        }

        /// <summary>
        /// Generate mesh using GreedyMeshingJob.
        /// </summary>
        private IEnumerator GenerateMeshAsync(TerrainChunk chunk)
        {
            // Allocate output containers
            NativeList<float3> vertices = new NativeList<float3>(Allocator.TempJob);
            NativeList<int> triangles = new NativeList<int>(Allocator.TempJob);
            NativeList<float3> normals = new NativeList<float3>(Allocator.TempJob);
            NativeList<float2> uvs = new NativeList<float2>(Allocator.TempJob);
            NativeList<float4> colors = new NativeList<float4>(Allocator.TempJob);

            // Temporary buffers for greedy algorithm
            int maskSize = voxelConfig.ChunkSize * voxelConfig.ChunkSize;
            NativeArray<bool> mask = new NativeArray<bool>(maskSize, Allocator.TempJob);
            NativeArray<VoxelType> maskVoxelTypes = new NativeArray<VoxelType>(maskSize, Allocator.TempJob);

            try
            {
                GreedyMeshingJob meshJob = new GreedyMeshingJob
                {
                    Voxels = chunk.VoxelData,
                    ChunkSize = voxelConfig.ChunkSize,
                    Vertices = vertices,
                    Triangles = triangles,
                    UVs = uvs,
                    Normals = normals,
                    Colors = colors,
                    Mask = mask,
                    MaskVoxelTypes = maskVoxelTypes
                };

                JobHandle meshHandle = meshJob.Schedule();

                yield return new WaitUntil(() => meshHandle.IsCompleted);

                meshHandle.Complete();

                // Apply mesh to chunk
                ApplyMeshToChunk(chunk, vertices, triangles, normals, uvs, colors);
            }
            finally
            {
                // Cleanup
                vertices.Dispose();
                triangles.Dispose();
                normals.Dispose();
                uvs.Dispose();
                colors.Dispose();
                mask.Dispose();
                maskVoxelTypes.Dispose();
            }
        }

        /// <summary>
        /// Apply generated mesh data to chunk.
        /// </summary>
        private void ApplyMeshToChunk(
            TerrainChunk chunk,
            NativeList<float3> vertices,
            NativeList<int> triangles,
            NativeList<float3> normals,
            NativeList<float2> uvs,
            NativeList<float4> colors)
        {
            if (vertices.Length == 0)
            {
                Debug.LogWarning($"Chunk {chunk.Coord} has 0 vertices (all air?)");
                return;
            }

            Mesh mesh = new Mesh
            {
                name = $"Chunk_{chunk.Coord.X}_{chunk.Coord.Y}_{chunk.Coord.Z}"
            };

            // Convert Mathematics types to Unity types
            Vector3[] unityVertices = ConvertFloat3ToVector3(vertices.AsArray());
            Vector3[] unityNormals = ConvertFloat3ToVector3(normals.AsArray());
            Vector2[] unityUVs = ConvertFloat2ToVector2(uvs.AsArray());
            Color[] unityColors = ConvertFloat4ToColor(colors.AsArray());

            mesh.SetVertices(unityVertices);
            mesh.SetTriangles(triangles.AsArray().ToArray(), 0);
            mesh.SetNormals(unityNormals);
            mesh.SetUVs(0, unityUVs);
            mesh.SetColors(unityColors);

            chunk.SetMesh(mesh);
        }

        /// <summary>
        /// Cache chunk and activate it.
        /// </summary>
        private void CacheAndActivateChunk(ChunkCoord coord, TerrainChunk chunk, float startTime)
        {
            // Store in cache for future reuse
            chunkCache.PutChunk(coord, chunk);

            // Add to active chunks
            activeChunks[coord] = chunk;

            // Mark loading complete
            loadingChunks.Remove(coord);

            float loadTime = (Time.realtimeSinceStartup - startTime) * 1000f;

            if (debugLogging)
                Debug.Log($"Chunk {coord} generated and loaded in {loadTime:F2}ms");
        }

        // Helper conversion methods
        private Vector3[] ConvertFloat3ToVector3(NativeArray<float3> source)
        {
            Vector3[] result = new Vector3[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = source[i];
            return result;
        }

        private Vector2[] ConvertFloat2ToVector2(NativeArray<float2> source)
        {
            Vector2[] result = new Vector2[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = source[i];
            return result;
        }

        private Color[] ConvertFloat4ToColor(NativeArray<float4> source)
        {
            Color[] result = new Color[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                float4 c = source[i];
                result[i] = new Color(c.x, c.y, c.z, c.w);
            }
            return result;
        }

        #endregion

        #region Unload Queue Processing

        private void ProcessUnloadQueue()
        {
            foreach (ChunkCoord coord in unloadQueue)
            {
                if (activeChunks.TryGetValue(coord, out TerrainChunk chunk))
                {
                    // Hide chunk but keep in cache
                    chunk.SetActive(false);

                    // Remove from active chunks
                    activeChunks.Remove(coord);

                    if (debugLogging)
                    {
                        Debug.Log($"Chunk {coord} unloaded");
                    }
                }

                // Remove from loading queue if pending
                loadingChunks.Remove(coord);
            }

            unloadQueue.Clear();
        }

        #endregion

        #region IEvictionHandler Implementation

        public void OnEvict(ChunkCoord key, TerrainChunk value)
        {
            if (debugLogging)
            {
                Debug.Log($"Chunk {key} evicted from cache");
            }

            // Dispose chunk resources (GameObject and native arrays)
            value?.Dispose();
        }

        #endregion

        #region Public API

        /// <summary>
        /// Number of chunks currently active (visible/loaded).
        /// </summary>
        public int ActiveChunkCount => activeChunks?.Count ?? 0;

        /// <summary>
        /// Number of chunks currently in the LRU cache (including inactive).
        /// </summary>
        public int CachedChunkCount => chunkCache?.Count ?? 0;

        /// <summary>
        /// Maximum capacity of the chunk cache.
        /// </summary>
        public int MaxCachedChunks => streamingConfig?.ChunkCacheCapacity ?? 0;

        /// <summary>
        /// Radius at which chunks are loaded (meters).
        /// </summary>
        public float LoadRadius => LOAD_DISTANCE;

        /// <summary>
        /// Radius at which chunks are unloaded (meters).
        /// </summary>
        public float UnloadRadius => UNLOAD_DISTANCE;

        /// <summary>
        /// Maximum number of chunks that can be loaded per frame.
        /// </summary>
        public int MaxChunksPerFrame => maxChunksPerFrame;

        /// <summary>
        /// Gets statistics about the streaming system.
        /// </summary>
        /// <returns>Formatted statistics string.</returns>
        public string GetStatistics()
        {
            if (chunkCache == null)
            {
                Debug.LogWarning("ChunkCache is not initialized. Did you call Awake()?");
                return "Streaming Statistics:\n" +
                       "  Active Chunks: 0\n" +
                       "  Load Queue: 0\n" +
                       "  Unload Queue: 0\n" +
                       "  Loading: 0\n" +
                       "  Cache Hits: 0\n" +
                       "  Cache Misses: 0\n" +
                       "  Hit Rate: 0.0%\n" +
                       "  Evictions: 0";
            }

            CacheStatistics stats = chunkCache.Statistics;

            return $"Streaming Statistics:\n" +
                   $"  Active Chunks: {activeChunks?.Count ?? 0}\n" +
                   $"  Load Queue: {loadQueue?.Count ?? 0}\n" +
                   $"  Unload Queue: {unloadQueue?.Count ?? 0}\n" +
                   $"  Loading: {loadingChunks?.Count ?? 0}\n" +
                   $"  Cache Hits: {stats.Hits}\n" +
                   $"  Cache Misses: {stats.Misses}\n" +
                   $"  Hit Rate: {stats.HitRate:P1}\n" +
                   $"  Evictions: {stats.Evictions}";
        }

        /// <summary>
        /// Clears all loaded chunks and cache.
        /// </summary>
        public void ClearAll()
        {
            // Unload all active chunks
            foreach (var chunk in activeChunks.Values)
            {
                chunk.Dispose();
            }
            activeChunks.Clear();

            // Clear queues
            loadQueue.Clear();
            unloadQueue.Clear();
            loadingChunks.Clear();

            // Clear cache
            chunkCache.Clear();

            if (debugLogging)
            {
                Debug.Log("All chunks and cache cleared");
            }
        }

        #endregion
    }
}
