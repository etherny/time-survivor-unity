# ADR-006: Hybrid Tri-Layer Voxel System with Destructible Overlay

**Status:** REJECTED
**Date:** 2025-11-20 (Proposed)
**Rejected:** 2025-11-20 (Same day rejection after architecture review)
**Decision Makers:** Voxel Engine Architect
**Scope:** Terrain Destructibility, Streaming Architecture, Memory Management

---

## Rejection Notice

**This ADR was REJECTED on the same day it was proposed** after comprehensive architectural analysis revealed that the solution was **over-engineered** for the actual gameplay requirements.

**Rejection Rationale:**
1. **Complexity**: Tri-layer system (9/10 complexity) for static terrain gameplay (unnecessary)
2. **Memory**: 780 MB (96% more than simpler procedural approach: 377 MB via ADR-007)
3. **Development Time**: 26 weeks vs 10 weeks for procedural static terrain
4. **Core Issue**: Terrain destructibility **not actually required** by gameplay (props/enemies destructible only)

**Replaced By:**
- **ADR-007**: Procedural Terrain Generation (static, non-destructible, 0.2 voxels)
- **ADR-001 v2.0**: Dual-Scale revised (0.2 terrain, 0.1 props)

**Key Insight:** A simpler dual-scale system with procedural static terrain (ADR-007) achieves all actual requirements (60 FPS, 2000+ enemies, visual quality) with:
- **-51% memory** (780 MB → 377 MB)
- **-62% dev time** (26 weeks → 10 weeks)
- **-67% complexity** (9/10 → 3/10)

**Status Summary:** Proposed → Analyzed → **REJECTED** (overengineered for needs)

---

## Context (Historical - Proposed Solution)

The original dual-scale architecture (ADR-001) separated terrain (macro 1.0) from props/enemies (micro 0.1), achieving excellent memory efficiency (514 MB). This proposal attempted to add **terrain destructibility with fine detail** near the player.

### Problem Statement

**Requirement:** Destructible terrain with 0.1 unit voxel resolution for detailed destruction.

**Constraint:** Full 1000x1000m world at 0.1 resolution is mathematically impossible:
- 10,000 x 640 x 10,000 = 64 billion voxels
- Memory: 64+ GB (unacceptable)
- Meshing: Multiple minutes (unplayable)

### Options Considered

**Option A: Full 1000x1000m Destructible @ 0.1**
- REJECTED: 10+ GB memory, impossible performance

**Option B: Limited Destructible Zone 200x200m @ 0.1 (CHOSEN)**
- Overlay zone follows player
- Streaming load/unload chunks
- Memory: 780 MB total (acceptable)

**Option C: Variable Resolution (LOD-based)**
- REJECTED: Complex blending, visual artifacts, 12+ weeks dev time

---

## Decision

**CHOSEN: Hybrid Tri-Layer Voxel System**

Architecture avec trois layers distinctes :

### Layer 1: Base Terrain (Static)
- **Scale**: Voxels 1.0 unit
- **Coverage**: Full 1000x1000m world
- **Chunks**: 16x16x16 voxels (16m³ world space)
- **Properties**: Static, non-destructible, generated once
- **Memory**: 380 MB (chunks + meshes)

### Layer 2: Destructible Overlay (Dynamic)
- **Scale**: Voxels 0.1 unit
- **Coverage**: 200x200m zone around player
- **Chunks**: 32x32x32 voxels (3.2m³ world space)
- **Properties**: Dynamic, destructible, streaming follows player
- **Memory**: 400 MB (active chunks + cache)

### Layer 3: Props/Enemies (Global)
- **Scale**: Voxels 0.1 unit
- **Coverage**: Spawned globally as needed
- **Chunks**: 32x32x32 voxels per entity
- **Properties**: Fully destructible, managed by ECS
- **Memory**: Unchanged from original architecture

---

## Consequences

### Positive

**Performance:**
- 60 FPS maintained with 2000+ enemies
- Streaming cost: 2ms/frame (amortized chunk loading)
- Frame budget impact: 3.5ms total overlay system (<21% frame)

**Memory:**
- Total terrain: 780 MB (380 base + 400 overlay)
- Well within 2 GB budget
- Compression (RLE+Palette) achieves 15:1 ratio

**Gameplay:**
- Detailed destruction exactly where player interacts
- Unlimited destruction persistence (serialized to disk)
- Smooth gameplay experience (destruction near player is 99% use case)

**Scalability:**
- Can adjust overlay zone size (200x200m → 300x300m if needed)
- Streaming proven technique (Minecraft, Valheim patterns)
- Extensible to multiplayer (synchronized overlay modifications)

### Negative

**Destruction Limitations:**
- Destruction limited to 200x200m active zone
- Areas outside overlay non-destructible (base terrain remains static)
- Visible streaming transition (~1.7s when moving fast)

**System Complexity:**
- Three distinct voxel systems to manage
- Blending logic base ↔ overlay (z-fighting mitigation)
- Dual collision system (overlay priority)

