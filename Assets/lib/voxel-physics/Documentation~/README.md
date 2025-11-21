# Voxel Physics Package

**Version**: 1.0.0
**Namespace**: `TimeSurvivor.Voxel.Physics`
**Assembly Definition**: `TimeSurvivor.Voxel.Physics`

---

## Overview

Collision detection and raycasting for voxel-based worlds. Provides efficient spatial queries, collision mesh generation, and DDA-based voxel raycasting.

---

## Responsibilities

### Collision
- `VoxelCollisionBaker` - Generates collision meshes from voxel data
- `SpatialHash<T>` - Spatial hash grid for fast spatial queries

### Raycasting
- `VoxelRaycast` - DDA-based voxel raycasting algorithm

---

## Dependencies

### Required Packages
- `TimeSurvivor.Voxel.Core` - Core voxel types and utilities
- `TimeSurvivor.Voxel.Rendering` - Mesh building utilities
- `Unity.Mathematics` - Math operations
- `Unity.Collections` - NativeArray support
- `Unity.Burst` - Burst compiler (future optimization)
- `Unity.Physics` - Unity Physics package

---

## Usage Examples

### Baking Collision Meshes

```csharp
using UnityEngine;
using Unity.Collections;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Physics;

public class ChunkCollisionSetup : MonoBehaviour
{
    [SerializeField] private VoxelConfiguration _config;

    public void SetupCollision(NativeArray<VoxelType> voxelData, GameObject chunkObject)
    {
        // Bake collision mesh from voxel data
        MeshCollider collider = VoxelCollisionBaker.BakeCollision(
            voxelData,
            _config.ChunkSize,
            chunkObject
        );

        Debug.Log($"Collision mesh created with {collider.sharedMesh.vertexCount} vertices");
    }
}
```

### Using Spatial Hash for Fast Queries

```csharp
using UnityEngine;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Physics;

public class EnemyManager : MonoBehaviour
{
    private SpatialHash<Enemy> _enemyGrid;

    void Start()
    {
        // Create spatial hash with 10 Unity unit cells
        _enemyGrid = new SpatialHash<Enemy>(10f);
    }

    public void AddEnemy(Enemy enemy)
    {
        float3 position = enemy.transform.position;
        _enemyGrid.Add(position, enemy);
    }

    public void RemoveEnemy(Enemy enemy)
    {
        float3 position = enemy.transform.position;
        _enemyGrid.Remove(position, enemy);
    }

    public List<Enemy> GetNearbyEnemies(float3 position, float radius)
    {
        return _enemyGrid.GetItemsInSphere(position, radius);
    }

    void Update()
    {
        // Example: Find enemies near player
        float3 playerPos = Player.Instance.transform.position;
        var nearbyEnemies = GetNearbyEnemies(playerPos, 20f);

        Debug.Log($"Found {nearbyEnemies.Count} enemies within 20 units");
    }
}
```

### Voxel Raycasting

