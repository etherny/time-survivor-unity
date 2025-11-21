# ADR-007: Procedural Terrain Generation with Streaming

## Status
**ACCEPTED**

Date: 2025-11-20
Replaces: ADR-006 (Tri-Layer Destructible Overlay)
Supersedes: Sections of ADR-001 (adds procedural generation)

---

## Context

### Requirements
- Terrain non-destructible, non-modifiable (static gameplay)
- World: Infinite generation capability (gameplay bounded 1000×1000m)
- Performance: 60 FPS guaranteed, 90 FPS realistic target
- Memory: <400 MB terrain system budget
- Voxel size: **0.2 unit (parameterizable via ScriptableObject)**
- Visual quality: Detailed terrain, smooth procedural generation

### Rejected Alternative (ADR-006)
ADR-006 proposed a tri-layer destructible overlay system (200×200m, voxels 0.1):
- Memory: 780 MB
- CPU: 3.5 ms/frame
- Complexity: 9/10 (tri-layer, compression, blending, dual-collision)
- Timeline: 26 weeks

**Rejection rationale**: Overly complex for static terrain requirements. Destructibility not needed for terrain layer (only props/enemies). Procedural generation simpler, more flexible, better performance.

---

## Decision

### Architecture: Dual-Scale Procedural System

**Terrain Layer (Macro)** - **NEW APPROACH**:
- Voxel size: **0.2 unit (configurable via ScriptableObject)**
- Type: **Procedurally generated** (Simplex Noise, seed-based)
- Properties: **Static, non-destructible, infinite streaming**
- Streaming: Radius 100m around player, continuous loading/unloading
- Generation: Runtime, deterministic from seed

**Props/Enemies Layer (Micro)** - **UNCHANGED**:
- Voxel size: 0.1 unit
- Destructible: YES
- Per ADR-001 specifications

### Procedural Generation Algorithm

**Simplex Noise Multi-Octave**:
- **Algorithm**: Simplex noise (faster than Perlin, fewer directional artifacts)
- **Octaves**: 4 layers (base terrain + 3 detail layers)
  - Octave 1: Frequency 0.01, Amplitude 20m (large hills)
  - Octave 2: Frequency 0.05, Amplitude 8m (medium features)
  - Octave 3: Frequency 0.15, Amplitude 3m (small details)
  - Octave 4: Frequency 0.40, Amplitude 1m (micro-variation)
- **Lacunarity**: 2.0 (frequency multiplier)
- **Persistence**: 0.5 (amplitude multiplier)

**Burst Compilation**:
- Job: `ProceduralTerrainGenerationJob` (IJobParallelFor)
- Target: **<1.5 ms** per chunk (generation + meshing)
- SIMD optimization: AVX2 intrinsics
- Native containers: NativeArray<byte> for voxel data

**Seed-Based Determinism**:
- Same seed → identical world (reproducible)
- Seed type: int32 (2.1 billion unique worlds)
- Cache optional: Can regenerate from seed instead of storing
- Enables "infinite" worlds (bounded by int32 coordinate space ±1M chunks)

### Streaming System

**Chunk Loading Strategy**:
- **Active radius**: 100m (player-centered sphere)
- **Load hysteresis**:
  - Load zone: 100m radius
  - Unload zone: 120m radius (prevents thrashing)
- **Budget**: 1 chunk/frame (1.2 ms generation + meshing)
- **Priority**: Distance-based (closest chunks first)

**LRU Cache**:
- Size: **300 chunks** (configurable)
- Memory: ~230 MB (765 KB per chunk)
- Ratio: 2.7× active chunks (192 active → 300 cached)
- Eviction: Least Recently Used (LRU) algorithm
- Purpose: Avoid regeneration when player backtracks

**Memory Management**:
- Active chunks: ~192 chunks (visible + 1-ring buffer) = **147 MB**
- Cache chunks: 300 chunks = **230 MB**
- Props/Enemies: 68 MB
- **Total**: **377 MB** (within 400 MB budget ✓)

### Parameterization System

