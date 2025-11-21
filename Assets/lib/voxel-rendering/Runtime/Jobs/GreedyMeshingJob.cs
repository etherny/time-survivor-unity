using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Rendering
{
    /// <summary>
    /// Burst-compiled job for greedy meshing algorithm (ADR-003).
    /// Generates optimized mesh by merging adjacent identical voxel faces.
    /// Algorithm: For each axis, scan perpendicular slices and merge rectangular regions.
    /// </summary>
    [BurstCompile]
    public struct GreedyMeshingJob : IJob
    {
        [ReadOnly] public NativeArray<VoxelType> Voxels;
        [ReadOnly] public int ChunkSize;

        [WriteOnly] public NativeList<float3> Vertices;
        [WriteOnly] public NativeList<int> Triangles;
        [WriteOnly] public NativeList<float2> UVs;
        [WriteOnly] public NativeList<float3> Normals;

        // Temporary buffers for greedy algorithm (allocated externally for reuse)
        public NativeArray<bool> Mask;

        public void Execute()
        {
            int vertexCount = 0;

            // Greedy meshing for each axis (X, Y, Z)
            // For each axis, we scan perpendicular slices and merge faces
            for (int axis = 0; axis < 3; axis++)
            {
                // For each direction along the axis (positive and negative)
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    MeshAxis(axis, direction, ref vertexCount);
                }
            }
        }

        private void MeshAxis(int axis, int direction, ref int vertexCount)
        {
            // Get the two perpendicular axes
            int u = (axis + 1) % 3;
            int v = (axis + 2) % 3;

            int3 axisVec = new int3(0, 0, 0);
            axisVec[axis] = 1;

            int3 uVec = new int3(0, 0, 0);
            uVec[u] = 1;

            int3 vVec = new int3(0, 0, 0);
            vVec[v] = 1;

            // Scan each slice perpendicular to the axis
            for (int d = 0; d < ChunkSize; d++)
            {
                // Build mask for this slice
                BuildMask(axis, d, direction, u, v);

                // Greedy merge rectangles in the mask
                for (int j = 0; j < ChunkSize; j++)
                {
                    for (int i = 0; i < ChunkSize; i++)
                    {
                        int maskIndex = i + j * ChunkSize;

                        if (Mask[maskIndex])
                        {
                            // Compute width (u direction)
                            int width = 1;
                            while (i + width < ChunkSize && Mask[i + width + j * ChunkSize])
                            {
                                width++;
                            }

                            // Compute height (v direction)
                            int height = 1;
                            bool canExtend = true;
                            while (j + height < ChunkSize && canExtend)
                            {
                                for (int k = 0; k < width; k++)
                                {
                                    if (!Mask[i + k + (j + height) * ChunkSize])
                                    {
                                        canExtend = false;
                                        break;
                                    }
                                }
                                if (canExtend) height++;
                            }

                            // Create quad for this rectangle
                            int3 origin = new int3(0, 0, 0);
                            origin[axis] = d + (direction > 0 ? 1 : 0);
                            origin[u] = i;
                            origin[v] = j;

                            int3 du = uVec * width;
                            int3 dv = vVec * height;

                            CreateQuad(origin, du, dv, direction > 0, ref vertexCount);

                            // Clear mask for merged region
                            for (int h = 0; h < height; h++)
                            {
                                for (int w = 0; w < width; w++)
                                {
                                    Mask[i + w + (j + h) * ChunkSize] = false;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void BuildMask(int axis, int d, int direction, int u, int v)
        {
            // Clear mask
            for (int i = 0; i < Mask.Length; i++)
            {
                Mask[i] = false;
            }

            // Build mask for this slice
            for (int j = 0; j < ChunkSize; j++)
            {
                for (int i = 0; i < ChunkSize; i++)
                {
                    int3 pos = new int3(0, 0, 0);
                    pos[axis] = d;
                    pos[u] = i;
                    pos[v] = j;

                    VoxelType current = GetVoxelSafe(pos);
                    if (current == VoxelType.Air) continue;

                    // Check neighbor in the direction
                    int3 neighborPos = pos;
                    neighborPos[axis] += direction;

                    VoxelType neighbor = GetVoxelSafe(neighborPos);

                    // Only create face if neighbor is air (face is visible)
                    if (neighbor == VoxelType.Air)
                    {
                        Mask[i + j * ChunkSize] = true;
                    }
                }
            }
        }

        private VoxelType GetVoxelSafe(int3 pos)
        {
            if (pos.x < 0 || pos.x >= ChunkSize ||
                pos.y < 0 || pos.y >= ChunkSize ||
                pos.z < 0 || pos.z >= ChunkSize)
            {
                return VoxelType.Air; // Out of bounds = air
            }

            int index = VoxelMath.Flatten3DIndex(pos.x, pos.y, pos.z, ChunkSize);
            return Voxels[index];
        }

        private void CreateQuad(int3 origin, int3 du, int3 dv, bool flipWinding, ref int vertexCount)
        {
            // Create 4 vertices for the quad
            float3 v0 = (float3)origin;
            float3 v1 = (float3)(origin + du);
            float3 v2 = (float3)(origin + du + dv);
            float3 v3 = (float3)(origin + dv);

            // Calculate normal (cross product)
            float3 normal = math.normalize(math.cross((float3)du, (float3)dv));
            if (!flipWinding) normal = -normal;

            // Add vertices
            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);
            Vertices.Add(v3);

            // Add normals
            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);

            // Add UVs (simple 0-1 mapping)
            UVs.Add(new float2(0, 0));
            UVs.Add(new float2(1, 0));
            UVs.Add(new float2(1, 1));
            UVs.Add(new float2(0, 1));

            // Add triangles (two triangles per quad)
            if (flipWinding)
            {
                Triangles.Add(vertexCount + 0);
                Triangles.Add(vertexCount + 1);
                Triangles.Add(vertexCount + 2);

                Triangles.Add(vertexCount + 0);
                Triangles.Add(vertexCount + 2);
                Triangles.Add(vertexCount + 3);
            }
            else
            {
                Triangles.Add(vertexCount + 0);
                Triangles.Add(vertexCount + 2);
                Triangles.Add(vertexCount + 1);

                Triangles.Add(vertexCount + 0);
                Triangles.Add(vertexCount + 3);
                Triangles.Add(vertexCount + 2);
            }

            vertexCount += 4;
        }
    }
}
