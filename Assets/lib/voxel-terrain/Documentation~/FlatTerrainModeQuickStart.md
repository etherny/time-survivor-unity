# Flat Terrain Mode - Quick Start Guide

## 5-Minute Setup

This guide gets you up and running with flat terrain mode in under 5 minutes.

---

## Step 1: Find Your VoxelConfiguration Asset (30 seconds)

1. Open your Unity project
2. Navigate to `Assets/Resources/` or your demo's config folder
3. Find the `VoxelConfiguration.asset` file
4. Click to select it

**Example Locations**:
- `Assets/Resources/VoxelConfiguration.asset`
- `Assets/demos/demo-procedural-terrain-streamer/Config/VoxelConfig.asset`
- `Assets/demos/demo-terrain-collision/Config/TerrainCollisionDemoConfig.asset`

---

## Step 2: Enable Flat Terrain Mode (30 seconds)

**In the Unity Inspector**:

1. Expand **"Flat Terrain Settings"** section
2. Check the box: `✅ IsFlatTerrain = true`
3. Set `FlatTerrainYLevel = 0`
4. **Save** (Ctrl+S / Cmd+S)

**Screenshot of Inspector**:
```
┌─────────────────────────────────────┐
│ Flat Terrain Settings              │
├─────────────────────────────────────┤
│ ✅ Is Flat Terrain                 │
│ Flat Terrain Y Level: 0            │
└─────────────────────────────────────┘
```

---

## Step 3: Optimize Cache Size (Optional - 30 seconds)

If you get a warning about excessive cache size:

1. Scroll to **"Streaming & LOD"** section
2. Set `MaxCachedChunks = 50` (instead of 300)
3. **Save**

**Why?** Flat terrain uses 80% fewer chunks, so less cache needed.

---

## Step 4: Test the Feature (2 minutes)

### Open a Terrain Demo Scene

**Recommended**: Use `demo-procedural-terrain-streamer`

```
Assets/demos/demo-procedural-terrain-streamer/Scenes/DemoScene.unity
```

**Or use any scene with**:
- ProceduralTerrainStreamer component
- VoxelConfiguration assigned

### Press Play and Observe

**BEFORE (IsFlatTerrain = false)**:
```
┌─────────────────────────────────────┐
│   Y=2  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓    │ ← Stacked
│   Y=1  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓    │   layers
│   Y=0  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓    │
│   Y=-1 ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓    │
│   Y=-2 ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓    │
└─────────────────────────────────────┘
Chunks Loaded: ~65
```

**AFTER (IsFlatTerrain = true)**:
```
┌─────────────────────────────────────┐
│   Y=0  ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓    │ ← Single
│        (flat terrain)              │   flat layer
└─────────────────────────────────────┘
Chunks Loaded: ~13
```

---

## Validation Checklist

After pressing Play, verify:

- ✅ **Only ONE horizontal plane** of terrain visible
- ✅ **No stacked layers** above or below
- ✅ **Chunk count significantly reduced** (check debug UI)
  - Expected: ~13 chunks (vs ~65 in 3D mode)
- ✅ **Player can walk** on the flat terrain
- ✅ **Collisions work** properly
- ✅ **Terrain streams** as player moves

---

## Compare Modes Side-by-Side

### Test 1: 3D Mode (Default)

1. **Set**: `IsFlatTerrain = false`
2. **Press Play**
3. **Observe**:
   - Multiple stacked terrain levels
   - Chunks at Y = -2, -1, 0, 1, 2
   - ~65 chunks loaded
4. **Exit Play Mode**

### Test 2: Flat Mode

1. **Set**: `IsFlatTerrain = true`
2. **Press Play**
3. **Observe**:
   - Single flat terrain level
   - Chunks only at Y = 0
   - ~13 chunks loaded
4. **Compare**: Notice 80% reduction in chunks!

---

## Common Use Cases

### Use Case 1: Top-Down Game

```
IsFlatTerrain: true
FlatTerrainYLevel: 0
RenderDistance: 3
MaxCachedChunks: 100
```

**Result**: Large flat world for top-down gameplay.

### Use Case 2: Platform Game (Elevated)

```
IsFlatTerrain: true
FlatTerrainYLevel: 10
RenderDistance: 2
MaxCachedChunks: 50
```

**Result**: Elevated platform at Y=10.

### Use Case 3: Cave/Underground

```
IsFlatTerrain: true
FlatTerrainYLevel: -5
RenderDistance: 2
MaxCachedChunks: 50
```

**Result**: Underground layer at Y=-5.

---

## Performance Comparison

| Metric | 3D Mode | Flat Mode | Improvement |
|--------|---------|-----------|-------------|
| Chunks Loaded | ~65 | ~13 | 80% reduction |
| Memory Usage | 100% | ~20% | 5× less |
| CPU (Generation) | High | Low | 5× faster |
| CPU (Meshing) | High | Low | 5× faster |
| Draw Calls | ~65 | ~13 | 80% reduction |

**Render Distance**: 2 chunks (typical)

---

## Troubleshooting

### ❌ Problem: Still seeing multiple Y levels

**Solution**:
```
1. Verify IsFlatTerrain = true (check the checkbox!)
2. Exit Play mode and re-enter
3. Ensure using correct VoxelConfiguration asset
```

### ❌ Problem: No terrain visible

**Solution**:
```
1. Check FlatTerrainYLevel matches player Y position
2. Try FlatTerrainYLevel = 0 (most common)
3. Verify ProceduralTerrainStreamer is active
```

### ❌ Problem: Getting cache size warning

**Solution**:
```
1. Set MaxCachedChunks = 50 (for RenderDistance=2)
2. Formula: MaxCachedChunks = 2 × expected chunks
3. Expected chunks ≈ (RenderDistance × 2 + 1)²
```

---

## Next Steps

**Congratulations!** You've successfully enabled flat terrain mode.

**Learn More**:
- Read full documentation: `FlatTerrainMode.md`
- Experiment with different `FlatTerrainYLevel` values
- Optimize `MaxCachedChunks` for your RenderDistance
- Try different RenderDistance values (2, 3, 4)

**Advanced**:
- Implement custom terrain generators for flat mode
- Create multi-level dungeons with different Y levels
- Combine with procedural generation for infinite worlds

---

## Summary

**What You Learned**:
1. ✅ How to enable flat terrain mode (2 settings)
2. ✅ Performance benefits (80% fewer chunks)
3. ✅ How to test and validate the feature
4. ✅ Common use cases and configurations

**Key Settings**:
```
IsFlatTerrain = true
FlatTerrainYLevel = 0
MaxCachedChunks = 50
```

**Result**: Fast, efficient flat terrain streaming!

---

**Time to Complete**: 5 minutes
**Difficulty**: Beginner
**Unity Version**: 6000.2.12f1+
