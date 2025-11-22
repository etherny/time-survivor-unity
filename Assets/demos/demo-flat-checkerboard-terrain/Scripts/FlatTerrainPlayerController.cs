using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeSurvivor.Demos.FlatCheckerboardTerrain
{
    /// <summary>
    /// Simple player controller for flat terrain without gravity.
    /// Uses Input System (Keyboard.current) for WASD movement with sprint support.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FlatTerrainPlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] public float moveSpeed = 10f;
        [SerializeField] public float sprintMultiplier = 2f;

        private CharacterController characterController;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (characterController == null)
            {
                Debug.LogError("[FlatTerrainPlayerController] CharacterController component is missing!");
            }
        }

        private void Update()
        {
            if (characterController == null) return;

            // Input System null check
            if (Keyboard.current == null)
            {
                return;
            }

            HandleMovement();
        }

        /// <summary>
        /// Handles planar movement (no gravity) using WASD keys.
        /// Shift key enables sprint for 2x speed.
        /// </summary>
        private void HandleMovement()
        {
            // Read input from keyboard
            float horizontal = 0f;
            float vertical = 0f;

            if (Keyboard.current.wKey.isPressed) vertical += 1f;
            if (Keyboard.current.sKey.isPressed) vertical -= 1f;
            if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
            if (Keyboard.current.dKey.isPressed) horizontal += 1f;

            // Calculate movement vector (planar only, no Y component)
            Vector3 movement = new Vector3(horizontal, 0, vertical);

            // Normalize to prevent faster diagonal movement
            if (movement.magnitude > 1f)
            {
                movement.Normalize();
            }

            // Apply sprint multiplier if Shift is held
            float currentSpeed = moveSpeed;
            if (Keyboard.current.shiftKey.isPressed)
            {
                currentSpeed *= sprintMultiplier;
            }

            // Apply movement using CharacterController
            characterController.Move(movement * currentSpeed * Time.deltaTime);
        }
    }
}
