# Unity Editor Setup Guide - Minecraft Terrain Demo

This guide provides step-by-step instructions to configure the Minecraft Terrain demonstration in Unity Editor.

## Prerequisites

- Unity Editor 6000.2.12f1 (or compatible version)
- Universal Render Pipeline (URP) installed
- All demo scripts already present in `Assets/demos/demo-minecraft-terrain/Scripts/`

## Setup Time

Estimated setup time: **5-10 minutes**

---

## Part 1: Create VoxelConfiguration ScriptableObject (Core Library)

### Step 1.1: Navigate to voxel-core Configurations

1. In Unity Editor, open the **Project** window
2. Navigate to: `Assets/lib/voxel-core/Configurations/`
3. If the folder doesn't exist, create it: Right-click `voxel-core` → `Create` → `Folder` → Name it `Configurations`

### Step 1.2: Create DefaultVoxelConfiguration

1. Right-click in `Assets/lib/voxel-core/Configurations/`
2. Select `Create` → `TimeSurvivor` → `Voxel Configuration`
3. Name the asset: **`DefaultVoxelConfiguration`**

### Step 1.3: Configure DefaultVoxelConfiguration

Select the newly created asset and configure in the **Inspector**:

```
Chunk Size: 64
Macro Voxel Size: 0.2
Seed: 12345
Noise Frequency: 0.02
Noise Octaves: 4
Lacunarity: 2.0
Persistence: 0.5
```

**Save**: `Ctrl+S` (Windows/Linux) or `Cmd+S` (Mac)

---

## Part 2: Create Minecraft Terrain Configuration ScriptableObjects

### Step 2.1: Navigate to Demo Configurations

1. In the **Project** window, navigate to: `Assets/demos/demo-minecraft-terrain/Configurations/`

### Step 2.2: Create Small Configuration (10x10x8)

1. Right-click in the Configurations folder
2. Select `Create` → `TimeSurvivor` → `Minecraft Terrain Configuration`
3. Name the asset: **`Small_10x10x8`**

Configure in the **Inspector**:

```
World Size X: 10
World Size Y: 8
World Size Z: 10
Base Terrain Height: 4
Terrain Variation: 2
Heightmap Frequency: 0.02
Heightmap Octaves: 4
Grass Layer Thickness: 1
Dirt Layer Thickness: 3
Generate Water: ✓ (checked)
Water Level: 3
```

**Save**: `Ctrl+S` / `Cmd+S`

### Step 2.3: Create Medium Configuration (20x20x8)

1. Right-click in the Configurations folder
2. Select `Create` → `TimeSurvivor` → `Minecraft Terrain Configuration`
3. Name the asset: **`Medium_20x20x8`**

Configure in the **Inspector**:

```
World Size X: 20
World Size Y: 8
World Size Z: 20
Base Terrain Height: 4
Terrain Variation: 2
Heightmap Frequency: 0.02
Heightmap Octaves: 4
Grass Layer Thickness: 1
Dirt Layer Thickness: 3
Generate Water: ✓ (checked)
Water Level: 3
```

**Save**: `Ctrl+S` / `Cmd+S`

### Step 2.4: Create Large Configuration (50x50x8)

1. Right-click in the Configurations folder
2. Select `Create` → `TimeSurvivor` → `Minecraft Terrain Configuration`
3. Name the asset: **`Large_50x50x8`**

Configure in the **Inspector**:

```
World Size X: 50
World Size Y: 8
World Size Z: 50
Base Terrain Height: 4
Terrain Variation: 3
Heightmap Frequency: 0.02
Heightmap Octaves: 4
Grass Layer Thickness: 1
Dirt Layer Thickness: 3
Generate Water: ✓ (checked)
Water Level: 3
```

**Save**: `Ctrl+S` / `Cmd+S`

---

## Part 3: Create or Locate VoxelTerrain Material

### Option A: Use Existing Material (Recommended)

1. In the **Project** window, search for: `VoxelTerrain` (material)
2. If found in `Assets/demos/demo-procedural-terrain-job/Materials/`, you can:
   - **Either**: Copy it to `Assets/demos/demo-minecraft-terrain/Materials/`
   - **Or**: Use the existing material directly (cross-demo reference)

### Option B: Create New Material

If no VoxelTerrain material exists:

1. Navigate to: `Assets/demos/demo-minecraft-terrain/Materials/`
2. Right-click → `Create` → `Material`
3. Name it: **`VoxelTerrain`**
4. In the **Inspector**:
   - **Shader**: Search for and select `Shader Graphs/VoxelTerrainShader` (URP compatible)
   - **Base Color**: `RGBA(1, 1, 1, 1)` (white)
5. **Save**: `Ctrl+S` / `Cmd+S`

---

## Part 4: Create the Demo Scene

### Step 4.1: Create New Scene

1. In Unity Editor menu: `File` → `New Scene`
2. Select **Basic (URP)** template
3. Click **Create**

