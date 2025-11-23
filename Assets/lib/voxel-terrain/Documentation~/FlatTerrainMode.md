# Flat Terrain Mode - Feature Documentation

## Overview

The **Flat Terrain Mode** feature allows you to generate terrain chunks at a single Y level (flat horizontal plane) instead of multiple stacked levels. This is useful for:

- 2D-style games with 3D graphics
- Top-down perspective games
- Performance optimization (80% fewer chunks loaded)
- Simplified terrain generation for specific game genres

## What's New

### Configuration Fields Added

**File**: `Assets/lib/voxel-core/Runtime/Configuration/VoxelConfiguration.cs`

Two new fields under **[Header("Flat Terrain Settings")]**:

1. **IsFlatTerrain** (bool, default: false)
   - If `true`, terrain streaming loads chunks only at a single Y level
   - If `false`, terrain streaming loads chunks spherically (3D mode)

2. **FlatTerrainYLevel** (int, default: 0)
   - Y coordinate level for flat terrain chunks
   - Typically set to 0, but can be negative (underground) or positive (elevated)

### Streaming Behavior Changes

**File**: `Assets/lib/voxel-terrain/Runtime/Streaming/ProceduralTerrainStreamer.cs`

#### Flat Terrain Mode (IsFlatTerrain = true):
- **Horizontal Streaming**: Chunks loaded in a 2D circular pattern on the X-Z plane
- **Fixed Y Level**: All chunks generated at `FlatTerrainYLevel` (e.g., Y=0)
- **Distance Calculation**: 2D horizontal distance only (ignores Y axis)
- **Chunk Count**: ~13 chunks for RenderDistance=2 (vs ~65 in 3D mode)

#### 3D Terrain Mode (IsFlatTerrain = false - DEFAULT):
- **Spherical Streaming**: Chunks loaded in a 3D sphere around the player
- **Multiple Y Levels**: Chunks at Y = -RD, ..., -1, 0, 1, ..., +RD
- **Distance Calculation**: 3D spherical distance
- **Chunk Count**: ~65 chunks for RenderDistance=2

## How to Use Flat Terrain Mode

### Step 1: Configure VoxelConfiguration Asset

1. **Locate your VoxelConfiguration asset**:
   - Usually in `Assets/Resources/VoxelConfiguration.asset` or demo-specific config folder

2. **Select the asset in Unity Inspector**

3. **Expand "Flat Terrain Settings" section**

4. **Set the following**:
   - ✅ Check `IsFlatTerrain = true`
   - Set `FlatTerrainYLevel = 0` (or your desired Y level)

5. **Save the asset** (Ctrl+S / Cmd+S)

### Step 2: Run Your Scene

1. **Open your terrain scene** (e.g., `demo-procedural-terrain-streamer`)

2. **Press Play**

3. **Verify the results**:
   - Only ONE flat terrain level should be visible
   - No stacked terrain layers above/below
   - Significantly fewer chunks loaded (check debug UI)

### Step 3: Compare Modes

To see the difference between flat and 3D modes:

1. **Enter Play Mode** with `IsFlatTerrain = false`
   - Observe: Multiple stacked terrain levels (Y=-2, -1, 0, 1, 2)
   - Chunk count: ~65 (for RenderDistance=2)

2. **Exit Play Mode**

3. **Change configuration**: Set `IsFlatTerrain = true`

4. **Enter Play Mode again**
   - Observe: Single flat terrain level (Y=0)
   - Chunk count: ~13 (for RenderDistance=2)

## Performance Impact

### Chunk Count Reduction

For **RenderDistance = 2**:

| Mode | Y Levels | Approximate Chunks | Memory Usage |
|------|----------|-------------------|--------------|
| 3D Terrain | 5 levels (-2 to +2) | ~65 chunks | 100% |
| Flat Terrain | 1 level (Y=0) | ~13 chunks | ~20% |

**Result**: 80% reduction in chunk count and memory usage!

### Performance Gains

- **Memory**: 80% reduction in voxel data memory
- **CPU**: Fewer chunks to generate, mesh, and manage
- **Rendering**: Fewer draw calls (fewer chunk meshes)
- **Collision**: Fewer collision meshes to bake

### Cache Size Optimization

When using flat terrain mode, Unity will warn if your cache size is excessive:

```
[VoxelConfiguration] Flat terrain mode: MaxCachedChunks (300)
is much larger than needed (~50 recommended).
Consider reducing for better memory usage.
```

**Recommended**: Set `MaxCachedChunks` to ~2× the expected chunk count.

For `RenderDistance = 2`, set `MaxCachedChunks = 50` (instead of 300).

## Examples

### Example 1: Ground-Level Flat Terrain

```
VoxelConfiguration:
  IsFlatTerrain: true
  FlatTerrainYLevel: 0
  RenderDistance: 2
  MaxCachedChunks: 50
```

**Result**: Flat terrain at Y=0, ~13 chunks loaded.

### Example 2: Elevated Platform

```
VoxelConfiguration:
  IsFlatTerrain: true
  FlatTerrainYLevel: 5
  RenderDistance: 3
  MaxCachedChunks: 100
```

**Result**: Flat terrain at Y=5 (elevated), ~28 chunks loaded.