**Development Time:**
- +10 weeks development vs original dual-scale
- Streaming system implementation (4 weeks)
- Testing edge cases (player speed, memory spikes)

### Neutral

**Player Experience:**
- Destruction zone 200x200m covers typical gameplay area (camera view ~100m radius)
- Streaming transition imperceptible during normal play (walking speed)
- Only noticeable when teleporting/fast travel (2s loading acceptable)

---

## Technical Architecture

### Data Structures

#### DestructibleOverlayManager

```csharp
/// <summary>
/// Manages the 200x200m destructible overlay zone that follows the player.
/// Handles streaming, chunk lifecycle, and destruction modifications.
/// </summary>
public class DestructibleOverlayManager : MonoBehaviour
{
    // Player tracking
    private Transform playerTransform;
    private Vector3 lastPlayerPosition;

    // Active chunks in overlay zone
    private Dictionary<ChunkCoord, OverlayChunk32> activeChunks;

    // Streaming management
    private Queue<ChunkCoord> loadQueue;
    private Queue<ChunkCoord> unloadQueue;
    private const int MAX_CHUNKS_PER_FRAME = 1; // 2ms budget

    // Cache for recently unloaded chunks
    private LRUCache<ChunkCoord, CompressedChunkData> memoryCache;
    private const int CACHE_SIZE_MB = 100;

    // Distance thresholds
    private const float LOAD_RADIUS = 100f;   // 200x200m zone
    private const float UNLOAD_RADIUS = 120f; // 20m hysteresis

    void Update()
    {
        UpdatePlayerPosition();
        EvaluateStreamingNeeds();
        ProcessStreamingQueue();
    }

    /// <summary>
    /// Destroy a single voxel at world position.
    /// If overlay chunk not loaded, queues modification for later application.
    /// </summary>
    public void DestroyVoxel(Vector3 worldPos)
    {
        ChunkCoord coord = WorldToChunkCoord(worldPos);

        if (activeChunks.TryGetValue(coord, out var chunk))
        {
            Vector3Int localPos = WorldToLocalVoxel(worldPos);
            chunk.data.SetVoxel(localPos.x, localPos.y, localPos.z, VoxelType.Air);
            chunk.isDirty = true;
            ScheduleRemesh(coord);
        }
        else
        {
            // Queue for when chunk loads
            QueueModification(coord, worldPos, VoxelType.Air);
        }
    }

    /// <summary>
    /// Destroy voxels in sphere (explosion damage).
    /// </summary>
    public void DestroySphere(Vector3 center, float radius)
    {
        // Calculate affected chunks
        var affectedChunks = GetChunksInSphere(center, radius);

        foreach (var coord in affectedChunks)
        {
            if (activeChunks.TryGetValue(coord, out var chunk))
            {
                // Burst job for parallel voxel modification
                var job = new VoxelSphereDestructionJob
                {
                    center = center,
                    radius = radius,
                    voxelData = chunk.data.GetNativeArray(),
                    chunkOrigin = chunk.WorldOrigin
                };

                job.Schedule().Complete(); // Or queue for amortized processing
                chunk.isDirty = true;
                ScheduleRemesh(coord);
            }
        }
    }

    private void EvaluateStreamingNeeds()
    {
        Vector3 playerPos = playerTransform.position;
        ChunkCoord playerChunk = WorldToChunkCoord(playerPos);

        // Calculate required chunks in LOAD_RADIUS
        HashSet<ChunkCoord> requiredChunks = GetChunksInRadius(playerPos, LOAD_RADIUS);

        // Identify chunks to LOAD (required but not active)
        foreach (var coord in requiredChunks)
        {
            if (!activeChunks.ContainsKey(coord))
            {
                loadQueue.Enqueue(coord);
            }
        }

        // Identify chunks to UNLOAD (active but beyond UNLOAD_RADIUS)
        foreach (var kvp in activeChunks)
        {
            float distance = Vector3.Distance(ChunkCoordToWorld(kvp.Key), playerPos);
            if (distance > UNLOAD_RADIUS)
            {
                unloadQueue.Enqueue(kvp.Key);
            }
        }
    }

    private void ProcessStreamingQueue()
    {
        float budgetStart = Time.realtimeSinceStartup;
        const float BUDGET_MS = 2f;

        // Process LOAD queue
        while (loadQueue.Count > 0)
        {
            if ((Time.realtimeSinceStartup - budgetStart) * 1000f > BUDGET_MS)
                break; // Exceed budget, continue next frame

            ChunkCoord coord = loadQueue.Dequeue();
            LoadChunk(coord);
        }

        // Process UNLOAD queue (less critical, lower priority)
        while (unloadQueue.Count > 0 && (Time.realtimeSinceStartup - budgetStart) * 1000f < BUDGET_MS)
        {
            ChunkCoord coord = unloadQueue.Dequeue();
            UnloadChunk(coord);
        }
    }

    private void LoadChunk(ChunkCoord coord)
    {
        // Check memory cache first
        if (memoryCache.TryGet(coord, out var cachedData))
        {
            var chunk = new OverlayChunk32(coord, cachedData);
            GenerateMesh(chunk); // Greedy meshing ~1.5ms
            activeChunks[coord] = chunk;
            return;
        }

        // Check disk cache (persistent modifications)
        if (DiskCache.TryLoad(coord, out var diskData))
        {
            var chunk = new OverlayChunk32(coord, diskData);
            GenerateMesh(chunk);
            activeChunks[coord] = chunk;
            return;
        }

        // Initialize new chunk (copy from base terrain or generate)
        var newChunk = InitializeOverlayChunk(coord);
        GenerateMesh(newChunk);
        activeChunks[coord] = newChunk;
    }

    private void UnloadChunk(ChunkCoord coord)
    {
        if (!activeChunks.TryGetValue(coord, out var chunk))
            return;

        // Serialize if dirty (has modifications)
        if (chunk.isDirty)
        {
            DiskCache.Save(coord, chunk.data);
        }

        // Add to memory cache (LRU)
        memoryCache.Add(coord, chunk.data);

        // Release mesh memory
        if (chunk.mesh != null)
        {
            Destroy(chunk.mesh);
        }

        // Remove from active chunks
        activeChunks.Remove(coord);
    }
}
```

