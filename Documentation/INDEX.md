# Time Survivor Game - Documentation Index
## Navigation Complète de l'Architecture Voxel

---

## Démarrage Rapide

### Pour comprendre l'architecture globale
**Lire en priorité:**
1. [ARCHITECTURE_EXECUTIVE_SUMMARY.md](./ARCHITECTURE_EXECUTIVE_SUMMARY.md) (10 min)
2. [Architecture/README.md](./Architecture/README.md) (15 min)
3. [Architecture/01_GLOBAL_ARCHITECTURE.md](./Architecture/01_GLOBAL_ARCHITECTURE.md) (20 min)

### Pour implémenter
**Suivre l'ordre:**
1. Lire tous les ADRs (comprendre les décisions)
2. Lire documents Architecture 01-10 dans l'ordre
3. Commencer Phase 1 du Roadmap (10_IMPLEMENTATION_ROADMAP.md)

---

## Structure Complète (18 Documents, ~185 KB) - Updated v1.1

**UPDATE (2025-11-20):** Architecture révisée avec système d'overlay destructible (ADR-006).

```
Documentation/
│
├─ INDEX.md (ce fichier)
├─ ARCHITECTURE_EXECUTIVE_SUMMARY.md ⭐ [Synthèse exécutive 10 min] - UPDATED
│
└─ Architecture/
   │
   ├─ README.md ⭐ [Guide navigation complet]
   │
   ├─ 01_GLOBAL_ARCHITECTURE.md (15 KB) - UPDATED
   │   • Vue d'ensemble système TRI-LAYER
   │   • DOTS/ECS Hybrid strategy
   │   • Memory & Threading (780 MB terrain)
   │   • Performance targets
   │
   ├─ 02_DUAL_SCALE_VOXEL_SYSTEM.md (20 KB)
   │   • Macro voxels 1.0 (terrain)
   │   • Micro voxels 0.1 (props/ennemis)
   │   • Data structures
   │   • Conversion utilities
   │
   ├─ 03_CHUNK_MANAGEMENT.md (30 KB) - UPDATED
   │   • Chunk size 16³ analysis (base terrain)
   │   • Chunk size 32³ analysis (overlay) ◄── NEW SECTION
   │   • Frustum culling & LOD
   │   • Spatial hash (micro objects)
   │   • Dirty tracking & pooling
   │   • Overlay streaming management ◄── NEW
   │
   ├─ 04_MESHING_SYSTEM.md (10 KB)
   │   • Greedy meshing algorithm
   │   • Burst-compiled jobs
   │   • Async remeshing pipeline
   │   • Texture atlas & UVs
   │
   ├─ 05_ECS_ARCHITECTURE.md (10 KB)
   │   • Components & Systems design
   │   • Enemy AI (2000+ entities)
   │   • LOD system
   │   • GPU Instancing rendering
   │
   ├─ 06_COLLISION_SYSTEM.md (7 KB)
   │   • Static terrain (MeshCollider)
   │   • Dynamic objects (Box/Compound)
   │   • Voxel raycasting (DDA)
   │   • Spatial queries optimization
   │
   ├─ 07_DESTRUCTION_PIPELINE.md (4 KB)
   │   • Voxel removal jobs
   │   • Debris VFX system
   │   • Amortized processing
   │   • Collider synchronization
   │
   ├─ 08_PACKAGE_STRUCTURE.md (4 KB)
   │   • Modular package organization
   │   • Clean architecture (SOLID)
   │   • Assembly definitions
   │   • Dependency graph
   │
   ├─ 09_PERFORMANCE_OPTIMIZATION.md (4 KB)
   │   • Burst Compiler strategies
   │   • Memory zero-allocation
   │   • GPU optimizations (URP)
   │   • Profiling methodology
   │
   ├─ 10_IMPLEMENTATION_ROADMAP.md (5 KB) - UPDATED
   │   • Phase 1: MVP (4-6 weeks)
   │   • Phase 1.5: Destructible Overlay (6 weeks) ◄── NEW PHASE
   │   • Phase 2: Props/Enemies (3-4 weeks)
   │   • Phase 3: ECS & Enemies (5-6 weeks)
   │   • Phase 4: Polish (3-4 weeks)
   │   • Total: 21-26 weeks (updated from 15-20)
   │
   ├─ 11_DESTRUCTIBLE_OVERLAY_SYSTEM.md (35 KB) ◄── NEW DOCUMENT
   │   • Tri-layer architecture overview
   │   • Streaming system (follow player)
   │   • Compression (RLE + Palette)
   │   • Blending base ↔ overlay
   │   • Dual-layer collision
   │   • Destruction API
   │   • Memory management (400 MB)
   │   • Testing strategy
   │
   └─ ADRs/ (Architecture Decision Records)
      │
      ├─ ADR-001-DUAL-SCALE-VOXEL-SYSTEM.md - SUPERSEDED
      │   Status: SUPERSEDED by ADR-006
      │   Decision: Pourquoi 1.0 + 0.1 (pas échelle unique)
      │   Rationale: 514 MB vs 50+ GB mémoire
      │
      ├─ ADR-002-DOTS-ECS-HYBRID.md
      │   Decision: Hybrid ECS/MonoBehaviour
      │   Rationale: 60 FPS (2000 enemies) + Dev experience
      │
      ├─ ADR-003-GREEDY-MESHING-ALGORITHM.md
      │   Decision: Greedy meshing (pas naive/culled)
      │   Rationale: 24x vertex reduction
      │
      ├─ ADR-004-CHUNK-SIZE-16x16x16.md - UPDATED
      │   Decision: Chunks 16³ base, 32³ overlay
      │   Rationale: Vertex safety + Culling optimal
      │   Update: Added 32³ analysis for overlay
      │
      ├─ ADR-005-AMORTIZED-REMESHING.md
      │   Decision: Budget 2ms/frame remeshing
      │   Rationale: Frame stability vs latency
      │
      └─ ADR-006-DESTRUCTIBLE-OVERLAY-SYSTEM.md ◄── NEW ADR
          Decision: Tri-layer avec overlay destructible 200x200m
          Rationale: 780 MB vs 10+ GB, destruction détaillée
          Implementation: Complete specs avec pseudocode C#
```

