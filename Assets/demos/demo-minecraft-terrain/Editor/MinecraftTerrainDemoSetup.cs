using UnityEngine;
using UnityEditor;
using System.IO;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Demos.MinecraftTerrain.Editor
{
    /// <summary>
    /// Editor utility to automatically create Minecraft Terrain demo assets.
    /// Menu: Tools > TimeSurvivor > Setup Minecraft Terrain Demo
    /// </summary>
    public static class MinecraftTerrainDemoSetup
    {
        private const string ConfigurationsPath = "Assets/demos/demo-minecraft-terrain/Configurations";
        private const string VoxelCoreConfigPath = "Assets/lib/voxel-core/Configurations";

        [MenuItem("Tools/TimeSurvivor/Setup Minecraft Terrain Demo")]
        public static void SetupDemo()
        {
            Debug.Log("[MinecraftTerrainDemoSetup] Starting demo setup...");

            // Ensure directories exist
            EnsureDirectoryExists(ConfigurationsPath);
            EnsureDirectoryExists(VoxelCoreConfigPath);

            // Create VoxelConfiguration if it doesn't exist
            CreateDefaultVoxelConfiguration();

            // Create MinecraftTerrainConfiguration assets
            CreateSmallConfiguration();
            CreateMediumConfiguration();
            CreateLargeConfiguration();

            // Refresh asset database
            AssetDatabase.Refresh();

            Debug.Log("[MinecraftTerrainDemoSetup] Demo setup complete! Assets created in:");
            Debug.Log($"  - {ConfigurationsPath}");
            Debug.Log($"  - {VoxelCoreConfigPath}");
            Debug.Log("You can now open the scene and assign references. See UNITY_SETUP_GUIDE.md for details.");

            // Highlight the configurations folder
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(ConfigurationsPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private static void EnsureDirectoryExists(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parentFolder = Path.GetDirectoryName(path).Replace("\\", "/");
                string folderName = Path.GetFileName(path);

                if (!AssetDatabase.IsValidFolder(parentFolder))
                {
                    // Create parent folders recursively
                    string[] parts = path.Split('/');
                    string currentPath = parts[0];
                    for (int i = 1; i < parts.Length; i++)
                    {
                        string nextPath = currentPath + "/" + parts[i];
                        if (!AssetDatabase.IsValidFolder(nextPath))
                        {
                            AssetDatabase.CreateFolder(currentPath, parts[i]);
                        }
                        currentPath = nextPath;
                    }
                }
                else
                {
                    AssetDatabase.CreateFolder(parentFolder, folderName);
                }
            }
        }

        private static void CreateDefaultVoxelConfiguration()
        {
            string assetPath = $"{VoxelCoreConfigPath}/DefaultVoxelConfiguration.asset";

            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<VoxelConfiguration>(assetPath);
            if (existing != null)
            {
                Debug.Log($"[MinecraftTerrainDemoSetup] VoxelConfiguration already exists at {assetPath}");
                return;
            }

            // Create new VoxelConfiguration
            var config = ScriptableObject.CreateInstance<VoxelConfiguration>();
            config.ChunkSize = 64;
            config.MacroVoxelSize = 0.2f;
            config.Seed = 12345;
            config.NoiseFrequency = 0.02f;
            config.NoiseOctaves = 4;

            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MinecraftTerrainDemoSetup] Created DefaultVoxelConfiguration at {assetPath}");
        }

        private static void CreateSmallConfiguration()
        {
            string assetPath = $"{ConfigurationsPath}/Small_10x10x8.asset";
            CreateMinecraftTerrainConfiguration(
                assetPath,
                worldSizeX: 10,
                worldSizeY: 8,
                worldSizeZ: 10,
                baseTerrainHeight: 4,
                terrainVariation: 2
            );
        }

        private static void CreateMediumConfiguration()
        {
            string assetPath = $"{ConfigurationsPath}/Medium_20x20x8.asset";
            CreateMinecraftTerrainConfiguration(
                assetPath,
                worldSizeX: 20,
                worldSizeY: 8,
                worldSizeZ: 20,
                baseTerrainHeight: 4,
                terrainVariation: 2
            );
        }

        private static void CreateLargeConfiguration()
        {
            string assetPath = $"{ConfigurationsPath}/Large_50x50x8.asset";
            CreateMinecraftTerrainConfiguration(
                assetPath,
                worldSizeX: 50,
                worldSizeY: 8,
                worldSizeZ: 50,
                baseTerrainHeight: 4,
                terrainVariation: 3
            );
        }

        private static void CreateMinecraftTerrainConfiguration(
            string assetPath,
            int worldSizeX,
            int worldSizeY,
            int worldSizeZ,
            int baseTerrainHeight,
            int terrainVariation)
        {
            // Check if already exists
            var existing = AssetDatabase.LoadAssetAtPath<MinecraftTerrainConfiguration>(assetPath);
            if (existing != null)
            {
                Debug.Log($"[MinecraftTerrainDemoSetup] Configuration already exists at {assetPath}");
                return;
            }

            // Create new configuration
            var config = ScriptableObject.CreateInstance<MinecraftTerrainConfiguration>();
            config.WorldSizeX = worldSizeX;
            config.WorldSizeY = worldSizeY;
            config.WorldSizeZ = worldSizeZ;
            config.BaseTerrainHeight = baseTerrainHeight;
            config.TerrainVariation = terrainVariation;
            config.HeightmapFrequency = 0.02f;
            config.HeightmapOctaves = 4;
            config.GrassLayerThickness = 1;
            config.DirtLayerThickness = 3;
            config.GenerateWater = true;
            config.WaterLevel = 3;

            AssetDatabase.CreateAsset(config, assetPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MinecraftTerrainDemoSetup] Created configuration at {assetPath}");
        }
    }
}