#### OverlayChunk32 Structure

```csharp
/// <summary>
/// Represents a 32x32x32 voxel chunk at 0.1 unit scale (3.2m world space).
/// Used for destructible overlay layer.
/// </summary>
public struct OverlayChunk32
{
    public ChunkCoord coord;
    public CompressedVoxelData data;
    public Mesh mesh;
    public bool isDirty;
    public float lastAccessTime;

    public Vector3 WorldOrigin => new Vector3(
        coord.x * 3.2f,
        coord.y * 3.2f,
        coord.z * 3.2f
    );

    public OverlayChunk32(ChunkCoord coord, CompressedVoxelData data)
    {
        this.coord = coord;
        this.data = data;
        this.mesh = null;
        this.isDirty = false;
        this.lastAccessTime = Time.realtimeSinceStartup;
    }
}
```

#### CompressedVoxelData (RLE + Palette)

```csharp
/// <summary>
/// Compressed voxel storage using Run-Length Encoding + Palette.
/// Typical compression: 32x32x32 = 32KB raw → 2KB compressed (15:1 ratio).
/// </summary>
public struct CompressedVoxelData
{
    // Palette: Maps voxel index (0-255) to VoxelType
    private byte[] palette;        // Max 256 unique types
    private int paletteCount;

    // RLE-encoded voxel data
    // Format: [paletteIndex, runLength] pairs
    private ushort[] rleData;
    private int rleLength;

    public CompressedVoxelData(int capacity = 1024)
    {
        palette = new byte[256];
        paletteCount = 0;
        rleData = new ushort[capacity];
        rleLength = 0;
    }

    /// <summary>
    /// Get voxel type at position (decompression on-demand).
    /// </summary>
    public byte GetVoxel(int x, int y, int z)
    {
        int index = x + y * 32 + z * 32 * 32;
        return DecompressVoxel(index);
    }

    /// <summary>
    /// Set voxel type at position (marks dirty, requires recompression).
    /// </summary>
    public void SetVoxel(int x, int y, int z, byte voxelType)
    {
        int index = x + y * 32 + z * 32 * 32;
        ModifyVoxel(index, voxelType);
        // Note: Recompression done in batch during UnloadChunk
    }

    /// <summary>
    /// Get compressed size in bytes.
    /// </summary>
    public int GetCompressedSize()
    {
        return paletteCount + (rleLength * sizeof(ushort));
    }

    /// <summary>
    /// Decompress to native array for Burst jobs.
    /// </summary>
    public NativeArray<byte> GetNativeArray(Allocator allocator)
    {
        var array = new NativeArray<byte>(32 * 32 * 32, allocator);
        DecompressToArray(array);
        return array;
    }

    /// <summary>
    /// Compress from native array (after Burst job modifications).
    /// </summary>
    public void CompressFromArray(NativeArray<byte> data)
    {
        // Build palette
        BuildPalette(data);

        // RLE encoding
        EncodeRLE(data);
    }

    private byte DecompressVoxel(int index)
    {
        // Walk RLE data to find voxel at index
        int currentIndex = 0;

        for (int i = 0; i < rleLength; i += 2)
        {
            byte paletteIndex = (byte)rleData[i];
            ushort runLength = rleData[i + 1];

            if (currentIndex + runLength > index)
            {
                // Found the run containing our voxel
                return palette[paletteIndex];
            }

            currentIndex += runLength;
        }

        return 0; // Air (fallback)
    }

    private void BuildPalette(NativeArray<byte> data)
    {
        // Count unique voxel types
        var uniqueTypes = new HashSet<byte>();
        for (int i = 0; i < data.Length; i++)
        {
            uniqueTypes.Add(data[i]);
        }

        // Build palette (limit 256 types)
        paletteCount = 0;
        foreach (byte type in uniqueTypes)
        {
            if (paletteCount >= 256) break;
            palette[paletteCount++] = type;
        }
    }

    private void EncodeRLE(NativeArray<byte> data)
    {
        rleLength = 0;

        byte currentVoxel = data[0];
        ushort runLength = 1;

        for (int i = 1; i < data.Length; i++)
        {
            if (data[i] == currentVoxel && runLength < ushort.MaxValue)
            {
                runLength++;
            }
            else
            {
                // Emit run
                byte paletteIndex = GetPaletteIndex(currentVoxel);
                rleData[rleLength++] = paletteIndex;
                rleData[rleLength++] = runLength;

                // Start new run
                currentVoxel = data[i];
                runLength = 1;
            }
        }

        // Emit final run
        byte finalIndex = GetPaletteIndex(currentVoxel);
        rleData[rleLength++] = finalIndex;
        rleData[rleLength++] = runLength;
    }

    private byte GetPaletteIndex(byte voxelType)
    {
        for (byte i = 0; i < paletteCount; i++)
        {
            if (palette[i] == voxelType)
                return i;
        }
        return 0; // Shouldn't happen if palette built correctly
    }
}
```