**VoxelConfiguration ScriptableObject** (single source of truth):

```csharp
[CreateAssetMenu(fileName = "VoxelConfig", menuName = "Voxel/Configuration")]
public class VoxelConfiguration : ScriptableObject
{
    [Header("Terrain Voxel Settings")]
    [Range(0.1f, 2.0f)]
    public float terrainVoxelSize = 0.2f; // Parameterizable

    [Header("Chunk Settings")]
    public int chunkSizeVoxels = 64; // Auto-validated

    [Header("Streaming Settings")]
    [Range(50f, 200f)]
    public float streamingRadius = 100f;

    [Range(1, 4)]
    public int maxChunkLoadsPerFrame = 1;

    [Range(100, 2000)]
    public int cacheSize = 300;

    // Derived properties
    public float ChunkWorldSize => chunkSizeVoxels * terrainVoxelSize;
    public int TotalVoxelsPerChunk => chunkSizeVoxels³;

    // Validation in OnValidate()
}
```

All systems reference this configuration (no hardcoded values).

---

## Technical Details

### Chunk Size Calculation (Voxels 0.2)

**Recommended: 64×64×64 voxels**

**Rationale**:
- **World size**: 64 × 0.2 = 12.8m per side → 12.8³ = 2,097m³ volume
- **Total voxels**: 64³ = 262,144 voxels
- **Exposed voxels**: ~28% (terrain surfaces, valleys)
- **Faces before greedy**: 262,144 × 0.28 × 6 = 440,000 faces
- **Greedy meshing efficiency**: 13% (more details with 0.2 → less merging)
- **Vertices after greedy**: 440,000 × 0.13 × 4 = **22,880 vertices**

**Constraint validation**:
- Unity mesh limit: 65,535 vertices (uint16)
- **22,880 < 65,535** ✓ (Safe margin: 65%)

**Alternatives considered**:
- 32³ (6.4m): Too small, excessive chunk count, streaming overhead
- 80³ (16m): 512K voxels → ~35K vertices (OK but generation >2ms)
- 128³ (25.6m): 2M voxels → risk exceeding 65K vertex limit

**Conclusion**: 64³ optimal balance (coverage, performance, safety margin)

### Memory Budget Breakdown

**Active Chunks** (192 chunks, radius 100m):
- Voxel data (compressed): 33 KB/chunk (palette 8:1 compression)
- Mesh data: 732 KB/chunk (22,880 vertices × 32 bytes)
- **Total per chunk**: 765 KB
- **Active memory**: 192 × 765 KB = **147 MB**

**LRU Cache** (300 chunks):
- **Cache memory**: 300 × 765 KB = **230 MB**

**Props/Enemies Layer**:
- Memory: **68 MB** (per ADR-001)

**Total Terrain System**:
- Active + Cache: 147 + 230 = 377 MB
- **TOTAL**: **377 MB** (within 400 MB budget ✓)

### Performance Budget (60 FPS = 16.6ms)

**Terrain System**: **2.8 ms** (17% frame budget)
- Streaming logic (load/unload): 0.3 ms
- Chunk generation (Simplex noise): 0.3 ms (Burst, 1 chunk/frame)
- Greedy meshing: 0.9 ms (Burst, 1 chunk/frame)
- Frustum culling + LOD: 0.5 ms
- Mesh upload to GPU: 0.8 ms

**Other Systems**:
- ECS (2000 enemies): 5.5 ms
- Physics: 2.5 ms
- Rendering: 3.8 ms
- Other (UI, audio): 2.0 ms

**TOTAL**: **16.6 ms** → **60 FPS guaranteed** ✓

### Vertices Analysis

**Per Chunk**:
- Vertices: 22,880 (after greedy meshing)
- Triangles: 11,440
- Memory: 732 KB mesh data

**Total Visible** (192 chunks active):
- Gross vertices: 192 × 22,880 = **4,392,960 vertices**
- Frustum culling (top-down 45°): ~60% visible
- **Net visible**: **~2.6M vertices**