---

## Navigation Par Thématique

### Performance & Optimisation
- 01_GLOBAL_ARCHITECTURE.md (section 6-8)
- 04_MESHING_SYSTEM.md (section 7)
- 05_ECS_ARCHITECTURE.md (section 6)
- 09_PERFORMANCE_OPTIMIZATION.md (complet)
- ADR-003, ADR-005

### Voxel Data & Structures
- 02_DUAL_SCALE_VOXEL_SYSTEM.md (complet)
- 03_CHUNK_MANAGEMENT.md (sections 2-3)
- ADR-001, ADR-004

### Rendering & Meshing
- 04_MESHING_SYSTEM.md (complet)
- 03_CHUNK_MANAGEMENT.md (section 4: LOD)
- ADR-003

### DOTS/ECS & Enemies
- 05_ECS_ARCHITECTURE.md (complet)
- 01_GLOBAL_ARCHITECTURE.md (section 2.3)
- ADR-002

### Physics & Collision
- 06_COLLISION_SYSTEM.md (complet)
- 02_DUAL_SCALE_VOXEL_SYSTEM.md (section 5.2)

### Destruction & VFX
- 07_DESTRUCTION_PIPELINE.md (complet)
- 03_CHUNK_MANAGEMENT.md (section 6)
- ADR-005

### Code Organization
- 08_PACKAGE_STRUCTURE.md (complet)
- 01_GLOBAL_ARCHITECTURE.md (section 8)

---

## Workflow de Lecture Recommandé

### 1. Premier Contact (30 minutes)
```
ARCHITECTURE_EXECUTIVE_SUMMARY.md
   ↓
Architecture/README.md
   ↓
ADRs (tous, rapidement)
```

### 2. Compréhension Approfondie (3 heures)
```
01_GLOBAL_ARCHITECTURE.md (vue d'ensemble)
   ↓
02_DUAL_SCALE_VOXEL_SYSTEM.md (fondation)
   ↓
03_CHUNK_MANAGEMENT.md (spatial partitioning)
   ↓
04_MESHING_SYSTEM.md (rendering)
   ↓
05_ECS_ARCHITECTURE.md (enemies)
   ↓
06_COLLISION_SYSTEM.md (physics)
   ↓
07_DESTRUCTION_PIPELINE.md (gameplay)
```

