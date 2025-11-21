# Voxel Terrain Package

**Version**: 1.0.0
**Namespace**: `TimeSurvivor.Voxel.Terrain`
**Assembly Definition**: `TimeSurvivor.Voxel.Terrain`

---

## Overview

Procedural terrain generation and streaming system for macro-scale voxels. Includes infinite terrain streaming, LRU cache management, and destructible overlay system for micro-voxels.

---

## Architecture Decisions

This package implements the following ADRs:
- **ADR-006**: Destructible overlay system for props (dual-scale approach)
- **ADR-007**: Procedural terrain generation with 3D Simplex noise
- **ADR-008**: Dual-scale voxel system (macro terrain + micro props)

---

## Responsibilities

### Terrain Generation
- `SimplexNoise3D` - 3D Simplex noise implementation (Burst-compatible)
- `ProceduralTerrainGenerationJob` - Procedural voxel generation Job

### Chunk Management
- `TerrainChunk` - Individual terrain chunk with voxel data and mesh
- `ChunkManager` - Manages chunk lifecycle (generation, meshing, unloading)

### Streaming
- `ProceduralTerrainStreamer` - MonoBehaviour for infinite terrain streaming
- `LRUCache<TKey, TValue>` - Least Recently Used cache for memory management

### Destructible Overlay (ADR-006)
- `OverlayChunk` - Micro-voxel overlay chunk for destructible props
- `DestructibleOverlayManager` - Manages overlay chunk lifecycle and destruction

---

## Dependencies

### Required Packages
- `TimeSurvivor.Voxel.Core` - Core voxel types and utilities
- `TimeSurvivor.Voxel.Rendering` - Mesh generation (greedy meshing)
- `Unity.Mathematics` - Math operations
- `Unity.Collections` - NativeArray/NativeList
- `Unity.Burst` - Burst compiler
- `Unity.Jobs` - Unity Job System

---

## Usage Examples

### Basic Terrain Streaming Setup

1. **Create VoxelConfiguration ScriptableObject**:
   - Right-click in Project: `Create > TimeSurvivor > Voxel Configuration`
   - Configure settings:
     - Chunk Size: 16
     - Macro Voxel Size: 0.2
     - Render Distance: 8
     - Max Cached Chunks: 300

2. **Create Terrain Streamer**:
```csharp
using UnityEngine;
using TimeSurvivor.Voxel.Terrain;

public class TerrainSetup : MonoBehaviour
{
    [SerializeField] private VoxelConfiguration _config;
    [SerializeField] private Material _terrainMaterial;

    void Start()
    {
        var streamerObj = new GameObject("TerrainStreamer");
        var streamer = streamerObj.AddComponent<ProceduralTerrainStreamer>();

        // Assign configuration via reflection or set in Inspector
        // streamer will automatically use MainCamera as streaming target
    }
}
```

3. **Assign in Inspector**:
   - Drag VoxelConfiguration to Streaming Config field
   - Drag terrain Material to Chunk Material field
   - Enable "Use Main Camera" or assign custom Transform

### Manual Chunk Management

```csharp
using UnityEngine;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

public class ManualChunkLoader : MonoBehaviour
{
    [SerializeField] private VoxelConfiguration _config;
    [SerializeField] private Material _chunkMaterial;

    private ChunkManager _chunkManager;

    void Start()
    {
        _chunkManager = new ChunkManager(_config, transform, _chunkMaterial);

        // Load chunks around origin
        for (int x = -2; x <= 2; x++)
        {
            for (int z = -2; z <= 2; z++)
            {
                ChunkCoord coord = new ChunkCoord(x, 0, z);
                _chunkManager.LoadChunk(coord);
            }
        }
    }

    void Update()
    {
        // Process chunk generation and meshing
        _chunkManager.ProcessGenerationQueue();
        _chunkManager.ProcessMeshingQueue(Time.deltaTime);
    }

    void OnDestroy()
    {
        _chunkManager?.Dispose();
    }
}
```

### Using LRU Cache

