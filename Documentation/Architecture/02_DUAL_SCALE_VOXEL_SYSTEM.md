# Dual-Scale Voxel System
## Architecture pour Voxels Multi-Échelles (1.0 + 0.1 unités)

---

## 1. PROBLÉMATIQUE

### Besoin de Deux Échelles

**Terrain Scale (1.0 unit voxels):**
- Collines, sol, formations rocheuses
- Grande surface (1000x1000m)
- Performance critique (statique, pré-calculé)

**Detail Scale (0.1 unit voxels):**
- Props destructibles (caisses, tonneaux, mobilier)
- Détails architecturaux (colonnes, ornements)
- Ennemis hyper détaillés (64+ voxels par ennemi)

### Challenge Architectural

**Approche Naive (unifiée à 0.1):**
- 1000x1000m à 0.1 = 10,000 x 10,000 x hauteur voxels
- Mémoire: PROHIBITIF (~50+ GB pour terrain seul)
- Performance: IMPOSSIBLE à mesher/render

**Solution: Dual-Scale Architecture Séparée**

---

## 2. ARCHITECTURE DUAL-SCALE

### 2.1 Séparation Conceptuelle

```
┌─────────────────────────────────────────────────────────────┐
│                  WORLD VOXEL SPACE                           │
│                                                               │
│  ┌────────────────────────────────────────┐                 │
│  │     MACRO LAYER (1.0 unit voxels)      │                 │
│  │  - Terrain (collines, sol)             │                 │
│  │  - Buildings (gros blocs)              │                 │
│  │  - Chunk size: 16x16x16 voxels         │                 │
│  │  - Resolution: 16m x 16m x 16m         │                 │
│  └────────────────────────────────────────┘                 │
│                                                               │
│  ┌────────────────────────────────────────┐                 │
│  │     MICRO LAYER (0.1 unit voxels)      │                 │
│  │  - Props (caisses, barils, mobilier)   │                 │
│  │  - Ennemis (voxelized models)          │                 │
│  │  - Chunk size: 32x32x32 voxels         │                 │
│  │  - Resolution: 3.2m x 3.2m x 3.2m      │                 │
│  └────────────────────────────────────────┘                 │
│                                                               │
│  Coordinate System: UNIFIED (world space meters)             │
│  Conversion: micro_pos = macro_pos (automatic)              │
└─────────────────────────────────────────────────────────────┘
```

### 2.2 Unified Coordinate System

**World Space (Unity):**
- Toutes positions en mètres (Vector3 standard)
- Player à (123.5, 10.2, 456.7) en world space

**Macro Layer Conversion:**
```csharp
// World position → Macro voxel coordinate
Vector3Int MacroVoxelCoord(Vector3 worldPos) {
    return new Vector3Int(
        Mathf.FloorToInt(worldPos.x / 1.0f),
        Mathf.FloorToInt(worldPos.y / 1.0f),
        Mathf.FloorToInt(worldPos.z / 1.0f)
    );
}
```

**Micro Layer Conversion:**
```csharp
// World position → Micro voxel coordinate
Vector3Int MicroVoxelCoord(Vector3 worldPos) {
    return new Vector3Int(
        Mathf.FloorToInt(worldPos.x / 0.1f),
        Mathf.FloorToInt(worldPos.y / 0.1f),
        Mathf.FloorToInt(worldPos.z / 0.1f)
    );
}
```

**Pas de conversion inter-layer nécessaire:** Les deux layers utilisent le même world space, juste différentes résolutions de quantification.

---

## 3. MACRO LAYER (Terrain 1.0 unit)

### 3.1 Data Structure

```csharp
// Macro chunk: 16x16x16 voxels = 16m x 16m x 16m world space
public struct MacroChunk {
    public const int SIZE = 16;
    public const float VOXEL_SIZE = 1.0f;

    // Voxel data (4096 voxels per chunk)
    public NativeArray<VoxelType> voxels; // 16*16*16 = 4096 bytes

    // Chunk world position (bottom corner)
    public int3 chunkCoord; // Chunk coordinate in chunk space

    // Mesh data (pre-baked, static)
    public Mesh terrainMesh;
    public MeshCollider collider;

    // Flags
    public bool isStatic; // true for terrain (no runtime modifications)
    public bool isDirty;  // false for terrain (never modified)
}
```

### 3.2 Voxel Type Definition

