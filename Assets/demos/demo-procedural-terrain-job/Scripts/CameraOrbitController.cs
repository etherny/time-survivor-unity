using UnityEngine;

namespace TimeSurvivor.Demos.ProceduralTerrain
{
    /// <summary>
    /// Simple orbit camera controller for voxel terrain inspection.
    /// - Left-click + drag to orbit around target
    /// - Mouse wheel to zoom in/out
    /// - Clamped distance for better UX
    /// </summary>
    public class CameraOrbitController : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The point to orbit around (typically center of terrain)")]
        [SerializeField] private Transform target;

        [Header("Orbit Settings")]
        [Tooltip("Initial distance from target")]
        [SerializeField] private float distance = 80f;

        [Tooltip("Rotation speed (degrees per pixel)")]
        [SerializeField] private float orbitSpeed = 0.2f;

        [Tooltip("Zoom speed (units per scroll tick)")]
        [SerializeField] private float zoomSpeed = 10f;

        [Tooltip("Minimum distance from target")]
        [SerializeField] private float minDistance = 20f;

        [Tooltip("Maximum distance from target")]
        [SerializeField] private float maxDistance = 200f;

        [Header("Auto-Rotation (Optional)")]
        [Tooltip("Enable automatic rotation when not interacting")]
        [SerializeField] private bool autoRotate = false;

        [Tooltip("Auto-rotation speed (degrees per second)")]
        [SerializeField] private float autoRotateSpeed = 10f;

        // Private state
        private float currentYaw = 45f;
        private float currentPitch = 30f;
        private Vector3 lastMousePosition;
        private bool isDragging;

        // ========== Unity Lifecycle ==========

        private void Start()
        {
            // Validate target
            if (target == null)
            {
                Debug.LogWarning("[CameraOrbitController] No target assigned. Creating default target at origin.");
                GameObject targetObj = new GameObject("CameraTarget");
                target = targetObj.transform;
                target.position = Vector3.zero;
            }

            // Initialize camera position
            UpdateCameraPosition();
        }

        private void LateUpdate()
        {
            HandleInput();
            UpdateCameraPosition();
        }

        // ========== Input Handling ==========

        /// <summary>
        /// Handles mouse input for orbiting and zooming.
        /// </summary>
        private void HandleInput()
        {
            // Zoom with mouse wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                distance -= scroll * zoomSpeed;
                distance = Mathf.Clamp(distance, minDistance, maxDistance);
            }

            // Orbit with left mouse button
            if (Input.GetMouseButtonDown(0))
            {
                isDragging = true;
                lastMousePosition = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0))
            {
                isDragging = false;
            }

            if (isDragging)
            {
                Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
                lastMousePosition = Input.mousePosition;

                currentYaw += mouseDelta.x * orbitSpeed;
                currentPitch -= mouseDelta.y * orbitSpeed;

                // Clamp pitch to avoid gimbal lock
                currentPitch = Mathf.Clamp(currentPitch, -89f, 89f);
            }
            else if (autoRotate)
            {
                // Auto-rotate when not dragging
                currentYaw += autoRotateSpeed * Time.deltaTime;
            }
        }

        // ========== Camera Positioning ==========

        /// <summary>
        /// Updates camera position and rotation based on current orbit parameters.
        /// </summary>
        private void UpdateCameraPosition()
        {
            if (target == null) return;

            // Calculate camera position using spherical coordinates
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 direction = rotation * Vector3.back; // Back = -Z = towards camera
            Vector3 position = target.position + direction * distance;

            // Apply to camera
            transform.position = position;
            transform.LookAt(target.position);
        }

        // ========== Public API ==========

        /// <summary>
        /// Sets the orbit target programmatically.
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            UpdateCameraPosition();
        }

        /// <summary>
        /// Resets the camera to default position.
        /// </summary>
        public void ResetCamera()
        {
            currentYaw = 45f;
            currentPitch = 30f;
            distance = 80f;
            UpdateCameraPosition();
        }

        /// <summary>
        /// Focuses the camera on a specific point with optional distance override.
        /// </summary>
        public void FocusOn(Vector3 point, float? customDistance = null)
        {
            if (target == null)
            {
                GameObject targetObj = new GameObject("CameraTarget");
                target = targetObj.transform;
            }

            target.position = point;

            if (customDistance.HasValue)
            {
                distance = Mathf.Clamp(customDistance.Value, minDistance, maxDistance);
            }

            UpdateCameraPosition();
        }

        // ========== Gizmos ==========

        private void OnDrawGizmosSelected()
        {
            if (target != null)
            {
                // Draw orbit sphere
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(target.position, distance);

                // Draw line to target
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }
}
