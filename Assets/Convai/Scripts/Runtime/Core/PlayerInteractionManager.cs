using System;
using System.Linq;
using Convai.Scripts.Runtime.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Convai.Scripts.Runtime.Core
{
    /// <summary>
    /// Manages player input (text and voice) specifically directed towards an associated ConvaiNPC.
    /// Handles communication with ConvaiInputManager, UI elements (InputField), and triggering
    /// NPC actions/speech via ConvaiNPC and ConvaiGRPCWebAPI.
    /// Requires initialization via its Initialize method, typically called by ConvaiNPC.
    /// </summary>
    public class ConvaiPlayerInteractionManager : MonoBehaviour
    {
        // References (set via Initialize method)
        private ConvaiNPC _convaiNPC;
        private ConvaiChatUIHandler _convaiChatUIHandler;       // For chat input field and display
        private ConvaiCrosshairHandler _convaiCrosshairHandler; // For getting attention object context for Actions
        private ConvaiInputManager _inputManager;              // Source of player input actions
        private ConvaiGRPCWebAPI _grpcWebAPI;                  // Used for updating action config

        // State
        private TMP_InputField _currentInputField; // Cache the active input field reference

        #region Initialization & Lifecycle

        /// <summary>
        /// Initializes the manager with necessary component references.
        /// Must be called after instantiation, typically by the owning ConvaiNPC during Awake.
        /// </summary>
        /// <param name="convaiNPC">The NPC this manager handles input for.</param>
        /// <param name="convaiCrosshairHandler">Reference to the crosshair handler (can be null if Actions not used).</param>
        /// <param name="convaiChatUIHandler">Reference to the chat UI handler (can be null if text chat UI not used).</param>
        /// <exception cref="ArgumentNullException">Thrown if convaiNPC is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if required singleton instances (InputManager, GRPCWebAPI) are not found.</exception>
        public void Initialize(ConvaiNPC convaiNPC, ConvaiCrosshairHandler convaiCrosshairHandler, ConvaiChatUIHandler convaiChatUIHandler)
        {
            _convaiNPC = convaiNPC ?? throw new ArgumentNullException(nameof(convaiNPC));

            // Store references
            _convaiChatUIHandler = convaiChatUIHandler;
            _convaiCrosshairHandler = convaiCrosshairHandler;

            // Get required singleton instances
            _inputManager = ConvaiInputManager.Instance ?? throw new InvalidOperationException($"{nameof(ConvaiInputManager)} instance not found.");
            _grpcWebAPI = ConvaiGRPCWebAPI.Instance ?? throw new InvalidOperationException($"{nameof(ConvaiGRPCWebAPI)} instance not found.");

            // Subscribe to input events immediately after successful initialization
            SubscribeToInputEvents();
        }

        // Subscribe when component is enabled
        private void OnEnable()
        {
            if (_inputManager != null) // Only if initialized
            {
                SubscribeToInputEvents();
            }
        }

        // Unsubscribe when component is disabled or destroyed
        private void OnDisable()
        {
            UnsubscribeFromInputEvents();
        }

        /// <summary> Subscribes to relevant input events from ConvaiInputManager. </summary>
        private void SubscribeToInputEvents()
        {
            if (_inputManager == null) return;
            UnsubscribeFromInputEvents(); // Prevent double subscription
            _inputManager.sendText += HandleTextInput;
            _inputManager.toggleChat += HandleToggleChat;
            _inputManager.talkKeyInteract += HandleVoiceInput;
        }

        /// <summary> Unsubscribes from all input events. </summary>
        private void UnsubscribeFromInputEvents()
        {
            if (_inputManager != null)
            {
                _inputManager.sendText -= HandleTextInput;
                _inputManager.toggleChat -= HandleToggleChat;
                _inputManager.talkKeyInteract -= HandleVoiceInput;
            }
        }

        #endregion

        #region Input Handlers (Called by ConvaiInputManager events)

        /// <summary>
        /// Handles the text submission action (e.g., pressing Enter in chat).
        /// Finds the active input field, validates text, and processes submission.
        /// </summary>
        private void HandleTextInput()
        {
            // Ignore if this NPC is not the globally active one
            if (_convaiNPC != ConvaiNPCManager.Instance?.activeConvaiNPC) return;

            // Find the active input field within the current chat UI
            TMP_InputField inputFieldInScene = FindActiveInputField();
            UpdateCurrentInputFieldCache(inputFieldInScene); // Update cached reference

            // Check if the correct input field is focused and contains actual text
            if (_currentInputField != null && _currentInputField.isFocused)
            {
                string inputText = _currentInputField.text;
                if (!string.IsNullOrWhiteSpace(inputText))
                {
                    HandleInputSubmission(inputText);
                }
            }
        }

        /// <summary>
        /// Handles the voice input action (press/release of the talk key).
        /// Starts or stops NPC listening based on key state and context.
        /// </summary>
        /// <param name="isStartingToTalk">True when the key is pressed down, false when released.</param>
        private void HandleVoiceInput(bool isStartingToTalk)
        {
            // Ignore voice input if a UI input field is focused or the mouse pointer is over a UI element
            if (UIUtilities.IsAnyInputFieldFocused() || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;

            // Ignore if this NPC is not the globally active one
            if (_convaiNPC != ConvaiNPCManager.Instance?.activeConvaiNPC) return;

            if (isStartingToTalk) // Talk key pressed down
            {
                // Update context (attention object) before starting the interaction
                UpdateActionConfig();
                // Tell the associated NPC to start listening (using the NPC's method)
                _convaiNPC.StartListening();
            }
            else // Talk key released
            {
                // Stop listening only if the NPC is still considered active
                if (_convaiNPC.isCharacterActive)
                {
                    _convaiNPC.StopListening(); // Tell the NPC to stop listening
                }
            }
        }

        /// <summary>
        /// Handles the toggle chat input action (e.g., pressing Enter/Tab to focus the chat input field).
        /// Activates the input field if it's not already focused.
        /// </summary>
        private void HandleToggleChat()
        {
            // Ignore if this NPC is not the globally active one
            if (_convaiNPC != ConvaiNPCManager.Instance?.activeConvaiNPC) return;

            TMP_InputField inputFieldInScene = FindActiveInputField();
            UpdateCurrentInputFieldCache(inputFieldInScene);

            // If an input field exists and isn't currently focused, activate and select it
            if (_currentInputField != null && !_currentInputField.isFocused)
            {
                _currentInputField.ActivateInputField(); // Give focus
                _currentInputField.Select(); // Ensure caret/selection is active
            }
        }

        #endregion

        #region Internal Helpers & Action/UI Logic

        /// <summary>
        /// Processes validated text input: updates action config, interrupts NPC speech,
        /// sends text data via ConvaiNPC, and updates local UI.
        /// </summary>
        /// <param name="validInputText">The non-empty text submitted by the player.</param>
        private void HandleInputSubmission(string validInputText)
        {
            // Ensure NPC is still active
            if (!_convaiNPC.isCharacterActive) return;

            // 1. Update attention object / action config
            UpdateActionConfig();

            // 2. Interrupt any ongoing speech from the character
            _convaiNPC.InterruptCharacterSpeech();

            // 3. Send the text data TO THE NPC (which then calls the GRPC API)
            _convaiNPC.SendTextData(validInputText);

            // 4. Display the player's text immediately in the chat UI (if available)
            _convaiChatUIHandler?.SendPlayerText(validInputText);

            // 5. Clear the input field
            ClearInputField();
        }

        /// <summary>
        /// Finds the currently interactable TMP_InputField within the Chat UI hierarchy.
        /// </summary>
        /// <returns>The active TMP_InputField, or null if none is found or interactable.</returns>
        public TMP_InputField FindActiveInputField()
        {
            if (_convaiChatUIHandler == null) return null;
            // Assumes GetCurrentUI exists and returns the relevant UI parent
            GameObject currentUIRoot = _convaiChatUIHandler.GetCurrentUI()?.GetCanvasGroup()?.gameObject;
            if (currentUIRoot == null || !currentUIRoot.activeInHierarchy) return null;

            // Find first interactable & active TMP_InputField in children
            return currentUIRoot.GetComponentsInChildren<TMP_InputField>(true)
                              .FirstOrDefault(inputField => inputField.interactable && inputField.gameObject.activeInHierarchy);
        }

        /// <summary>
        /// Updates the internal cached reference `_currentInputField`.
        /// </summary>
        /// <param name="foundInputField">The input field potentially found this frame.</param>
        private void UpdateCurrentInputFieldCache(TMP_InputField foundInputField)
        {
            if (_currentInputField != foundInputField)
            {
                _currentInputField = foundInputField;
            }
        }

        /// <summary>
        /// Clears the text content and deactivates the currently cached input field.
        /// Also attempts to deselect it from the global EventSystem.
        /// </summary>
        private void ClearInputField()
        {
            if (_currentInputField != null)
            {
                _currentInputField.text = string.Empty;
                _currentInputField.DeactivateInputField();
                // Deselect from EventSystem if it was the selected object
                if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject == _currentInputField.gameObject)
                {
                    EventSystem.current.SetSelectedGameObject(null);
                }
            }
        }

        /// <summary>
        /// Updates the Action Configuration's `currentAttentionObject` based on the CrosshairHandler
        /// and sends the update via the GRPC API.
        /// </summary>
        private void UpdateActionConfig()
        {
            // Requires ActionsHandler on NPC, its ActionConfig, CrosshairHandler, and GRPC API
            ActionConfig currentActionConfig = _convaiNPC?.ActionsHandler?.ActionConfig; // Assumes ActionConfig is defined elsewhere
            if (currentActionConfig == null || _convaiCrosshairHandler == null || _grpcWebAPI == null)
            {
                return; // Silently return if prerequisites are missing
            }

            // Get the object identifier the player is looking at
            string attentionObjectName = _convaiCrosshairHandler.FindPlayerReferenceObject();


            currentActionConfig.currentAttentionObject = attentionObjectName;
            // Send the *entire* updated config object via the GRPC API
            _grpcWebAPI.UpdateActionConfig(currentActionConfig);

        }

        #endregion

    }
}