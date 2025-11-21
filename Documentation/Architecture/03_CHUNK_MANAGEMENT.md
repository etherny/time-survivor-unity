# Chunk Management System
## Spatial Partitioning, Culling, LOD pour Arène 1000x1000m

---

## 1. OVERVIEW

### Responsabilités du Chunk Manager

- **Spatial partitioning**: Diviser le monde en chunks gérables
- **Visibility culling**: Ne render que les chunks visibles
- **LOD management**: Multi-niveaux de détail selon distance
- **Memory management**: Load/unload chunks, pooling
- **Dirty tracking**: Identifier chunks nécessitant remeshing

---

## 2. CHUNK SIZE ANALYSIS

### 2.1 Optimal Chunk Size (Macro Terrain)

**Facteurs:**
- Vertex limit: 65,535 per mesh
- Camera view: Top-down 45°, 50-100m view distance
- Culling granularity: Assez petit pour cull efficacement
- Memory footprint: Balance entre nombre chunks et taille

**Analysis:**

| Size | Voxels | Meshes/World | Culling | Verdict |
|------|--------|--------------|---------|---------|
| 8x8x8 | 512 | 125,000 | Très fin | Trop de chunks |
| 16x16x16 | 4,096 | 15,876 | Optimal | CHOISI |
| 32x32x32 | 32,768 | 1,984 | Trop gros | Vertex overflow |

**Decision: 16x16x16 voxels = 16m x 16m x 16m world space**

**Justification:**
- Greedy meshing: ~500 vertices average per chunk (safe << 65k)
- Culling granularity: 16m chunks → frustum culling efficace
- Total chunks: 15,876 (manageable avec pooling)
- View distance 100m: ~49 chunks visibles (7x7 grid)

### 2.2 Micro Chunk Size (Props/Enemies)

**Decision: 32x32x32 voxels = 3.2m x 3.2m x 3.2m world space**

**Justification:**
- Enemy model: 32^3 voxels permet détail hyper fin
- Crate/Prop: 16-32 voxels suffisant pour recognition
- Memory: 32KB voxel data per chunk (fit L3 cache)
- Meshing: Small chunks → fast remeshing (<0.5ms)

---

## 3. MACRO CHUNK MANAGEMENT (Terrain)

### 3.1 Data Structure

```csharp
// Chunk Manager pour terrain statique
public class MacroChunkManager : MonoBehaviour {

    // Chunk storage (spatial hash)
    private NativeHashMap<int3, MacroChunkHandle> chunkMap;

    // Active chunks (visible + neighbors)
    private NativeList<MacroChunkHandle> activeChunks;

    // Chunk pool (reuse mesh objects)
    private ChunkPool chunkPool;

    // Camera reference (pour culling)
    private Camera mainCamera;

    // Configuration
    public int viewDistanceChunks = 8; // 8 chunks = 128m
    public bool enableOcclusionCulling = true;

    void Awake() {
        chunkMap = new NativeHashMap<int3, MacroChunkHandle>(
            20000, // Max chunks (1000x1000m world)
            Allocator.Persistent
        );
        activeChunks = new NativeList<MacroChunkHandle>(
            200, // ~50-100 visible typical
            Allocator.Persistent
        );
        chunkPool = new ChunkPool(500); // Pool size
    }

    void Update() {
        UpdateVisibleChunks();
    }

    void OnDestroy() {
        chunkMap.Dispose();
        activeChunks.Dispose();
    }
}

// Chunk handle (ref to actual chunk data)
public struct MacroChunkHandle {
    public int3 chunkCoord;
    public Entity chunkEntity; // ECS entity si hybrid approach
    public int poolIndex; // Index dans chunk pool
    public byte lodLevel; // 0 = full detail, 1-3 = LOD
}
```

### 3.2 Chunk Loading/Unloading

