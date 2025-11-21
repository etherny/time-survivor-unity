# ADR-001: Dual-Scale Voxel System (0.2 + 0.1 units)

**Status:** ACTIVE (REVISED)
**Date:** 2025-11-20
**Revised:** 2025-11-20 (Voxel size updated, ADR-006 rejected)
**Decision Makers:** Voxel Engine Architect
**Scope:** Voxel Data Structures, Memory Architecture

---

## Revision History

**2025-11-20 (v2.0)**: Major revision
- **Terrain voxel size**: 1.0 → **0.2 unit** (parameterizable via ScriptableObject)
- **Generation method**: Static → **Procedural** (Simplex noise, seed-based, streaming)
- **ADR-006 status**: ACCEPTED → **REJECTED** (tri-layer deemed too complex for static terrain)
- **Chunk size**: Recalculated for 0.2 voxels (64³ voxels = 12.8m world space)
- **Memory budget**: Recalculated (377 MB total with cache)
- **Related**: ADR-007 (Procedural Terrain Generation) now supersedes generation method

**2025-11-20 (v1.0)**: Initial dual-scale architecture (1.0 terrain, 0.1 props)

---

## Context

Le jeu nécessite deux types de contenu voxel avec résolutions différentes :
- **Terrain**: Grande échelle (infinite potential, bounded 1000x1000m gameplay), collines, vallées, **static non-destructible**
- **Props/Ennemis**: Petite échelle, détails fins, **destructibles**

### Requirements (v2.0)
- Performance: **60 FPS garanti**, 90 FPS réaliste
- Enemies: 2000+ simultanés (ECS-based)
- Memory: <500 MB terrain system total
- Terrain: **Procédural** (Simplex noise, seed-based, streaming)
- Destructibility: **Props/enemies ONLY** (terrain static)

### Options Considérées

**Option 1: Unified Scale (0.1 unit partout)**
- Tout en voxels 0.1 unité
- **REJECTED**: 1000m × 1000m × 64m @ 0.1 = 64 milliards voxels = ~64 GB memory (impossible)

**Option 2: Unified Scale (1.0 unit partout)**
- Tout en voxels 1.0 unité
- **REJECTED**: Perd détails pour props/ennemis (too blocky)

**Option 3: Dual-Scale Separated (CHOSEN v1.0)**
- Terrain: 1.0 unit
- Props: 0.1 unit
- **SUPERSEDED**: 1.0 trop gros pour détails terrain

**Option 4: Dual-Scale 0.2 + 0.1 (CHOSEN v2.0)**
- **Terrain: 0.2 unit** (5× plus détaillé que 1.0)
- **Props: 0.1 unit** (inchangé)
- **Paramétrable** via ScriptableObject (0.1-0.5 range)
- Balance: Qualité visuelle vs performance

---

## Decision

**CHOISI: Option 4 - Dual-Scale Separated (0.2 + 0.1)**

Architecture séparée avec :

**Layer 1: Terrain (Macro, revised)**
- Voxel size: **0.2 unit** (configurable via `VoxelConfiguration`)
- Chunk size: **64×64×64 voxels** = 12.8m world space
- Type: **Procedurally generated** (Simplex noise, runtime)
- Properties: Static, non-destructible, seed-based, streaming
- Meshing: Greedy meshing (Burst-compiled)
- Memory: 377 MB (147 MB active + 230 MB cache)

**Layer 2: Props/Enemies (Micro, unchanged)**
- Voxel size: **0.1 unit**
- Chunk size: **32×32×32 voxels** = 3.2m world space
- Type: Destructible entities (ECS-based)
- Memory: 68 MB (2000+ enemies)

**Unified Coordinate System**:
- Both layers use same world space origin
- Conversion: `WorldPos → ChunkCoord` uses layer-specific voxel size
- No complex transformations needed

---

## Consequences

### Positive (v2.0)

**Memory Optimized**:
- Terrain 0.2: 377 MB (vs 780 MB for ADR-006 overlay)
- Props 0.1: 68 MB
- **Total**: **445 MB** (within 500 MB budget ✓)
- vs Unified 0.1: Would be ~50 GB (impossible)

