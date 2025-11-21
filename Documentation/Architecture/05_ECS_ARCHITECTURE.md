# DOTS/ECS Architecture - 2000+ Enemies
## Entity Component System pour Performance Maximale

---

## 1. ECS COMPONENTS (Data-Oriented Design)

```csharp
// Enemy voxel entity components
public struct EnemyVoxelComponent : IComponentData {
    public Entity voxelMeshEntity; // Reference to mesh renderer
    public int health;
    public int maxHealth;
    public float moveSpeed;
    public EnemyType type;
}

public enum EnemyType : byte {
    BasicZombie,
    FastRunner,
    Tank,
    Boss
}

public struct VoxelMeshDataComponent : IComponentData {
    public VoxelModelAsset modelAsset;
    public int currentLOD; // 0-3
    public bool isDirty; // Needs remeshing
}

public struct VoxelDestructionComponent : IComponentData {
    public NativeArray<VoxelType> voxelData; // 32³ voxels
    public int destroyedVoxelCount;
    public bool isFullyDestroyed;
}

// Movement/AI
public struct TargetComponent : IComponentData {
    public Entity targetEntity; // Usually player
    public float3 targetPosition;
}

public struct VelocityComponent : IComponentData {
    public float3 value;
}

// Spatial partitioning tag
public struct SpatialHashCellComponent : IComponentData {
    public int3 cellCoord;
}
```

---

## 2. ECS SYSTEMS (Update Pipeline)

```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
public partial class EnemyUpdateSystemGroup : ComponentSystemGroup { }

// System 1: Enemy AI/Movement
[BurstCompile]
[UpdateInGroup(typeof(EnemyUpdateSystemGroup))]
public partial struct EnemyAISystem : ISystem {

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        float deltaTime = SystemAPI.Time.DeltaTime;
        Entity playerEntity = SystemAPI.GetSingleton<PlayerTag>().entity;
        float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

        // Parallel job pour 2000+ enemies
        var job = new EnemyMoveJob {
            deltaTime = deltaTime,
            playerPosition = playerPos
        };

        job.ScheduleParallel();
    }
}

[BurstCompile]
partial struct EnemyMoveJob : IJobEntity {
    public float deltaTime;
    public float3 playerPosition;

    void Execute(
        ref LocalTransform transform,
        ref VelocityComponent velocity,
        in EnemyVoxelComponent enemy,
        in TargetComponent target
    ) {
        // Simple chase AI
        float3 direction = math.normalize(playerPosition - transform.Position);
        velocity.value = direction * enemy.moveSpeed;
        transform.Position += velocity.value * deltaTime;

        // Face target
        transform.Rotation = quaternion.LookRotationSafe(direction, math.up());
    }
}

// System 2: Spatial Hash Update
[BurstCompile]
[UpdateInGroup(typeof(EnemyUpdateSystemGroup))]
[UpdateAfter(typeof(EnemyAISystem))]
public partial struct SpatialHashUpdateSystem : ISystem {

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        // Update spatial hash cells basé sur nouvelles positions
        foreach (var (transform, cellComp) in SystemAPI.Query<
            RefRO<LocalTransform>,
            RefRW<SpatialHashCellComponent>>()
        ) {
            int3 newCell = CalculateCell(transform.ValueRO.Position);
            if (!newCell.Equals(cellComp.ValueRO.cellCoord)) {
                cellComp.ValueRW.cellCoord = newCell;
            }
        }
    }

    int3 CalculateCell(float3 position) {
        const float CELL_SIZE = 3.2f;
        return new int3(
            (int)math.floor(position.x / CELL_SIZE),
            (int)math.floor(position.y / CELL_SIZE),
            (int)math.floor(position.z / CELL_SIZE)
        );
    }
}

// System 3: LOD Calculation
[BurstCompile]
[UpdateInGroup(typeof(EnemyUpdateSystemGroup))]
public partial struct EnemyLODSystem : ISystem {

    [BurstCompile]
    public void OnUpdate(ref SystemState state) {
        Entity playerEntity = SystemAPI.GetSingleton<PlayerTag>().entity;
        float3 playerPos = SystemAPI.GetComponent<LocalTransform>(playerEntity).Position;

        foreach (var (transform, meshData) in SystemAPI.Query<
            RefRO<LocalTransform>,
            RefRW<VoxelMeshDataComponent>>()
        ) {
            float distance = math.distance(transform.ValueRO.Position, playerPos);

            int newLOD = 0;
            if (distance > 50f) newLOD = 1;
            if (distance > 100f) newLOD = 2;
            if (distance > 200f) newLOD = 3;

            if (newLOD != meshData.ValueRO.currentLOD) {
                meshData.ValueRW.currentLOD = newLOD;
                meshData.ValueRW.isDirty = true; // Need mesh swap
            }
        }
    }
}

// System 4: Voxel Destruction Processing
[UpdateInGroup(typeof(EnemyUpdateSystemGroup))]
public partial class VoxelDestructionSystem : SystemBase {

    protected override void OnUpdate() {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        Entities
            .WithAll<VoxelDestructionComponent>()
            .ForEach((Entity entity, ref EnemyVoxelComponent enemy, ref VoxelMeshDataComponent meshData, in VoxelDestructionComponent destruction) => {

                // Check if enemy fully destroyed
                if (destruction.isFullyDestroyed || enemy.health <= 0) {
                    // Spawn debris VFX
                    ecb.AddComponent<DestroyedTag>(entity);
                    return;
                }

                // Partial destruction → remesh
                if (destruction.destroyedVoxelCount > 0) {
                    meshData.isDirty = true;
                }

            }).Schedule();

        Dependency.Complete();
        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}
```