```csharp
// Determine chunks à charger basé sur camera position
[BurstCompile]
struct DetermineVisibleChunksJob : IJob {
    [ReadOnly] public float3 cameraPosition;
    [ReadOnly] public int viewDistanceChunks;
    [ReadOnly] public NativeHashMap<int3, MacroChunkHandle> allChunks;

    [WriteOnly] public NativeList<int3> chunksToLoad;
    [WriteOnly] public NativeList<int3> chunksToUnload;

    public void Execute() {
        // Camera chunk position
        int3 camChunk = new int3(
            (int)math.floor(cameraPosition.x / 16f),
            0, // Terrain vertical chunks fixed
            (int)math.floor(cameraPosition.z / 16f)
        );

        // Determine required chunks (square autour camera)
        for (int x = -viewDistanceChunks; x <= viewDistanceChunks; x++) {
            for (int z = -viewDistanceChunks; z <= viewDistanceChunks; z++) {
                for (int y = 0; y < 4; y++) { // Terrain height fixed
                    int3 chunkCoord = camChunk + new int3(x, y, z);

                    // Check si chunk existe déjà
                    if (!allChunks.ContainsKey(chunkCoord)) {
                        chunksToLoad.Add(chunkCoord);
                    }
                }
            }
        }

        // Determine chunks trop loin (unload)
        var keys = allChunks.GetKeyArray(Allocator.Temp);
        foreach (var key in keys) {
            float distance = math.distance(
                new float3(key.x, 0, key.z),
                new float3(camChunk.x, 0, camChunk.z)
            );

            if (distance > viewDistanceChunks + 2) { // Hysteresis
                chunksToUnload.Add(key);
            }
        }
        keys.Dispose();
    }
}
```

### 3.3 Frustum Culling (Aggressive)

```csharp
// Frustum culling par chunk (Unity Burst)
[BurstCompile]
struct FrustumCullChunksJob : IJobParallelFor {

    [ReadOnly] public NativeArray<MacroChunkHandle> chunks;
    [ReadOnly] public FrustumPlanes frustum; // Camera frustum planes

    [WriteOnly] public NativeArray<bool> visibilityResults;

    public void Execute(int index) {
        var chunk = chunks[index];

        // Chunk bounds (AABB)
        float3 chunkMin = new float3(
            chunk.chunkCoord.x * 16f,
            chunk.chunkCoord.y * 16f,
            chunk.chunkCoord.z * 16f
        );
        float3 chunkMax = chunkMin + new float3(16f, 16f, 16f);

        Bounds bounds = new Bounds(
            (chunkMin + chunkMax) * 0.5f,
            new float3(16f, 16f, 16f)
        );

        // Test against frustum planes
        visibilityResults[index] = GeometryUtility.TestPlanesAABB(
            frustum.planes,
            bounds
        );
    }
}

// Frustum planes struct (passable à Burst job)
public struct FrustumPlanes {
    public Plane plane0, plane1, plane2, plane3, plane4, plane5;

    public Plane[] planes => new[] { plane0, plane1, plane2, plane3, plane4, plane5 };

    public static FrustumPlanes FromCamera(Camera camera) {
        var planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return new FrustumPlanes {
            plane0 = planes[0],
            plane1 = planes[1],
            plane2 = planes[2],
            plane3 = planes[3],
            plane4 = planes[4],
            plane5 = planes[5]
        };
    }
}
```

### 3.4 Occlusion Culling (Baked)

**Unity Occlusion Culling:**
- Terrain statique → Bake occlusion data (Unity built-in)
- Top-down 45° view → Peu de occlusion (collines)
- Utilité marginale pour ce type de jeu

**Recommendation:**
- **Frustum culling ONLY** suffisant
- Occlusion culling: optional, faible gain
- Alternative: Manual occlusion (hills masquent valleys)

```csharp
// Manual height-based occlusion (optionnel)
bool IsOccludedByTerrain(int3 chunkCoord, float3 cameraPos) {
    // Si chunk derrière une colline haute
    // Raycasting simpliste terrain height
    // Gain: ~5-10% chunks culled supplémentaires
    // Coût: ~0.2ms per frame
    // Decision: SKIP (frustum culling suffisant)
    return false;
}
```

---

## 4. LOD SYSTEM (Level of Detail)

### 4.1 LOD Levels Definition

**Macro Terrain LOD:**

| LOD | Distance | Voxel Skip | Vertices | Detail |
|-----|----------|------------|----------|--------|
| LOD0 | 0-50m | None | ~500 | Full detail |
| LOD1 | 50-100m | Every 2nd | ~125 | Medium |
| LOD2 | 100-200m | Every 4th | ~30 | Low |
| LOD3 | 200m+ | Every 8th | ~8 | Silhouette |

**Micro Objects LOD:**

| LOD | Distance | Strategy | Vertices |
|-----|----------|----------|----------|
| LOD0 | 0-20m | Full voxel model | 500-2000 |
| LOD1 | 20-50m | Half-res voxel | 125-500 |
| LOD2 | 50-100m | Billboard quad | 4 |
| LOD3 | 100m+ | Culled (invisible) | 0 |

### 4.2 LOD Selection Job

