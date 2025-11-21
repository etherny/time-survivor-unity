using NUnit.Framework;
using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Core.Tests
{
    /// <summary>
    /// Unit tests for VoxelMath static utility class.
    /// Tests coordinate conversions, indexing, and validation methods.
    /// </summary>
    [TestFixture]
    public class VoxelMathTests
    {
        private const float EPSILON = 0.0001f;

        #region WorldToChunkCoord Tests

        [Test]
        public void Test_WorldToChunkCoord_PositiveCoordinates_ReturnsCorrectChunk()
        {
            // Arrange
            float3 worldPos = new float3(10f, 5f, 15f);
            int chunkSize = 16;
            float voxelSize = 0.2f;
            // Chunk world size = 16 * 0.2 = 3.2 units
            // Expected chunk: (3, 1, 4) because 10/3.2=3.125, 5/3.2=1.56, 15/3.2=4.68

            // Act
            ChunkCoord result = VoxelMath.WorldToChunkCoord(worldPos, chunkSize, voxelSize);

            // Assert
            Assert.That(result.X, Is.EqualTo(3), "X chunk coordinate should be 3");
            Assert.That(result.Y, Is.EqualTo(1), "Y chunk coordinate should be 1");
            Assert.That(result.Z, Is.EqualTo(4), "Z chunk coordinate should be 4");
        }

        [Test]
        public void Test_WorldToChunkCoord_NegativeCoordinates_ReturnsCorrectChunk()
        {
            // Arrange
            float3 worldPos = new float3(-5f, -10f, -2f);
            int chunkSize = 16;
            float voxelSize = 0.2f;
            // Chunk world size = 3.2 units
            // Expected: floor(-5/3.2)=-2, floor(-10/3.2)=-4, floor(-2/3.2)=-1

            // Act
            ChunkCoord result = VoxelMath.WorldToChunkCoord(worldPos, chunkSize, voxelSize);

            // Assert
            Assert.That(result.X, Is.EqualTo(-2), "Negative X should floor correctly");
            Assert.That(result.Y, Is.EqualTo(-4), "Negative Y should floor correctly");
            Assert.That(result.Z, Is.EqualTo(-1), "Negative Z should floor correctly");
        }

        [Test]
        public void Test_WorldToChunkCoord_ChunkBoundary_ReturnsCorrectChunk()
        {
            // Arrange - Test exactly at chunk boundary
            int chunkSize = 16;
            float voxelSize = 0.2f;
            float chunkWorldSize = chunkSize * voxelSize; // 3.2
            float3 worldPos = new float3(chunkWorldSize, 0f, 0f); // Exactly at boundary

            // Act
            ChunkCoord result = VoxelMath.WorldToChunkCoord(worldPos, chunkSize, voxelSize);

            // Assert
            Assert.That(result.X, Is.EqualTo(1), "Boundary position should be in next chunk");
            Assert.That(result.Y, Is.EqualTo(0));
            Assert.That(result.Z, Is.EqualTo(0));
        }

        #endregion

        #region ChunkCoordToWorld Tests

        [Test]
        public void Test_ChunkCoordToWorld_PositiveChunk_ReturnsCorrectWorldPos()
        {
            // Arrange
            ChunkCoord chunkCoord = new ChunkCoord(2, 3, 4);
            int chunkSize = 16;
            float voxelSize = 0.2f;
            // Expected: (2*3.2, 3*3.2, 4*3.2) = (6.4, 9.6, 12.8)

            // Act
            float3 result = VoxelMath.ChunkCoordToWorld(chunkCoord, chunkSize, voxelSize);

            // Assert
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.x, 6.4f, EPSILON), Is.True,
                "X world position should be 6.4");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.y, 9.6f, EPSILON), Is.True,
                "Y world position should be 9.6");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.z, 12.8f, EPSILON), Is.True,
                "Z world position should be 12.8");
        }

        [Test]
        public void Test_ChunkCoordToWorld_NegativeChunk_ReturnsCorrectWorldPos()
        {
            // Arrange
            ChunkCoord chunkCoord = new ChunkCoord(-1, -2, 0);
            int chunkSize = 16;
            float voxelSize = 0.2f;
            // Expected: (-1*3.2, -2*3.2, 0*3.2) = (-3.2, -6.4, 0.0)

            // Act
            float3 result = VoxelMath.ChunkCoordToWorld(chunkCoord, chunkSize, voxelSize);

            // Assert
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.x, -3.2f, EPSILON), Is.True,
                "Negative X should convert correctly");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.y, -6.4f, EPSILON), Is.True,
                "Negative Y should convert correctly");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.z, 0f, EPSILON), Is.True,
                "Zero Z should be 0");
        }

        [Test]
        public void Test_ChunkCoordToWorld_RoundTrip_ReturnsOriginalChunk()
        {
            // Arrange
            ChunkCoord original = new ChunkCoord(5, -3, 7);
            int chunkSize = 16;
            float voxelSize = 0.2f;

            // Act - Convert to world and back
            float3 worldPos = VoxelMath.ChunkCoordToWorld(original, chunkSize, voxelSize);
            ChunkCoord result = VoxelMath.WorldToChunkCoord(worldPos, chunkSize, voxelSize);

            // Assert
            Assert.That(result.X, Is.EqualTo(original.X), "Round-trip X should match");
            Assert.That(result.Y, Is.EqualTo(original.Y), "Round-trip Y should match");
            Assert.That(result.Z, Is.EqualTo(original.Z), "Round-trip Z should match");
        }

        #endregion

        #region VoxelCoordToWorld Tests

        [Test]
        public void Test_VoxelCoordToWorld_ReturnsVoxelCenter()
        {
            // Arrange
            int3 voxelCoord = new int3(10, 20, 30);
            float voxelSize = 0.2f;
            // Expected: (10.5*0.2, 20.5*0.2, 30.5*0.2) = (2.1, 4.1, 6.1)

            // Act
            float3 result = VoxelMath.VoxelCoordToWorld(voxelCoord, voxelSize);

            // Assert
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.x, 2.1f, EPSILON), Is.True,
                "X should be at voxel center");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.y, 4.1f, EPSILON), Is.True,
                "Y should be at voxel center");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.z, 6.1f, EPSILON), Is.True,
                "Z should be at voxel center");
        }

        [Test]
        public void Test_VoxelCoordToWorld_OriginVoxel_ReturnsHalfVoxelSize()
        {
            // Arrange
            int3 voxelCoord = new int3(0, 0, 0);
            float voxelSize = 0.2f;
            // Expected: (0.5*0.2, 0.5*0.2, 0.5*0.2) = (0.1, 0.1, 0.1)

            // Act
            float3 result = VoxelMath.VoxelCoordToWorld(voxelCoord, voxelSize);

            // Assert
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.x, 0.1f, EPSILON), Is.True,
                "Origin voxel center X should be 0.1");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.y, 0.1f, EPSILON), Is.True,
                "Origin voxel center Y should be 0.1");
            Assert.That(VoxelTestUtilities.ApproximatelyEqual(result.z, 0.1f, EPSILON), Is.True,
                "Origin voxel center Z should be 0.1");
        }

        #endregion

        #region Flatten3DIndex Tests

        [Test]
        public void Test_Flatten3DIndex_Origin_ReturnsZero()
        {
            // Arrange
            int x = 0, y = 0, z = 0;
            int chunkSize = 16;

            // Act
            int result = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);

            // Assert
            Assert.That(result, Is.EqualTo(0), "Origin coordinate should flatten to index 0");
        }

        [Test]
        public void Test_Flatten3DIndex_XYZOrdering_IsCorrect()
        {
            // Arrange
            int chunkSize = 16;

            // Act - Test ordering: X changes fastest, then Y, then Z
            // Formula: x + y*chunkSize + z*chunkSize*chunkSize
            int index_1_0_0 = VoxelMath.Flatten3DIndex(1, 0, 0, chunkSize);
            int index_0_1_0 = VoxelMath.Flatten3DIndex(0, 1, 0, chunkSize);
            int index_0_0_1 = VoxelMath.Flatten3DIndex(0, 0, 1, chunkSize);

            // Assert
            Assert.That(index_1_0_0, Is.EqualTo(1), "X=1 should be index 1 (X changes fastest)");
            Assert.That(index_0_1_0, Is.EqualTo(16), "Y=1 should be index 16 (Y * chunkSize)");
            Assert.That(index_0_0_1, Is.EqualTo(256), "Z=1 should be index 256 (Z * chunkSize * chunkSize)");
        }

        [Test]
        public void Test_Flatten3DIndex_MaxCoordinate_ReturnsLastIndex()
        {
            // Arrange
            int chunkSize = 16;
            int max = chunkSize - 1;

            // Act
            int result = VoxelMath.Flatten3DIndex(max, max, max, chunkSize);

            // Assert
            int expectedLastIndex = chunkSize * chunkSize * chunkSize - 1; // 4095 for 16^3
            Assert.That(result, Is.EqualTo(expectedLastIndex),
                "Maximum coordinates should flatten to last index");
        }

        #endregion

        #region Unflatten3DIndex Tests

        [Test]
        public void Test_Unflatten3DIndex_Zero_ReturnsOrigin()
        {
            // Arrange
            int index = 0;
            int chunkSize = 16;

            // Act
            int3 result = VoxelMath.Unflatten3DIndex(index, chunkSize);

            // Assert
            Assert.That(VoxelTestUtilities.ExactlyEqual(result, new int3(0, 0, 0)), Is.True,
                "Index 0 should unflatten to origin (0,0,0)");
        }

        [Test]
        public void Test_Unflatten3DIndex_RoundTrip_ReturnsOriginalCoordinate()
        {
            // Arrange
            int chunkSize = 16;
            int3 originalCoord = new int3(5, 7, 11);

            // Act
            int flatIndex = VoxelMath.Flatten3DIndex(originalCoord.x, originalCoord.y, originalCoord.z, chunkSize);
            int3 result = VoxelMath.Unflatten3DIndex(flatIndex, chunkSize);

            // Assert
            Assert.That(VoxelTestUtilities.ExactlyEqual(result, originalCoord), Is.True,
                "Round-trip flatten/unflatten should return original coordinate");
        }

        [Test]
        public void Test_Unflatten3DIndex_LastIndex_ReturnsMaxCoordinates()
        {
            // Arrange
            int chunkSize = 16;
            int lastIndex = chunkSize * chunkSize * chunkSize - 1; // 4095

            // Act
            int3 result = VoxelMath.Unflatten3DIndex(lastIndex, chunkSize);

            // Assert
            int max = chunkSize - 1;
            Assert.That(VoxelTestUtilities.ExactlyEqual(result, new int3(max, max, max)), Is.True,
                "Last index should unflatten to maximum coordinates");
        }

        #endregion

        #region IsValidLocalCoord Tests

        [Test]
        public void Test_IsValidLocalCoord_WithinBounds_ReturnsTrue()
        {
            // Arrange
            int3 validCoord = new int3(8, 10, 5);
            int chunkSize = 16;

            // Act
            bool result = VoxelMath.IsValidLocalCoord(validCoord, chunkSize);

            // Assert
            Assert.That(result, Is.True, "Coordinate within bounds should be valid");
        }

        [Test]
        public void Test_IsValidLocalCoord_OutOfBounds_ReturnsFalse()
        {
            // Arrange
            int chunkSize = 16;
            var invalidCoords = new[]
            {
                new int3(-1, 5, 5),   // Negative X
                new int3(5, -1, 5),   // Negative Y
                new int3(5, 5, -1),   // Negative Z
                new int3(16, 5, 5),   // X = chunkSize
                new int3(5, 16, 5),   // Y = chunkSize
                new int3(5, 5, 16),   // Z = chunkSize
                new int3(20, 5, 5)    // X > chunkSize
            };

            // Act & Assert
            foreach (var coord in invalidCoords)
            {
                bool result = VoxelMath.IsValidLocalCoord(coord, chunkSize);
                Assert.That(result, Is.False,
                    $"Coordinate {coord} should be invalid for chunkSize {chunkSize}");
            }
        }

        [Test]
        public void Test_IsValidLocalCoord_BoundaryValues_ReturnsCorrectly()
        {
            // Arrange
            int chunkSize = 16;
            int3 minValid = new int3(0, 0, 0);
            int3 maxValid = new int3(15, 15, 15);

            // Act
            bool minResult = VoxelMath.IsValidLocalCoord(minValid, chunkSize);
            bool maxResult = VoxelMath.IsValidLocalCoord(maxValid, chunkSize);

            // Assert
            Assert.That(minResult, Is.True, "Minimum boundary (0,0,0) should be valid");
            Assert.That(maxResult, Is.True, "Maximum boundary (15,15,15) should be valid");
        }

        #endregion

        #region Additional Tests

        [Test]
        public void Test_WorldToVoxelCoord_PositiveCoordinates_ReturnsCorrect()
        {
            // Arrange
            float3 worldPos = new float3(1.5f, 2.0f, 3.7f);
            float voxelSize = 0.2f;
            // Expected: floor(1.5/0.2)=7, floor(2.0/0.2)=10, floor(3.7/0.2)=18

            // Act
            int3 result = VoxelMath.WorldToVoxelCoord(worldPos, voxelSize);

            // Assert
            Assert.That(result.x, Is.EqualTo(7), "X voxel coordinate should be 7");
            Assert.That(result.y, Is.EqualTo(10), "Y voxel coordinate should be 10");
            Assert.That(result.z, Is.EqualTo(18), "Z voxel coordinate should be 18");
        }

        [Test]
        public void Test_ChunkManhattanDistance_ReturnsCorrectDistance()
        {
            // Arrange
            ChunkCoord a = new ChunkCoord(0, 0, 0);
            ChunkCoord b = new ChunkCoord(3, 4, 5);

            // Act
            int distance = VoxelMath.ChunkManhattanDistance(a, b);

            // Assert
            Assert.That(distance, Is.EqualTo(12), "Manhattan distance should be |3|+|4|+|5| = 12");
        }

        [Test]
        public void Test_ChunkDistanceSquared_ReturnsCorrectSquaredDistance()
        {
            // Arrange
            ChunkCoord a = new ChunkCoord(0, 0, 0);
            ChunkCoord b = new ChunkCoord(3, 4, 0);

            // Act
            int distanceSquared = VoxelMath.ChunkDistanceSquared(a, b);

            // Assert
            Assert.That(distanceSquared, Is.EqualTo(25), "Squared distance should be 3^2 + 4^2 = 25");
        }

        [Test]
        public void Test_VoxelToLocalCoord_PositiveGlobalCoord_ReturnsCorrectLocal()
        {
            // Arrange
            int3 globalCoord = new int3(18, 35, 50);
            int chunkSize = 16;
            // Expected: 18%16=2, 35%16=3, 50%16=2

            // Act
            int3 result = VoxelMath.VoxelToLocalCoord(globalCoord, chunkSize);

            // Assert
            Assert.That(result.x, Is.EqualTo(2), "Local X should be 2");
            Assert.That(result.y, Is.EqualTo(3), "Local Y should be 3");
            Assert.That(result.z, Is.EqualTo(2), "Local Z should be 2");
        }

        [Test]
        public void Test_VoxelToLocalCoord_NegativeGlobalCoord_ReturnsCorrectLocal()
        {
            // Arrange
            int3 globalCoord = new int3(-5, -18, -1);
            int chunkSize = 16;
            // For negative coords, modulo wraps: -5%16=-5+16=11, -18%16=-2+16=14, -1%16=-1+16=15

            // Act
            int3 result = VoxelMath.VoxelToLocalCoord(globalCoord, chunkSize);

            // Assert
            Assert.That(result.x, Is.EqualTo(11), "Negative local X should wrap correctly");
            Assert.That(result.y, Is.EqualTo(14), "Negative local Y should wrap correctly");
            Assert.That(result.z, Is.EqualTo(15), "Negative local Z should wrap correctly");
        }

        #endregion
    }
}