---

## 3. HYBRID RENDERING (ECS + GameObjects)

```csharp
// Hybrid approach: ECS entities avec GameObject meshes
public class EnemyVoxelSpawner : MonoBehaviour {

    public VoxelModelAsset enemyModel;
    public GameObject enemyPrefab; // Hybrid GameObject

    public void SpawnEnemy(float3 position) {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // Create ECS entity
        var entity = entityManager.CreateEntity(
            typeof(EnemyVoxelComponent),
            typeof(VoxelMeshDataComponent),
            typeof(VoxelDestructionComponent),
            typeof(TargetComponent),
            typeof(VelocityComponent),
            typeof(LocalTransform)
        );

        // Initialize components
        entityManager.SetComponentData(entity, new EnemyVoxelComponent {
            health = 100,
            maxHealth = 100,
            moveSpeed = 5f,
            type = EnemyType.BasicZombie
        });

        entityManager.SetComponentData(entity, new LocalTransform {
            Position = position,
            Rotation = quaternion.identity,
            Scale = 1f
        });

        // Allocate voxel data
        var voxelData = new NativeArray<VoxelType>(32*32*32, Allocator.Persistent);
        // ... load from model asset ...

        entityManager.SetComponentData(entity, new VoxelDestructionComponent {
            voxelData = voxelData,
            destroyedVoxelCount = 0,
            isFullyDestroyed = false
        });

        // Instantiate GameObject (Hybrid)
        var go = Instantiate(enemyPrefab, position, Quaternion.identity);
        var hybrid = go.AddComponent<EnemyHybridLink>();
        hybrid.entity = entity;
    }
}

// Hybrid link (GameObject <-> ECS)
public class EnemyHybridLink : MonoBehaviour {
    public Entity entity;

    void LateUpdate() {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        if (entityManager.Exists(entity)) {
            var transform = entityManager.GetComponentData<LocalTransform>(entity);
            this.transform.position = transform.Position;
            this.transform.rotation = transform.Rotation;
        }
    }
}
```

---

## 4. INSTANCING FOR IDENTICAL ENEMIES

```csharp
// GPU Instancing pour enemies identiques (même mesh)
public class VoxelInstanceRenderer : MonoBehaviour {

    public Mesh sharedMesh;
    public Material instanceMaterial;

    List<Matrix4x4> matrices = new List<Matrix4x4>(1000);

    void Update() {
        matrices.Clear();

        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = entityManager.CreateEntityQuery(
            typeof(EnemyVoxelComponent),
            typeof(LocalTransform)
        );

        var transforms = query.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        foreach (var t in transforms) {
            matrices.Add(Matrix4x4.TRS(t.Position, t.Rotation, Vector3.one));
        }

        transforms.Dispose();

        // Render batches (1023 instances max per call)
        int batchSize = 1023;
        for (int i = 0; i < matrices.Count; i += batchSize) {
            int count = Mathf.Min(batchSize, matrices.Count - i);
            Graphics.DrawMeshInstanced(
                sharedMesh,
                0,
                instanceMaterial,
                matrices.GetRange(i, count)
            );
        }
    }
}
```

---

## 5. MEMORY LAYOUT (Cache-Friendly)

```
ECS ARCHETYPE (2000 enemies):
┌──────────────────────────────────┐
│ Chunk 0 (128 entities)           │
├──────────────────────────────────┤
│ LocalTransform[128]   (contig)   │  ← Cache-friendly
│ EnemyVoxelComponent[128]         │
│ VelocityComponent[128]           │
│ ...                              │
└──────────────────────────────────┘

TOTAL MEMORY (2000 enemies):
├─ ECS Components: ~50 KB per enemy × 2000 = 100 MB
├─ Voxel data: 32 KB × 2000 = 64 MB
├─ Mesh instances: Shared (GPU instancing) = 10 MB
└─ TOTAL: ~174 MB
```

---

## 6. PERFORMANCE TARGETS

```
ECS SYSTEMS BUDGET: 3.0 ms per frame (2000 enemies)

├─ EnemyAISystem (parallel): 1.5 ms
├─ SpatialHashUpdate: 0.5 ms
├─ LODSystem: 0.3 ms
├─ DestructionSystem: 0.5 ms
├─ Hybrid sync: 0.2 ms
└─ TOTAL: 3.0 ms ✓
```

---

## 7. IMPLEMENTATION CHECKLIST

- [ ] Define ECS components (Enemy, Voxel, AI)
- [ ] Implement EnemyAISystem avec Burst
- [ ] Create SpatialHashUpdateSystem
- [ ] Implement LODSystem pour 2000+ entities
- [ ] Setup GPU Instancing rendering
- [ ] Create Hybrid GameObject<->ECS link
- [ ] Test performance (2000 enemies, 60 FPS)
- [ ] Profile avec ECS Profiler

---

**Document Version:** 1.0
**Dependencies:** 01, 02, 03, 04
**Next:** 06_COLLISION_SYSTEM.md
