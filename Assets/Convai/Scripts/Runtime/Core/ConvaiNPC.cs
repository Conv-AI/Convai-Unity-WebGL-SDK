using System;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Attributes;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.UI;
using Convai.Scripts.Runtime.LoggerSystem;
using UnityEngine;
using UnityEngine.Events;

namespace Convai.Scripts.Runtime.Core
{
    /// <summary>
    /// Represents a Convai Non-Player Character (NPC) in the scene.
    /// Manages character data, state, core components, optional features, configuration flags,
    /// interaction triggers, and links player input to the ConvaiGRPCWebAPI.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Convai/ConvaiNPC")]
    public class ConvaiNPC : MonoBehaviour
    {
        // Constants & Static Readonly
        private static readonly int TalkAnimHash = Animator.StringToHash("Talk");

        [Header("Character Information")]
        [Tooltip("Display name for this NPC.")]
        public string characterName = "NPC Name";
        [Tooltip("Unique Convai Character ID from the Convai")]
        public string characterID = "";
        [Tooltip("Current session ID for the conversation")]
        [ReadOnly] public string sessionID = "-1";

        [Header("State (Read Only)")]
        [Tooltip("Is this character currently speaking?")]
        [ReadOnly] public bool isCharacterTalking;
        [Tooltip("Is this character the primary interaction target?")]
        [ReadOnly] public bool isCharacterActive;

        // Core Components
        private Animator _characterAnimator;

        // API & Manager References
        private ConvaiGRPCWebAPI _grpcWebAPI;
        private ConvaiChatUIHandler _convaiChatUIHandler;
        private ConvaiCrosshairHandler _convaiCrosshairHandler;

        [HideInInspector] public ConvaiPlayerInteractionManager playerInteractionManager;

        #region Feature Components & Configuration Flags

        public ConvaiLipSync LipSync { get; private set; }
        public NarrativeDesignManager NarrativeDesignManager { get; private set; }
        public ConvaiActionsHandler ActionsHandler { get; private set; }
        public NarrativeDesignKeyController NarrativeDesignKeyController { get; private set; }

        // --- Internal State Flags ---
        private bool _animationPlaying;
        private bool _isLipSyncComponentActive;

        // --- Configuration Flags ---
        [Tooltip("Enable Actions feature handling for this NPC.")]
        [field: NonSerialized] public bool enableActionsHandler { get; set; }

        [Tooltip("Enable LipSync feature for this NPC.")]
        [field: NonSerialized] public bool enableLipSync { get; set; }

        [Tooltip("Enable Narrative Design Manager feature for this NPC.")]
        [field: NonSerialized] public bool enableNarrativeDesignManager { get; set; }

        [Tooltip("Enable Narrative Design Key Controller feature for this NPC.")]
        [field: NonSerialized] public bool enableNarrativeDesignKeyController { get; set; }

        [Tooltip("Enable Head & Eye Tracking feature for this NPC.")]
        [field: NonSerialized] public bool enableHeadEyeTracking { get; set; }

        [Tooltip("Enable automatic eye blinking feature for this NPC.")]
        [field: NonSerialized] public bool enableEyeBlinking { get; set; }

        #endregion

        #region Unity Events
        [Header("Events")]
        [Tooltip("UnityEvent invoked when a trigger message/event is sent FROM this NPC.")]
        public TriggerUnityEvent onTriggerSent;
        #endregion

        #region Unity Lifecycle Methods

        private void Awake()
        {
            // Get essential components
            _characterAnimator = GetComponent<Animator>();
            if (_characterAnimator == null)
                ConvaiLogger.Error("Missing required Animator component!", ConvaiLogger.LogCategory.Character, this);

            // Find scene components
            _convaiChatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();
            _convaiCrosshairHandler = FindObjectOfType<ConvaiCrosshairHandler>();

            // Ensure the PlayerInteractionManager component exists and initialize it
            InitializePlayerInteractionManager();

            // Dynamically find and assign optional feature components to public properties
            _isLipSyncComponentActive = TryGetComponent(out ConvaiLipSync lipSyncRef); // Check if component exists
            if (_isLipSyncComponentActive) LipSync = lipSyncRef; // Assign if found

            if (TryGetComponent(out NarrativeDesignManager narrativeDesignManager)) NarrativeDesignManager = narrativeDesignManager;
            if (TryGetComponent(out ConvaiActionsHandler actionsHandler)) ActionsHandler = actionsHandler;
            if (TryGetComponent(out NarrativeDesignKeyController narrativeDesignKeyController)) NarrativeDesignKeyController = narrativeDesignKeyController;
        }