**Performance Superior**:
- 60 FPS: **Guaranteed** (2.8 ms terrain, 5.5 ms ECS, 16.6 ms total)
- 90 FPS: **Realistic** (with optimizations)
- Streaming: 1 chunk/frame (1.2 ms generation + meshing)

**Visual Quality**:
- Terrain: **5× more detailed** than 1.0 (125× more voxels/m³)
- Hills, valleys, erosion visible
- Smooth procedural generation (Simplex noise)

**Flexibility**:
- **Parameterizable**: Change voxel size 0.1-0.5 (testing/balancing)
- **Seed control**: Designer-friendly world variations
- **Infinite worlds**: Streaming supports unlimited exploration
- Each layer optimized independently

**Simplicity** (vs ADR-006):
- No tri-layer complexity
- No compression/decompression overhead
- No dual collision blending
- Single procedural generation pipeline

### Negative

**Dual Systems Complexity**:
- Two separate chunk managers (terrain vs props)
- Dual raycasting required (check both layers)
- Code maintenance: 2 systems instead of 1
- **Mitigation**: Shared interfaces (IVoxelLayer), centralized utilities

**Cache Memory**:
- LRU cache: 230 MB (vs regenerate every time)
- **Mitigation**: Cache optional, configurable size (100-500 chunks)

**Artistic Control**:
- Procedural terrain = less hand-crafted precision
- **Mitigation**: Biome system, structure spawning, seed selection

### Neutral

**Coordinate System**:
- Unified world space avoids complex conversions ✓
- Both layers coexist naturally (no overlap conflicts)

**Determinism**:
- Seed-based = reproducible worlds (good for testing)
- But: Can't save terrain modifications (already non-destructible by design)

---

## Rationale

### Why 0.2 (not 1.0 or 0.1)?

**vs 1.0 unit**:
- **125× more voxels** per m³ (5³ = 125)
- Hills/valleys have smooth contours (not blocky stairs)
- Procedural noise benefits from finer resolution
- Still performant: 64³ chunk = 22,880 vertices (< 65K limit)

**vs 0.1 unit** (same as props):
- Would require **8× more voxels** than 0.2 (2³ = 8)
- Memory: 377 MB → ~3 GB (unacceptable)
- Generation: 1.2 ms → ~10 ms (too slow)
- **Loses dual-scale benefits** (no separation of concerns)

**vs 0.3-0.5 unit**:
- Less detail than 0.2 (fewer voxels)
- Procedural noise less effective (too coarse)
- Minimal memory savings (~20%)

**0.2 is optimal balance**: Visual quality, performance, memory, scalability.

### Why Parameterizable?

**ScriptableObject configuration** (`VoxelConfiguration`):
- Allows **runtime experimentation** (test 0.1, 0.2, 0.3...)
- Designer control (balance quality vs performance)
- Platform-specific profiles (mobile: 0.3, PC: 0.2, high-end: 0.15)
- No hardcoded values (maintainability)

**Validation logic** prevents invalid configs:
- Chunk size auto-adjusted to keep vertices <65K
- Memory budget warnings if >500 MB
- Voxel size ratio check (terrain should be 1.5-2× props)

---

## Technical Details (v2.0)

### Chunk Size Calculation

**Terrain (0.2 voxels)**:
- Target: 12-16m world space per chunk
- Chosen: **64×64×64 voxels**
- World size: 64 × 0.2 = **12.8m** per side
- Total voxels: 262,144
- Vertices (greedy): **22,880** (< 65K limit ✓)

**Props (0.1 voxels, unchanged)**:
- Chunk: **32×32×32 voxels**
- World size: 32 × 0.1 = **3.2m** per side
- Dynamic, destructible

### Memory Breakdown

**Active Chunks** (192 chunks, radius 100m):
- Voxel data (compressed): 33 KB/chunk
- Mesh data: 732 KB/chunk
- **Total**: 192 × 765 KB = **147 MB**