```csharp
// Compact voxel representation (1 byte per voxel)
public enum VoxelType : byte {
    Air = 0,           // Empty space

    // Terrain types (Macro layer)
    Grass = 1,
    Dirt = 2,
    Stone = 3,
    Sand = 4,
    Rock = 5,

    // Building types (Macro layer, can be destructible)
    Wood = 10,
    Brick = 11,
    Concrete = 12,

    // Reserved 13-255 for future types
}

// Material properties (ScriptableObject database)
[CreateAssetMenu]
public class VoxelMaterialData : ScriptableObject {
    public VoxelType type;
    public Color color;
    public Material renderMaterial;
    public bool isDestructible;
    public float hardness;
}
```

### 3.3 Terrain Generation (Static, Once)

```csharp
// Procedural terrain generation (execute once at world load)
public class MacroTerrainGenerator {

    [BurstCompile]
    struct TerrainGenerationJob : IJobParallelFor {
        public int chunkSize;
        public float noiseScale;
        public float heightMultiplier;

        [WriteOnly]
        public NativeArray<VoxelType> voxels;

        public int3 chunkCoord;

        public void Execute(int index) {
            // Flatten 3D index
            int x = index % chunkSize;
            int y = (index / chunkSize) % chunkSize;
            int z = index / (chunkSize * chunkSize);

            // World position
            float3 worldPos = new float3(
                chunkCoord.x * chunkSize + x,
                chunkCoord.y * chunkSize + y,
                chunkCoord.z * chunkSize + z
            );

            // 3D Perlin noise for terrain height
            float height = noise.cnoise(worldPos.xz * noiseScale) * heightMultiplier;

            // Determine voxel type based on height
            if (worldPos.y < height) {
                if (worldPos.y > height - 2) {
                    voxels[index] = VoxelType.Grass;
                } else if (worldPos.y > height - 5) {
                    voxels[index] = VoxelType.Dirt;
                } else {
                    voxels[index] = VoxelType.Stone;
                }
            } else {
                voxels[index] = VoxelType.Air;
            }
        }
    }

    public MacroChunk GenerateTerrainChunk(int3 chunkCoord) {
        var chunk = new MacroChunk {
            chunkCoord = chunkCoord,
            voxels = new NativeArray<VoxelType>(
                MacroChunk.SIZE * MacroChunk.SIZE * MacroChunk.SIZE,
                Allocator.Persistent
            ),
            isStatic = true,
            isDirty = false
        };

        var job = new TerrainGenerationJob {
            chunkSize = MacroChunk.SIZE,
            noiseScale = 0.05f,
            heightMultiplier = 20f,
            voxels = chunk.voxels,
            chunkCoord = chunkCoord
        };

        job.Schedule(chunk.voxels.Length, 64).Complete();

        return chunk;
    }
}
```

### 3.4 Memory Footprint (Macro Layer)

```
WORLD: 1000m x 1000m x 64m (height)
CHUNK SIZE: 16m x 16m x 16m

CHUNK COUNT:
├─ X axis: 1000 / 16 = 62.5 → 63 chunks
├─ Z axis: 1000 / 16 = 62.5 → 63 chunks
├─ Y axis: 64 / 16 = 4 chunks
└─ TOTAL: 63 * 63 * 4 = 15,876 chunks

MEMORY PER CHUNK:
├─ Voxel data: 4096 bytes (16^3 voxels * 1 byte)
├─ Chunk metadata: ~100 bytes
└─ Total: ~4.2 KB per chunk

TOTAL VOXEL DATA: 15,876 * 4.2 KB = 66.7 MB

MESH DATA (baked once):
├─ Vertices: ~500 per chunk average (greedy meshed)
├─ Memory: ~20 KB per chunk (vertices + normals + UVs + indices)
└─ Total: 15,876 * 20 KB = 317 MB

MACRO LAYER TOTAL: ~384 MB (acceptable!)
```

---

## 4. MICRO LAYER (Props/Enemies 0.1 unit)

### 4.1 Data Structure

