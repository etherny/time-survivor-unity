using UnityEngine;

namespace TimeSurvivor.Demos.TerrainCollision
{
    /// <summary>
    /// Simple first-person character controller using Unity's CharacterController.
    /// Supports WASD movement, mouse look, jumping, and ground detection.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SimpleCharacterController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpVelocity = 5f;
        [SerializeField] private float gravity = -9.81f;

        [Header("Mouse Look Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float verticalLookLimit = 80f;

        [Header("Ground Detection")]
        [SerializeField] private float groundCheckDistance = 0.3f;
        [SerializeField] private LayerMask groundLayer;

        [Header("References")]
        [SerializeField] private Camera playerCamera;

        // Component references
        private CharacterController characterController;

        // State
        private Vector3 velocity;
        private float verticalRotation;
        private bool isGrounded;

        /// <summary>
        /// Whether the player is currently on the ground.
        /// </summary>
        public bool IsGrounded => isGrounded;

        /// <summary>
        /// Current vertical velocity (useful for debugging).
        /// </summary>
        public float VerticalVelocity => velocity.y;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            // Find camera if not assigned
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            // Lock cursor for FPS controls
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            // Allow ESC to unlock cursor
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            // Re-lock cursor on mouse click
            if (Input.GetMouseButtonDown(0) && Cursor.lockState == CursorLockMode.None)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            HandleMouseLook();
            HandleMovement();
            HandleGroundDetection();
        }

        /// <summary>
        /// Handles first-person mouse look (rotation).
        /// </summary>
        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            // Horizontal rotation (Y-axis)
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            transform.Rotate(Vector3.up * mouseX);

            // Vertical rotation (X-axis) - clamped
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -verticalLookLimit, verticalLookLimit);

            if (playerCamera != null)
            {
                playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            }
        }

        /// <summary>
        /// Handles WASD movement and jumping.
        /// </summary>
        private void HandleMovement()
        {
            // Get input
            float horizontal = Input.GetAxis("Horizontal"); // A/D
            float vertical = Input.GetAxis("Vertical");     // W/S

            // Calculate movement direction (relative to player rotation)
            Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
            moveDirection.y = 0f; // Keep movement horizontal
            moveDirection = moveDirection.normalized * moveSpeed;

            // Handle jumping
            if (isGrounded && Input.GetButtonDown("Jump"))
            {
                velocity.y = jumpVelocity;
            }

            // Apply gravity
            if (!isGrounded)
            {
                velocity.y += gravity * Time.deltaTime;
            }
            else if (velocity.y < 0)
            {
                // Keep slight downward force when grounded
                velocity.y = -2f;
            }

            // Combine horizontal movement and vertical velocity
            Vector3 finalMovement = moveDirection + new Vector3(0, velocity.y, 0);

            // Move character
            characterController.Move(finalMovement * Time.deltaTime);
        }

        /// <summary>
        /// Detects if the character is on the ground using a raycast.
        /// </summary>
        private void HandleGroundDetection()
        {
            // Cast ray from bottom of character controller
            Vector3 rayStart = transform.position;
            float rayDistance = groundCheckDistance + (characterController.height / 2f);

            isGrounded = Physics.Raycast(rayStart, Vector3.down, rayDistance, groundLayer);

            // Debug visualization
            Debug.DrawRay(rayStart, Vector3.down * rayDistance, isGrounded ? Color.green : Color.red);
        }

        private void OnDrawGizmos()
        {
            // Visualize ground check sphere
            if (characterController != null)
            {
                Vector3 groundCheckPos = transform.position - Vector3.up * (characterController.height / 2f);
                Gizmos.color = isGrounded ? Color.green : Color.red;
                Gizmos.DrawWireSphere(groundCheckPos, groundCheckDistance);
            }
        }
    }
}
