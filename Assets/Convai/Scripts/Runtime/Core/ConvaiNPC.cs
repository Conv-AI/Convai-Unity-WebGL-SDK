using System;
using System.Collections;
using System.Collections.Generic;
using Convai.Scripts.Runtime.Attributes;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.UI;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

namespace Convai.Scripts.Runtime.Core
{
    [RequireComponent(typeof(Animator), typeof(AudioSource))]
    [AddComponentMenu("Convai/ConvaiNPC")]
    public class ConvaiNPC : MonoBehaviour
    {
        private static readonly int Talk = Animator.StringToHash("Talk");

        [Header("Character Information")] [Tooltip("Enter the character name for this NPC.")]
        public string characterName;

        [Tooltip("Enter the character ID for this NPC.")]
        public string characterID;

        [Tooltip("The current session ID for the chat with this NPC.")] [ReadOnly]
        public string sessionID = "-1";

        [Tooltip("Is this character talking?")] [ReadOnly]
        public bool isCharacterTalking;

        [Tooltip("Is this character active?")] [ReadOnly]
        public bool isCharacterActive;

        private readonly List<ResponseAudio> _responseAudios = new();
        private readonly List<AudioData> _audioDataList = new();
        private bool _animationPlaying;
        private AudioSource _audioSource;
        private bool _canPlayAudio;
        private Animator _characterAnimator;
        private ConvaiChatUIHandler _convaiChatUIHandler;
        private TMP_InputField _currentInputField;
        private ConvaiGRPCWebAPI _grpcWebAPI;
        private bool _isActionActive;
        private bool _isLipSyncActive;
        private string _lastReceivedText;
        private bool _playingStopLoop;
        public ConvaiLipSync ConvaiLipSync { get; private set; }
        [HideInInspector] public ConvaiPlayerInteractionManager playerInteractionManager;
        private bool IsCharacterActive => isCharacterActive;
        private bool IsCharacterTalking
        {
            get => isCharacterTalking;
            set => isCharacterTalking = value;
        }

        // Properties with getters and setters
        [field: NonSerialized] public bool LipSync { get; set; }
        [field: NonSerialized] public bool HeadEyeTracking { get; set; }
        [field: NonSerialized] public bool EyeBlinking { get; set; }

        /// <summary>
        ///     Unity method called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            // Find and assign necessary components
            _convaiChatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();
            _audioSource = GetComponent<AudioSource>();
            _characterAnimator = GetComponent<Animator>();
            InitializePlayerInteractionManager();

            if (TryGetComponent(out ConvaiLipSync convaiLipSync))
            {
                _isLipSyncActive = true;
                ConvaiLipSync = convaiLipSync;
            }

            OnCharacterTalking += HandleCharacterTalkingAnimation;
        }

        /// <summary>
        ///     Unity method called on the frame when a script is enabled.
        /// </summary>
        private void Start()
        {
            // Assign the ConvaiGRPCAPI component in the scene
            _grpcWebAPI = ConvaiGRPCWebAPI.Instance;
        }

