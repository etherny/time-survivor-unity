# Terrain Collision Demo

## Description

This demonstration scene showcases the voxel terrain collision system implemented in Issue #7. It features:

- **Voxel terrain with collision meshes** - Generated using the `VoxelCollisionBaker` at half-resolution for optimal performance
- **First-person character controller** - Walk, jump, and interact with the terrain
- **Dynamic physics objects** - Spawn spheres and cubes that fall and collide with the terrain
- **Collision visualization** - Toggle to see collision meshes rendered as wireframes
- **Real-time metrics** - FPS counter, player position, grounded status, collision statistics
- **Asynchronous collision baking** - Non-blocking collision mesh generation using Unity's Job System

This demo validates that the collision system works correctly with player movement and physics objects.

## Prerequisites

- **Unity Version**: 6000.2.12f1
- **Render Pipeline**: Universal Render Pipeline (URP)
- **Packages Required**:
  - Unity.Collections
  - Unity.Mathematics
  - Unity.Burst
  - Unity.Jobs
- **Configuration**: TerrainStatic layer must exist in the project

## Installation

### Step 1: Create the TerrainStatic Layer

Before running the demo, you MUST create the `TerrainStatic` physics layer:

1. Open Unity Editor
2. Go to menu: `Tools > Terrain Collision Demo > Create TerrainStatic Layer`
3. Verify the layer was created:
   - Go to `Edit > Project Settings > Tags and Layers`
   - Confirm "TerrainStatic" appears in one of the User Layer slots (8-31)

### Step 2: Generate Demo Assets

The demo includes an automated asset generator that creates all necessary files:

1. Open Unity Editor
2. Go to menu: `Tools > Terrain Collision Demo > Create Demo Assets`
3. Wait for the process to complete (should take 5-10 seconds)
4. Check the Console for confirmation messages

This will create:
- `Config/TerrainCollisionDemoConfig.asset` - VoxelConfiguration with collision settings
- `Materials/TerrainMaterial.mat` - Material for voxel terrain
- `Materials/PhysicsObjectMaterial.mat` - Material for physics objects
- `Materials/GroundIndicator.mat` - Material for ground detection indicator
- `Prefabs/PhysicsSphere.prefab` - Sphere with Rigidbody
- `Prefabs/PhysicsCube.prefab` - Cube with Rigidbody
- `Prefabs/DemoCamera.prefab` - Player with CharacterController and camera
- `Scenes/TerrainCollisionDemo.unity` - Complete demo scene

## Usage

### Opening the Demo Scene

1. In Unity Project window, navigate to:
   ```
   Assets/demos/demo-terrain-collision/Scenes/TerrainCollisionDemo.unity
   ```
2. Double-click to open the scene
3. You should see:
   - A "Player" GameObject at position (32, 10, 32)
   - A "TerrainManager" GameObject with all demo components
   - A "Directional Light" for scene lighting

### Configuration Check

Before running, verify the following in the Inspector:

#### Player GameObject
- **CharacterController**:
  - Radius: 0.5
  - Height: 1.8
  - Center: (0, 0.9, 0)
- **SimpleCharacterController**:
  - Move Speed: 5
  - Jump Velocity: 5
  - Gravity: -9.81
  - Ground Layer: TerrainStatic

#### TerrainManager GameObject
- **CollisionDemoController**:
  - Config: TerrainCollisionDemoConfig asset
  - Terrain Material: TerrainMaterial
  - Enable Collision: ✓ (checked)
  - Use Async Baking: ✓ (checked)
  - Player Transform: Player GameObject

- **PhysicsObjectSpawner**:
  - Sphere Prefab: PhysicsSphere
  - Cube Prefab: PhysicsCube
  - Max Objects: 20
  - Player Transform: Player GameObject

- **CollisionVisualizer**:
  - Show Colliders: ✓ (checked)
  - Player Controller: Player's SimpleCharacterController

- **DemoUI**:
  - All references auto-assigned

### Running the Demo

