using UnityEngine;
using UnityEngine.UI;
using TimeSurvivor.Voxel.Terrain;

namespace TimeSurvivor.Demos.MinecraftTerrain
{
    /// <summary>
    /// Demo controller for Minecraft-style terrain generation.
    /// Displays generation progress and statistics in Unity Console and optional UI.
    ///
    /// Features:
    /// - Real-time progress logging during generation
    /// - Completion statistics (time, chunks, avg time per chunk)
    /// - Optional UI Text display for in-game progress
    /// - Post-generation terrain analysis (voxel type distribution)
    ///
    /// Usage:
    /// 1. Attach to GameObject in demo scene
    /// 2. Assign MinecraftTerrainGenerator reference
    /// 3. Optionally assign UI Text for progress display
    /// </summary>
    public class MinecraftTerrainDemoController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("MinecraftTerrainGenerator to monitor")]
        [SerializeField] private MinecraftTerrainGenerator _terrainGenerator;

        [Header("UI (Optional)")]
        [Tooltip("Optional UI Text for displaying generation progress")]
        [SerializeField] private Text _progressText;

        [Tooltip("Optional UI Text for displaying completion stats")]
        [SerializeField] private Text _statsText;

        private int _totalChunks;
        private System.Diagnostics.Stopwatch _stopwatch;

        private void Start()
        {
            if (_terrainGenerator == null)
            {
                Debug.LogError("[MinecraftTerrainDemoController] TerrainGenerator is null. Assign in Inspector.");
                return;
            }

            // Subscribe to terrain generation events
            _terrainGenerator.OnGenerationStarted.AddListener(OnGenerationStarted);
            _terrainGenerator.OnGenerationProgress.AddListener(OnGenerationProgress);
            _terrainGenerator.OnGenerationCompleted.AddListener(OnGenerationCompleted);
            _terrainGenerator.OnGenerationFailed.AddListener(OnGenerationFailed);

            Debug.Log("[MinecraftTerrainDemoController] Demo controller initialized. Waiting for terrain generation...");
        }

        private void OnDestroy()
        {
            // Unsubscribe from events to avoid memory leaks
            if (_terrainGenerator != null)
            {
                _terrainGenerator.OnGenerationStarted.RemoveListener(OnGenerationStarted);
                _terrainGenerator.OnGenerationProgress.RemoveListener(OnGenerationProgress);
                _terrainGenerator.OnGenerationCompleted.RemoveListener(OnGenerationCompleted);
                _terrainGenerator.OnGenerationFailed.RemoveListener(OnGenerationFailed);
            }
        }

        // ========== Event Handlers ==========

        private void OnGenerationStarted()
        {
            Debug.Log("=== TERRAIN GENERATION STARTED ===");

            _stopwatch = System.Diagnostics.Stopwatch.StartNew();

            if (_progressText != null)
            {
                _progressText.text = "Generating terrain...";
            }

            if (_statsText != null)
            {
                _statsText.text = "";
            }
        }

        private void OnGenerationProgress(int currentChunks, int totalChunks)
        {
            _totalChunks = totalChunks;
            float progressPercent = (float)currentChunks / totalChunks * 100f;

            // Log progress every 10%
            if (currentChunks % Mathf.Max(1, totalChunks / 10) == 0)
            {
                Debug.Log($"[PROGRESS] Generating terrain... {currentChunks}/{totalChunks} chunks ({progressPercent:F1}%)");
            }

            // Update UI
            if (_progressText != null)
            {
                _progressText.text = $"Generating terrain...\n{currentChunks}/{totalChunks} chunks ({progressPercent:F1}%)";
            }
        }

        private void OnGenerationCompleted(float elapsedMs)
        {
            _stopwatch?.Stop();

            float avgMsPerChunk = _totalChunks > 0 ? elapsedMs / _totalChunks : 0f;

            // Log completion stats
            Debug.Log("=== TERRAIN GENERATION COMPLETED ===");
            Debug.Log($"  ✅ Total Time: {elapsedMs:F0}ms ({elapsedMs / 1000f:F2}s)");
            Debug.Log($"  ✅ Chunks: {_totalChunks}");
            Debug.Log($"  ✅ Avg Time/Chunk: {avgMsPerChunk:F2}ms");
            Debug.Log("=====================================");

            // Update UI
            if (_progressText != null)
            {
                _progressText.text = $"✅ Generation Complete!\n{_totalChunks} chunks in {elapsedMs / 1000f:F2}s";
            }

            // Analyze terrain (voxel distribution)
            AnalyzeTerrain();
        }

        private void OnGenerationFailed(string errorMessage)
        {
            Debug.LogError($"=== TERRAIN GENERATION FAILED ===");
            Debug.LogError($"  ❌ Error: {errorMessage}");
            Debug.LogError("==================================");

            if (_progressText != null)
            {
                _progressText.text = $"❌ Generation Failed!\n{errorMessage}";
            }
        }

        // ========== Terrain Analysis ==========

        /// <summary>
        /// Analyze generated terrain and display voxel type distribution statistics.
        /// </summary>
        private void AnalyzeTerrain()
        {
            if (_terrainGenerator == null || _terrainGenerator.ChunkManager == null)
            {
                Debug.LogWarning("[MinecraftTerrainDemoController] Cannot analyze terrain - ChunkManager is null");
                return;
            }

            string statsString = TerrainStatsAnalyzer.AnalyzeTerrain(_terrainGenerator.ChunkManager);

            Debug.Log("=== TERRAIN STATISTICS ===");
            Debug.Log(statsString);
            Debug.Log("==========================");

            if (_statsText != null)
            {
                _statsText.text = statsString;
            }
        }
    }
}
