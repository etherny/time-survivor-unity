using System.Collections.Generic;
using UnityEngine;

namespace TimeSurvivor.Demos.TerrainCollision
{
    /// <summary>
    /// Spawns physics objects (spheres and cubes) for collision testing.
    /// Spawns in front of player with 'O' key. Maintains a maximum count using object pooling.
    /// </summary>
    public class PhysicsObjectSpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private GameObject spherePrefab;
        [SerializeField] private GameObject cubePrefab;
        [SerializeField] private int maxObjects = 20;
        [SerializeField] private float spawnDistanceAhead = 5f;
        [SerializeField] private float spawnHeightOffset = 3f;

        [Header("References")]
        [SerializeField] private Transform playerTransform;

        // Object tracking
        private readonly Queue<GameObject> spawnedObjects = new Queue<GameObject>();
        private int spawnCount = 0;

        /// <summary>
        /// Total number of objects spawned (for UI display).
        /// </summary>
        public int TotalSpawned => spawnCount;

        /// <summary>
        /// Current active object count.
        /// </summary>
        public int ActiveCount => spawnedObjects.Count;

        private void Awake()
        {
            // Validate references
            if (spherePrefab == null)
            {
                Debug.LogError("[PhysicsObjectSpawner] Sphere prefab is not assigned!");
            }

            if (cubePrefab == null)
            {
                Debug.LogError("[PhysicsObjectSpawner] Cube prefab is not assigned!");
            }

            if (playerTransform == null)
            {
                Debug.LogWarning("[PhysicsObjectSpawner] Player transform not assigned. Attempting to find...");
                var playerController = FindObjectOfType<SimpleCharacterController>();
                if (playerController != null)
                {
                    playerTransform = playerController.transform;
                }
                else
                {
                    Debug.LogError("[PhysicsObjectSpawner] Could not find player transform!");
                }
            }
        }

        private void Update()
        {
            // Validate input manager
            if (DemoInputManager.Instance == null)
            {
                return;
            }

            // Spawn on 'O' key press
            if (DemoInputManager.Instance.SpawnObjectPressed)
            {
                SpawnRandomObject();
            }
        }

        /// <summary>
        /// Spawns a random physics object (sphere or cube) in front of the player.
        /// </summary>
        private void SpawnRandomObject()
        {
            if (playerTransform == null)
            {
                Debug.LogError("[PhysicsObjectSpawner] Cannot spawn: player transform is null!");
                return;
            }

            // Choose random prefab
            GameObject prefabToSpawn = Random.value > 0.5f ? spherePrefab : cubePrefab;

            if (prefabToSpawn == null)
            {
                Debug.LogError("[PhysicsObjectSpawner] Cannot spawn: prefab is null!");
                return;
            }

            // Calculate spawn position (in front of player, elevated)
            Vector3 spawnPosition = playerTransform.position +
                                   playerTransform.forward * spawnDistanceAhead +
                                   Vector3.up * spawnHeightOffset;

            // Instantiate object
            GameObject spawnedObject = Instantiate(prefabToSpawn, spawnPosition, Random.rotation);
            spawnedObject.name = $"{prefabToSpawn.name}_{spawnCount}";

            // Ensure Rigidbody exists
            Rigidbody rb = spawnedObject.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = spawnedObject.AddComponent<Rigidbody>();
            }

            // Add to queue
            spawnedObjects.Enqueue(spawnedObject);
            spawnCount++;

            // Enforce max count (destroy oldest)
            if (spawnedObjects.Count > maxObjects)
            {
                GameObject oldestObject = spawnedObjects.Dequeue();
                if (oldestObject != null)
                {
                    Destroy(oldestObject);
                }
            }

            Debug.Log($"[PhysicsObjectSpawner] Spawned {spawnedObject.name} at {spawnPosition}. Active: {spawnedObjects.Count}/{maxObjects}");
        }

        /// <summary>
        /// Clears all spawned objects (useful for reset).
        /// </summary>
        public void ClearAllObjects()
        {
            while (spawnedObjects.Count > 0)
            {
                GameObject obj = spawnedObjects.Dequeue();
                if (obj != null)
                {
                    Destroy(obj);
                }
            }

            Debug.Log("[PhysicsObjectSpawner] Cleared all spawned objects.");
        }

        private void OnDestroy()
        {
            // Clean up on destroy
            ClearAllObjects();
        }
    }
}
