# Voxel Rendering Package

**Version**: 1.0.0
**Namespace**: `TimeSurvivor.Voxel.Rendering`
**Assembly Definition**: `TimeSurvivor.Voxel.Rendering`

---

## Overview

Mesh generation and rendering system for voxel terrain. Implements greedy meshing algorithm (ADR-003) and amortized meshing (ADR-005) for optimal performance.

---

## Architecture Decisions

This package implements the following ADRs:
- **ADR-002**: Burst + Jobs for parallel mesh generation
- **ADR-003**: Greedy meshing algorithm for polygon reduction
- **ADR-005**: Amortized meshing to spread work across frames

---

## Responsibilities

### Meshing Jobs
- `GreedyMeshingJob` - Burst-compiled greedy meshing algorithm
- `AmortizedMeshingJob` - Time-budgeted meshing wrapper

### Mesh Building
- `MeshBuilder` - Converts NativeArray data to Unity Mesh objects

### Material Management
- `VoxelMaterialAtlas` - ScriptableObject for texture atlas management
- `VoxelTextureMapping` - Maps VoxelType to atlas tiles

---

## Dependencies

### Required Packages
- `TimeSurvivor.Voxel.Core` - Core voxel types and interfaces
- `Unity.Mathematics` - Math operations
- `Unity.Collections` - NativeArray and NativeList
- `Unity.Burst` - Burst compiler
- `Unity.Jobs` - Unity Job System

---

## Usage Examples

### Basic Greedy Meshing

```csharp
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Rendering;

public class ChunkRenderer : MonoBehaviour
{
    [SerializeField] private VoxelConfiguration _config;

    public void GenerateMesh(NativeArray<VoxelType> voxelData)
    {
        int chunkSize = _config.ChunkSize;

        // Allocate output buffers
        var vertices = new NativeList<float3>(Allocator.TempJob);
        var triangles = new NativeList<int>(Allocator.TempJob);
        var uvs = new NativeList<float2>(Allocator.TempJob);
        var normals = new NativeList<float3>(Allocator.TempJob);

        // Allocate mask buffer for greedy algorithm
        var mask = new NativeArray<bool>(chunkSize * chunkSize, Allocator.TempJob);

        // Create and schedule job
        var job = new GreedyMeshingJob
        {
            Voxels = voxelData,
            ChunkSize = chunkSize,
            Vertices = vertices,
            Triangles = triangles,
            UVs = uvs,
            Normals = normals,
            Mask = mask
        };

        job.Schedule().Complete();

        // Build Unity mesh
        Mesh mesh = MeshBuilder.BuildMesh(vertices, triangles, uvs, normals);

        // Apply to MeshFilter
        GetComponent<MeshFilter>().mesh = mesh;

        // Cleanup
        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();
        normals.Dispose();
        mask.Dispose();
    }
}
```

### Amortized Meshing (Spread Work Across Frames)

```csharp
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Rendering;

public class AmortizedMeshManager : MonoBehaviour
{
    [SerializeField] private VoxelConfiguration _config;
    [SerializeField] private float _maxMeshingTimeMs = 3f;

    private Queue<ChunkCoord> _meshQueue = new Queue<ChunkCoord>();

    void Update()
    {
        float startTime = Time.realtimeSinceStartup;

        while (_meshQueue.Count > 0)
        {
            // Check time budget
            float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
            if (elapsed > _maxMeshingTimeMs)
                break;

            ChunkCoord coord = _meshQueue.Dequeue();
            MeshChunk(coord);
        }
    }

    public void QueueChunkForMeshing(ChunkCoord coord)
    {
        _meshQueue.Enqueue(coord);
    }

    private void MeshChunk(ChunkCoord coord)
    {
        // Retrieve voxel data for chunk
        NativeArray<VoxelType> voxelData = GetVoxelData(coord);

        // Schedule meshing job
        var vertices = new NativeList<float3>(Allocator.TempJob);
        var triangles = new NativeList<int>(Allocator.TempJob);
        var uvs = new NativeList<float2>(Allocator.TempJob);
        var normals = new NativeList<float3>(Allocator.TempJob);
        var mask = new NativeArray<bool>(_config.ChunkSize * _config.ChunkSize, Allocator.TempJob);

        var job = new AmortizedMeshingJob
        {
            Voxels = voxelData,
            ChunkSize = _config.ChunkSize,
            Vertices = vertices,
            Triangles = triangles,
            UVs = uvs,
            Normals = normals,
            Mask = mask
        };

        job.Schedule().Complete();

        // Build and apply mesh
        Mesh mesh = MeshBuilder.BuildMesh(vertices, triangles, uvs, normals);
        ApplyMeshToChunk(coord, mesh);

        // Cleanup
        vertices.Dispose();
        triangles.Dispose();
        uvs.Dispose();
        normals.Dispose();
        mask.Dispose();
        voxelData.Dispose();
    }

    private NativeArray<VoxelType> GetVoxelData(ChunkCoord coord)
    {
        // TODO: Implement chunk data retrieval
        return new NativeArray<VoxelType>(0, Allocator.Temp);
    }

    private void ApplyMeshToChunk(ChunkCoord coord, Mesh mesh)
    {
        // TODO: Apply mesh to chunk GameObject
    }
}
```

### Creating Voxel Material Atlas

