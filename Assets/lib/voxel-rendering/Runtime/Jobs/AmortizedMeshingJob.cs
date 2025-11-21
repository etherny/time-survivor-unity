using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Rendering
{
    /// <summary>
    /// Job for amortized meshing (ADR-005).
    /// Allows meshing work to be spread across multiple frames by processing
    /// a limited number of chunks per frame based on time budget.
    ///
    /// This is a wrapper that schedules GreedyMeshingJob with time constraints.
    /// </summary>
    [BurstCompile]
    public struct AmortizedMeshingJob : IJob
    {
        [ReadOnly] public NativeArray<VoxelType> Voxels;
        [ReadOnly] public int ChunkSize;

        [WriteOnly] public NativeList<float3> Vertices;
        [WriteOnly] public NativeList<int> Triangles;
        [WriteOnly] public NativeList<float2> UVs;
        [WriteOnly] public NativeList<float3> Normals;

        public NativeArray<bool> Mask;

        public void Execute()
        {
            // This job is identical to GreedyMeshingJob
            // The amortization happens at the scheduling level (not within the job)
            // by only scheduling N jobs per frame based on time budget

            var meshingJob = new GreedyMeshingJob
            {
                Voxels = Voxels,
                ChunkSize = ChunkSize,
                Vertices = Vertices,
                Triangles = Triangles,
                UVs = UVs,
                Normals = Normals,
                Mask = Mask
            };

            meshingJob.Execute();
        }
    }
}