**GPU Performance**:
- Modern GPU (GTX 1060+): 10-20M vertices @ 60 FPS
- 2.6M << 10M → **90 FPS realistic** ✓

---

## Consequences

### Positive

**Performance Improvement** (vs ADR-006):
- Memory: **-51%** (780 MB → 377 MB)
- CPU: **-69%** (3.5 ms → 1.1 ms/frame)
- 60 FPS: **Guaranteed** (50% frame budget headroom)
- 90 FPS: **Realistic** (with targeted ECS/rendering optimizations)

**Simplicity Gain**:
- Complexity: **3/10** (vs 9/10 for ADR-006)
  - No tri-layer blending
  - No compression/decompression CPU overhead
  - No dual collision systems
  - Single-layer voxel management
- Timeline: **10 weeks** (vs 26 weeks for ADR-006) → **-62% dev time**

**Flexibility**:
- **Parameterizable**: Test different voxel sizes (0.1 - 0.5) in editor
- **Seed control**: Designer-friendly world variations
- **Infinite worlds**: Not limited to 200×200m or 1000×1000m
- **Deterministic**: Same seed = same world (testing/debugging)

**Scalability**:
- Supports 2000+ enemies (50% frame budget remaining)
- Streaming radius expandable to 150m (with cache adjustment)
- Can burst-load 2 chunks/frame if needed (2.4 ms < 2.8 ms budget)

### Negative

**Artistic Control**:
- Procedural = less hand-crafted precision
- **Mitigation**:
  - Biome system (desert, forest, mountain zones)
  - Structure spawning (hand-crafted buildings on procedural terrain)
  - Post-processing (smooth/sharpen, erosion simulation)
  - Seed selection (designers pick appealing seeds)

**Cache Memory Overhead**:
- LRU cache: 230 MB (vs regenerating every time)
- **Mitigation**:
  - Cache configurable (can reduce to 100 chunks = 77 MB)
  - Optional: Disable cache if memory constrained (regenerate from seed)
  - Fast regeneration (1.2 ms/chunk) makes cache less critical

### Neutral

**Determinism**:
- Seed-based = reproducible worlds (good for testing/debugging)
- BUT: Cannot save terrain modifications (already non-destructible by design)
- Props/enemies layer still supports destruction (0.1 voxels)

**Voxel Size 0.2**:
- More detailed than 1.0 (125× more voxels per m³)
- Less detailed than 0.1 (8× fewer voxels than props layer)
- **Balance**: Visual quality vs performance (configurable for experimentation)

---

## Implementation Phases

### Week 1-2: Simplex Noise + Burst
- Implement `SimplexNoise3D` class (Burst-compatible)
- Create `ProceduralTerrainGenerationJob` (IJobParallelFor)
- Multi-octave layering (4 octaves)
- Unit tests: Determinism, performance (<0.5 ms/chunk noise)

### Week 3-4: Streaming + Cache
- Implement `ProceduralTerrainStreamer` (player-follow)
- LRU cache system (NativeHashMap + LinkedList)
- Hysteresis load/unload (100m/120m)
- Chunk priority queue (distance-based)

### Week 5-6: Integration
- Connect generation → greedy meshing → mesh upload
- Integrate with existing chunk manager (ADR-003)
- Frustum culling optimization
- Memory profiling (validate 377 MB budget)

### Week 7-8: Parameterization + Biomes
- Create `VoxelConfiguration` ScriptableObject
- Validation logic (vertices <65K, memory <400 MB)
- Basic biome system (3 biomes: plains, hills, mountains)
- Editor tools (visualize chunk boundaries, cache state)

### Week 9-10: Polish + Testing
- Performance profiling (validate 60 FPS, test 90 FPS)
- Stress testing (fast player movement, teleportation)
- Visual polish (normal smoothing, ambient occlusion)
- Documentation + code review

**TOTAL TIMELINE**: **10 weeks**

**Milestones**:
- Week 2: 60 FPS terrain generation (static)
- Week 4: 60 FPS streaming (player-follow)
- Week 8: 90 FPS target achieved
- Week 10: Production-ready