```csharp
// Micro chunk: 32x32x32 voxels = 3.2m x 3.2m x 3.2m world space
public struct MicroChunk {
    public const int SIZE = 32;
    public const float VOXEL_SIZE = 0.1f;

    // Voxel data (32,768 voxels per chunk)
    public NativeArray<VoxelType> voxels; // 32*32*32 = 32KB

    // Chunk world position (bottom corner)
    public float3 worldPosition; // In meters (world space)

    // Mesh data (dynamic, regenerated on destruction)
    public Mesh dynamicMesh;
    public MeshCollider collider;

    // Flags
    public bool isActive;      // Chunk contains voxels (not all air)
    public bool isDirty;       // Needs remeshing
    public bool isDestructible; // Can be destroyed

    // Lifecycle
    public float lastAccessTime; // For pooling/unloading
}
```

### 4.2 Micro Voxel Types

```csharp
// Extended voxel types for micro layer
public enum VoxelType : byte {
    // ... Macro types 0-20 ...

    // Micro destructibles (21-100)
    WoodCrate = 21,
    MetalBarrel = 22,
    GlassBottle = 23,
    Pottery = 24,
    Cloth = 25,

    // Enemy voxels (101-200)
    EnemyFlesh = 101,
    EnemyBone = 102,
    EnemyArmor = 103,
    EnemyGore = 104,

    // Props (201-255)
    Vegetation = 201,
    Decoration = 202,
}
```

### 4.3 Micro Object Placement

```csharp
// Props et ennemis sont des MicroChunks positionnés dans le monde
public struct MicroVoxelObject {
    public Entity entity; // ECS entity (pour ennemis)
    public MicroChunk chunk; // Voxel data
    public float3 worldPosition; // Position in world space
    public ObjectType type; // Prop, Enemy, etc.

    // Pour ennemis
    public bool isAlive;
    public int health;
}

public enum ObjectType {
    StaticProp,    // Crate, barrel (pas de ECS)
    Enemy,         // Managed par ECS
    Projectile,    // Managed par ECS
}

// Factory pour créer des objets micro voxel
public class MicroVoxelFactory {

    // Créer une caisse destructible
    public MicroVoxelObject CreateCrate(float3 worldPosition) {
        var obj = new MicroVoxelObject {
            worldPosition = worldPosition,
            type = ObjectType.StaticProp,
            chunk = new MicroChunk {
                worldPosition = worldPosition,
                voxels = new NativeArray<VoxelType>(
                    MicroChunk.SIZE * MicroChunk.SIZE * MicroChunk.SIZE,
                    Allocator.Persistent
                ),
                isActive = true,
                isDestructible = true
            }
        };

        // Fill voxels (simple cube pour exemple)
        FillCrateVoxels(obj.chunk.voxels, MicroChunk.SIZE);
        obj.chunk.isDirty = true; // Needs meshing

        return obj;
    }

    // Créer un ennemi voxelisé
    public MicroVoxelObject CreateEnemy(float3 worldPosition, VoxelModelAsset model) {
        var obj = new MicroVoxelObject {
            worldPosition = worldPosition,
            type = ObjectType.Enemy,
            entity = CreateEnemyEntity(), // ECS entity
            chunk = new MicroChunk {
                worldPosition = worldPosition,
                voxels = new NativeArray<VoxelType>(
                    MicroChunk.SIZE * MicroChunk.SIZE * MicroChunk.SIZE,
                    Allocator.Persistent
                ),
                isActive = true,
                isDestructible = true
            },
            isAlive = true,
            health = 100
        };

        // Load voxel model from asset
        LoadVoxelModel(obj.chunk.voxels, model);
        obj.chunk.isDirty = true;

        return obj;
    }
}
```

### 4.4 Voxel Model Asset (Ennemis Hyper Détaillés)

```csharp
// Asset pour stocker des modèles voxel pré-conçus
[CreateAssetMenu]
public class VoxelModelAsset : ScriptableObject {
    public int3 size; // Ex: (32, 32, 32) pour enemy
    public VoxelType[] voxels; // Flatten 3D array

    // Metadata
    public string modelName;
    public ObjectType objectType;
    public int vertexCount; // Pre-calculated pour optimization

    // Bounds (pour collision)
    public Bounds bounds;

    // LOD versions (pre-baked pour performance)
    public VoxelModelAsset lodLevel1; // Demi-résolution
    public VoxelModelAsset lodLevel2; // Quart résolution
}

// Editor tool pour importer .vox files (MagicaVoxel)
#if UNITY_EDITOR
public class VoxelModelImporter {
    [MenuItem("Assets/Import Voxel Model (.vox)")]
    public static void ImportVoxFile() {
        // Parse .vox file format
        // Create VoxelModelAsset
        // Generate LOD versions
        // Save as ScriptableObject
    }
}
#endif
```

