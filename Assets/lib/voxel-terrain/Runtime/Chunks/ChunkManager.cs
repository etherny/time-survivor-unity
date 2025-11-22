using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Rendering;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Manages terrain chunks: loading, unloading, generation, and meshing.
    /// Implements IChunkManager interface from voxel-core.
    /// </summary>
    public class ChunkManager : IChunkManager<TerrainChunk>
    {
        private readonly VoxelConfiguration _config;
        private readonly Dictionary<ChunkCoord, TerrainChunk> _chunks;
        private readonly Queue<ChunkCoord> _generationQueue;
        private readonly Queue<ChunkCoord> _meshingQueue;
        private readonly Transform _chunkParent;
        private readonly Material _chunkMaterial;
        private readonly IVoxelGenerator _customGenerator;

        public ChunkManager(VoxelConfiguration config, Transform chunkParent = null, Material chunkMaterial = null, IVoxelGenerator customGenerator = null)
        {
            _config = config;
            _chunks = new Dictionary<ChunkCoord, TerrainChunk>();
            _generationQueue = new Queue<ChunkCoord>();
            _meshingQueue = new Queue<ChunkCoord>();

            // Create parent GameObject for organizing chunks
            if (chunkParent == null)
            {
                var parentObj = new GameObject("TerrainChunks");
                _chunkParent = parentObj.transform;
            }
            else
            {
                _chunkParent = chunkParent;
            }

            _chunkMaterial = chunkMaterial;
            _customGenerator = customGenerator;
        }

        /// <summary>
        /// Load a chunk at the specified coordinate.
        /// Queues for generation if not already loaded.
        /// </summary>
        public void LoadChunk(ChunkCoord coord)
        {
            if (_chunks.ContainsKey(coord))
                return;

            // Create chunk object with voxel size for proper scaling
            var chunk = new TerrainChunk(coord, _chunkParent, _chunkMaterial, _config.MacroVoxelSize);
            chunk.AllocateVoxelData(_config.ChunkSize);

            // Set world position
            float3 worldPos = VoxelMath.ChunkCoordToWorld(coord, _config.ChunkSize, _config.MacroVoxelSize);
            chunk.SetWorldPosition(worldPos);

            _chunks[coord] = chunk;

            // Queue for generation
            _generationQueue.Enqueue(coord);
        }

        /// <summary>
        /// Unload a chunk at the specified coordinate.
        /// Disposes of resources.
        /// </summary>
        public void UnloadChunk(ChunkCoord coord)
        {
            if (_chunks.TryGetValue(coord, out var chunk))
            {
                chunk.Dispose();
                _chunks.Remove(coord);
            }
        }

        /// <summary>
        /// Check if a chunk is currently loaded.
        /// </summary>
        public bool IsChunkLoaded(ChunkCoord coord)
        {
            return _chunks.ContainsKey(coord);
        }

        /// <summary>
        /// Check if a chunk exists at the specified coordinate (alias for IsChunkLoaded).
        /// </summary>
        public bool HasChunk(ChunkCoord coord)
        {
            return IsChunkLoaded(coord);
        }

        /// <summary>
        /// Get the number of currently active (loaded) chunks.
        /// </summary>
        public int ActiveChunkCount => _chunks.Count;

        /// <summary>
        /// Mark a chunk as dirty (needs remeshing).
        /// </summary>
        public void MarkDirty(ChunkCoord coord)
        {
            if (_chunks.TryGetValue(coord, out var chunk))
            {
                chunk.IsDirty = true;
                _meshingQueue.Enqueue(coord);
            }
        }

        /// <summary>
        /// Get chunk at coordinate (IChunkManager implementation).
        /// </summary>
        public TerrainChunk GetChunk(ChunkCoord coord)
        {
            return _chunks.TryGetValue(coord, out var chunk) ? chunk : null;
        }

        /// <summary>
        /// Get chunk at coordinate (alias for compatibility).
        /// </summary>
        public TerrainChunk GetTerrainChunk(ChunkCoord coord)
        {
            return GetChunk(coord);
        }

        /// <summary>
        /// Process chunk generation queue (call from Update).
        /// Generates up to MaxChunksLoadedPerFrame chunks per frame.
        /// </summary>
        public void ProcessGenerationQueue()
        {
            int processed = 0;
            while (_generationQueue.Count > 0 && processed < _config.MaxChunksLoadedPerFrame)
            {
                ChunkCoord coord = _generationQueue.Dequeue();

                if (_chunks.TryGetValue(coord, out var chunk))
                {
                    GenerateChunk(chunk);
                    processed++;
                }
            }
        }

        /// <summary>
        /// Process chunk meshing queue (call from Update).
        /// Uses amortized meshing if enabled in config.
        /// </summary>
        public void ProcessMeshingQueue(float deltaTime)
        {
            float startTime = Time.realtimeSinceStartup;
            float timeLimit = _config.UseAmortizedMeshing ? _config.MaxMeshingTimePerFrameMs / 1000f : float.MaxValue;

            while (_meshingQueue.Count > 0)
            {
                float elapsed = Time.realtimeSinceStartup - startTime;
                if (elapsed > timeLimit)
                    break;

                ChunkCoord coord = _meshingQueue.Dequeue();

                if (_chunks.TryGetValue(coord, out var chunk))
                {
                    MeshChunk(chunk);
                }
            }
        }

        /// <summary>
        /// Generate voxel data for a chunk using procedural generation or custom generator.
        /// </summary>
        private void GenerateChunk(TerrainChunk chunk)
        {
            if (_customGenerator != null)
            {
                // Use custom generator
                var generatedData = _customGenerator.Generate(chunk.Coord, _config.ChunkSize, Allocator.Temp);
                NativeArray<VoxelType>.Copy(generatedData, chunk.VoxelData);
                generatedData.Dispose();
            }
            else
            {
                // Use default procedural generation
                int totalVoxels = _config.ChunkSize * _config.ChunkSize * _config.ChunkSize;
                var job = new ProceduralTerrainGenerationJob
                {
                    ChunkCoord = chunk.Coord,
                    ChunkSize = _config.ChunkSize,
                    VoxelSize = _config.MacroVoxelSize,
                    Seed = _config.Seed == 0 ? UnityEngine.Random.Range(1, 1000000) : _config.Seed,
                    NoiseFrequency = _config.NoiseFrequency,
                    NoiseOctaves = _config.NoiseOctaves,
                    Lacunarity = 2.0f, // ADR-007 default
                    Persistence = 0.5f, // ADR-007 default
                    VoxelData = chunk.VoxelData
                };

                // Schedule parallel job with batch size 64 (ADR-007 recommended)
                var handle = job.Schedule(totalVoxels, 64);
                handle.Complete(); // TODO: Make async with job handles
            }

            chunk.MarkGenerated();

            // Queue for meshing
            _meshingQueue.Enqueue(chunk.Coord);
        }

        /// <summary>
        /// Generate mesh for a chunk using greedy meshing.
        /// </summary>
        private void MeshChunk(TerrainChunk chunk)
        {
            if (!chunk.IsGenerated)
                return;

            // Allocate output buffers
            var vertices = new NativeList<float3>(Allocator.TempJob);
            var triangles = new NativeList<int>(Allocator.TempJob);
            var uvs = new NativeList<float2>(Allocator.TempJob);
            var normals = new NativeList<float3>(Allocator.TempJob);
            var colors = new NativeList<float4>(Allocator.TempJob);
            var mask = new NativeArray<bool>(_config.ChunkSize * _config.ChunkSize, Allocator.TempJob);
            var maskVoxelTypes = new NativeArray<VoxelType>(_config.ChunkSize * _config.ChunkSize, Allocator.TempJob);

            // Create meshing job
            var job = new GreedyMeshingJob
            {
                Voxels = chunk.VoxelData,
                ChunkSize = _config.ChunkSize,
                Vertices = vertices,
                Triangles = triangles,
                UVs = uvs,
                Normals = normals,
                Colors = colors,
                Mask = mask,
                MaskVoxelTypes = maskVoxelTypes
            };

            // Schedule and complete
            var handle = job.Schedule();
            handle.Complete();

            // Build mesh
            Mesh mesh = MeshBuilder.BuildMesh(vertices, triangles, uvs, normals, colors);
            chunk.SetMesh(mesh);

            // Cleanup
            vertices.Dispose();
            triangles.Dispose();
            uvs.Dispose();
            normals.Dispose();
            colors.Dispose();
            mask.Dispose();
            maskVoxelTypes.Dispose();
        }

        /// <summary>
        /// Get all currently loaded chunks.
        /// </summary>
        public IEnumerable<TerrainChunk> GetAllChunks()
        {
            return _chunks.Values;
        }

        /// <summary>
        /// Get number of chunks waiting for generation.
        /// </summary>
        public int GenerationQueueCount => _generationQueue.Count;

        /// <summary>
        /// Get number of chunks waiting for meshing.
        /// </summary>
        public int MeshingQueueCount => _meshingQueue.Count;

        /// <summary>
        /// Dispose of all chunks and cleanup.
        /// </summary>
        public void Dispose()
        {
            foreach (var chunk in _chunks.Values)
            {
                chunk.Dispose();
            }
            _chunks.Clear();
            _generationQueue.Clear();
            _meshingQueue.Clear();
        }
    }
}
