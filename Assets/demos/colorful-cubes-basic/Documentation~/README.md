# Colorful Cubes Basic Demo

## Overview

The **Colorful Cubes Basic Demo** is a showcase demonstration of the TimeSurvivor Voxel Engine that displays vibrant, colorful voxel patterns. This demo highlights the core capabilities of the voxel engine including chunk management, procedural generation, and runtime pattern switching.

### Features

- **5 Unique Color Patterns**: Rainbow layers, 3D checkerboard, rainbow grid, gradient sphere, and random colors
- **Multiple Color Palettes**: Default, rainbow, pastel, and neon color schemes
- **Runtime Controls**: Change patterns and view distance during gameplay
- **Performance Monitoring**: Real-time display of chunk count, voxel count, and FPS
- **Auto-rotating Camera**: Orbital camera for 360-degree viewing
- **Production-ready Code**: Clean, well-documented, SOLID-compliant implementation

### Purpose

This demo serves as:
1. **Visual Showcase**: Demonstrates the voxel engine's rendering capabilities
2. **Learning Resource**: Example implementation of `IVoxelGenerator` interface
3. **Testing Tool**: Performance baseline for voxel engine optimization
4. **Development Template**: Starting point for custom voxel generators

---

## Quick Start

### Prerequisites

- Unity 6000.2.12f1 or later
- TimeSurvivor Voxel Engine packages:
  - `voxel-core`
  - `voxel-terrain`
  - `voxel-rendering`

### Setup Instructions

#### 1. Create a New Scene

1. Open Unity Editor
2. Create a new scene: `File > New Scene`
3. Save it as `ColorfulCubesDemo.unity`

#### 2. Setup Demo GameObject

1. Create an empty GameObject: `GameObject > Create Empty`
2. Rename it to `ColorfulCubesDemo`
3. Add the `ColorfulCubesDemo` component:
   - In the Inspector, click `Add Component`
   - Search for "Colorful Cubes Demo"
   - Select `TimeSurvivor > Demos > Colorful Cubes Demo`

#### 3. Create VoxelConfiguration Asset