### Step 4.2: Save the Scene

1. `File` → `Save As...`
2. Navigate to: `Assets/demos/demo-minecraft-terrain/Scenes/`
3. Name it: **`MinecraftTerrainDemoScene`**
4. Click **Save**

### Step 4.3: Adjust Main Camera

Select **Main Camera** in the Hierarchy and configure:

```
Position:
  X: 64
  Y: 80
  Z: 30

Rotation:
  X: 30
  Y: 0
  Z: 0

Field of View: 60
Far Clipping Plane: 1000
```

### Step 4.4: Adjust Directional Light

Select **Directional Light** in the Hierarchy and configure:

```
Position:
  X: 0
  Y: 50
  Z: 0

Rotation:
  X: 50
  Y: -30
  Z: 0

Intensity: 1
Color: White (or slightly warm: R=1, G=0.96, B=0.84)
Shadow Type: Soft Shadows
```

---

## Part 5: Create Minecraft Terrain Manager GameObject

### Step 5.1: Create Empty GameObject

1. In the **Hierarchy** window, right-click
2. Select `Create Empty`
3. Name it: **`MinecraftTerrainManager`**

### Step 5.2: Reset Transform

With `MinecraftTerrainManager` selected:

1. In the **Inspector**, click the **⚙** (gear icon) next to Transform
2. Select **Reset**

This ensures Position = (0, 0, 0), Rotation = (0, 0, 0), Scale = (1, 1, 1)

### Step 5.3: Add MinecraftTerrainGenerator Component

1. With `MinecraftTerrainManager` selected
2. In the **Inspector**, click `Add Component`
3. Search for: **`MinecraftTerrainGenerator`**
4. Click to add it

### Step 5.4: Configure MinecraftTerrainGenerator

In the **Inspector**, configure the component:

```
Voxel Configuration: [Drag DefaultVoxelConfiguration asset here]
  → From: Assets/lib/voxel-core/Configurations/DefaultVoxelConfiguration

Minecraft Terrain Configuration: [Drag Small_10x10x8 asset here]
  → From: Assets/demos/demo-minecraft-terrain/Configurations/Small_10x10x8

Terrain Material: [Drag VoxelTerrain material here]
  → From: Assets/demos/demo-minecraft-terrain/Materials/VoxelTerrain
  → OR: Assets/demos/demo-procedural-terrain-job/Materials/VoxelTerrain

Chunks Per Frame: 2
Auto Generate: ✓ (checked)
```

**How to drag assets**:
1. In the **Project** window, locate the asset (e.g., `Small_10x10x8`)
2. Click and drag it to the corresponding field in the **Inspector**
3. Release the mouse button when the field highlights

### Step 5.5: Add MinecraftTerrainDemoController Component

1. With `MinecraftTerrainManager` still selected
2. In the **Inspector**, click `Add Component`
3. Search for: **`MinecraftTerrainDemoController`**
4. Click to add it

No configuration needed - it will automatically find the `MinecraftTerrainGenerator` component.

### Step 5.6: Save the Scene

**Save**: `Ctrl+S` / `Cmd+S`

---

## Part 6: Test the Demo

### Step 6.1: Enter Play Mode

1. Click the **Play** button at the top of Unity Editor (or press `Ctrl+P` / `Cmd+P`)
2. Wait for terrain generation to complete

### Step 6.2: Expected Results

You should see:

- ✅ Minecraft-style terrain with plateaus and valleys
- ✅ Grass blocks (green) on the surface
- ✅ Dirt blocks (brown) beneath grass
- ✅ Stone blocks (gray) at the bottom
- ✅ Water blocks (blue) at water level (Y=3)
- ✅ Console logs showing generation progress:
  ```
  [MinecraftTerrainGenerator] Generating Minecraft terrain with 10x8x10 chunks...
  [MinecraftTerrainGenerator] Generated heightmap for 10x10 world
  [MinecraftTerrainGenerator] Chunk (0,0,0) generated in X.XXms
  ...
  [MinecraftTerrainGenerator] All 80 chunks generated in X.XXXs
  ```

### Step 6.3: Terrain Analysis (Optional)

After generation completes, the console should display:

```
=== Minecraft Terrain Analysis ===
Total chunks: 80
Total voxels: XXXXXX
Grass blocks: XXX (XX.X%)
Dirt blocks: XXX (XX.X%)
Stone blocks: XXX (XX.X%)
Water blocks: XXX (XX.X%)
Air blocks: XXX (XX.X%)
Average height: X.XX voxels
===================================
```

### Step 6.4: Camera Controls

Use standard Unity Scene view controls in Play mode:

- **Right Mouse Button + WASD**: Fly camera
- **Middle Mouse Button**: Pan
- **Mouse Scroll**: Zoom

---

## Part 7: Testing Different Configurations

