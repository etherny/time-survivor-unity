# Implementation Roadmap - Phases MVP à Production

---

## PHASE 1: MVP FOUNDATION (4-6 weeks)

### Scope
- Terrain statique basique (macro voxels 1.0)
- Chunk management simple (pas de streaming)
- Greedy meshing basique
- Collision terrain statique
- Camera top-down 45°

### Deliverables
- [ ] VoxelType enum et structures de données
- [ ] MacroChunk struct avec NativeArray
- [ ] Terrain generator (Perlin noise simple)
- [ ] Greedy meshing algorithm (Burst)
- [ ] MacroChunkManager avec spatial hash
- [ ] MeshCollider baking pour terrain
- [ ] Camera controller basique

### Success Criteria
- Terrain 1000x1000m généré et affiché
- 60 FPS stable (terrain seul)
- Player peut se déplacer sur terrain
- Collision terrain fonctionne

### Estimated Time: 4-6 weeks

---

## PHASE 1.5: DESTRUCTIBLE OVERLAY FOUNDATION (6 weeks)

**UPDATE (ADR-006):** Nouvelle phase pour implémenter le système d'overlay destructible.

### Scope
- Destructible overlay layer (200x200m @ 0.1 unit)
- Streaming system (follow player)
- Compression (RLE + Palette)
- Blending avec base terrain
- Dual-layer collision

### Deliverables
- [ ] CompressedVoxelData struct (RLE+Palette compression)
- [ ] OverlayChunk32 struct (32x32x32 voxels @ 0.1)
- [ ] DestructibleOverlayManager (main coordinator)
- [ ] StreamingCoordinator (load/unload logic)
- [ ] OverlayCacheManager (memory + disk cache)
- [ ] BlendingController (base ↔ overlay rendering)
- [ ] DualLayerCollision (raycast priority system)
- [ ] VoxelDestructionAPI (public interface)
- [ ] AmortizedRemeshingManager (overlay queue)

### Implementation Breakdown

**Week 5-6: Data Structures & Compression**
- [ ] Implement CompressedVoxelData
  - RLE encoding/decoding (Burst-compiled)
  - Palette-based compression
  - Unit tests: Compression ratios (target 15:1)
  - Memory profiling: Validate 2 KB per chunk average
- [ ] Implement OverlayChunk32
  - 32x32x32 voxel storage
  - Get/Set voxel methods
  - Dirty tracking
  - Memory size calculation

**Week 7-8: Streaming System**
- [ ] Implement DestructibleOverlayManager
  - Player tracking (follow logic)
  - Chunk activation/deactivation
  - Pending modifications queue
- [ ] Implement StreamingCoordinator
  - Distance-based streaming evaluation
  - Load/unload queues
  - Frame budget management (2ms)
  - Predictive loading (velocity-based)
- [ ] Implement OverlayCacheManager
  - LRU memory cache (100 MB)
  - Disk cache serialization
  - Cache hit rate tracking

**Week 9-10: Blending & Collision**
- [ ] Implement BlendingController
  - Overlay → Base chunk mapping
  - Disable base rendering in overlay zone
  - Re-enable when overlay unloads
  - Handle overlapping cases
- [ ] Implement DualLayerCollision
  - Raycast priority (overlay first)
  - PhysX integration (separate layers)
  - Simplified collision mesh (8x8x8)
  - DDA voxel traversal algorithm
- [ ] Test collision accuracy and performance

**Week 11-12: Destruction API & Integration**
- [ ] Implement VoxelDestructionAPI
  - DestroyVoxel(worldPos)
  - DestroySphere(center, radius)
  - DestroyBox(center, size)
  - IsDestructible(worldPos) query
- [ ] Implement VoxelSphereDestructionJob (Burst)
- [ ] Integrate with MeshingSystem
  - Separate remesh queue for overlay
  - Priority system (overlay > base)
- [ ] Integrate with VFX system
  - Debris spawning on destruction
  - Particle pooling
- [ ] Performance profiling
  - Streaming: Target <2ms per frame
  - Destruction: 100 simultaneous @ 60 FPS
  - Memory: Peak <450 MB overlay

### Success Criteria
- 200x200m overlay zone active and streaming
- Player movement: Smooth transitions, no hitches
- Destruction: Detailed (0.1 unit voxels) near player
- Performance: 60 FPS with 2000 enemies + overlay
- Memory: <450 MB overlay (within 780 MB terrain total)
- Streaming: Visible chunks load <0.5s

