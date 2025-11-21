using NUnit.Framework;

namespace TimeSurvivor.Voxel.Core.Tests
{
    /// <summary>
    /// Unit tests for MicroVoxelData struct.
    /// Tests destructible behavior, health management, and immutability.
    /// </summary>
    [TestFixture]
    public class MicroVoxelDataTests
    {
        #region IsSolid Tests

        [Test]
        public void Test_IsSolid_AirVoxel_ReturnsFalse()
        {
            // Arrange
            var voxel = new MicroVoxelData(VoxelType.Air, 100);

            // Act
            bool result = voxel.IsSolid;

            // Assert
            Assert.That(result, Is.False, "Air voxels should never be solid, regardless of health");
        }

        [Test]
        public void Test_IsSolid_HealthyVoxel_ReturnsTrue()
        {
            // Arrange
            var voxel = new MicroVoxelData(VoxelType.Stone, 100);

            // Act
            bool result = voxel.IsSolid;

            // Assert
            Assert.That(result, Is.True, "Voxels with health > 0 should be solid");
        }

        [Test]
        public void Test_IsSolid_ZeroHealth_ReturnsFalse()
        {
            // Arrange
            var voxel = new MicroVoxelData(VoxelType.Stone, 0);

            // Act
            bool result = voxel.IsSolid;

            // Assert
            Assert.That(result, Is.False, "Voxels with 0 health should not be solid");
        }

        #endregion

        #region IsDestroyed Tests

        [Test]
        public void Test_IsDestroyed_ZeroHealth_ReturnsTrue()
        {
            // Arrange
            var voxel = new MicroVoxelData(VoxelType.Wood, 0);

            // Act
            bool result = voxel.IsDestroyed;

            // Assert
            Assert.That(result, Is.True, "Voxels with 0 health should be destroyed");
        }

        [Test]
        public void Test_IsDestroyed_PositiveHealth_ReturnsFalse()
        {
            // Arrange
            var voxel = new MicroVoxelData(VoxelType.Wood, 1);

            // Act
            bool result = voxel.IsDestroyed;

            // Assert
            Assert.That(result, Is.False, "Voxels with health > 0 should not be destroyed");
        }

        #endregion

        #region WithDamage Tests - Immutability

        [Test]
        public void Test_WithDamage_PartialDamage_ReturnsNewVoxelWithReducedHealth()
        {
            // Arrange
            var original = new MicroVoxelData(VoxelType.Stone, 100);
            byte damageAmount = 30;

            // Act
            var damaged = original.WithDamage(damageAmount);

            // Assert
            Assert.That(damaged.Health, Is.EqualTo(70),
                "Health should be reduced by damage amount");
            Assert.That(damaged.Type, Is.EqualTo(VoxelType.Stone),
                "Type should remain the same");
            Assert.That(original.Health, Is.EqualTo(100),
                "Original voxel should be unchanged (immutability)");
        }

        [Test]
        public void Test_WithDamage_ExactDamage_ConvertsToAir()
        {
            // Arrange
            var original = new MicroVoxelData(VoxelType.Wood, 50);
            byte damageAmount = 50;

            // Act
            var damaged = original.WithDamage(damageAmount);

            // Assert
            Assert.That(damaged.Type, Is.EqualTo(VoxelType.Air),
                "Voxel should become Air when health reaches exactly 0");
            Assert.That(damaged.Health, Is.EqualTo(0),
                "Health should be 0");
            Assert.That(damaged.IsDestroyed, Is.True,
                "Voxel should be marked as destroyed");
            Assert.That(original.Health, Is.EqualTo(50),
                "Original voxel should be unchanged (immutability)");
        }

        [Test]
        public void Test_WithDamage_Overkill_ConvertsToAirWithZeroHealth()
        {
            // Arrange
            var original = new MicroVoxelData(VoxelType.Dirt, 20);
            byte damageAmount = 100; // More than current health

            // Act
            var damaged = original.WithDamage(damageAmount);

            // Assert
            Assert.That(damaged.Type, Is.EqualTo(VoxelType.Air),
                "Voxel should become Air when overkilled");
            Assert.That(damaged.Health, Is.EqualTo(0),
                "Health should be 0, not negative");
            Assert.That(damaged.IsDestroyed, Is.True,
                "Voxel should be marked as destroyed");
            Assert.That(original.Health, Is.EqualTo(20),
                "Original voxel should be unchanged (immutability)");
        }

