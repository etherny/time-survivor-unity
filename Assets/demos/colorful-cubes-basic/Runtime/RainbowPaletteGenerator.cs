using UnityEngine;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Demos.ColorfulCubes
{
    /// <summary>
    /// Static utility class for generating color palettes.
    /// Provides preset palettes for different visual styles.
    /// </summary>
    public static class RainbowPaletteGenerator
    {
        /// <summary>
        /// Get the default natural color palette.
        /// Uses realistic colors for each voxel type (grass = green, dirt = brown, etc.).
        /// </summary>
        /// <returns>Array of colors indexed by VoxelType</returns>
        public static Color[] GetDefaultPalette()
        {
            var palette = new Color[8]; // VoxelType has 8 values (0-7)

            palette[(int)VoxelType.Air] = Color.clear;                        // Transparent
            palette[(int)VoxelType.Grass] = HexToColor("#33CC33");           // Bright green
            palette[(int)VoxelType.Dirt] = HexToColor("#996633");            // Brown
            palette[(int)VoxelType.Stone] = HexToColor("#808080");           // Gray
            palette[(int)VoxelType.Sand] = HexToColor("#FFEE66");            // Yellow
            palette[(int)VoxelType.Water] = HexToColor("#3377FF");           // Blue
            palette[(int)VoxelType.Wood] = HexToColor("#663300");            // Dark brown
            palette[(int)VoxelType.Leaves] = HexToColor("#66FF66");          // Light green

            return palette;
        }

        /// <summary>
        /// Get vibrant rainbow color palette.
        /// Uses saturated rainbow colors for maximum visual impact.
        /// </summary>
        /// <returns>Array of rainbow colors indexed by VoxelType</returns>
        public static Color[] GetRainbowPalette()
        {
            var palette = new Color[8]; // VoxelType has 8 values (0-7)

            palette[(int)VoxelType.Air] = Color.clear;                        // Transparent
            palette[(int)VoxelType.Grass] = HexToColor("#FF0000");           // Red
            palette[(int)VoxelType.Dirt] = HexToColor("#FF8800");            // Orange
            palette[(int)VoxelType.Stone] = HexToColor("#FFFF00");           // Yellow
            palette[(int)VoxelType.Sand] = HexToColor("#00FF00");            // Green
            palette[(int)VoxelType.Water] = HexToColor("#00FFFF");           // Cyan
            palette[(int)VoxelType.Wood] = HexToColor("#0000FF");            // Blue
            palette[(int)VoxelType.Leaves] = HexToColor("#8800FF");          // Purple

            return palette;
        }

        /// <summary>
        /// Get soft pastel color palette.
        /// Uses desaturated pastel colors for a gentle aesthetic.
        /// </summary>
        /// <returns>Array of pastel colors indexed by VoxelType</returns>
        public static Color[] GetPastelPalette()
        {
            var palette = new Color[8]; // VoxelType has 8 values (0-7)

            palette[(int)VoxelType.Air] = Color.clear;                        // Transparent
            palette[(int)VoxelType.Grass] = HexToColor("#FFB3BA");           // Pastel red/pink
            palette[(int)VoxelType.Dirt] = HexToColor("#FFDFBA");            // Pastel orange
            palette[(int)VoxelType.Stone] = HexToColor("#FFFFBA");           // Pastel yellow
            palette[(int)VoxelType.Sand] = HexToColor("#BAFFC9");            // Pastel green
            palette[(int)VoxelType.Water] = HexToColor("#BAE1FF");           // Pastel blue
            palette[(int)VoxelType.Wood] = HexToColor("#C9B3FF");            // Pastel purple
            palette[(int)VoxelType.Leaves] = HexToColor("#FFB3F0");          // Pastel magenta

            return palette;
        }

        /// <summary>
        /// Get neon/cyberpunk color palette.
        /// Uses highly saturated neon colors for a futuristic look.
        /// </summary>
        /// <returns>Array of neon colors indexed by VoxelType</returns>
        public static Color[] GetNeonPalette()
        {
            var palette = new Color[8]; // VoxelType has 8 values (0-7)

            palette[(int)VoxelType.Air] = Color.clear;                        // Transparent
            palette[(int)VoxelType.Grass] = HexToColor("#FF006E");           // Neon pink
            palette[(int)VoxelType.Dirt] = HexToColor("#FB5607");            // Neon orange
            palette[(int)VoxelType.Stone] = HexToColor("#FFBE0B");           // Neon yellow
            palette[(int)VoxelType.Sand] = HexToColor("#8338EC");            // Neon purple
            palette[(int)VoxelType.Water] = HexToColor("#3A86FF");           // Neon blue
            palette[(int)VoxelType.Wood] = HexToColor("#06FFA5");            // Neon cyan
            palette[(int)VoxelType.Leaves] = HexToColor("#FF006E");          // Neon magenta

            return palette;
        }

        /// <summary>
        /// Convert hex color string to Unity Color.
        /// Supports both #RGB and #RRGGBB formats.
        /// </summary>
        /// <param name="hex">Hex color string (e.g., "#FF0000" or "#F00")</param>
        /// <returns>Unity Color object</returns>
        private static Color HexToColor(string hex)
        {
            // Remove # if present
            if (hex.StartsWith("#"))
            {
                hex = hex.Substring(1);
            }

            // Expand shorthand format (#RGB to #RRGGBB)
            if (hex.Length == 3)
            {
                hex = string.Format("{0}{0}{1}{1}{2}{2}",
                    hex[0], hex[1], hex[2]);
            }

            // Validate length
            if (hex.Length != 6)
            {
                Debug.LogWarning($"[RainbowPaletteGenerator] Invalid hex color: {hex}. Using white.");
                return Color.white;
            }

            // Parse RGB values
            try
            {
                int r = System.Convert.ToInt32(hex.Substring(0, 2), 16);
                int g = System.Convert.ToInt32(hex.Substring(2, 2), 16);
                int b = System.Convert.ToInt32(hex.Substring(4, 2), 16);

                return new Color(r / 255f, g / 255f, b / 255f, 1f);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RainbowPaletteGenerator] Failed to parse hex color: {hex}. Error: {e.Message}");
                return Color.white;
            }
        }

        /// <summary>
        /// Apply a color palette to a material.
        /// Assumes the material has a _ColorPalette texture property.
        /// </summary>
        /// <param name="material">Material to apply palette to</param>
        /// <param name="palette">Color palette to apply</param>
        public static void ApplyPaletteToMaterial(Material material, Color[] palette)
        {
            if (material == null)
            {
                Debug.LogWarning("[RainbowPaletteGenerator] Cannot apply palette to null material.");
                return;
            }

            if (palette == null || palette.Length == 0)
            {
                Debug.LogWarning("[RainbowPaletteGenerator] Cannot apply null or empty palette.");
                return;
            }

            // Create a 1D texture for the palette
            Texture2D paletteTexture = new Texture2D(palette.Length, 1, TextureFormat.RGBA32, false);
            paletteTexture.filterMode = FilterMode.Point; // No filtering for crisp colors
            paletteTexture.wrapMode = TextureWrapMode.Clamp;

            // Set pixel data
            for (int i = 0; i < palette.Length; i++)
            {
                paletteTexture.SetPixel(i, 0, palette[i]);
            }
            paletteTexture.Apply();

            // Apply to material
            if (material.HasProperty("_ColorPalette"))
            {
                material.SetTexture("_ColorPalette", paletteTexture);
            }
            else
            {
                Debug.LogWarning("[RainbowPaletteGenerator] Material does not have _ColorPalette property.");
            }
        }

        /// <summary>
        /// Get a random color from a palette.
        /// Useful for testing and previews.
        /// </summary>
        /// <param name="palette">Color palette</param>
        /// <param name="excludeAir">If true, excludes the Air voxel color (index 0)</param>
        /// <returns>Random color from palette</returns>
        public static Color GetRandomColorFromPalette(Color[] palette, bool excludeAir = true)
        {
            if (palette == null || palette.Length == 0)
            {
                return Color.white;
            }

            int startIndex = excludeAir ? 1 : 0;
            int randomIndex = Random.Range(startIndex, palette.Length);
            return palette[randomIndex];
        }
    }
}