### Streaming Algorithm

#### Player-Follow Logic

```
EVERY FRAME (Update):
1. Get player position
2. Calculate required chunks (LOAD_RADIUS = 100m)
3. Identify chunks to LOAD (required but not active)
4. Identify chunks to UNLOAD (active but beyond UNLOAD_RADIUS = 120m)
5. Queue streaming operations (loadQueue, unloadQueue)
6. Process queues with 2ms frame budget

DISTANCE MANAGEMENT:
- LOAD_RADIUS: 100m (200x200m zone)
- UNLOAD_RADIUS: 120m (20m hysteresis prevents thrashing)
- Predictive loading: Preload chunks in player movement direction
```

#### Chunk Loading Pipeline

```
CHUNK LOADING (2ms budget per chunk):
1. Check memory cache (LRU)
   → If cached: Decompress RLE data (0.5ms)

2. Check disk cache (persistent modifications)
   → If cached: Load from disk, decompress (1.5ms)

3. Initialize new chunk
   → Option A: Copy from base terrain (sample 1.0 voxels → 0.1 overlay)
   → Option B: Generate fresh (Perlin noise, match base)

4. Generate mesh (greedy meshing, Burst)
   → ~1.5ms per chunk (32x32x32)

5. Activate chunk (add to activeChunks, enable rendering)

TOTAL LOAD TIME: ~2ms per chunk (within budget)
```

#### Chunk Unloading Pipeline

```
CHUNK UNLOADING (1ms budget per chunk):
1. Check if dirty (has modifications)
   → If dirty: Serialize to disk cache (0.5ms)

2. Add to memory cache (LRU, 100 MB capacity)
   → Compressed data (~2 KB per chunk)

3. Release mesh memory
   → Destroy Unity Mesh object

4. Remove from activeChunks dictionary

TOTAL UNLOAD TIME: ~1ms per chunk (fast)
```

#### Streaming Performance

```
ACTIVE ZONE: 200x200m = 6,250 chunks (32³ at 0.1 scale)

STREAMING TRANSITION TIME:
- Worst case: Player teleports across map
- Chunks to load: 6,250 chunks
- Load time: 6,250 × 2ms = 12,500ms = 12.5s
- Amortized: 1 chunk/frame @ 60 FPS = 104 seconds

OPTIMIZATION: Priority loading (visible chunks first)
- Visible chunks (frustum): ~100 chunks (camera view)
- Load visible first: 100 × 2ms = 200ms = 0.2s (acceptable)
- Background load rest: Amortized over 100 seconds (imperceptible)

RESULT: Player sees terrain in 0.2s, full zone loaded in background
```

### Blending Base ↔ Overlay

**Problem:** Z-fighting when overlay and base terrain occupy same space.

#### Solution A: Y-Offset (Simple)

```csharp
// Render overlay slightly above base terrain
Vector3 overlayPosition = basePosition + new Vector3(0, 0.01f, 0);
```

**Pros:** Simple, no shader changes
**Cons:** Visible gap in some angles, not seamless

#### Solution B: Shader Depth Bias

```glsl
// In vertex shader
gl_Position.z += depthBias; // Bias overlay toward camera
```

**Pros:** No visible gap, seamless
**Cons:** Requires custom shader, depth precision issues at distance

#### Solution C: Disable Base Terrain in Overlay Zone (RECOMMENDED)

