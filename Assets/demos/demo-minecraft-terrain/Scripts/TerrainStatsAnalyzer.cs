using System.Collections.Generic;
using System.Text;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Demos.MinecraftTerrain
{
    /// <summary>
    /// Static utility class for analyzing generated Minecraft-style terrain.
    /// Provides voxel type distribution statistics for validation and debugging.
    ///
    /// Features:
    /// - Count voxels by type across all chunks
    /// - Calculate percentage distribution
    /// - Format as readable string for display/logging
    ///
    /// Usage:
    /// string stats = TerrainStatsAnalyzer.AnalyzeTerrain(chunkManager);
    /// Debug.Log(stats);
    /// </summary>
    public static class TerrainStatsAnalyzer
    {
        /// <summary>
        /// Analyze terrain and return voxel type distribution statistics as formatted string.
        /// </summary>
        /// <param name="chunkManager">ChunkManager with generated chunks</param>
        /// <returns>Formatted statistics string with voxel counts and percentages</returns>
        public static string AnalyzeTerrain(ChunkManager chunkManager)
        {
            if (chunkManager == null)
            {
                return "ERROR: ChunkManager is null";
            }

            // Count voxels by type
            var voxelCounts = CountVoxelsByType(chunkManager);

            // Calculate total
            long totalVoxels = 0;
            foreach (var kvp in voxelCounts)
            {
                totalVoxels += kvp.Value;
            }

            if (totalVoxels == 0)
            {
                return "ERROR: No voxels found in terrain";
            }

            // Format statistics string
            var sb = new StringBuilder();
            sb.AppendLine($"Total Voxels: {totalVoxels:N0}");
            sb.AppendLine();
            sb.AppendLine("Voxel Distribution:");

            // Sort by count (descending) for better readability
            var sortedCounts = new List<KeyValuePair<VoxelType, long>>(voxelCounts);
            sortedCounts.Sort((a, b) => b.Value.CompareTo(a.Value));

            foreach (var kvp in sortedCounts)
            {
                if (kvp.Value > 0)
                {
                    float percentage = (float)kvp.Value / totalVoxels * 100f;
                    sb.AppendLine($"  {GetVoxelTypeIcon(kvp.Key)} {kvp.Key,-8}: {kvp.Value,12:N0} ({percentage,6:F2}%)");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Count voxels by type across all chunks in ChunkManager.
        /// </summary>
        private static Dictionary<VoxelType, long> CountVoxelsByType(ChunkManager chunkManager)
        {
            var counts = new Dictionary<VoxelType, long>();

            // Initialize counts for all voxel types
            for (int i = 0; i <= 7; i++)
            {
                counts[(VoxelType)i] = 0;
            }

            // Iterate through all chunks
            foreach (var chunk in chunkManager.GetAllChunks())
            {
                if (!chunk.IsGenerated)
                    continue;

                var voxelData = chunk.VoxelData;
                if (!voxelData.IsCreated)
                    continue;

                // Count voxels in this chunk
                for (int i = 0; i < voxelData.Length; i++)
                {
                    VoxelType type = voxelData[i];
                    counts[type]++;
                }
            }

            return counts;
        }

        /// <summary>
        /// Get a simple icon/emoji for each voxel type (for better readability in logs).
        /// </summary>
        private static string GetVoxelTypeIcon(VoxelType type)
        {
            switch (type)
            {
                case VoxelType.Air:    return "⬜"; // White square (empty)
                case VoxelType.Grass:  return "🟩"; // Green square
                case VoxelType.Dirt:   return "🟫"; // Brown square
                case VoxelType.Stone:  return "⬛"; // Black square (gray in most terminals)
                case VoxelType.Sand:   return "🟨"; // Yellow square
                case VoxelType.Water:  return "🟦"; // Blue square
                case VoxelType.Wood:   return "🟫"; // Brown square
                case VoxelType.Leaves: return "🟩"; // Green square
                default:               return "❓"; // Question mark
            }
        }
    }
}