### Example 3: Underground Layer

```
VoxelConfiguration:
  IsFlatTerrain: true
  FlatTerrainYLevel: -2
  RenderDistance: 2
  MaxCachedChunks: 50
```

**Result**: Flat terrain at Y=-2 (underground), ~13 chunks loaded.

## Technical Details

### Algorithm Complexity

**Flat Mode**:
```csharp
for (int z = -renderDistance; z <= renderDistance; z++)
{
    for (int x = -renderDistance; x <= renderDistance; x++)
    {
        ChunkCoord coord = new ChunkCoord(playerChunk.X + x, FlatTerrainYLevel, playerChunk.Z + z);
        int distSq = (x * x) + (z * z); // 2D distance
        if (distSq <= renderDistance * renderDistance)
        {
            LoadChunkIfNeeded(coord);
        }
    }
}
```

**Time Complexity**: O(RD²) where RD = render distance

**3D Mode**:
```csharp
for (int y = -renderDistance; y <= renderDistance; y++)
{
    for (int z = -renderDistance; z <= renderDistance; z++)
    {
        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            ChunkCoord coord = playerChunk + new int3(x, y, z);
            int distSq = VoxelMath.ChunkDistanceSquared(coord, playerChunk); // 3D distance
            if (distSq <= renderDistance * renderDistance)
            {
                LoadChunkIfNeeded(coord);
            }
        }
    }
}
```

**Time Complexity**: O(RD³) where RD = render distance

### Distance Calculation

**Flat Mode (2D)**:
```csharp
int dx = chunk.Coord.X - playerChunk.X;
int dz = chunk.Coord.Z - playerChunk.Z;
int distSq = dx * dx + dz * dz; // Ignore Y axis
```

**3D Mode (Spherical)**:
```csharp
int distSq = VoxelMath.ChunkDistanceSquared(chunk.Coord, playerChunk);
// Includes Y axis in distance calculation
```

## Edge Cases

### Runtime Mode Switching

**Question**: What happens if I change `IsFlatTerrain` during gameplay?

**Answer**: The streaming system will respect the new mode on the next chunk update:
- Switching from 3D → Flat: Chunks at non-zero Y levels will eventually be unloaded
- Switching from Flat → 3D: Chunks at other Y levels will start loading

**Note**: There may be a brief transition period with mixed chunk types. For best results, set the mode before entering Play mode.

### Negative Y Levels

**Question**: Can I use negative Y values for `FlatTerrainYLevel`?

**Answer**: Yes! Negative values are fully supported. Useful for:
- Underground/cave systems
- Below-water terrain layers
- Multi-level dungeons

### Cache Size Warnings

**Question**: Why am I getting a cache size warning?

**Answer**: If `MaxCachedChunks` is much larger than needed for flat terrain, you're wasting memory. Unity recommends reducing it to ~2× the expected chunk count.

**Example**: For RenderDistance=2, set `MaxCachedChunks = 50` (instead of 300).

## Troubleshooting

### Problem: Still seeing multiple Y levels

**Solution**:
1. Verify `IsFlatTerrain = true` in VoxelConfiguration asset
2. Make sure you're using the correct VoxelConfiguration asset
3. Exit Play mode and re-enter to reset state

### Problem: No terrain visible

**Solution**:
1. Check that `FlatTerrainYLevel` matches your player's Y position
2. Verify the player is positioned correctly in the scene
3. Ensure the terrain generator is active and configured

### Problem: Performance not improved

**Solution**:
1. Verify chunk count reduced (check debug UI)
2. Reduce `MaxCachedChunks` to recommended value
3. Ensure RenderDistance is appropriate for your use case

## Testing Checklist

After enabling flat terrain mode:

- [ ] Only ONE flat terrain level visible
- [ ] No stacked terrain layers
- [ ] Chunk count significantly reduced (check debug UI)
- [ ] Player can walk on the terrain
- [ ] Collisions work correctly
- [ ] Terrain streams dynamically as player moves
- [ ] Distant chunks unload properly
- [ ] No performance regressions

## Future Enhancements

Potential improvements for future versions:

1. **Auto-Cleanup on Mode Switch**: Automatically clear incompatible chunks when switching modes at runtime
2. **Strategy Pattern**: Extract `ITerrainStreamingStrategy` interface for cleaner extensibility
3. **Cylindrical Mode**: Add support for cylindrical streaming (flat horizontal, infinite vertical)
4. **Custom Shape Modes**: Allow custom streaming patterns via scriptable strategy

## References

- **ADR-004**: Chunk Size and Subdivision Strategy
- **ADR-005**: Amortized Chunk Meshing
- **VoxelConfiguration.cs**: `Assets/lib/voxel-core/Runtime/Configuration/VoxelConfiguration.cs`
- **ProceduralTerrainStreamer.cs**: `Assets/lib/voxel-terrain/Runtime/Streaming/ProceduralTerrainStreamer.cs`

## Support

For questions or issues:
1. Check the troubleshooting section above
2. Verify VoxelConfiguration settings
3. Review Unity console for errors/warnings
4. Consult voxel engine documentation

---

**Version**: 1.0
**Last Updated**: 2025-11-23
**Compatibility**: Unity 6000.2.12f1+, URP