```csharp
// When overlay chunk loads, disable base terrain rendering in that area
void LoadOverlayChunk(ChunkCoord overlayCoord)
{
    // Calculate corresponding base terrain chunk(s)
    var baseChunks = OverlayToBaseChunkMapping(overlayCoord);

    foreach (var baseCoord in baseChunks)
    {
        // Disable mesh rendering (keep collision)
        baseTerrainChunks[baseCoord].meshRenderer.enabled = false;
    }

    // Load and activate overlay chunk
    ActivateOverlayChunk(overlayCoord);
}

void UnloadOverlayChunk(ChunkCoord overlayCoord)
{
    // Unload overlay chunk
    DeactivateOverlayChunk(overlayCoord);

    // Re-enable base terrain rendering
    var baseChunks = OverlayToBaseChunkMapping(overlayCoord);
    foreach (var baseCoord in baseChunks)
    {
        baseTerrainChunks[baseCoord].meshRenderer.enabled = true;
    }
}
```

**Pros:** No z-fighting, perfect blending, no visual artifacts
**Cons:** Slightly more complex logic

### Collision System Dual-Layer

#### Raycast Priority

```csharp
/// <summary>
/// Raycast against voxel terrain, checking overlay first (if loaded).
/// </summary>
public bool VoxelRaycast(Ray ray, out VoxelHit hit, float maxDistance)
{
    // Check overlay first (destructible, higher priority)
    if (RaycastOverlay(ray, out hit, maxDistance))
    {
        return true;
    }

    // Fallback to base terrain
    if (RaycastBaseTerrain(ray, out hit, maxDistance))
    {
        return true;
    }

    hit = default;
    return false;
}

private bool RaycastOverlay(Ray ray, out VoxelHit hit, float maxDistance)
{
    // Get overlay chunks along ray path
    var chunks = GetOverlayChunksAlongRay(ray, maxDistance);

    foreach (var coord in chunks)
    {
        if (activeOverlayChunks.TryGetValue(coord, out var chunk))
        {
            if (DDAVoxelRaycast(ray, chunk.data, out hit))
            {
                return true;
            }
        }
    }

    hit = default;
    return false;
}
```

#### PhysX Integration

```
BASE TERRAIN:
- Layer: "TerrainStatic" (Layer 8)
- Collider: MeshCollider, static, baked once
- Physics: Non-destructible, optimized for static queries

OVERLAY CHUNKS:
- Layer: "TerrainDynamic" (Layer 9)
- Collider: MeshCollider per chunk, dynamic
- Update: On destruction (remesh → recalculate collider)
- Optimization: Simplified collision mesh (1/4 resolution)

COLLISION LAYERS MATRIX:
Layer              | TerrainStatic | TerrainDynamic | Player | Enemies
-------------------|---------------|----------------|--------|--------
TerrainStatic      | No            | No             | Yes    | Yes
TerrainDynamic     | No            | No             | Yes    | Yes
Player             | Yes           | Yes            | No     | Yes
Enemies            | Yes           | Yes            | Yes    | No
```

#### Simplified Collision Mesh

```csharp
/// <summary>
/// Generate simplified collision mesh (1/4 resolution of render mesh).
/// Reduces PhysX overhead while maintaining acceptable precision.
/// </summary>
Mesh GenerateCollisionMesh(OverlayChunk32 chunk)
{
    // Sample every 4th voxel (8x8x8 collision grid from 32x32x32)
    const int COLLISION_RESOLUTION = 8;

    var collisionData = new NativeArray<byte>(
        COLLISION_RESOLUTION * COLLISION_RESOLUTION * COLLISION_RESOLUTION,
        Allocator.Temp
    );

    for (int z = 0; z < COLLISION_RESOLUTION; z++)
    for (int y = 0; y < COLLISION_RESOLUTION; y++)
    for (int x = 0; x < COLLISION_RESOLUTION; x++)
    {
        // Sample 4x4x4 voxel block, set collision if any solid
        bool isSolid = IsBlockSolid(chunk.data, x * 4, y * 4, z * 4, 4);
        collisionData[x + y * 8 + z * 64] = isSolid ? (byte)1 : (byte)0;
    }

    // Generate mesh from collision data (simplified greedy meshing)
    Mesh collisionMesh = SimplifiedGreedyMesh(collisionData, COLLISION_RESOLUTION);

    collisionData.Dispose();
    return collisionMesh;
}
```

### Destruction Pipeline

#### Public API

