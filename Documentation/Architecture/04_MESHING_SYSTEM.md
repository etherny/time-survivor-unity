# Meshing System - Greedy Algorithm & Burst Optimization
## Ultra-Performant Mesh Generation pour Voxels

---

## 1. ALGORITHME: GREEDY MESHING

### Principe
- Combine voxels adjacents identiques en quads larges
- Réduit drastiquement vertex count (vs naive cubes)
- Culling faces internes automatique

### Performance vs Naive

| Method | Vertices (chunk 16³) | Gain |
|--------|---------------------|------|
| Naive (6 faces/voxel) | ~12,000 | Baseline |
| Culled (faces visibles) | ~3,000 | 4x |
| Greedy | ~500 | 24x |

---

## 2. GREEDY MESHING ALGORITHM (Burst)

```csharp
[BurstCompile]
public struct GreedyMeshingJob : IJob {
    [ReadOnly] public NativeArray<VoxelType> voxels;
    public int chunkSize;

    [WriteOnly] public NativeList<Vector3> vertices;
    [WriteOnly] public NativeList<int> triangles;
    [WriteOnly] public NativeList<Vector3> normals;
    [WriteOnly] public NativeList<Vector2> uvs;

    public void Execute() {
        // Process each axis (X, Y, Z) and direction (+/-)
        for (int axis = 0; axis < 3; axis++) {
            for (int direction = -1; direction <= 1; direction += 2) {
                ProcessSlice(axis, direction);
            }
        }
    }

    void ProcessSlice(int axis, int direction) {
        int u = (axis + 1) % 3;
        int v = (axis + 2) % 3;

        int3 x = new int3(0);
        int3 q = new int3(0);
        q[axis] = 1;

        var mask = new NativeArray<VoxelType>(
            chunkSize * chunkSize,
            Allocator.Temp
        );

        // Sweep through slice layers
        for (x[axis] = -1; x[axis] < chunkSize; ) {
            int n = 0;

            // Compute mask
            for (x[v] = 0; x[v] < chunkSize; x[v]++) {
                for (x[u] = 0; x[u] < chunkSize; x[u]++) {
                    VoxelType current = GetVoxel(x);
                    VoxelType next = GetVoxel(x + q);

                    mask[n++] = (current != VoxelType.Air && next == VoxelType.Air)
                        ? current
                        : VoxelType.Air;
                }
            }

            x[axis]++;

            // Generate mesh from mask (greedy)
            n = 0;
            for (int j = 0; j < chunkSize; j++) {
                for (int i = 0; i < chunkSize; ) {
                    if (mask[n] != VoxelType.Air) {
                        VoxelType voxelType = mask[n];

                        // Compute width
                        int width;
                        for (width = 1; i + width < chunkSize && mask[n + width] == voxelType; width++) { }

                        // Compute height
                        int height;
                        bool done = false;
                        for (height = 1; j + height < chunkSize; height++) {
                            for (int k = 0; k < width; k++) {
                                if (mask[n + k + height * chunkSize] != voxelType) {
                                    done = true;
                                    break;
                                }
                            }
                            if (done) break;
                        }

                        // Add quad
                        x[u] = i;
                        x[v] = j;
                        AddQuad(x, u, v, width, height, voxelType, direction);

                        // Clear mask
                        for (int l = 0; l < height; l++) {
                            for (int k = 0; k < width; k++) {
                                mask[n + k + l * chunkSize] = VoxelType.Air;
                            }
                        }

                        i += width;
                        n += width;
                    } else {
                        i++;
                        n++;
                    }
                }
            }
        }

        mask.Dispose();
    }

    VoxelType GetVoxel(int3 pos) {
        if (pos.x < 0 || pos.x >= chunkSize ||
            pos.y < 0 || pos.y >= chunkSize ||
            pos.z < 0 || pos.z >= chunkSize) {
            return VoxelType.Air;
        }
        int index = pos.x + pos.y * chunkSize + pos.z * chunkSize * chunkSize;
        return voxels[index];
    }

    void AddQuad(int3 pos, int u, int v, int width, int height, VoxelType type, int direction) {
        int startVertex = vertices.Length;

        // Compute quad vertices
        float3 v0 = new float3(pos.x, pos.y, pos.z);
        float3 du = new float3(0);
        float3 dv = new float3(0);
        du[u] = width;
        dv[v] = height;

        vertices.Add(v0);
        vertices.Add(v0 + du);
        vertices.Add(v0 + du + dv);
        vertices.Add(v0 + dv);

        // Triangles
        if (direction > 0) {
            triangles.Add(startVertex);
            triangles.Add(startVertex + 2);
            triangles.Add(startVertex + 1);
            triangles.Add(startVertex);
            triangles.Add(startVertex + 3);
            triangles.Add(startVertex + 2);
        } else {
            triangles.Add(startVertex);
            triangles.Add(startVertex + 1);
            triangles.Add(startVertex + 2);
            triangles.Add(startVertex);
            triangles.Add(startVertex + 2);
            triangles.Add(startVertex + 3);
        }

        // Normals, UVs (simplified)
        float3 normal = new float3(0);
        normal[(u + 2) % 3] = direction;
        for (int i = 0; i < 4; i++) {
            normals.Add(normal);
            uvs.Add(new Vector2(0, 0)); // Atlas UVs à calculer
        }
    }
}
```

