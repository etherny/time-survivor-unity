using UnityEngine;
using UnityEngine.InputSystem;

namespace TimeSurvivor.Demos.TerrainCollision
{
    /// <summary>
    /// Manages input for the Terrain Collision demo using Unity's new Input System.
    /// Provides a centralized singleton for accessing player input actions.
    /// </summary>
    public class DemoInputManager : MonoBehaviour
    {
        private static DemoInputManager instance;

        /// <summary>
        /// Singleton instance of the DemoInputManager.
        /// </summary>
        public static DemoInputManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindObjectOfType<DemoInputManager>();
                    if (instance == null)
                    {
                        Debug.LogError("[DemoInputManager] No DemoInputManager found in scene! Please add one.");
                    }
                }
                return instance;
            }
        }

        [Header("Input Actions Asset")]
        [SerializeField] private InputActionAsset inputActions;

        // Input Action Maps
        private InputActionMap playerActionMap;

        // Input Actions
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction jumpAction;
        private InputAction spawnObjectAction;
        private InputAction toggleVisualizationAction;
        private InputAction toggleUIAction;
        private InputAction unlockCursorAction;

        /// <summary>
        /// Movement input (WASD). Returns Vector2 with x = horizontal, y = vertical.
        /// </summary>
        public Vector2 MoveInput => moveAction?.ReadValue<Vector2>() ?? Vector2.zero;

        /// <summary>
        /// Mouse look delta input. Returns Vector2 with x = horizontal delta, y = vertical delta.
        /// </summary>
        public Vector2 LookInput => lookAction?.ReadValue<Vector2>() ?? Vector2.zero;

        /// <summary>
        /// Jump button was pressed this frame.
        /// </summary>
        public bool JumpPressed => jumpAction?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// Spawn object button was pressed this frame.
        /// </summary>
        public bool SpawnObjectPressed => spawnObjectAction?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// Toggle visualization button was pressed this frame.
        /// </summary>
        public bool ToggleVisualizationPressed => toggleVisualizationAction?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// Toggle UI button was pressed this frame.
        /// </summary>
        public bool ToggleUIPressed => toggleUIAction?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// Unlock cursor button was pressed this frame.
        /// </summary>
        public bool UnlockCursorPressed => unlockCursorAction?.WasPressedThisFrame() ?? false;

        /// <summary>
        /// Mouse button 0 was pressed this frame (for cursor re-locking).
        /// </summary>
        public bool MouseLeftPressed => Mouse.current?.leftButton.wasPressedThisFrame ?? false;

        private void Awake()
        {
            // Enforce singleton pattern
            if (instance != null && instance != this)
            {
                Debug.LogWarning("[DemoInputManager] Multiple DemoInputManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            instance = this;

            // Validate input actions asset
            if (inputActions == null)
            {
                Debug.LogError("[DemoInputManager] Input Actions asset is not assigned! Input will not work.");
                return;
            }

            // Get action map
            playerActionMap = inputActions.FindActionMap("Player");
            if (playerActionMap == null)
            {
                Debug.LogError("[DemoInputManager] 'Player' action map not found in Input Actions asset!");
                return;
            }

            // Cache individual actions
            moveAction = playerActionMap.FindAction("Move");
            lookAction = playerActionMap.FindAction("Look");
            jumpAction = playerActionMap.FindAction("Jump");
            spawnObjectAction = playerActionMap.FindAction("SpawnObject");
            toggleVisualizationAction = playerActionMap.FindAction("ToggleVisualization");
            toggleUIAction = playerActionMap.FindAction("ToggleUI");
            unlockCursorAction = playerActionMap.FindAction("UnlockCursor");

            // Validate all actions found
            ValidateAction(moveAction, "Move");
            ValidateAction(lookAction, "Look");
            ValidateAction(jumpAction, "Jump");
            ValidateAction(spawnObjectAction, "SpawnObject");
            ValidateAction(toggleVisualizationAction, "ToggleVisualization");
            ValidateAction(toggleUIAction, "ToggleUI");
            ValidateAction(unlockCursorAction, "UnlockCursor");

            Debug.Log("[DemoInputManager] Initialized successfully");
        }

        private void OnEnable()
        {
            // Enable all actions
            playerActionMap?.Enable();
        }

        private void OnDisable()
        {
            // Disable all actions
            playerActionMap?.Disable();
        }

        private void OnDestroy()
        {
            // Clean up singleton reference
            if (instance == this)
            {
                instance = null;
            }
        }

        /// <summary>
        /// Validates that an action was found and logs an error if not.
        /// </summary>
        /// <param name="action">The action to validate</param>
        /// <param name="actionName">The name of the action for logging</param>
        private void ValidateAction(InputAction action, string actionName)
        {
            if (action == null)
            {
                Debug.LogError($"[DemoInputManager] Input action '{actionName}' not found in Player action map!");
            }
        }
    }
}