```csharp
using UnityEngine;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Physics;
using TimeSurvivor.Voxel.Terrain;

public class VoxelInteraction : MonoBehaviour
{
    [SerializeField] private VoxelConfiguration _config;
    [SerializeField] private ChunkManager _chunkManager;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastVoxel();
        }
    }

    void RaycastVoxel()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        float3 origin = ray.origin;
        float3 direction = ray.direction;
        float maxDistance = 100f;
        float voxelSize = _config.MacroVoxelSize;

        // Perform voxel raycast
        var hit = VoxelRaycast.Raycast(
            origin,
            direction,
            maxDistance,
            voxelSize,
            GetVoxelAtCoord // Callback function
        );

        if (hit.Hit)
        {
            Debug.Log($"Hit voxel: {hit.VoxelType} at {hit.VoxelCoord}");
            Debug.Log($"Distance: {hit.Distance}, Normal: {hit.Normal}");

            // Example: Destroy voxel on hit
            SetVoxelAtCoord(hit.VoxelCoord, VoxelType.Air);
        }
    }

    VoxelType GetVoxelAtCoord(int3 voxelCoord)
    {
        // Convert voxel coord to chunk coord and local coord
        ChunkCoord chunkCoord = VoxelMath.WorldToChunkCoord(
            (float3)voxelCoord * _config.MacroVoxelSize,
            _config.ChunkSize,
            _config.MacroVoxelSize
        );

        var chunk = _chunkManager.GetTerrainChunk(chunkCoord);
        if (chunk == null) return VoxelType.Air;

        int3 localCoord = VoxelMath.VoxelToLocalCoord(voxelCoord, _config.ChunkSize);
        return chunk.GetVoxel(localCoord, _config.ChunkSize);
    }

    void SetVoxelAtCoord(int3 voxelCoord, VoxelType type)
    {
        // Similar to GetVoxelAtCoord but sets the voxel
        ChunkCoord chunkCoord = VoxelMath.WorldToChunkCoord(
            (float3)voxelCoord * _config.MacroVoxelSize,
            _config.ChunkSize,
            _config.MacroVoxelSize
        );

        var chunk = _chunkManager.GetTerrainChunk(chunkCoord);
        if (chunk == null) return;

        int3 localCoord = VoxelMath.VoxelToLocalCoord(voxelCoord, _config.ChunkSize);
        chunk.SetVoxel(localCoord, type, _config.ChunkSize);

        // Mark chunk for remeshing
        _chunkManager.MarkDirty(chunkCoord);
    }
}
```

### Line of Sight Check

```csharp
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Physics;

public class LineOfSightChecker
{
    private VoxelConfiguration _config;
    private System.Func<int3, VoxelType> _getVoxel;

    public bool HasLineOfSight(float3 from, float3 to)
    {
        float3 direction = math.normalize(to - from);
        float distance = math.distance(from, to);

        var hit = VoxelRaycast.Raycast(
            from,
            direction,
            distance,
            _config.MacroVoxelSize,
            _getVoxel
        );

        // If ray hits something before reaching target, no line of sight
        return !hit.Hit || hit.Distance >= distance;
    }
}
```

### Getting All Voxels Along Ray

```csharp
using UnityEngine;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Physics;

public class LaserBeam : MonoBehaviour
{
    [SerializeField] private float _voxelSize = 0.2f;
    [SerializeField] private float _maxDistance = 50f;

    void FireLaser()
    {
        float3 origin = transform.position;
        float3 direction = transform.forward;

        // Get all voxels along the laser path
        var voxels = VoxelRaycast.GetVoxelsAlongRay(origin, direction, _maxDistance, _voxelSize);

        Debug.Log($"Laser traversed {voxels.Count} voxels");

        // Example: Visualize the path
        foreach (var voxelCoord in voxels)
        {
            float3 worldPos = (float3)voxelCoord * _voxelSize;
            Debug.DrawLine(worldPos, worldPos + new float3(0, 0.1f, 0), Color.red, 1f);
        }
    }
}
```

---

## Performance Considerations

### Collision Mesh Generation
- **Simplified Geometry**: Uses box primitives per solid voxel (not per-face quads)
- **Exposed Face Culling**: Only generates collision for voxels with exposed faces
- **Future Optimization**: Merge adjacent voxels into larger boxes

Current collision mesh complexity:
- **Worst case**: 8 vertices + 12 triangles per exposed voxel
- **16³ chunk**: ~500-2000 exposed voxels typical (depends on terrain)
- **Collision mesh**: ~4000-16000 vertices typical

### Spatial Hash
- **O(1) insertion and removal** (average case)
- **Fast neighbor queries**: Only checks relevant cells
- **Memory**: Proportional to number of items stored
- **Cell size**: Should match typical query radius for best performance

### Voxel Raycasting
- **DDA Algorithm**: O(distance / voxelSize) complexity
- **No triangle intersection tests**: Direct voxel grid traversal
- **Very fast**: 100+ rays per frame achievable
- **Use cases**: Player interaction, AI line of sight, projectiles

