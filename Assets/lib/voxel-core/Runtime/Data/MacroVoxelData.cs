using Unity.Collections;

namespace TimeSurvivor.Voxel.Core
{
    /// <summary>
    /// Data structure for macro-scale voxels (terrain, 0.2 Unity units).
    /// Used for non-destructible world terrain.
    /// </summary>
    public struct MacroVoxelData
    {
        /// <summary>
        /// Type of voxel at this position
        /// </summary>
        public VoxelType Type;

        /// <summary>
        /// Optional metadata flags (for future use: biome info, temperature, etc.)
        /// </summary>
        public byte Metadata;

        public MacroVoxelData(VoxelType type, byte metadata = 0)
        {
            Type = type;
            Metadata = metadata;
        }

        /// <summary>
        /// Returns true if this voxel is solid (should be rendered and have collision)
        /// </summary>
        public bool IsSolid => Type != VoxelType.Air;

        /// <summary>
        /// Returns true if this voxel is transparent (affects rendering order)
        /// </summary>
        public bool IsTransparent => Type == VoxelType.Water || Type == VoxelType.Leaves;
    }
}
