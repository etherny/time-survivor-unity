using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using TimeSurvivor.Voxel.Core;
using TimeSurvivor.Voxel.Terrain;
using TimeSurvivor.Voxel.Rendering;

namespace TimeSurvivor.Demos.ProceduralTerrain
{
    /// <summary>
    /// Orchestrates the procedural terrain generation demo.
    /// Manages job scheduling, mesh generation, UI updates, and performance metrics.
    ///
    /// Workflow:
    /// 1. User adjusts sliders (Seed, Frequency, Amplitude, OffsetY)
    /// 2. Click "Generate" or "Randomize" button
    /// 3. Schedule ProceduralTerrainGenerationJob
    /// 4. Wait for completion
    /// 5. Schedule GreedyMeshingJob to convert voxels to mesh
    /// 6. Apply mesh to GameObject with vertex colors
    /// 7. Update UI stats (generation time, voxel count, distribution, FPS)
    /// </summary>
    public class DemoController : MonoBehaviour
    {
        // ========== Inspector References ==========

        [Header("Scene References")]
        [Tooltip("Parent transform for the generated terrain mesh")]
        [SerializeField] private Transform terrainContainer;

        [Tooltip("Material with vertex color support (URP/Lit or custom shader)")]
        [SerializeField] private Material voxelMaterial;

        [Header("UI Controls")]
        [SerializeField] private Slider seedSlider;
        [SerializeField] private Slider frequencySlider;
        [SerializeField] private Slider amplitudeSlider;
        [SerializeField] private Slider offsetYSlider;
        [SerializeField] private Button generateButton;
        [SerializeField] private Button randomizeButton;

        [Header("UI Stats Display")]
        [SerializeField] private TMP_Text generationTimeText;
        [SerializeField] private TMP_Text voxelCountText;
        [SerializeField] private TMP_Text distributionText;
        [SerializeField] private TMP_Text fpsText;

        [Header("Generation Settings")]
        [Tooltip("Chunk dimension in voxels (64 = 64x64x64 chunk)")]
        [Range(16, 128)]
        [SerializeField] private int chunkSize = 64;

        [Tooltip("Size of a single voxel in Unity world units")]
        [SerializeField] private float voxelSize = 0.2f;

        [Tooltip("Number of noise octaves (more = more detail, slower)")]
        [Range(1, 6)]
        [SerializeField] private int noiseOctaves = 4;

        [Tooltip("Frequency multiplier per octave")]
        [SerializeField] private float lacunarity = 2.0f;

        [Tooltip("Amplitude multiplier per octave")]
        [SerializeField] private float persistence = 0.5f;

        // ========== Private State ==========

        private GameObject currentTerrainObject;
        private float lastGenerationTime;
        private int lastVoxelCount;
        private float fpsUpdateTimer;
        private float[] fpsBuffer = new float[60];
        private int fpsBufferIndex;

        // ========== Unity Lifecycle ==========

        private void Start()
        {
            // Validate references
            ValidateReferences();

            // Setup UI listeners
            generateButton.onClick.AddListener(OnGenerateClicked);
            randomizeButton.onClick.AddListener(OnRandomizeClicked);

            // Setup slider value change listeners for live feedback
            seedSlider.onValueChanged.AddListener(_ => UpdateSliderLabels());
            frequencySlider.onValueChanged.AddListener(_ => UpdateSliderLabels());
            amplitudeSlider.onValueChanged.AddListener(_ => UpdateSliderLabels());
            offsetYSlider.onValueChanged.AddListener(_ => UpdateSliderLabels());

            // Initial generation
            UpdateSliderLabels();
            GenerateChunk();
        }

        private void Update()
        {
            // Update FPS counter
            UpdateFPS();
        }

        private void OnDestroy()
        {
            // Cleanup listeners
            generateButton.onClick.RemoveListener(OnGenerateClicked);
            randomizeButton.onClick.RemoveListener(OnRandomizeClicked);
        }