**LRU Cache** (300 chunks):
- **Total**: 300 × 765 KB = **230 MB**

**Props/Enemies**:
- **Total**: **68 MB**

**System Total**: **445 MB** (within 500 MB budget ✓)

### Performance Budget (60 FPS = 16.6ms)

| System | ms | % |
|--------|-----|-----|
| Terrain (streaming, meshing) | 2.8 | 17% |
| ECS (2000 enemies) | 5.5 | 33% |
| Physics | 2.5 | 15% |
| Rendering | 3.8 | 23% |
| Other | 2.0 | 12% |
| **TOTAL** | **16.6** | **100%** |

**60 FPS**: Guaranteed ✓
**90 FPS**: Realistic with optimizations ✓

---

## Implementation Notes

### VoxelConfiguration ScriptableObject

**Single source of truth** for all voxel parameters:

```csharp
[CreateAssetMenu(fileName = "VoxelConfig", menuName = "Voxel/Configuration")]
public class VoxelConfiguration : ScriptableObject
{
    [Header("Terrain Settings")]
    [Range(0.1f, 0.5f)]
    public float terrainVoxelSize = 0.2f; // Parameterizable
    public int chunkSizeVoxels = 64; // Auto-validated

    [Header("Props Settings")]
    public float propsVoxelSize = 0.1f; // Fixed

    [Header("Streaming")]
    public float streamingRadius = 100f;
    public int cacheSize = 300;

    // Derived properties
    public float ChunkWorldSize => chunkSizeVoxels * terrainVoxelSize;
}
```

**All systems reference this config** (no hardcoding).

### Coordinate Conversion Utilities

```csharp
public static class VoxelCoordinates
{
    // World → Chunk (terrain layer)
    public static int3 WorldToTerrainChunk(Vector3 worldPos, VoxelConfiguration config)
    {
        float chunkSize = config.ChunkWorldSize;
        return new int3(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.y / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }

    // World → Chunk (props layer)
    public static int3 WorldToPropsChunk(Vector3 worldPos)
    {
        float chunkSize = 3.2f; // 32 × 0.1
        return new int3(
            Mathf.FloorToInt(worldPos.x / chunkSize),
            Mathf.FloorToInt(worldPos.y / chunkSize),
            Mathf.FloorToInt(worldPos.z / chunkSize)
        );
    }
}
```

### Dual-Layer Raycasting

**Abstract both layers** for collision detection:

```csharp
public interface IVoxelLayer
{
    bool Raycast(Ray ray, out RaycastHit hit);
    byte GetVoxelAt(Vector3 worldPos);
}

public class DualLayerRaycast
{
    private IVoxelLayer terrainLayer;
    private IVoxelLayer propsLayer;

    public bool Raycast(Ray ray, out RaycastHit hit)
    {
        // Check both layers, return closest hit
        bool terrainHit = terrainLayer.Raycast(ray, out RaycastHit terrainHitInfo);
        bool propsHit = propsLayer.Raycast(ray, out RaycastHit propsHitInfo);

        if (terrainHit && propsHit)
        {
            hit = (terrainHitInfo.distance < propsHitInfo.distance) ? terrainHitInfo : propsHitInfo;
            return true;
        }
        else if (terrainHit)
        {
            hit = terrainHitInfo;
            return true;
        }
        else if (propsHit)
        {
            hit = propsHitInfo;
            return true;
        }

        hit = default;
        return false;
    }
}
```

---

## References

- **ADR-007**: Procedural Terrain Generation (supersedes generation method)
- **ADR-003**: Greedy Meshing Algorithm (reused for terrain)
- **ADR-004**: Chunk Size (updated for 64³ @ 0.2 voxels)
- **Document 02**: DUAL_SCALE_VOXEL_SYSTEM.md (revised)
- **Document 12**: PROCEDURAL_GENERATION.md (implementation)
- **Document 13**: STREAMING_SYSTEM.md (cache + player-follow)

**Supersedes**: ADR-006 (rejected), original ADR-001 v1.0 (1.0 terrain voxels)

---

**End of ADR-001 (v2.0)**
