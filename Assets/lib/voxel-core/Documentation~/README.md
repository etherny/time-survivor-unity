# Voxel Core Package

**Version**: 1.0.0
**Namespace**: `TimeSurvivor.Voxel.Core`
**Assembly Definition**: `TimeSurvivor.Voxel.Core`

---

## Overview

Foundation package for the TimeSurvivor voxel engine. Provides core data types, interfaces, and utilities used by all other voxel packages.

This package is **dependency-free** (except Unity packages) and can be used as a standalone foundation for any voxel-based system.

---

## Architecture Decisions

This package implements the following ADRs (Architecture Decision Records):
- **ADR-004**: 16x16x16 chunk size for optimal memory and performance
- **ADR-008**: Dual-scale voxel system (macro 0.2u terrain, micro 0.1u props)

---

## Responsibilities

### Core Data Types
- `VoxelType` - Enum defining all voxel types (Air, Grass, Stone, etc.)
- `ChunkCoord` - Immutable struct for chunk coordinates with hash/equality
- `MacroVoxelData` - Data structure for terrain-scale voxels (0.2 Unity units)
- `MicroVoxelData` - Data structure for prop-scale voxels (0.1 Unity units, destructible)

### Interfaces
- `IChunkManager` - Contract for chunk lifecycle management
- `IVoxelGenerator` - Contract for procedural voxel generation

### Configuration
- `VoxelConfiguration` - ScriptableObject for engine-wide voxel settings

### Utilities
- `VoxelMath` - Static utility class for coordinate conversions and spatial calculations

---

## Dependencies

### Unity Packages (Required)
- `Unity.Mathematics` - High-performance math operations (float3, int3, etc.)
- `Unity.Collections` - NativeArray and other collections for Jobs
- `Unity.Burst` - Burst compiler for Jobs performance

### Installation
These packages are included in Unity 2022.3+ by default. If missing, install via Package Manager:
```
com.unity.mathematics
com.unity.collections
com.unity.burst
```

---

## Usage Examples

### Creating a ChunkCoord
```csharp
using TimeSurvivor.Voxel.Core;
using Unity.Mathematics;

// Method 1: Individual coordinates
ChunkCoord coord1 = new ChunkCoord(0, 0, 0);

// Method 2: int3
ChunkCoord coord2 = new ChunkCoord(new int3(5, 2, -3));

// Method 3: Operators
ChunkCoord offset = coord1 + new int3(1, 0, 0); // Move one chunk east
```

### Converting World Position to Chunk
```csharp
using TimeSurvivor.Voxel.Core;
using Unity.Mathematics;

float3 playerPosition = new float3(10.5f, 5.2f, -8.3f);
int chunkSize = 16;
float voxelSize = 0.2f;

ChunkCoord chunk = VoxelMath.WorldToChunkCoord(playerPosition, chunkSize, voxelSize);
// Result: ChunkCoord(3, 1, -3)
```

### Creating VoxelConfiguration
1. Right-click in Project window
2. Select `Create > TimeSurvivor > Voxel Configuration`
3. Name it `VoxelConfig`
4. Assign to your managers in the Inspector

```csharp
[SerializeField] private VoxelConfiguration _config;

void Start()
{
    Debug.Log($"Chunk size: {_config.ChunkSize}");
    Debug.Log($"Macro voxel size: {_config.MacroVoxelSize}");
    Debug.Log($"Chunk volume: {_config.ChunkVolume} voxels");
}
```

### Implementing IVoxelGenerator
```csharp
using TimeSurvivor.Voxel.Core;
using Unity.Collections;

public class FlatTerrainGenerator : IVoxelGenerator
{
    public NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator)
    {
        int volume = chunkSize * chunkSize * chunkSize;
        var voxels = new NativeArray<VoxelType>(volume, allocator);

        for (int i = 0; i < volume; i++)
        {
            int3 localCoord = VoxelMath.Unflatten3DIndex(i, chunkSize);

            // Simple flat terrain: grass on top, dirt/stone below
            if (localCoord.y == 0)
                voxels[i] = VoxelType.Grass;
            else if (localCoord.y < 0)
                voxels[i] = localCoord.y > -5 ? VoxelType.Dirt : VoxelType.Stone;
            else
                voxels[i] = VoxelType.Air;
        }

        return voxels;
    }

    public VoxelType GetVoxelAt(int worldX, int worldY, int worldZ)
    {
        if (worldY == 0) return VoxelType.Grass;
        if (worldY < 0) return worldY > -5 ? VoxelType.Dirt : VoxelType.Stone;
        return VoxelType.Air;
    }
}
```

---

## Performance Considerations

### Memory Layout
- `VoxelType` is 1 byte (enum byte)
- `ChunkCoord` is 12 bytes (3 int32)
- `MacroVoxelData` is 2 bytes (1 byte type + 1 byte metadata)
- `MicroVoxelData` is 3 bytes (1 byte type + 1 byte health + 1 byte metadata)

### Burst Compilation
All structs are Burst-compatible. Use in Jobs for maximum performance:
```csharp
[BurstCompile]
public struct ConversionJob : IJob
{
    public float3 worldPosition;
    public int chunkSize;
    public float voxelSize;

    public ChunkCoord result;

    public void Execute()
    {
        result = VoxelMath.WorldToChunkCoord(worldPosition, chunkSize, voxelSize);
    }
}
```

### Coordinate Conversions
- All `VoxelMath` methods are static and Burst-compatible
- Avoid conversions in hot paths (cache results when possible)
- Use Manhattan distance for chunk loading (faster than Euclidean)

---

## API Reference

### VoxelType Enum
| Value | Description | Properties |
|-------|-------------|------------|
| Air | Empty space | No collision, not rendered |
| Grass | Grass surface | Solid, opaque |
| Dirt | Dirt block | Solid, opaque |
| Stone | Stone block | Solid, opaque |
| Sand | Sand block | Solid, opaque |
| Water | Water block | Solid, transparent |
| Wood | Wood block | Solid, opaque |
| Leaves | Leaves block | Solid, transparent |

### VoxelMath Static Methods
- `WorldToChunkCoord(float3, int, float)` → ChunkCoord
- `WorldToVoxelCoord(float3, float)` → int3
- `ChunkCoordToWorld(ChunkCoord, int, float)` → float3
- `VoxelCoordToWorld(int3, float)` → float3
- `VoxelToLocalCoord(int3, int)` → int3
- `Flatten3DIndex(int, int, int, int)` → int
- `Unflatten3DIndex(int, int)` → int3
- `IsValidLocalCoord(int3, int)` → bool
- `ChunkManhattanDistance(ChunkCoord, ChunkCoord)` → int
- `ChunkDistanceSquared(ChunkCoord, ChunkCoord)` → int

---

## Used By

This package is referenced by:
- `TimeSurvivor.Voxel.Rendering` - Mesh generation and rendering
- `TimeSurvivor.Voxel.Terrain` - Procedural terrain and streaming
- `TimeSurvivor.Voxel.Physics` - Collision and raycasting
- `TimeSurvivor.Game.*` - Game-specific implementations

---

## Testing

Test assembly: `TimeSurvivor.Voxel.Core.Tests`

Run tests via Unity Test Runner:
1. Window > General > Test Runner
2. Select PlayMode or EditMode
3. Run All Tests

---

## License

Copyright (c) 2025 TimeSurvivor. All rights reserved.