### 4.5 Memory Footprint (Micro Layer)

```
ASSUMPTIONS:
├─ Props destructibles: 500 max simultaneous
├─ Ennemis: 2000 max simultaneous
├─ Micro chunk size: 32^3 = 32KB voxel data

MEMORY:
├─ Props: 500 * 32KB = 16 MB (voxel data)
├─ Ennemis: 2000 * 32KB = 64 MB (voxel data)
├─ Mesh data: ~50KB per object average
│   └─ (500 + 2000) * 50KB = 125 MB
└─ TOTAL: ~205 MB

OPTIMIZATION: Instancing identiques (crates, ennemis même modèle)
└─ Mesh sharing → Reduce to ~50 MB mesh data

MICRO LAYER TOTAL: ~130 MB (excellent!)
```

---

## 5. UNIFIED VS SEPARATED ARCHITECTURE

### 5.1 Architecture Choisie: SEPARATED

**Justification:**

| Aspect | Unified (0.1 partout) | Separated (1.0 + 0.1) |
|--------|----------------------|----------------------|
| Memory | PROHIBITIF (50+ GB) | OPTIMAL (514 MB) |
| Meshing | Impossible terrain | Macro: fast, Micro: small chunks |
| Collision | Trop de voxels | Macro: baked, Micro: dynamic |
| Maintainability | Simple mais impraticable | Deux systems séparés |
| Performance | CATASTROPHIQUE | EXCELLENT |

**Décision: SEPARATED** (évident)

### 5.2 Interaction Entre Layers

**Collision Detection:**
```csharp
// Raycasting doit check les deux layers
public struct DualLayerRaycast {

    public bool Raycast(Ray ray, out RaycastHit hit) {
        RaycastHit macroHit, microHit;
        bool hitMacro = MacroLayerRaycast(ray, out macroHit);
        bool hitMicro = MicroLayerRaycast(ray, out microHit);

        // Return closest hit
        if (hitMacro && hitMicro) {
            hit = (macroHit.distance < microHit.distance) ? macroHit : microHit;
            return true;
        } else if (hitMacro) {
            hit = macroHit;
            return true;
        } else if (hitMicro) {
            hit = microHit;
            return true;
        }

        hit = default;
        return false;
    }
}
```

**Spatial Queries (OverlapSphere):**
```csharp
// Query objects dans une sphère (pour enemy AI)
public List<MicroVoxelObject> QueryMicroObjects(float3 center, float radius) {
    // Spatial hash (voir section Collision System)
    return spatialHash.Query(center, radius);
}
```

---

## 6. DATA STRUCTURES OPTIMALES

### 6.1 Macro Layer: Dense Array (Static)

