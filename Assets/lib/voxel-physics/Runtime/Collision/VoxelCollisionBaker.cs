using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Physics
{
    /// <summary>
    /// Bakes collision meshes from voxel data using simplified resolution.
    /// Supports both synchronous and asynchronous (Job System) baking.
    /// </summary>
    public static class VoxelCollisionBaker
    {
        /// <summary>
        /// Collision baking job with Burst compilation for high performance.
        /// Generates simplified collision mesh at reduced resolution.
        /// </summary>
        [BurstCompile]
        public struct CollisionBakingJob : IJob
        {
            [ReadOnly] public NativeArray<VoxelType> SourceVoxels;
            [ReadOnly] public int SourceChunkSize;
            [ReadOnly] public int ResolutionDivider;

            public NativeList<float3> Vertices;
            public NativeList<int> Triangles;

            public void Execute()
            {
                int targetSize = SourceChunkSize / ResolutionDivider;

                // Process at reduced resolution
                for (int y = 0; y < targetSize; y++)
                {
                    for (int z = 0; z < targetSize; z++)
                    {
                        for (int x = 0; x < targetSize; x++)
                        {
                            // Sample from source voxels at reduced resolution
                            int3 sourcePos = new int3(
                                x * ResolutionDivider,
                                y * ResolutionDivider,
                                z * ResolutionDivider
                            );

                            // Check if this reduced-resolution voxel should be solid
                            if (IsSolidAtReducedResolution(sourcePos))
                            {
                                // Check if voxel has any exposed faces
                                if (HasExposedFace(x, y, z, targetSize))
                                {
                                    // Add box geometry for this collision voxel
                                    float3 position = new float3(x, y, z) * ResolutionDivider;
                                    float scale = ResolutionDivider;
                                    AddVoxelBox(position, scale);
                                }
                            }
                        }
                    }
                }
            }

            /// <summary>
            /// Check if a reduced-resolution voxel should be considered solid.
            /// Samples the dominant voxel type in the source region.
            /// </summary>
            private bool IsSolidAtReducedResolution(int3 sourcePos)
            {
                // Sample center voxel of the region (simple approach)
                int3 centerOffset = new int3(ResolutionDivider / 2);
                int3 samplePos = sourcePos + centerOffset;

                // Clamp to source bounds
                samplePos = math.clamp(samplePos, int3.zero, new int3(SourceChunkSize - 1));

                int index = Flatten3DIndex(samplePos.x, samplePos.y, samplePos.z, SourceChunkSize);
                if (index >= 0 && index < SourceVoxels.Length)
                {
                    return SourceVoxels[index] != VoxelType.Air;
                }

                return false;
            }

            /// <summary>
            /// Check if a collision voxel has any exposed faces (borders air).
            /// </summary>
            private bool HasExposedFace(int x, int y, int z, int size)
            {
                // Check all 6 neighbors in reduced-resolution space
                if (x > 0 && !IsSolidAtReducedResolution(new int3((x - 1) * ResolutionDivider, y * ResolutionDivider, z * ResolutionDivider))) return true;
                if (x < size - 1 && !IsSolidAtReducedResolution(new int3((x + 1) * ResolutionDivider, y * ResolutionDivider, z * ResolutionDivider))) return true;
                if (y > 0 && !IsSolidAtReducedResolution(new int3(x * ResolutionDivider, (y - 1) * ResolutionDivider, z * ResolutionDivider))) return true;
                if (y < size - 1 && !IsSolidAtReducedResolution(new int3(x * ResolutionDivider, (y + 1) * ResolutionDivider, z * ResolutionDivider))) return true;
                if (z > 0 && !IsSolidAtReducedResolution(new int3(x * ResolutionDivider, y * ResolutionDivider, (z - 1) * ResolutionDivider))) return true;
                if (z < size - 1 && !IsSolidAtReducedResolution(new int3(x * ResolutionDivider, y * ResolutionDivider, (z + 1) * ResolutionDivider))) return true;

                // Check boundaries (always expose at chunk edges)
                if (x == 0 || x == size - 1) return true;
                if (y == 0 || y == size - 1) return true;
                if (z == 0 || z == size - 1) return true;

                return false;
            }

            /// <summary>
            /// Add a box to the collision mesh.
            /// </summary>
            private void AddVoxelBox(float3 position, float scale)
            {
                int startIndex = Vertices.Length;

                // Define 8 vertices of a unit cube scaled and positioned
                float3 p0 = position + new float3(0, 0, 0);
                float3 p1 = position + new float3(scale, 0, 0);
                float3 p2 = position + new float3(scale, scale, 0);
                float3 p3 = position + new float3(0, scale, 0);
                float3 p4 = position + new float3(0, 0, scale);
                float3 p5 = position + new float3(scale, 0, scale);
                float3 p6 = position + new float3(scale, scale, scale);
                float3 p7 = position + new float3(0, scale, scale);

                // Add vertices
                Vertices.Add(p0);
                Vertices.Add(p1);
                Vertices.Add(p2);
                Vertices.Add(p3);
                Vertices.Add(p4);
                Vertices.Add(p5);
                Vertices.Add(p6);
                Vertices.Add(p7);

                // Add triangles (12 triangles for 6 faces)
                // Front face (Z-)
                Triangles.Add(startIndex + 0);
                Triangles.Add(startIndex + 2);
                Triangles.Add(startIndex + 1);
                Triangles.Add(startIndex + 0);
                Triangles.Add(startIndex + 3);
                Triangles.Add(startIndex + 2);

                // Back face (Z+)
                Triangles.Add(startIndex + 5);
                Triangles.Add(startIndex + 6);
                Triangles.Add(startIndex + 4);
                Triangles.Add(startIndex + 4);
                Triangles.Add(startIndex + 6);
                Triangles.Add(startIndex + 7);

                // Left face (X-)
                Triangles.Add(startIndex + 4);
                Triangles.Add(startIndex + 7);
                Triangles.Add(startIndex + 0);
                Triangles.Add(startIndex + 0);
                Triangles.Add(startIndex + 7);
                Triangles.Add(startIndex + 3);

                // Right face (X+)
                Triangles.Add(startIndex + 1);
                Triangles.Add(startIndex + 6);
                Triangles.Add(startIndex + 5);
                Triangles.Add(startIndex + 1);
                Triangles.Add(startIndex + 2);
                Triangles.Add(startIndex + 6);

                // Top face (Y+)
                Triangles.Add(startIndex + 3);
                Triangles.Add(startIndex + 7);
                Triangles.Add(startIndex + 6);
                Triangles.Add(startIndex + 3);
                Triangles.Add(startIndex + 6);
                Triangles.Add(startIndex + 2);

                // Bottom face (Y-)
                Triangles.Add(startIndex + 4);
                Triangles.Add(startIndex + 0);
                Triangles.Add(startIndex + 1);
                Triangles.Add(startIndex + 4);
                Triangles.Add(startIndex + 1);
                Triangles.Add(startIndex + 5);
            }

            /// <summary>
            /// Flatten 3D index to 1D array index.
            /// </summary>
            private int Flatten3DIndex(int x, int y, int z, int size)
            {
                return x + (z * size) + (y * size * size);
            }
        }

        /// <summary>
        /// Data structure to hold async baking job and its resources.
        /// </summary>
        public class AsyncBakingHandle
        {
            public JobHandle JobHandle;
            public NativeList<float3> Vertices;
            public NativeList<int> Triangles;
            public bool IsCompleted => JobHandle.IsCompleted;

            /// <summary>
            /// Complete the job and extract the resulting mesh.
            /// Caller is responsible for disposing native collections.
            /// </summary>
            public Mesh Complete()
            {
                JobHandle.Complete();
                return BuildMesh(Vertices, Triangles);
            }

            /// <summary>
            /// Dispose of native resources.
            /// Should only be called after Complete().
            /// </summary>
            public void Dispose()
            {
                if (Vertices.IsCreated) Vertices.Dispose();
                if (Triangles.IsCreated) Triangles.Dispose();
            }
        }

        /// <summary>
        /// Bake collision mesh asynchronously using Job System.
        /// Returns handle that can be checked for completion and completed later.
        /// </summary>
        /// <param name="voxels">Source voxel data</param>
        /// <param name="chunkSize">Size of source chunk</param>
        /// <param name="resolutionDivider">Resolution divider (1=full, 2=half, 4=quarter)</param>
        /// <returns>Handle to async baking job</returns>
        public static AsyncBakingHandle BakeCollisionAsync(
            NativeArray<VoxelType> voxels,
            int chunkSize,
            int resolutionDivider = 2)
        {
            // Allocate output buffers
            var vertices = new NativeList<float3>(Allocator.TempJob);
            var triangles = new NativeList<int>(Allocator.TempJob);

            // Create and schedule job
            var job = new CollisionBakingJob
            {
                SourceVoxels = voxels,
                SourceChunkSize = chunkSize,
                ResolutionDivider = resolutionDivider,
                Vertices = vertices,
                Triangles = triangles
            };

            var handle = job.Schedule();

            return new AsyncBakingHandle
            {
                JobHandle = handle,
                Vertices = vertices,
                Triangles = triangles
            };
        }

        /// <summary>
        /// Bake collision mesh synchronously.
        /// Blocks until mesh is generated.
        /// </summary>
        /// <param name="voxels">Source voxel data</param>
        /// <param name="chunkSize">Size of source chunk</param>
        /// <param name="resolutionDivider">Resolution divider (1=full, 2=half, 4=quarter)</param>
        /// <returns>Generated collision mesh</returns>
        public static Mesh BakeCollisionSync(
            NativeArray<VoxelType> voxels,
            int chunkSize,
            int resolutionDivider = 2)
        {
            var handle = BakeCollisionAsync(voxels, chunkSize, resolutionDivider);
            var mesh = handle.Complete();
            handle.Dispose();
            return mesh;
        }

        /// <summary>
        /// Apply collision mesh to a GameObject's MeshCollider.
        /// Creates MeshCollider if it doesn't exist.
        /// </summary>
        /// <param name="collisionMesh">Collision mesh to apply</param>
        /// <param name="target">Target GameObject</param>
        /// <param name="layerName">Physics layer name</param>
        /// <returns>The MeshCollider component</returns>
        public static MeshCollider ApplyCollisionMesh(
            Mesh collisionMesh,
            GameObject target,
            string layerName = "Default")
        {
            if (collisionMesh == null)
            {
                Debug.LogWarning("[VoxelCollisionBaker] Cannot apply null collision mesh");
                return null;
            }

            if (target == null)
            {
                Debug.LogWarning("[VoxelCollisionBaker] Cannot apply collision to null GameObject");
                return null;
            }

            // Get or add MeshCollider
            var meshCollider = target.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = target.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = collisionMesh;
            meshCollider.convex = false; // Terrain uses non-convex collision

            // Set layer
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
            {
                target.layer = layer;
            }
            else
            {
                Debug.LogWarning($"[VoxelCollisionBaker] Layer '{layerName}' not found. Using default layer.");
            }

            return meshCollider;
        }

        /// <summary>
        /// Build a Unity Mesh from vertices and triangles.
        /// </summary>
        private static Mesh BuildMesh(NativeList<float3> vertices, NativeList<int> triangles)
        {
            if (vertices.Length == 0 || triangles.Length == 0)
            {
                // Return empty mesh if no geometry
                return new Mesh { name = "EmptyCollisionMesh" };
            }

            var mesh = new Mesh
            {
                name = "VoxelCollisionMesh"
            };

            // Convert NativeList to arrays
            var vertexArray = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertexArray[i] = vertices[i];
            }

            var triangleArray = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                triangleArray[i] = triangles[i];
            }

            mesh.vertices = vertexArray;
            mesh.triangles = triangleArray;

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Calculate estimated memory usage of collision mesh in bytes.
        /// </summary>
        public static int CalculateCollisionMemoryUsage(Mesh collisionMesh)
        {
            if (collisionMesh == null) return 0;

            int verticesSize = collisionMesh.vertexCount * sizeof(float) * 3;
            int trianglesSize = collisionMesh.triangles.Length * sizeof(int);

            return verticesSize + trianglesSize;
        }

        /// <summary>
        /// Legacy synchronous baking method (deprecated - use BakeCollisionSync instead).
        /// Kept for backward compatibility.
        /// </summary>
        [System.Obsolete("Use BakeCollisionSync instead")]
        public static MeshCollider BakeCollision(
            NativeArray<VoxelType> voxels,
            int chunkSize,
            GameObject target)
        {
            var mesh = BakeCollisionSync(voxels, chunkSize, resolutionDivider: 2);
            return ApplyCollisionMesh(mesh, target);
        }
    }
}
