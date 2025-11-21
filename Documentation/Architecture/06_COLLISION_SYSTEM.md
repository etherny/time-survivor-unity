# Collision System - Physics & Raycasting
## Optimized Collision pour Voxels Statiques + Dynamiques

---

## 1. DUAL COLLISION STRATEGY

### Macro Terrain (Static)
- **MeshCollider baked** (one-time generation)
- **Non-convex** (terrain complexe)
- **Static** (jamais modifié après bake)

### Micro Objects (Dynamic)
- **Box Colliders** (AABB approximation)
- **Compound colliders** (pour voxels complexes)
- **Dynamic** (destruction updates colliders)

---

## 2. TERRAIN COLLISION (Macro)

```csharp
public class TerrainCollisionBaker {

    public void BakeTerrainCollision(MacroChunk chunk, Mesh terrainMesh) {
        var go = chunk.gameObject;

        // Add MeshCollider
        var meshCollider = go.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = terrainMesh;
        meshCollider.convex = false; // Non-convex pour terrain
        meshCollider.cookingOptions = MeshColliderCookingOptions.CookForFasterSimulation;

        // Static layer
        go.layer = LayerMask.NameToLayer("TerrainStatic");
        go.isStatic = true;
    }
}
```

---

## 3. DYNAMIC OBJECT COLLISION (Micro)

```csharp
public class VoxelObjectCollider : MonoBehaviour {

    public MicroVoxelObject voxelObject;
    BoxCollider[] subColliders;

    public void GenerateColliders() {
        // Option 1: Single AABB (fast, approximate)
        var bounds = CalculateVoxelBounds(voxelObject.chunk.voxels);
        var collider = gameObject.AddComponent<BoxCollider>();
        collider.center = bounds.center;
        collider.size = bounds.size;

        // Option 2: Compound colliders (précis, plus coûteux)
        // GenerateCompoundColliders();
    }

    Bounds CalculateVoxelBounds(NativeArray<VoxelType> voxels) {
        // Find min/max non-air voxels
        int3 min = new int3(int.MaxValue);
        int3 max = new int3(int.MinValue);

        for (int i = 0; i < voxels.Length; i++) {
            if (voxels[i] != VoxelType.Air) {
                int3 pos = IndexToCoord(i);
                min = math.min(min, pos);
                max = math.max(max, pos);
            }
        }

        float3 center = (min + max) * 0.5f * 0.1f; // 0.1 = micro voxel size
        float3 size = (max - min + 1) * 0.1f;

        return new Bounds(center, size);
    }
}
```

---

## 4. SPATIAL PARTITIONING (Queries Optimization)

```csharp
// Physics.OverlapSphere optimized avec spatial hash
public class VoxelPhysicsQueries {

    MicroObjectSpatialHash spatialHash;

    public List<Collider> OverlapSphere(float3 center, float radius, int layerMask) {
        // Query spatial hash (O(n cells) vs O(n total objects))
        var candidates = spatialHash.QuerySphere(center, radius);

        var results = new List<Collider>();
        foreach (var obj in candidates) {
            // Precise distance check
            if (math.distance(obj.position, center) <= radius) {
                results.Add(obj.collider);
            }
        }

        return results;
    }

    // Raycast against voxels (precise)
    public bool VoxelRaycast(Ray ray, out RaycastHit hit, float maxDistance) {
        // DDA algorithm (voxel traversal)
        return DDAVoxelRaycast(ray, out hit, maxDistance);
    }
}
```

---

## 5. VOXEL RAYCASTING (DDA Algorithm)