**Raison:**
- Terrain statique, jamais modifié
- Dense (peu d'air au sol)
- Accès random fréquent (meshing, collision)

```csharp
// Simple flat array optimal pour cache locality
public NativeArray<VoxelType> macroVoxels; // 16^3 = 4096 bytes
```

### 6.2 Micro Layer: Palette + RLE (Dynamic)

**Raison:**
- Objets souvent creux (beaucoup d'air)
- Destructions fréquentes (données changent)
- Compression utile (mémoire)

```csharp
// Palette-based storage (MagicaVoxel style)
public struct PalettedMicroChunk {
    public NativeArray<byte> paletteIndices; // 32^3 = 32KB (or less si compressed)
    public NativeArray<VoxelType> palette;   // Max 256 types

    // Run-Length Encoding (optional, pour compression)
    public bool useRLE;
    public NativeArray<RLESpan> rleData;
}

public struct RLESpan {
    public byte paletteIndex;
    public ushort count; // Nombre de voxels consécutifs identiques
}
```

**Gain mémoire:**
- Ennemi typique: 70% air → RLE réduit 32KB à ~10KB
- Crate: 90% air → 32KB à ~3KB
- **Total saving: ~60% sur micro layer**

---

## 7. CONVERSION UTILITIES

```csharp
// Utilities pour conversion world space <-> voxel coordinates
public static class VoxelCoordinates {

    // World position → Macro voxel coord
    public static int3 ToMacroVoxel(float3 worldPos) {
        return new int3(
            (int)math.floor(worldPos.x / MacroChunk.VOXEL_SIZE),
            (int)math.floor(worldPos.y / MacroChunk.VOXEL_SIZE),
            (int)math.floor(worldPos.z / MacroChunk.VOXEL_SIZE)
        );
    }

    // World position → Micro voxel coord
    public static int3 ToMicroVoxel(float3 worldPos) {
        return new int3(
            (int)math.floor(worldPos.x / MicroChunk.VOXEL_SIZE),
            (int)math.floor(worldPos.y / MicroChunk.VOXEL_SIZE),
            (int)math.floor(worldPos.z / MicroChunk.VOXEL_SIZE)
        );
    }

    // Macro voxel → World position (center)
    public static float3 MacroVoxelToWorld(int3 voxelCoord) {
        return new float3(
            voxelCoord.x * MacroChunk.VOXEL_SIZE + MacroChunk.VOXEL_SIZE * 0.5f,
            voxelCoord.y * MacroChunk.VOXEL_SIZE + MacroChunk.VOXEL_SIZE * 0.5f,
            voxelCoord.z * MacroChunk.VOXEL_SIZE + MacroChunk.VOXEL_SIZE * 0.5f
        );
    }

    // Micro voxel → World position (center)
    public static float3 MicroVoxelToWorld(int3 voxelCoord) {
        return new float3(
            voxelCoord.x * MicroChunk.VOXEL_SIZE + MicroChunk.VOXEL_SIZE * 0.5f,
            voxelCoord.y * MicroChunk.VOXEL_SIZE + MicroChunk.VOXEL_SIZE * 0.5f,
            voxelCoord.z * MicroChunk.VOXEL_SIZE + MicroChunk.VOXEL_SIZE * 0.5f
        );
    }

    // World position → Macro chunk coordinate
    public static int3 ToMacroChunkCoord(float3 worldPos) {
        return new int3(
            (int)math.floor(worldPos.x / (MacroChunk.SIZE * MacroChunk.VOXEL_SIZE)),
            (int)math.floor(worldPos.y / (MacroChunk.SIZE * MacroChunk.VOXEL_SIZE)),
            (int)math.floor(worldPos.z / (MacroChunk.SIZE * MacroChunk.VOXEL_SIZE))
        );
    }
}
```

---

## 8. PERFORMANCE ANALYSIS

### 8.1 Cache Locality

**Macro Layer:**
- Dense array → Excellent cache locality
- Sequential access durant meshing → Cache hits 95%+

**Micro Layer:**
- Smaller chunks (32^3) → Fit L3 cache (32KB)
- RLE compression → Less memory bandwidth

### 8.2 Meshing Performance

**Macro (Terrain):**
- One-time cost (baked)
- Greedy meshing: ~5ms per chunk
- 15,876 chunks * 5ms = 79 seconds total (acceptable, one-time)
- Can parallelize: 8 threads → 10 seconds

**Micro (Props/Enemies):**
- Dynamic, frequent remeshing
- Smaller chunks → Faster meshing (~0.5ms per object)
- 2000 enemies * 0.5ms = 1000ms total
- Parallelized + amortized → <2ms per frame

---

## 9. TRADE-OFFS SUMMARY

| Aspect | Unified | Separated (CHOSEN) |
|--------|---------|-------------------|
| **Memory** | Impraticable | Optimal (514 MB) |
| **Performance** | Impossible | 60 FPS garanti |
| **Complexity** | Simple concept | Deux systems (manageable) |
| **Flexibility** | Limitée | Excellente |
| **Maintainability** | N/A (non viable) | Bonne (modules séparés) |

**Decision: SEPARATED est le seul choix viable.**

---

## 10. IMPLEMENTATION CHECKLIST

- [ ] Implement MacroChunk struct avec dense array
- [ ] Implement MicroChunk struct avec palette/RLE
- [ ] Créer VoxelType enum étendu
- [ ] Implement coordinate conversion utilities
- [ ] Créer VoxelModelAsset ScriptableObject
- [ ] Implement VoxelModelImporter (.vox files)
- [ ] Créer MicroVoxelFactory (props, enemies)
- [ ] Setup dual-layer spatial partitioning
- [ ] Implement dual-layer raycasting
- [ ] Performance profiling (memory, meshing)

---

**Document Version:** 1.0
**Last Updated:** 2025-11-20
**Dependencies:** 01_GLOBAL_ARCHITECTURE.md
**Next:** 03_CHUNK_MANAGEMENT.md
