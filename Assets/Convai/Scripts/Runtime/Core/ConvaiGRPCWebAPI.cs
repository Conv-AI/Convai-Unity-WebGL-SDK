using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Convai.Scripts.Runtime.Features;
using Convai.Scripts.Runtime.UI;
using Convai.Scripts.Runtime.Utils;
using UnityEngine;

namespace Convai.Scripts.Runtime.Core
{
    /// <summary>
    ///     Main class for handling Convai GRPC Web API.
    /// </summary>
    public class ConvaiGRPCWebAPI : MonoBehaviour
    {
        public static ConvaiGRPCWebAPI Instance; // Singleton instance
        [HideInInspector] public ConvaiNPC activeConvaiNPC; // Active Convai NPC
        [HideInInspector] public string APIKey; // API Key for Convai
        private ConvaiObjectPool<AudioData> _audioDataPool;
        private ConvaiChatUIHandler _convaiChatUIHandler; // UI handler for Convai chat
        private string _lastReceivedText;

        private ConvaiObjectPool<VisemesData> _visemesDataPool;

        /// <summary>
        ///     Awake is called when the script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);

            _convaiChatUIHandler = FindObjectOfType<ConvaiChatUIHandler>();
            try
            {
                initMicrophone();
            }
            catch (Exception)
            {
                Debug.LogWarning("WebGL SDK does not run in Unity Editor. Please build and run in WebGL.");
            }

            ConvaiAPIKeySetup apiKeyScriptableObject = Resources.Load<ConvaiAPIKeySetup>("ConvaiAPIKey");

            if (apiKeyScriptableObject != null)
                APIKey = apiKeyScriptableObject.APIKey;
            else
                Debug.LogError(
                    "No API Key data found. Please complete the Convai Setup. In the Menu Bar, click Convai > Setup.");

            _audioDataPool = new ConvaiObjectPool<AudioData>(10);
            _visemesDataPool = new ConvaiObjectPool<VisemesData>(10);
        }

        private void Start()
        {
            ConvaiNPCManager.Instance.OnActiveNPCChanged += HandleActiveNPCChanged;
        }

        private void OnDestroy()
        {
            ConvaiNPCManager.Instance.OnActiveNPCChanged -= HandleActiveNPCChanged;
        }

        /// <summary>
        ///     Handles active NPC changed event. Will be called when the active NPC is changed.
        /// </summary>
        /// <param name="newActiveNPC"> Gets the new active NPC </param>
        private void HandleActiveNPCChanged(ConvaiNPC newActiveNPC)
        {
            if (newActiveNPC == null) return;
            activeConvaiNPC = newActiveNPC;


#if UNITY_WEBGL && !UNITY_EDITOR
            if (activeConvaiNPC.isInitialized) return;

            string templateKeyJSON = String.Empty;
            string actionConfig = String.Empty;

            if (activeConvaiNPC.ConvaiNarrativeDesignKeyController != null)
            {
                templateKeyJSON = JsonUtility.ToJson(activeConvaiNPC.ConvaiNarrativeDesignKeyController.narrativeDesignKeyController);
            }

            if (activeConvaiNPC.ConvaiActionsHandler != null)
            {
                actionConfig = JsonUtility.ToJson(activeConvaiNPC.ConvaiActionsHandler.ActionConfig);
            }

            if (string.IsNullOrEmpty(templateKeyJSON))
            {
                InitializeConvaiClient(activeConvaiNPC.characterID, true, false, actionConfig);
            }
            else
            {
                InitializeConvaiClient(activeConvaiNPC.characterID, true, false, actionConfig, templateKeyJSON);
            }
            activeConvaiNPC.isInitialized = true;
#else
            Debug.LogWarning("WebGL SDK does not run in Unity Editor. Please build and run in WebGL.");
#endif
        }

        /// <summary>
        ///     Converts 16 bit byte array to float audio clip data.
        /// </summary>
        private float[] Convert16BitByteArrayToFloatAudioClipData(byte[] source)
        {
            int convertedSize = source.Length / sizeof(short);
            float[] data = new float[convertedSize];

            int byteIdx = 0;
            int dataIdx = 0;

            while (byteIdx < source.Length)
            {
                short sample = BitConverter.ToInt16(source, byteIdx);
                float normalizedSample = sample / 32768.0f;

                data[dataIdx] = normalizedSample;

                byteIdx += sizeof(short);
                dataIdx++;
            }

            return data;
        }

