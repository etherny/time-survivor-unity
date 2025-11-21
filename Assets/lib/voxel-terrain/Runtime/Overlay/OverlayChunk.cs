using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Terrain
{
    /// <summary>
    /// Overlay chunk for destructible micro-voxels (ADR-006).
    /// Layered on top of terrain chunks for props like trees, rocks, buildings.
    /// Uses MicroVoxelData with health system.
    /// </summary>
    public class OverlayChunk
    {
        public ChunkCoord Coord { get; private set; }
        public GameObject GameObject { get; private set; }

        private NativeArray<MicroVoxelData> _voxelData;
        public NativeArray<MicroVoxelData> VoxelData => _voxelData;

        public bool IsGenerated { get; private set; }
        public bool IsMeshed { get; private set; }
        public bool IsDirty { get; set; }

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        public OverlayChunk(ChunkCoord coord, Transform parent, Material material)
        {
            Coord = coord;

            // Create GameObject for overlay chunk
            GameObject = new GameObject($"Overlay_{coord.X}_{coord.Y}_{coord.Z}");
            GameObject.transform.SetParent(parent);

            // Add rendering components
            _meshFilter = GameObject.AddComponent<MeshFilter>();
            _meshRenderer = GameObject.AddComponent<MeshRenderer>();
            _meshCollider = GameObject.AddComponent<MeshCollider>();

            _meshRenderer.material = material;

            IsGenerated = false;
            IsMeshed = false;
            IsDirty = false;
        }

        /// <summary>
        /// Allocate micro-voxel data array for this overlay chunk.
        /// </summary>
        public void AllocateVoxelData(int chunkSize)
        {
            int volume = chunkSize * chunkSize * chunkSize;
            _voxelData = new NativeArray<MicroVoxelData>(volume, Allocator.Persistent);

            // Initialize all as air
            for (int i = 0; i < volume; i++)
            {
                _voxelData[i] = new MicroVoxelData(VoxelType.Air, 0);
            }
        }

        /// <summary>
        /// Set the overlay chunk's mesh.
        /// </summary>
        public void SetMesh(Mesh mesh)
        {
            _meshFilter.mesh = mesh;
            _meshCollider.sharedMesh = mesh;
            IsMeshed = true;
            IsDirty = false;
        }

        /// <summary>
        /// Set the world position of this overlay chunk.
        /// </summary>
        public void SetWorldPosition(Vector3 position)
        {
            GameObject.transform.position = position;
        }

        /// <summary>
        /// Get a micro-voxel at a local coordinate.
        /// </summary>
        public MicroVoxelData GetVoxel(int3 localCoord, int chunkSize)
        {
            if (!VoxelMath.IsValidLocalCoord(localCoord, chunkSize))
                return new MicroVoxelData(VoxelType.Air, 0);

            int index = VoxelMath.Flatten3DIndex(localCoord.x, localCoord.y, localCoord.z, chunkSize);
            return VoxelData[index];
        }

        /// <summary>
        /// Set a micro-voxel at a local coordinate.
        /// </summary>
        public void SetVoxel(int3 localCoord, MicroVoxelData data, int chunkSize)
        {
            if (!VoxelMath.IsValidLocalCoord(localCoord, chunkSize))
                return;

            int index = VoxelMath.Flatten3DIndex(localCoord.x, localCoord.y, localCoord.z, chunkSize);
            _voxelData[index] = data;
            IsDirty = true;
        }

        /// <summary>
        /// Apply damage to a voxel at local coordinate.
        /// Returns true if voxel was destroyed.
        /// </summary>
        public bool DamageVoxel(int3 localCoord, byte damageAmount, int chunkSize)
        {
            if (!VoxelMath.IsValidLocalCoord(localCoord, chunkSize))
                return false;

            int index = VoxelMath.Flatten3DIndex(localCoord.x, localCoord.y, localCoord.z, chunkSize);
            MicroVoxelData voxel = _voxelData[index];

            // Apply damage immutably and write back the new struct
            MicroVoxelData damagedVoxel = voxel.WithDamage(damageAmount);
            _voxelData[index] = damagedVoxel;

            bool destroyed = damagedVoxel.IsDestroyed;
            if (destroyed)
            {
                IsDirty = true;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Mark this overlay chunk as generated.
        /// </summary>
        public void MarkGenerated()
        {
            IsGenerated = true;
        }

        /// <summary>
        /// Check if this overlay chunk is empty (all air).
        /// </summary>
        public bool IsEmpty()
        {
            for (int i = 0; i < VoxelData.Length; i++)
            {
                if (VoxelData[i].IsSolid)
                    return false;
            }
            return true;
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
        /// Show or hide this overlay chunk.
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
