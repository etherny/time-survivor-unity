using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TMPro;
using System.Diagnostics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Rendering;

namespace TimeSurvivor.Demos.GreedyMeshing
{
    /// <summary>
    /// Main controller for the Greedy Meshing demonstration.
    /// Orchestrates voxel generation and meshing for various patterns.
    /// </summary>
    public class GreedyMeshingDemo : MonoBehaviour
    {
        [Header("Configuration")]
        [Range(8, 64)]
        public int ChunkSize = 32;

        [Header("UI References")]
        public TextMeshProUGUI FpsText;
        public TextMeshProUGUI VertexCountText;
        public TextMeshProUGUI MeshingTimeText;
        public TextMeshProUGUI PatternText;

        [Header("Rendering")]
        public MeshFilter MeshFilter;
        public MeshRenderer MeshRenderer;

        private float _fpsTimer;
        private int _frameCount;
        private float _fps;

        private void Start()
        {
            // Generate default terrain at startup
            GenerateTerrain();
        }

        private void Update()
        {
            // Update FPS counter
            _frameCount++;
            _fpsTimer += Time.deltaTime;

            if (_fpsTimer >= 1f)
            {
                _fps = _frameCount / _fpsTimer;
                if (FpsText != null)
                {
                    FpsText.text = $"FPS: {_fps:F1}";
                }
                _frameCount = 0;
                _fpsTimer = 0f;
            }
        }

        /// <summary>
        /// Generates a single cube at the center of the chunk.
        /// Expects 24 vertices (6 faces × 4 vertices).
        /// </summary>
        public void GenerateSingleCube()
        {
            int3 centerPos = new int3(ChunkSize / 2, ChunkSize / 2, ChunkSize / 2);
            NativeArray<VoxelType> voxels = CreateSingleCube(ChunkSize, centerPos, VoxelType.Stone);
            GenerateMesh(voxels, "Single Cube");
        }

        /// <summary>
        /// Generates a flat 16×16 plane at Y=0.
        /// Tests optimal greedy meshing fusion.
        /// </summary>
        public void GenerateFlatPlane()
        {
            NativeArray<VoxelType> voxels = CreateFlatPlane(ChunkSize, 16, 16);
            GenerateMesh(voxels, "Flat Plane 16×16");
        }

        /// <summary>
        /// Generates realistic procedural terrain using heightmap.
        /// Fill ratio: ~28%
        /// </summary>
        public void GenerateTerrain()
        {
            NativeArray<VoxelType> voxels = CreateProceduralTerrain(ChunkSize);
            GenerateMesh(voxels, "Procedural Terrain");
        }

        /// <summary>
        /// Generates a 3D checkerboard pattern (worst-case for greedy meshing).
        /// </summary>
        public void GenerateCheckerboard()
        {
            NativeArray<VoxelType> voxels = CreateCheckerboard(ChunkSize);
            GenerateMesh(voxels, "Checkerboard (Worst Case)");
        }

        /// <summary>
        /// Generates a random voxel pattern (~20% fill ratio).
        /// </summary>
        public void GenerateRandom()
        {
            NativeArray<VoxelType> voxels = CreateRandomPattern(ChunkSize, 0.2f);
            GenerateMesh(voxels, "Random Pattern (~20%)");
        }

        /// <summary>
        /// Executes the GreedyMeshingJob and updates the mesh.
        /// </summary>
        private void GenerateMesh(NativeArray<VoxelType> voxels, string patternName)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            // Allocate output buffers
            var vertices = new NativeList<float3>(Allocator.TempJob);
            var triangles = new NativeList<int>(Allocator.TempJob);
            var uvs = new NativeList<float2>(Allocator.TempJob);
            var normals = new NativeList<float3>(Allocator.TempJob);
            var colors = new NativeList<float4>(Allocator.TempJob);

            // Allocate temporary mask buffers
            int maskSize = ChunkSize * ChunkSize;
            var mask = new NativeArray<bool>(maskSize, Allocator.TempJob);
            var maskVoxelTypes = new NativeArray<VoxelType>(maskSize, Allocator.TempJob);

            try
            {
                // Create and execute GreedyMeshingJob
                var job = new GreedyMeshingJob
                {
                    Voxels = voxels,
                    ChunkSize = ChunkSize,
                    Vertices = vertices,
                    Triangles = triangles,
                    UVs = uvs,
                    Normals = normals,
                    Colors = colors,
                    Mask = mask,
                    MaskVoxelTypes = maskVoxelTypes
                };

                job.Run();

                stopwatch.Stop();

                // Build Unity mesh using MeshBuilder
                Mesh mesh = MeshBuilder.BuildMesh(vertices, triangles, uvs, normals, colors);

                // Apply translation to center chunk at origin
                Vector3[] verticesArray = mesh.vertices;
                Vector3 offset = new Vector3(-ChunkSize / 2f, 0f, -ChunkSize / 2f);
                for (int i = 0; i < verticesArray.Length; i++)
                {
                    verticesArray[i] += offset;
                }
                mesh.vertices = verticesArray;
                mesh.RecalculateBounds();

                // Apply mesh to renderer
                if (MeshFilter != null)
                {
                    MeshFilter.mesh = mesh;
                }

                // Update UI
                if (VertexCountText != null)
                {
                    VertexCountText.text = $"Vertices: {mesh.vertexCount}";
                }

                if (MeshingTimeText != null)
                {
                    MeshingTimeText.text = $"Meshing Time: {stopwatch.Elapsed.TotalMilliseconds:F2}ms";
                }

                if (PatternText != null)
                {
                    PatternText.text = $"Pattern: {patternName}";
                }
            }
            finally
            {
                // Always dispose all native containers
                if (voxels.IsCreated) voxels.Dispose();
                if (vertices.IsCreated) vertices.Dispose();
                if (triangles.IsCreated) triangles.Dispose();
                if (uvs.IsCreated) uvs.Dispose();
                if (normals.IsCreated) normals.Dispose();
                if (colors.IsCreated) colors.Dispose();
                if (mask.IsCreated) mask.Dispose();
                if (maskVoxelTypes.IsCreated) maskVoxelTypes.Dispose();
            }
        }