```csharp
/// <summary>
/// Public API for voxel destruction operations.
/// </summary>
public class VoxelDestructionAPI : MonoBehaviour
{
    private DestructibleOverlayManager overlayManager;
    private VFXDebrisSpawner debrisSpawner;

    /// <summary>
    /// Destroy a single voxel at world position.
    /// </summary>
    public void DestroyVoxel(Vector3 worldPos)
    {
        overlayManager.DestroyVoxel(worldPos);
        debrisSpawner.SpawnDebris(worldPos, 1); // Single particle
    }

    /// <summary>
    /// Destroy voxels in sphere (explosion, projectile impact).
    /// </summary>
    public void DestroySphere(Vector3 center, float radius)
    {
        overlayManager.DestroySphere(center, radius);

        // Calculate destroyed voxel count for VFX intensity
        int voxelCount = EstimateDestroyedVoxels(center, radius);
        debrisSpawner.SpawnDebrisCluster(center, voxelCount);
    }

    /// <summary>
    /// Destroy voxels in box (building collapse, area damage).
    /// </summary>
    public void DestroyBox(Vector3 center, Vector3 size)
    {
        overlayManager.DestroyBox(center, size);

        int voxelCount = EstimateDestroyedVoxels(center, size);
        debrisSpawner.SpawnDebrisCluster(center, voxelCount);
    }

    /// <summary>
    /// Query if position is destructible (inside overlay zone).
    /// </summary>
    public bool IsDestructible(Vector3 worldPos)
    {
        return overlayManager.IsPositionInOverlayZone(worldPos);
    }
}
```

#### Destruction Flow

```
USER CALLS: DestroyVoxel(worldPos)
   ↓
1. World Position → Overlay Chunk Coord
   - ChunkCoord coord = WorldToChunkCoord(worldPos)

2. Check if Chunk Loaded
   ├→ YES: Modify voxel data immediately
   │   - chunk.data.SetVoxel(localX, localY, localZ, VoxelType.Air)
   │   - chunk.isDirty = true
   │   - Schedule remesh (amortized, 2ms budget)
   │
   └→ NO: Queue modification for later
       - pendingModifications.Add(coord, modification)
       - Apply when chunk loads (in LoadChunk)

3. Spawn Debris VFX
   - debrisSpawner.SpawnDebris(worldPos, count)
   - Pooled particle system

4. Update Collision Mesh (if needed)
   - Schedule collision mesh rebuild (lower priority)
   - Amortized over frames

5. Persist Modification (on unload)
   - When chunk unloads, serialize to disk cache
   - Destruction persists across sessions
```

#### Amortized Remeshing

```csharp
/// <summary>
/// Manages remeshing queue with frame budget.
/// Prevents FPS spikes from massive destruction.
/// </summary>
public class AmortizedRemeshingManager
{
    private Queue<(ChunkCoord, Priority)> remeshQueue;
    private const float BUDGET_MS = 2f;

    public enum Priority { Low, Normal, High }

    public void ScheduleRemesh(ChunkCoord coord, Priority priority)
    {
        // Insert based on priority (high priority to front)
        remeshQueue.Enqueue((coord, priority));
    }

    public void ProcessRemeshQueue()
    {
        float startTime = Time.realtimeSinceStartup;

        while (remeshQueue.Count > 0)
        {
            if ((Time.realtimeSinceStartup - startTime) * 1000f > BUDGET_MS)
                break; // Budget exceeded, continue next frame

            var (coord, priority) = remeshQueue.Dequeue();

            // Greedy meshing (Burst-compiled, ~1.5ms per chunk)
            RegenerateMesh(coord);
        }
    }
}
```

### Memory Management

#### Budget Breakdown (400 MB Overlay System)

```
OVERLAY MEMORY BUDGET: 400 MB
├─ Active Chunks: 250 MB
│  ├─ Chunk data (compressed): 12.5 MB (6,250 chunks × 2 KB)
│  ├─ Mesh vertices: 150 MB (6,250 × 2,000 verts × 12 bytes)
│  └─ Mesh indices: 87.5 MB (6,250 × 3,000 indices × 4 bytes)
│
├─ Memory Cache (LRU): 100 MB
│  └─ Recently unloaded chunks (compressed): ~50,000 chunks
│
├─ Compression Overhead: 25 MB
│  └─ Temp buffers for compression/decompression
│
└─ Reserve: 25 MB
   └─ Worst-case spikes, GC overhead

TOTAL: 400 MB (within budget)
```

#### Compression Strategy (RLE + Palette)

```
RAW VOXEL DATA (32x32x32 chunk):
- 32,768 voxels × 1 byte = 32 KB per chunk
- 6,250 chunks = 200 MB raw

COMPRESSED (RLE + Palette):
- Typical terrain: 80% solid, 20% transitions
- RLE compression: Long runs of same voxel
- Palette: Max 256 unique types (1 byte index)

COMPRESSION RATIO ANALYSIS:
Best case (uniform terrain): 1000:1 (32 KB → 32 bytes)
Typical case (varied terrain): 15:1 (32 KB → 2 KB)
Worst case (noisy, destroyed): 5:1 (32 KB → 6 KB)

AVERAGE: 15:1 compression
- 32 KB raw → 2 KB compressed
- 6,250 chunks × 2 KB = 12.5 MB (vs 200 MB raw)
- SAVINGS: 187.5 MB (94% reduction)
```

#### Memory Cache (LRU)

