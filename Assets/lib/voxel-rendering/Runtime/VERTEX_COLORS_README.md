# Vertex Color Support in Voxel Rendering

## Overview

The voxel rendering system now supports **vertex colors**, allowing each voxel type to be rendered with a unique color without requiring textures. This feature is implemented using Unity's vertex color system and is fully compatible with the Burst-compiled greedy meshing algorithm.

## Architecture

### Key Components

1. **GreedyMeshingJob** (`Jobs/GreedyMeshingJob.cs`)
   - Added `Colors` output field (NativeList<float4>)
   - Added `MaskVoxelTypes` buffer to track voxel types during meshing
   - Implemented `GetColorForVoxelType()` method with predefined color mapping
   - Modified greedy merging to only merge identical VoxelTypes

2. **ChunkManager** (`voxel-terrain/Runtime/Chunks/ChunkManager.cs`)
   - Allocates color buffer for meshing jobs
   - Passes colors to MeshBuilder
   - Disposes color buffers after use

3. **MeshBuilder** (`Builders/MeshBuilder.cs`)
   - New overload: `BuildMesh(..., NativeList<float4> colors)`
   - Converts float4 colors to Unity Color array
   - Assigns colors to mesh.colors property

4. **VoxelVertexColor Shader** (`Shaders/VoxelVertexColor.shader`)
   - URP-compatible shader that reads vertex colors
   - Simple diffuse lighting based on normals
   - Toggle between vertex colors and texture (future-proofing)
   - Supports transparency (for water voxels)

## VoxelType Color Mapping

The following colors are assigned to each VoxelType:

| VoxelType | Color (RGBA) | Description |
|-----------|--------------|-------------|
| Grass     | (0.2, 0.8, 0.2, 1.0) | Bright green |
| Dirt      | (0.6, 0.4, 0.2, 1.0) | Brown |
| Stone     | (0.5, 0.5, 0.5, 1.0) | Gray |
| Sand      | (0.9, 0.8, 0.5, 1.0) | Yellow |
| Water     | (0.2, 0.4, 0.8, 0.7) | Blue (semi-transparent) |
| Wood      | (0.4, 0.25, 0.1, 1.0) | Dark brown |
| Leaves    | (0.1, 0.6, 0.1, 1.0) | Green |
| Air       | (1.0, 0.0, 1.0, 1.0) | Magenta (debug) |

Colors can be customized by modifying the `GetColorForVoxelType()` method in `GreedyMeshingJob.cs`.

## Usage

### Basic Setup

1. **Create a Material with the VoxelVertexColor shader:**
   ```
   Right-click in Project → Create → Material
   Shader: Voxel/VertexColor
   Set "Use Vertex Color" toggle to ON
   ```

2. **Assign Material to ChunkManager:**
   ```csharp
   var chunkManager = new ChunkManager(config, chunkParent, vertexColorMaterial);
   ```

3. **Generate Chunks:**
   - The system automatically generates vertex colors during meshing
   - No additional code changes needed in your generation logic

### Demo Scene

A complete demo is available in `Assets/demos/chessboard-voxel/`:

- **ChessboardVoxelGenerator**: IVoxelGenerator implementation that creates a 16x16 chessboard
- **ChessboardVoxelDemo**: MonoBehaviour that sets up the demo scene

**To use the demo:**
1. Create an empty GameObject in your scene
2. Add the `ChessboardVoxelDemo` component
3. Assign the VoxelVertexColor material to the "Voxel Vertex Color Material" field
4. Press Play

## Technical Details

### Burst Compatibility

All vertex color code is **Burst-compatible**:
- Uses `float4` instead of Unity's `Color` struct in jobs
- Uses switch statements instead of dictionaries for color mapping
- No managed allocations in hot paths

### Greedy Meshing Behavior

The greedy meshing algorithm only merges faces of **identical VoxelType**:
- Before: Merged all non-air voxels regardless of type
- After: Only merges adjacent faces if VoxelType matches
- Result: Each merged quad has a single uniform color

### Performance Impact

- **Memory**: +16 bytes per vertex (float4 color)
- **CPU**: Negligible overhead (simple switch statement)
- **GPU**: Standard vertex color rendering (no additional cost)

### Compatibility

- **Unity Version**: 6000.2.12f1+
- **Render Pipeline**: Universal Render Pipeline (URP) only
- **Platforms**: All platforms supported by Unity Jobs + Burst

## Extending the System

### Custom Color Palettes

To customize voxel colors, edit the `GetColorForVoxelType()` method:

```csharp
private float4 GetColorForVoxelType(VoxelType voxelType)
{
    switch (voxelType)
    {
        case VoxelType.Grass:
            return new float4(0.2f, 0.8f, 0.2f, 1.0f); // Your custom color
        // ... other cases
    }
}
```

### Per-Voxel Color Variation

For more advanced use cases (per-voxel colors, noise-based variation):
1. Modify `BuildMask()` to store additional color data
2. Pass color variation parameters to `CreateQuad()`
3. Compute final color in `CreateQuad()` based on position/noise

### Texture + Vertex Color Blending

The VoxelVertexColor shader supports toggling between vertex colors and textures:
- Set `_UseVertexColor` to 0 to use texture
- Set `_UseVertexColor` to 1 to use vertex colors
- Future: Blend both for tinted textures

## Troubleshooting

### Voxels Appear White/Wrong Color

**Solution**: Ensure you're using a material with the `Voxel/VertexColor` shader.

### Voxels Appear Black

**Solution**: Check that lighting is set up correctly. The shader requires at least a directional light.

### Different VoxelTypes Merge Together

**Solution**: This shouldn't happen after the update. If it does, verify that `MaskVoxelTypes` buffer is correctly allocated in ChunkManager.

### Build Errors

**Solution**: Ensure Unity.Mathematics and Unity.Collections packages are installed and assembly definitions reference them.

## Future Enhancements

Potential improvements for the vertex color system:

1. **ScriptableObject Color Palettes**: Define color mappings in asset files
2. **Per-Face Color Variation**: Add noise/randomness to colors
3. **Ambient Occlusion**: Darken vertex colors in corners
4. **Height-Based Gradients**: Blend colors based on Y position
5. **Biome-Specific Palettes**: Different colors per biome

## Related Files

- `/Assets/lib/voxel-rendering/Runtime/Jobs/GreedyMeshingJob.cs`
- `/Assets/lib/voxel-rendering/Runtime/Builders/MeshBuilder.cs`
- `/Assets/lib/voxel-rendering/Runtime/Shaders/VoxelVertexColor.shader`
- `/Assets/lib/voxel-terrain/Runtime/Chunks/ChunkManager.cs`
- `/Assets/demos/chessboard-voxel/Runtime/ChessboardVoxelDemo.cs`

## License

Part of the TimeSurvivor voxel engine. All rights reserved.
