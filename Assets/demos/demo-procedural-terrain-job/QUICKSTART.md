# Quick Start Guide - Procedural Terrain Generation Demo

This is a **2-minute quick start** to get the demo running immediately.

## Prerequisites Check

Before starting, verify you have:
- ✅ Unity 6000.2.12f1 (or newer)
- ✅ Universal Render Pipeline (URP) configured
- ✅ Burst Compiler package installed
- ✅ TextMeshPro package installed

If any are missing, see full README.md for installation instructions.

---

## Option A: Automated Setup (Recommended - 2 minutes)

### Step 1: Create the Scene (30 seconds)

1. In Unity, go to menu: **Tools > Voxel Demos > Setup Procedural Terrain Demo Scene**
2. Wait for scene creation (~5 seconds)
3. Scene will open automatically at: `Assets/demos/demo-procedural-terrain-job/Scenes/DemoScene.unity`

### Step 2: Create the Shader & Material (30 seconds)

1. Go to menu: **Tools > Voxel Demos > Create Voxel Terrain Shader**
2. Click **"Overwrite"** if prompted
3. Click **"Yes"** when asked to create material
4. Wait for Unity to compile shader (~10 seconds)

### Step 3: Configure DemoController (30 seconds)

1. In Hierarchy, select **"Demo Controller"**
2. In Inspector, find **"Voxel Material"** field
3. Drag the material from `Assets/demos/.../Materials/VoxelTerrain.mat` to this field

**Note**: UI elements (sliders, buttons, texts) are already created by the setup script. You need to manually connect them:

4. In Hierarchy, expand **"UI Canvas"**
5. Manually create and assign:
   - **4 Sliders** in "Panel - Controls" (Seed, Frequency, Amplitude, OffsetY)
   - **2 Buttons** in "Panel - Controls" (Generate, Randomize)
   - Assign all UI references in DemoController Inspector

See README.md section "Configuration de l'UI" for detailed slider settings.

### Step 4: Test the Demo (30 seconds)

1. Press **Play**
2. Wait for initial generation (~2 seconds)
3. Observe the terrain in Game view
4. Use camera controls:
   - **Left-click + Drag**: Orbit camera
   - **Mouse wheel**: Zoom
5. Test generation controls:
   - Click **"Randomize"** button
   - Adjust sliders and click **"Generate"**

**Expected Result**:
- 64³ voxel terrain with colored voxels (Green=Grass, Brown=Dirt, Gray=Stone, Blue=Water)
- Generation time < 5ms
- FPS > 60

---

## Option B: Manual Scene Creation (Advanced - 10 minutes)

If automated setup fails or you want full control, follow the detailed manual setup in **README.md** section "Installation > Option B".

---

## Troubleshooting Quick Fixes

### Terrain appears white or no colors
**Fix**: Shader doesn't support vertex colors
1. Verify material uses shader `Custom/VoxelTerrainVertexColor`
2. If shader missing, run: **Tools > Voxel Demos > Create Voxel Terrain Shader**

### Terrain doesn't appear
**Fix**: Missing references
1. Select "Demo Controller" in Hierarchy
2. Check Inspector for any "None (Transform)" or "None (Material)" fields
3. Assign:
   - **Terrain Container**: Drag from Hierarchy
   - **Voxel Material**: Drag from Materials folder
   - **All UI elements**: Drag from UI Canvas children

### Compilation errors
**Fix**: Missing packages
1. Go to **Window > Package Manager**
2. Install: Burst, Mathematics, Collections, TextMeshPro
3. Restart Unity

### UI doesn't respond
**Fix**: EventSystem missing
1. Check Hierarchy for "EventSystem"
2. If missing: Right-click Hierarchy > UI > Event System

### Performance issues (<10 FPS)
**Fix**: Burst not enabled
1. Go to **Jobs > Burst > Enable Compilation**
2. Restart Unity
3. Regenerate terrain

---

## Next Steps

Once demo is running:

1. **Explore Parameters**
   - Try different seeds (0-999999)
   - Adjust frequency (0.01-0.2) to change terrain scale
   - Adjust amplitude (5-50) to change height variation
   - Modify offsetY (0-64) to see underground/surface/mountains

2. **Validate Performance**
   - Check "Generation Time" in stats panel (should be <5ms)
   - Monitor FPS (should be >60)
   - Observe voxel distribution percentages

3. **Read Full Documentation**
   - See **README.md** for complete feature list
   - Learn about architecture in "Notes techniques" section
   - Explore extension ideas in "Extension de la démo" section

---

## Support

- **Full Documentation**: `Assets/demos/demo-procedural-terrain-job/README.md`
- **Material Setup**: `Assets/demos/demo-procedural-terrain-job/Materials/MATERIAL_SETUP.md`
- **Voxel Engine Docs**: `Assets/lib/voxel-*/Documentation~/`

**Still stuck?** Check "Problèmes connus" section in README.md for detailed troubleshooting.

---

**Total Setup Time**: ~2-3 minutes (Automated) | ~10 minutes (Manual)

**Result**: Fully functional procedural terrain generation demo ready for testing and exploration.