### 3. Implémentation (référence continue)
```
10_IMPLEMENTATION_ROADMAP.md (guide phases)
   ↓
08_PACKAGE_STRUCTURE.md (setup projet)
   ↓
Documents 01-07 (référence selon phase)
   ↓
09_PERFORMANCE_OPTIMIZATION.md (profiling continu)
```

---

## Statistiques Documentation - Updated v1.1

- **Total documents**: 18 fichiers Markdown (+1 nouveau)
- **Taille totale**: ~185 KB (+33 KB)
- **Pages estimées**: ~75 pages (+15 pages)
- **Temps lecture**: ~6 heures (complète)
- **Temps lecture rapide**: 30 minutes (executive summary)

### Breakdown par Type
- **Documents Architecture**: 11 fichiers (142 KB) - +1 nouveau document
- **ADRs**: 6 fichiers (30 KB) - +1 nouveau ADR
- **Guides**: 2 fichiers (README, Executive Summary)

---

## Utilisation

### Pour l'Architecte (Vous)
Documentation COMPLÈTE, prête pour handoff au développeur.
Tous les choix sont documentés et justifiés (ADRs).

### Pour le Développeur C# Unity
1. Lire Executive Summary (10 min)
2. Lire ADRs (comprendre "pourquoi")
3. Implémenter Phase 1 (référencer docs 01-04)
4. Itérer phases suivantes

### Pour Review/Validation
- Executive Summary suffit (décisions clés)
- ADRs pour comprendre trade-offs
- Performance targets (section dans chaque doc)

---

## Maintenance Documentation

### Mises à Jour Futures
Si architecture évolue, mettre à jour :
1. ADR correspondant (ajouter version 2.0)
2. Document(s) architecture impacté(s)
3. README.md (changelog)
4. Executive Summary (si décision majeure)

### Versioning
- **v1.0 (2025-11-20)**: Architecture initiale complète (dual-scale)
- **v1.1 (2025-11-20)**: Révision architecture tri-layer avec overlay destructible
  - ADR-006 ajouté (Destructible Overlay System)
  - Document 11 créé (implementation guide)
  - ADR-001 superseded, ADR-004 updated
  - Documents 01, 03, 10 mis à jour
  - Timeline: 15-20 → 21-26 semaines
  - Memory: 380 → 780 MB terrain
- Futures versions: Incrémenter selon changements

---

## Fichiers Clés Absolus

| Fichier | Chemin Absolu |
|---------|---------------|
| Executive Summary | `/Users/etherny/Documents/work/games/TimeSurvivorGame/Documentation/ARCHITECTURE_EXECUTIVE_SUMMARY.md` |
| Architecture README | `/Users/etherny/Documents/work/games/TimeSurvivorGame/Documentation/Architecture/README.md` |
| Global Architecture | `/Users/etherny/Documents/work/games/TimeSurvivorGame/Documentation/Architecture/01_GLOBAL_ARCHITECTURE.md` |
| Implementation Roadmap | `/Users/etherny/Documents/work/games/TimeSurvivorGame/Documentation/Architecture/10_IMPLEMENTATION_ROADMAP.md` |

---

## Prochaines Actions

1. **Lire Executive Summary** (maintenant, 10 min)
2. **Parcourir ADRs** (comprendre décisions, 20 min)
3. **Valider Architecture** avec équipe si applicable
4. **Démarrer Phase 1** (référencer Roadmap)
5. **Déléguer Implémentation** au développeur C# Unity

---

**Documentation Status**: COMPLETE ✓
**Architecture Status**: APPROVED FOR IMPLEMENTATION ✓
**Version**: 1.1 (Tri-Layer Architecture)
**Last Updated**: 2025-11-20

**Major Changes v1.1:**
- Architecture étendue : Dual-scale → Tri-layer
- Nouveau système : Destructible Overlay (200x200m streaming)
- Memory terrain : 380 MB → 780 MB (acceptable)
- Timeline : 21-26 semaines (vs 15-20 original)
- Nouveau document : 11_DESTRUCTIBLE_OVERLAY_SYSTEM.md (35 KB)
- Nouveau ADR : ADR-006 (Tri-Layer System)

---

Bonne lecture et bon développement !