1. Click the **Play** button in Unity Editor
2. Wait 2-5 seconds for terrain chunks to generate and bake collision
3. The player will spawn at (32, 10, 32) and fall onto the terrain

## Controls

| Input | Action |
|-------|--------|
| **WASD** | Move (forward/left/backward/right) |
| **Mouse** | Look around (first-person camera) |
| **Spacebar** | Jump |
| **O** | Spawn physics object (sphere or cube) |
| **V** | Toggle collision visualization (wireframe) |
| **H** | Toggle UI overlay |
| **ESC** | Unlock cursor (click to re-lock) |

## Validation

### What You Should See:

Upon starting the demo, you should observe the following behaviors within 2 minutes:

#### 1. Terrain Generation
- ✅ Flat checkerboard terrain appears (Grass and Dirt voxels in 8x8 tiles)
- ✅ Terrain is centered at origin with chunks streaming around the player
- ✅ No errors in the Console during terrain generation

#### 2. Player Collision
- ✅ Player falls and **stops on the terrain** (collision working)
- ✅ "Grounded: Yes" appears in green in the UI
- ✅ Player can walk on terrain using WASD keys
- ✅ Terrain feels solid (no falling through)

#### 3. Player Movement
- ✅ WASD keys move the player horizontally
- ✅ Mouse movement rotates the camera smoothly
- ✅ Spacebar makes the player jump
- ✅ Player position updates in the UI (top-left corner)

#### 4. Physics Objects
- ✅ Press 'O' to spawn a sphere or cube 5 meters ahead
- ✅ Object falls due to gravity
- ✅ Object **collides with and rests on the terrain** (collision working)
- ✅ Multiple objects can be spawned (up to 20 max)
- ✅ Active object count updates in UI

#### 5. Collision Visualization
- ✅ Press 'V' to toggle collision visualization
- ✅ Green wireframe meshes appear overlaying the terrain
- ✅ Wireframes match the terrain shape (half-resolution)
- ✅ Ground raycast appears as red/green line from player

#### 6. Performance
- ✅ **FPS stays at or above 60** (displayed in UI)
- ✅ No frame stuttering or lag
- ✅ Initial collision baking completes within 2-5 seconds
- ✅ "Baking Queue" count drops to 0 after initial load

#### 7. UI Metrics
- ✅ FPS displays in green (≥60)
- ✅ Player position shows X, Y, Z coordinates
- ✅ "Grounded: Yes" when on terrain (green text)
- ✅ "Collision Chunks" shows non-zero count (16 chunks expected)
- ✅ "Baking Queue" shows 0 after initial load
- ✅ "Physics Objects" count updates when spawning

## Technical Details

### Terrain Configuration

The demo uses a `VoxelConfiguration` asset with these settings:

```
Chunk Size: 16 voxels
Macro Voxel Size: 1.0 meter
Macro Scale: 4 (creates 4x4x4 = 64 chunks total, but only ~16 loaded at once)
Render Distance: 2 chunks

Collision Settings:
- Enable Collision: true
- Collision Resolution Divider: 2 (half-resolution = 8x8x8 collision voxels per chunk)
- Use Async Collision Baking: true
- Max Collision Bakes Per Frame: 2
- Max Collision Bake Time: 5ms
- Terrain Layer Name: "TerrainStatic"

Meshing Settings:
- Meshing Mode: Greedy
- Max Mesh Jobs Per Frame: 4
- Max Meshing Time: 10ms
```

### Collision System Architecture

The collision system works as follows:

1. **Terrain Generation**: `FlatCheckerboardGenerator` creates voxel data
2. **Meshing**: `GreedyMeshingJob` generates visible mesh (full resolution)
3. **Collision Baking**: `VoxelCollisionBaker.CollisionBakingJob` generates collision mesh (half resolution)
4. **Async Processing**: Collision baking runs asynchronously using Unity's Job System (Burst compiled)
5. **Application**: Collision mesh applied to `MeshCollider` on chunk GameObject
6. **Physics Layer**: Chunk assigned to "TerrainStatic" layer for proper ground detection