```csharp
/// <summary>
/// LRU cache for recently unloaded overlay chunks.
/// Prevents redundant disk I/O when player moves back and forth.
/// </summary>
public class LRUCache<TKey, TValue>
{
    private Dictionary<TKey, LinkedListNode<(TKey, TValue)>> cache;
    private LinkedList<(TKey, TValue)> lruList;
    private int maxSizeBytes;
    private int currentSizeBytes;

    public LRUCache(int maxSizeBytes)
    {
        this.maxSizeBytes = maxSizeBytes;
        cache = new Dictionary<TKey, LinkedListNode<(TKey, TValue)>>();
        lruList = new LinkedList<(TKey, TValue)>();
        currentSizeBytes = 0;
    }

    public bool TryGet(TKey key, out TValue value)
    {
        if (cache.TryGetValue(key, out var node))
        {
            // Move to front (most recently used)
            lruList.Remove(node);
            lruList.AddFirst(node);

            value = node.Value.Item2;
            return true;
        }

        value = default;
        return false;
    }

    public void Add(TKey key, TValue value)
    {
        int valueSize = GetSize(value);

        // Evict until space available
        while (currentSizeBytes + valueSize > maxSizeBytes && lruList.Count > 0)
        {
            EvictLRU();
        }

        // Add to cache
        var node = lruList.AddFirst((key, value));
        cache[key] = node;
        currentSizeBytes += valueSize;
    }

    private void EvictLRU()
    {
        var lru = lruList.Last;
        if (lru != null)
        {
            int size = GetSize(lru.Value.Item2);
            cache.Remove(lru.Value.Item1);
            lruList.RemoveLast();
            currentSizeBytes -= size;
        }
    }

    private int GetSize(TValue value)
    {
        if (value is CompressedVoxelData cvd)
        {
            return cvd.GetCompressedSize();
        }
        return 1024; // Default estimate
    }
}
```

### Performance Targets

#### Frame Budget (60 FPS = 16.6ms)

```
OVERLAY SYSTEM BREAKDOWN:
├─ Overlay Management: 1.0 ms
│  ├─ Player tracking: 0.1 ms
│  ├─ Streaming evaluation: 0.3 ms
│  └─ Cache management: 0.6 ms
│
├─ Remeshing (amortized): 2.0 ms
│  ├─ Greedy meshing (1 chunk): 1.5 ms
│  └─ Mesh upload to GPU: 0.5 ms
│
└─ Collision Updates: 0.5 ms
   ├─ Collision mesh generation: 0.3 ms
   └─ PhysX mesh update: 0.2 ms

TOTAL OVERLAY IMPACT: 3.5 ms (21% of 16.6ms frame budget)

REMAINING BUDGET: 13.1 ms
├─ Gameplay Logic: 1.0 ms
├─ ECS (2000 enemies): 3.0 ms
├─ Physics: 2.0 ms
├─ Rendering (URP): 5.0 ms
├─ VFX/Audio: 1.0 ms
└─ Reserve: 1.1 ms

TOTAL: 16.6 ms → 60 FPS ✓
```

#### Streaming Performance

```
CHUNK LOAD TIME: 2 ms per chunk
├─ Cache lookup: 0.1 ms
├─ Decompression (RLE): 0.4 ms
├─ Greedy meshing (Burst): 1.3 ms
└─ Mesh allocation: 0.2 ms

CHUNKS PER FRAME: 1 chunk (2ms budget)

STREAMING TRANSITION TIME:
├─ Visible chunks (100): 100 × 2ms = 200ms = 0.2s ✓
└─ Full zone (6,250): Background loaded over 104s (imperceptible)

PLAYER EXPERIENCE:
- Normal movement: No visible loading (chunks preloaded)
- Fast movement/teleport: 0.2s load time (acceptable)
- Worst case: 2s full zone load (rare, acceptable)
```

### Edge Cases & Mitigations

#### Player Moves Fast

**Problem:** Player moves faster than streaming can load chunks.

**Mitigation:**
```csharp
// Predictive loading based on velocity
Vector3 playerVelocity = (playerPos - lastPlayerPos) / Time.deltaTime;
Vector3 predictedPos = playerPos + playerVelocity * PREDICTION_TIME;

// Preload chunks in movement direction
var predictiveChunks = GetChunksInRadius(predictedPos, LOAD_RADIUS);
foreach (var coord in predictiveChunks)
{
    loadQueue.Enqueue(coord); // Higher priority
}
```

#### Destruction During Streaming

**Problem:** Player destroys voxels before chunk fully loaded.

**Mitigation:**
```csharp
// Queue modifications, apply when chunk loads
Dictionary<ChunkCoord, List<VoxelModification>> pendingModifications;

void DestroyVoxel(Vector3 worldPos)
{
    ChunkCoord coord = WorldToChunkCoord(worldPos);

    if (!activeChunks.ContainsKey(coord))
    {
        // Queue for later application
        if (!pendingModifications.ContainsKey(coord))
            pendingModifications[coord] = new List<VoxelModification>();

        pendingModifications[coord].Add(new VoxelModification
        {
            position = worldPos,
            voxelType = VoxelType.Air
        });
    }
}

void LoadChunk(ChunkCoord coord)
{
    // ... normal loading ...

    // Apply pending modifications
    if (pendingModifications.TryGetValue(coord, out var mods))
    {
        foreach (var mod in mods)
        {
            chunk.data.SetVoxel(mod.localX, mod.localY, mod.localZ, mod.voxelType);
        }
        pendingModifications.Remove(coord);
        chunk.isDirty = true;
    }
}
```

