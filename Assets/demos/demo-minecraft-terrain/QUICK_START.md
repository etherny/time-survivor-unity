# Minecraft Terrain Demo - Quick Start Guide

**Total Setup Time**: 10-15 minutes

This is a condensed quick-start guide. For detailed instructions, see `UNITY_SETUP_GUIDE.md`.

---

## Prerequisites

- Unity Editor 6000.2.12f1 open with this project
- All scripts have compiled successfully (check Console for errors)

---

## Step 1: Run Automated Setup (2 minutes)

### 1.1: Open Setup Tool

In Unity menu:

```
Tools > TimeSurvivor > Setup Minecraft Terrain Demo
```

### 1.2: Verify Creation

Check Console for confirmation:

```
[MinecraftTerrainDemoSetup] Demo setup complete! Assets created in:
  - Assets/demos/demo-minecraft-terrain/Configurations
  - Assets/lib/voxel-core/Configurations
```

### 1.3: Verify Assets Created

In Project window, check these folders:

- `Assets/demos/demo-minecraft-terrain/Configurations/`
  - ✅ Small_10x10x8.asset
  - ✅ Medium_20x20x8.asset
  - ✅ Large_50x50x8.asset

- `Assets/lib/voxel-core/Configurations/`
  - ✅ DefaultVoxelConfiguration.asset

**If assets not created**: See troubleshooting in `UNITY_SETUP_GUIDE.md`

---

## Step 2: Create the Scene (5-10 minutes)

### 2.1: Create New Scene

```
File > New Scene > Basic (URP) > Create
```

### 2.2: Save Scene

```
File > Save As...
Navigate to: Assets/demos/demo-minecraft-terrain/Scenes/
Name: MinecraftTerrainDemoScene
Click: Save
```

### 2.3: Configure Main Camera

Select **Main Camera** in Hierarchy, set in Inspector:

```
Position:  X=64,  Y=80,  Z=30
Rotation:  X=30,  Y=0,   Z=0
Far Clipping Plane: 1000
```

### 2.4: Configure Directional Light

Select **Directional Light** in Hierarchy, set in Inspector:

```
Position:  X=0,   Y=50,  Z=0
Rotation:  X=50,  Y=-30, Z=0
Shadow Type: Soft Shadows
```

### 2.5: Create Terrain Manager

In Hierarchy:

```
Right-click > Create Empty
Name: MinecraftTerrainManager
```

Select it, in Inspector:

```
Transform: Reset (gear icon > Reset)
  → Position: (0, 0, 0)
  → Rotation: (0, 0, 0)
  → Scale: (1, 1, 1)
```

### 2.6: Add Components

With **MinecraftTerrainManager** selected:

**Add Component 1**:
```
Add Component > Search: "MinecraftTerrainGenerator"
```

**Add Component 2**:
```
Add Component > Search: "MinecraftTerrainDemoController"
```

### 2.7: Assign References

With **MinecraftTerrainManager** selected, in **MinecraftTerrainGenerator** component:

**Drag assets from Project window to Inspector fields**:

```
Voxel Configuration:
  → Drag: Assets/lib/voxel-core/Configurations/DefaultVoxelConfiguration

Minecraft Terrain Configuration:
  → Drag: Assets/demos/demo-minecraft-terrain/Configurations/Small_10x10x8

Terrain Material:
  → Drag: Assets/demos/demo-minecraft-terrain/Materials/VoxelTerrain

Chunks Per Frame: 2
Auto Generate: ✓ (checked)
```

**How to drag**: Click asset in Project window, drag to field in Inspector, release.

### 2.8: Save Scene

```
Ctrl+S (Windows/Linux)
Cmd+S (Mac)
```

---

## Step 3: Test the Demo (1-2 minutes)

### 3.1: Press Play

Click **Play** button (or `Ctrl+P` / `Cmd+P`)

### 3.2: Watch Console

You should see:

