# Minecraft Terrain Configurations Reference

This document provides a quick reference for the three pre-configured terrain sizes.

## Configuration Files

All configuration files are ScriptableObjects located in:
`Assets/demos/demo-minecraft-terrain/Configurations/`

---

## Small Configuration (10x10x8)

**File**: `Small_10x10x8.asset`

**Use Case**: Quick testing, prototyping, low-end hardware

**Parameters**:
```
World Size X: 10 chunks
World Size Y: 8 chunks
World Size Z: 10 chunks

Total Chunks: 10 × 8 × 10 = 800 chunks
Approximate Generation Time: 1-2 seconds
Memory Usage: ~5-10 MB

Base Terrain Height: 4 voxels
Terrain Variation: 2 voxels (height range: 2-6 voxels)
Heightmap Frequency: 0.02
Heightmap Octaves: 4

Grass Layer Thickness: 1 voxel
Dirt Layer Thickness: 3 voxels

Generate Water: Yes
Water Level: 3 voxels
```

**Expected Terrain**:
- Small plateau-based world
- Height range: 2-6 voxels (Y = 128-384 in world space)
- Water fills areas below Y = 192
- Grass layer on top, 3 voxels of dirt, then stone

---

## Medium Configuration (20x20x8)

**File**: `Medium_20x20x8.asset`

**Use Case**: Standard gameplay, medium-sized worlds

**Parameters**:
```
World Size X: 20 chunks
World Size Y: 8 chunks
World Size Z: 20 chunks

Total Chunks: 20 × 8 × 20 = 3,200 chunks
Approximate Generation Time: 3-5 seconds
Memory Usage: ~20-30 MB

Base Terrain Height: 4 voxels
Terrain Variation: 2 voxels (height range: 2-6 voxels)
Heightmap Frequency: 0.02
Heightmap Octaves: 4

Grass Layer Thickness: 1 voxel
Dirt Layer Thickness: 3 voxels

Generate Water: Yes
Water Level: 3 voxels
```

**Expected Terrain**:
- Medium-sized plateau world
- Same height range as Small (2-6 voxels)
- More variation in plateau shapes and sizes
- Larger water bodies

---

## Large Configuration (50x50x8)

**File**: `Large_50x50x8.asset`

**Use Case**: Exploration, open-world gameplay, high-end hardware

**Parameters**:
```
World Size X: 50 chunks
World Size Y: 8 chunks
World Size Z: 50 chunks

Total Chunks: 50 × 8 × 50 = 20,000 chunks
Approximate Generation Time: 10-30 seconds (hardware dependent)
Memory Usage: ~100-150 MB

Base Terrain Height: 4 voxels
Terrain Variation: 3 voxels (height range: 1-7 voxels)
Heightmap Frequency: 0.02
Heightmap Octaves: 4

Grass Layer Thickness: 1 voxel
Dirt Layer Thickness: 3 voxels

Generate Water: Yes
Water Level: 3 voxels
```

**Expected Terrain**:
- Very large plateau world
- Greater height variation (1-7 voxels)
- More dramatic plateaus and valleys
- Large lakes and water bodies

**Performance Note**: This configuration may cause frame drops during generation on slower hardware. Consider increasing `Chunks Per Frame` to 4 or 8 for faster generation.

---

## Parameter Explanations

### World Size (X, Y, Z)

- **Definition**: Number of chunks along each axis
- **Chunk Size**: 64 × 64 × 64 voxels (from VoxelConfiguration)
- **Total World Size**: WorldSizeX × 64 = world width in voxels

Example (Small):
- World Size X = 10 chunks = 10 × 64 = 640 voxels wide
- World Size Y = 8 chunks = 8 × 64 = 512 voxels tall
- World Size Z = 10 chunks = 10 × 64 = 640 voxels deep

### Base Terrain Height

- **Definition**: The average/base height of terrain in voxels (before variation)
- **Range**: 0 to (WorldSizeY × 64) - TerrainVariation
- **Example**: Base = 4 means terrain centers around Y = 256 (4 × 64)

