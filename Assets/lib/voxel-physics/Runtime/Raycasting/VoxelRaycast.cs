using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;

namespace TimeSurvivor.Voxel.Physics
{
    /// <summary>
    /// Voxel raycasting using DDA (Digital Differential Analyzer) algorithm.
    /// Efficiently traverses voxel grid along a ray.
    /// </summary>
    public static class VoxelRaycast
    {
        /// <summary>
        /// Raycast result containing hit information.
        /// </summary>
        public struct RaycastHit
        {
            public bool Hit;
            public float3 HitPoint;
            public int3 VoxelCoord;
            public int3 Normal;
            public float Distance;
            public VoxelType VoxelType;
        }

        /// <summary>
        /// Perform a raycast through voxel space.
        /// Uses DDA algorithm for efficient voxel traversal.
        /// </summary>
        /// <param name="origin">Ray origin in world space</param>
        /// <param name="direction">Ray direction (normalized)</param>
        /// <param name="maxDistance">Maximum ray distance</param>
        /// <param name="voxelSize">Size of voxels in Unity units</param>
        /// <param name="getVoxel">Function to retrieve voxel at coordinate</param>
        /// <returns>Raycast result</returns>
        public static RaycastHit Raycast(
            float3 origin,
            float3 direction,
            float maxDistance,
            float voxelSize,
            System.Func<int3, VoxelType> getVoxel)
        {
            direction = math.normalize(direction);

            // Convert to voxel grid coordinates
            int3 voxelCoord = (int3)math.floor(origin / voxelSize);

            // DDA setup
            int3 step = new int3(
                direction.x >= 0 ? 1 : -1,
                direction.y >= 0 ? 1 : -1,
                direction.z >= 0 ? 1 : -1
            );

            // Distance to next voxel boundary along each axis
            float3 tDelta = math.abs(voxelSize / direction);

            // Distance to first boundary
            float3 fractional = (origin / voxelSize) - (float3)voxelCoord;
            float3 tMax = new float3(
                step.x > 0 ? (1 - fractional.x) * tDelta.x : fractional.x * tDelta.x,
                step.y > 0 ? (1 - fractional.y) * tDelta.y : fractional.y * tDelta.y,
                step.z > 0 ? (1 - fractional.z) * tDelta.z : fractional.z * tDelta.z
            );

            int3 normal = new int3(0, 0, 0);
            float distance = 0f;
            int maxSteps = (int)(maxDistance / voxelSize) + 1;

            // DDA traversal
            for (int i = 0; i < maxSteps; i++)
            {
                // Check current voxel
                VoxelType voxelType = getVoxel(voxelCoord);

                if (voxelType != VoxelType.Air)
                {
                    // Hit solid voxel
                    float3 hitPoint = origin + direction * distance;

                    return new RaycastHit
                    {
                        Hit = true,
                        HitPoint = hitPoint,
                        VoxelCoord = voxelCoord,
                        Normal = normal,
                        Distance = distance,
                        VoxelType = voxelType
                    };
                }

                // Step to next voxel
                if (tMax.x < tMax.y)
                {
                    if (tMax.x < tMax.z)
                    {
                        // X is smallest
                        distance = tMax.x;
                        tMax.x += tDelta.x;
                        voxelCoord.x += step.x;
                        normal = new int3(-step.x, 0, 0);
                    }
                    else
                    {
                        // Z is smallest
                        distance = tMax.z;
                        tMax.z += tDelta.z;
                        voxelCoord.z += step.z;
                        normal = new int3(0, 0, -step.z);
                    }
                }
                else
                {
                    if (tMax.y < tMax.z)
                    {
                        // Y is smallest
                        distance = tMax.y;
                        tMax.y += tDelta.y;
                        voxelCoord.y += step.y;
                        normal = new int3(0, -step.y, 0);
                    }
                    else
                    {
                        // Z is smallest
                        distance = tMax.z;
                        tMax.z += tDelta.z;
                        voxelCoord.z += step.z;
                        normal = new int3(0, 0, -step.z);
                    }
                }

                if (distance > maxDistance)
                    break;
            }

            // No hit
            return new RaycastHit { Hit = false };
        }

        /// <summary>
        /// Simplified raycast that returns only hit/miss and voxel coordinate.
        /// </summary>
        public static bool RaycastSimple(
            float3 origin,
            float3 direction,
            float maxDistance,
            float voxelSize,
            System.Func<int3, VoxelType> getVoxel,
            out int3 hitVoxel)
        {
            var result = Raycast(origin, direction, maxDistance, voxelSize, getVoxel);
            hitVoxel = result.VoxelCoord;
            return result.Hit;
        }

        /// <summary>
        /// Get all voxels along a ray path (even if they're air).
        /// Useful for effects like lasers or line-of-sight checks.
        /// </summary>
        public static System.Collections.Generic.List<int3> GetVoxelsAlongRay(
            float3 origin,
            float3 direction,
            float maxDistance,
            float voxelSize)
        {
            var voxels = new System.Collections.Generic.List<int3>();

            direction = math.normalize(direction);
            int3 voxelCoord = (int3)math.floor(origin / voxelSize);

            int3 step = new int3(
                direction.x >= 0 ? 1 : -1,
                direction.y >= 0 ? 1 : -1,
                direction.z >= 0 ? 1 : -1
            );

            float3 tDelta = math.abs(voxelSize / direction);
            float3 fractional = (origin / voxelSize) - (float3)voxelCoord;
            float3 tMax = new float3(
                step.x > 0 ? (1 - fractional.x) * tDelta.x : fractional.x * tDelta.x,
                step.y > 0 ? (1 - fractional.y) * tDelta.y : fractional.y * tDelta.y,
                step.z > 0 ? (1 - fractional.z) * tDelta.z : fractional.z * tDelta.z
            );

            float distance = 0f;
            int maxSteps = (int)(maxDistance / voxelSize) + 1;

            for (int i = 0; i < maxSteps; i++)
            {
                voxels.Add(voxelCoord);

                // Step to next voxel
                if (tMax.x < tMax.y)
                {
                    if (tMax.x < tMax.z)
                    {
                        distance = tMax.x;
                        tMax.x += tDelta.x;
                        voxelCoord.x += step.x;
                    }
                    else
                    {
                        distance = tMax.z;
                        tMax.z += tDelta.z;
                        voxelCoord.z += step.z;
                    }
                }
                else
                {
                    if (tMax.y < tMax.z)
                    {
                        distance = tMax.y;
                        tMax.y += tDelta.y;
                        voxelCoord.y += step.y;
                    }
                    else
                    {
                        distance = tMax.z;
                        tMax.z += tDelta.z;
                        voxelCoord.z += step.z;
                    }
                }

                if (distance > maxDistance)
                    break;
            }

            return voxels;
        }
    }
}