        [Test]
        public void Test_WithDamage_PreservesMetadata()
        {
            // Arrange
            byte metadata = 42;
            var original = new MicroVoxelData(VoxelType.Stone, 100, metadata);
            byte damageAmount = 30;

            // Act
            var damaged = original.WithDamage(damageAmount);

            // Assert
            Assert.That(damaged.Metadata, Is.EqualTo(metadata),
                "Metadata should be preserved after damage");
        }

        [Test]
        public void Test_WithDamage_MetadataPreservedOnDestruction()
        {
            // Arrange
            byte metadata = 99;
            var original = new MicroVoxelData(VoxelType.Wood, 10, metadata);
            byte damageAmount = 50; // Overkill

            // Act
            var damaged = original.WithDamage(damageAmount);

            // Assert
            Assert.That(damaged.Type, Is.EqualTo(VoxelType.Air),
                "Type should be Air after destruction");
            Assert.That(damaged.Metadata, Is.EqualTo(metadata),
                "Metadata should be preserved even when converted to Air");
        }

        #endregion

        #region Constructor Tests

        [Test]
        public void Test_Constructor_DefaultHealth_SetsMaxHealth()
        {
            // Arrange & Act
            var voxel = new MicroVoxelData(VoxelType.Stone);

            // Assert
            Assert.That(voxel.Health, Is.EqualTo(255),
                "Default health should be maximum (255)");
            Assert.That(voxel.Type, Is.EqualTo(VoxelType.Stone),
                "Type should be stored correctly");
        }

        [Test]
        public void Test_Constructor_CustomHealth_StoresCorrectly()
        {
            // Arrange
            byte customHealth = 127;

            // Act
            var voxel = new MicroVoxelData(VoxelType.Wood, customHealth);

            // Assert
            Assert.That(voxel.Health, Is.EqualTo(customHealth),
                "Custom health should be stored correctly");
        }

        [Test]
        public void Test_Constructor_WithAllParameters_StoresAllValues()
        {
            // Arrange
            VoxelType expectedType = VoxelType.Dirt;
            byte expectedHealth = 150;
            byte expectedMetadata = 77;

            // Act
            var voxel = new MicroVoxelData(expectedType, expectedHealth, expectedMetadata);

            // Assert
            Assert.That(voxel.Type, Is.EqualTo(expectedType), "Type should be stored");
            Assert.That(voxel.Health, Is.EqualTo(expectedHealth), "Health should be stored");
            Assert.That(voxel.Metadata, Is.EqualTo(expectedMetadata), "Metadata should be stored");
        }

        #endregion

        #region Edge Cases

        [Test]
        public void Test_WithDamage_OneHealthPoint_ConvertsToAir()
        {
            // Arrange
            var voxel = new MicroVoxelData(VoxelType.Stone, 1);
            byte damage = 1;

            // Act
            var damaged = voxel.WithDamage(damage);

            // Assert
            Assert.That(damaged.Type, Is.EqualTo(VoxelType.Air),
                "Voxel with 1 health should become Air after 1 damage");
            Assert.That(damaged.Health, Is.EqualTo(0),
                "Health should be 0");
        }

        [Test]
        public void Test_WithDamage_ZeroDamage_ReturnsUnchangedCopy()
        {
            // Arrange
            var original = new MicroVoxelData(VoxelType.Stone, 100);

            // Act
            var result = original.WithDamage(0);

            // Assert
            Assert.That(result.Health, Is.EqualTo(100),
                "Health should remain unchanged with 0 damage");
            Assert.That(result.Type, Is.EqualTo(VoxelType.Stone),
                "Type should remain unchanged");
        }

        [Test]
        public void Test_IsSolid_MinimumHealth_ReturnsTrue()
        {
            // Arrange
            var voxel = new MicroVoxelData(VoxelType.Wood, 1);

            // Act
            bool result = voxel.IsSolid;

            // Assert
            Assert.That(result, Is.True,
                "Voxel with minimum health (1) should still be solid");
        }

        #endregion
    }
}
