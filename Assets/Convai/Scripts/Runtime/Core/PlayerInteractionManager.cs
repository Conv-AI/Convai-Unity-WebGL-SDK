using System;
using System.Linq;
using Convai.Scripts.Runtime.UI;
using TMPro;
using UnityEngine;

namespace Convai.Scripts.Runtime.Core
{
    public class ConvaiPlayerInteractionManager : MonoBehaviour
    {
        private ConvaiChatUIHandler _convaiChatUIHandler;
        private ConvaiCrosshairHandler _convaiCrosshairHandler;
        private ConvaiNPC _convaiNPC;
        private TMP_InputField _currentInputField;
        private ConvaiInputManager _inputManager;

        private void Start()
        {
            if (_inputManager == null) return;
            _inputManager.sendText += HandleTextInput;
            _inputManager.toggleChat += HandleToggleChat;
            _inputManager.talkKeyInteract += HandleVoiceInput;
        }

        private void OnDisable()
        {
            if (_inputManager == null) return;
            _inputManager.sendText -= HandleTextInput;
            _inputManager.toggleChat -= HandleToggleChat;
            _inputManager.talkKeyInteract -= HandleVoiceInput;
        }

        public void Initialize(ConvaiNPC convaiNPC, ConvaiCrosshairHandler convaiCrosshairHandler, ConvaiChatUIHandler convaiChatUIHandler)
        {
            _convaiNPC = convaiNPC ? convaiNPC : throw new ArgumentNullException(nameof(convaiNPC));
            _convaiChatUIHandler = convaiChatUIHandler ? convaiChatUIHandler : throw new ArgumentNullException(nameof(convaiChatUIHandler));
            _convaiCrosshairHandler = convaiCrosshairHandler ? convaiCrosshairHandler : throw new ArgumentNullException(nameof(convaiCrosshairHandler));
            _inputManager = ConvaiInputManager.Instance ? ConvaiInputManager.Instance : throw new InvalidOperationException("ConvaiInputManager instance not found.");
        }

        private void UpdateCurrentInputField(TMP_InputField inputFieldInScene)
        {
            if (inputFieldInScene != null && _currentInputField != inputFieldInScene) _currentInputField = inputFieldInScene;
        }

        private void HandleInputSubmission(string input)
        {
            if (!_convaiNPC.isCharacterActive) return;
            UpdateActionConfig();
            _convaiNPC.InterruptCharacterSpeech();
            _convaiNPC.SendTextData(input);
            _convaiChatUIHandler.SendPlayerText(input);
            ClearInputField();
        }

        public TMP_InputField FindActiveInputField()
        {
            // TODO : Implement Text Send for ChatUIBase and get input field directly instead of finding here
            return _convaiChatUIHandler.GetCurrentUI().GetCanvasGroup().gameObject.GetComponentsInChildren<TMP_InputField>(true)
                .FirstOrDefault(inputField => inputField.interactable);
        }

        private void ClearInputField()
        {
            if (_currentInputField != null)
            {
                _currentInputField.text = string.Empty;
                _currentInputField.DeactivateInputField();
            }
        }

        private void HandleTextInput()
        {
            TMP_InputField inputFieldInScene = FindActiveInputField();
            UpdateCurrentInputField(inputFieldInScene);
            if (_currentInputField != null && _currentInputField.isFocused && _convaiNPC.isCharacterActive && (!String.IsNullOrEmpty(_currentInputField.text) && !String.IsNullOrWhiteSpace(_currentInputField.text))) HandleInputSubmission(_currentInputField.text);
        }

        private void HandleVoiceInput(bool listenState)
        {
            if (UIUtilities.IsAnyInputFieldFocused() || !_convaiNPC.isCharacterActive) return;
            switch (listenState)
            {
                case true:
                    UpdateActionConfig();
                    _convaiNPC.StartListening();
                    break;
                case false:
                {
                    if (_convaiNPC.isCharacterActive && (_currentInputField == null || !_currentInputField.isFocused)) _convaiNPC.StopListening();
                    break;
                }
            }
        }

        private void HandleToggleChat()
        {
            TMP_InputField inputFieldInScene = FindActiveInputField();
            if (!inputFieldInScene.isFocused && _convaiNPC.isCharacterActive)
            {
                inputFieldInScene.ActivateInputField();
            }
        }

        private void UpdateActionConfig()
        {
            if (_convaiNPC.ConvaiActionsHandler.ActionConfig != null && _convaiCrosshairHandler != null)
            {
                _convaiNPC.ConvaiActionsHandler.ActionConfig.currentAttentionObject = _convaiCrosshairHandler.FindPlayerReferenceObject();
                ConvaiGRPCWebAPI.Instance.UpdateActionConfig(_convaiNPC.ConvaiActionsHandler.ActionConfig);
            }
        }
    }
}