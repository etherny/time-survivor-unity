# ADR-005: Amortized Remeshing Strategy

**Status:** ACCEPTED
**Date:** 2025-11-20
**Decision Makers:** Voxel Engine Architect
**Scope:** Destruction System, Performance Stability

---

## Context

Destructions voxel nécessitent regeneration meshes. Destructions massives (100 voxels simultanés) peuvent causer lag spikes.

### Options Considérées

**Option 1: Immediate Remeshing**
- Remesh immédiatement après destruction
- Simple, synchrone
- LAG SPIKES énormes

**Option 2: Full Async (No Limit)**
- Tout en jobs asynchrones
- Pas de frame spikes
- Latence visuelle (plusieurs frames delay)

**Option 3: Amortized Budget (CHOSEN)**
- Budget temps par frame (2ms)
- Queue destructions
- Spread over frames
- Balance latency/performance

---

## Decision

**CHOISI: Option 3 - Amortized Remeshing avec Budget**

Système de queue avec :
- **Budget**: 2ms max remeshing per frame
- **Queue**: FIFO destructions
- **Priority**: Near-camera prioritized
- **Async Jobs**: Chaque remesh en Job Burst

---

## Consequences

### Positive

- **Frame Stability**: Jamais >2ms spike (60 FPS garanti)
- **Scalability**: 100 destructions simultanées = spread 5-10 frames
- **Predictability**: Budget constant, pas de surprises

### Negative

- **Latency**: 1-3 frames delay avant mesh update visible
- **Complexity**: Queue management, priority system
- **Memory**: Queue storage (~100 KB max)

### Neutral

- **Visual Impact**: 1-3 frame latency imperceptible (16-50ms)

---

## Rationale

**Immediate Remeshing:**
```
100 destructions × 0.5ms meshing = 50ms spike
60 FPS budget: 16.66ms
Spike: 50ms → 20 FPS drop → UNACCEPTABLE
```

**Full Async (No Limit):**
```
1000 jobs queued → Unity Job scheduler saturated
Worker threads busy → other systems starved
Unpredictable latency (1-10 frames)
```

**Amortized (2ms budget):**
```
100 destructions queued
2ms budget = ~4 remeshing jobs/frame
100 jobs / 4 = 25 frames = 416ms total
User perception: Smooth, no spikes ✓
```

---

## Performance Validation

```
WORST-CASE SCENARIO:
- 500 props destroyed (explosion)
- 500 × 0.5ms = 250ms total work
- Spread: 250ms / 2ms per frame = 125 frames = 2 seconds
- Visual: Debris appear progressively (ACCEPTABLE)

TYPICAL SCENARIO:
- 10 destructions/frame (combat)
- 10 × 0.5ms = 5ms
- Budget 2ms: 2-3 frames latency
- User perception: Instant ✓
```

---

## Implementation Notes

- Priority queue (near-camera first)
- JobHandle pooling (reuse handles)
- Visual masking (debris VFX hide latency)
- Profiler markers (track budget usage)

```csharp
class AmortizedRemeshingScheduler {
    float budgetMs = 2.0f;
    Queue<RemeshJob> queue;

    void ProcessQueue() {
        float elapsed = 0f;
        while (queue.Count > 0 && elapsed < budgetMs) {
            var job = queue.Dequeue();
            ExecuteRemesh(job);
            elapsed = MeasureElapsed();
        }
    }
}
```

---

**References:** 07_DESTRUCTION_PIPELINE.md, 04_MESHING_SYSTEM.md
