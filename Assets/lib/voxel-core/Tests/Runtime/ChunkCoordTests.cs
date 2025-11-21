using NUnit.Framework;
using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Core.Tests
{
    /// <summary>
    /// Unit tests for ChunkCoord struct.
    /// Tests constructors, equality, operators, and hash code functionality.
    /// </summary>
    [TestFixture]
    public class ChunkCoordTests
    {
        #region Constructor Tests

        [Test]
        public void Test_Constructor_IntParameters_CreatesCorrectCoord()
        {
            // Arrange
            int x = 5, y = 10, z = 15;

            // Act
            var coord = new ChunkCoord(x, y, z);

            // Assert
            Assert.That(coord.X, Is.EqualTo(x), "X coordinate should match input");
            Assert.That(coord.Y, Is.EqualTo(y), "Y coordinate should match input");
            Assert.That(coord.Z, Is.EqualTo(z), "Z coordinate should match input");
            Assert.That(coord.Value.x, Is.EqualTo(x), "Value.x should match input");
            Assert.That(coord.Value.y, Is.EqualTo(y), "Value.y should match input");
            Assert.That(coord.Value.z, Is.EqualTo(z), "Value.z should match input");
        }

        [Test]
        public void Test_Constructor_Int3Parameter_CreatesCorrectCoord()
        {
            // Arrange
            int3 value = new int3(7, 14, 21);

            // Act
            var coord = new ChunkCoord(value);

            // Assert
            Assert.That(coord.X, Is.EqualTo(value.x), "X should match input");
            Assert.That(coord.Y, Is.EqualTo(value.y), "Y should match input");
            Assert.That(coord.Z, Is.EqualTo(value.z), "Z should match input");
            Assert.That(VoxelTestUtilities.ExactlyEqual(coord.Value, value), Is.True,
                "Value should exactly match input int3");
        }

        #endregion

        #region Equality Tests

        [Test]
        public void Test_Equals_SameCoordinates_ReturnsTrue()
        {
            // Arrange
            var coord1 = new ChunkCoord(3, 6, 9);
            var coord2 = new ChunkCoord(3, 6, 9);

            // Act
            bool result = coord1.Equals(coord2);

            // Assert
            Assert.That(result, Is.True, "ChunkCoords with same values should be equal");
        }

        [Test]
        public void Test_Equals_DifferentCoordinates_ReturnsFalse()
        {
            // Arrange
            var coord1 = new ChunkCoord(1, 2, 3);
            var coord2 = new ChunkCoord(1, 2, 4); // Different Z

            // Act
            bool result = coord1.Equals(coord2);

            // Assert
            Assert.That(result, Is.False, "ChunkCoords with different values should not be equal");
        }

        [Test]
        public void Test_EqualsOperator_SameCoordinates_ReturnsTrue()
        {
            // Arrange
            var coord1 = new ChunkCoord(10, 20, 30);
            var coord2 = new ChunkCoord(10, 20, 30);

            // Act
            bool result = coord1 == coord2;

            // Assert
            Assert.That(result, Is.True, "== operator should return true for equal coords");
        }

        [Test]
        public void Test_NotEqualsOperator_DifferentCoordinates_ReturnsTrue()
        {
            // Arrange
            var coord1 = new ChunkCoord(5, 5, 5);
            var coord2 = new ChunkCoord(6, 5, 5); // Different X

            // Act
            bool result = coord1 != coord2;

            // Assert
            Assert.That(result, Is.True, "!= operator should return true for different coords");
        }

        #endregion

        #region Operator Tests

        [Test]
        public void Test_AdditionOperator_PositiveOffset_ReturnsCorrectCoord()
        {
            // Arrange
            var coord = new ChunkCoord(10, 20, 30);
            int3 offset = new int3(5, -3, 7);

            // Act
            var result = coord + offset;

            // Assert
            Assert.That(result.X, Is.EqualTo(15), "X should be 10 + 5 = 15");
            Assert.That(result.Y, Is.EqualTo(17), "Y should be 20 + (-3) = 17");
            Assert.That(result.Z, Is.EqualTo(37), "Z should be 30 + 7 = 37");
        }

        [Test]
        public void Test_SubtractionOperator_PositiveOffset_ReturnsCorrectCoord()
        {
            // Arrange
            var coord = new ChunkCoord(10, 20, 30);
            int3 offset = new int3(5, 3, 7);

            // Act
            var result = coord - offset;

            // Assert
            Assert.That(result.X, Is.EqualTo(5), "X should be 10 - 5 = 5");
            Assert.That(result.Y, Is.EqualTo(17), "Y should be 20 - 3 = 17");
            Assert.That(result.Z, Is.EqualTo(23), "Z should be 30 - 7 = 23");
        }

        #endregion

        #region Additional Tests

        [Test]
        public void Test_GetHashCode_EqualCoords_ReturnsSameHash()
        {
            // Arrange
            var coord1 = new ChunkCoord(100, 200, 300);
            var coord2 = new ChunkCoord(100, 200, 300);

            // Act
            int hash1 = coord1.GetHashCode();
            int hash2 = coord2.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2),
                "Equal ChunkCoords should produce the same hash code");
        }

        [Test]
        public void Test_ToString_ReturnsFormattedString()
        {
            // Arrange
            var coord = new ChunkCoord(42, 84, 126);

            // Act
            string result = coord.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("ChunkCoord(42, 84, 126)"),
                "ToString should return formatted coordinate string");
        }

        [Test]
        public void Test_Constructor_NegativeCoordinates_WorksCorrectly()
        {
            // Arrange & Act
            var coord = new ChunkCoord(-5, -10, -15);

            // Assert
            Assert.That(coord.X, Is.EqualTo(-5), "Negative X should be preserved");
            Assert.That(coord.Y, Is.EqualTo(-10), "Negative Y should be preserved");
            Assert.That(coord.Z, Is.EqualTo(-15), "Negative Z should be preserved");
        }

        [Test]
        public void Test_Operators_NegativeCoordinates_WorksCorrectly()
        {
            // Arrange
            var coord = new ChunkCoord(-10, -20, -30);
            int3 offset = new int3(5, 10, 15);

            // Act
            var added = coord + offset;
            var subtracted = coord - offset;

            // Assert
            Assert.That(added.X, Is.EqualTo(-5), "Addition with negative coords should work");
            Assert.That(added.Y, Is.EqualTo(-10));
            Assert.That(added.Z, Is.EqualTo(-15));

            Assert.That(subtracted.X, Is.EqualTo(-15), "Subtraction with negative coords should work");
            Assert.That(subtracted.Y, Is.EqualTo(-30));
            Assert.That(subtracted.Z, Is.EqualTo(-45));
        }

        [Test]
        public void Test_Equals_ObjectOverload_WorksCorrectly()
        {
            // Arrange
            var coord1 = new ChunkCoord(1, 2, 3);
            object coord2 = new ChunkCoord(1, 2, 3);
            object notCoord = "not a coord";

            // Act & Assert
            Assert.That(coord1.Equals(coord2), Is.True,
                "Equals(object) should work for matching ChunkCoord");
            Assert.That(coord1.Equals(notCoord), Is.False,
                "Equals(object) should return false for non-ChunkCoord objects");
            Assert.That(coord1.Equals(null), Is.False,
                "Equals(object) should return false for null");
        }

        #endregion
    }
}