### Estimated Time: 6 weeks

---

## PHASE 2: PROPS/ENEMIES MICRO VOXELS (3-4 weeks)

### Scope
- Micro voxel system (0.1 unit)
- Props destructibles (crates, barrels)
- Destruction pipeline basique
- Debris VFX
- Dual-scale architecture

### Deliverables
- [ ] MicroChunk struct (32³ voxels)
- [ ] VoxelModelAsset ScriptableObject
- [ ] MicroVoxelFactory (props creation)
- [ ] VoxelDestructionJob (Burst)
- [ ] DestructionManager avec amortization
- [ ] VoxelDebrisSpawner (pooling)
- [ ] Dynamic collider update

### Success Criteria
- 100+ props destructibles simultanés
- Destruction fluide (60 FPS)
- VFX debris convaincants
- Collisions dynamiques précises

### Estimated Time: 3-4 weeks

---

## PHASE 3: ECS & 2000+ ENEMIES (5-6 weeks)

### Scope
- DOTS/ECS integration
- Enemy voxel entities (hyper détaillés)
- AI system (chase player)
- LOD system multi-niveaux
- GPU Instancing
- Spatial hash optimization

### Deliverables
- [ ] ECS Components (Enemy, Voxel, AI)
- [ ] EnemyAISystem (Burst, parallel)
- [ ] SpatialHashUpdateSystem
- [ ] LODSystem (4 niveaux)
- [ ] VoxelInstanceRenderer (GPU instancing)
- [ ] Enemy spawning system
- [ ] Hybrid GameObject<->ECS sync

### Success Criteria
- 2000+ enemies à l'écran
- 60 FPS stable (toutes plateformes)
- AI performante (chase, avoid)
- LOD transitions seamless

### Estimated Time: 5-6 weeks

---

## PHASE 4: POLISH & OPTIMIZATIONS (3-4 weeks)

### Scope
- Performance profiling complet
- Optimisations finales (Burst, SIMD)
- Memory leak fixes
- URP rendering polish
- Editor tools
- Documentation code

### Deliverables
- [ ] Unity Profiler analysis complète
- [ ] Burst Inspector optimizations
- [ ] Memory Profiler leak fixes
- [ ] SRP Batcher compatibility
- [ ] Custom Editor tools (voxel painter)
- [ ] Code documentation (XML comments)
- [ ] Performance benchmarks

### Success Criteria
- 60 FPS garanti (worst-case: 2000 enemies + 500 props)
- Memory stable (no leaks)
- Draw calls optimisés (<500)
- Build size acceptable (<500 MB)

### Estimated Time: 3-4 weeks

---

## TOTAL ESTIMATED TIME: 21-26 weeks (5-6.5 mois)

**UPDATE (ADR-006):** Timeline révisée avec Phase 1.5 (overlay destructible).

```
ORIGINAL TIMELINE:  15-20 weeks
PHASE 1.5 ADDITION: +6 weeks
NEW TIMELINE:       21-26 weeks

BREAKDOWN:
- Phase 1:   4-6 weeks  (MVP Foundation)
- Phase 1.5: 6 weeks    (Destructible Overlay) ◄── NEW
- Phase 2:   3-4 weeks  (Props/Enemies Micro Voxels)
- Phase 3:   5-6 weeks  (ECS & 2000+ Enemies)
- Phase 4:   3-4 weeks  (Polish & Optimizations)

TOTAL: 21-26 weeks
```

**Note:** Phase 1.5 peut être parallélisée partiellement avec Phase 2 si équipe >1 développeur.

---

## RISK MITIGATION

| Risk | Impact | Mitigation |
|------|--------|------------|
| Performance non atteinte (Phase 3) | HIGH | Early profiling, LOD fallbacks |
| DOTS learning curve | MEDIUM | Prototypes early, hybrid approach |
| Meshing lag (destructions) | HIGH | Amortization, async jobs |
| Memory overflow | MEDIUM | Profiling Phase 1-2, pooling |

---

## DEPENDENCIES

- Unity 6.2+ (6000.2.12f1)
- URP 17.x
- Entities 1.3+
- Burst 1.8+
- Mathematics 1.3+
- Collections 2.x

---

**Document Version:** 1.0