---

## API Reference

### VoxelCollisionBaker
```csharp
public static class VoxelCollisionBaker
{
    // Bake collision mesh and apply to GameObject
    public static MeshCollider BakeCollision(
        NativeArray<VoxelType> voxels,
        int chunkSize,
        GameObject target);

    // Calculate collision mesh memory usage
    public static int CalculateCollisionMemoryUsage(Mesh collisionMesh);
}
```

### SpatialHash<T>
```csharp
public class SpatialHash<T> where T : class
{
    public SpatialHash(float cellSize);

    // Get grid cell for world position
    public int3 GetCellCoord(float3 worldPosition);

    // Add/remove items
    public void Add(float3 worldPosition, T item);
    public bool Remove(float3 worldPosition, T item);

    // Spatial queries
    public List<T> GetItemsAt(float3 worldPosition);
    public List<T> GetItemsInSphere(float3 center, float radius);
    public List<T> GetItemsInBox(float3 min, float3 max);

    // Utility
    public void Clear();
    public int CellCount { get; }
    public int ItemCount { get; }
}
```

### VoxelRaycast
```csharp
public static class VoxelRaycast
{
    // Main raycast method
    public static RaycastHit Raycast(
        float3 origin,
        float3 direction,
        float maxDistance,
        float voxelSize,
        System.Func<int3, VoxelType> getVoxel);

    // Simplified raycast (returns only hit/voxel coord)
    public static bool RaycastSimple(
        float3 origin,
        float3 direction,
        float maxDistance,
        float voxelSize,
        System.Func<int3, VoxelType> getVoxel,
        out int3 hitVoxel);

    // Get all voxels along ray (including air)
    public static List<int3> GetVoxelsAlongRay(
        float3 origin,
        float3 direction,
        float maxDistance,
        float voxelSize);
}

// Raycast result
public struct RaycastHit
{
    public bool Hit;
    public float3 HitPoint;
    public int3 VoxelCoord;
    public int3 Normal;
    public float Distance;
    public VoxelType VoxelType;
}
```

---

## Known Limitations

1. **Collision Mesh Optimization**: Currently generates one box per exposed voxel
   - **Future**: Merge adjacent voxels into larger boxes for fewer triangles
2. **No Burst for Raycasting**: Raycasting uses managed code callback
   - **Future**: Create Burst-compatible raycasting with NativeArray voxel data
3. **Spatial Hash Not Thread-Safe**: Uses managed Dictionary
   - **Future**: Implement NativeMultiHashMap version for Jobs
4. **Collision Mesh Complexity**: Can be high for complex terrain
   - **Solution**: Use simpler collision geometry or Unity Physics for player/entities

---

## Testing

Test assembly: `TimeSurvivor.Voxel.Physics.Tests`

Recommended tests:
- Spatial hash correctness (insertion, removal, queries)
- Raycast accuracy (compare to known results)
- DDA algorithm edge cases (axis-aligned rays, diagonal rays)
- Collision mesh generation (vertex count, topology)
- Performance benchmarks (raycasting speed, spatial query speed)

---

## Integration with Other Packages

### voxel-core
- Uses `VoxelType`, `ChunkCoord`, `VoxelMath`

### voxel-rendering
- Collision meshes use similar generation approach to render meshes

### voxel-terrain
- Raycasting requires chunk manager for voxel queries
- Collision baking integrated into chunk generation

---

## Future Enhancements

1. **Burst-Compiled Raycasting**: NativeArray-based voxel raycasting in Jobs
2. **Physics Material Per Voxel**: Different friction/bounciness per VoxelType
3. **Swept Collision Detection**: Moving object vs voxel world collision
4. **Optimized Collision Meshes**: Greedy merging of adjacent collision boxes
5. **Unity Physics Integration**: Convert to Unity.Physics colliders for DOTS

---

## License

Copyright (c) 2025 TimeSurvivor. All rights reserved.
