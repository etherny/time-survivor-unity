using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Physics
{
    /// <summary>
    /// Bakes collision meshes from voxel data.
    /// Generates simplified collision geometry for physics interactions.
    /// </summary>
    public static class VoxelCollisionBaker
    {
        /// <summary>
        /// Bake a collision mesh from voxel data and apply to GameObject.
        /// Uses simplified geometry (less detailed than render mesh).
        /// </summary>
        /// <param name="voxels">Voxel data array</param>
        /// <param name="chunkSize">Size of chunk in voxels</param>
        /// <param name="target">Target GameObject to add MeshCollider</param>
        /// <returns>Created MeshCollider component</returns>
        public static MeshCollider BakeCollision(
            NativeArray<VoxelType> voxels,
            int chunkSize,
            GameObject target)
        {
            // Generate simplified collision mesh (box per solid voxel cluster)
            Mesh collisionMesh = GenerateSimplifiedCollisionMesh(voxels, chunkSize);

            // Get or add MeshCollider
            var meshCollider = target.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = target.AddComponent<MeshCollider>();
            }

            meshCollider.sharedMesh = collisionMesh;
            meshCollider.convex = false; // Use non-convex for terrain

            return meshCollider;
        }

        /// <summary>
        /// Generate simplified collision mesh.
        /// Uses box primitives for clusters of voxels (more efficient than per-voxel).
        /// </summary>
        private static Mesh GenerateSimplifiedCollisionMesh(NativeArray<VoxelType> voxels, int chunkSize)
        {
            var vertices = new System.Collections.Generic.List<Vector3>();
            var triangles = new System.Collections.Generic.List<int>();

            // Simple approach: create a box for each solid voxel
            // TODO: Optimize by merging adjacent voxels into larger boxes
            for (int y = 0; y < chunkSize; y++)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    for (int x = 0; x < chunkSize; x++)
                    {
                        int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);
                        VoxelType voxelType = voxels[index];

                        if (voxelType != VoxelType.Air)
                        {
                            // Check if voxel has exposed faces (optimization)
                            if (HasExposedFace(voxels, x, y, z, chunkSize))
                            {
                                AddVoxelBox(vertices, triangles, new float3(x, y, z));
                            }
                        }
                    }
                }
            }

            var mesh = new Mesh
            {
                name = "VoxelCollisionMesh",
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Check if a voxel has any exposed faces (border with air).
        /// </summary>
        private static bool HasExposedFace(NativeArray<VoxelType> voxels, int x, int y, int z, int chunkSize)
        {
            // Check all 6 neighbors
            if (x > 0 && GetVoxelSafe(voxels, x - 1, y, z, chunkSize) == VoxelType.Air) return true;
            if (x < chunkSize - 1 && GetVoxelSafe(voxels, x + 1, y, z, chunkSize) == VoxelType.Air) return true;
            if (y > 0 && GetVoxelSafe(voxels, x, y - 1, z, chunkSize) == VoxelType.Air) return true;
            if (y < chunkSize - 1 && GetVoxelSafe(voxels, x, y + 1, z, chunkSize) == VoxelType.Air) return true;
            if (z > 0 && GetVoxelSafe(voxels, x, y, z - 1, chunkSize) == VoxelType.Air) return true;
            if (z < chunkSize - 1 && GetVoxelSafe(voxels, x, y, z + 1, chunkSize) == VoxelType.Air) return true;

            return false;
        }

        /// <summary>
        /// Get voxel safely (returns Air if out of bounds).
        /// </summary>
        private static VoxelType GetVoxelSafe(NativeArray<VoxelType> voxels, int x, int y, int z, int chunkSize)
        {
            if (x < 0 || x >= chunkSize || y < 0 || y >= chunkSize || z < 0 || z >= chunkSize)
                return VoxelType.Air;

            int index = VoxelMath.Flatten3DIndex(x, y, z, chunkSize);
            return voxels[index];
        }

        /// <summary>
        /// Add a cube's vertices and triangles to the mesh lists.
        /// </summary>
        private static void AddVoxelBox(
            System.Collections.Generic.List<Vector3> vertices,
            System.Collections.Generic.List<int> triangles,
            float3 position)
        {
            int startIndex = vertices.Count;

            // Define 8 vertices of a unit cube at position
            Vector3 p0 = position + new float3(0, 0, 0);
            Vector3 p1 = position + new float3(1, 0, 0);
            Vector3 p2 = position + new float3(1, 1, 0);
            Vector3 p3 = position + new float3(0, 1, 0);
            Vector3 p4 = position + new float3(0, 0, 1);
            Vector3 p5 = position + new float3(1, 0, 1);
            Vector3 p6 = position + new float3(1, 1, 1);
            Vector3 p7 = position + new float3(0, 1, 1);

            // Add vertices
            vertices.AddRange(new[] { p0, p1, p2, p3, p4, p5, p6, p7 });

            // Add triangles (12 triangles for 6 faces)
            // Front face
            triangles.AddRange(new[] { startIndex + 0, startIndex + 2, startIndex + 1 });
            triangles.AddRange(new[] { startIndex + 0, startIndex + 3, startIndex + 2 });

            // Back face
            triangles.AddRange(new[] { startIndex + 5, startIndex + 6, startIndex + 4 });
            triangles.AddRange(new[] { startIndex + 4, startIndex + 6, startIndex + 7 });

            // Left face
            triangles.AddRange(new[] { startIndex + 4, startIndex + 7, startIndex + 0 });
            triangles.AddRange(new[] { startIndex + 0, startIndex + 7, startIndex + 3 });

            // Right face
            triangles.AddRange(new[] { startIndex + 1, startIndex + 6, startIndex + 5 });
            triangles.AddRange(new[] { startIndex + 1, startIndex + 2, startIndex + 6 });

            // Top face
            triangles.AddRange(new[] { startIndex + 3, startIndex + 7, startIndex + 6 });
            triangles.AddRange(new[] { startIndex + 3, startIndex + 6, startIndex + 2 });

            // Bottom face
            triangles.AddRange(new[] { startIndex + 4, startIndex + 0, startIndex + 1 });
            triangles.AddRange(new[] { startIndex + 4, startIndex + 1, startIndex + 5 });
        }

        /// <summary>
        /// Calculate estimated memory usage of collision mesh.
        /// </summary>
        public static int CalculateCollisionMemoryUsage(Mesh collisionMesh)
        {
            if (collisionMesh == null) return 0;

            int verticesSize = collisionMesh.vertexCount * sizeof(float) * 3;
            int trianglesSize = collisionMesh.triangles.Length * sizeof(int);

            return verticesSize + trianglesSize;
        }
    }
}