```csharp
[BurstCompile]
public struct VoxelRaycastJob : IJob {

    public Ray ray;
    public float maxDistance;
    public NativeArray<VoxelType> voxels;
    public int chunkSize;

    public NativeReference<bool> hitResult;
    public NativeReference<RaycastHit> hit;

    public void Execute() {
        // DDA Voxel Traversal
        float3 pos = ray.origin;
        float3 dir = math.normalize(ray.direction);

        float3 step = math.sign(dir);
        float3 tDelta = math.abs(1f / dir);
        float3 tMax = tDelta * (0.5f - math.frac(pos));

        float distance = 0f;

        while (distance < maxDistance) {
            // Check current voxel
            int3 voxelCoord = (int3)math.floor(pos);
            VoxelType voxel = GetVoxel(voxelCoord);

            if (voxel != VoxelType.Air) {
                // Hit!
                hitResult.Value = true;
                hit.Value = new RaycastHit {
                    point = pos,
                    distance = distance
                };
                return;
            }

            // Step to next voxel
            if (tMax.x < tMax.y) {
                if (tMax.x < tMax.z) {
                    pos.x += step.x;
                    distance = tMax.x;
                    tMax.x += tDelta.x;
                } else {
                    pos.z += step.z;
                    distance = tMax.z;
                    tMax.z += tDelta.z;
                }
            } else {
                if (tMax.y < tMax.z) {
                    pos.y += step.y;
                    distance = tMax.y;
                    tMax.y += tDelta.y;
                } else {
                    pos.z += step.z;
                    distance = tMax.z;
                    tMax.z += tDelta.z;
                }
            }
        }

        hitResult.Value = false;
    }

    VoxelType GetVoxel(int3 coord) {
        if (coord.x < 0 || coord.x >= chunkSize ||
            coord.y < 0 || coord.y >= chunkSize ||
            coord.z < 0 || coord.z >= chunkSize) {
            return VoxelType.Air;
        }
        int index = coord.x + coord.y * chunkSize + coord.z * chunkSize * chunkSize;
        return voxels[index];
    }
}
```

---

## 6. COLLISION LAYERS STRATEGY

```csharp
public enum CollisionLayer {
    TerrainStatic = 8,   // Never moves, MeshCollider
    Destructible = 9,    // Buildings, props (Box/Compound)
    Enemy = 10,          // 2000+ enemies (AABB)
    Player = 11,         // Player character
    Projectile = 12      // Bullets, magic
}

// Physics settings (Layer Collision Matrix)
// TerrainStatic collides with: Player, Enemy, Projectile
// Destructible collides with: All
// Enemy collides with: Player, Projectile, TerrainStatic
// Projectile collides with: Enemy, Destructible, TerrainStatic
```

---

## 7. BATCH COLLISION QUERIES (ECS)

```csharp
[BurstCompile]
public partial struct ProjectileCollisionJob : IJobEntity {

    [ReadOnly] public CollisionWorld collisionWorld;
    public EntityCommandBuffer.ParallelWriter ecb;

    void Execute(
        Entity entity,
        [EntityIndexInQuery] int entityIndex,
        in LocalTransform transform,
        in VelocityComponent velocity
    ) {
        // Raycast from last pos to current pos
        float3 start = transform.Position - velocity.value * SystemAPI.Time.DeltaTime;
        float3 end = transform.Position;

        var rayInput = new RaycastInput {
            Start = start,
            End = end,
            Filter = CollisionFilter.Default
        };

        if (collisionWorld.CastRay(rayInput, out var hit)) {
            // Hit something!
            ecb.DestroyEntity(entityIndex, entity);

            // Apply damage (if enemy)
            if (hit.Entity != Entity.Null) {
                ecb.AddComponent(entityIndex, hit.Entity, new DamageEvent { damage = 10 });
            }
        }
    }
}
```

---

## 8. PERFORMANCE TARGETS

```
COLLISION BUDGET: 2.0 ms per frame

├─ Static terrain queries: 0.2 ms (rare)
├─ Enemy collision detection: 1.0 ms (2000 entities)
├─ Projectile raycasts: 0.5 ms (batch)
├─ Spatial hash queries: 0.3 ms
└─ TOTAL: 2.0 ms ✓
```

---

**Document Version:** 1.0
**Next:** 07_DESTRUCTION_PIPELINE.md
