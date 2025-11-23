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

        /// <summary>
        /// Indicates if this chunk has collision mesh assigned.
        /// </summary>
        public bool HasCollision { get; private set; }

        /// <summary>
        /// Indicates if collision is pending (queued for baking).
        /// </summary>
        public bool IsCollisionPending { get; private set; }

        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;

        public TerrainChunk(ChunkCoord coord, Transform parent, Material material, float voxelSize = 1f)
        {
            Coord = coord;

            // Create GameObject for this chunk
            GameObject = new GameObject($"Chunk_{coord.X}_{coord.Y}_{coord.Z}");
            GameObject.transform.SetParent(parent);

            // Scale GameObject by voxel size so mesh vertices (in voxel coords) map to world units
            GameObject.transform.localScale = Vector3.one * voxelSize;

            // Add required components for rendering
            _meshFilter = GameObject.AddComponent<MeshFilter>();
            _meshRenderer = GameObject.AddComponent<MeshRenderer>();

            _meshRenderer.material = material;

            // Note: MeshCollider is NOT added by default - managed separately via collision system
            _meshCollider = null;

            IsGenerated = false;
            IsMeshed = false;
            IsDirty = false;
            HasCollision = false;
            IsCollisionPending = false;
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
        /// Set the chunk's render mesh.
        /// Note: This does NOT set collision mesh - use SetCollisionMesh() for collision.
        /// </summary>
        public void SetMesh(Mesh mesh)
        {
            _meshFilter.mesh = mesh;
            // DO NOT assign to collider here - collision is managed separately
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
        /// Set the collision mesh for this chunk and assign to specified physics layer.
        /// Creates MeshCollider if needed.
        /// IMPORTANT: Also adds a kinematic Rigidbody to ensure CharacterController can detect the collision.
        /// </summary>
        /// <param name="collisionMesh">Collision mesh to assign</param>
        /// <param name="layerName">Physics layer name (e.g., "TerrainStatic")</param>
        public void SetCollisionMesh(Mesh collisionMesh, string layerName)
        {
            if (collisionMesh == null)
            {
                Debug.LogWarning($"[TerrainChunk] Attempted to set null collision mesh on chunk {Coord}");
                return;
            }

            // Get or create MeshCollider
            if (_meshCollider == null)
            {
                _meshCollider = GameObject.AddComponent<MeshCollider>();
            }

            // Add Rigidbody (kinematic) if not present
            // This is REQUIRED for CharacterController to detect the MeshCollider
            // CharacterController only collides with colliders that have a Rigidbody component
            var rigidbody = GameObject.GetComponent<Rigidbody>();
            if (rigidbody == null)
            {
                rigidbody = GameObject.AddComponent<Rigidbody>();
                rigidbody.isKinematic = true;  // No physics simulation (static terrain)
                rigidbody.useGravity = false;   // No gravity
                rigidbody.constraints = RigidbodyConstraints.FreezeAll; // Freeze all movement/rotation
            }

            _meshCollider.sharedMesh = collisionMesh;
            _meshCollider.convex = false; // Terrain uses non-convex collision

            // Set physics layer
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                GameObject.layer = layer;
            }
            else
            {
                Debug.LogWarning($"[TerrainChunk] Layer '{layerName}' not found. Using default layer.");
            }

            HasCollision = true;
            IsCollisionPending = false;
        }

        /// <summary>
        /// Remove collision from this chunk.
        /// Destroys MeshCollider component and clears collision mesh.
        /// Also removes the Rigidbody component if present.
        /// </summary>
        public void RemoveCollision()
        {
            if (_meshCollider != null)
            {
                #if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying)
                {
                    Object.DestroyImmediate(_meshCollider);
                }
                else
                #endif
                {
                    Object.Destroy(_meshCollider);
                }
                _meshCollider = null;
            }

            // Clean up Rigidbody if present
            var rigidbody = GameObject.GetComponent<Rigidbody>();
            if (rigidbody != null)
            {
                #if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying)
                {
                    Object.DestroyImmediate(rigidbody);
                }
                else
                #endif
                {
                    Object.Destroy(rigidbody);
                }
            }

            HasCollision = false;
            IsCollisionPending = false;
        }

        /// <summary>
        /// Mark this chunk as pending collision baking.
        /// Used by async collision system to track state.
        /// </summary>
        public void MarkCollisionPending()
        {
            IsCollisionPending = true;
        }

        /// <summary>
        /// Dispose of native resources.
        /// Cleans up Rigidbody, MeshCollider, and GameObject.
        /// </summary>
        public void Dispose()
        {
            if (_voxelData.IsCreated)
            {
                _voxelData.Dispose();
            }

            if (GameObject != null)
            {
                // Clean up Rigidbody if present
                var rigidbody = GameObject.GetComponent<Rigidbody>();
                if (rigidbody != null)
                {
                    #if UNITY_EDITOR
                    if (!UnityEngine.Application.isPlaying)
                    {
                        Object.DestroyImmediate(rigidbody);
                    }
                    else
                    #endif
                    {
                        Object.Destroy(rigidbody);
                    }
                }

                #if UNITY_EDITOR
                if (!UnityEngine.Application.isPlaying)
                {
                    Object.DestroyImmediate(GameObject);
                }
                else
                #endif
                {
                    Object.Destroy(GameObject);
                }
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
