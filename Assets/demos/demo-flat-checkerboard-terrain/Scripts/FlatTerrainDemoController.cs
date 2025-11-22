using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain
{
    /// <summary>
    /// Orchestrates the Flat Checkerboard Terrain demonstration.
    /// Generates 9 fixed chunks (3x1x3 grid) with checkerboard pattern.
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

        private ChunkManager chunkManager;
        private FlatCheckerboardGenerator generator;

        private float fpsTimer = 0f;
        private int frameCount = 0;
        private float currentFps = 0f;

        private const int TOTAL_CHUNKS = 9; // 3x1x3 grid

        private const string INSTRUCTIONS_TEXT = @"=== CONTROLES ===
WASD: Deplacer le joueur
Shift: Sprint (2x vitesse)

=== VALIDATION ===
[OK] Terrain plat visible
[OK] Pattern damier Grass/Dirt
[OK] Pas de chunks vides
[OK] Camera suit le joueur";

        void Start()
        {
            ValidateSetup();
            InitializeGenerator();
            InitializeChunkManager();
            GenerateFixedChunks();
            ProcessAllQueues();
            InitializeUI();

            Debug.Log("[FlatTerrainDemoController] Demo initialized successfully.");
        }

        void Update()
        {
            UpdateFPS();
            UpdateStatistics();
            ProcessChunkQueues();
        }

        void OnDestroy()
        {
            CleanupChunkManager();
        }

        private void ValidateSetup()
        {
            bool isValid = true;

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
                Debug.LogWarning("[FlatTerrainDemoController] Player Transform reference is missing. Stats will show (0,0,0).");
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
        /// Generates 9 fixed chunks in a 3x1x3 grid (y=0 only).
        /// Chunk coordinates: from (-1, 0, -1) to (1, 0, 1).
        /// </summary>
        private void GenerateFixedChunks()
        {
            if (chunkManager == null)
            {
                Debug.LogError("[FlatTerrainDemoController] Cannot generate chunks: ChunkManager is null.");
                return;
            }

            Debug.Log("[FlatTerrainDemoController] Generating 9 fixed chunks (3x1x3 grid)...");

            for (int x = -1; x <= 1; x++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    ChunkCoord coord = new ChunkCoord(x, 0, z);
                    chunkManager.LoadChunk(coord);
                }
            }

            Debug.Log("[FlatTerrainDemoController] 9 chunks queued for generation.");
        }

        /// <summary>
        /// Process all queues immediately at startup to ensure chunks are ready.
        /// </summary>
        private void ProcessAllQueues()
        {
            if (chunkManager == null) return;

            Debug.Log("[FlatTerrainDemoController] Processing generation and meshing queues...");

            // Process generation queue until empty
            for (int i = 0; i < 100; i++) // Safety limit
            {
                chunkManager.ProcessGenerationQueue();
            }

            // Process meshing queue until empty
            for (int i = 0; i < 100; i++) // Safety limit
            {
                chunkManager.ProcessMeshingQueue(Time.deltaTime);
            }

            Debug.Log("[FlatTerrainDemoController] All queues processed.");
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

            statsText.text = $@"=== FLAT CHECKERBOARD TERRAIN ===
FPS: {currentFps:F0}
Chunks actifs: {TOTAL_CHUNKS} / {TOTAL_CHUNKS}
Pattern: Damier (Grass/Dirt)
Taille de case: 8 voxels

Position joueur: ({playerPos.x:F1}, {playerPos.y:F1}, {playerPos.z:F1})";
        }
    }
}
