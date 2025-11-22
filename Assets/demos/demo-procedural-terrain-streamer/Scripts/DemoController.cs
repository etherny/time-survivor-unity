using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using VoxelStreamer = TimeSurvivor.Voxel.Streaming.ProceduralTerrainStreamer;

namespace TimeSurvivor.Demos.ProceduralTerrainStreamer
{
    /// <summary>
    /// Controls and orchestrates the ProceduralTerrainStreamer demonstration.
    /// Displays real-time statistics, instructions, and debug gizmos.
    /// </summary>
    public class DemoController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private VoxelStreamer terrainStreamer;
        [SerializeField] private Transform player;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private TextMeshProUGUI instructionsText;

        [Header("Debug Visualization")]
        [SerializeField] public bool showGizmos = true;
        [SerializeField] public Color loadRadiusColor = Color.green;
        [SerializeField] public Color unloadRadiusColor = Color.red;

        [Header("FPS Calculation")]
        [SerializeField] private float fpsUpdateInterval = 1f;

        private float fpsTimer = 0f;
        private int frameCount = 0;
        private float currentFps = 0f;

        private const string INSTRUCTIONS_TEXT = @"=== CONTRÔLES ===
WASD: Déplacer le joueur
Shift: Sprint (2x vitesse)
G: Toggle Gizmos debug

=== VALIDATION ===
✅ Déplacez-vous: chunks load automatiquement
✅ Revenez en arrière: chunks load instantanément (cache)
✅ Sprintez loin: max 1 chunk load/frame (smooth)";

        void Start()
        {
            ValidateSetup();
            InitializeUI();
        }

        void Update()
        {
            UpdateFPS();
            UpdateStatistics();
            HandleInput();
        }

        private void ValidateSetup()
        {
            bool isValid = true;

            if (terrainStreamer == null)
            {
                Debug.LogError("[DemoController] ProceduralTerrainStreamer reference is missing!");
                isValid = false;
            }

            if (player == null)
            {
                Debug.LogError("[DemoController] Player Transform reference is missing!");
                isValid = false;
            }

            if (statsText == null)
            {
                Debug.LogWarning("[DemoController] Stats TextMeshProUGUI reference is missing. Stats will not be displayed.");
            }

            if (instructionsText == null)
            {
                Debug.LogWarning("[DemoController] Instructions TextMeshProUGUI reference is missing. Instructions will not be displayed.");
            }

            if (isValid)
            {
                Debug.Log("[DemoController] Demo setup validated successfully.");
            }
            else
            {
                Debug.LogError("[DemoController] Demo setup validation FAILED. Please check references in Inspector.");
            }
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
            if (statsText == null || terrainStreamer == null || player == null)
                return;

            // Get streaming stats
            int activeChunks = terrainStreamer.ActiveChunkCount;
            int cachedChunks = terrainStreamer.CachedChunkCount;
            int maxChunks = terrainStreamer.MaxCachedChunks;
            float loadRadius = terrainStreamer.LoadRadius;
            float unloadRadius = terrainStreamer.UnloadRadius;
            int maxChunksPerFrame = terrainStreamer.MaxChunksPerFrame;

            // Calculate hysteresis
            float hysteresis = unloadRadius - loadRadius;

            // Get player position
            Vector3 playerPos = player.position;

            // Estimate memory usage (rough estimate: 1 chunk ≈ 2-3 MB with voxel data + mesh)
            float estimatedMemoryMB = (activeChunks + cachedChunks) * 2.5f;

            // Build stats text
            statsText.text = $@"=== PROCEDURAL TERRAIN STREAMER ===
FPS: {currentFps:F0} (moyenne {fpsUpdateInterval}s)
Active Chunks: {activeChunks} / {maxChunks}
Cached Chunks: {cachedChunks} / {maxChunks}
Memory: ~{estimatedMemoryMB:F1} MB (estimated)
Player Position: ({playerPos.x:F1}, {playerPos.y:F1}, {playerPos.z:F1})

Load Radius: {loadRadius}m
Unload Radius: {unloadRadius}m (hysteresis: {hysteresis}m)
Max Chunks/Frame: {maxChunksPerFrame}

Gizmos: {(showGizmos ? "ON (G to toggle)" : "OFF (G to toggle)")}";
        }

        private void HandleInput()
        {
            // Use new Input System
            if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            {
                showGizmos = !showGizmos;
                Debug.Log($"[DemoController] Gizmos debug: {(showGizmos ? "ON" : "OFF")}");
            }
        }

        private void OnDrawGizmos()
        {
            if (!showGizmos || player == null || terrainStreamer == null)
                return;

            Vector3 playerPos = player.position;
            float loadRadius = terrainStreamer.LoadRadius;
            float unloadRadius = terrainStreamer.UnloadRadius;

            // Draw load radius (green)
            Gizmos.color = loadRadiusColor;
            DrawWireSphere(playerPos, loadRadius);

            // Draw unload radius (red)
            Gizmos.color = unloadRadiusColor;
            DrawWireSphere(playerPos, unloadRadius);
        }

        private void DrawWireSphere(Vector3 center, float radius, int segments = 32)
        {
            // Draw horizontal circle
            DrawCircle(center, radius, Vector3.up, segments);

            // Draw vertical circle (X-Y plane)
            DrawCircle(center, radius, Vector3.forward, segments);

            // Draw vertical circle (Z-Y plane)
            DrawCircle(center, radius, Vector3.right, segments);
        }

        private void DrawCircle(Vector3 center, float radius, Vector3 normal, int segments)
        {
            Vector3 forward = Vector3.Slerp(normal, -normal, 0.5f);
            if (forward == Vector3.zero)
                forward = Vector3.up;

            Vector3 right = Vector3.Cross(normal, forward).normalized;
            forward = Vector3.Cross(right, normal).normalized;

            Vector3 prevPoint = center + right * radius;

            for (int i = 1; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                Vector3 nextPoint = center + (right * Mathf.Cos(angle) + forward * Mathf.Sin(angle)) * radius;
                Gizmos.DrawLine(prevPoint, nextPoint);
                prevPoint = nextPoint;
            }
        }
    }
}