```csharp
[BurstCompile]
struct CalculateLODJob : IJobParallelFor {

    [ReadOnly] public float3 cameraPosition;
    [ReadOnly] public NativeArray<MacroChunkHandle> chunks;

    [WriteOnly] public NativeArray<byte> lodLevels; // 0-3

    // LOD distance thresholds
    public float lod0Distance; // 50m
    public float lod1Distance; // 100m
    public float lod2Distance; // 200m

    public void Execute(int index) {
        var chunk = chunks[index];

        // Chunk center position
        float3 chunkCenter = new float3(
            chunk.chunkCoord.x * 16f + 8f,
            chunk.chunkCoord.y * 16f + 8f,
            chunk.chunkCoord.z * 16f + 8f
        );

        float distance = math.distance(cameraPosition, chunkCenter);

        // Determine LOD level
        if (distance < lod0Distance) {
            lodLevels[index] = 0;
        } else if (distance < lod1Distance) {
            lodLevels[index] = 1;
        } else if (distance < lod2Distance) {
            lodLevels[index] = 2;
        } else {
            lodLevels[index] = 3;
        }
    }
}
```

### 4.3 LOD Meshing Strategy

**Approach: Pre-bake LOD meshes (Terrain static)**

```csharp
// Generate all LOD levels for terrain chunk (once at generation)
public class MacroChunkLODGenerator {

    public MacroChunkLODSet GenerateLODs(MacroChunk chunk) {
        var lodSet = new MacroChunkLODSet();

        // LOD0: Full detail greedy meshing
        lodSet.lod0Mesh = GreedyMeshingFull(chunk.voxels);

        // LOD1: Skip every 2nd voxel (subsample)
        var subsampledLOD1 = SubsampleVoxels(chunk.voxels, 2);
        lodSet.lod1Mesh = GreedyMeshing(subsampledLOD1);

        // LOD2: Skip every 4th voxel
        var subsampledLOD2 = SubsampleVoxels(chunk.voxels, 4);
        lodSet.lod2Mesh = GreedyMeshing(subsampledLOD2);

        // LOD3: Skip every 8th voxel (très simple)
        var subsampledLOD3 = SubsampleVoxels(chunk.voxels, 8);
        lodSet.lod3Mesh = GreedyMeshing(subsampledLOD3);

        return lodSet;
    }

    NativeArray<VoxelType> SubsampleVoxels(NativeArray<VoxelType> voxels, int skip) {
        int newSize = MacroChunk.SIZE / skip;
        var subsampled = new NativeArray<VoxelType>(
            newSize * newSize * newSize,
            Allocator.Temp
        );

        for (int x = 0; x < newSize; x++) {
            for (int y = 0; y < newSize; y++) {
                for (int z = 0; z < newSize; z++) {
                    int srcX = x * skip;
                    int srcY = y * skip;
                    int srcZ = z * skip;
                    int srcIndex = srcX + srcY * MacroChunk.SIZE + srcZ * MacroChunk.SIZE * MacroChunk.SIZE;
                    int dstIndex = x + y * newSize + z * newSize * newSize;
                    subsampled[dstIndex] = voxels[srcIndex];
                }
            }
        }

        return subsampled;
    }
}

public struct MacroChunkLODSet {
    public Mesh lod0Mesh; // Full detail
    public Mesh lod1Mesh; // Medium
    public Mesh lod2Mesh; // Low
    public Mesh lod3Mesh; // Silhouette
}
```

### 4.4 LOD Memory Overhead

```
PER CHUNK LOD MEMORY:
├─ LOD0: ~500 vertices × 32 bytes = 16 KB
├─ LOD1: ~125 vertices × 32 bytes = 4 KB
├─ LOD2: ~30 vertices × 32 bytes = 1 KB
├─ LOD3: ~8 vertices × 32 bytes = 0.25 KB
└─ Total: ~21 KB per chunk

WORLD (15,876 chunks):
├─ Total LOD meshes: 15,876 × 21 KB = 333 MB
└─ Acceptable (dans budget)

ALTERNATIVE (mémoire serrée):
└─ Generate LOD meshes on-demand, cache LRU
   (plus complexe, gain ~200 MB)
```

---

## 5. MICRO CHUNK MANAGEMENT (Props/Enemies)

### 5.1 Spatial Hash for Micro Objects