### Performance Characteristics

Expected performance metrics:

- **Initial Load**: 2-5 seconds to bake collision for all visible chunks (~16 chunks)
- **Runtime FPS**: 60+ FPS with player movement and physics objects
- **Collision Baking**: ~50-100ms per chunk (async, non-blocking)
- **Memory**: ~10-20KB per collision mesh (half-resolution)
- **Physics Objects**: Supports 20+ dynamic Rigidbody objects with stable FPS

### File Structure

```
Assets/demos/demo-terrain-collision/
├── Scenes/
│   └── TerrainCollisionDemo.unity          # Main demo scene
├── Scripts/
│   ├── SimpleCharacterController.cs        # First-person player controller
│   ├── PhysicsObjectSpawner.cs             # Spawns physics test objects
│   ├── CollisionVisualizer.cs              # Visualizes collision meshes
│   ├── DemoUI.cs                           # UI overlay with metrics
│   ├── CollisionDemoController.cs          # Main orchestrator
│   └── TimeSurvivor.Demos.TerrainCollision.asmdef
├── Editor/
│   ├── DemoAssetCreator.cs                 # Automated asset generation
│   └── TimeSurvivor.Demos.TerrainCollision.Editor.asmdef
├── Materials/
│   ├── TerrainMaterial.mat                 # Voxel terrain material (URP Lit)
│   ├── PhysicsObjectMaterial.mat           # Physics object material (URP Lit)
│   └── GroundIndicator.mat                 # Ground detection visual (Transparent)
├── Prefabs/
│   ├── PhysicsSphere.prefab                # Sphere with Rigidbody
│   ├── PhysicsCube.prefab                  # Cube with Rigidbody
│   └── DemoCamera.prefab                   # Player with CharacterController
├── Config/
│   └── TerrainCollisionDemoConfig.asset    # VoxelConfiguration ScriptableObject
└── README.md                               # This file
```

## Troubleshooting

### Player Falls Through Terrain

**Problem**: Player spawns and falls infinitely without stopping.

**Solutions**:
1. Check that "TerrainStatic" layer exists:
   - `Edit > Project Settings > Tags and Layers`
   - Run `Tools > Terrain Collision Demo > Create TerrainStatic Layer`
2. Verify SimpleCharacterController "Ground Layer" includes "TerrainStatic"
3. Wait for collision baking to complete (check "Baking Queue" in UI)
4. Check Console for errors related to `VoxelCollisionBaker`

### No Terrain Appears

**Problem**: Scene is empty after pressing Play.

**Solutions**:
1. Verify `TerrainCollisionDemoConfig.asset` is assigned to `CollisionDemoController`
2. Check that `FlatCheckerboardGenerator` is being used (no errors in Console)
3. Ensure `TerrainMaterial` is assigned and valid (URP shader)
4. Check that player is at position (32, 10, 32) - terrain generates around this point

### Low FPS (<30)

**Problem**: Demo runs slowly with low frame rate.

**Solutions**:
1. Reduce `CollisionResolutionDivider` to 4 (quarter-resolution collision)
2. Decrease `RenderDistance` to 1 in VoxelConfiguration
3. Lower `MaxCollisionBakesPerFrame` to 1
4. Ensure "Use Async Collision Baking" is enabled (checked)
5. Check Unity Profiler for bottlenecks

### Physics Objects Don't Collide

**Problem**: Spawned objects fall through terrain.

**Solutions**:
1. Wait for collision baking to complete (check "Baking Queue: 0")
2. Verify chunks have `MeshCollider` components (use 'V' to visualize)
3. Check that physics objects have `Rigidbody` component
4. Ensure collision layer matrix allows collision between "Default" and "TerrainStatic"

### Collision Visualization Doesn't Show

**Problem**: Pressing 'V' doesn't display wireframes.

