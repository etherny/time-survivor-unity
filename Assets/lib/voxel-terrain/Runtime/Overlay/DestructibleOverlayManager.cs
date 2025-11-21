using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Manages destructible overlay chunks (micro-voxels) for props.
    /// Handles generation, destruction, and lifecycle of overlay chunks.
    /// </summary>
    public class DestructibleOverlayManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private VoxelConfiguration _config;
        [SerializeField] private Material _overlayMaterial;

        [Header("Streaming")]
        [SerializeField] private Transform _streamingTarget;
        [SerializeField] private bool _useMainCamera = true;

        private Dictionary<ChunkCoord, OverlayChunk> _overlayChunks;
        private Transform _overlayParent;

        private void Awake()
        {
            // Validation
            if (_config == null)
            {
                Debug.LogError("[DestructibleOverlayManager] VoxelConfiguration is not assigned!");
                enabled = false;
                return;
            }

            if (_overlayMaterial == null)
            {
                Debug.LogWarning("[DestructibleOverlayManager] Overlay material not assigned. Creating default.");
                _overlayMaterial = new Material(Shader.Find("Standard"));
            }

            // Initialize
            _overlayChunks = new Dictionary<ChunkCoord, OverlayChunk>();

            var parentObj = new GameObject("OverlayChunks");
            parentObj.transform.SetParent(transform);
            _overlayParent = parentObj.transform;

            // Find streaming target
            if (_useMainCamera)
            {
                _streamingTarget = Camera.main?.transform;
            }
        }

        /// <summary>
        /// Load an overlay chunk at the specified coordinate.
        /// </summary>
        public void LoadOverlayChunk(ChunkCoord coord)
        {
            if (_overlayChunks.ContainsKey(coord))
                return;

            var overlayChunk = new OverlayChunk(coord, _overlayParent, _overlayMaterial);
            overlayChunk.AllocateVoxelData(_config.ChunkSize);

            // Set world position (using micro voxel scale)
            float3 worldPos = VoxelMath.ChunkCoordToWorld(coord, _config.ChunkSize, _config.MicroVoxelSize);
            overlayChunk.SetWorldPosition(worldPos);

            _overlayChunks[coord] = overlayChunk;

            // Generate overlay content (trees, rocks, etc.)
            GenerateOverlayContent(overlayChunk);
        }

        /// <summary>
        /// Unload an overlay chunk.
        /// </summary>
        public void UnloadOverlayChunk(ChunkCoord coord)
        {
            if (_overlayChunks.TryGetValue(coord, out var chunk))
            {
                chunk.Dispose();
                _overlayChunks.Remove(coord);
            }
        }

        /// <summary>
        /// Check if an overlay chunk is loaded.
        /// </summary>
        public bool IsOverlayChunkLoaded(ChunkCoord coord)
        {
            return _overlayChunks.ContainsKey(coord);
        }

        /// <summary>
        /// Get overlay chunk at coordinate.
        /// </summary>
        public OverlayChunk GetOverlayChunk(ChunkCoord coord)
        {
            return _overlayChunks.TryGetValue(coord, out var chunk) ? chunk : null;
        }

        /// <summary>
        /// Apply damage to a voxel at world position.
        /// Returns true if voxel was destroyed.
        /// </summary>
        public bool DamageVoxelAt(float3 worldPosition, byte damageAmount)
        {
            // Convert world position to chunk coord and local coord
            ChunkCoord chunkCoord = VoxelMath.WorldToChunkCoord(
                worldPosition,
                _config.ChunkSize,
                _config.MicroVoxelSize
            );

            if (!_overlayChunks.TryGetValue(chunkCoord, out var chunk))
                return false;

            // Get local coordinate within chunk
            int3 voxelCoord = VoxelMath.WorldToVoxelCoord(worldPosition, _config.MicroVoxelSize);
            int3 localCoord = VoxelMath.VoxelToLocalCoord(voxelCoord, _config.ChunkSize);

            return chunk.DamageVoxel(localCoord, damageAmount, _config.ChunkSize);
        }

        /// <summary>
        /// Generate procedural overlay content (trees, rocks, etc.).
        /// This is a placeholder - implement actual generation logic.
        /// </summary>
        private void GenerateOverlayContent(OverlayChunk chunk)
        {
            // TODO: Implement procedural generation of props
            // For now, leave empty (all air)

            chunk.MarkGenerated();

            // Only mesh if not empty
            if (!chunk.IsEmpty())
            {
                // Queue for meshing (TODO: integrate with meshing system)
            }
        }

        /// <summary>
        /// Cleanup all overlay chunks.
        /// </summary>
        private void OnDestroy()
        {
            foreach (var chunk in _overlayChunks.Values)
            {
                chunk.Dispose();
            }
            _overlayChunks.Clear();
        }

        /// <summary>
        /// Get count of loaded overlay chunks.
        /// </summary>
        public int LoadedOverlayChunkCount => _overlayChunks.Count;
    }
}
