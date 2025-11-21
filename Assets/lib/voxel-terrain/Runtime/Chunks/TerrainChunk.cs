using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Represents a single terrain chunk with voxel data and mesh.
    /// Manages lifecycle: generation → meshing → rendering.
    /// </summary>
    public class TerrainChunk
    {
        public ChunkCoord Coord { get; private set; }
        public GameObject GameObject { get; private set; }

        private NativeArray<VoxelType> _voxelData;
        public NativeArray<VoxelType> VoxelData => _voxelData;

        public bool IsGenerated { get; private set; }
        public bool IsMeshed { get; private set; }
        public bool IsDirty { get; set; }

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        public TerrainChunk(ChunkCoord coord, Transform parent, Material material)
        {
            Coord = coord;

            // Create GameObject for this chunk
            GameObject = new GameObject($"Chunk_{coord.X}_{coord.Y}_{coord.Z}");
            GameObject.transform.SetParent(parent);

            // Add required components
            _meshFilter = GameObject.AddComponent<MeshFilter>();
            _meshRenderer = GameObject.AddComponent<MeshRenderer>();
            _meshCollider = GameObject.AddComponent<MeshCollider>();

            _meshRenderer.material = material;

            IsGenerated = false;
            IsMeshed = false;
            IsDirty = false;
        }

        /// <summary>
        /// Allocate voxel data array for this chunk.
        /// </summary>
        public void AllocateVoxelData(int chunkSize)
        {
            int volume = chunkSize * chunkSize * chunkSize;
            _voxelData = new NativeArray<VoxelType>(volume, Allocator.Persistent);
        }

        /// <summary>
        /// Set the chunk's mesh.
        /// </summary>
        public void SetMesh(Mesh mesh)
        {
            _meshFilter.mesh = mesh;
            _meshCollider.sharedMesh = mesh;
            IsMeshed = true;
            IsDirty = false;
        }

        /// <summary>
        /// Set the world position of this chunk.
        /// </summary>
        public void SetWorldPosition(Vector3 position)
        {
            GameObject.transform.position = position;
        }

        /// <summary>
        /// Get a voxel at a local coordinate within this chunk.
        /// </summary>
        public VoxelType GetVoxel(int3 localCoord, int chunkSize)
        {
            if (!VoxelMath.IsValidLocalCoord(localCoord, chunkSize))
                return VoxelType.Air;

            int index = VoxelMath.Flatten3DIndex(localCoord.x, localCoord.y, localCoord.z, chunkSize);
            return VoxelData[index];
        }

        /// <summary>
        /// Set a voxel at a local coordinate within this chunk.
        /// </summary>
        public void SetVoxel(int3 localCoord, VoxelType type, int chunkSize)
        {
            if (!VoxelMath.IsValidLocalCoord(localCoord, chunkSize))
                return;

            int index = VoxelMath.Flatten3DIndex(localCoord.x, localCoord.y, localCoord.z, chunkSize);
            _voxelData[index] = type;
            IsDirty = true;
        }

        /// <summary>
        /// Mark this chunk as generated.
        /// </summary>
        public void MarkGenerated()
        {
            IsGenerated = true;
        }

        /// <summary>
        /// Dispose of native resources.
        /// </summary>
        public void Dispose()
        {
            if (_voxelData.IsCreated)
            {
                _voxelData.Dispose();
            }

            if (GameObject != null)
            {
                Object.Destroy(GameObject);
            }
        }

        /// <summary>
        /// Show or hide this chunk.
        /// </summary>
        public void SetActive(bool active)
        {
            if (GameObject != null)
            {
                GameObject.SetActive(active);
            }
        }
    }
}
