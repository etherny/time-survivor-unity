# Unity Menu Commands - Procedural Terrain Demo

This demo provides automated setup tools accessible via Unity's menu system.

---

## Available Commands

### 1. Setup Procedural Terrain Demo Scene

**Menu Path**: `Tools > Voxel Demos > Setup Procedural Terrain Demo Scene`

**What it does**:
- Creates a new Unity scene with complete hierarchy
- Adds Main Camera with CameraOrbitController
- Creates Terrain Container (empty GameObject)
- Creates Demo Controller with DemoController script
- Creates UI Canvas with panel structure (Controls + Stats)
- Adds Directional Light for proper lighting
- Saves scene to: `Assets/demos/demo-procedural-terrain-job/Scenes/DemoScene.unity`

**Duration**: ~5 seconds

**Manual Steps After**:
1. Create VoxelTerrain material (see command #2 below)
2. Assign material to DemoController.voxelMaterial field
3. Manually add and wire UI elements:
   - 4 Sliders (Seed, Frequency, Amplitude, OffsetY)
   - 2 Buttons (Generate, Randomize)
   - Assign all UI references in DemoController Inspector

**When to Use**:
- First time setup
- Resetting demo scene after breaking changes
- Creating scene from scratch

---

### 2. Create Voxel Terrain Shader

**Menu Path**: `Tools > Voxel Demos > Create Voxel Terrain Shader`

**What it does**:
- Creates custom URP vertex color shader at: `Assets/demos/.../Materials/VoxelTerrainShader.shader`
- Optionally creates VoxelTerrain material using this shader
- Configures material with correct settings:
  - Shader: Custom/VoxelTerrainVertexColor
  - Smoothness: 0.2
  - Base Texture: White

**Duration**: ~10 seconds (includes shader compilation)

**Dialog Options**:
1. **"Shader Already Exists"** - Choose "Overwrite" or "Cancel"
2. **"Create Material?"** - Choose "Yes" or "No"

**When to Use**:
- After scene setup (command #1)
- When vertex colors don't display with URP/Lit shader
- When shader file is accidentally deleted

**Output**:
- Shader file: `VoxelTerrainShader.shader`
- Material file (optional): `VoxelTerrain.mat`

---

## Recommended Workflow

### First Time Setup

**Step 1**: Create Scene
```
Tools > Voxel Demos > Setup Procedural Terrain Demo Scene
```
Result: DemoScene.unity created with basic structure

**Step 2**: Create Shader & Material
```
Tools > Voxel Demos > Create Voxel Terrain Shader
→ Click "Overwrite" (if prompted)
→ Click "Yes" to create material
```
Result: VoxelTerrain.mat created with vertex color support

**Step 3**: Manual Configuration (5-10 minutes)
1. Open `DemoScene.unity`
2. Select "Demo Controller"
3. Assign VoxelTerrain.mat to "Voxel Material" field
4. Create UI elements (sliders, buttons) in Canvas panels
5. Wire all UI references in DemoController Inspector
6. Configure slider ranges (see README.md)

**Step 4**: Test
```
Press Play in Unity Editor
→ Terrain should generate automatically
→ Test camera controls and UI
```

---

## Troubleshooting Menu Commands

### Command doesn't appear in menu

**Symptoms**: Menu items missing under `Tools > Voxel Demos`

**Causes**:
- Editor scripts not compiled yet
- Scripts in wrong folder (must be in `Editor/` folder)

**Solutions**:
1. Wait for Unity to finish compiling (check bottom-right spinner)
2. Verify files exist:
   - `Assets/demos/.../Editor/DemoSceneSetup.cs`
   - `Assets/demos/.../Editor/CreateVoxelShader.cs`
3. Force reimport: Right-click Editor folder > Reimport
4. Restart Unity Editor if still not appearing

---

### Scene creation fails

**Symptoms**: Error after running "Setup Procedural Terrain Demo Scene"

**Common Errors**:
- "Cannot create scene at path" → Directory doesn't exist
- "DemoController script not found" → Script compilation failed

**Solutions**:
1. Verify all scripts compiled successfully (check Console)
2. Ensure `Scenes/` directory exists:
   ```
   Assets/demos/demo-procedural-terrain-job/Scenes/
   ```
3. Check Console for specific error messages
4. Try manual scene creation (see README.md Option B)

---

### Shader creation fails

**Symptoms**: Error after running "Create Voxel Terrain Shader"

**Common Errors**:
- "Shader compilation failed" → Syntax error in shader code
- "Could not find shader" → Shader not imported yet

**Solutions**:
1. Wait 10-15 seconds for shader compilation
2. Check Console for shader errors
3. Verify URP package is installed:
   ```
   Window > Package Manager > Universal RP
   ```
4. Manually create shader file (copy code from README.md)

---

### Material not assigned automatically

**Symptoms**: DemoController.voxelMaterial field is still "None"

**Cause**: This is expected behavior - menu commands create assets but don't wire Inspector fields

**Solution**:
1. Find VoxelTerrain.mat in Project window:
   ```
   Assets/demos/.../Materials/VoxelTerrain.mat
   ```
2. Drag to DemoController.voxelMaterial field in Inspector
3. This is a one-time manual step

---

## Advanced Usage

### Resetting the Demo

If demo is broken or you want to start fresh:

1. **Delete existing scene**:
   - Delete `DemoScene.unity` from `Scenes/` folder

2. **Delete shader/material** (optional):
   - Delete `VoxelTerrainShader.shader`
   - Delete `VoxelTerrain.mat`

3. **Re-run commands**:
   ```
   Tools > Voxel Demos > Setup Procedural Terrain Demo Scene
   Tools > Voxel Demos > Create Voxel Terrain Shader
   ```

4. **Reconfigure** (see Step 3 in Recommended Workflow)

---

### Customizing Menu Commands

To modify menu commands behavior:

**Edit Setup Script**:
```
Assets/demos/.../Editor/DemoSceneSetup.cs
```

**Edit Shader Creation**:
```
Assets/demos/.../Editor/CreateVoxelShader.cs
```

**Note**: These are Editor scripts - changes only affect Editor, not runtime behavior.

---

## Menu Command Source Code

### DemoSceneSetup.cs
- Location: `Assets/demos/.../Editor/DemoSceneSetup.cs`
- Lines: 202
- Entry Point: `[MenuItem("Tools/Voxel Demos/Setup Procedural Terrain Demo Scene")]`

### CreateVoxelShader.cs
- Location: `Assets/demos/.../Editor/CreateVoxelShader.cs`
- Lines: 137
- Entry Point: `[MenuItem("Tools/Voxel Demos/Create Voxel Terrain Shader")]`

---

## See Also

- **QUICKSTART.md** - 2-minute setup guide using these commands
- **README.md** - Complete documentation including manual setup
- **MATERIAL_SETUP.md** - Material configuration details
- **IMPLEMENTATION_SUMMARY.md** - Technical architecture overview

---

**Note**: Menu commands are designed to automate 80% of setup work. The remaining 20% (UI wiring, reference assignment) must be done manually due to Unity Editor API limitations.