```csharp
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

public class CacheExample
{
    private LRUCache<ChunkCoord, TerrainChunk> _cache;

    public void Initialize()
    {
        _cache = new LRUCache<ChunkCoord, TerrainChunk>(300); // Max 300 chunks
    }

    public void AddChunk(ChunkCoord coord, TerrainChunk chunk)
    {
        // Add to cache (evicts LRU chunk if full)
        TerrainChunk evicted = _cache.Put(coord, chunk);

        if (evicted != null)
        {
            // Handle evicted chunk (unload, dispose, etc.)
            evicted.Dispose();
        }
    }

    public TerrainChunk GetChunk(ChunkCoord coord)
    {
        if (_cache.TryGet(coord, out var chunk))
        {
            return chunk; // Marks as recently used
        }
        return null;
    }
}
```

### Destructible Overlay System

```csharp
using UnityEngine;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Terrain;

public class DestructibleProp : MonoBehaviour
{
    [SerializeField] private DestructibleOverlayManager _overlayManager;
    [SerializeField] private byte _damagePerHit = 50;

    void OnMouseDown()
    {
        // Apply damage to voxel at hit position
        float3 hitPosition = transform.position;
        bool destroyed = _overlayManager.DamageVoxelAt(hitPosition, _damagePerHit);

        if (destroyed)
        {
            Debug.Log("Voxel destroyed!");
        }
    }
}
```

### Custom Terrain Generation

```csharp
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;

[BurstCompile]
public struct CustomTerrainJob : IJob
{
    [ReadOnly] public ChunkCoord ChunkCoord;
    [ReadOnly] public int ChunkSize;
    [ReadOnly] public float VoxelSize;

    [WriteOnly] public NativeArray<VoxelType> VoxelData;

    public void Execute()
    {
        var noise = new SimplexNoise3D(12345, 0.02f, 4);

        float3 chunkWorldOrigin = VoxelMath.ChunkCoordToWorld(ChunkCoord, ChunkSize, VoxelSize);

        for (int i = 0; i < VoxelData.Length; i++)
        {
            int3 localCoord = VoxelMath.Unflatten3DIndex(i, ChunkSize);
            float3 worldPos = chunkWorldOrigin + (float3)localCoord * VoxelSize;

            float density = noise.GetNoise(worldPos.x, worldPos.y, worldPos.z);

            // Custom generation logic
            if (density > 0.2f && worldPos.y < 5f)
                VoxelData[i] = VoxelType.Grass;
            else if (density > 0.4f)
                VoxelData[i] = VoxelType.Stone;
            else
                VoxelData[i] = VoxelType.Air;
        }
    }
}
```

---

## Performance Considerations

### Terrain Streaming
- **Render Distance**: 8 chunks = ~(8*2+1)³ = 4913 chunks visible (most are culled)
- **Load Budget**: 2 chunks/frame prevents frame spikes
- **LRU Cache**: Automatically evicts least used chunks when memory limit reached

### Chunk Generation
- **Burst Compilation**: 10-30x faster than C# for noise generation
- **Job System**: Parallel generation of multiple chunks
- **Amortized Loading**: Spread work across frames

### Memory Usage
- **Macro Voxel Chunk**: 16³ bytes = 4 KB per chunk (voxel data only)
- **Mesh Data**: ~500-2000 vertices per chunk (varies by terrain)
- **300 Chunks Cached**: ~230 MB (150 MB voxel data + 80 MB meshes, estimated)

### Optimization Tips
1. **Reduce Render Distance**: Lower value = fewer chunks loaded
2. **Increase Chunk Size**: 32³ chunks = fewer chunks but more memory per chunk
3. **LOD System**: Use lower-detail meshes for distant chunks (future enhancement)
4. **Async Jobs**: Don't Complete() immediately, use JobHandle dependencies

---

## API Reference

