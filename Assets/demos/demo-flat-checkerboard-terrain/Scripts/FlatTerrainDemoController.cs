using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain
{
    /// <summary>
    /// Orchestrates the Flat Checkerboard Terrain demonstration.
    /// Features dynamic chunk streaming based on player position.
    /// Displays real-time statistics and instructions.
    /// </summary>
    public class FlatTerrainDemoController : MonoBehaviour
    {
        [Header("Voxel Configuration")]
        [SerializeField] public VoxelConfiguration voxelConfig;

        [Header("Rendering")]
        [SerializeField] public Material chunkMaterial;

        [Header("Player Reference")]
        [SerializeField] public Transform player;

        [Header("UI References")]
        [SerializeField] public TextMeshProUGUI statsText;
        [SerializeField] public TextMeshProUGUI instructionsText;

        [Header("FPS Calculation")]
        [SerializeField] public float fpsUpdateInterval = 1f;

        [Header("Streaming Settings")]
        [SerializeField] public int loadRadius = 2;        // Chunks to load around player
        [SerializeField] public int unloadRadius = 3;      // Chunks to unload if too far
        [SerializeField] public float updateInterval = 0.5f; // Streaming update frequency (seconds)

        private ChunkManager chunkManager;
        private FlatCheckerboardGenerator generator;

        private float fpsTimer = 0f;
        private int frameCount = 0;
        private float currentFps = 0f;

        // Streaming state
        private ChunkCoord lastPlayerChunkCoord;
        private float streamingTimer = 0f;
        private bool isValid = false;

        private const string INSTRUCTIONS_TEXT = @"=== CONTROLES ===
WASD: Deplacer le joueur
Shift: Sprint (2x vitesse)

=== VALIDATION ===
[OK] Terrain plat visible
[OK] Pattern damier Grass/Dirt
[OK] Streaming dynamique actif
[OK] Nouveaux chunks apparaissent
[OK] Chunks lointains se dechargeant";

        void Start()
        {
            ValidateSetup();

            if (!isValid) return;

            InitializeGenerator();
            InitializeChunkManager();
            InitializeUI();

            // Generate initial chunks around player with streaming
            UpdateStreaming(forceUpdate: true);

            Debug.Log("[FlatTerrainDemoController] Demo initialized successfully with dynamic streaming.");
        }

        void Update()
        {
            UpdateFPS();
            UpdateStatistics();
            ProcessChunkQueues();
            UpdateStreaming(forceUpdate: false);
        }

        void OnDestroy()
        {
            CleanupChunkManager();
        }

        private void ValidateSetup()
        {
            isValid = true;

            if (voxelConfig == null)
            {
                Debug.LogError("[FlatTerrainDemoController] VoxelConfiguration reference is missing!");
                isValid = false;
            }

            if (chunkMaterial == null)
            {
                Debug.LogError("[FlatTerrainDemoController] Chunk Material reference is missing!");
                isValid = false;
            }

            if (player == null)
            {
                Debug.LogWarning("[FlatTerrainDemoController] Player Transform reference is missing. Streaming will not work properly.");
            }

            if (statsText == null)
            {
                Debug.LogWarning("[FlatTerrainDemoController] Stats TextMeshProUGUI reference is missing. Stats will not be displayed.");
            }

            if (instructionsText == null)
            {
                Debug.LogWarning("[FlatTerrainDemoController] Instructions TextMeshProUGUI reference is missing. Instructions will not be displayed.");
            }

            if (isValid)
            {
                Debug.Log("[FlatTerrainDemoController] Demo setup validated successfully.");
            }
            else
            {
                Debug.LogError("[FlatTerrainDemoController] Demo setup validation FAILED. Please check references in Inspector.");
            }
        }

        private void InitializeGenerator()
        {
            generator = new FlatCheckerboardGenerator();
            Debug.Log("[FlatTerrainDemoController] FlatCheckerboardGenerator created.");
        }

        private void InitializeChunkManager()
        {
            if (voxelConfig == null || chunkMaterial == null)
            {
                Debug.LogError("[FlatTerrainDemoController] Cannot create ChunkManager: missing config or material.");
                return;
            }

            // Create parent transform for chunks
            Transform chunkParent = transform;

            // Create ChunkManager with custom generator
            chunkManager = new ChunkManager(voxelConfig, chunkParent, chunkMaterial, generator);

            Debug.Log("[FlatTerrainDemoController] ChunkManager created with FlatCheckerboardGenerator.");
        }

        /// <summary>
        /// Update chunk streaming based on player position.
        /// Loads chunks within loadRadius and unloads chunks beyond unloadRadius.
        /// </summary>
        private void UpdateStreaming(bool forceUpdate)
        {
            if (player == null || chunkManager == null) return;

            // Timer to avoid too frequent updates
            streamingTimer += Time.deltaTime;
            if (!forceUpdate && streamingTimer < updateInterval) return;
            streamingTimer = 0f;

            // Calculate chunk coordinate of player
            ChunkCoord playerChunk = GetChunkCoordFromPosition(player.position);

            // If player hasn't changed chunks, nothing to do
            if (!forceUpdate && playerChunk.Equals(lastPlayerChunkCoord)) return;

            lastPlayerChunkCoord = playerChunk;

            // Load chunks around player
            LoadChunksInRadius(playerChunk, loadRadius);

            // Unload chunks too far away
            UnloadChunksOutsideRadius(playerChunk, unloadRadius);
        }

        /// <summary>
        /// Calculate the chunk coordinate from a world position.
        /// </summary>
        private ChunkCoord GetChunkCoordFromPosition(Vector3 worldPosition)
        {
            int chunkSize = voxelConfig.ChunkSize;
            float voxelSize = voxelConfig.MacroVoxelSize;
            float chunkWorldSize = chunkSize * voxelSize;

            int chunkX = Mathf.FloorToInt(worldPosition.x / chunkWorldSize);
            int chunkY = 0; // Flat terrain, always Y=0
            int chunkZ = Mathf.FloorToInt(worldPosition.z / chunkWorldSize);

            return new ChunkCoord(chunkX, chunkY, chunkZ);
        }

        /// <summary>
        /// Load all chunks within the specified radius around the center chunk.
        /// </summary>
        private void LoadChunksInRadius(ChunkCoord center, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int z = -radius; z <= radius; z++)
                {
                    ChunkCoord coord = new ChunkCoord(center.X + x, 0, center.Z + z);

                    if (!chunkManager.HasChunk(coord))
                    {
                        chunkManager.LoadChunk(coord);
                    }
                }
            }
        }

        /// <summary>
        /// Unload chunks that are beyond the specified radius from the center chunk.
        /// </summary>
        private void UnloadChunksOutsideRadius(ChunkCoord center, int radius)
        {
            var allChunks = chunkManager.GetAllChunks();

            foreach (var chunk in allChunks.ToArray()) // ToArray to avoid modification during iteration
            {
                int dx = Mathf.Abs(chunk.Coord.X - center.X);
                int dz = Mathf.Abs(chunk.Coord.Z - center.Z);

                if (dx > radius || dz > radius)
                {
                    chunkManager.UnloadChunk(chunk.Coord);
                }
            }
        }

        /// <summary>
        /// Process chunk queues during Update (should be empty after initial generation).
        /// </summary>
        private void ProcessChunkQueues()
        {
            if (chunkManager == null) return;

            chunkManager.ProcessGenerationQueue();
            chunkManager.ProcessMeshingQueue(Time.deltaTime);
        }

        private void CleanupChunkManager()
        {
            // ChunkManager cleanup is handled by Unity's garbage collection
            // Chunk disposal happens when chunks are unloaded
            Debug.Log("[FlatTerrainDemoController] Cleanup complete.");
        }

        private void InitializeUI()
        {
            if (instructionsText != null)
            {
                instructionsText.text = INSTRUCTIONS_TEXT;
            }
        }

        private void UpdateFPS()
        {
            frameCount++;
            fpsTimer += Time.deltaTime;

            if (fpsTimer >= fpsUpdateInterval)
            {
                currentFps = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer = 0f;
            }
        }

        private void UpdateStatistics()
        {
            if (statsText == null) return;

            Vector3 playerPos = player != null ? player.position : Vector3.zero;
            ChunkCoord playerChunk = GetChunkCoordFromPosition(playerPos);
            int activeChunks = chunkManager != null ? chunkManager.ActiveChunkCount : 0;

            statsText.text = $@"=== FLAT CHECKERBOARD TERRAIN ===
FPS: {currentFps:F0}
Chunks actifs: {activeChunks} (streaming actif)
Pattern: Damier (Grass/Dirt)
Taille de case: 8 voxels
Load radius: {loadRadius} chunks
Unload radius: {unloadRadius} chunks

Position joueur: ({playerPos.x:F1}, {playerPos.y:F1}, {playerPos.z:F1})
Chunk joueur: ({playerChunk.X}, {playerChunk.Y}, {playerChunk.Z})";
        }
    }
}
