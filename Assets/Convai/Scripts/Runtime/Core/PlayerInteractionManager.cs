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
        private ConvaiNPC _convaiNPC;
        private TMP_InputField _currentInputField;
        private ConvaiInputManager _inputManager;

        private void Start()
        {
            _inputManager.sendText += HandleTextInput;
            _inputManager.talkKeyInteract += HandleVoiceInput;
        }

        private void OnDisable()
        {
            _inputManager.sendText -= HandleTextInput;
            _inputManager.talkKeyInteract -= HandleVoiceInput;
        }

        public void Initialize(ConvaiNPC convaiNPC, ConvaiChatUIHandler convaiChatUIHandler)
        {
            _convaiNPC = convaiNPC ? convaiNPC : throw new ArgumentNullException(nameof(convaiNPC));
            _convaiChatUIHandler = convaiChatUIHandler ? convaiChatUIHandler : throw new ArgumentNullException(nameof(convaiChatUIHandler));
            _inputManager = ConvaiInputManager.Instance ? ConvaiInputManager.Instance : throw new InvalidOperationException("ConvaiInputManager instance not found.");
        }

        private void UpdateCurrentInputField(TMP_InputField inputFieldInScene)
        {
            if (inputFieldInScene != null && _currentInputField != inputFieldInScene) _currentInputField = inputFieldInScene;
        }

        private void HandleInputSubmission(string input)
        {
            if (!_convaiNPC.isCharacterActive) return;
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
            if (_currentInputField != null && _currentInputField.isFocused && _convaiNPC.isCharacterActive) HandleInputSubmission(_currentInputField.text);
        }

        private void HandleVoiceInput(bool listenState)
        {
            if (UIUtilities.IsAnyInputFieldFocused() || !_convaiNPC.isCharacterActive) return;
            switch (listenState)
            {
                case true:
                    _convaiNPC.StartListening();
                    break;
                case false:
                {
                    if (_convaiNPC.isCharacterActive && (_currentInputField == null || !_currentInputField.isFocused)) _convaiNPC.StopListening();
                    break;
                }
            }
        }
    }
}