1. **Create Atlas Texture**:
   - Create a square texture (e.g., 1024x1024)
   - Divide into grid (e.g., 16x16 = 256 tiles)
   - Place voxel textures in tiles (grass, dirt, stone, etc.)
   - Set texture to Point filtering (no interpolation between voxels)

2. **Create Material**:
   - Create new Material with Standard or URP/Lit shader
   - Assign atlas texture to Base Map/Albedo
   - Disable texture tiling

3. **Create Atlas ScriptableObject**:
   - Right-click in Project: `Create > TimeSurvivor > Voxel Material Atlas`
   - Assign Atlas Texture and Material
   - Set Atlas Size (e.g., 16 for 16x16 grid)
   - Configure Texture Mappings for each VoxelType

4. **Configure Texture Mappings**:
```csharp
// Example mapping in Inspector:
VoxelType: Grass
TopTileIndex: 0    (grass top texture)
SideTileIndex: 1   (grass side texture)
BottomTileIndex: 2 (dirt texture)

VoxelType: Stone
TopTileIndex: 5
SideTileIndex: 5   (all faces same)
BottomTileIndex: 5
```

---

## Performance Considerations

### Greedy Meshing Benefits
- **Polygon Reduction**: Reduces quad count by 60-80% vs naive meshing
- **Fill Rate**: Less overdraw from fewer polygons
- **Vertex Processing**: Fewer vertices to transform

### Burst Compilation
- Jobs compile to native code via Burst
- **10-30x faster** than equivalent C# code
- Ensure `UseBurstCompilation` is enabled in VoxelConfiguration

### Amortized Meshing
- Spreads heavy meshing work across multiple frames
- Prevents frame spikes when loading many chunks
- Budget: 2-3ms per frame for meshing (configurable)

### Memory Layout
- Use `NativeList` for dynamic output (vertices grow as mesh is built)
- Reuse `mask` buffer between meshing operations to reduce allocations
- Expected mesh size: ~500-2000 vertices per 16³ chunk (varies by terrain)

### Optimization Tips
1. **Object Pooling**: Reuse NativeArray allocations between chunks
2. **Parallel Jobs**: Schedule multiple meshing jobs for different chunks
3. **LOD**: Use lower-detail meshes for distant chunks (future enhancement)
4. **Frustum Culling**: Unity automatically culls off-screen chunks

---

## API Reference

### GreedyMeshingJob
```csharp
[BurstCompile]
public struct GreedyMeshingJob : IJob
{
    [ReadOnly] public NativeArray<VoxelType> Voxels;
    [ReadOnly] public int ChunkSize;

    [WriteOnly] public NativeList<float3> Vertices;
    [WriteOnly] public NativeList<int> Triangles;
    [WriteOnly] public NativeList<float2> UVs;
    [WriteOnly] public NativeList<float3> Normals;

    public NativeArray<bool> Mask; // Temp buffer (size = ChunkSize²)

    public void Execute();
}
```

### MeshBuilder
```csharp
public static class MeshBuilder
{
    // Build mesh from native arrays
    public static Mesh BuildMesh(
        NativeArray<float3> vertices,
        NativeArray<int> triangles,
        NativeArray<float2> uvs,
        NativeArray<float3> normals);

    // Build mesh from native lists (more common)
    public static Mesh BuildMesh(
        NativeList<float3> vertices,
        NativeList<int> triangles,
        NativeList<float2> uvs,
        NativeList<float3> normals);

    // Build with auto-calculated normals (slower)
    public static Mesh BuildMeshAutoNormals(
        NativeArray<float3> vertices,
        NativeArray<int> triangles,
        NativeArray<float2> uvs);

    // Update existing mesh (reuse)
    public static void UpdateMesh(Mesh mesh, ...);

    // Calculate memory usage
    public static int CalculateMeshMemoryUsage(int vertexCount, int triangleCount);
}
```

### VoxelMaterialAtlas
```csharp
public class VoxelMaterialAtlas : ScriptableObject
{
    public Texture2D AtlasTexture;
    public Material VoxelMaterial;
    public int AtlasSize; // Grid size (e.g., 16 for 16x16)
    public VoxelTextureMapping[] TextureMappings;

    // Get UV rect for a voxel type's face
    public Vector4 GetUVRect(VoxelType voxelType, VoxelFace face);
}
```

---

## Used By

- `TimeSurvivor.Voxel.Terrain` - Terrain chunk meshing
- `TimeSurvivor.Voxel.Physics` - Collision mesh generation
- `TimeSurvivor.Game.*` - Game-specific rendering features

---

## Known Limitations

1. **Current UV Implementation**: Simple 0-1 mapping, not atlas-aware yet
   - **TODO**: Integrate VoxelMaterialAtlas UVs into GreedyMeshingJob
2. **No Ambient Occlusion**: Voxels have flat shading
   - **Future**: Add vertex color AO pass
3. **No Transparency Sorting**: Transparent voxels may render incorrectly
   - **Future**: Separate opaque/transparent mesh passes

---

## Testing

Test assembly: `TimeSurvivor.Voxel.Rendering.Tests`

Recommended tests:
- Greedy meshing correctness (compare output to naive meshing)
- Mesh topology validation (no holes, correct winding)
- Performance benchmarks (meshing time for various chunk densities)

---

## License

Copyright (c) 2025 TimeSurvivor. All rights reserved.