### ProceduralTerrainStreamer (MonoBehaviour)
```csharp
public class ProceduralTerrainStreamer : MonoBehaviour
{
    [SerializeField] private VoxelConfiguration _config;
    [SerializeField] private Material _chunkMaterial;
    [SerializeField] private Transform _streamingTarget;
    [SerializeField] private bool _useMainCamera = true;
    [SerializeField] private bool _showDebugInfo = true;
}
```

### ChunkManager
```csharp
public class ChunkManager : IChunkManager
{
    // IChunkManager implementation
    public void LoadChunk(ChunkCoord coord);
    public void UnloadChunk(ChunkCoord coord);
    public bool IsChunkLoaded(ChunkCoord coord);
    public void MarkDirty(ChunkCoord coord);
    public object GetChunk(ChunkCoord coord);

    // Typed getter
    public TerrainChunk GetTerrainChunk(ChunkCoord coord);

    // Queue processing (call from Update)
    public void ProcessGenerationQueue();
    public void ProcessMeshingQueue(float deltaTime);

    // Status
    public int GenerationQueueCount { get; }
    public int MeshingQueueCount { get; }
    public IEnumerable<TerrainChunk> GetAllChunks();

    // Cleanup
    public void Dispose();
}
```

### LRUCache<TKey, TValue>
```csharp
public class LRUCache<TKey, TValue>
{
    public LRUCache(int capacity);

    public bool TryGet(TKey key, out TValue value);
    public TValue Put(TKey key, TValue value); // Returns evicted value
    public bool Contains(TKey key);
    public bool Remove(TKey key);
    public void Clear();

    public int Count { get; }
    public int Capacity { get; }
    public IEnumerable<TKey> Keys { get; }
    public IEnumerable<TValue> Values { get; }
}
```

### SimplexNoise3D
```csharp
[BurstCompile]
public struct SimplexNoise3D
{
    public SimplexNoise3D(int seed, float frequency, int octaves,
                          float lacunarity = 2.0f, float persistence = 0.5f);

    public float GetNoise(float x, float y, float z); // Returns [-1, 1]
}
```

### DestructibleOverlayManager (MonoBehaviour)
```csharp
public class DestructibleOverlayManager : MonoBehaviour
{
    public void LoadOverlayChunk(ChunkCoord coord);
    public void UnloadOverlayChunk(ChunkCoord coord);
    public bool IsOverlayChunkLoaded(ChunkCoord coord);
    public OverlayChunk GetOverlayChunk(ChunkCoord coord);

    // Destruction
    public bool DamageVoxelAt(float3 worldPosition, byte damageAmount);

    public int LoadedOverlayChunkCount { get; }
}
```

---

## Known Limitations

1. **Synchronous Job Completion**: Jobs currently Complete() immediately
   - **TODO**: Implement async job scheduling with JobHandle dependencies
2. **No LOD System**: All chunks use same detail level
   - **Future**: Implement distance-based LOD for distant chunks
3. **Simple Terrain Generation**: Basic noise-based generation
   - **Future**: Add biomes, caves, structures, features
4. **Overlay Generation Placeholder**: Empty implementation
   - **TODO**: Implement procedural prop generation (trees, rocks, etc.)
5. **No Chunk Saving**: Chunks regenerated from seed every time
   - **Future**: Implement chunk serialization for modified terrain

---

## Testing

Test assembly: `TimeSurvivor.Voxel.Terrain.Tests`

Recommended tests:
- LRU cache correctness (eviction order, capacity limits)
- Chunk coordinate conversions
- Terrain generation determinism (same seed = same terrain)
- Streaming behavior (load/unload at boundaries)
- Performance benchmarks (generation time, meshing time)

---

## Integration with Other Packages

### voxel-rendering
- Uses `GreedyMeshingJob` for chunk meshing
- Uses `MeshBuilder` for mesh creation

### voxel-core
- Uses `VoxelType`, `ChunkCoord`, `VoxelMath`
- Implements `IChunkManager` interface

### voxel-physics (future)
- Collision mesh generation for terrain chunks
- Raycasting against voxel terrain

---

## License

Copyright (c) 2025 TimeSurvivor. All rights reserved.