```
[MinecraftTerrainGenerator] Generating Minecraft terrain with 10x8x10 chunks...
[MinecraftTerrainGenerator] Generated heightmap for 10x10 world
[MinecraftTerrainGenerator] Chunk (0,0,0) generated in X.XXms
...
[MinecraftTerrainGenerator] All 800 chunks generated in X.XXXs

=== Minecraft Terrain Analysis ===
Total chunks: 800
Total voxels: 209715200
Grass blocks: 21605493 (10.3%)
Dirt blocks: 43210987 (20.6%)
Stone blocks: 76543210 (36.5%)
Water blocks: 4034412 (1.9%)
Air blocks: 64321098 (30.7%)
===================================
```

### 3.3: View Terrain

In Scene view, you should see:

- ✅ Minecraft-style terrain with plateaus and valleys
- ✅ Green grass blocks on top
- ✅ Brown dirt blocks beneath grass
- ✅ Gray stone blocks at bottom
- ✅ Blue water in valleys

### 3.4: Camera Controls

- **Right-click + Move Mouse**: Rotate camera
- **Mouse Wheel**: Zoom in/out
- **Middle-click + Move Mouse**: Pan (move sideways)

---

## Step 4: Try Other Configurations (Optional)

### 4.1: Stop Play Mode

Press **Play** button again to stop

### 4.2: Change Configuration

Select **MinecraftTerrainManager** in Hierarchy

In Inspector, change **Minecraft Terrain Configuration**:

**For Medium terrain (20×20×8)**:
- Drag: `Medium_20x20x8.asset`

**For Large terrain (50×50×8)**:
- Drag: `Large_50x50x8.asset`
- ⚠️ Warning: Large takes 10-30 seconds to generate

### 4.3: Press Play Again

Watch new terrain generate with different size

---

## Success Checklist

Before considering setup complete:

- [ ] Automated setup tool ran successfully
- [ ] 4 ScriptableObject assets created
- [ ] MinecraftTerrainDemoScene created and saved
- [ ] Main Camera and Directional Light configured
- [ ] MinecraftTerrainManager GameObject created
- [ ] Both components added (Generator + Controller)
- [ ] All references assigned (no "None" fields)
- [ ] Scene saved
- [ ] Play mode: Terrain generates without errors
- [ ] Terrain appears with correct colors (grass/dirt/stone/water)
- [ ] No errors in Console

**If all checkboxes complete**: Setup successful! 🎉

---

## Troubleshooting Quick Fix

| Problem | Quick Fix |
|---------|-----------|
| Setup tool doesn't appear | Wait for compilation (progress bar bottom-right) |
| Assets not created | Run tool again, check Console for errors |
| "Missing Reference" error | Re-assign assets in Inspector (drag from Project window) |
| Terrain doesn't appear | Check Camera position (64, 80, 30) and rotation (30, 0, 0) |
| Terrain is white/black | Assign VoxelTerrain material in Terrain Material field |
| Too slow | Increase Chunks Per Frame to 4 or 8 |

For detailed troubleshooting, see `UNITY_SETUP_GUIDE.md` section "Troubleshooting".

---

## Next Steps

After successful setup:

1. **Experiment**: Try different configurations (Small/Medium/Large)
2. **Customize**: Modify parameters in ScriptableObjects
3. **Extend**: Add player controller, voxel destruction, building
4. **Learn**: Read architecture docs in `README.md`

---

## Documentation Files

- **QUICK_START.md** (this file) - Fast setup guide
- **UNITY_SETUP_GUIDE.md** - Comprehensive step-by-step guide
- **CONFIGURATIONS_REFERENCE.md** - Parameter explanations
- **README.md** - Main documentation with architecture details
- **DEMO_ASSETS_SUMMARY.md** - Complete assets inventory
- **SETUP_STATUS.md** - Current setup status and progress

---

**Need Help?**

1. Check `UNITY_SETUP_GUIDE.md` for detailed instructions
2. Check Console for error messages
3. Verify all checkboxes in Success Checklist above
4. Start with Small configuration (easier to debug)

---

**Estimated Time Breakdown**:
- Step 1 (Automated Setup): 2 minutes
- Step 2 (Scene Creation): 5-10 minutes
- Step 3 (Testing): 1-2 minutes
- **Total**: 10-15 minutes

**Good luck and enjoy the demo!** 🎮