#### Memory Spike

**Problem:** Sudden memory spike (e.g., player teleports, loading spike).

**Mitigation:**
```csharp
// Hard memory limit with forced unload
const int MAX_OVERLAY_MEMORY_MB = 450;

void CheckMemoryLimit()
{
    int currentMemory = CalculateOverlayMemory();

    if (currentMemory > MAX_OVERLAY_MEMORY_MB)
    {
        // Force unload oldest chunks (even if in LOAD_RADIUS)
        var sortedChunks = activeChunks.OrderBy(kvp => kvp.Value.lastAccessTime);

        int toFree = currentMemory - MAX_OVERLAY_MEMORY_MB;
        int freed = 0;

        foreach (var kvp in sortedChunks)
        {
            UnloadChunk(kvp.Key);
            freed += EstimateChunkMemory();

            if (freed >= toFree)
                break;
        }
    }
}
```

#### Multiplayer Synchronization

**Problem:** Multiple players destroying same overlay chunk.

**Mitigation:**
```csharp
// Delta compression for network sync
struct VoxelModificationDelta
{
    ChunkCoord chunkCoord;
    ushort[] modifiedVoxels;  // List of modified voxel indices
    byte[] newValues;         // New voxel types
    uint timestamp;
}

// Send only modifications, not full chunks
void SendDestructionToNetwork(ChunkCoord coord)
{
    var delta = GenerateDelta(coord);
    NetworkManager.Send(delta); // ~100 bytes per destruction event
}
```

---

## Implementation Plan

### Phase 1.5: Destructible Overlay Foundation (4 weeks)

**Week 1-2: Data Structures & Compression**
- Implement CompressedVoxelData (RLE + Palette)
- Unit tests: Compression ratios, correctness
- Implement OverlayChunk32 structure
- Memory profiling: Validate 2 KB per chunk

**Week 3-4: Streaming System**
- Implement DestructibleOverlayManager
- Player-follow logic (LOAD/UNLOAD_RADIUS)
- Chunk loading pipeline (cache → disk → generate)
- Chunk unloading pipeline (serialize → cache)
- Performance profiling: 2ms budget validation

**Week 5: Blending & Collision**
- Base ↔ Overlay blending (disable base in overlay zone)
- Z-fighting fix validation
- Dual-layer collision system
- Raycast priority (overlay first)
- Simplified collision mesh generation

**Week 6: Destruction API & Integration**
- VoxelDestructionAPI implementation
- DestroySphere, DestroyBox, DestroyVoxel
- Amortized remeshing integration
- Debris VFX spawning
- Full system performance profiling

**Milestone:** Destructible overlay working, 60 FPS maintained

---

## Performance Validation

### Benchmarks Required

```
BENCHMARK 1: Streaming Performance
- Scenario: Player moves 200m (full zone transition)
- Target: <0.5s visible loading, <2s full load
- Measure: Chunk load time, frame budget impact

BENCHMARK 2: Destruction Performance
- Scenario: 100 simultaneous sphere destructions (explosions)
- Target: 60 FPS maintained (no spikes >20ms)
- Measure: Remeshing queue depth, amortization effectiveness

BENCHMARK 3: Memory Usage
- Scenario: Player explores 1000x1000m map (full traversal)
- Target: <450 MB overlay memory (peak)
- Measure: Memory profiler, cache efficiency

BENCHMARK 4: Collision Performance
- Scenario: 2000 enemies + player raycasting against overlay
- Target: <2ms collision per frame
- Measure: Raycast time, PhysX overhead

PASS CRITERIA: All benchmarks meet targets @ 60 FPS
```

---

## Risks & Contingencies

| Risk | Probability | Mitigation |
|------|-------------|------------|
| Streaming lag visible | MEDIUM | Predictive loading, priority queue, visual masking (fog) |
| Memory spikes exceed 450 MB | LOW | Hard limit + forced unload, aggressive compression |
| Compression ratio worse than 15:1 | LOW | Fallback to 10:1 assumption, reduce active zone to 150x150m |
| 2ms budget insufficient | MEDIUM | Reduce chunk size to 16³ overlay, or increase budget to 3ms |

---

## References

- ADR-001: Dual-Scale Voxel System (superseded by this ADR)
- ADR-004: Chunk Size 16x16x16 (extended with 32³ overlay)
- Document 11_DESTRUCTIBLE_OVERLAY_SYSTEM.md (detailed implementation)
- Document 03_CHUNK_MANAGEMENT.md (overlay section)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-20
**Status:** APPROVED FOR IMPLEMENTATION
