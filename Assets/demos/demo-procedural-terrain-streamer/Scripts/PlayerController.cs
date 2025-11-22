using UnityEngine;

namespace TimeSurvivor.Demos.ProceduralTerrainStreamer
{
    /// <summary>
    /// Simple player controller for the ProceduralTerrainStreamer demo.
    /// Provides WASD movement with sprint functionality.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float sprintMultiplier = 2f;

        [Header("Gravity")]
        [SerializeField] private float gravity = 9.81f;

        private CharacterController characterController;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController == null)
            {
                characterController = gameObject.AddComponent<CharacterController>();
                Debug.LogWarning("[PlayerController] CharacterController component was missing, added automatically.");
            }
        }

        void Update()
        {
            HandleMovement();
        }

        private void HandleMovement()
        {
            // Get input
            float horizontal = Input.GetAxis("Horizontal"); // A/D
            float vertical = Input.GetAxis("Vertical");     // W/S
            bool sprinting = Input.GetKey(KeyCode.LeftShift);

            // Calculate movement vector
            Vector3 movement = new Vector3(horizontal, 0, vertical);

            // Apply speed multiplier
            float currentSpeed = sprinting ? moveSpeed * sprintMultiplier : moveSpeed;

            // Apply gravity
            movement.y = -gravity;

            // Move the character
            characterController.Move(movement * currentSpeed * Time.deltaTime);
        }
    }
}