```csharp
// Spatial hash pour fast queries (enemy AI, collisions)
public class MicroObjectSpatialHash {

    // Grid cell size (matching micro chunk size: 3.2m)
    private const float CELL_SIZE = 3.2f;

    // Hash map: cell coord → list of objects
    private NativeHashMap<int3, UnsafeList<MicroObjectHandle>> spatialGrid;

    public MicroObjectSpatialHash(int capacity = 10000) {
        spatialGrid = new NativeHashMap<int3, UnsafeList<MicroObjectHandle>>(
            capacity,
            Allocator.Persistent
        );
    }

    // Insert object into spatial hash
    public void Insert(MicroObjectHandle obj, float3 position) {
        int3 cellCoord = GetCellCoord(position);

        if (!spatialGrid.ContainsKey(cellCoord)) {
            spatialGrid[cellCoord] = new UnsafeList<MicroObjectHandle>(
                10,
                Allocator.Persistent
            );
        }

        spatialGrid[cellCoord].Add(obj);
    }

    // Query objects dans une sphère
    public NativeList<MicroObjectHandle> QuerySphere(float3 center, float radius) {
        var results = new NativeList<MicroObjectHandle>(50, Allocator.Temp);

        // Determine affected cells
        int3 minCell = GetCellCoord(center - new float3(radius));
        int3 maxCell = GetCellCoord(center + new float3(radius));

        for (int x = minCell.x; x <= maxCell.x; x++) {
            for (int y = minCell.y; y <= maxCell.y; y++) {
                for (int z = minCell.z; z <= maxCell.z; z++) {
                    int3 cell = new int3(x, y, z);
                    if (spatialGrid.TryGetValue(cell, out var objects)) {
                        // Filter by actual distance
                        foreach (var obj in objects) {
                            if (math.distance(obj.position, center) <= radius) {
                                results.Add(obj);
                            }
                        }
                    }
                }
            }
        }

        return results;
    }

    int3 GetCellCoord(float3 position) {
        return new int3(
            (int)math.floor(position.x / CELL_SIZE),
            (int)math.floor(position.y / CELL_SIZE),
            (int)math.floor(position.z / CELL_SIZE)
        );
    }

    public void Dispose() {
        // Dispose all UnsafeLists
        foreach (var kvp in spatialGrid) {
            kvp.Value.Dispose();
        }
        spatialGrid.Dispose();
    }
}

public struct MicroObjectHandle {
    public Entity entity; // ECS entity (enemies)
    public int objectId; // Unique ID
    public float3 position; // World position
    public ObjectType type; // Enemy, Prop, etc.
}
```

### 5.2 Micro Object Pooling (Enemies)

```csharp
// Pool pour réutiliser enemy voxel meshes (identical models)
public class EnemyVoxelMeshPool {

    // Pool par model type
    private Dictionary<VoxelModelAsset, Queue<Mesh>> meshPools;

    // Pre-allocate pools
    public void Initialize(VoxelModelAsset[] enemyTypes, int countPerType = 100) {
        meshPools = new Dictionary<VoxelModelAsset, Queue<Mesh>>();

        foreach (var model in enemyTypes) {
            var pool = new Queue<Mesh>(countPerType);

            // Pre-generate meshes
            for (int i = 0; i < countPerType; i++) {
                var mesh = GenerateVoxelMesh(model);
                pool.Enqueue(mesh);
            }

            meshPools[model] = pool;
        }
    }

    // Acquire mesh from pool
    public Mesh AcquireMesh(VoxelModelAsset model) {
        if (meshPools.TryGetValue(model, out var pool) && pool.Count > 0) {
            return pool.Dequeue();
        }

        // Pool épuisé, générer nouveau (rare)
        Debug.LogWarning($"Mesh pool exhausted for {model.name}, generating new");
        return GenerateVoxelMesh(model);
    }

    // Return mesh to pool
    public void ReleaseMesh(VoxelModelAsset model, Mesh mesh) {
        if (meshPools.TryGetValue(model, out var pool)) {
            mesh.Clear(); // Clear mesh data
            pool.Enqueue(mesh);
        }
    }

    Mesh GenerateVoxelMesh(VoxelModelAsset model) {
        // Meshing logic (voir 04_MESHING_SYSTEM.md)
        return new Mesh();
    }
}
```

---

## 6. DIRTY TRACKING & REMESHING

### 6.1 Chunk Dirty Flags