        // ========== UI Event Handlers ==========

        private void OnGenerateClicked()
        {
            GenerateChunk();
        }

        private void OnRandomizeClicked()
        {
            // Generate random seed
            int randomSeed = UnityEngine.Random.Range(0, 1000000);
            seedSlider.value = randomSeed;
            UpdateSliderLabels();
            GenerateChunk();
        }

        // ========== Core Generation Logic ==========

        /// <summary>
        /// Main generation workflow:
        /// 1. Read UI parameters
        /// 2. Schedule ProceduralTerrainGenerationJob
        /// 3. Schedule GreedyMeshingJob
        /// 4. Create mesh and apply to GameObject
        /// 5. Update UI stats
        /// </summary>
        private void GenerateChunk()
        {
            // Clear previous terrain
            ClearPreviousChunk();

            // Read UI parameters
            int seed = Mathf.RoundToInt(seedSlider.value);
            float frequency = frequencySlider.value;
            float amplitude = amplitudeSlider.value;
            float offsetY = offsetYSlider.value;

            // Start timing
            float startTime = Time.realtimeSinceStartup;

            // Allocate voxel data
            int voxelCount = chunkSize * chunkSize * chunkSize;
            NativeArray<VoxelType> voxelData = new NativeArray<VoxelType>(voxelCount, Allocator.TempJob);

            try
            {
                // === STEP 1: Generate voxel data ===
                var terrainJob = new ProceduralTerrainGenerationJob
                {
                    ChunkCoord = new ChunkCoord(0, 0, 0), // Origin chunk
                    ChunkSize = chunkSize,
                    VoxelSize = voxelSize,
                    Seed = seed,
                    NoiseFrequency = frequency,
                    NoiseOctaves = noiseOctaves,
                    Lacunarity = lacunarity,
                    Persistence = persistence,
                    TerrainOffsetY = offsetY, // Use offsetY as terrain base altitude
                    VoxelData = voxelData
                };

                JobHandle terrainHandle = terrainJob.Schedule(voxelCount, 64);
                terrainHandle.Complete();

                // === STEP 2: Generate mesh from voxels ===
                // Allocate mesh data containers
                NativeList<float3> vertices = new NativeList<float3>(Allocator.TempJob);
                NativeList<int> triangles = new NativeList<int>(Allocator.TempJob);
                NativeList<float2> uvs = new NativeList<float2>(Allocator.TempJob);
                NativeList<float3> normals = new NativeList<float3>(Allocator.TempJob);
                NativeList<float4> colors = new NativeList<float4>(Allocator.TempJob);

                // Allocate temporary buffers for greedy algorithm
                int maskSize = chunkSize * chunkSize;
                NativeArray<bool> mask = new NativeArray<bool>(maskSize, Allocator.TempJob);
                NativeArray<VoxelType> maskVoxelTypes = new NativeArray<VoxelType>(maskSize, Allocator.TempJob);

                try
                {
                    var meshingJob = new GreedyMeshingJob
                    {
                        Voxels = voxelData,
                        ChunkSize = chunkSize,
                        Vertices = vertices,
                        Triangles = triangles,
                        UVs = uvs,
                        Normals = normals,
                        Colors = colors,
                        Mask = mask,
                        MaskVoxelTypes = maskVoxelTypes
                    };

                    JobHandle meshingHandle = meshingJob.Schedule();
                    meshingHandle.Complete();

                    // Measure total generation time
                    float endTime = Time.realtimeSinceStartup;
                    lastGenerationTime = (endTime - startTime) * 1000f; // Convert to milliseconds

                    // === STEP 3: Create Unity mesh ===
                    Mesh mesh = CreateMeshFromJobData(vertices, triangles, uvs, normals, colors);

                    // === STEP 4: Create GameObject and apply mesh ===
                    CreateTerrainGameObject(mesh);

                    // === STEP 5: Analyze voxel data and update UI ===
                    lastVoxelCount = CountSolidVoxels(voxelData);
                    UpdateStatsUI(voxelData);
                }
                finally
                {
                    // Cleanup mesh data
                    vertices.Dispose();
                    triangles.Dispose();
                    uvs.Dispose();
                    normals.Dispose();
                    colors.Dispose();
                    mask.Dispose();
                    maskVoxelTypes.Dispose();
                }
            }
            finally
            {
                // Cleanup voxel data
                voxelData.Dispose();
            }
        }