        /// <summary>
        ///     Processes byte audio data to audio clip.
        /// </summary>
        public AudioClip ProcessByteAudioDataToAudioClip(byte[] byteAudio, string stringSampleRate)
        {
            float[] samples = Convert16BitByteArrayToFloatAudioClipData(byteAudio);

            int channels = 1;
            int sampleRate = int.Parse(stringSampleRate);

            if (samples.Length > 0)
            {
                AudioClip clip = AudioClip.Create("ClipName", samples.Length, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }

            return null;
        }

        /// <summary>
        ///     Processes byte audio data to trimmed audio clip.
        /// </summary>
        public AudioClip ProcessByteAudioDataToTrimmedAudioClip(byte[] byteAudio, string stringSampleRate)
        {
            // trim 44 bytes of header

            byte[] trimmedByteAudio = new byte[byteAudio.Length - 44];

            for (int i = 0, j = 44; i < byteAudio.Length - 44; i++, j++) trimmedByteAudio[i] = byteAudio[j];

            float[] samples = Convert16BitByteArrayToFloatAudioClipData(trimmedByteAudio);

            int channels = 1;
            int sampleRate = int.Parse(stringSampleRate);

            if (samples.Length > 0)
            {
                AudioClip clip = AudioClip.Create("ClipName", samples.Length, channels, sampleRate, false);
                clip.SetData(samples, 0);
                return clip;
            }

            return null;
        }

        /// <summary>
        ///     Initializes Convai with given parameters.
        /// </summary>
        public void InitializeConvaiClient(string characterID, bool enableAudioRecorder, bool enableAudioPlayer, string actionConfig = "", string templateKeys = "")
        {
            Debug.Log("Character ID: " + activeConvaiNPC.characterID);

            if (string.IsNullOrEmpty(templateKeys))
            {
                initializeConvaiClient(APIKey, characterID, enableAudioRecorder, enableAudioPlayer, actionConfig);
            }
            else
            {
                initializeConvaiClient(APIKey, characterID, enableAudioRecorder, enableAudioPlayer, actionConfig, templateKeys);
            }
        }

        /// <summary>
        ///     Starts recording audio.
        /// </summary>
        public void StartRecordAudio()
        {
            OnPlayerSpeakingChanged?.Invoke(true);
            startAudioChunk();
        }

        /// <summary>
        ///     Stops recording audio.
        /// </summary>
        public void StopRecordAudio()
        {
            endAudioChunk();
            OnPlayerSpeakingChanged?.Invoke(false);
        }

        /// <summary>
        ///     Sends text data to Convai.
        /// </summary>
        public void SendTextData(string text)
        {
            sendTextRequest(text);
        }

        /// <summary>
        ///     Receives the text and sends it to the UI handler for displaying
        /// </summary>
        /// <param name="text"> The text response sent from jslib </param>
        public void OnUserResponseReceived(string text)
        {
            if (_convaiChatUIHandler == null) return;

            if (!string.IsNullOrWhiteSpace(text))
            {
                _convaiChatUIHandler.SendPlayerText(text);
            }
        }

        /// <summary>
        ///     Handles audio response received event.
        /// </summary>
        public void OnAudioResponseReceived(string audData)
        {
            try
            {
                AudioData audioData = _audioDataPool.GetObject();

                JsonUtility.FromJsonOverwrite(audData, audioData);


                if (audioData == null)
                {
                    Debug.LogError("Deserialization returned null");
                    return;
                }

                if (!audioData.hasVisemesData) activeConvaiNPC.AddAudioData(audioData);

                if (audioData.resText != _lastReceivedText)
                    if (!string.IsNullOrEmpty(audioData.resText))
                    {
                        _convaiChatUIHandler.SendCharacterText(activeConvaiNPC.characterName, audioData.resText);
                        _lastReceivedText = audioData.resText;
                    }

                _audioDataPool.ReleaseObject(audioData);
            }
            catch (Exception e)
            {
                Debug.LogError("Error Deserializing Audio Data: " + e.Message);
                throw;
            }
        }

        public void OnVisemeResponseReceived(string visemeData)
        {
            try
            {
                string[] dataValues = visemeData.Trim('[', ']').Split(',');

                Viseme viseme = new();

                int loopCount = Mathf.Min(dataValues.Length, 15);

                for (int i = 0; i < loopCount; i++)
                    if (dataValues[i] != "null" && float.TryParse(dataValues[i], NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                        viseme.SetFieldValue(i, value);

                VisemesData visemesData = _visemesDataPool.GetObject();

                visemesData.Visemes = viseme;
                ProcessVisemeData(visemesData);
            }
            catch (Exception e)
            {
                Debug.LogError("Error Deserializing Viseme Data: " + e.Message);
                throw;
            }
        }

        public void OnBTResponseReceived(string narrativeSectionID)
        {
            if (activeConvaiNPC.ConvaiNarrativeDesignManager != null)
            {
                Debug.Log("narrativeSectionID: " + narrativeSectionID);
                activeConvaiNPC.ConvaiNarrativeDesignManager.UpdateCurrentSection(narrativeSectionID);
            }
        }

        public void OnActionResponseReceived(string actionResponse)
        {
            if (activeConvaiNPC.ConvaiActionsHandler != null)
            {
                activeConvaiNPC.ConvaiActionsHandler.actionResponseList.Add(actionResponse);
            }
        }

        private void ProcessVisemeData(VisemesData visemesData)
        {
            if (visemesData.Visemes.Sil == -2)
            {
                if (activeConvaiNPC.ConvaiLipSync.FaceDataList == null) activeConvaiNPC.ConvaiLipSync.FaceDataList = new List<List<VisemesData>>();

                activeConvaiNPC.ConvaiLipSync.FaceDataList.Add(new List<VisemesData>());
            }
            else
            {
                if (activeConvaiNPC.ConvaiLipSync.FaceDataList == null || activeConvaiNPC.ConvaiLipSync.FaceDataList.Count == 0) return;

                activeConvaiNPC.ConvaiLipSync.FaceDataList[activeConvaiNPC.ConvaiLipSync.FaceDataList.Count - 1].Add(visemesData);
            }
        }

        /// <summary>
        ///     Sends feedback to Convai.
        /// </summary>
        /// <param name="characterID"></param>
        /// <param name="sessionID"></param>
        /// <param name="thumbsUp"></param>
        /// <param name="feedbackText"></param>
        public void SendFeedback(string characterID, string sessionID, bool thumbsUp, string feedbackText)
        {
            sendFeedback(characterID, sessionID, thumbsUp, feedbackText);
        }

        public void SendTriggerConfig(TriggerConfig triggerConfig)
        {
            string triggerName = triggerConfig.TriggerName;
            string triggerMessage = triggerConfig.TriggerMessage;

            Debug.Log("Sending Trigger Data: " + triggerName + " : " + triggerMessage);
            sendTriggerData(triggerName, triggerMessage);
        }

        public void UpdateActionConfig(ActionConfig actionConfig)
        {
            setActionConfig(JsonUtility.ToJson(actionConfig));
        }

        /// <summary>
        ///     Interrupts the character speech.
        /// </summary>
        public void InterruptCharacterSpeech()
        {
            interruptCharacter();
        }

        #region Events

        // Events to notify when the player starts or stops speaking
        public event Action<bool> OnPlayerSpeakingChanged;

        #endregion

        #region External Functions

        [DllImport("__Internal")]
        private static extern void startAudioChunk(); // Starts audio chunk

        [DllImport("__Internal")]
        private static extern void endAudioChunk(); // Ends audio chunk

        [DllImport("__Internal")]
        private static extern void initializeConvaiClient(string apiKey, string characterId, bool enableAudioRecorder, bool enableAudioPlayer, string actionConfig = "", string templateKeys = ""); // Initializes Convai client

        [DllImport("__Internal")]
        private static extern void initMicrophone(); // Initializes microphone

        [DllImport("__Internal")]
        private static extern void sendTextRequest(string request); // Sends text request

        [DllImport("__Internal")]
        private static extern void sendFeedback(string character_id, string session_id, bool thumbs_up, string feedback_text);

        [DllImport("__Internal")]
        private static extern void sendTriggerData(string triggerName, string triggerMessage);

        [DllImport("__Internal")]
        private static extern void setActionConfig(string actionConfig);

        [DllImport("__Internal")]
        private static extern void interruptCharacter();

        #endregion
    }
}

/// <summary>
///     Class to hold audio data.
/// </summary>
public class AudioData
{
    public byte[] audData; // Audio data in byte array
    public bool hasVisemesData; // Check for audio stream to have visemes data
    public bool isFirst; // Flag to check if it's the first audio data
    public string resText; // Response text
    public int sampleRate; // Sample rate of the audio data
}