```csharp
// Tracking chunks nécessitant remeshing
public class ChunkDirtyTracker {

    // Dirty chunks queue (FIFO)
    private NativeQueue<int3> dirtyMacroChunks;
    private NativeQueue<MicroObjectHandle> dirtyMicroObjects;

    // Budgets (max remeshing per frame)
    public int maxMacroRemeshPerFrame = 2; // Terrain rarement dirty
    public int maxMicroRemeshPerFrame = 10; // Destructibles fréquemment dirty

    public ChunkDirtyTracker() {
        dirtyMacroChunks = new NativeQueue<int3>(Allocator.Persistent);
        dirtyMicroObjects = new NativeQueue<MicroObjectHandle>(Allocator.Persistent);
    }

    // Mark chunk dirty (après destruction)
    public void MarkDirty(int3 chunkCoord) {
        dirtyMacroChunks.Enqueue(chunkCoord);
    }

    public void MarkDirty(MicroObjectHandle obj) {
        dirtyMicroObjects.Enqueue(obj);
    }

    // Process dirty chunks (amortized over frames)
    public void ProcessDirtyChunks(
        MacroChunkManager macroManager,
        MicroObjectManager microManager
    ) {
        // Process macro chunks
        int processedMacro = 0;
        while (processedMacro < maxMacroRemeshPerFrame && dirtyMacroChunks.TryDequeue(out var coord)) {
            macroManager.RemeshChunk(coord);
            processedMacro++;
        }

        // Process micro objects
        int processedMicro = 0;
        while (processedMicro < maxMicroRemeshPerFrame && dirtyMicroObjects.TryDequeue(out var obj)) {
            microManager.RemeshObject(obj);
            processedMicro++;
        }
    }

    public void Dispose() {
        dirtyMacroChunks.Dispose();
        dirtyMicroObjects.Dispose();
    }
}
```

### 6.2 Amortized Remeshing Strategy

**Problem:**
- Destruction massive (100 voxels détruits simultanément)
- 100 remeshing jobs → Spike lag

**Solution: Amortization**
```csharp
// Spread remeshing over multiple frames
public class AmortizedRemeshingScheduler {

    private Queue<RemeshJob> remeshQueue = new Queue<RemeshJob>();

    // Budget: 2ms max remeshing per frame
    private float remeshBudgetMs = 2.0f;

    public void ScheduleRemesh(MacroChunk chunk, int priority = 0) {
        remeshQueue.Enqueue(new RemeshJob {
            chunk = chunk,
            priority = priority,
            timestamp = Time.time
        });
    }

    public void ProcessQueue() {
        float startTime = Time.realtimeSinceStartup;

        while (remeshQueue.Count > 0) {
            // Check budget
            float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
            if (elapsed > remeshBudgetMs) {
                break; // Budget épuisé, continue next frame
            }

            var job = remeshQueue.Dequeue();
            ExecuteRemesh(job);
        }
    }

    void ExecuteRemesh(RemeshJob job) {
        // Launch async job (voir 04_MESHING_SYSTEM.md)
        var meshJob = new GreedyMeshingJob { /* ... */ };
        var handle = meshJob.Schedule();
        // Store handle pour récupération next frame
    }
}

struct RemeshJob {
    public MacroChunk chunk;
    public int priority; // 0 = normal, 1 = high (near camera)
    public float timestamp;
}
```

---

## 7. MEMORY MANAGEMENT

### 7.1 Chunk Pooling

```csharp
// Pool de chunks pour réutilisation (éviter allocations)
public class ChunkPool {

    private Queue<MacroChunk> availableChunks;
    private int poolSize;

    public ChunkPool(int size) {
        poolSize = size;
        availableChunks = new Queue<MacroChunk>(size);

        // Pre-allocate chunks
        for (int i = 0; i < size; i++) {
            availableChunks.Enqueue(CreateNewChunk());
        }
    }

    public MacroChunk Acquire() {
        if (availableChunks.Count > 0) {
            return availableChunks.Dequeue();
        }

        // Pool exhausted, allocate new (rare)
        Debug.LogWarning("Chunk pool exhausted, allocating new");
        return CreateNewChunk();
    }

    public void Release(MacroChunk chunk) {
        // Reset chunk data
        chunk.voxels.Clear();
        chunk.isDirty = false;

        // Return to pool
        if (availableChunks.Count < poolSize) {
            availableChunks.Enqueue(chunk);
        } else {
            // Pool full, dispose
            chunk.Dispose();
        }
    }

    MacroChunk CreateNewChunk() {
        return new MacroChunk {
            voxels = new NativeArray<VoxelType>(
                MacroChunk.SIZE * MacroChunk.SIZE * MacroChunk.SIZE,
                Allocator.Persistent
            ),
            isDirty = false,
            isStatic = false
        };
    }

    public void Dispose() {
        while (availableChunks.Count > 0) {
            availableChunks.Dequeue().Dispose();
        }
    }
}
```