        // ========== Mesh Creation ==========

        /// <summary>
        /// Converts NativeArrays from GreedyMeshingJob to a Unity Mesh.
        /// </summary>
        private Mesh CreateMeshFromJobData(
            NativeList<float3> vertices,
            NativeList<int> triangles,
            NativeList<float2> uvs,
            NativeList<float3> normals,
            NativeList<float4> colors)
        {
            Mesh mesh = new Mesh
            {
                name = "ProceduralTerrainMesh"
            };

            // Check if we need 32-bit indices (Unity default is 16-bit = max 65535 vertices)
            if (vertices.Length > 65535)
            {
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            // Convert NativeArray to managed arrays and assign to mesh
            Vector3[] verticesArray = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                verticesArray[i] = vertices[i];
            }
            mesh.vertices = verticesArray;

            // Convert triangles NativeList to managed array
            int[] trianglesArray = new int[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                trianglesArray[i] = triangles[i];
            }
            mesh.triangles = trianglesArray;

            Vector2[] uvsArray = new Vector2[uvs.Length];
            for (int i = 0; i < uvs.Length; i++)
            {
                uvsArray[i] = uvs[i];
            }
            mesh.uv = uvsArray;

            Vector3[] normalsArray = new Vector3[normals.Length];
            for (int i = 0; i < normals.Length; i++)
            {
                normalsArray[i] = normals[i];
            }
            mesh.normals = normalsArray;

            // IMPORTANT: Vertex colors for material
            Color[] colorsArray = new Color[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                float4 c = colors[i];
                colorsArray[i] = new Color(c.x, c.y, c.z, c.w);
            }
            mesh.colors = colorsArray;

            mesh.RecalculateBounds();

            return mesh;
        }

        /// <summary>
        /// Creates a GameObject with MeshFilter and MeshRenderer, applies the generated mesh.
        /// </summary>
        private void CreateTerrainGameObject(Mesh mesh)
        {
            currentTerrainObject = new GameObject("ProceduralTerrain");
            currentTerrainObject.transform.SetParent(terrainContainer);
            currentTerrainObject.transform.localPosition = Vector3.zero;

            MeshFilter meshFilter = currentTerrainObject.AddComponent<MeshFilter>();
            meshFilter.mesh = mesh;

            MeshRenderer meshRenderer = currentTerrainObject.AddComponent<MeshRenderer>();
            meshRenderer.material = voxelMaterial;
        }

        // ========== Stats and Analysis ==========

