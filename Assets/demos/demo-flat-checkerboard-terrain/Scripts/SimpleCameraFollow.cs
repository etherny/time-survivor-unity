using UnityEngine;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain
{
    /// <summary>
    /// Simple camera follow script with isometric offset.
    /// Follows a target (player) with optional smooth interpolation.
    /// </summary>
    public class SimpleCameraFollow : MonoBehaviour
    {
        [Header("Follow Settings")]
        [SerializeField] public Transform target;

        [Header("Offset Settings")]
        [SerializeField] public Vector3 offset = new Vector3(0, 12, -12); // Closer to terrain for better pattern visibility

        [Header("Smooth Follow")]
        [SerializeField] public bool smoothFollow = true;
        [SerializeField] public float smoothSpeed = 5f;

        private void Awake()
        {
            if (target == null)
            {
                Debug.LogWarning("[SimpleCameraFollow] No target assigned. Camera will not follow anything.");
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            UpdateCameraPosition();
            UpdateCameraRotation();
        }

        /// <summary>
        /// Updates camera position to follow the target with offset.
        /// Uses smooth interpolation if enabled.
        /// </summary>
        private void UpdateCameraPosition()
        {
            Vector3 desiredPosition = target.position + offset;

            if (smoothFollow)
            {
                transform.position = Vector3.Lerp(
                    transform.position,
                    desiredPosition,
                    smoothSpeed * Time.deltaTime
                );
            }
            else
            {
                transform.position = desiredPosition;
            }
        }

        /// <summary>
        /// Updates camera rotation to look at the target.
        /// </summary>
        private void UpdateCameraRotation()
        {
            transform.LookAt(target);
        }
    }
}
