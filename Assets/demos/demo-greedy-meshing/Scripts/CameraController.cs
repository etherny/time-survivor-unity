using UnityEngine;

namespace TimeSurvivor.Demos.GreedyMeshing
{
    /// <summary>
    /// Orbit camera controller for the demo scene.
    /// Allows orbiting around a target and zooming in/out.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        public Transform Target;
        public float Distance = 40f;
        public float OrbitSpeed = 100f;
        public float ZoomSpeed = 10f;
        public float MinDistance = 10f;
        public float MaxDistance = 100f;

        private float _currentX = 0f;
        private float _currentY = 0f;
        private bool _isOrbiting = false;

        private void Start()
        {
            if (Target == null)
            {
                Debug.LogError("CameraController: Target is not assigned!");
                return;
            }

            // Initialize camera position
            UpdateCameraPosition();
        }

        private void Update()
        {
            if (Target == null) return;

            // Handle orbit input (right mouse button)
            if (Input.GetMouseButtonDown(1))
            {
                _isOrbiting = true;
            }

            if (Input.GetMouseButtonUp(1))
            {
                _isOrbiting = false;
            }

            if (_isOrbiting)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                _currentX += mouseX * OrbitSpeed * Time.deltaTime;
                _currentY -= mouseY * OrbitSpeed * Time.deltaTime;

                // Clamp vertical rotation to avoid flipping
                _currentY = Mathf.Clamp(_currentY, -89f, 89f);
            }

            // Handle zoom input (mouse wheel)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                Distance -= scroll * ZoomSpeed;
                Distance = Mathf.Clamp(Distance, MinDistance, MaxDistance);
            }

            // Update camera position
            UpdateCameraPosition();
        }

        private void UpdateCameraPosition()
        {
            if (Target == null) return;

            // Calculate position based on orbital angles and distance
            Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0f);
            Vector3 direction = rotation * Vector3.back;
            Vector3 position = Target.position + direction * Distance;

            // Apply position and look at target
            transform.position = position;
            transform.LookAt(Target);
        }
    }
}
