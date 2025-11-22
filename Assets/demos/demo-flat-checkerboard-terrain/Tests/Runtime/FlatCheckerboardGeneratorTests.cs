using NUnit.Framework;
using Unity.Collections;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain.Tests
{
    /// <summary>
    /// Unit tests for FlatCheckerboardGenerator.
    /// Tests terrain generation logic, checkerboard pattern consistency, and edge cases.
    /// </summary>
    [TestFixture]
    public class FlatCheckerboardGeneratorTests
    {
        private FlatCheckerboardGenerator generator;
        private const int CHUNK_SIZE = 16; // Standard chunk size for tests
        private const int GROUND_HEIGHT = 8; // Expected ground height
        private const int TILE_SIZE = 8; // Checkerboard tile size

        [SetUp]
        public void SetUp()
        {
            generator = new FlatCheckerboardGenerator();
        }

        /// <summary>
        /// Test 1: Verify that Generate() creates a NativeArray with correct size.
        /// </summary>
        [Test]
        public void Generate_CreatesChunkWithCorrectSize()
        {
            // Arrange
            ChunkCoord coord = new ChunkCoord(0, 0, 0);
            int expectedSize = CHUNK_SIZE * CHUNK_SIZE * CHUNK_SIZE;

            // Act
            NativeArray<VoxelType> voxels = generator.Generate(coord, CHUNK_SIZE, Allocator.Temp);

            // Assert
            Assert.AreEqual(expectedSize, voxels.Length, "Generated chunk should have correct volume");

            // Cleanup
            voxels.Dispose();
        }

        /// <summary>
        /// Test 2: Verify flat terrain at correct height (Y < 8 solid, Y >= 8 air).
        /// </summary>
        [Test]
        public void Generate_CreatesFlatTerrainAtCorrectHeight()
        {
            // Arrange
            ChunkCoord coord = new ChunkCoord(0, 0, 0);

            // Act
            NativeArray<VoxelType> voxels = generator.Generate(coord, CHUNK_SIZE, Allocator.Temp);

            // Assert - Below ground height should be solid (Grass or Dirt)
            for (int y = 0; y < GROUND_HEIGHT; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    for (int x = 0; x < CHUNK_SIZE; x++)
                    {
                        int index = VoxelMath.Flatten3DIndex(x, y, z, CHUNK_SIZE);
                        VoxelType voxel = voxels[index];
                        Assert.IsTrue(voxel == VoxelType.Grass || voxel == VoxelType.Dirt,
                            $"Voxel at ({x},{y},{z}) should be solid (Grass or Dirt), but was {voxel}");
                    }
                }
            }

            // Assert - Above ground height should be air
            for (int y = GROUND_HEIGHT; y < CHUNK_SIZE; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z++)
                {
                    for (int x = 0; x < CHUNK_SIZE; x++)
                    {
                        int index = VoxelMath.Flatten3DIndex(x, y, z, CHUNK_SIZE);
                        VoxelType voxel = voxels[index];
                        Assert.AreEqual(VoxelType.Air, voxel,
                            $"Voxel at ({x},{y},{z}) should be Air, but was {voxel}");
                    }
                }
            }

            // Cleanup
            voxels.Dispose();
        }

        /// <summary>
        /// Test 3: Verify checkerboard pattern at specific coordinates at origin chunk.
        /// Tests corners of tiles at chunk (0,0,0).
        /// </summary>
        [Test]
        public void Generate_CreatesCheckerboardPattern_AtOrigin()
        {
            // Arrange
            ChunkCoord coord = new ChunkCoord(0, 0, 0);

            // Act
            NativeArray<VoxelType> voxels = generator.Generate(coord, CHUNK_SIZE, Allocator.Temp);

            // Assert - Test specific pattern positions
            // At (0,0): even tile X (0/8=0), even tile Z (0/8=0) → even == even → Grass
            int index_0_0 = VoxelMath.Flatten3DIndex(0, 0, 0, CHUNK_SIZE);
            Assert.AreEqual(VoxelType.Grass, voxels[index_0_0],
                "Position (0,0,0) should be Grass (even X tile, even Z tile)");

            // At (8,0): odd tile X (8/8=1), even tile Z (0/8=0) → odd != even → Dirt
            int index_8_0 = VoxelMath.Flatten3DIndex(8, 0, 0, CHUNK_SIZE);
            Assert.AreEqual(VoxelType.Dirt, voxels[index_8_0],
                "Position (8,0,0) should be Dirt (odd X tile, even Z tile)");

            // At (0,8): even tile X (0/8=0), odd tile Z (8/8=1) → even != odd → Dirt
            int index_0_8 = VoxelMath.Flatten3DIndex(0, 0, 8, CHUNK_SIZE);
            Assert.AreEqual(VoxelType.Dirt, voxels[index_0_8],
                "Position (0,0,8) should be Dirt (even X tile, odd Z tile)");

            // At (8,8): odd tile X (8/8=1), odd tile Z (8/8=1) → odd == odd → Grass
            int index_8_8 = VoxelMath.Flatten3DIndex(8, 0, 8, CHUNK_SIZE);
            Assert.AreEqual(VoxelType.Grass, voxels[index_8_8],
                "Position (8,0,8) should be Grass (odd X tile, odd Z tile)");

            // Cleanup
            voxels.Dispose();
        }

        /// <summary>
        /// Test 4: Verify checkerboard pattern is consistent across adjacent chunks.
        /// Tests boundary voxels between chunk (0,0,0) and chunk (1,0,0).
        /// </summary>
        [Test]
        public void Generate_CheckerboardPatternIsConsistent_AcrossChunks()
        {
            // Arrange
            ChunkCoord coord0 = new ChunkCoord(0, 0, 0);
            ChunkCoord coord1 = new ChunkCoord(1, 0, 0);

            // Act
            NativeArray<VoxelType> voxels0 = generator.Generate(coord0, CHUNK_SIZE, Allocator.Temp);
            NativeArray<VoxelType> voxels1 = generator.Generate(coord1, CHUNK_SIZE, Allocator.Temp);

            // Assert - Boundary consistency
            // Last voxel of chunk 0 at X=15 should match pattern expectation
            // World X = 0*16 + 15 = 15, tile index = 15/8 = 1 (odd)
            // First voxel of chunk 1 at X=0 should continue pattern
            // World X = 1*16 + 0 = 16, tile index = 16/8 = 2 (even)

            // At chunk 0, local (15, 0, 0): world (15, 0, 0) → tile X=1 (odd), tile Z=0 (even) → Dirt
            int index_chunk0_boundary = VoxelMath.Flatten3DIndex(15, 0, 0, CHUNK_SIZE);
            VoxelType voxel_chunk0 = voxels0[index_chunk0_boundary];

            // At chunk 1, local (0, 0, 0): world (16, 0, 0) → tile X=2 (even), tile Z=0 (even) → Grass
            int index_chunk1_boundary = VoxelMath.Flatten3DIndex(0, 0, 0, CHUNK_SIZE);
            VoxelType voxel_chunk1 = voxels1[index_chunk1_boundary];

            // They should be different (pattern switches at tile boundary)
            Assert.AreNotEqual(voxel_chunk0, voxel_chunk1,
                "Pattern should change across tile boundary between chunks");

            // Verify specific expected values
            Assert.AreEqual(VoxelType.Dirt, voxel_chunk0,
                "Chunk 0 boundary (world 15,0,0) should be Dirt");
            Assert.AreEqual(VoxelType.Grass, voxel_chunk1,
                "Chunk 1 boundary (world 16,0,0) should be Grass");

            // Cleanup
            voxels0.Dispose();
            voxels1.Dispose();
        }

        /// <summary>
        /// Test 5: Verify GetVoxelAt() returns consistent results with Generate().
        /// </summary>
        [Test]
        public void GetVoxelAt_ReturnsConsistentResults_WithGenerateMethod()
        {
            // Arrange
            ChunkCoord coord = new ChunkCoord(0, 0, 0);

            // Act
            NativeArray<VoxelType> voxels = generator.Generate(coord, CHUNK_SIZE, Allocator.Temp);

            // Assert - Test multiple positions
            for (int y = 0; y < CHUNK_SIZE; y++)
            {
                for (int z = 0; z < CHUNK_SIZE; z += 4) // Sample every 4th voxel to reduce test time
                {
                    for (int x = 0; x < CHUNK_SIZE; x += 4)
                    {
                        int worldX = coord.X * CHUNK_SIZE + x;
                        int worldY = coord.Y * CHUNK_SIZE + y;
                        int worldZ = coord.Z * CHUNK_SIZE + z;

                        int index = VoxelMath.Flatten3DIndex(x, y, z, CHUNK_SIZE);
                        VoxelType generatedVoxel = voxels[index];
                        VoxelType queriedVoxel = generator.GetVoxelAt(worldX, worldY, worldZ);

                        Assert.AreEqual(generatedVoxel, queriedVoxel,
                            $"GetVoxelAt({worldX},{worldY},{worldZ}) should match Generate() at local ({x},{y},{z})");
                    }
                }
            }

            // Cleanup
            voxels.Dispose();
        }

        /// <summary>
        /// Test 6: Verify GetVoxelAt() returns Air above ground height.
        /// </summary>
        [Test]
        public void GetVoxelAt_ReturnsAir_AboveGroundHeight()
        {
            // Arrange & Act & Assert
            for (int y = GROUND_HEIGHT; y < GROUND_HEIGHT + 10; y++)
            {
                VoxelType voxel = generator.GetVoxelAt(0, y, 0);
                Assert.AreEqual(VoxelType.Air, voxel,
                    $"GetVoxelAt(0,{y},0) should return Air above ground height");
            }
        }

        /// <summary>
        /// Test 7: Verify GetVoxelAt() returns solid voxel below ground height.
        /// </summary>
        [Test]
        public void GetVoxelAt_ReturnsSolidVoxel_BelowGroundHeight()
        {
            // Arrange & Act & Assert
            for (int y = 0; y < GROUND_HEIGHT; y++)
            {
                VoxelType voxel = generator.GetVoxelAt(0, y, 0);
                Assert.IsTrue(voxel == VoxelType.Grass || voxel == VoxelType.Dirt,
                    $"GetVoxelAt(0,{y},0) should return Grass or Dirt below ground height, but was {voxel}");
            }
        }

        /// <summary>
        /// Test 8: Verify checkerboard uses correct XOR pattern formula.
        /// Tests mathematical correctness: (X_tile % 2 == Z_tile % 2) → Grass, else Dirt.
        /// </summary>
        [Test]
        public void Generate_CheckerboardUsesXORPattern()
        {
            // Arrange
            ChunkCoord coord = new ChunkCoord(0, 0, 0);

            // Act
            NativeArray<VoxelType> voxels = generator.Generate(coord, CHUNK_SIZE, Allocator.Temp);

            // Assert - Test XOR pattern formula at various positions
            for (int z = 0; z < CHUNK_SIZE; z++)
            {
                for (int x = 0; x < CHUNK_SIZE; x++)
                {
                    int worldX = coord.X * CHUNK_SIZE + x;
                    int worldZ = coord.Z * CHUNK_SIZE + z;

                    bool isEvenTileX = ((worldX / TILE_SIZE) % 2) == 0;
                    bool isEvenTileZ = ((worldZ / TILE_SIZE) % 2) == 0;

                    VoxelType expectedVoxel = (isEvenTileX == isEvenTileZ) ? VoxelType.Grass : VoxelType.Dirt;

                    int index = VoxelMath.Flatten3DIndex(x, 0, z, CHUNK_SIZE);
                    VoxelType actualVoxel = voxels[index];

                    Assert.AreEqual(expectedVoxel, actualVoxel,
                        $"Voxel at world ({worldX},0,{worldZ}) should follow XOR pattern");
                }
            }

            // Cleanup
            voxels.Dispose();
        }
    }
}