        /// <summary>
        ///     Unity method called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            _convaiChatUIHandler = ConvaiChatUIHandler.Instance;
            if (_convaiChatUIHandler != null) _convaiChatUIHandler.UpdateCharacterList();
        }

        /// <summary>
        ///     Unity method called when the MonoBehaviour will be destroyed.
        /// </summary>
        private void OnDestroy()
        {
            OnCharacterTalking -= HandleCharacterTalkingAnimation;
            if (_convaiChatUIHandler != null) _convaiChatUIHandler.UpdateCharacterList();
        }
        
        // Events
        public event Action<bool> OnCharacterTalking;

        /// <summary>
        ///     Initializes the session in an asynchronous manner and handles the receiving of results from the server.
        ///     Initiates the audio recording process using the gRPC API.
        /// </summary>
        public void StartListening()
        {
            InterruptCharacterSpeech();
            _grpcWebAPI.StartRecordAudio();
        }

        /// <summary>
        ///     Stops the ongoing audio recording process.
        /// </summary>
        public void StopListening()
        {
            // Stop the audio recording process using the ConvaiGRPCAPI StopRecordAudio method
            _grpcWebAPI.StopRecordAudio();
        }

        /// <summary>
        ///     Interrupts the speech playback, clears audio and response lists, and resets character animation.
        /// </summary>
        public void InterruptCharacterSpeech()
        {
            if (!isCharacterTalking) return;
            isCharacterTalking = false;
            _canPlayAudio = false;
            StopAllCoroutines();
            _grpcWebAPI.InterruptCharacterSpeech();
            _audioDataList.Clear();
            _responseAudios.Clear();
            if (_isLipSyncActive) ConvaiLipSync.StopLipSync();

            HandleCharacterTalkingAnimation(false);
            StopAllAudioPlayback();
        }

        /// <summary>
        ///     Processes a response fetched from a character.
        /// </summary>
        /// <remarks>
        ///     1. Processes audio/text/face data from the response and adds it to _responseAudios.
        ///     2. Identifies actions from the response and parses them for execution.
        /// </remarks>
        private void ProcessResponse()
        {
            if (IsCharacterActive && _audioDataList.Count > 0)
            {
                AudioData audioData = _audioDataList[0];

                SetCharacterTalking(true);

                AudioClip clip;

                if (audioData.isFirst)
                    clip = _grpcWebAPI.ProcessByteAudioDataToTrimmedAudioClip(audioData.audData,
                        audioData.sampleRate.ToString());
                else
                    clip = _grpcWebAPI.ProcessByteAudioDataToAudioClip(audioData.audData,
                        audioData.sampleRate.ToString());

                if (clip != null)
                    _responseAudios.Add(new ResponseAudio
                    {
                        AudioClip = clip,
                        ResponseText = audioData.resText
                    });

                _audioDataList.RemoveAt(0);
            }

            if (_responseAudios.Count > 0 && !_canPlayAudio)
            {
                _canPlayAudio = true;
                StartCoroutine(PlayAudioInOrder());
            }
            else if (_responseAudios.Count <= 0 && _canPlayAudio)
            {
                _canPlayAudio = false;
                StopCoroutine(PlayAudioInOrder());
            }
        }

        /// <summary>
        ///     Plays audio clips attached to characters in the order of responses.
        /// </summary>
        /// <returns>
        ///     A IEnumerator that can facilitate coroutine functionality
        /// </returns>
        /// <remarks>
        ///     Starts a loop that plays audio from response, and performs corresponding actions and animations.
        /// </remarks>
        private IEnumerator PlayAudioInOrder()
        {
            while (_canPlayAudio)
                // Check if there are audio clips in the playlist
                if (_responseAudios.Count > 0)
                {
                    PlayResponseAudio(_responseAudios[0]);

                    yield return new WaitForSeconds(_responseAudios[0].AudioClip.length);
                    StopAllAudioPlayback();
                    _responseAudios.RemoveAt(0);
                }
                else
                {
                    yield return new WaitForSeconds(0.1f);
                    SetCharacterTalking(false);
                }
        }

        public void StopAllAudioPlayback()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                _audioSource.Stop(); // Stops the audio if it's playing
                _audioSource.clip = null;
            }
        }

        /// <summary>
        ///     Handles the character's talking animation based on whether the character is currently talking.
        /// </summary>
        private void HandleCharacterTalkingAnimation(bool isTalking)
        {
            if (isTalking)
            {
                if (!_animationPlaying)
                {
                    _animationPlaying = true;
                    _characterAnimator.SetBool(Talk, true);
                }
            }
            else if (_animationPlaying)
            {
                _animationPlaying = false;
                _characterAnimator.SetBool(Talk, false);
            }
        }

        private void InitializePlayerInteractionManager()
        {
            playerInteractionManager = gameObject.AddComponent<ConvaiPlayerInteractionManager>();
            playerInteractionManager.Initialize(this, _convaiChatUIHandler);
        }

        /// <summary>
        ///     Sends text data to the server.
        /// </summary>
        /// <param name="text">The text to send.</param>
        public void SendTextData(string text)
        {
            try
            {
                _grpcWebAPI.SendTextData(text);
            }
            catch (Exception ex)
            {
                // Handle the exception, e.g., show a message to the user.
                ConvaiLogger.Error(ex, ConvaiLogger.LogCategory.Character);
            }
        }

        /// <summary>
        ///     Sets the character talking state.
        /// </summary>
        /// <param name="isTalking">Specifies whether the character is talking.</param>
        public void SetCharacterTalking(bool isTalking)
        {
            if (IsCharacterTalking != isTalking)
            {
                ConvaiLogger.Info($"Character {characterName} is talking: {isTalking}", ConvaiLogger.LogCategory.Character);
                IsCharacterTalking = isTalking;
                OnCharacterTalking?.Invoke(IsCharacterTalking);
            }
        }

        /// <summary>
        ///     Adds the given audio data to the list and processes the response.
        /// </summary>
        /// <param name="audioData">The audio data to be added.</param>
        public void AddAudioData(AudioData audioData)
        {
            _audioDataList.Add(audioData);
            ProcessResponse();
        }

        /// <summary>
        ///     Plays the audio from the given response.
        /// </summary>
        /// <param name="responseAudio">The response containing audio to be played.</param>
        private void PlayResponseAudio(ResponseAudio responseAudio)
        {
            _audioSource.clip = responseAudio.AudioClip;
            _audioSource.Play();
            SetCharacterTalking(true);
        }

        /// <summary>
        ///     Represents audio data,text and its finality status in a response.
        /// </summary>
        private class ResponseAudio
        {
            /// <summary>
            ///     The audio clip associated with the response.
            /// </summary>
            public AudioClip AudioClip;

            /// <summary>
            ///     Specifies whether the audio is final or not.
            /// </summary>
            public bool IsFinal;

            /// <summary>
            ///     The text associated with the audio.
            /// </summary>
            public string ResponseText;
        }
    }
}