---

## 3. MESH GENERATION PIPELINE

```csharp
public class VoxelMeshGenerator {

    public Mesh GenerateMesh(NativeArray<VoxelType> voxels, int chunkSize) {
        var vertices = new NativeList<Vector3>(500, Allocator.TempJob);
        var triangles = new NativeList<int>(1500, Allocator.TempJob);
        var normals = new NativeList<Vector3>(500, Allocator.TempJob);
        var uvs = new NativeList<Vector2>(500, Allocator.TempJob);

        var job = new GreedyMeshingJob {
            voxels = voxels,
            chunkSize = chunkSize,
            vertices = vertices,
            triangles = triangles,
            normals = normals,
            uvs = uvs
        };

        job.Schedule().Complete();

        // Create Unity Mesh
        var mesh = new Mesh();
        mesh.SetVertices(vertices.AsArray().ToArray());
        mesh.SetTriangles(triangles.AsArray().ToArray(), 0);
        mesh.SetNormals(normals.AsArray().ToArray());
        mesh.SetUVs(0, uvs.AsArray().ToArray());

        vertices.Dispose();
        triangles.Dispose();
        normals.Dispose();
        uvs.Dispose();

        return mesh;
    }
}
```

---

## 4. ASYNC REMESHING (Non-blocking)

```csharp
public class AsyncMeshingScheduler : MonoBehaviour {

    struct MeshJobData {
        public JobHandle handle;
        public NativeList<Vector3> vertices;
        public NativeList<int> triangles;
        public Action<Mesh> callback;
    }

    List<MeshJobData> runningJobs = new List<MeshJobData>();

    public void ScheduleMeshing(NativeArray<VoxelType> voxels, Action<Mesh> callback) {
        var vertices = new NativeList<Vector3>(500, Allocator.Persistent);
        var triangles = new NativeList<int>(1500, Allocator.Persistent);
        var normals = new NativeList<Vector3>(500, Allocator.Persistent);
        var uvs = new NativeList<Vector2>(500, Allocator.Persistent);

        var job = new GreedyMeshingJob {
            voxels = voxels,
            chunkSize = 16,
            vertices = vertices,
            triangles = triangles,
            normals = normals,
            uvs = uvs
        };

        var handle = job.Schedule();

        runningJobs.Add(new MeshJobData {
            handle = handle,
            vertices = vertices,
            triangles = triangles,
            callback = callback
        });
    }

    void LateUpdate() {
        for (int i = runningJobs.Count - 1; i >= 0; i--) {
            if (runningJobs[i].handle.IsCompleted) {
                runningJobs[i].handle.Complete();

                var mesh = CreateMeshFromData(runningJobs[i]);
                runningJobs[i].callback?.Invoke(mesh);

                runningJobs[i].vertices.Dispose();
                runningJobs[i].triangles.Dispose();

                runningJobs.RemoveAt(i);
            }
        }
    }
}
```

---

## 5. TEXTURE ATLAS & UVs

```csharp
// Voxel material atlas (16x16 textures)
public class VoxelTextureAtlas {
    public const int ATLAS_SIZE = 16;
    public const float TILE_SIZE = 1f / ATLAS_SIZE;

    public Vector2[] GetUVs(VoxelType type) {
        int index = (int)type;
        int x = index % ATLAS_SIZE;
        int y = index / ATLAS_SIZE;

        float u = x * TILE_SIZE;
        float v = y * TILE_SIZE;

        return new[] {
            new Vector2(u, v),
            new Vector2(u + TILE_SIZE, v),
            new Vector2(u + TILE_SIZE, v + TILE_SIZE),
            new Vector2(u, v + TILE_SIZE)
        };
    }
}
```

---

## 6. LOD MESHING

```csharp
// Generate LOD meshes (subsampling)
public Mesh GenerateLODMesh(NativeArray<VoxelType> voxels, int lodLevel) {
    int skip = 1 << lodLevel; // 1, 2, 4, 8
    int newSize = 16 / skip;

    var subsampledVoxels = SubsampleVoxels(voxels, skip, newSize);
    var mesh = GenerateMesh(subsampledVoxels, newSize);

    subsampledVoxels.Dispose();
    return mesh;
}
```

---

## 7. PERFORMANCE TARGETS

```
MESHING BUDGET:
├─ Macro terrain chunk (16³): ~5ms (Burst)
├─ Micro object (32³): ~0.5ms (Burst)
├─ Async overhead: ~0.1ms
└─ Total: Amortized over frames (<2ms/frame)

OPTIMIZATIONS:
├─ Burst Compile: 10-20x speedup
├─ Job System: Parallel meshing
├─ Greedy algorithm: 24x vertex reduction
└─ Mesh pooling: Zero allocation
```

---

**Document Version:** 1.0
**Dependencies:** 01, 02, 03
**Next:** 05_ECS_ARCHITECTURE.md