### 7.2 Garbage Collection Avoidance

**Stratégies:**
- NativeCollections (pas de GC)
- Struct-based data (value types)
- Object pooling (meshes, colliders)
- Avoid LINQ en hot paths
- StringBuilder pour logging

```csharp
// Example: Zero-allocation culling
[BurstCompile]
struct CullingJob : IJobParallelFor {
    // Tout en NativeCollections → Zero GC allocation
    [ReadOnly] public NativeArray<MacroChunkHandle> chunks;
    [WriteOnly] public NativeArray<bool> results;

    public void Execute(int index) {
        // Pure calcul, pas d'allocation
        results[index] = ComputeVisibility(chunks[index]);
    }

    bool ComputeVisibility(MacroChunkHandle chunk) {
        // Frustum culling logic
        return true;
    }
}
```

---

## 8. DESTRUCTIBLE OVERLAY CHUNK MANAGEMENT

**Related:** ADR-006, Document 11_DESTRUCTIBLE_OVERLAY_SYSTEM.md

With the tri-layer architecture (ADR-006), a third chunk management system is introduced for the **Destructible Overlay Layer**.

### 8.1 Overlay vs Base Terrain Chunks

**Key Differences:**

| Property | Base Terrain Chunks | Overlay Chunks |
|----------|-------------------|----------------|
| Chunk Size | 16x16x16 @ 1.0 unit | 32x32x32 @ 0.1 unit |
| World Coverage | 16m³ | 3.2m³ |
| Count (Full World) | 15,876 chunks | N/A (streaming) |
| Count (Overlay Zone) | N/A | 6,250 chunks (200x200m) |
| Static/Dynamic | Static (loaded once) | Dynamic (streaming) |
| Compression | Uncompressed | RLE+Palette (15:1) |
| Memory per Chunk | 24 KB | 2 KB (compressed) |
| Management System | MacroChunkManager | DestructibleOverlayManager |

### 8.2 Overlay Chunk Manager Architecture

```csharp
/// <summary>
/// Manages streaming overlay chunks (200x200m zone following player).
/// Separate from MacroChunkManager.
/// </summary>
public class DestructibleOverlayManager : MonoBehaviour
{
    // Active overlay chunks (within load radius)
    private Dictionary<ChunkCoord, OverlayChunk32> activeOverlayChunks;

    // Streaming coordinator
    private StreamingCoordinator streamingCoordinator;

    // Cache manager (memory + disk)
    private OverlayCacheManager cacheManager;

    // Blending with base terrain
    private BlendingController blendingController;

    // Constants
    private const float LOAD_RADIUS = 100f;   // 200x200m zone
    private const float UNLOAD_RADIUS = 120f; // Hysteresis

    void Update()
    {
        // Track player position
        Vector3 playerPos = GetPlayerPosition();

        // Evaluate streaming needs
        streamingCoordinator.EvaluateStreamingNeeds(playerPos);

        // Process load/unload queues (2ms budget)
        streamingCoordinator.ProcessStreamingQueue(maxChunksPerFrame: 1);
    }
}
```

### 8.3 Streaming Algorithm

**Player-Follow Logic:**

```
EVERY FRAME:
1. Get player position
2. Calculate required chunks (LOAD_RADIUS = 100m)
   → 200x200m zone = 6,250 chunks (max)

3. Identify chunks to LOAD:
   - Required but not active
   - Add to loadQueue

4. Identify chunks to UNLOAD:
   - Active but beyond UNLOAD_RADIUS (120m)
   - Add to unloadQueue

5. Process queues (2ms frame budget):
   - Load 1 chunk/frame: Cache → Disk → Generate
   - Unload 1 chunk/frame: Serialize → Cache → Release

RESULT:
- Visible chunks load in ~0.2s (100 chunks)
- Full zone loads in background (~104 frames = 1.7s)
```

### 8.4 Dual-Chunk System Interaction

**Base Terrain + Overlay Coexistence:**

