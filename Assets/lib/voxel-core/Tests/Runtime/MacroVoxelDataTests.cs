using NUnit.Framework;

namespace TimeSurvivor.Voxel.Core.Tests
{
    /// <summary>
    /// Unit tests for MacroVoxelData struct.
    /// Tests solidity detection, transparency detection, and basic data storage.
    /// </summary>
    [TestFixture]
    public class MacroVoxelDataTests
    {
        #region IsSolid Tests

        [Test]
        public void Test_IsSolid_AirVoxel_ReturnsFalse()
        {
            // Arrange
            var voxel = new MacroVoxelData(VoxelType.Air);

            // Act
            bool result = voxel.IsSolid;

            // Assert
            Assert.That(result, Is.False, "Air voxels should not be solid");
        }

        [Test]
        public void Test_IsSolid_SolidVoxelTypes_ReturnsTrue()
        {
            // Arrange - Test multiple solid types
            var solidTypes = new[]
            {
                VoxelType.Grass,
                VoxelType.Dirt,
                VoxelType.Stone,
                VoxelType.Sand,
                VoxelType.Wood
            };

            // Act & Assert
            foreach (var type in solidTypes)
            {
                var voxel = new MacroVoxelData(type);
                Assert.That(voxel.IsSolid, Is.True,
                    $"{type} voxels should be solid");
            }
        }

        #endregion

        #region IsTransparent Tests

        [Test]
        public void Test_IsTransparent_WaterAndLeaves_ReturnsTrue()
        {
            // Arrange
            var waterVoxel = new MacroVoxelData(VoxelType.Water);
            var leavesVoxel = new MacroVoxelData(VoxelType.Leaves);

            // Act
            bool waterTransparent = waterVoxel.IsTransparent;
            bool leavesTransparent = leavesVoxel.IsTransparent;

            // Assert
            Assert.That(waterTransparent, Is.True, "Water should be transparent");
            Assert.That(leavesTransparent, Is.True, "Leaves should be transparent");
        }

        [Test]
        public void Test_IsTransparent_OpaqueVoxelTypes_ReturnsFalse()
        {
            // Arrange - Test opaque types
            var opaqueTypes = new[]
            {
                VoxelType.Air,
                VoxelType.Grass,
                VoxelType.Dirt,
                VoxelType.Stone,
                VoxelType.Sand,
                VoxelType.Wood
            };

            // Act & Assert
            foreach (var type in opaqueTypes)
            {
                var voxel = new MacroVoxelData(type);
                Assert.That(voxel.IsTransparent, Is.False,
                    $"{type} voxels should not be transparent");
            }
        }

        #endregion

        #region Constructor and Data Storage Tests

        [Test]
        public void Test_Constructor_DefaultMetadata_StoresTypeCorrectly()
        {
            // Arrange & Act
            var voxel = new MacroVoxelData(VoxelType.Stone);

            // Assert
            Assert.That(voxel.Type, Is.EqualTo(VoxelType.Stone),
                "Type should be stored correctly");
            Assert.That(voxel.Metadata, Is.EqualTo(0),
                "Metadata should default to 0");
        }

        [Test]
        public void Test_Constructor_WithMetadata_StoresBothValues()
        {
            // Arrange
            VoxelType expectedType = VoxelType.Grass;
            byte expectedMetadata = 42;

            // Act
            var voxel = new MacroVoxelData(expectedType, expectedMetadata);

            // Assert
            Assert.That(voxel.Type, Is.EqualTo(expectedType),
                "Type should be stored correctly");
            Assert.That(voxel.Metadata, Is.EqualTo(expectedMetadata),
                "Metadata should be stored correctly");
        }

        [Test]
        public void Test_Constructor_AllVoxelTypes_StoredCorrectly()
        {
            // Arrange - Test all voxel types
            var allTypes = new[]
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

            // Act & Assert
            foreach (var type in allTypes)
            {
                var voxel = new MacroVoxelData(type);
                Assert.That(voxel.Type, Is.EqualTo(type),
                    $"VoxelType {type} should be stored correctly");
            }
        }

        [Test]
        public void Test_Metadata_MaxValue_StoredCorrectly()
        {
            // Arrange
            byte maxMetadata = byte.MaxValue; // 255

            // Act
            var voxel = new MacroVoxelData(VoxelType.Stone, maxMetadata);

            // Assert
            Assert.That(voxel.Metadata, Is.EqualTo(maxMetadata),
                "Maximum metadata value should be stored correctly");
        }

        #endregion

        #region Combined Behavior Tests

        [Test]
        public void Test_WaterVoxel_IsSolidAndTransparent()
        {
            // Arrange
            var waterVoxel = new MacroVoxelData(VoxelType.Water);

            // Act & Assert
            Assert.That(waterVoxel.IsSolid, Is.True,
                "Water should be solid (has collision/rendering)");
            Assert.That(waterVoxel.IsTransparent, Is.True,
                "Water should be transparent");
        }

        [Test]
        public void Test_LeavesVoxel_IsSolidAndTransparent()
        {
            // Arrange
            var leavesVoxel = new MacroVoxelData(VoxelType.Leaves);

            // Act & Assert
            Assert.That(leavesVoxel.IsSolid, Is.True,
                "Leaves should be solid (has collision/rendering)");
            Assert.That(leavesVoxel.IsTransparent, Is.True,
                "Leaves should be transparent");
        }

        #endregion
    }
}