**Solutions**:
1. Ensure you're in Scene view (not Game view) - gizmos only show in Scene view
2. Check that Scene view has "Gizmos" enabled (top-right of Scene view)
3. Verify `CollisionVisualizer` component is on TerrainManager GameObject
4. Check Console for errors in `CollisionVisualizer`

### "TerrainStatic Layer Not Found" Warning

**Problem**: Console shows layer warning during collision baking.

**Solutions**:
1. Run `Tools > Terrain Collision Demo > Create TerrainStatic Layer` menu command
2. Manually create layer:
   - `Edit > Project Settings > Tags and Layers`
   - Find empty User Layer slot (8-31)
   - Name it "TerrainStatic" (exact spelling, case-sensitive)
3. Restart the scene after creating the layer

## Known Issues

- **Initial spawn delay**: First 2-5 seconds may show player in air while collision bakes
  - **Workaround**: Wait for "Baking Queue: 0" before testing collision

- **Edge chunk popping**: Chunks at render distance edge may pop in/out
  - **Workaround**: This is expected behavior with streaming, increase RenderDistance if needed

- **Cursor lock**: Sometimes cursor stays locked after exiting Play mode
  - **Workaround**: Press ESC key to unlock, or click in Scene view

## Notes

### Design Decisions

- **Half-resolution collision** (divider=2): Balances performance and accuracy. Terrain is simple enough that half-res provides adequate collision.
- **Asynchronous baking**: Prevents frame stuttering during chunk load. Critical for smooth gameplay.
- **Flat checkerboard terrain**: Simplifies testing and makes collision validation easy (flat surface = no complex geometry bugs).
- **First-person controller**: Best demonstrates grounded state and collision response compared to free-cam.

### Performance Tuning

If you need to optimize further:

1. **Increase collision divider** (2 → 4): Quarter-resolution collision, faster baking, less accurate
2. **Reduce render distance** (2 → 1): Fewer chunks loaded = less collision baking
3. **Lower max bakes per frame** (2 → 1): Slower baking but more stable frame time
4. **Disable async baking**: Debug synchronous baking issues (will cause frame spikes)

### Integration with Other Systems

This demo can be extended with:

- **Destructible terrain**: Integrate with `DestructibleOverlayManager` for voxel modification
- **LOD collision**: Implement distance-based collision resolution (near=full, far=quarter)
- **Custom terrain generators**: Replace `FlatCheckerboardGenerator` with caves, mountains, etc.
- **Advanced physics**: Add character controller with climbing, swimming, etc.

## Related Documentation

- [Issue #7: Terrain Collision System](../../docs/issues/issue-7-terrain-collision.md)
- [VoxelCollisionBaker API](../../Assets/lib/voxel-physics/Runtime/Collision/VoxelCollisionBaker.cs)
- [Architecture Decision Record: Collision System](../../docs/adr/ADR-007-voxel-collision.md)

## Testing Checklist

Use this checklist to validate the demo:

- [ ] TerrainStatic layer exists
- [ ] Demo assets created successfully (via Tools menu)
- [ ] Scene opens without errors
- [ ] Player spawns at (32, 10, 32)
- [ ] Terrain generates within 5 seconds
- [ ] Player falls and stops on terrain (collision works)
- [ ] "Grounded: Yes" appears in UI (green text)
- [ ] WASD movement works smoothly
- [ ] Mouse look rotates camera
- [ ] Spacebar jumps correctly
- [ ] 'O' spawns physics objects
- [ ] Physics objects fall and rest on terrain
- [ ] 'V' toggles collision visualization (Scene view)
- [ ] FPS ≥ 60 (shown in UI)
- [ ] Baking Queue reaches 0
- [ ] Collision Chunks shows ~16
- [ ] No errors in Console
- [ ] No warnings about missing layers

**Target**: All checkboxes ✅ in under 2 minutes of testing.

---

**Created**: 2025-11-22
**Unity Version**: 6000.2.12f1
**URP Version**: Compatible with Unity 6 URP
**Issue**: #7 - Terrain Collision System
**Status**: Complete and validated
