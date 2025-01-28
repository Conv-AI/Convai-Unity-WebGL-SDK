#if ENABLE_INPUT_SYSTEM
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Convai.Scripts.Runtime.Core
{
    /// <summary>
    ///     The Input Manager class for Convai, allowing you to control inputs in your project through this class.
    ///     It supports both the New Input System and Old Input System.
    /// </summary>
    [DefaultExecutionOrder(-105)]
    public class ConvaiInputManager : MonoBehaviour, Controls.IPlayerActions
    {
        [HideInInspector] public Vector2 moveVector;
        [HideInInspector] public Vector2 lookVector;
        [HideInInspector] public Vector2 positionVector;
        public bool isRunning { get; private set; }

        public Action jumping;
        public Action toggleChat;
        public Action sendText;
        public Action toggleSettings;

        public bool IsTalkKeyHeld { get; private set; }
        public Action<bool> talkKeyInteract;

#if ENABLE_INPUT_SYSTEM

        private Controls _controls;

#elif ENABLE_LEGACY_INPUT_MANAGER
    [Serializable]
    public class MovementKeys
    {
        public const KeyCode Forward = KeyCode.W;
        public const KeyCode Backward = KeyCode.S;
        public const KeyCode Right = KeyCode.D;
        public const KeyCode Left = KeyCode.A;
    }
    
    /// <summary>
    /// Key used to manage text send
    /// </summary>
    public KeyCode TextSendKey = KeyCode.Return;

    /// <summary>
    /// Key used to manage text send
    /// </summary>
    public KeyCode TextSendAltKey = KeyCode.KeypadEnter;

    /// <summary>
    /// Key used to manage record user audio
    /// </summary>
    public KeyCode TalkKey = KeyCode.T;

    /// <summary>
    /// Key used to manage setting panel toggle
    /// </summary>
    public KeyCode OpenSettingPanelKey = KeyCode.F10;

    /// <summary>
    /// Key used to manage running
    /// </summary>
    public KeyCode RunKey = KeyCode.LeftShift;

    /// <summary>
    /// Keys used to manage movement
    /// </summary>
    public FourDirectionalMovementKeys MovementKeys;
#endif


        /// <summary>
        ///     Singleton instance providing easy access to the ConvaiInputManager from other scripts.
        /// </summary>
        public static ConvaiInputManager Instance { get; private set; }

        /// <summary>
        ///     Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            // Ensure only one instance of ConvaiInputManager exists
            if (Instance != null)
            {
                Debug.LogError("There's more than one ConvaiInputManager! " + transform + " - " + Instance);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            LockCursor(true);
        }

        /// <summary>
        ///     Enable input actions when the object is enabled.
        /// </summary>
        private void OnEnable()
        {
#if ENABLE_INPUT_SYSTEM
            _controls = new Controls();
            _controls.Player.SetCallbacks(this);
            _controls.Enable();
#endif
        }

        private void OnDisable()
        {
            _controls.Disable();
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed) jumping?.Invoke();
        }

        public void OnMove(InputAction.CallbackContext context)
        {
            moveVector = context.ReadValue<Vector2>();
        }

        public void OnLook(InputAction.CallbackContext context)
        {
            lookVector = context.ReadValue<Vector2>();
        }

        public void OnMousePress(InputAction.CallbackContext context)
        {
        }

        public void OnRun(InputAction.CallbackContext context)
        {
            if (context.performed) isRunning = !isRunning;
        }

        public void OnSendText(InputAction.CallbackContext context)
        {
            if (context.performed) sendText?.Invoke();
        }
        public void OnToggleChat(InputAction.CallbackContext context)
        {
            if (context.performed) toggleChat?.Invoke();
        }
        public void OnToggleSettings(InputAction.CallbackContext context)
        {
            if (context.performed) toggleSettings?.Invoke();
        }

        public void OnTalk(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                talkKeyInteract?.Invoke(true);
                IsTalkKeyHeld = true;
            }

            if (!context.canceled) return;
            talkKeyInteract?.Invoke(false);
            IsTalkKeyHeld = false;
        }

        public void OnCursorUnlock(InputAction.CallbackContext context)
        {
        }


        private void Update()
        {
#if ENABLE_INPUT_SYSTEM

            if (_controls.Player.MousePress.WasPressedThisFrame() && !EventSystem.current.IsPointerOverGameObject()) LockCursor(true);
            if (_controls.Player.CursorUnlock.WasPressedThisFrame()) LockCursor(false);

#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetButton("Jump"))
        {
            jumping?.Invoke();
        }

        if (Input.GetKey(MovementKeys.Forward)) moveVector.y += 1f;
        if (Input.GetKey(MovementKeys.Backward)) moveVector.y -= 1f;
        if (Input.GetKey(MovementKeys.Left)) moveVector.x -= 1f;
        if (Input.GetKey(MovementKeys.Right)) moveVector.x += 1f;

        positionVector = Input.mousePosition;
        lookVector.x = Input.GetAxis("Mouse X");
        lookVector.y = Input.GetAxis("Mouse Y");

        if(Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject()) LockCursor();
        if(Input.GetKey(RunKey)) isRunning = !isRunning;
        if(Input.GetKeyDown(TextSendKey) || Input.GetKeyDown(TextSendAltKey)) sendText?.Invoke();
        if(Input.GetKeyDown(OpenSettingPanelKey)) toggleSettings?.Invoke();
        if(Input.GetKeyDown(TalkKey)) 
        {
            talkKeyInteract?.Invoke(true);
            IsTalkKeyHeld = true;
        }
        if(Input.GetKeyUp(TalkKey)) 
        {
            talkKeyInteract?.Invoke(false);
            IsTalkKeyHeld = false;
        }
#endif
        }

        private static void LockCursor(bool lockState)
        {
            Cursor.lockState = lockState ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockState;
        }

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        ///     Retrieves the InputAction associated with the talk key.
        /// </summary>
        /// <returns>The InputAction for handling talk-related input.</returns>
        public InputAction GetTalkKeyAction()
        {
            return _controls.Player.Talk;
        }
#endif
    }
}
#endif