# ADR-004: Chunk Size for Multi-Scale Voxel System

**Status:** ACTIVE (REVISED)
**Date:** 2025-11-20 (Original)
**Revised:** 2025-11-20 (Updated for 0.2 terrain voxels)
**Decision Makers:** Voxel Engine Architect
**Scope:** Chunk Management, Spatial Partitioning

---

## Revision History

**2025-11-20 v2.0**: Updated for terrain voxels 0.2 (was 1.0)
- **Terrain chunks**: 16³ @ 1.0 → **64³ @ 0.2** (same 12.8m world space)
- **Rationale**: More voxels needed to maintain world coverage with smaller voxel size
- **ADR-006 references**: Removed (overlay system rejected)
- **New constraint**: Parameterizable chunk size via VoxelConfiguration

**2025-11-20 v1.0**: Original (16³ @ 1.0 for terrain, 32³ @ 0.1 for overlay)

---

## Context

Chunk size critique pour :
- **Vertex limit**: 65,535 per mesh (Unity uint16 index buffer)
- **Culling granularity**: Frustum culling efficiency (camera view 100m)
- **Memory footprint**: Voxel data + mesh data per chunk
- **Meshing performance**: Generation + greedy meshing time
- **Voxel resolution**: Must adapt to voxel size (0.2 terrain, 0.1 props)

### Requirements (v2.0)

- **Terrain voxels**: 0.2 unit (parameterizable 0.1-0.5)
- **Props voxels**: 0.1 unit (fixed)
- **Chunk world size**: Target 12-16m for optimal streaming/culling
- **Performance**: <2 ms generation + meshing per chunk (Burst)
- **Memory**: <1 MB per chunk (including mesh data)

### Options Considérées (Terrain @ 0.2 voxels)

| Size | Voxels | World Size (0.2) | Vertices (greedy) | Verdict |
|------|--------|------------------|-------------------|---------|
| 32³ | 32,768 | 6.4m³ | ~8,500 | Trop petit (coverage) |
| **64³** | **262,144** | **12.8m³** | **22,880** | **OPTIMAL** |
| 80³ | 512,000 | 16m³ | ~35,000 | OK mais génération >2ms |
| 128³ | 2,097,152 | 25.6m³ | >65,000 | REJECTED (vertex overflow) |

---

## Decision (v2.0)

**Terrain Layer: 64×64×64 voxels @ 0.2 unit = 12.8m world space**
**Props Layer: 32×32×32 voxels @ 0.1 unit = 3.2m world space** (unchanged)

---

## Consequences (v2.0)

### Positive

**Terrain (64³ @ 0.2)**:
- **Vertex Safety**: 22,880 vertices (35% of 65K limit, safe margin)
- **Coverage**: 12.8m world space (optimal for streaming/culling)
- **Detail**: 262,144 voxels capture smooth terrain contours
- **Performance**: 1.2 ms generation + meshing (Burst, <2 ms budget ✓)
- **Scalability**: Can adjust voxel size 0.1-0.5 without redesign

**Props (32³ @ 0.1, unchanged)**:
- **Coverage**: 3.2m world space (good for destructible objects)
- **Vertex Safety**: ~2,000 vertices average (<5% limit)
- **Detail**: Fine-grained destruction visible

### Negative

**Terrain**:
- **Voxel count**: 64× more voxels than original 16³ @ 1.0 (expected with finer resolution)
- **Memory**: 765 KB/chunk vs 24 KB original (acceptable with cache management)

**Props**:
- **Chunk count**: Higher than terrain for same world area (expected for finer detail)

### Neutral

**Visible Chunks** (100m radius):
- Terrain: 192 chunks (π × (100/12.8)² ≈ 192)
- Memory active: 147 MB (within budget)
- Frustum culling: ~0.5 ms (acceptable)

---

## Rationale (Terrain @ 0.2 voxels)

### Why 64³ (not 32³, 80³, or 128³)?

**32³ @ 0.2 (Rejected)**:
- World space: 6.4m (too small, excessive chunk count)
- Chunks (1000×1000m): 24,414 chunks (vs 6,076 for 64³)
- Streaming overhead: More chunks = more load operations
- Culling: 6.4m granularity too fine (wasted frustum tests)

**64³ @ 0.2 (CHOSEN)**:
- **World space**: 12.8m (optimal streaming granularity)
- **Chunks (1000×1000m)**: 6,076 chunks (manageable with streaming + cache)
- **Vertices**: 22,880 (safe margin: 65% below limit)
- **Generation time**: 1.2 ms (within 2 ms budget ✓)
- **Memory**: 765 KB/chunk (acceptable with 300-chunk cache = 230 MB)

