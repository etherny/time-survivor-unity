using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Demos.ColorfulCubes
{
    /// <summary>
    /// Main demo MonoBehaviour for showcasing colorful voxel patterns.
    /// Demonstrates the voxel engine with various visual patterns and runtime controls.
    ///
    /// Setup Instructions:
    /// 1. Attach this script to a GameObject in your scene
    /// 2. Assign a VoxelConfiguration ScriptableObject
    /// 3. Assign a Material for rendering chunks
    /// 4. Press Play and use the Inspector controls to change patterns
    /// </summary>
    [AddComponentMenu("TimeSurvivor/Demos/Colorful Cubes Demo")]
    public class ColorfulCubesDemo : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Configuration")]
        [Tooltip("Voxel engine configuration (ChunkSize, RenderDistance, etc.)")]
        [SerializeField] private VoxelConfiguration _configuration;

        [Tooltip("Material used for rendering voxel chunks")]
        [SerializeField] private Material _chunkMaterial;

        [Header("Pattern Settings")]
        [Tooltip("Current color pattern to display")]
        [SerializeField] private ColorPattern _currentPattern = ColorPattern.RainbowLayers;

        [Tooltip("Seed for random pattern generation")]
        [SerializeField] private int _randomSeed = 12345;

        [Header("World Settings")]
        [Tooltip("Number of chunks to load in each direction (total = viewDistance² chunks)")]
        [Range(1, 8)]
        [SerializeField] private int _viewDistance = 4;

        [Tooltip("World center position (chunks are loaded around this point)")]
        [SerializeField] private Vector3Int _worldCenter = Vector3Int.zero;

        [Header("Camera Controls")]
        [Tooltip("Auto-rotate camera around the voxel world")]
        [SerializeField] private bool _autoRotateCamera = true;

        [Tooltip("Camera rotation speed in degrees per second")]
        [Range(1f, 60f)]
        [SerializeField] private float _rotationSpeed = 10f;

        [Tooltip("Camera distance from world center")]
        [Range(10f, 100f)]
        [SerializeField] private float _cameraDistance = 40f;

        [Tooltip("Camera height above world center")]
        [Range(5f, 50f)]
        [SerializeField] private float _cameraHeight = 20f;

        [Header("Performance Display")]
        [Tooltip("Show performance statistics in GUI")]
        [SerializeField] private bool _showPerformanceStats = true;

        #endregion

        #region Private Fields

        private CustomChunkManager _chunkManager;
        private ColorfulTerrainGenerator _generator;
        private Camera _mainCamera;
        private float _cameraRotationAngle = 0f;

        // Performance tracking
        private int _loadedChunkCount = 0;
        private int _totalVoxelCount = 0;
        private float _lastUpdateTime = 0f;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Validate required references
            if (_configuration == null)
            {
                Debug.LogError("[ColorfulCubesDemo] VoxelConfiguration is not assigned! Please assign it in the Inspector.");
                enabled = false;
                return;
            }

            if (_chunkMaterial == null)
            {
                Debug.LogWarning("[ColorfulCubesDemo] ChunkMaterial is not assigned. Chunks will use default material.");
            }

            // Get or create main camera
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                var cameraObj = new GameObject("DemoCamera");
                _mainCamera = cameraObj.AddComponent<Camera>();
                _mainCamera.tag = "MainCamera";
                Debug.LogWarning("[ColorfulCubesDemo] No main camera found. Created a new one.");
            }

            // Initialize camera position
            UpdateCameraPosition();
        }

        private void Start()
        {
            InitializeChunkSystem();
            LoadInitialChunks();

            Debug.Log($"[ColorfulCubesDemo] Initialized with pattern: {_currentPattern}, ViewDistance: {_viewDistance}");
        }

        private void Update()
        {
            // Update camera rotation
            if (_autoRotateCamera)
            {
                _cameraRotationAngle += _rotationSpeed * Time.deltaTime;
                UpdateCameraPosition();
            }

            // Process chunk generation and meshing queues
            if (_chunkManager != null)
            {
                _chunkManager.ProcessGenerationQueue();
                _chunkManager.ProcessMeshingQueue(Time.deltaTime);
            }

            // Update performance stats periodically
            if (Time.time - _lastUpdateTime > 0.5f)
            {
                UpdatePerformanceStats();
                _lastUpdateTime = Time.time;
            }
        }

        private void OnDestroy()
        {
            // Cleanup resources
            if (_chunkManager != null)
            {
                _chunkManager.Dispose();
                _chunkManager = null;
            }
        }

        private void OnValidate()
        {
            // React to Inspector changes in Edit mode
            if (!Application.isPlaying)
                return;

            // Reload chunks if pattern or view distance changed
            if (_chunkManager != null)
            {
                RegenerateWorld();
            }
        }

        private void OnGUI()
        {
            if (!_showPerformanceStats)
                return;

            // Performance statistics overlay
            GUILayout.BeginArea(new Rect(10, 10, 300, 150));
            GUILayout.Box("Colorful Cubes Demo - Performance");

            GUILayout.Label($"Pattern: {_currentPattern}");
            GUILayout.Label($"Loaded Chunks: {_loadedChunkCount}");
            GUILayout.Label($"Total Voxels: {_totalVoxelCount:N0}");

            if (_chunkManager != null)
            {
                GUILayout.Label($"Generation Queue: {_chunkManager.GenerationQueueCount}");
                GUILayout.Label($"Meshing Queue: {_chunkManager.MeshingQueueCount}");
            }

            GUILayout.Label($"FPS: {(int)(1f / Time.deltaTime)}");

            GUILayout.EndArea();
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize the chunk management system with custom generator.
        /// </summary>
        private void InitializeChunkSystem()
        {
            // Create generator with current pattern
            _generator = new ColorfulTerrainGenerator(
                _currentPattern,
                _randomSeed,
                new int3(_worldCenter.x, _worldCenter.y, _worldCenter.z)
            );

            // Create custom chunk manager that uses our generator
            _chunkManager = new CustomChunkManager(
                _configuration,
                _generator,
                transform,
                _chunkMaterial
            );

            Debug.Log("[ColorfulCubesDemo] Chunk system initialized successfully.");
        }

        /// <summary>
        /// Load initial chunks in a grid around the world center.
        /// </summary>
        private void LoadInitialChunks()
        {
            int halfDistance = _viewDistance / 2;

            // Load chunks in a grid pattern
            for (int x = -halfDistance; x <= halfDistance; x++)
            {
                for (int z = -halfDistance; z <= halfDistance; z++)
                {
                    for (int y = -1; y <= 1; y++) // Load 3 vertical layers
                    {
                        ChunkCoord coord = new ChunkCoord(
                            _worldCenter.x + x,
                            _worldCenter.y + y,
                            _worldCenter.z + z
                        );

                        _chunkManager.LoadChunk(coord);
                    }
                }
            }

            Debug.Log($"[ColorfulCubesDemo] Queued {(_viewDistance + 1) * (_viewDistance + 1) * 3} chunks for loading.");
        }

        #endregion

        #region Runtime Controls

        /// <summary>
        /// Change the current color pattern and regenerate the world.
        /// Can be called from UI buttons or Inspector.
        /// </summary>
        public void SetPattern(ColorPattern newPattern)
        {
            if (_currentPattern == newPattern)
                return;

            _currentPattern = newPattern;
            RegenerateWorld();

            Debug.Log($"[ColorfulCubesDemo] Pattern changed to: {newPattern}");
        }

        /// <summary>
        /// Change the view distance and reload chunks.
        /// </summary>
        public void SetViewDistance(int distance)
        {
            distance = Mathf.Clamp(distance, 1, 8);

            if (_viewDistance == distance)
                return;

            _viewDistance = distance;
            RegenerateWorld();

            Debug.Log($"[ColorfulCubesDemo] View distance changed to: {distance}");
        }

        /// <summary>
        /// Regenerate the entire world with current settings.
        /// Unloads all chunks and reloads them with the new pattern.
        /// </summary>
        private void RegenerateWorld()
        {
            if (_chunkManager == null)
                return;

            // Cleanup existing chunks
            _chunkManager.Dispose();

            // Reinitialize with new settings
            InitializeChunkSystem();
            LoadInitialChunks();

            Debug.Log("[ColorfulCubesDemo] World regenerated.");
        }

        #endregion

        #region Camera Controls

        /// <summary>
        /// Update camera position based on rotation angle and distance settings.
        /// Creates an orbital camera around the world center.
        /// </summary>
        private void UpdateCameraPosition()
        {
            if (_mainCamera == null)
                return;

            // Calculate orbital position
            float radians = _cameraRotationAngle * Mathf.Deg2Rad;
            float x = _worldCenter.x + Mathf.Cos(radians) * _cameraDistance;
            float z = _worldCenter.z + Mathf.Sin(radians) * _cameraDistance;
            float y = _worldCenter.y + _cameraHeight;

            _mainCamera.transform.position = new Vector3(x, y, z);

            // Look at world center
            Vector3 lookTarget = new Vector3(
                _worldCenter.x * _configuration.ChunkSize * _configuration.MacroVoxelSize,
                _worldCenter.y * _configuration.ChunkSize * _configuration.MacroVoxelSize,
                _worldCenter.z * _configuration.ChunkSize * _configuration.MacroVoxelSize
            );

            _mainCamera.transform.LookAt(lookTarget);
        }

        #endregion

        #region Performance Tracking

        /// <summary>
        /// Update performance statistics for display.
        /// </summary>
        private void UpdatePerformanceStats()
        {
            if (_chunkManager == null)
                return;

            _loadedChunkCount = 0;
            _totalVoxelCount = 0;

            foreach (var chunk in _chunkManager.GetAllChunks())
            {
                _loadedChunkCount++;
                _totalVoxelCount += _configuration.ChunkVolume;
            }
        }

        #endregion

        #region Custom Chunk Manager

        /// <summary>
        /// Custom ChunkManager that uses ColorfulTerrainGenerator instead of procedural noise.
        /// Extends the base ChunkManager to inject our custom generator.
        /// </summary>
        private class CustomChunkManager : ChunkManager
        {
            private readonly ColorfulTerrainGenerator _customGenerator;
            private readonly VoxelConfiguration _config;

            public CustomChunkManager(
                VoxelConfiguration config,
                ColorfulTerrainGenerator generator,
                Transform chunkParent,
                Material chunkMaterial)
                : base(config, chunkParent, chunkMaterial)
            {
                _customGenerator = generator;
                _config = config;
            }

            /// <summary>
            /// Override LoadChunk to use our custom generator.
            /// This bypasses the default procedural terrain generation.
            /// </summary>
            public new void LoadChunk(ChunkCoord coord)
            {
                if (IsChunkLoaded(coord))
                    return;

                // Use base method to create chunk infrastructure
                base.LoadChunk(coord);

                // Immediately generate using our custom generator
                var chunk = GetChunk(coord);
                if (chunk != null)
                {
                    // Generate voxel data
                    var voxelData = _customGenerator.Generate(coord, _config.ChunkSize, Allocator.Temp);

                    // Copy to chunk's voxel data
                    chunk.VoxelData.CopyFrom(voxelData);

                    // Cleanup temporary data
                    voxelData.Dispose();

                    // Mark as generated and dirty for meshing
                    chunk.MarkGenerated();
                    MarkDirty(coord);
                }
            }
        }

        #endregion
    }
}