```csharp
/// <summary>
/// Coordinates rendering between base terrain and overlay.
/// Prevents z-fighting by disabling base terrain in overlay zones.
/// </summary>
public class BlendingController
{
    public void OnOverlayChunkLoaded(ChunkCoord overlayCoord)
    {
        // Calculate corresponding base terrain chunks
        List<ChunkCoord> baseChunks = MapOverlayToBaseChunks(overlayCoord);

        // Disable base terrain rendering (keep collision)
        foreach (var baseCoord in baseChunks)
        {
            MacroChunkManager.DisableRendering(baseCoord);
        }
    }

    public void OnOverlayChunkUnloaded(ChunkCoord overlayCoord)
    {
        // Re-enable base terrain rendering
        List<ChunkCoord> baseChunks = GetMappedBaseChunks(overlayCoord);

        foreach (var baseCoord in baseChunks)
        {
            // Only re-enable if no other overlay covers this area
            if (!IsBaseChunkCoveredByOtherOverlay(baseCoord))
            {
                MacroChunkManager.EnableRendering(baseCoord);
            }
        }
    }

    private List<ChunkCoord> MapOverlayToBaseChunks(ChunkCoord overlayCoord)
    {
        // Overlay: 3.2m chunks
        // Base: 16m chunks
        // Ratio: 16 / 3.2 = 5 overlay per base (per dimension)

        Vector3 overlayWorldPos = overlayCoord.ToWorldPosition_Overlay();

        // Calculate intersecting base chunks
        // (Overlay chunks can span multiple base chunks at boundaries)
        return CalculateIntersectingBaseChunks(overlayWorldPos, 3.2f);
    }
}
```

**Collision System Dual-Layer:**

```csharp
/// <summary>
/// Raycast checks overlay first (priority), then base terrain.
/// </summary>
public bool VoxelRaycast(Ray ray, out VoxelHit hit)
{
    // PRIORITY 1: Check overlay (if chunk loaded)
    if (RaycastOverlay(ray, out hit))
        return true;

    // PRIORITY 2: Fallback to base terrain
    if (RaycastBaseTerrain(ray, out hit))
        return true;

    hit = default;
    return false;
}
```

### 8.5 Spatial Queries: Hybrid System

**When querying spatial data (e.g., enemy pathfinding):**

```csharp
/// <summary>
/// Get voxel at world position (checks overlay first).
/// </summary>
public VoxelType GetVoxelAtPosition(Vector3 worldPos)
{
    // Check if position is within overlay zone
    if (IsPositionInOverlayZone(worldPos))
    {
        ChunkCoord overlayCoord = WorldToOverlayChunkCoord(worldPos);

        // Check if overlay chunk is loaded
        if (overlayManager.TryGetChunk(overlayCoord, out var overlayChunk))
        {
            Vector3Int localVoxel = WorldToLocalVoxel_Overlay(worldPos);
            return overlayChunk.GetVoxel(localVoxel.x, localVoxel.y, localVoxel.z);
        }
    }

    // Fallback to base terrain
    ChunkCoord baseCoord = WorldToBaseChunkCoord(worldPos);
    var baseChunk = macroChunkManager.GetChunk(baseCoord);
    Vector3Int localVoxel = WorldToLocalVoxel_Base(worldPos);
    return baseChunk.GetVoxel(localVoxel.x, localVoxel.y, localVoxel.z);
}
```

### 8.6 Memory Management: Overlay Cache

**LRU Memory Cache (100 MB):**

```csharp
/// <summary>
/// Caches recently unloaded overlay chunks in memory.
/// Prevents redundant disk I/O when player moves back and forth.
/// </summary>
public class OverlayCacheManager
{
    private LRUCache<ChunkCoord, CompressedVoxelData> memoryCache;
    private DiskCache diskCache;

    public OverlayCacheManager()
    {
        memoryCache = new LRUCache<ChunkCoord, CompressedVoxelData>(
            maxSizeBytes: 100 * 1024 * 1024 // 100 MB
        );
        diskCache = new DiskCache("OverlayChunks");
    }

    public bool TryGetFromMemory(ChunkCoord coord, out CompressedVoxelData data)
    {
        return memoryCache.TryGet(coord, out data);
    }

    public bool TryLoadFromDisk(ChunkCoord coord, out CompressedVoxelData data)
    {
        return diskCache.TryLoad(coord, out data);
    }

    public void AddToMemory(ChunkCoord coord, CompressedVoxelData data)
    {
        memoryCache.Add(coord, data);
    }

    public void SaveToDisk(ChunkCoord coord, CompressedVoxelData data)
    {
        diskCache.Save(coord, data);
    }
}
```

**Cache Hit Rates (Expected):**
- Memory cache hit rate: 70-80% (player revisits nearby areas)
- Disk cache hit rate: 15-20% (persistent modifications)
- Generate new: 5-10% (first visit)

### 8.7 Amortized Remeshing: Overlay vs Base

**Key Difference:** Overlay chunks remesh more frequently (destruction events).

