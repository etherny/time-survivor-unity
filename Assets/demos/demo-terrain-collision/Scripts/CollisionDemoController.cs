using UnityEngine;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;
using TimeSurvivor.Demos.FlatCheckerboardTerrain;

namespace TimeSurvivor.Demos.TerrainCollision
{
    /// <summary>
    /// Main controller for the Terrain Collision demonstration.
    /// Orchestrates terrain generation with collision, player controller, physics objects, and UI.
    /// </summary>
    public class CollisionDemoController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VoxelConfiguration config;
        [SerializeField] private Material terrainMaterial;

        [Header("Terrain Setup")]
        [SerializeField] private bool enableCollision = true;
        [SerializeField] private bool useAsyncBaking = true;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

        // Internal components
        private ProceduralTerrainStreamer terrainStreamer;
        private FlatCheckerboardGenerator terrainGenerator;
        private ChunkManager chunkManager;

        /// <summary>
        /// Number of chunks with collision currently loaded.
        /// </summary>
        public int CollisionChunkCount
        {
            get
            {
                if (chunkManager == null) return 0;
                int count = 0;
                foreach (var chunk in chunkManager.GetAllChunks())
                {
                    if (chunk.HasCollision)
                    {
                        count++;
                    }
                }
                return count;
            }
        }

        /// <summary>
        /// Number of chunks waiting in the collision baking queue.
        /// </summary>
        public int BakingQueueCount
        {
            get
            {
                if (chunkManager == null) return 0;
                return chunkManager.CollisionQueueCount;
            }
        }

        private void Awake()
        {
            // Validate configuration
            if (config == null)
            {
                Debug.LogError("[CollisionDemoController] VoxelConfiguration is not assigned!");
                enabled = false;
                return;
            }

            if (terrainMaterial == null)
            {
                Debug.LogWarning("[CollisionDemoController] Terrain material not assigned. Creating default.");
                terrainMaterial = new Material(Shader.Find("Standard"));
            }

            // Ensure collision settings are correct
            config.EnableCollision = enableCollision;
            config.UseAsyncCollisionBaking = useAsyncBaking;

            // Validate player transform
            if (playerTransform == null)
            {
                var playerController = FindObjectOfType<SimpleCharacterController>();
                if (playerController != null)
                {
                    playerTransform = playerController.transform;
                }
                else
                {
                    Debug.LogError("[CollisionDemoController] Player transform not found!");
                }
            }

            // Initialize terrain generation
            InitializeTerrain();
        }

        /// <summary>
        /// Initialize terrain generation with collision support.
        /// </summary>
        private void InitializeTerrain()
        {
            // Create terrain generator
            terrainGenerator = new FlatCheckerboardGenerator();

            // Create chunk manager directly with generator
            chunkManager = new ChunkManager(config, transform, terrainMaterial, terrainGenerator);

            // Create terrain streamer
            terrainStreamer = gameObject.AddComponent<ProceduralTerrainStreamer>();

            // Initialize streamer using the new public API (replaces reflection-based approach)
            terrainStreamer.Initialize(
                config: config,
                chunkManager: chunkManager,
                streamingTarget: playerTransform,
                useMainCamera: false,
                showDebugInfo: false
            );

            Debug.Log("[CollisionDemoController] Terrain initialized with collision support");
        }

        private void OnDestroy()
        {
            // Cleanup handled by ProceduralTerrainStreamer
        }

        /// <summary>
        /// Get collision statistics for debugging.
        /// </summary>
        public string GetCollisionStats()
        {
            if (chunkManager == null) return "No chunk manager";

            return $"Collision Chunks: {CollisionChunkCount}, Baking Queue: {BakingQueueCount}";
        }
    }
}