        /// <summary>
        /// Counts non-air voxels in the generated data.
        /// </summary>
        private int CountSolidVoxels(NativeArray<VoxelType> voxelData)
        {
            int count = 0;
            for (int i = 0; i < voxelData.Length; i++)
            {
                if (voxelData[i] != VoxelType.Air)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Analyzes voxel type distribution and updates UI stats.
        /// </summary>
        private void UpdateStatsUI(NativeArray<VoxelType> voxelData)
        {
            // Count each voxel type
            int airCount = 0;
            int grassCount = 0;
            int dirtCount = 0;
            int stoneCount = 0;
            int waterCount = 0;

            for (int i = 0; i < voxelData.Length; i++)
            {
                switch (voxelData[i])
                {
                    case VoxelType.Air: airCount++; break;
                    case VoxelType.Grass: grassCount++; break;
                    case VoxelType.Dirt: dirtCount++; break;
                    case VoxelType.Stone: stoneCount++; break;
                    case VoxelType.Water: waterCount++; break;
                }
            }

            int totalVoxels = voxelData.Length;

            // Update UI texts
            generationTimeText.text = $"Generation Time: {lastGenerationTime:F2} ms";
            voxelCountText.text = $"Solid Voxels: {lastVoxelCount:N0} / {totalVoxels:N0}";

            // Distribution percentages
            float airPercent = (airCount / (float)totalVoxels) * 100f;
            float grassPercent = (grassCount / (float)totalVoxels) * 100f;
            float dirtPercent = (dirtCount / (float)totalVoxels) * 100f;
            float stonePercent = (stoneCount / (float)totalVoxels) * 100f;
            float waterPercent = (waterCount / (float)totalVoxels) * 100f;

            distributionText.text = $"Distribution:\n" +
                                     $"Air: {airPercent:F1}%\n" +
                                     $"Grass: {grassPercent:F1}%\n" +
                                     $"Dirt: {dirtPercent:F1}%\n" +
                                     $"Stone: {stonePercent:F1}%\n" +
                                     $"Water: {waterPercent:F1}%";
        }

        /// <summary>
        /// Updates FPS counter with smoothing (60-frame rolling average).
        /// </summary>
        private void UpdateFPS()
        {
            fpsUpdateTimer += Time.deltaTime;

            // Update buffer
            fpsBuffer[fpsBufferIndex] = 1f / Time.unscaledDeltaTime;
            fpsBufferIndex = (fpsBufferIndex + 1) % fpsBuffer.Length;

            // Update text every 0.2 seconds
            if (fpsUpdateTimer >= 0.2f)
            {
                float averageFPS = 0f;
                for (int i = 0; i < fpsBuffer.Length; i++)
                {
                    averageFPS += fpsBuffer[i];
                }
                averageFPS /= fpsBuffer.Length;

                fpsText.text = $"FPS: {averageFPS:F0}";
                fpsUpdateTimer = 0f;
            }
        }

        // ========== Helper Methods ==========

        /// <summary>
        /// Destroys the previous terrain GameObject to avoid memory leaks.
        /// </summary>
        private void ClearPreviousChunk()
        {
            if (currentTerrainObject != null)
            {
                Destroy(currentTerrainObject);
                currentTerrainObject = null;
            }
        }

        /// <summary>
        /// Updates slider labels with current values (for better UX).
        /// </summary>
        private void UpdateSliderLabels()
        {
            // This method can be expanded to update labels next to sliders
            // For now, it's a placeholder for future UI enhancements
        }

        /// <summary>
        /// Validates all required references are assigned in the Inspector.
        /// Logs clear errors if any are missing.
        /// </summary>
        private void ValidateReferences()
        {
            if (terrainContainer == null)
                Debug.LogError("[DemoController] Terrain Container is not assigned!");

            if (voxelMaterial == null)
                Debug.LogError("[DemoController] Voxel Material is not assigned!");

            if (seedSlider == null)
                Debug.LogError("[DemoController] Seed Slider is not assigned!");

            if (frequencySlider == null)
                Debug.LogError("[DemoController] Frequency Slider is not assigned!");

            if (amplitudeSlider == null)
                Debug.LogError("[DemoController] Amplitude Slider is not assigned!");

            if (offsetYSlider == null)
                Debug.LogError("[DemoController] OffsetY Slider is not assigned!");

            if (generateButton == null)
                Debug.LogError("[DemoController] Generate Button is not assigned!");

            if (randomizeButton == null)
                Debug.LogError("[DemoController] Randomize Button is not assigned!");

            if (generationTimeText == null)
                Debug.LogError("[DemoController] Generation Time Text is not assigned!");

            if (voxelCountText == null)
                Debug.LogError("[DemoController] Voxel Count Text is not assigned!");

            if (distributionText == null)
                Debug.LogError("[DemoController] Distribution Text is not assigned!");

            if (fpsText == null)
                Debug.LogError("[DemoController] FPS Text is not assigned!");
        }
    }
}
