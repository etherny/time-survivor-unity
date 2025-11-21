namespace TimeSurvivor.Voxel.Core
{
    /// <summary>
    /// Data structure for micro-scale voxels (destructible props, 0.1 Unity units).
    /// Used for destructible objects like trees, rocks, buildings.
    /// </summary>
    public struct MicroVoxelData
    {
        /// <summary>
        /// Type of voxel at this position
        /// </summary>
        public VoxelType Type;

        /// <summary>
        /// Health points for destructible voxels (0-255)
        /// When health reaches 0, voxel is destroyed
        /// </summary>
        public byte Health;

        /// <summary>
        /// Optional metadata flags (for future use: material hardness, color variants, etc.)
        /// </summary>
        public byte Metadata;

        public MicroVoxelData(VoxelType type, byte health = 255, byte metadata = 0)
        {
            Type = type;
            Health = health;
            Metadata = metadata;
        }

        /// <summary>
        /// Returns true if this voxel is solid (should be rendered and have collision)
        /// </summary>
        public bool IsSolid => Type != VoxelType.Air && Health > 0;

        /// <summary>
        /// Returns true if this voxel is destroyed (health depleted)
        /// </summary>
        public bool IsDestroyed => Health == 0;

        /// <summary>
        /// Returns a new MicroVoxelData with damage applied.
        /// Immutable operation - does not modify the original struct.
        /// </summary>
        /// <param name="damageAmount">Amount of damage to apply</param>
        /// <returns>New MicroVoxelData with updated health and type</returns>
        public MicroVoxelData WithDamage(byte damageAmount)
        {
            if (Health > damageAmount)
            {
                return new MicroVoxelData(Type, (byte)(Health - damageAmount), Metadata);
            }
            else
            {
                return new MicroVoxelData(VoxelType.Air, 0, Metadata);
            }
        }
    }
}