```csharp
/// <summary>
/// Separate remeshing queue for overlay (higher priority).
/// </summary>
public class AmortizedRemeshingManager
{
    private Queue<RemeshJob> baseTerrainQueue;   // Rare (static terrain)
    private Queue<RemeshJob> overlayQueue;       // Frequent (destruction)

    public void ProcessQueues()
    {
        float budgetMs = 2.0f;
        float startTime = Time.realtimeSinceStartup;

        // PRIORITY 1: Overlay queue (destruction visible to player)
        while (overlayQueue.Count > 0)
        {
            float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
            if (elapsed > budgetMs)
                break;

            var job = overlayQueue.Dequeue();
            ExecuteRemesh(job);
        }

        // PRIORITY 2: Base terrain queue (lower priority)
        while (baseTerrainQueue.Count > 0)
        {
            float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
            if (elapsed > budgetMs)
                break;

            var job = baseTerrainQueue.Dequeue();
            ExecuteRemesh(job);
        }
    }
}
```

### 8.8 Performance Implications

**Overlay System Frame Budget:**

```
OVERLAY CHUNK MANAGEMENT: 1.0 ms per frame
├─ Player tracking: 0.1 ms
├─ Streaming evaluation: 0.3 ms
├─ Cache management: 0.2 ms
├─ Load/unload processing: 0.3 ms
└─ Blending coordination: 0.1 ms

TOTAL: 1.0 ms (6% of 16.6ms frame budget)

COMBINED WITH BASE TERRAIN MANAGEMENT:
├─ Base terrain management: 2.0 ms
├─ Overlay management: 1.0 ms
└─ Total: 3.0 ms (18% frame budget) ✓
```

### 8.9 Implementation Notes

**Integration Points:**

1. **MacroChunkManager** remains unchanged (base terrain)
2. **DestructibleOverlayManager** operates independently (overlay)
3. **BlendingController** coordinates rendering between layers
4. **DualLayerCollision** prioritizes overlay in queries
5. **AmortizedRemeshing** manages both queues with priority

**Best Practices:**

- Keep systems decoupled (base terrain doesn't know about overlay)
- Use interfaces for voxel queries (abstracts dual-layer)
- Profile both systems separately (distinct Unity profiler markers)
- Cache efficiency critical for overlay (track hit rates)

**See Also:**
- ADR-006: Complete overlay architecture
- Document 11: Detailed implementation guide
- Document 07: Destruction pipeline integration

---

## 9. PERFORMANCE METRICS

### 8.1 Target Frame Budget

```
CHUNK MANAGEMENT BUDGET: 2.0 ms per frame (60 FPS)

BREAKDOWN:
├─ Visible chunks determination: 0.2 ms (job)
├─ Frustum culling: 0.5 ms (parallel job)
├─ LOD calculation: 0.3 ms (parallel job)
├─ Dirty tracking: 0.1 ms (queue ops)
├─ Spatial hash queries: 0.5 ms (enemy AI queries)
├─ Amortized remeshing: 0.4 ms (2 chunks max)
└─ Reserve: 0.0 ms
──────────────────────────────────
TOTAL: 2.0 ms
```

### 8.2 Profiling Markers

```csharp
using Unity.Profiling;

public class ChunkManager {
    static readonly ProfilerMarker s_VisibleChunksMarker = new ProfilerMarker("ChunkManager.DetermineVisibleChunks");
    static readonly ProfilerMarker s_FrustumCullMarker = new ProfilerMarker("ChunkManager.FrustumCulling");
    static readonly ProfilerMarker s_LODMarker = new ProfilerMarker("ChunkManager.LODCalculation");

    void UpdateVisibleChunks() {
        using (s_VisibleChunksMarker.Auto()) {
            // Logic here
        }
    }
}
```

---

## 9. IMPLEMENTATION CHECKLIST

- [ ] Implement MacroChunkManager avec spatial hash
- [ ] Implement FrustumCullingJob (Burst)
- [ ] Implement CalculateLODJob (Burst)
- [ ] Create MacroChunkLODGenerator (pre-bake LODs)
- [ ] Implement MicroObjectSpatialHash
- [ ] Create ChunkDirtyTracker avec amortization
- [ ] Implement ChunkPool (pooling)
- [ ] Setup EnemyVoxelMeshPool
- [ ] Add profiling markers
- [ ] Performance test (2000 enemies, 100m view)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-20
**Dependencies:** 01_GLOBAL_ARCHITECTURE.md, 02_DUAL_SCALE_VOXEL_SYSTEM.md
**Next:** 04_MESHING_SYSTEM.md