        private void Start()
        {
            // Assign the singleton GRPC API instance
            _grpcWebAPI = ConvaiGRPCWebAPI.Instance;
            if (_grpcWebAPI == null)
            {
                ConvaiLogger.Error($"{nameof(ConvaiGRPCWebAPI)} instance not found! Interactions will fail. Disabling component.", ConvaiLogger.LogCategory.Character, this);
                enabled = false; return;
            }
            // Subscribe to the global speaking status change event.
            SubscribeToSpeakingChanges();
        }

        private void OnEnable()
        {
            _convaiChatUIHandler ??= ConvaiChatUIHandler.Instance;
            _convaiChatUIHandler?.UpdateCharacterList();
            SubscribeToSpeakingChanges();
        }

        private void OnDisable()
        {
            InterruptCharacterSpeech();
            UnsubscribeFromSpeakingChanges();
            _convaiChatUIHandler?.UpdateCharacterList();
            if (isCharacterTalking) { ForceStopLocalPlaybackAndAnimation(); }
            _animationPlaying = false;
            if (_characterAnimator != null) _characterAnimator.SetBool(TalkAnimHash, false);
        }

        private void SubscribeToSpeakingChanges()
        {
            if (_grpcWebAPI != null)
            {
                _grpcWebAPI.OnCharacterSpeakingChanged += HandleCharacterSpeakingStatusChanged;
            }
        }

        private void UnsubscribeFromSpeakingChanges()
        {
            if (_grpcWebAPI != null) { _grpcWebAPI.OnCharacterSpeakingChanged -= HandleCharacterSpeakingStatusChanged; }
        }

        #endregion

        #region Public Interaction API Methods

        /// <summary> Starts voice input recording via GRPC API after interrupting self. </summary>
        public void StartListening()
        {
            if (_grpcWebAPI == null) { ConvaiLogger.Warn($"Cannot {nameof(StartListening)}: API instance missing.", ConvaiLogger.LogCategory.Character, this); return; }
            InterruptCharacterSpeech();
            _grpcWebAPI.RequestStartRecordAudio();
        }

        /// <summary> Stops voice input recording via GRPC API. </summary>
        public void StopListening()
        {
            if (_grpcWebAPI == null) { ConvaiLogger.Warn($"Cannot {nameof(StopListening)}: API instance missing.", ConvaiLogger.LogCategory.Character, this); return; }
            _grpcWebAPI.RequestStopRecordAudio();
        }

        /// <summary> Sends text input via GRPC API. </summary>
        public void SendTextData(string text)
        {
            if (_grpcWebAPI == null) { ConvaiLogger.Warn($"Cannot {nameof(SendTextData)}: API instance missing.", ConvaiLogger.LogCategory.Character, this); return; }
            if (string.IsNullOrWhiteSpace(text)) { ConvaiLogger.Warn("Attempted to send empty text data.", ConvaiLogger.LogCategory.Character, this); return; }
            _grpcWebAPI.RequestSendTextData(text);
        }

        /// <summary> Sends a named trigger event via GRPC API and invokes local event. </summary>
        public void TriggerEvent(string triggerName)
        {
            if (_grpcWebAPI == null) { ConvaiLogger.Warn($"Cannot {nameof(TriggerEvent)}: API instance missing.", ConvaiLogger.LogCategory.Character, this); return; }
            if (string.IsNullOrEmpty(triggerName)) { ConvaiLogger.Warn($"{nameof(TriggerEvent)} called with empty trigger name.", ConvaiLogger.LogCategory.Character, this); }
            TriggerConfig triggerConfig = new() { TriggerName = triggerName, TriggerMessage = "" };
            _grpcWebAPI.SendTriggerConfig(triggerConfig);
            onTriggerSent?.Invoke(triggerConfig.TriggerMessage, triggerConfig.TriggerName);
        }

