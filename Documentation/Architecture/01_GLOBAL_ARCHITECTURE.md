# Architecture Globale - Moteur Voxel Haute Performance
## Vampire Survivor-like Game - Unity 6.2 URP

---

## 1. CONTEXTE ET CONTRAINTES

### Specifications Techniques
- **Plateforme**: PC et Console (pas mobile)
- **Target Performance**: 60 FPS minimum GARANTI
- **Engine**: Unity 6.2 (6000.2.12f1)
- **Rendering Pipeline**: URP (Universal Render Pipeline)
- **Architecture Code**: DOTS/ECS 1.0+ pour performance maximale

### Contraintes Gameplay
- **Genre**: Top-down 45°, vagues d'ennemis massives, survie
- **Arène**: 1000x1000m minimum (extensible)
- **Ennemis simultanés**: 2000+ à l'écran (hyper détaillés)
- **Distance de vue**: Moyenne 50-100m (caméra top-down)
- **Destructions**: Très fréquentes (combat intense)

### Contraintes Voxel
- **Terrain**: Statique avec collines (NON destructible)
- **Bâtiments**: Voxels destructibles (~1.0 unité)
- **Props/Décors**: Voxels fins destructibles (~0.1 unité)
- **Ennemis**: Voxels hyper détaillés destructibles

---

## 2. ARCHITECTURE SYSTÈME GLOBALE

### 2.1 Vue d'Ensemble - Tri-Layer Architecture

**UPDATE (ADR-006):** Architecture étendue avec couche overlay destructible.

```
┌─────────────────────────────────────────────────────────────────┐
│                      GAMEPLAY LAYER                              │
│  (MonoBehaviour/ECS Hybrid - Game Logic, Player Input, AI)      │
└────────────────────────┬────────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────────┐
│                 VOXEL ENGINE LAYER (TRI-LAYER)                   │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐       │
│  │ BASE TERRAIN │  │ DESTRUCTIBLE │  │  ENTITY VOXEL   │       │
│  │   SYSTEM     │  │   OVERLAY    │  │    SYSTEM       │       │
│  │ (Static 1.0) │  │ (Dynamic 0.1)│  │  (Enemies 0.1)  │       │
│  │ 1000x1000m   │  │  200x200m    │  │   Global        │       │
│  └──────┬───────┘  └──────┬───────┘  └────────┬────────┘       │
│         │                 │                    │                │
│  ┌──────▼─────────────────▼────────────────────▼────────┐       │
│  │         DUAL CHUNK MANAGEMENT SYSTEM                 │       │
│  │  • MacroChunkManager (base terrain)                  │       │
│  │  • DestructibleOverlayManager (streaming overlay)    │       │
│  │  • BlendingController (base ↔ overlay coordination)  │       │
│  └──────┬──────────────────────────────────────┬────────┘       │
│         │                                      │                │
│  ┌──────▼──────────┐                  ┌────────▼──────────┐     │
│  │  MESHING SYSTEM │                  │ DUAL-LAYER        │     │
│  │  (Greedy/Culled)│                  │ COLLISION SYSTEM  │     │
│  │  • Base mesh    │                  │ • Overlay priority│     │
│  │  • Overlay mesh │                  │ • Base fallback   │     │
│  └──────┬──────────┘                  └────────┬──────────┘     │
└─────────┼──────────────────────────────────────┼────────────────┘
          │                                      │
┌─────────▼──────────────────────────────────────▼────────────────┐
│                    DOTS/ECS RUNTIME LAYER                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐          │
│  │  Job System  │  │ Burst Compile│  │  Entity Query│          │
│  │  (Parallel)  │  │  (SIMD/Opt.) │  │  (Cache Loc.)│          │
│  └──────────────┘  └──────────────┘  └──────────────┘          │
└─────────────────────────────────────────────────────────────────┘
          │
┌─────────▼──────────────────────────────────────────────────────┐
│                    UNITY CORE SERVICES                           │
│  Rendering (URP) | Physics | Memory Management | Threading      │
└─────────────────────────────────────────────────────────────────┘
```

**Tri-Layer Breakdown:**