### Switching to Medium Configuration (20x20x8)

1. Exit Play mode
2. Select `MinecraftTerrainManager` in the Hierarchy
3. In the **Inspector**, find `MinecraftTerrainGenerator` component
4. Change **Minecraft Terrain Configuration** to: `Medium_20x20x8`
5. Enter Play mode again

Expected: Larger terrain (20 chunks × 20 chunks × 8 chunks = 3,200 chunks)

### Switching to Large Configuration (50x50x8)

1. Exit Play mode
2. Select `MinecraftTerrainManager` in the Hierarchy
3. In the **Inspector**, find `MinecraftTerrainGenerator` component
4. Change **Minecraft Terrain Configuration** to: `Large_50x50x8`
5. Enter Play mode again

Expected: Very large terrain (50 × 50 × 8 = 20,000 chunks) - may take 10-30 seconds to generate

**Performance Note**: Large configuration generates 20,000 chunks. Generation time depends on your hardware:
- Fast CPU: ~10-15 seconds
- Medium CPU: ~20-30 seconds
- Slow CPU: ~30-60 seconds

---

## Troubleshooting

### Issue: "Missing Reference" errors

**Solution**: Ensure all ScriptableObject assets are correctly assigned:
1. Select `MinecraftTerrainManager` in Hierarchy
2. Check that all fields in `MinecraftTerrainGenerator` have assets assigned (not "None")
3. Re-drag assets from Project window if needed

### Issue: No terrain visible

**Solution**: Check camera position and orientation:
1. Select `Main Camera` in Hierarchy
2. Verify Position: (64, 80, 30), Rotation: (30, 0, 0)
3. Ensure Far Clipping Plane is set to 1000

### Issue: Compilation errors

**Solution**: Ensure all scripts are present:
1. Check that `Assets/demos/demo-minecraft-terrain/Scripts/` contains:
   - MinecraftTerrainConfiguration.cs
   - MinecraftHeightmapGenerator.cs
   - MinecraftTerrainCustomGenerator.cs
   - MinecraftTerrainGenerator.cs
   - MinecraftTerrainDemoController.cs
   - TerrainStatsAnalyzer.cs
2. Let Unity recompile (wait for progress bar to complete)

### Issue: Terrain generates too slowly

**Solution**: Adjust Chunks Per Frame:
1. Select `MinecraftTerrainManager` in Hierarchy
2. In `MinecraftTerrainGenerator` component, increase **Chunks Per Frame** to 4 or 8
3. Note: Higher values = faster generation but potential frame drops

---

## Advanced Configuration

### Tweaking Terrain Parameters

You can modify the ScriptableObject configurations to experiment:

**For more variation**:
- Increase `Terrain Variation` (e.g., 4 or 5)
- Increase `Heightmap Octaves` (e.g., 5 or 6)

**For smoother terrain**:
- Decrease `Heightmap Frequency` (e.g., 0.01)
- Decrease `Heightmap Octaves` (e.g., 2 or 3)

**For deeper water**:
- Increase `Water Level` (e.g., 4 or 5)
- Note: Water level must be < Base Terrain Height + Terrain Variation

**For thicker grass/dirt layers**:
- Increase `Grass Layer Thickness` (e.g., 2 or 3)
- Increase `Dirt Layer Thickness` (e.g., 5 or 8)

After modifying a configuration:
1. Save the asset (`Ctrl+S` / `Cmd+S`)
2. Exit and re-enter Play mode to regenerate terrain

---

## Summary Checklist

Before running the demo, verify:

- ✅ `DefaultVoxelConfiguration` created in `Assets/lib/voxel-core/Configurations/`
- ✅ `Small_10x10x8` created in `Assets/demos/demo-minecraft-terrain/Configurations/`
- ✅ `Medium_20x20x8` created in `Assets/demos/demo-minecraft-terrain/Configurations/`
- ✅ `Large_50x50x8` created in `Assets/demos/demo-minecraft-terrain/Configurations/`
- ✅ `VoxelTerrain` material located or created
- ✅ `MinecraftTerrainDemoScene` created in `Assets/demos/demo-minecraft-terrain/Scenes/`
- ✅ `MinecraftTerrainManager` GameObject created with both components
- ✅ All asset references assigned in Inspector
- ✅ Camera and Light configured correctly

Once all checkboxes are complete, press **Play** and enjoy the Minecraft-style terrain generation!

---

## Next Steps

After successfully running the demo, you can:

1. **Experiment with configurations**: Try different world sizes and parameters
2. **Add player controller**: Implement first-person or third-person controls to explore the terrain
3. **Implement terrain modification**: Add voxel placement/destruction (digging)
4. **Add biomes**: Extend `MinecraftTerrainCustomGenerator` with biome support
5. **Optimize rendering**: Implement LOD (Level of Detail) for distant chunks

Refer to the main `README.md` for more information about the implementation and architecture.