        /// <summary> Sends a trigger message (for speech/action) via GRPC API and invokes local event. </summary>
        public void TriggerSpeech(string triggerMessage)
        {
            if (_grpcWebAPI == null) { ConvaiLogger.Warn($"Cannot {nameof(TriggerSpeech)}: API instance missing.", ConvaiLogger.LogCategory.Character, this); return; }
            if (string.IsNullOrEmpty(triggerMessage)) { ConvaiLogger.Warn($"{nameof(TriggerSpeech)} called with empty trigger message.", ConvaiLogger.LogCategory.Character, this); }
            TriggerConfig triggerConfig = new() { TriggerName = "", TriggerMessage = triggerMessage };
            _grpcWebAPI.SendTriggerConfig(triggerConfig);
            onTriggerSent?.Invoke(triggerConfig.TriggerMessage, triggerConfig.TriggerName);
        }


        /// <summary> Interrupts character speech locally and requests interruption via GRPC API. </summary>
        public void InterruptCharacterSpeech()
        {
            if (!isCharacterTalking) return;
            ConvaiLogger.Info("Interrupting speech.", ConvaiLogger.LogCategory.Character, this);
            isCharacterTalking = false;
            StopAllCoroutines();
            _grpcWebAPI?.InterruptCharacterSpeech();
            // Local cleanup
            StopLipSyncIfActive();
            HandleCharacterTalkingAnimation(false);
            _animationPlaying = false;
        }

        /// <summary> Forcefully stops lipsync and resets animation state. </summary>
        public void ForceStopLocalPlaybackAndAnimation()
        {
            StopLipSyncIfActive();
            HandleCharacterTalkingAnimation(false);
            isCharacterTalking = false;
            _animationPlaying = false;
        }

        #endregion

        #region Internal State, Feature Handling & Helpers

        /// <summary> Stops LipSync using the public component reference if active. </summary>
        private void StopLipSyncIfActive()
        {
            if (_isLipSyncComponentActive && LipSync != null)
            {
                LipSync.StopLipSync();
            }
        }

        /// <summary>
        /// Handles the global talking status update from ConvaiGRPCWebAPI.
        /// Updates state/animation only if the event is for this NPC instance.
        /// </summary>
        private void HandleCharacterSpeakingStatusChanged(bool isNowTalking)
        {
            if (_grpcWebAPI != null && _grpcWebAPI.CurrentInteractingNPC == this)
            {
                if (isCharacterTalking != isNowTalking)
                {
                    isCharacterTalking = isNowTalking;
                    HandleCharacterTalkingAnimation(isNowTalking);
                }
            }
        }

        /// <summary> Manages the Animator's 'Talk' parameter based on speaking status. </summary>
        private void HandleCharacterTalkingAnimation(bool shouldTalk)
        {
            if (_characterAnimator == null) return;
            if (shouldTalk && !_animationPlaying)
            {
                _animationPlaying = true; _characterAnimator.SetBool(TalkAnimHash, true);
            }
            else if (!shouldTalk && _animationPlaying)
            {
                _animationPlaying = false; _characterAnimator.SetBool(TalkAnimHash, false);
            }
            else if (!shouldTalk && _characterAnimator.GetBool(TalkAnimHash))
            { _characterAnimator.SetBool(TalkAnimHash, false); }
        }

        /// <summary> Ensures the ConvaiPlayerInteractionManager component exists and initializes it. </summary>
        private void InitializePlayerInteractionManager()
        {
            playerInteractionManager = GetComponent<ConvaiPlayerInteractionManager>();
            if (playerInteractionManager == null)
                playerInteractionManager = gameObject.AddComponent<ConvaiPlayerInteractionManager>();
            _convaiCrosshairHandler ??= FindFirstObjectByType<ConvaiCrosshairHandler>();
            _convaiChatUIHandler ??= ConvaiChatUIHandler.Instance;
            try { playerInteractionManager.Initialize(this, _convaiCrosshairHandler, _convaiChatUIHandler); }
            catch (Exception e) { ConvaiLogger.Error($"Failed to initialize {nameof(ConvaiPlayerInteractionManager)}: {e.Message}", ConvaiLogger.LogCategory.Character, this); }
        }

        #endregion // Internal State & Helpers Ends

        #region Nested Classes & Events

        /// <summary> Defines a UnityEvent for triggers, accepting message and name strings. </summary>
        [Serializable]
        public class TriggerUnityEvent : UnityEvent<string, string> { }

        #endregion // Nested Classes & Events Ends

    }
}