| Layer | Scale | Coverage | Destructible | Memory | Manager |
|-------|-------|----------|--------------|--------|---------|
| **Base Terrain** | 1.0 unit | 1000x1000m | No | 380 MB | MacroChunkManager |
| **Destructible Overlay** | 0.1 unit | 200x200m (streaming) | Yes | 400 MB | DestructibleOverlayManager |
| **Props/Enemies** | 0.1 unit | Global (spawned) | Yes | Variable | ECS Systems |

**Total Terrain Memory:** 780 MB (380 + 400)

### 2.2 Flow de Données Principal

```
INITIALIZATION PHASE:
1. TerrainSystem → Generate static terrain (once)
2. ChunkManager → Partition world into chunks
3. MeshingSystem → Pre-compute terrain meshes (greedy meshing)
4. CollisionSystem → Bake static mesh colliders
5. PoolingSystem → Pre-allocate mesh/collider pools

RUNTIME PHASE (per frame):
1. CameraSystem → Determine visible frustum
2. ChunkManager → Cull invisible chunks, Update LOD
3. EnemySystem (DOTS) → Update 2000+ enemies positions/states
4. DestructionSystem → Process voxel destructions (if any)
   ├─> MeshingSystem → Regenerate affected meshes (async, Jobs)
   └─> CollisionSystem → Update dynamic colliders
5. RenderingSystem → Batch and render visible meshes (URP)
6. PhysicsSystem → Query collisions (enemies, projectiles)

DESTRUCTION EVENT:
VoxelDestruction → ChunkDirtyFlag → AsyncMeshRebuild (Job) →
MeshPoolAllocation → ColliderSync → VisualEffects (debris)
```

### 2.3 Choix Architectural Majeur: DOTS/ECS Hybrid

#### Pourquoi Hybrid (pas Full ECS) ?

**DOTS/ECS pour:**
- Ennemis (2000+) - Performance critique, data-oriented
- Projectiles - Nombreux, patterns parallélisables
- Meshing Jobs - Calculs lourds, parallélisables
- Destruction processing - Batch operations

**MonoBehaviour pour:**
- Player Controller - Gameplay, inputs, feedbacks
- Camera System - Flexibilité Unity, Cinemachine
- UI/UX - Unity UI, EventSystem
- Game Management - Scene loading, progression

**Justification:**
Unity 6.2 supporte parfaitement l'hybride. Le Full ECS apporterait:
- Complexité développement +300%
- Gains performance marginaux sur MonoBehaviour légers
- Perte tooling Unity (Inspector, Prefabs)

**Trade-off accepté:** Overhead minime de synchronisation ECS<->MonoBehaviour
VS. Developer experience et maintenabilité excellentes.

---

## 3. CHOIX TECHNOLOGIQUES CLÉS

### 3.1 ECS 1.0+ (Unity 6.2)

**Avantages Unity 6.2:**
- ECS 1.0 full release (stable, production-ready)
- Burst Compiler 1.8+ (SIMD auto, optimisations++)
- Job System v2 (scheduling amélioré)
- Meilleur debugging (Entity Inspector)
- Compatibility URP native

**Packages requis:**
```
com.unity.entities: 1.3.x
com.unity.burst: 1.8.x
com.unity.collections: 2.x
com.unity.mathematics: 1.3.x
com.unity.jobs: 0.x
```

### 3.2 URP Optimizations

**Features utilisées:**
- SRP Batcher (batch voxel meshes)
- GPU Instancing (props identiques)
- Occlusion Culling (baked pour terrain)
- Dynamic Batching (petits meshes destructibles)

**Renderer Features custom:**
- VoxelOpaquePass (forward rendering optimisé)
- OutlinePass (ennemis sélection)
- DebrisParticlePass (effets destruction)

---

## 4. MEMORY STRATEGY (2000+ Ennemis + Tri-Layer Voxels)

**UPDATE (ADR-006):** Memory budget révisé pour architecture tri-layer.

### 4.1 Memory Budget (PC/Console)

