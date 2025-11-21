using UnityEngine;

namespace TimeSurvivor.Voxel.Rendering
{
    /// <summary>
    /// ScriptableObject managing material atlas for voxel textures.
    /// Maps VoxelType to UV coordinates in a texture atlas for efficient rendering.
    /// </summary>
    [CreateAssetMenu(fileName = "VoxelMaterialAtlas", menuName = "TimeSurvivor/Voxel Material Atlas")]
    public class VoxelMaterialAtlas : ScriptableObject
    {
        [Header("Atlas Configuration")]
        [Tooltip("Main atlas texture containing all voxel textures")]
        public Texture2D AtlasTexture;

        [Tooltip("Material to use for rendering voxel meshes")]
        public Material VoxelMaterial;

        [Tooltip("Number of tiles per row/column in atlas (e.g., 16 for 16x16 grid)")]
        [Range(1, 64)]
        public int AtlasSize = 16;

        [Header("Voxel Texture Mappings")]
        [Tooltip("Array mapping VoxelType enum index to atlas tile index")]
        public VoxelTextureMapping[] TextureMappings;

        /// <summary>
        /// Get UV coordinates for a voxel type's face.
        /// Returns (minU, minV, maxU, maxV) for texture sampling.
        /// </summary>
        public Vector4 GetUVRect(Core.VoxelType voxelType, VoxelFace face)
        {
            // Find mapping for this voxel type
            foreach (var mapping in TextureMappings)
            {
                if (mapping.VoxelType == voxelType)
                {
                    int tileIndex = GetTileIndexForFace(mapping, face);
                    return CalculateUVRect(tileIndex);
                }
            }

            // Fallback: return first tile (error texture)
            Debug.LogWarning($"[VoxelMaterialAtlas] No texture mapping for {voxelType}, using fallback.");
            return CalculateUVRect(0);
        }

        /// <summary>
        /// Get tile index for a specific face of a voxel.
        /// Some voxels have different textures per face (e.g., grass has different top/side/bottom).
        /// </summary>
        private int GetTileIndexForFace(VoxelTextureMapping mapping, VoxelFace face)
        {
            switch (face)
            {
                case VoxelFace.Top: return mapping.TopTileIndex;
                case VoxelFace.Bottom: return mapping.BottomTileIndex;
                case VoxelFace.North:
                case VoxelFace.South:
                case VoxelFace.East:
                case VoxelFace.West:
                    return mapping.SideTileIndex;
                default:
                    return mapping.TopTileIndex;
            }
        }

        /// <summary>
        /// Calculate UV rectangle for a tile index in the atlas.
        /// </summary>
        private Vector4 CalculateUVRect(int tileIndex)
        {
            float tileSize = 1f / AtlasSize;

            int x = tileIndex % AtlasSize;
            int y = tileIndex / AtlasSize;

            float minU = x * tileSize;
            float minV = y * tileSize;
            float maxU = minU + tileSize;
            float maxV = minV + tileSize;

            return new Vector4(minU, minV, maxU, maxV);
        }

        private void OnValidate()
        {
            if (AtlasTexture != null)
            {
                // Check if texture is square
                if (AtlasTexture.width != AtlasTexture.height)
                {
                    Debug.LogWarning($"[VoxelMaterialAtlas] Atlas texture should be square. " +
                                     $"Current size: {AtlasTexture.width}x{AtlasTexture.height}");
                }

                // Check if texture size matches atlas size
                int expectedSize = AtlasSize * 16; // Assuming 16px per tile minimum
                if (AtlasTexture.width < expectedSize)
                {
                    Debug.LogWarning($"[VoxelMaterialAtlas] Atlas texture resolution might be too low. " +
                                     $"Expected at least {expectedSize}x{expectedSize} for {AtlasSize}x{AtlasSize} atlas.");
                }
            }

            if (VoxelMaterial != null)
            {
                // Verify material has the atlas texture assigned
                if (VoxelMaterial.mainTexture != AtlasTexture)
                {
                    Debug.LogWarning("[VoxelMaterialAtlas] Material's main texture doesn't match AtlasTexture. " +
                                     "Make sure they are synchronized.");
                }
            }
        }
    }

    /// <summary>
    /// Mapping from VoxelType to texture tiles in the atlas.
    /// </summary>
    [System.Serializable]
    public struct VoxelTextureMapping
    {
        public Core.VoxelType VoxelType;

        [Tooltip("Tile index for top face")]
        public int TopTileIndex;

        [Tooltip("Tile index for side faces (N/S/E/W)")]
        public int SideTileIndex;

        [Tooltip("Tile index for bottom face")]
        public int BottomTileIndex;
    }

    /// <summary>
    /// Enum for voxel cube faces.
    /// </summary>
    public enum VoxelFace
    {
        Top,
        Bottom,
        North,
        South,
        East,
        West
    }
}
