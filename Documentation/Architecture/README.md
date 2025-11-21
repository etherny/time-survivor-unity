# Architecture Voxel Engine - Time Survivor Game
## Documentation Complète & Spécifications Techniques

---

## Vue d'Ensemble

Cette documentation contient l'architecture complète d'un **moteur voxel haute performance** conçu pour un jeu type Vampire Survivor avec :
- 2000+ ennemis simultanés
- Terrain voxel 1000x1000m
- Destructions massives en temps réel
- 60 FPS garanti (PC/Console)

---

## Structure Documentation

### Documents Architecturaux (Ordre de Lecture)

1. **[01_GLOBAL_ARCHITECTURE.md](./01_GLOBAL_ARCHITECTURE.md)**
   - Vue d'ensemble système complet
   - Choix technologiques (DOTS/ECS Hybrid, URP)
   - Memory strategy & threading
   - Performance targets & profiling

2. **[02_DUAL_SCALE_VOXEL_SYSTEM.md](./02_DUAL_SCALE_VOXEL_SYSTEM.md)**
   - Macro voxels (1.0 unit) pour terrain
   - Micro voxels (0.1 unit) pour props/ennemis
   - Data structures optimales
   - Conversion utilities

3. **[03_CHUNK_MANAGEMENT.md](./03_CHUNK_MANAGEMENT.md)**
   - Spatial partitioning (16x16x16 chunks)
   - Frustum culling & LOD system
   - Chunk pooling & memory management
   - Dirty tracking & remeshing

4. **[04_MESHING_SYSTEM.md](./04_MESHING_SYSTEM.md)**
   - Greedy meshing algorithm (Burst-compiled)
   - Async remeshing pipeline
   - Texture atlas & UVs
   - LOD mesh generation

5. **[05_ECS_ARCHITECTURE.md](./05_ECS_ARCHITECTURE.md)**
   - DOTS/ECS components & systems
   - Enemy AI (2000+ entities)
   - GPU Instancing rendering
   - Hybrid GameObject<->ECS

6. **[06_COLLISION_SYSTEM.md](./06_COLLISION_SYSTEM.md)**
   - Static terrain (MeshCollider baked)
   - Dynamic objects (Box/Compound colliders)
   - Voxel raycasting (DDA algorithm)
   - Spatial hash queries

7. **[07_DESTRUCTION_PIPELINE.md](./07_DESTRUCTION_PIPELINE.md)**
   - Voxel removal jobs
   - Debris VFX system
   - Amortized destruction processing
   - Collider synchronization

8. **[08_PACKAGE_STRUCTURE.md](./08_PACKAGE_STRUCTURE.md)**
   - Modular package organization
   - Clean architecture (SOLID)
   - Assembly definitions
   - Dependency graph

9. **[09_PERFORMANCE_OPTIMIZATION.md](./09_PERFORMANCE_OPTIMIZATION.md)**
   - Burst Compiler optimizations
   - Memory zero-allocation strategies
   - GPU optimizations (URP)
   - Profiling strategy

10. **[10_IMPLEMENTATION_ROADMAP.md](./10_IMPLEMENTATION_ROADMAP.md)**
    - Phase 1: MVP Foundation (4-6 weeks)
    - Phase 2: Destructibles (3-4 weeks)
    - Phase 3: ECS & Enemies (5-6 weeks)
    - Phase 4: Polish (3-4 weeks)
    - Total: 15-20 weeks

---

### Architecture Decision Records (ADRs)

Les ADRs documentent toutes les décisions architecturales majeures avec leur contexte, options considérées, et rationale.

- **[ADR-001: Dual-Scale Voxel System](./ADRs/ADR-001-DUAL-SCALE-VOXEL-SYSTEM.md)**
  - Pourquoi deux échelles (1.0 + 0.1) au lieu d'une
  - Analyse mémoire & performance

- **[ADR-002: DOTS/ECS Hybrid](./ADRs/ADR-002-DOTS-ECS-HYBRID.md)**
  - Pourquoi Hybrid au lieu de Full ECS ou Pure MonoBehaviour
  - Trade-offs performance vs developer experience

- **[ADR-003: Greedy Meshing Algorithm](./ADRs/ADR-003-GREEDY-MESHING-ALGORITHM.md)**
  - Pourquoi greedy meshing (24x vertex reduction)
  - Comparaison avec naive/culled/dual contouring

- **[ADR-004: Chunk Size 16x16x16](./ADRs/ADR-004-CHUNK-SIZE-16x16x16.md)**
  - Pourquoi 16³ au lieu de 8³, 32³, ou 64³
  - Vertex limit safety, culling granularity

- **[ADR-005: Amortized Remeshing](./ADRs/ADR-005-AMORTIZED-REMESHING.md)**
  - Pourquoi budget 2ms/frame pour remeshing
  - Frame stability vs latency trade-off