```
TOTAL BUDGET: ~2GB max (confortable pour PC/Console)

BREAKDOWN (TRI-LAYER ARCHITECTURE):
├─ Base Terrain (Static):        380 MB  (1000x1000m @ 1.0 unit)
│  ├─ Voxel data: 63 MB
│  └─ Meshes + LODs: 317 MB
│
├─ Destructible Overlay:         400 MB  (200x200m @ 0.1 unit, streaming)
│  ├─ Compressed chunks: 12.5 MB (RLE+Palette 15:1)
│  ├─ Render meshes: 150 MB
│  ├─ Collision meshes: 37.5 MB
│  ├─ Memory cache (LRU): 100 MB
│  └─ Buffers: 100 MB
│
├─ Enemy Entities (2000):        170 MB  (ECS components + voxel meshes)
├─ Props/Destructibles:           50 MB  (micro voxels, spawned)
├─ Textures/Materials:           150 MB  (atlases, URP materials)
├─ VFX/Particles:                100 MB  (debris, effects)
├─ Audio/Other:                  100 MB  (sounds, misc)
└─ Reserve/Overhead:             650 MB  (GC, Unity internal)

TOTAL:                          ~2000 MB

TERRAIN TOTAL: 780 MB (380 base + 400 overlay)
```

**Key Changes from Original:**
- Base terrain: 150 MB → 380 MB (includes full LOD chain)
- New: Destructible overlay 400 MB (streaming, cached)
- Adjusted reserve: 850 MB → 650 MB (offset by overlay needs)

### 4.2 Memory Management Strategy

**Zero-Allocation Hot Paths:**
- Native collections (NativeArray, NativeHashMap)
- Struct-based components (no classes en hot path)
- Object pooling (meshes, colliders, VFX)
- Pre-allocated buffers pour meshing

**GC Pressure Minimization:**
- Avoid LINQ en runtime
- StringBuilder pour logs/debug
- Reuse collections (Clear() pas new)
- Struct tuples, pas classes temporaires

---

## 5. THREADING STRATEGY

### 5.1 Thread Allocation (8-core CPU typical PC/Console)

```
MAIN THREAD (Unity Update):
├─ Player Input
├─ Camera Update
├─ Game Logic (state machines)
├─ Render commands submission
└─ Physics queries results collection

WORKER THREADS (Jobs):
├─ Thread 0-1: Enemy AI update (DOTS)
├─ Thread 2-3: Voxel meshing (greedy algorithm)
├─ Thread 4-5: Collision detection batch
├─ Thread 6: Destruction processing
└─ Thread 7: LOD calculations, culling

RENDER THREAD (Unity internal):
└─ URP rendering pipeline
```

### 5.2 Job Dependencies Graph

```
┌──────────────┐
│ FrameStart   │
└──────┬───────┘
       │
   ┌───▼────────────────┐
   │ CameraFrustumJob   │ (determine visible chunks)
   └───┬────────────────┘
       │
   ┌───▼─────────────────┐
   │ ChunkCullingJob     │ (parallel per chunk)
   └───┬─────────────────┘
       │
   ┌───▼──────────────────┐
   │ EnemyUpdateJob       │ (2000+ enemies, batched)
   └───┬──────────────────┘
       │
   ┌───▼───────────────────┐
   │ DestructionProcessJob │ (if destructions occurred)
   └───┬───────────────────┘
       │
   ┌───▼─────────────────┐
   │ MeshRegenerationJob │ (async, can span frames)
   └───┬─────────────────┘
       │
   ┌───▼──────────────────┐
   │ CollisionQueryJob    │ (physics raycasts)
   └───┬──────────────────┘
       │
   ┌───▼─────────────────┐
   │ RenderSubmitSync    │ (main thread sync)
   └─────────────────────┘
```

---

## 6. PERFORMANCE TARGETS & PROFILING

### 6.1 Frame Budget (60 FPS = 16.66ms)

```
BUDGET BREAKDOWN:
├─ Gameplay Logic:           1.0 ms  (player, AI state machines)
├─ ECS Systems:              3.0 ms  (2000+ enemies update)
├─ Voxel Systems:            2.0 ms  (chunking, culling, LOD)
├─ Mesh Regeneration:        1.5 ms  (amortized, async jobs)
├─ Physics:                  2.0 ms  (collision queries, raycasts)
├─ Rendering (URP):          5.0 ms  (draw calls, shaders)
├─ VFX/Particles:            0.5 ms  (debris effects)
├─ Audio:                    0.5 ms  (spatial audio)
└─ Reserve/Spikes:           1.16ms  (safety margin)
────────────────────────────────────
TOTAL:                      16.66ms  (60 FPS exact)
```

