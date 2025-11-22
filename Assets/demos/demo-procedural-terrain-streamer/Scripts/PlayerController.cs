using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeSurvivor.Demos.ProceduralTerrainStreamer
{
    /// <summary>
    /// Simple player controller for the ProceduralTerrainStreamer demo.
    /// Provides WASD movement with sprint functionality.
    /// Uses the new Input System (UnityEngine.InputSystem).
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] public float moveSpeed = 10f;
        [SerializeField] public float sprintMultiplier = 2f;

        [Header("Gravity")]
        [SerializeField] public float gravity = 9.81f;

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
            // Get input using new Input System
            float horizontal = 0f;
            float vertical = 0f;
            bool sprinting = false;

            if (Keyboard.current != null)
            {
                // WASD movement
                if (Keyboard.current.aKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed) horizontal += 1f;
                if (Keyboard.current.wKey.isPressed) vertical += 1f;
                if (Keyboard.current.sKey.isPressed) vertical -= 1f;

                // Sprint with Left Shift
                sprinting = Keyboard.current.leftShiftKey.isPressed;
            }

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
