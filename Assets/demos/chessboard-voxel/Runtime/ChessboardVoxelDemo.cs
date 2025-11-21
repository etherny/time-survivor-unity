using UnityEngine;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Demos.ChessboardVoxel
{
    /// <summary>
    /// Demo MonoBehaviour that showcases vertex color support in the voxel engine.
    /// Creates a 16x16 chessboard pattern using the voxel engine with vertex colors.
    /// Demonstrates how VoxelType colors (Stone=gray, Sand=yellow) are rendered using vertex colors.
    /// </summary>
    public class ChessboardVoxelDemo : MonoBehaviour
    {
        #region Inspector Configuration

        [Header("Voxel Configuration")]
        [SerializeField]
        [Tooltip("Voxel engine configuration (REQUIRED)")]
        private VoxelConfiguration voxelConfig;

        [SerializeField]
        [Tooltip("Board size (number of cells per side). Must be <= chunk size.\nNOTE: Changes require stopping and restarting Play mode.")]
        [Range(4, 32)]
        private int boardSize = 16;

        [Header("Material")]
        [SerializeField]
        [Tooltip("Material with VoxelVertexColor shader (REQUIRED for vertex colors)")]
        private Material voxelVertexColorMaterial;

        #endregion

        #region Private Fields

        private ChunkManager _chunkManager;
        private VoxelConfiguration _config;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            // Validate configuration
            if (voxelConfig == null)
            {
                Debug.LogError("[ChessboardVoxelDemo] VoxelConfiguration is not assigned! " +
                    "Please assign a VoxelConfiguration asset.", this);
                return;
            }

            if (voxelVertexColorMaterial == null)
            {
                Debug.LogError("[ChessboardVoxelDemo] Vertex color material is not assigned! " +
                    "Please assign a material using the VoxelVertexColor shader.", this);
                return;
            }

            if (boardSize <= 0)
            {
                Debug.LogError("[ChessboardVoxelDemo] Board size must be positive!", this);
                return;
            }

            // Use provided voxel configuration
            _config = voxelConfig;

            // Create custom chessboard generator
            var generator = new ChessboardVoxelGenerator(boardSize, baseHeight: 0);

            // Create chunk manager with our custom generator
            _chunkManager = new ChunkManager(_config, transform, voxelVertexColorMaterial, generator);

            // Generate the chessboard (single chunk at origin)
            GenerateChessboard();

            Debug.Log($"[ChessboardVoxelDemo] Chessboard generated! " +
                $"Size: {boardSize}x{boardSize}, ChunkSize: {_config.ChunkSize}, VoxelSize: {_config.MacroVoxelSize}m");
        }

        private void Update()
        {
            if (_chunkManager == null) return;

            // Process generation and meshing queues
            _chunkManager.ProcessGenerationQueue();
            _chunkManager.ProcessMeshingQueue(Time.deltaTime);
        }

        private void OnDestroy()
        {
            // Cleanup resources
            _chunkManager?.Dispose();
        }

        private void OnValidate()
        {
            // Ensure board size doesn't exceed chunk size
            if (voxelConfig != null && boardSize > voxelConfig.ChunkSize)
            {
                Debug.LogWarning($"[ChessboardVoxelDemo] Board size ({boardSize}) cannot exceed chunk size ({voxelConfig.ChunkSize}). " +
                    "Clamping to chunk size.", this);
                boardSize = voxelConfig.ChunkSize;
            }
        }

        #endregion

        #region Chessboard Generation

        /// <summary>
        /// Generates the chessboard by loading a single chunk at the origin.
        /// The ChunkManager will use the ChessboardVoxelGenerator passed in the constructor.
        /// </summary>
        private void GenerateChessboard()
        {
            // Load the chunk at origin (0, 0, 0)
            // ChunkManager will automatically use our custom ChessboardVoxelGenerator
            ChunkCoord origin = new ChunkCoord(0, 0, 0);
            _chunkManager.LoadChunk(origin);
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (voxelConfig == null) return;

            // Draw chessboard bounds
            float voxelSize = voxelConfig.MacroVoxelSize;
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3(
                (boardSize * voxelSize) / 2f,
                voxelSize / 2f,  // Center at half voxel height
                (boardSize * voxelSize) / 2f
            );
            Vector3 size = new Vector3(
                boardSize * voxelSize,
                voxelSize,
                boardSize * voxelSize
            );
            Gizmos.DrawWireCube(center, size);
        }

        #endregion
    }
}