### Terrain Variation

- **Definition**: How much terrain height varies above/below base height
- **Range**: ±TerrainVariation voxels
- **Example**: Base = 4, Variation = 2 → Height range = 2 to 6 voxels

### Heightmap Frequency

- **Definition**: Controls the scale of terrain features (higher = smaller features)
- **Typical Range**: 0.005 to 0.05
- **Effect**:
  - Low (0.01): Large, smooth plateaus
  - High (0.05): Small, frequent height changes

### Heightmap Octaves

- **Definition**: Number of noise layers combined for detail
- **Typical Range**: 1 to 8
- **Effect**:
  - Low (1-2): Smooth, simple terrain
  - High (6-8): Detailed, complex terrain with fine features

### Grass/Dirt Layer Thickness

- **Definition**: Number of voxels for each surface layer
- **Grass**: Top-most layer (green)
- **Dirt**: Below grass (brown)
- **Stone**: Below dirt (gray)

Example (default):
```
Y=6: Grass (1 voxel)
Y=5: Dirt (3 voxels)
Y=4: Dirt
Y=3: Dirt
Y=2: Stone (all remaining voxels below)
Y=1: Stone
```

### Water Level

- **Definition**: Height at which water generates (in voxels)
- **Rule**: Water fills all air voxels at or below this level
- **Typical Range**: 2 to 5 voxels
- **Effect**: Creates lakes, rivers, and ocean-like areas

---

## Performance Comparison

| Configuration | Chunks | Generation Time | Memory   | FPS Impact |
|---------------|--------|-----------------|----------|------------|
| Small         | 800    | 1-2s            | ~5-10 MB | Minimal    |
| Medium        | 3,200  | 3-5s            | ~20-30 MB| Low        |
| Large         | 20,000 | 10-30s          | ~100-150 MB| Medium   |

**Notes**:
- Generation time varies based on CPU performance
- Memory usage includes mesh data and voxel data
- FPS impact is during generation only (post-generation FPS is stable)

---

## Creating Custom Configurations

To create your own configuration:

1. Right-click in the Configurations folder
2. Select `Create` → `TimeSurvivor` → `Minecraft Terrain Configuration`
3. Name it descriptively (e.g., `Custom_100x16x100`)
4. Configure parameters based on your needs

### Recommended Ratios

**For balanced terrain**:
- WorldSizeY = WorldSizeX / 5 to WorldSizeX / 10
- TerrainVariation = BaseTerrainHeight / 2
- WaterLevel = BaseTerrainHeight - 1

**For mountainous terrain**:
- Increase TerrainVariation (e.g., 5-8)
- Increase HeightmapOctaves (e.g., 6-8)
- Lower WaterLevel (e.g., 1-2)

**For flat terrain**:
- Decrease TerrainVariation (e.g., 0-1)
- Decrease HeightmapOctaves (e.g., 1-2)
- Higher WaterLevel (e.g., 4-5)

---

## Troubleshooting

### Terrain too flat

- Increase `Terrain Variation`
- Increase `Heightmap Octaves`

### Terrain too spiky

- Decrease `Terrain Variation`
- Increase `Heightmap Frequency` (smooths features)

### Too much/little water

- Adjust `Water Level` up or down
- Ensure `Water Level` < `Base Terrain Height`

### Generation too slow

- Reduce world size (WorldSizeX, WorldSizeZ)
- Increase `Chunks Per Frame` in MinecraftTerrainGenerator

### Out of memory errors

- Reduce world size
- Reduce WorldSizeY (height)
- Close other applications

---

## Future Configuration Ideas

- **Biome-based configurations**: Different parameters for desert, forest, snow
- **Seed-based variations**: Same world size, different heightmap seeds
- **Multi-layer configurations**: Different layer thicknesses per biome
- **Dynamic water**: Ocean vs. river water levels

Refer to `UNITY_SETUP_GUIDE.md` for setup instructions and `README.md` for implementation details.