---

## Spécifications Techniques Clés

### Performance Targets

```
60 FPS (16.66ms per frame):
├─ Gameplay Logic:      1.0 ms
├─ ECS Systems:         3.0 ms  (2000 enemies)
├─ Voxel Systems:       2.0 ms  (chunking, LOD)
├─ Meshing:             1.5 ms  (amortized)
├─ Physics:             2.0 ms  (collisions)
├─ Rendering (URP):     5.0 ms  (draw calls)
├─ VFX/Audio:           1.0 ms
└─ Reserve:             1.16ms
──────────────────────────────
TOTAL:                 16.66ms ✓
```

### Memory Budget

```
TOTAL: ~2 GB (PC/Console)

├─ Terrain Static:     384 MB  (15,876 chunks, LODs)
├─ Enemies (2000):     174 MB  (ECS + voxel data)
├─ Props:               50 MB  (destructibles)
├─ Textures:           150 MB  (atlases, URP)
├─ VFX/Audio:          100 MB
└─ Reserve:           1142 MB  (safety margin)
```

### Technologies

- **Unity**: 6.2 (6000.2.12f1)
- **Rendering**: URP 17.x
- **ECS**: Entities 1.3+
- **Burst**: 1.8+
- **Mathematics**: 1.3+
- **Collections**: 2.x

---

## Quick Start Implementation

### Ordre d'Implémentation Recommandé

1. **Setup Packages** (08_PACKAGE_STRUCTURE.md)
   - Créer structure modulaire
   - Assembly definitions

2. **Core Data Structures** (02_DUAL_SCALE_VOXEL_SYSTEM.md)
   - VoxelType enum
   - MacroChunk, MicroChunk structs
   - Coordinate utilities

3. **Terrain Generation** (01_GLOBAL_ARCHITECTURE.md)
   - Perlin noise generator
   - Chunk creation

4. **Greedy Meshing** (04_MESHING_SYSTEM.md)
   - Burst-compiled meshing job
   - Async scheduler

5. **Chunk Management** (03_CHUNK_MANAGEMENT.md)
   - Spatial hash
   - Frustum culling
   - LOD system

6. **ECS Integration** (05_ECS_ARCHITECTURE.md)
   - Components & Systems
   - Enemy spawning
   - AI simple

7. **Destruction System** (07_DESTRUCTION_PIPELINE.md)
   - Voxel removal jobs
   - Amortized remeshing
   - VFX debris

8. **Collision & Physics** (06_COLLISION_SYSTEM.md)
   - MeshCollider baking
   - Raycasting
   - Spatial queries

9. **Optimizations** (09_PERFORMANCE_OPTIMIZATION.md)
   - Profiling
   - Burst tuning
   - Memory pooling

---

## Validation & Testing

### Performance Benchmarks

Tester à chaque phase :
- **Terrain**: 1000x1000m @ 60 FPS
- **Enemies**: 2000 entities @ 60 FPS
- **Destructions**: 100 simultanées, pas de spike >2ms
- **Memory**: Stable, no leaks, <2 GB

### Profiling Points

```csharp
using Unity.Profiling;

// Markers critiques
ProfilerMarker s_ChunkMeshing = new("VoxelMeshing");
ProfilerMarker s_EnemyUpdate = new("EnemyAI");
ProfilerMarker s_Destruction = new("VoxelDestruction");

// Counters
ProfilerCounter<int> s_ActiveChunks = new("Active Chunks");
ProfilerCounter<int> s_EnemyCount = new("Enemy Count");
```

---

## Risques & Mitigations

| Risque | Probabilité | Impact | Mitigation |
|--------|-------------|--------|------------|
| Performance <60 FPS | MEDIUM | HIGH | Early profiling, LOD fallbacks |
| Memory overflow | LOW | HIGH | Pooling, profiling Phase 1-2 |
| Meshing lag spikes | MEDIUM | HIGH | Amortization 2ms budget |
| DOTS learning curve | LOW | MEDIUM | Hybrid approach, docs |

---

## Contacts & Support

- **Architecture**: Ce document (read-only specifications)
- **Implementation**: Déléguer à `unity-csharp-developer` agent
- **Questions**: Consulter ADRs pour rationale

---

## Changelog

- **2025-11-20**: Version 1.0 - Architecture initiale complète
  - 10 documents architecturaux
  - 5 ADRs
  - Roadmap 4 phases
  - Performance targets validés

---

## Licence & Usage

Architecture documentaire pour **Time Survivor Game**.
Propriété exclusive du projet.

---

**Status**: APPROVED FOR IMPLEMENTATION
**Version**: 1.0
**Last Updated**: 2025-11-20
**Total Pages**: ~50 (10 docs + 5 ADRs)