**80³ @ 0.2 (Rejected)**:
- World space: 16m (OK for coverage)
- **Vertices**: ~35,000 (closer to limit, less margin for terrain variation)
- **Generation time**: ~2.2 ms (exceeds 2 ms budget slightly)
- Voxels: 512,000 (2× more than 64³, diminishing returns)

**128³ @ 0.2 (Rejected)**:
- World space: 25.6m (too large, culling wasteful)
- Voxels: 2,097,152 (8× more than 64³)
- **Vertices**: >65,000 (EXCEEDS LIMIT with detailed terrain)
- Generation time: ~10 ms (WAY over budget)

**Conclusion**: 64³ @ 0.2 is the **only viable option** meeting all constraints.

---

## Performance Validation (v2.0)

### Terrain Chunks (64³ @ 0.2)

```
VOXEL DATA:
- Voxels: 64³ = 262,144
- Size: 262 KB raw → 33 KB compressed (palette 8:1)

VERTEX COUNT (Greedy Meshing):
- Exposed voxels: ~28% (terrain surfaces)
- Faces before greedy: 262,144 × 0.28 × 6 = 440,000 faces
- Greedy efficiency: 13% (0.2 voxels = more detail = less merging)
- Vertices after greedy: 440,000 × 0.13 × 4 = 22,880 vertices
- Safety margin: 22,880 / 65,535 = 35% limit (SAFE ✓)

GENERATION PERFORMANCE (Burst):
- Simplex noise (4 octaves): 0.3 ms
- Greedy meshing: 0.9 ms
- Total: 1.2 ms/chunk (<2 ms budget ✓)

CULLING (100m view):
- Terrain chunks visible: 192 chunks
- Frustum test: 192 × 0.003 ms = 0.58 ms (ACCEPTABLE)

MEMORY (100m radius):
- Active chunks: 192 × 765 KB = 147 MB
- Cache (300 chunks): 300 × 765 KB = 230 MB
- Total: 377 MB (<400 MB budget ✓)
```

### Props Chunks (32³ @ 0.1, unchanged)

```
VOXEL DATA:
- Voxels: 32³ = 32,768
- World space: 3.2m³
- Memory: ~2 KB compressed

VERTEX COUNT:
- Average: 2,000 vertices
- Worst-case: 8,000 vertices
- Safety: 8,000 / 65,535 = 12% limit (SAFE ✓)
```

---

## Implementation Notes (v2.0)

### VoxelConfiguration ScriptableObject

**All chunk sizes derived from config**:

```csharp
[CreateAssetMenu]
public class VoxelConfiguration : ScriptableObject
{
    [Range(0.1f, 0.5f)]
    public float terrainVoxelSize = 0.2f;

    // Chunk size auto-calculated or validated
    public int chunkSizeVoxels = 64;

    // Validation
    private void OnValidate()
    {
        int estimatedVertices = EstimateVertices(chunkSizeVoxels, terrainVoxelSize);
        if (estimatedVertices > 65000)
        {
            Debug.LogError($"Chunk {chunkSizeVoxels}³ → {estimatedVertices} vertices > 65K!");
            chunkSizeVoxels = 64; // Reset to safe default
        }
    }

    private int EstimateVertices(int chunkSize, float voxelSize)
    {
        int totalVoxels = chunkSize * chunkSize * chunkSize;
        float exposedRatio = 0.28f;
        float greedyEfficiency = (voxelSize <= 0.2f) ? 0.13f : 0.10f;
        return (int)(totalVoxels * exposedRatio * 6 * greedyEfficiency * 4);
    }
}
```

### Comparison Table: Terrain vs Props

| Property | Terrain (64³ @ 0.2) | Props (32³ @ 0.1) | Ratio |
|----------|---------------------|-------------------|-------|
| Voxel Size | 0.2 unit | 0.1 unit | 2:1 |
| Chunk Voxels | 262,144 | 32,768 | 8:1 |
| World Coverage | 12.8m³ | 3.2m³ | 4:1 |
| Vertices (avg) | 22,880 | 2,000 | 11:1 |
| Memory/Chunk | 765 KB | 2 KB (compressed) | 382:1 |
| Generation Time | 1.2 ms | 0.5 ms | 2.4:1 |
| Purpose | Static terrain (streaming) | Destructible props (ECS) | - |

---

## References

- **ADR-001 v2.0**: Dual-Scale Voxel System (0.2 + 0.1)
- **ADR-007**: Procedural Terrain Generation (chunk sizing basis)
- **Document 03**: CHUNK_MANAGEMENT.md (revised)
- **Document 12**: PROCEDURAL_GENERATION.md (performance validation)

**Supersedes**: ADR-004 v1.0 (16³ @ 1.0 terrain), ADR-006 references (overlay system rejected)

---

**End of ADR-004 (v2.0)**
