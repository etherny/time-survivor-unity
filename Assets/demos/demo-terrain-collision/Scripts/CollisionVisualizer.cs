using UnityEngine;

namespace TimeSurvivor.Demos.TerrainCollision
{
    /// <summary>
    /// Visualizes collision meshes and ground detection for debugging.
    /// Toggle with 'V' key to show/hide collision geometry as green wireframes.
    /// </summary>
    public class CollisionVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool showColliders = true;
        [SerializeField] private Color colliderColor = Color.green;
        [SerializeField] private bool showGroundRaycast = true;
        [SerializeField] private Color raycastColor = Color.red;

        [Header("References")]
        [SerializeField] private SimpleCharacterController playerController;

        /// <summary>
        /// Whether collision visualization is currently enabled.
        /// </summary>
        public bool IsVisualizationEnabled => showColliders;

        private void Awake()
        {
            // Find player controller if not assigned
            if (playerController == null)
            {
                playerController = FindObjectOfType<SimpleCharacterController>();
                if (playerController == null)
                {
                    Debug.LogWarning("[CollisionVisualizer] Could not find SimpleCharacterController. Ground raycast visualization will be disabled.");
                }
            }
        }

        private void Update()
        {
            // Toggle visualization with 'V' key
            if (Input.GetKeyDown(KeyCode.V))
            {
                showColliders = !showColliders;
                Debug.Log($"[CollisionVisualizer] Collision visualization: {(showColliders ? "ON" : "OFF")}");
            }
        }

        private void OnDrawGizmos()
        {
            if (!showColliders) return;

            // Draw all MeshColliders in the scene
            DrawMeshColliders();

            // Draw ground raycast
            if (showGroundRaycast && playerController != null)
            {
                DrawGroundRaycast();
            }
        }

        /// <summary>
        /// Draws all MeshColliders in the scene as green wireframes.
        /// </summary>
        private void DrawMeshColliders()
        {
            MeshCollider[] colliders = FindObjectsOfType<MeshCollider>();

            Gizmos.color = colliderColor;

            foreach (MeshCollider meshCollider in colliders)
            {
                if (meshCollider.sharedMesh == null) continue;

                // Get the mesh and transform
                Mesh mesh = meshCollider.sharedMesh;
                Transform colliderTransform = meshCollider.transform;

                // Draw mesh as wireframe
                Vector3[] vertices = mesh.vertices;
                int[] triangles = mesh.triangles;

                for (int i = 0; i < triangles.Length; i += 3)
                {
                    // Get triangle vertices in world space
                    Vector3 v0 = colliderTransform.TransformPoint(vertices[triangles[i]]);
                    Vector3 v1 = colliderTransform.TransformPoint(vertices[triangles[i + 1]]);
                    Vector3 v2 = colliderTransform.TransformPoint(vertices[triangles[i + 2]]);

                    // Draw triangle edges
                    Gizmos.DrawLine(v0, v1);
                    Gizmos.DrawLine(v1, v2);
                    Gizmos.DrawLine(v2, v0);
                }
            }
        }

        /// <summary>
        /// Draws the player's ground detection raycast.
        /// </summary>
        private void DrawGroundRaycast()
        {
            if (playerController == null) return;

            Transform playerTransform = playerController.transform;
            CharacterController charController = playerController.GetComponent<CharacterController>();

            if (charController == null) return;

            // Calculate raycast parameters (same as in SimpleCharacterController)
            Vector3 rayStart = playerTransform.position;
            float rayDistance = 0.3f + (charController.height / 2f);

            // Draw raycast
            Gizmos.color = playerController.IsGrounded ? Color.green : raycastColor;
            Gizmos.DrawLine(rayStart, rayStart + Vector3.down * rayDistance);

            // Draw endpoint sphere
            Gizmos.DrawWireSphere(rayStart + Vector3.down * rayDistance, 0.1f);
        }
    }
}