        #region Helper Methods

        /// <summary>
        /// Creates an empty chunk filled with Air voxels.
        /// </summary>
        private NativeArray<VoxelType> CreateEmptyChunk(int chunkSize)
        {
            int totalVoxels = chunkSize * chunkSize * chunkSize;
            var voxels = new NativeArray<VoxelType>(totalVoxels, Allocator.TempJob);

            for (int i = 0; i < totalVoxels; i++)
            {
                voxels[i] = VoxelType.Air;
            }

            return voxels;
        }

        /// <summary>
        /// Creates a chunk with a single voxel at specified position.
        /// </summary>
        private NativeArray<VoxelType> CreateSingleCube(int chunkSize, int3 position, VoxelType type)
        {
            var voxels = CreateEmptyChunk(chunkSize);
            int index = VoxelMath.Flatten3DIndex(position.x, position.y, position.z, chunkSize);
            voxels[index] = type;
            return voxels;
        }

        /// <summary>
        /// Creates a flat plane of Grass voxels at Y=0.
        /// </summary>
        private NativeArray<VoxelType> CreateFlatPlane(int chunkSize, int width, int depth)
        {
            var voxels = CreateEmptyChunk(chunkSize);

            int startX = (chunkSize - width) / 2;
            int startZ = (chunkSize - depth) / 2;

            for (int x = startX; x < startX + width; x++)
            {
                for (int z = startZ; z < startZ + depth; z++)
                {
                    int index = VoxelMath.Flatten3DIndex(x, 0, z, chunkSize);
                    voxels[index] = VoxelType.Grass;
                }
            }

            return voxels;
        }

        /// <summary>
        /// Creates procedural terrain using heightmap with Perlin noise.
        /// </summary>
        private NativeArray<VoxelType> CreateProceduralTerrain(int chunkSize)
        {
            var voxels = CreateEmptyChunk(chunkSize);

            float scale = 0.1f;
            int baseHeight = chunkSize / 4;
            int maxHeight = chunkSize / 2;

            for (int x = 0; x < chunkSize; x++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    // Generate heightmap using Perlin noise
                    float noise = Mathf.PerlinNoise(x * scale, z * scale);
                    int height = baseHeight + Mathf.FloorToInt(noise * maxHeight);
                    height = Mathf.Clamp(height, 0, chunkSize - 1);

                    // Fill column up to height
                    for (int y = 0; y <= height; y++)
                    {
                        int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);

                        if (y == height)
                        {
                            voxels[index] = VoxelType.Grass;
                        }
                        else if (y >= height - 3)
                        {
                            voxels[index] = VoxelType.Dirt;
                        }
                        else
                        {
                            voxels[index] = VoxelType.Stone;
                        }
                    }
                }
            }

            return voxels;
        }

        /// <summary>
        /// Creates a 3D checkerboard pattern (worst-case for greedy meshing).
        /// </summary>
        private NativeArray<VoxelType> CreateCheckerboard(int chunkSize)
        {
            var voxels = CreateEmptyChunk(chunkSize);

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        bool isChecked = (x + y + z) % 2 == 0;
                        if (isChecked)
                        {
                            int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);
                            voxels[index] = VoxelType.Stone;
                        }
                    }
                }
            }

            return voxels;
        }

        /// <summary>
        /// Creates a random voxel pattern with specified fill ratio.
        /// </summary>
        private NativeArray<VoxelType> CreateRandomPattern(int chunkSize, float fillRatio)
        {
            var voxels = CreateEmptyChunk(chunkSize);

            System.Random random = new System.Random(42); // Fixed seed for reproducibility

            for (int x = 0; x < chunkSize; x++)
            {
                for (int y = 0; y < chunkSize; y++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        if (random.NextDouble() < fillRatio)
                        {
                            int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);

                            // Random voxel type
                            int typeValue = random.Next(1, 4); // Stone, Dirt, or Grass
                            voxels[index] = (VoxelType)typeValue;
                        }
                    }
                }
            }

            return voxels;
        }

        #endregion

        private void OnDestroy()
        {
            // Cleanup is handled in GenerateMesh via try-finally
        }
    }
}
