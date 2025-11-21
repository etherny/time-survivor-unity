# Performance Optimizations - Burst, SIMD, Profiling

---

## 1. BURST COMPILER OPTIMIZATIONS

```csharp
// Enable all Burst features
[BurstCompile(
    CompileSynchronously = true,
    FloatMode = FloatMode.Fast,
    FloatPrecision = FloatPrecision.Low,
    OptimizeFor = OptimizeFor.Performance
)]
public struct OptimizedJob : IJobParallelFor {
    // SIMD-friendly data layout
    [ReadOnly] public NativeArray<float4> positions; // Vector4 utilise SIMD
    [WriteOnly] public NativeArray<float4> results;

    public void Execute(int index) {
        // Burst auto-vectorize avec SIMD instructions
        results[index] = math.normalize(positions[index]) * 2f;
    }
}
```

---

## 2. MEMORY OPTIMIZATIONS

### Zero-Allocation Hot Paths
```csharp
// BAD: Allocations en hot path
void UpdateEnemies() {
    var enemies = GetEnemies().ToList(); // Allocation
    foreach (var e in enemies.Where(x => x.isAlive)) { // LINQ allocation
        // ...
    }
}

// GOOD: Zero-allocation
void UpdateEnemies() {
    var query = EntityManager.CreateEntityQuery(typeof(EnemyComponent));
    var enemies = query.ToComponentDataArray<EnemyComponent>(Allocator.Temp);

    for (int i = 0; i < enemies.Length; i++) {
        if (enemies[i].isAlive) {
            // ...
        }
    }

    enemies.Dispose();
}
```

### Struct Packing
```csharp
// Optimize struct layout (cache lines)
[StructLayout(LayoutKind.Sequential, Pack = 16)]
public struct EnemyData {
    public float3 position;      // 12 bytes
    public float health;         // 4 bytes (total: 16 bytes = 1 cache line)
    public float3 velocity;      // 12 bytes
    public float speed;          // 4 bytes (total: 32 bytes = 2 cache lines)
}
```

---

## 3. GPU OPTIMIZATIONS (URP)

### SRP Batcher
```csharp
// Material compatible SRP Batcher
Shader "Custom/VoxelSRPBatcher" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader {
        Tags { "RenderPipeline" = "UniversalRenderPipeline" }

        CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
        CBUFFER_END

        // Shader code...
    }
}
```

### GPU Instancing
```csharp
// Enable instancing pour voxel materials
Material voxelMat = new Material(voxelShader);
voxelMat.enableInstancing = true;

// DrawMeshInstanced (1023 instances max per call)
Graphics.DrawMeshInstanced(mesh, 0, voxelMat, matrices);
```

---

## 4. PROFILING STRATEGY

### Custom Profiler Markers
```csharp
using Unity.Profiling;

static readonly ProfilerMarker s_ChunkMeshingMarker =
    new ProfilerMarker(ProfilerCategory.Render, "VoxelMeshing");

void MeshChunk(MacroChunk chunk) {
    using (s_ChunkMeshingMarker.Auto()) {
        // Meshing code
    }
}
```

### Performance Counters
```csharp
static readonly ProfilerCounter<int> s_ActiveChunksCounter =
    new ProfilerCounter<int>(ProfilerCategory.Memory, "Active Chunks", ProfilerMarkerDataUnit.Count);

void Update() {
    s_ActiveChunksCounter.Value = activeChunks.Count;
}
```

---

## 5. TARGET BENCHMARKS

```
60 FPS BUDGET: 16.66ms per frame

BREAKDOWN:
├─ Gameplay Logic:      1.0 ms
├─ ECS Systems:         3.0 ms (2000 enemies)
├─ Voxel Systems:       2.0 ms (chunking, LOD)
├─ Meshing:             1.5 ms (amortized)
├─ Physics:             2.0 ms (collisions)
├─ Rendering (URP):     5.0 ms (draw calls)
├─ VFX:                 0.5 ms
├─ Audio:               0.5 ms
└─ Reserve:             1.16ms
─────────────────────────────
TOTAL:                 16.66ms ✓

MEMORY BUDGET:
├─ Terrain:           384 MB
├─ Enemies:           174 MB
├─ Props:              50 MB
├─ Textures:          150 MB
├─ Other:             100 MB
└─ Reserve:          1142 MB
─────────────────────────────
TOTAL:              ~2000 MB ✓
```

---

**Document Version:** 1.0