### 6.2 Profiling Strategy

**Critical Metrics:**
- Frame time (Unity Profiler)
- Job System utilization (Worker threads saturation)
- ECS Chunk iterations (cache misses)
- Mesh regeneration latency (destruction lag)
- Draw calls count (URP batching effectiveness)
- Memory allocations (GC spikes)

**Tools:**
- Unity Profiler (Deep Profile mode)
- Frame Debugger (draw call analysis)
- Memory Profiler (leaks, fragmentation)
- Burst Inspector (SIMD utilization)
- Custom markers (ProfilerMarker API)

**Optimization Loop:**
1. Profile baseline (vanilla implementation)
2. Identify top 3 bottlenecks
3. Optimize (Burst, Jobs, algorithms)
4. Re-profile, compare gains
5. Iterate until target met

---

## 7. SCALABILITY CONSIDERATIONS

### 7.1 Arena Scaling (1000m → 5000m)

**Current Architecture Supports:**
- Chunk-based streaming (load/unload dynamic)
- LOD system (terrain peut aggressive LOD au loin)
- Enemy pooling (inactive enemies en NativeArray)
- Procedural generation (extend terrain procéduralement)

**Modifications Required for 5000x5000m:**
- Augmenter chunk cache size
- LOD levels supplémentaires (LOD4, LOD5)
- Floating-point precision (origin shifting)
- Async loading (chunks background threads)

### 7.2 Enemy Scaling (2000 → 5000+)

**Bottleneck Analysis:**
- Rendering: GPU instancing résout (identical meshes)
- AI: DOTS parallelize bien (linear scaling)
- Physics: Spatial partitioning critical (octree/grid)
- Meshing: Enemies meshes pré-baked, pas de meshing runtime

**Max Theoretical:** ~10,000 enemies (GPU instancing limit)

---

## 8. RISKS & MITIGATIONS

### 8.1 Identified Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Mesh regeneration lag (destruction) | HIGH | HIGH | Async jobs, amortize sur frames, pooling |
| Vertex limit 65k per mesh | MEDIUM | MEDIUM | Chunk size limit, split large chunks |
| GC spikes from voxel operations | MEDIUM | HIGH | Zero-alloc hot paths, native collections |
| Physics queries overhead (2000 enemies) | HIGH | MEDIUM | Spatial partitioning, batch queries |
| Draw calls explosion (destructibles) | MEDIUM | HIGH | SRP Batcher, GPU instancing, LOD |
| DOTS learning curve | LOW | MEDIUM | Hybrid approach, progressive adoption |

### 8.2 Performance Fallbacks

**Si 60 FPS non atteint:**
1. Reduce enemy detail (LOD agressif)
2. Lower mesh resolution (moins de voxels)
3. Disable debris VFX
4. Reduce shadow quality (URP settings)
5. Chunk view distance reduction

---

## 9. NEXT STEPS

Ce document établit l'architecture globale. Les documents suivants détaillent chaque sous-système:

1. **02_DUAL_SCALE_VOXEL_SYSTEM.md** - Voxels 1.0 + 0.1 unités
2. **03_CHUNK_MANAGEMENT.md** - Spatial partitioning, culling, LOD
3. **04_MESHING_SYSTEM.md** - Greedy meshing, async jobs
4. **05_ECS_ARCHITECTURE.md** - DOTS design, components, systems
5. **06_COLLISION_SYSTEM.md** - Physics, raycasting, queries
6. **07_DESTRUCTION_PIPELINE.md** - Voxel destruction workflow
7. **08_PACKAGE_STRUCTURE.md** - Clean architecture, modules
8. **09_PERFORMANCE_OPTIMIZATION.md** - Burst, SIMD, profiling
9. **10_IMPLEMENTATION_ROADMAP.md** - Phases, MVP, timeline
10. **ADRs/** - Architecture Decision Records

---

**Document Version:** 1.0
**Last Updated:** 2025-11-20
**Author:** Unity Voxel Engine Architect
**Status:** APPROVED FOR IMPLEMENTATION