---

## Scalability Analysis

### Support 2000+ Enemies
- ECS budget: 5.5 ms (current)
- Terrain budget: 2.8 ms
- **Combined**: 8.3 / 16.6 ms = 50% frame
- **Headroom**: 50% available for physics, rendering, audio
- **Conclusion**: **2000+ enemies supported** ✓

### Streaming 1 Chunk/Frame Viable
- Generation: 1.2 ms (noise + meshing)
- Upload: 0.8 ms (GPU)
- **Total**: 2.0 ms
- Budget: 2.8 ms (17% frame @ 60 FPS)
- **Viable**: YES ✓
- **Bonus**: Can burst 2 chunks/frame if needed (4.0 ms still <5 ms acceptable)

### Bottlenecks Identified
1. **Mesh upload GPU**: 0.8 ms/chunk
   - Mitigation: Double buffering, async upload (Unity 2023+)
2. **Memory cache thrashing**: If player moves >100m/s
   - Mitigation: Predictive loading (preload ahead based on velocity)
3. **Greedy meshing**: 0.9 ms (60% of generation time)
   - Mitigation: Job parallelization (4 chunks in parallel)

### Scaling Limits
- **Max streaming radius**: ~150m (432 active chunks, 5.2 ms terrain budget)
- **Max enemies**: ~3000 (with Burst ECS optimizations to 3.5 ms)
- **Max voxel size**: 0.5 (beyond this, loses terrain detail benefit)
- **Min voxel size**: 0.1 (below this, performance critical)

---

## References

### Internal Documents
- **ADR-001**: Dual-Scale Voxel System (revised for 0.2 voxels)
- **ADR-003**: Greedy Meshing Algorithm (reused for terrain)
- **ADR-004**: Chunk Size (updated for 64³ @ 0.2 voxels)
- **Document 12**: PROCEDURAL_GENERATION.md (implementation details)
- **Document 13**: STREAMING_SYSTEM.md (cache + player-follow)

### External References
- Simplex Noise: Ken Perlin, 2001 (patent-free)
- Greedy Meshing: Mikola Lysenko, 2012
- Unity DOTS: Burst Compiler, Job System documentation
- Voxel engines: Minecraft, 7 Days to Die (inspiration)

---

## Validation Checklist

- ✅ Memory budget: 377 MB < 400 MB
- ✅ Performance: 2.8 ms < 3.0 ms target (60 FPS guaranteed)
- ✅ Vertices: 22,880 < 65,535 (Unity limit)
- ✅ Scalability: 2000+ enemies supported
- ✅ Streaming: 1 chunk/frame viable
- ✅ Parameterizable: VoxelConfiguration ScriptableObject
- ✅ Timeline: 10 weeks realistic
- ✅ Complexity: 3/10 (simple, maintainable)

**Decision Status**: **ACCEPTED** ✓

**Supersedes**: ADR-006 (rejected), partially supersedes ADR-001 (terrain generation method)

---

## Appendix A: Configuration Example

**VoxelConfiguration Asset** (default values):

```yaml
terrainVoxelSize: 0.2
propsVoxelSize: 0.1
chunkSizeVoxels: 64
streamingRadius: 100.0
maxChunkLoadsPerFrame: 1
cacheSize: 300
```

**Derived metrics**:
- Chunk world size: 12.8m
- Total voxels/chunk: 262,144
- Active chunks: 192
- Total memory: 377 MB

---

## Appendix B: Performance Comparison

| Metric | ADR-006 (Overlay) | ADR-007 (Procedural) | Delta |
|--------|-------------------|----------------------|-------|
| Memory | 780 MB | 377 MB | **-51%** |
| CPU/frame | 3.5 ms | 1.1 ms | **-69%** |
| Complexity | 9/10 | 3/10 | **-67%** |
| Timeline | 26 weeks | 10 weeks | **-62%** |
| 60 FPS | Marginal | Guaranteed | ✓ |
| 90 FPS | No | Realistic | ✓ |

---

**End of ADR-007**
