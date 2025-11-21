using System;
using Unity.Collections;
using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Generates a 2D heightmap for Minecraft-style terrain using Simplex noise.
    /// Heightmap is stored as NativeArray for efficient access during chunk generation.
    /// Resolution: WorldSizeX × ChunkSize × WorldSizeZ × ChunkSize (one height value per X-Z column).
    ///
    /// Example: For 10×10 chunks at ChunkSize=64:
    ///   Resolution = 10×64 × 10×64 = 640×640 = 409,600 floats (~1.56 MB)
    ///
    /// Thread-safe and Burst-compatible for use in Unity Jobs.
    /// Must call Dispose() to free native memory.
    /// </summary>
    public class MinecraftHeightmapGenerator : IDisposable
    {
        private readonly int _worldSizeX;
        private readonly int _worldSizeZ;
        private readonly int _chunkSize;
        private readonly int _baseTerrainHeight;
        private readonly int _terrainVariation;
        private readonly float _heightmapFrequency;
        private readonly int _heightmapOctaves;
        private readonly int _seed;

        private NativeArray<float> _heightmap;
        private bool _isGenerated;
        private bool _isDisposed;

        /// <summary>
        /// Width of heightmap in voxels (WorldSizeX × ChunkSize).
        /// </summary>
        public int HeightmapWidth => _worldSizeX * _chunkSize;

        /// <summary>
        /// Height of heightmap in voxels (WorldSizeZ × ChunkSize).
        /// </summary>
        public int HeightmapHeight => _worldSizeZ * _chunkSize;

        /// <summary>
        /// Total heightmap elements (Width × Height).
        /// </summary>
        public int HeightmapSize => HeightmapWidth * HeightmapHeight;

        /// <summary>
        /// Check if heightmap has been generated.
        /// </summary>
        public bool IsGenerated => _isGenerated;

        /// <summary>
        /// Get read-only access to heightmap data.
        /// </summary>
        public NativeArray<float> Heightmap
        {
            get
            {
                if (!_isGenerated)
                    throw new InvalidOperationException("Heightmap not generated. Call GenerateHeightmap() first.");
                if (_isDisposed)
                    throw new ObjectDisposedException(nameof(MinecraftHeightmapGenerator));
                return _heightmap;
            }
        }

        /// <summary>
        /// Constructor. Does NOT generate heightmap automatically - call GenerateHeightmap().
        /// </summary>
        /// <param name="worldSizeX">World width in chunks</param>
        /// <param name="worldSizeZ">World depth in chunks</param>
        /// <param name="chunkSize">Chunk size in voxels (e.g., 64)</param>
        /// <param name="baseTerrainHeight">Base terrain height in voxels</param>
        /// <param name="terrainVariation">Terrain height variation ± in voxels</param>
        /// <param name="heightmapFrequency">Noise frequency (lower = larger features)</param>
        /// <param name="heightmapOctaves">Number of noise octaves (more = more detail)</param>
        /// <param name="seed">Random seed for deterministic generation</param>
        public MinecraftHeightmapGenerator(
            int worldSizeX,
            int worldSizeZ,
            int chunkSize,
            int baseTerrainHeight,
            int terrainVariation,
            float heightmapFrequency,
            int heightmapOctaves,
            int seed)
        {
            _worldSizeX = worldSizeX;
            _worldSizeZ = worldSizeZ;
            _chunkSize = chunkSize;
            _baseTerrainHeight = baseTerrainHeight;
            _terrainVariation = terrainVariation;
            _heightmapFrequency = heightmapFrequency;
            _heightmapOctaves = heightmapOctaves;
            _seed = seed;

            _isGenerated = false;
            _isDisposed = false;
        }

        /// <summary>
        /// Generate the 2D heightmap using Simplex noise.
        /// Allocates NativeArray - remember to call Dispose() when done.
        /// </summary>
        public void GenerateHeightmap()
        {
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MinecraftHeightmapGenerator));

            if (_isGenerated)
            {
                // Already generated - dispose old data first
                _heightmap.Dispose();
            }

            // Allocate heightmap
            int totalElements = HeightmapSize;
            _heightmap = new NativeArray<float>(totalElements, Allocator.Persistent);

            // Generate heightmap using Simplex noise
            int width = HeightmapWidth;
            int height = HeightmapHeight;

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Sample 2D Simplex noise (Y=0 for 2D)
                    float noiseValue = SimplexNoise3D.MultiOctave(
                        x, 0f, z,
                        _seed,
                        _heightmapFrequency,
                        _heightmapOctaves,
                        2.0f, // Lacunarity (ADR-007 default)
                        0.5f  // Persistence (ADR-007 default)
                    );

                    // Map noise [-1, 1] to terrain height [baseHeight - variation, baseHeight + variation]
                    // Example: baseHeight=192, variation=128, noise=0.5
                    //   terrainHeight = 192 + (0.5 × 128) = 256 voxels
                    float terrainHeight = _baseTerrainHeight + (noiseValue * _terrainVariation);

                    // Store in heightmap (row-major order: X changes fastest)
                    int index = z * width + x;
                    _heightmap[index] = terrainHeight;
                }
            }

            _isGenerated = true;
        }

        /// <summary>
        /// Get terrain height at a specific world voxel coordinate.
        /// Clamps coordinates to heightmap bounds.
        /// </summary>
        /// <param name="worldVoxelX">World X coordinate in voxels</param>
        /// <param name="worldVoxelZ">World Z coordinate in voxels</param>
        /// <returns>Terrain height in voxels at that X-Z position</returns>
        public float GetHeightAt(int worldVoxelX, int worldVoxelZ)
        {
            if (!_isGenerated)
                throw new InvalidOperationException("Heightmap not generated. Call GenerateHeightmap() first.");
            if (_isDisposed)
                throw new ObjectDisposedException(nameof(MinecraftHeightmapGenerator));

            // Clamp to heightmap bounds
            int width = HeightmapWidth;
            int height = HeightmapHeight;
            int x = math.clamp(worldVoxelX, 0, width - 1);
            int z = math.clamp(worldVoxelZ, 0, height - 1);

            // Lookup in heightmap
            int index = z * width + x;
            return _heightmap[index];
        }

        /// <summary>
        /// Dispose of native memory. MUST be called to avoid memory leaks.
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed)
                return;

            if (_isGenerated && _heightmap.IsCreated)
            {
                _heightmap.Dispose();
            }

            _isDisposed = true;
            _isGenerated = false;
        }

        ~MinecraftHeightmapGenerator()
        {
            // Finalizer warns about memory leak if Dispose() was not called
            if (_isGenerated && !_isDisposed)
            {
                UnityEngine.Debug.LogWarning($"[MinecraftHeightmapGenerator] Memory leak detected! " +
                                             $"Dispose() was not called. Heightmap size: {HeightmapSize} floats.");
            }
        }
    }
}
