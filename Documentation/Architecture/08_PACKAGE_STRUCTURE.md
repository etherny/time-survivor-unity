# Package Structure - Clean Architecture

---

## 1. MODULAR PACKAGE ORGANIZATION

```
Packages/
├── com.timesurvivor.voxel.core/
│   ├── Runtime/
│   │   ├── Data/
│   │   │   ├── VoxelType.cs
│   │   │   ├── MacroChunk.cs
│   │   │   ├── MicroChunk.cs
│   │   │   └── VoxelCoordinates.cs
│   │   └── Interfaces/
│   │       ├── IVoxelChunk.cs
│   │       └── IChunkManager.cs
│   └── package.json
│
├── com.timesurvivor.voxel.terrain/
│   ├── Runtime/
│   │   ├── MacroChunkManager.cs
│   │   ├── TerrainGenerator.cs
│   │   └── TerrainLODSystem.cs
│   └── package.json (depends: voxel.core)
│
├── com.timesurvivor.voxel.destructible/
│   ├── Runtime/
│   │   ├── MicroObjectManager.cs
│   │   ├── DestructionSystem.cs
│   │   └── VoxelDebrisSpawner.cs
│   └── package.json (depends: voxel.core)
│
├── com.timesurvivor.voxel.rendering/
│   ├── Runtime/
│   │   ├── GreedyMeshing.cs
│   │   ├── AsyncMeshingScheduler.cs
│   │   └── VoxelMaterialAtlas.cs
│   └── package.json (depends: voxel.core)
│
├── com.timesurvivor.voxel.physics/
│   ├── Runtime/
│   │   ├── VoxelCollisionBaker.cs
│   │   ├── VoxelRaycast.cs
│   │   └── SpatialHash.cs
│   └── package.json (depends: voxel.core)
│
└── com.timesurvivor.voxel.ecs/
    ├── Runtime/
    │   ├── Components/
    │   │   ├── EnemyVoxelComponent.cs
    │   │   └── VoxelMeshDataComponent.cs
    │   ├── Systems/
    │   │   ├── EnemyAISystem.cs
    │   │   ├── LODSystem.cs
    │   │   └── DestructionSystem.cs
    │   └── Hybrid/
    │       └── EnemyHybridLink.cs
    └── package.json (depends: voxel.core, Unity.Entities)
```

---

## 2. DEPENDENCY GRAPH

```
                  voxel.core
                       │
        ┌──────────────┼──────────────┐
        │              │              │
   voxel.terrain  voxel.rendering  voxel.physics
        │              │              │
        └──────────────┼──────────────┘
                       │
                 voxel.destructible
                       │
                   voxel.ecs
                       │
                  Game Assembly
```

---

## 3. PUBLIC INTERFACES

```csharp
// voxel.core/Runtime/Interfaces/IChunkManager.cs
namespace TimeSurvivor.Voxel.Core {
    public interface IChunkManager {
        void LoadChunk(int3 chunkCoord);
        void UnloadChunk(int3 chunkCoord);
        MacroChunk GetChunk(int3 chunkCoord);
        void MarkDirty(int3 chunkCoord);
    }

    public interface IVoxelMesher {
        Mesh GenerateMesh(NativeArray<VoxelType> voxels, int chunkSize);
        JobHandle ScheduleMeshing(NativeArray<VoxelType> voxels, Action<Mesh> callback);
    }
}
```

---

## 4. ASSEMBLY DEFINITIONS

```json
// com.timesurvivor.voxel.core/Runtime/TimeSurvivor.Voxel.Core.asmdef
{
  "name": "TimeSurvivor.Voxel.Core",
  "references": [
    "Unity.Mathematics",
    "Unity.Collections",
    "Unity.Burst"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": true
}

// com.timesurvivor.voxel.ecs/Runtime/TimeSurvivor.Voxel.ECS.asmdef
{
  "name": "TimeSurvivor.Voxel.ECS",
  "references": [
    "TimeSurvivor.Voxel.Core",
    "Unity.Entities",
    "Unity.Transforms",
    "Unity.Burst"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": true
}
```

---

**Document Version:** 1.0
