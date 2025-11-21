using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;

namespace TimeSurvivor.Voxel.Rendering
{
    /// <summary>
    /// Utility class for building Unity Mesh objects from NativeArray data.
    /// Handles conversion from Job output to Unity's Mesh API.
    /// </summary>
    public static class MeshBuilder
    {
        /// <summary>
        /// Build a Unity Mesh from native arrays (typically Job output).
        /// Automatically chooses 16-bit or 32-bit index format based on vertex count.
        /// </summary>
        /// <param name="vertices">Vertex positions</param>
        /// <param name="triangles">Triangle indices</param>
        /// <param name="uvs">UV coordinates</param>
        /// <param name="normals">Vertex normals</param>
        /// <returns>Complete Unity Mesh ready for rendering</returns>
        public static Mesh BuildMesh(
            NativeArray<float3> vertices,
            NativeArray<int> triangles,
            NativeArray<float2> uvs,
            NativeArray<float3> normals)
        {
            var mesh = new Mesh
            {
                name = "VoxelChunkMesh",
                indexFormat = vertices.Length > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            // Convert native arrays to managed arrays
            var vertArray = ToVector3Array(vertices);
            var triArray = ToIntArray(triangles);
            var uvArray = ToVector2Array(uvs);
            var normalArray = ToVector3Array(normals);

            // Assign to mesh
            mesh.vertices = vertArray;
            mesh.triangles = triArray;
            mesh.uv = uvArray;
            mesh.normals = normalArray;

            // Recalculate bounds for culling
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Build a Unity Mesh from NativeList data (more common from Jobs).
        /// </summary>
        public static Mesh BuildMesh(
            NativeList<float3> vertices,
            NativeList<int> triangles,
            NativeList<float2> uvs,
            NativeList<float3> normals)
        {
            return BuildMesh(
                vertices.AsArray(),
                triangles.AsArray(),
                uvs.AsArray(),
                normals.AsArray()
            );
        }

        /// <summary>
        /// Build a Unity Mesh with vertex colors from NativeList data.
        /// Supports vertex color rendering for voxel types.
        /// </summary>
        public static Mesh BuildMesh(
            NativeList<float3> vertices,
            NativeList<int> triangles,
            NativeList<float2> uvs,
            NativeList<float3> normals,
            NativeList<float4> colors)
        {
            return BuildMesh(
                vertices.AsArray(),
                triangles.AsArray(),
                uvs.AsArray(),
                normals.AsArray(),
                colors.AsArray()
            );
        }

        /// <summary>
        /// Build a Unity Mesh with vertex colors from NativeArray data.
        /// </summary>
        public static Mesh BuildMesh(
            NativeArray<float3> vertices,
            NativeArray<int> triangles,
            NativeArray<float2> uvs,
            NativeArray<float3> normals,
            NativeArray<float4> colors)
        {
            var mesh = new Mesh
            {
                name = "VoxelChunkMesh",
                indexFormat = vertices.Length > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            // Convert native arrays to managed arrays
            var vertArray = ToVector3Array(vertices);
            var triArray = ToIntArray(triangles);
            var uvArray = ToVector2Array(uvs);
            var normalArray = ToVector3Array(normals);
            var colorArray = ToColorArray(colors);

            // Assign to mesh
            mesh.vertices = vertArray;
            mesh.triangles = triArray;
            mesh.uv = uvArray;
            mesh.normals = normalArray;
            mesh.colors = colorArray;

            // Recalculate bounds for culling
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Build mesh without normals (auto-calculate them).
        /// Slower but useful when normals aren't computed in Job.
        /// </summary>
        public static Mesh BuildMeshAutoNormals(
            NativeArray<float3> vertices,
            NativeArray<int> triangles,
            NativeArray<float2> uvs)
        {
            var mesh = new Mesh
            {
                name = "VoxelChunkMesh",
                indexFormat = vertices.Length > 65535
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            // Convert native arrays to managed arrays
            var vertArray = ToVector3Array(vertices);
            var triArray = ToIntArray(triangles);
            var uvArray = ToVector2Array(uvs);

            mesh.vertices = vertArray;
            mesh.triangles = triArray;
            mesh.uv = uvArray;

            mesh.RecalculateNormals(); // Auto-calculate
            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Update an existing mesh with new data (faster than creating new).
        /// Useful for dynamic voxel modifications.
        /// </summary>
        public static void UpdateMesh(
            Mesh mesh,
            NativeArray<float3> vertices,
            NativeArray<int> triangles,
            NativeArray<float2> uvs,
            NativeArray<float3> normals)
        {
            mesh.Clear();

            mesh.indexFormat = vertices.Length > 65535
                ? UnityEngine.Rendering.IndexFormat.UInt32
                : UnityEngine.Rendering.IndexFormat.UInt16;

            // Convert native arrays to managed arrays
            var vertArray = ToVector3Array(vertices);
            var triArray = ToIntArray(triangles);
            var uvArray = ToVector2Array(uvs);
            var normalArray = ToVector3Array(normals);

            mesh.vertices = vertArray;
            mesh.triangles = triArray;
            mesh.uv = uvArray;
            mesh.normals = normalArray;

            mesh.RecalculateBounds();
        }

        /// <summary>
        /// Calculate estimated memory usage of a mesh in bytes.
        /// Useful for tracking memory budget.
        /// </summary>
        public static int CalculateMeshMemoryUsage(int vertexCount, int triangleCount)
        {
            int verticesSize = vertexCount * sizeof(float) * 3; // Vector3
            int trianglesSize = triangleCount * sizeof(int);
            int uvsSize = vertexCount * sizeof(float) * 2; // Vector2
            int normalsSize = vertexCount * sizeof(float) * 3; // Vector3

            return verticesSize + trianglesSize + uvsSize + normalsSize;
        }

        #region Private Conversion Helpers

        /// <summary>
        /// Convert NativeArray of float3 to Vector3 array.
        /// </summary>
        private static Vector3[] ToVector3Array(NativeArray<float3> source)
        {
            var result = new Vector3[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = source[i];
            return result;
        }

        /// <summary>
        /// Convert NativeArray of float2 to Vector2 array.
        /// </summary>
        private static Vector2[] ToVector2Array(NativeArray<float2> source)
        {
            var result = new Vector2[source.Length];
            for (int i = 0; i < source.Length; i++)
                result[i] = source[i];
            return result;
        }

        /// <summary>
        /// Convert NativeArray of int to int array.
        /// </summary>
        private static int[] ToIntArray(NativeArray<int> source)
        {
            var result = new int[source.Length];
            source.CopyTo(result);
            return result;
        }

        /// <summary>
        /// Convert NativeArray of float4 to Color array.
        /// </summary>
        private static Color[] ToColorArray(NativeArray<float4> source)
        {
            var result = new Color[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                var c = source[i];
                result[i] = new Color(c.x, c.y, c.z, c.w);
            }
            return result;
        }

        #endregion
    }
}