1. In the Project window, right-click in the `Assets/demos/colorful-cubes-basic/Resources/` folder
2. Select `Create > TimeSurvivor > Voxel Configuration`
3. Name it `ColorfulCubesConfig`
4. Configure the settings:
   - **Chunk Size**: 16 (recommended)
   - **Macro Voxel Size**: 0.2
   - **Render Distance**: 8 (not used by demo, use demo's View Distance instead)
   - **Use Burst Compilation**: True
   - **Use Job System**: True

#### 4. Create Voxel Material

1. Create a new material: `Assets > Create > Material`
2. Name it `ColorfulVoxelMaterial`
3. Configure the material:
   - **Shader**: Standard (or your custom voxel shader)
   - **Rendering Mode**: Opaque
   - **Albedo**: White (colors are set via palette)

#### 5. Assign References

1. Select the `ColorfulCubesDemo` GameObject
2. In the Inspector, assign:
   - **Configuration**: Drag `ColorfulCubesConfig` asset
   - **Chunk Material**: Drag `ColorfulVoxelMaterial`

#### 6. Configure Demo Settings

Adjust the demo settings in the Inspector:

**Pattern Settings:**
- **Current Pattern**: RainbowLayers (default)
- **Random Seed**: 12345 (for RandomColors pattern)

**World Settings:**
- **View Distance**: 4 (creates 9x9x3 grid = 243 chunks)
- **World Center**: (0, 0, 0)

**Camera Controls:**
- **Auto Rotate Camera**: True
- **Rotation Speed**: 10 degrees/second
- **Camera Distance**: 40 units
- **Camera Height**: 20 units

**Performance Display:**
- **Show Performance Stats**: True

#### 7. Press Play!

Press the Play button and watch the colorful voxel world generate.

---

## Color Patterns

### 1. Rainbow Layers

**Description**: Horizontal layers cycling through voxel types based on Y coordinate.

**Algorithm**:
```csharp
int index = Mathf.Abs(worldY) % 6;
return RainbowTypes[index];
```

**Visual Effect**: Horizontal stripes of different colors stacked vertically.

**Best For**: Understanding basic procedural generation, testing vertical chunk alignment.

---

### 2. Checkerboard 3D

**Description**: 3D checkerboard pattern based on `(x+y+z) % 2`.

**Algorithm**:
```csharp
bool isEven = ((worldX + worldY + worldZ) % 2) == 0;
return isEven ? VoxelType.Stone : VoxelType.Sand;
```

**Visual Effect**: Alternating cubes in 3D space, creates a classic checkerboard in all dimensions.

**Best For**: Testing neighbor detection, visualizing 3D grid structure.

---

### 3. Rainbow Grid

**Description**: Vertical columns with different colors based on XZ position.

**Algorithm**:
```csharp
int columnIndex = (Mathf.Abs(worldX) + Mathf.Abs(worldZ)) % 6;
return RainbowTypes[columnIndex];
```

**Visual Effect**: Colored columns when viewed from above, creates a grid pattern.

**Best For**: Testing horizontal chunk boundaries, visualizing XZ plane.

---

### 4. Gradient Sphere

**Description**: Radial gradient from center based on distance.

**Algorithm**:
```csharp
int distance = (int)math.sqrt(dx*dx + dy*dy + dz*dz);
int colorIndex = distance % 6;
return RainbowTypes[colorIndex];
```

**Visual Effect**: Concentric spherical layers of different colors emanating from world center.

**Best For**: Testing radial generation, visualizing distance-based algorithms.

---

### 5. Random Colors

**Description**: Controlled random distribution using deterministic hash.

**Algorithm**:
```csharp
int hash = HashPosition(worldX, worldY, worldZ, seed);
int colorIndex = Mathf.Abs(hash) % 6;
return RainbowTypes[colorIndex];
```

**Visual Effect**: Seemingly random distribution of colors, but consistent across sessions.

**Best For**: Testing randomization, creating organic-looking structures.

---

## Color Palettes

The demo includes 4 preset color palettes via `RainbowPaletteGenerator`:

### Default Palette (Natural)
- Grass: Bright green (#33CC33)
- Dirt: Brown (#996633)
- Stone: Gray (#808080)
- Sand: Yellow (#FFEE66)
- Water: Blue (#3377FF)
- Wood: Dark brown (#663300)
- Leaves: Light green (#66FF66)

### Rainbow Palette (Vibrant)
- Grass: Red (#FF0000)
- Dirt: Orange (#FF8800)
- Stone: Yellow (#FFFF00)
- Sand: Green (#00FF00)
- Water: Cyan (#00FFFF)
- Wood: Blue (#0000FF)
- Leaves: Purple (#8800FF)

### Pastel Palette (Soft)
- Grass: Pastel pink (#FFB3BA)
- Dirt: Pastel orange (#FFDFBA)
- Stone: Pastel yellow (#FFFFBA)
- Sand: Pastel green (#BAFFC9)
- Water: Pastel blue (#BAE1FF)
- Wood: Pastel purple (#C9B3FF)
- Leaves: Pastel magenta (#FFB3F0)

### Neon Palette (Cyberpunk)
- Grass: Neon pink (#FF006E)
- Dirt: Neon orange (#FB5607)
- Stone: Neon yellow (#FFBE0B)
- Sand: Neon purple (#8338EC)
- Water: Neon blue (#3A86FF)
- Wood: Neon cyan (#06FFA5)
- Leaves: Neon magenta (#FF006E)

**Usage**:
```csharp
Color[] palette = RainbowPaletteGenerator.GetRainbowPalette();
RainbowPaletteGenerator.ApplyPaletteToMaterial(material, palette);
```

---

## Runtime Controls

### Changing Patterns (via Code)

```csharp
var demo = GetComponent<ColorfulCubesDemo>();
demo.SetPattern(ColorPattern.Checkerboard3D);
```

### Changing View Distance (via Code)

```csharp
var demo = GetComponent<ColorfulCubesDemo>();
demo.SetViewDistance(6); // Load more chunks
```

### Inspector Controls

All demo settings can be changed in the Inspector during Play mode:
- Changing `Current Pattern` will regenerate the world
- Changing `View Distance` will reload chunks
- Changing camera settings will update immediately
- Changing `Random Seed` requires pattern switch to take effect

---

## Performance Notes

### Expected Performance

With default settings (ChunkSize=16, ViewDistance=4):
- **Chunks Loaded**: 243 (9x9x3 grid)
- **Total Voxels**: 989,184 (243 chunks × 4096 voxels)
- **Memory Usage**: ~1 MB for voxel data
- **Target FPS**: 60+ FPS on modern hardware

### Optimization Tips

1. **Reduce View Distance**: Lower value = fewer chunks = better performance
2. **Enable Amortized Meshing**: Spreads mesh generation across frames
3. **Use Burst Compilation**: Significant speedup for generation and meshing
4. **Adjust Max Chunks Per Frame**: Balance between load time and frame drops
5. **Profile with Unity Profiler**: Identify bottlenecks (generation, meshing, rendering)

### Performance Comparison

| View Distance | Chunks | Voxels    | Est. Memory | Est. FPS (1080p) |
|---------------|--------|-----------|-------------|------------------|
| 2             | 75     | 307,200   | 300 KB      | 120+ FPS         |
| 4             | 243    | 989,184   | 966 KB      | 60+ FPS          |
| 6             | 507    | 2,064,384 | 2.0 MB      | 30-60 FPS        |
| 8             | 867    | 3,530,752 | 3.4 MB      | 15-30 FPS        |

*Note: Actual performance depends on hardware, shader complexity, and other scene objects.*

---

## Architecture

### Component Diagram

```
ColorfulCubesDemo (MonoBehaviour)
├── CustomChunkManager
│   ├── ChunkManager (base)
│   └── ColorfulTerrainGenerator (IVoxelGenerator)
├── RainbowPaletteGenerator (static utility)
└── VoxelConfiguration (ScriptableObject)
```

### Class Responsibilities

**ColorfulCubesDemo**:
- MonoBehaviour lifecycle management
- Camera controls (orbital rotation)
- Runtime pattern switching
- Performance statistics display
- User input handling (Inspector changes)

**ColorfulTerrainGenerator**:
- Implements `IVoxelGenerator` interface
- Pattern-based voxel generation
- Thread-safe for Unity Jobs
- Deterministic randomization

**RainbowPaletteGenerator**:
- Static utility class
- Color palette generation
- Hex color conversion
- Material palette application

**CustomChunkManager**:
- Extends base `ChunkManager`
- Injects custom generator
- Overrides chunk loading logic
- Maintains chunk lifecycle

---

## Extension Ideas

### 1. Add UI Controls

Create a Canvas with buttons to switch patterns at runtime:

```csharp
public void OnButtonClick_RainbowLayers()
{
    FindObjectOfType<ColorfulCubesDemo>().SetPattern(ColorPattern.RainbowLayers);
}
```

### 2. Implement Player Movement

Add a character controller to walk through the voxel world:

```csharp
// Disable auto-rotate camera
demo._autoRotateCamera = false;

// Attach camera to player
Camera.main.transform.SetParent(playerTransform);
```

### 3. Create Custom Patterns

Extend `ColorfulTerrainGenerator` with your own patterns:

```csharp
case ColorPattern.MyCustomPattern:
    return GetCustomPatternVoxel(worldX, worldY, worldZ);
```

### 4. Add Voxel Destruction

Implement raycasting and voxel modification:

```csharp
if (Input.GetMouseButtonDown(0))
{
    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    // Raycast against voxel world
    // Remove voxel at hit position
    // Mark chunk as dirty for remeshing
}
```

### 5. Implement Color Palette Switching

Add runtime palette switching with UI:

```csharp
public void ApplyRainbowPalette()
{
    var palette = RainbowPaletteGenerator.GetRainbowPalette();
    RainbowPaletteGenerator.ApplyPaletteToMaterial(_chunkMaterial, palette);
}
```

### 6. Create Animated Patterns

Make patterns change over time:

```csharp
private float _animationTime = 0f;

void Update()
{
    _animationTime += Time.deltaTime;
    // Regenerate chunks with time-based offset
}
```

---

## Troubleshooting

### Problem: No chunks are visible

**Solution**:
1. Check that VoxelConfiguration and Material are assigned
2. Verify camera is positioned correctly (not inside chunks)
3. Check Unity Console for errors
4. Ensure chunk material shader is compatible

### Problem: Poor performance / Low FPS

**Solution**:
1. Reduce View Distance in Inspector
2. Enable "Use Amortized Meshing" in VoxelConfiguration
3. Enable "Use Burst Compilation" in VoxelConfiguration
4. Profile with Unity Profiler to identify bottlenecks
5. Reduce Chunk Size (try 8 instead of 16)

### Problem: Patterns don't change in Inspector

**Solution**:
1. OnValidate only works in Play mode
2. Try calling SetPattern() from code instead
3. Check that the demo component is enabled

### Problem: Chunks have wrong colors

**Solution**:
1. Verify material has correct shader
2. Check that color palette is applied to material
3. Ensure material's albedo is white (not tinted)

### Problem: Memory errors / Out of memory

**Solution**:
1. Reduce View Distance significantly
2. Lower Max Cached Chunks in VoxelConfiguration
3. Decrease Chunk Size to 8
4. Check for memory leaks (chunks not being disposed)

---

## Code Examples

### Basic Usage

```csharp
using TimeSurvivor.Demos.ColorfulCubes;
using Unity.Mathematics;

// Create a generator
var generator = new ColorfulTerrainGenerator(
    ColorPattern.RainbowLayers,
    seed: 12345,
    centerPoint: int3.zero
);

// Generate a chunk
ChunkCoord coord = new ChunkCoord(0, 0, 0);
var voxelData = generator.Generate(coord, chunkSize: 16, Allocator.Temp);

// Query a single voxel
VoxelType voxel = generator.GetVoxelAt(10, 5, 8);

// Cleanup
voxelData.Dispose();
```

### Custom Pattern Implementation

```csharp
public class MyCustomGenerator : ColorfulTerrainGenerator
{
    public MyCustomGenerator() : base(ColorPattern.RainbowLayers) { }

    protected VoxelType GetWavePatternVoxel(int x, int y, int z)
    {
        // Create a sine wave pattern
        float wave = Mathf.Sin(x * 0.1f + y * 0.1f) * 3f;
        int waveY = (int)wave;

        return (y == waveY) ? VoxelType.Grass : VoxelType.Air;
    }
}
```

### Palette Switching

```csharp
using TimeSurvivor.Demos.ColorfulCubes;

public class PaletteSwitcher : MonoBehaviour
{
    [SerializeField] private Material _chunkMaterial;

    public void ApplyDefaultPalette()
    {
        var palette = RainbowPaletteGenerator.GetDefaultPalette();
        RainbowPaletteGenerator.ApplyPaletteToMaterial(_chunkMaterial, palette);
    }

    public void ApplyRainbowPalette()
    {
        var palette = RainbowPaletteGenerator.GetRainbowPalette();
        RainbowPaletteGenerator.ApplyPaletteToMaterial(_chunkMaterial, palette);
    }

    public void ApplyPastelPalette()
    {
        var palette = RainbowPaletteGenerator.GetPastelPalette();
        RainbowPaletteGenerator.ApplyPaletteToMaterial(_chunkMaterial, palette);
    }

    public void ApplyNeonPalette()
    {
        var palette = RainbowPaletteGenerator.GetNeonPalette();
        RainbowPaletteGenerator.ApplyPaletteToMaterial(_chunkMaterial, palette);
    }
}
```

---

## API Reference

### ColorfulCubesDemo

**Public Methods**:
- `void SetPattern(ColorPattern newPattern)`: Change the current pattern and regenerate world
- `void SetViewDistance(int distance)`: Change view distance and reload chunks

**Serialized Fields**:
- `VoxelConfiguration _configuration`: Engine configuration
- `Material _chunkMaterial`: Rendering material
- `ColorPattern _currentPattern`: Current color pattern
- `int _randomSeed`: Seed for random generation
- `int _viewDistance`: Chunks to load (1-8)
- `Vector3Int _worldCenter`: World center position
- `bool _autoRotateCamera`: Auto-rotate camera
- `float _rotationSpeed`: Rotation speed (deg/s)
- `float _cameraDistance`: Camera orbital distance
- `float _cameraHeight`: Camera height above center
- `bool _showPerformanceStats`: Show GUI stats

### ColorfulTerrainGenerator

**Constructor**:
```csharp
ColorfulTerrainGenerator(ColorPattern pattern, int seed = 12345, int3 centerPoint = default)
```

**Public Methods**:
- `NativeArray<VoxelType> Generate(ChunkCoord coord, int chunkSize, Allocator allocator)`
- `VoxelType GetVoxelAt(int worldX, int worldY, int worldZ)`

### RainbowPaletteGenerator

**Static Methods**:
- `Color[] GetDefaultPalette()`: Natural colors
- `Color[] GetRainbowPalette()`: Vibrant rainbow colors
- `Color[] GetPastelPalette()`: Soft pastel colors
- `Color[] GetNeonPalette()`: Neon cyberpunk colors
- `void ApplyPaletteToMaterial(Material material, Color[] palette)`: Apply palette to material
- `Color GetRandomColorFromPalette(Color[] palette, bool excludeAir = true)`: Get random color

---

## License

Part of the TimeSurvivor Voxel Engine.
Copyright 2025 TimeSurvivor Team.

---

## Support

For issues, questions, or contributions:
- GitHub Issues: [TimeSurvivorGame/issues](https://github.com/etherny/TimeSurvivorGame/issues)
- Documentation: See `Assets/lib/voxel-core/Documentation~/`
- Unity Forums: Search "TimeSurvivor Voxel Engine"

---

## Changelog

### Version 1.0.0 (2025-11-21)
- Initial release
- 5 color patterns (Rainbow Layers, Checkerboard 3D, Rainbow Grid, Gradient Sphere, Random Colors)
- 4 color palettes (Default, Rainbow, Pastel, Neon)
- Runtime controls for pattern and view distance
- Performance monitoring GUI
- Auto-rotating orbital camera
- Complete documentation and examples
