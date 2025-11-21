# Destruction Pipeline - Voxel Destruction Workflow

---

## 1. DESTRUCTION EVENT FLOW

```
Projectile Hit → Identify Affected Voxels → Remove Voxels → Mark Dirty →
Schedule Remesh (Async) → Update Collider → Spawn VFX (Debris)
```

---

## 2. VOXEL REMOVAL JOB

```csharp
[BurstCompile]
public struct VoxelDestructionJob : IJob {
    public NativeArray<VoxelType> voxels;
    public int chunkSize;
    public float3 impactPoint;
    public float radius;

    public NativeReference<int> destroyedCount;

    public void Execute() {
        int3 center = (int3)math.floor(impactPoint / 0.1f); // Micro voxel size
        int radiusVoxels = (int)math.ceil(radius / 0.1f);

        for (int x = -radiusVoxels; x <= radiusVoxels; x++) {
            for (int y = -radiusVoxels; y <= radiusVoxels; y++) {
                for (int z = -radiusVoxels; z <= radiusVoxels; z++) {
                    int3 pos = center + new int3(x, y, z);

                    if (math.distance(pos, center) <= radiusVoxels) {
                        int index = GetIndex(pos);
                        if (index >= 0 && index < voxels.Length && voxels[index] != VoxelType.Air) {
                            voxels[index] = VoxelType.Air;
                            destroyedCount.Value++;
                        }
                    }
                }
            }
        }
    }

    int GetIndex(int3 pos) {
        if (pos.x < 0 || pos.x >= chunkSize) return -1;
        if (pos.y < 0 || pos.y >= chunkSize) return -1;
        if (pos.z < 0 || pos.z >= chunkSize) return -1;
        return pos.x + pos.y * chunkSize + pos.z * chunkSize * chunkSize;
    }
}
```

---

## 3. DEBRIS VFX SYSTEM

```csharp
public class VoxelDebrisSpawner : MonoBehaviour {

    public GameObject debrisPrefab;
    ObjectPool<GameObject> debrisPool;

    void Awake() {
        debrisPool = new ObjectPool<GameObject>(
            () => Instantiate(debrisPrefab),
            go => go.SetActive(true),
            go => go.SetActive(false),
            go => Destroy(go),
            maxSize: 500
        );
    }

    public void SpawnDebris(float3 position, VoxelType voxelType, int count) {
        for (int i = 0; i < count; i++) {
            var debris = debrisPool.Get();
            debris.transform.position = position + UnityEngine.Random.insideUnitSphere * 0.5f;

            var rb = debris.GetComponent<Rigidbody>();
            rb.velocity = UnityEngine.Random.insideUnitSphere * 5f;
            rb.angularVelocity = UnityEngine.Random.insideUnitSphere * 10f;

            StartCoroutine(ReturnToPool(debris, 2f));
        }
    }

    IEnumerator ReturnToPool(GameObject debris, float delay) {
        yield return new WaitForSeconds(delay);
        debrisPool.Release(debris);
    }
}
```

---

## 4. AMORTIZED DESTRUCTION

```csharp
public class DestructionManager : MonoBehaviour {

    Queue<DestructionEvent> eventQueue = new Queue<DestructionEvent>();
    float destructionBudgetMs = 1.5f;

    public void QueueDestruction(MicroVoxelObject obj, float3 impactPoint, float radius) {
        eventQueue.Enqueue(new DestructionEvent {
            obj = obj,
            impactPoint = impactPoint,
            radius = radius,
            timestamp = Time.time
        });
    }

    void Update() {
        float startTime = Time.realtimeSinceStartup;

        while (eventQueue.Count > 0) {
            float elapsed = (Time.realtimeSinceStartup - startTime) * 1000f;
            if (elapsed > destructionBudgetMs) break;

            ProcessDestruction(eventQueue.Dequeue());
        }
    }

    void ProcessDestruction(DestructionEvent evt) {
        var job = new VoxelDestructionJob {
            voxels = evt.obj.chunk.voxels,
            chunkSize = MicroChunk.SIZE,
            impactPoint = evt.impactPoint,
            radius = evt.radius,
            destroyedCount = new NativeReference<int>(Allocator.TempJob)
        };

        job.Schedule().Complete();

        // Mark for remesh
        evt.obj.chunk.isDirty = true;
        chunkDirtyTracker.MarkDirty(evt.obj.handle);

        // Spawn debris
        debrisSpawner.SpawnDebris(evt.impactPoint, VoxelType.WoodCrate, job.destroyedCount.Value / 10);

        job.destroyedCount.Dispose();
    }
}

struct DestructionEvent {
    public MicroVoxelObject obj;
    public float3 impactPoint;
    public float radius;
    public float timestamp;
}
```

---

**Document Version:** 1.0
