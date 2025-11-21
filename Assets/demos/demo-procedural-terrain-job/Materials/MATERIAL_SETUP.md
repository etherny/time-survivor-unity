# Material Setup Instructions

## VoxelTerrain Material Configuration

### Quick Setup (URP with Custom Shader)

1. **Create the Shader**
   - Create file: `Assets/demos/demo-procedural-terrain-job/Materials/VoxelTerrainShader.shader`
   - Copy the shader code from README.md section "Shader Custom"
   - Wait for Unity to compile the shader

2. **Create the Material**
   - Right-click in `Assets/demos/demo-procedural-terrain-job/Materials/`
   - Select `Create > Material`
   - Name it `VoxelTerrain`

3. **Configure the Material**
   - Select the `VoxelTerrain` material
   - In Inspector:
     - **Shader**: Select `Custom/VoxelTerrainVertexColor`
     - **Main Tex**: White (default)
     - **Smoothness**: 0.2

4. **Assign to DemoController**
   - Open `DemoScene.unity`
   - Select `Demo Controller` in hierarchy
   - Drag `VoxelTerrain` material to `Voxel Material` field

---

## Alternative: Using URP/Lit (May not show vertex colors)

If you want to try the default URP shader (not recommended):

1. **Create Material**
   - Right-click > Create > Material
   - Name: `VoxelTerrain`

2. **Configure**
   - Shader: `Universal Render Pipeline/Lit`
   - Surface Type: Opaque
   - Workflow: Metallic
   - Base Map: White (#FFFFFF)
   - Smoothness: 0.2

**Warning**: URP/Lit may not display vertex colors correctly. If terrain appears white or incorrect colors, use the custom shader method above.

---

## Vertex Color Requirements

The material MUST support vertex colors because:
- `GreedyMeshingJob` outputs vertex colors for each voxel type
- Colors are baked into the mesh (no texture required)
- Color mapping:
  - **Grass**: RGB(0.2, 0.8, 0.2) - Bright green
  - **Dirt**: RGB(0.6, 0.4, 0.2) - Brown
  - **Stone**: RGB(0.5, 0.5, 0.5) - Gray
  - **Water**: RGB(0.2, 0.4, 0.8) - Blue

If colors don't display, the shader doesn't support vertex colors → use custom shader.

---

## Troubleshooting

### Terrain appears white
- Shader doesn't support vertex colors
- Solution: Use custom shader from README.md

### Terrain appears black
- Lighting issue or shader compilation error
- Check Console for shader errors
- Verify Directional Light is in scene

### Terrain has wrong colors
- Shader may be overriding vertex colors
- Verify shader code includes `input.color` in fragment shader
- Check that mesh.colors is assigned in DemoController

---

## Custom Shader Location

For reference, the complete custom shader code is available in:
`Assets/demos/demo-procedural-terrain-job/README.md`

Section: "Shader Custom (Optionnel - Si URP/Lit ne supporte pas vertex colors)"

Copy this shader code to a new .shader file if vertex colors don't display with URP/Lit.
