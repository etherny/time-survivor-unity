namespace TimeSurvivor.Voxel.Core
{
    /// <summary>
    /// Defines the different types of voxels available in the game.
    /// Uses byte for memory efficiency (256 possible types).
    /// </summary>
    public enum VoxelType : byte
    {
        /// <summary>Empty space - no collision, no rendering</summary>
        Air = 0,

        /// <summary>Grass surface block</summary>
        Grass = 1,

        /// <summary>Dirt block</summary>
        Dirt = 2,

        /// <summary>Stone block</summary>
        Stone = 3,

        /// <summary>Sand block</summary>
        Sand = 4,

        /// <summary>Water block</summary>
        Water = 5,

        /// <summary>Wood block</summary>
        Wood = 6,

        /// <summary>Leaves block</summary>
        Leaves = 7
    }
}
