using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace TimeSurvivor.Voxel.Core.Tests
{
    /// <summary>
    /// Utility methods and helpers for voxel unit tests.
    /// Provides common functionality to reduce code duplication across test classes.
    /// </summary>
    public static class VoxelTestUtilities
    {
        #region Test Configuration Creation

        /// <summary>
        /// Creates a VoxelConfiguration ScriptableObject with default test settings.
        /// </summary>
        /// <param name="chunkSize">Size of chunk in voxels (default: 16)</param>
        /// <param name="macroVoxelSize">Macro voxel size in Unity units (default: 0.2f)</param>
        /// <param name="microVoxelSize">Micro voxel size in Unity units (default: 0.1f)</param>
        /// <returns>Configured VoxelConfiguration instance</returns>
        public static VoxelConfiguration CreateTestConfig(
            int chunkSize = 16,
            float macroVoxelSize = 0.2f,
            float microVoxelSize = 0.1f)
        {
            var config = ScriptableObject.CreateInstance<VoxelConfiguration>();
            config.ChunkSize = chunkSize;
            config.MacroVoxelSize = macroVoxelSize;
            config.MicroVoxelSize = microVoxelSize;
            config.MaxCachedChunks = 100;
            config.RenderDistance = 4;
            config.UseAmortizedMeshing = false; // Disable for tests
            config.UseBurstCompilation = true;
            config.UseJobSystem = true;
            config.Seed = 12345; // Deterministic seed
            return config;
        }

        #endregion

        #region Chunk Creation Helpers

        /// <summary>
        /// Creates a NativeArray for macro voxel chunk filled with Air.
        /// Caller is responsible for disposing the array.
        /// </summary>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <param name="allocator">Memory allocator (default: TempJob)</param>
        /// <returns>NativeArray of MacroVoxelData filled with Air</returns>
        public static NativeArray<MacroVoxelData> CreateEmptyMacroChunk(
            int chunkSize = 16,
            Allocator allocator = Allocator.TempJob)
        {
            int volume = chunkSize * chunkSize * chunkSize;
            var chunk = new NativeArray<MacroVoxelData>(volume, allocator);

            // Initialize all voxels to Air
            for (int i = 0; i < volume; i++)
            {
                chunk[i] = new MacroVoxelData(VoxelType.Air);
            }

            return chunk;
        }

        /// <summary>
        /// Creates a NativeArray for micro voxel chunk filled with Air.
        /// Caller is responsible for disposing the array.
        /// </summary>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <param name="allocator">Memory allocator (default: TempJob)</param>
        /// <returns>NativeArray of MicroVoxelData filled with Air</returns>
        public static NativeArray<MicroVoxelData> CreateEmptyMicroChunk(
            int chunkSize = 16,
            Allocator allocator = Allocator.TempJob)
        {
            int volume = chunkSize * chunkSize * chunkSize;
            var chunk = new NativeArray<MicroVoxelData>(volume, allocator);

            // Initialize all voxels to Air with 0 health
            for (int i = 0; i < volume; i++)
            {
                chunk[i] = new MicroVoxelData(VoxelType.Air, 0);
            }

            return chunk;
        }

        /// <summary>
        /// Creates a NativeArray for macro voxel chunk filled with a solid type.
        /// Caller is responsible for disposing the array.
        /// </summary>
        /// <param name="voxelType">Type to fill the chunk with</param>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <param name="allocator">Memory allocator (default: TempJob)</param>
        /// <returns>NativeArray of MacroVoxelData filled with specified type</returns>
        public static NativeArray<MacroVoxelData> CreateSolidMacroChunk(
            VoxelType voxelType,
            int chunkSize = 16,
            Allocator allocator = Allocator.TempJob)
        {
            int volume = chunkSize * chunkSize * chunkSize;
            var chunk = new NativeArray<MacroVoxelData>(volume, allocator);

            // Fill all voxels with specified type
            for (int i = 0; i < volume; i++)
            {
                chunk[i] = new MacroVoxelData(voxelType);
            }

            return chunk;
        }

        /// <summary>
        /// Creates a test chunk with a pattern useful for debugging:
        /// - Bottom layer (y=0): Stone
        /// - Middle layers: Dirt
        /// - Top layer (y=chunkSize-1): Grass
        /// </summary>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <param name="allocator">Memory allocator (default: TempJob)</param>
        /// <returns>NativeArray with layered pattern</returns>
        public static NativeArray<MacroVoxelData> CreateLayeredMacroChunk(
            int chunkSize = 16,
            Allocator allocator = Allocator.TempJob)
        {
            int volume = chunkSize * chunkSize * chunkSize;
            var chunk = new NativeArray<MacroVoxelData>(volume, allocator);

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);
                        VoxelType type;

                        if (y == 0)
                            type = VoxelType.Stone;
                        else if (y == chunkSize - 1)
                            type = VoxelType.Grass;
                        else
                            type = VoxelType.Dirt;

                        chunk[index] = new MacroVoxelData(type);
                    }
                }
            }

            return chunk;
        }

        #endregion

        #region Mock Implementations

        /// <summary>
        /// Simple mock implementation of IVoxelGenerator for testing.
        /// Generates flat terrain at y=0 with Stone, and Air above.
        /// </summary>
        public class MockVoxelGenerator : IVoxelGenerator
        {
            private readonly int groundLevel;

            public MockVoxelGenerator(int groundLevel = 0)
            {
                this.groundLevel = groundLevel;
            }

            public NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator)
            {
                int volume = chunkSize * chunkSize * chunkSize;
                var voxels = new NativeArray<VoxelType>(volume, allocator);

                for (int y = 0; y < chunkSize; y++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        for (int z = 0; z < chunkSize; z++)
                        {
                            int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);
                            int worldY = coord.Y * chunkSize + y;

                            if (worldY <= groundLevel)
                                voxels[index] = VoxelType.Stone;
                            else
                                voxels[index] = VoxelType.Air;
                        }
                    }
                }

                return voxels;
            }

            public VoxelType GetVoxelAt(int worldX, int worldY, int worldZ)
            {
                if (worldY <= groundLevel)
                    return VoxelType.Stone;
                else
                    return VoxelType.Air;
            }
        }

        #endregion

        #region Assertion Helpers

        /// <summary>
        /// Checks if two float values are approximately equal within Unity's default epsilon.
        /// </summary>
        /// <param name="a">First value</param>
        /// <param name="b">Second value</param>
        /// <param name="epsilon">Tolerance (default: 0.0001f)</param>
        /// <returns>True if values are approximately equal</returns>
        public static bool ApproximatelyEqual(float a, float b, float epsilon = 0.0001f)
        {
            return Mathf.Abs(a - b) < epsilon;
        }

        /// <summary>
        /// Checks if two float3 vectors are approximately equal component-wise.
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <param name="epsilon">Tolerance (default: 0.0001f)</param>
        /// <returns>True if vectors are approximately equal</returns>
        public static bool ApproximatelyEqual(float3 a, float3 b, float epsilon = 0.0001f)
        {
            return ApproximatelyEqual(a.x, b.x, epsilon) &&
                   ApproximatelyEqual(a.y, b.y, epsilon) &&
                   ApproximatelyEqual(a.z, b.z, epsilon);
        }

        /// <summary>
        /// Checks if two int3 vectors are exactly equal.
        /// </summary>
        /// <param name="a">First vector</param>
        /// <param name="b">Second vector</param>
        /// <returns>True if vectors are exactly equal</returns>
        public static bool ExactlyEqual(int3 a, int3 b)
        {
            return a.x == b.x && a.y == b.y && a.z == b.z;
        }

        /// <summary>
        /// Validates that a NativeArray chunk has the expected size.
        /// </summary>
        /// <param name="chunk">Chunk to validate</param>
        /// <param name="expectedChunkSize">Expected chunk size in voxels</param>
        /// <returns>True if chunk has correct volume</returns>
        public static bool IsValidChunkSize<T>(NativeArray<T> chunk, int expectedChunkSize) where T : struct
        {
            int expectedVolume = expectedChunkSize * expectedChunkSize * expectedChunkSize;
            return chunk.Length == expectedVolume;
        }

        #endregion

        #region Data Generation Helpers

        /// <summary>
        /// Generates a deterministic sequence of VoxelTypes for testing.
        /// Cycles through all available voxel types.
        /// </summary>
        /// <param name="index">Index in sequence</param>
        /// <returns>VoxelType at this index</returns>
        public static VoxelType GetTestVoxelType(int index)
        {
            var types = new[]
            {
                VoxelType.Air,
                VoxelType.Grass,
                VoxelType.Dirt,
                VoxelType.Stone,
                VoxelType.Sand,
                VoxelType.Water,
                VoxelType.Wood,
                VoxelType.Leaves
            };

            return types[index % types.Length];
        }

        /// <summary>
        /// Creates a deterministic pattern of voxels useful for mesh testing.
        /// Creates a checkerboard pattern of solid and air voxels.
        /// </summary>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <param name="allocator">Memory allocator</param>
        /// <returns>Checkerboard pattern chunk</returns>
        public static NativeArray<MacroVoxelData> CreateCheckerboardChunk(
            int chunkSize = 16,
            Allocator allocator = Allocator.TempJob)
        {
            int volume = chunkSize * chunkSize * chunkSize;
            var chunk = new NativeArray<MacroVoxelData>(volume, allocator);

            for (int y = 0; y < chunkSize; y++)
            {
                for (int x = 0; x < chunkSize; x++)
                {
                    for (int z = 0; z < chunkSize; z++)
                    {
                        int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);
                        bool isSolid = (x + y + z) % 2 == 0;
                        chunk[index] = new MacroVoxelData(
                            isSolid ? VoxelType.Stone : VoxelType.Air
                        );
                    }
                }
            }

            return chunk;
        }

        #endregion

        #region Performance Testing Helpers

        /// <summary>
        /// Measures execution time of an action in milliseconds.
        /// Useful for performance benchmarking in tests.
        /// </summary>
        /// <param name="action">Action to measure</param>
        /// <returns>Execution time in milliseconds</returns>
        public static float MeasureExecutionTime(Action action)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            return (float)stopwatch.Elapsed.TotalMilliseconds;
        }

        #endregion
    }
}
