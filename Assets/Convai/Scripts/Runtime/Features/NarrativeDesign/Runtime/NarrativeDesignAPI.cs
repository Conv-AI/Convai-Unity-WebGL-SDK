using System.Collections;
using System.Collections.Generic;
using Convai.Scripts.Runtime.LoggerSystem;
using Convai.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Convai.Scripts.Runtime.Features
{
    /// <summary>
    ///     API client for the Narrative Design API.
    /// </summary>
    public class NarrativeDesignAPI
    {
        private const string BASE_URL = "https://api.convai.com/character/narrative/";

        public IEnumerator CreateSectionCoroutine(string characterId, string objective, string sectionName, System.Action<string> callback, string behaviorTreeCode = null, string btConstants = null)
        {
            if (string.IsNullOrEmpty(characterId) || string.IsNullOrEmpty(sectionName))
            {
                callback?.Invoke("Invalid character ID or section name.");
                yield break; // Early exit on validation failure
            }

            string endpoint = "create-section";
            var jsonData = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "character_id", characterId },
                { "objective", objective },
                { "section_name", sectionName },
                { "behavior_tree_code", behaviorTreeCode },
                { "bt_constants", btConstants }
            });
            yield return SendPostRequestCoroutine(endpoint, jsonData, callback);
        }

        public IEnumerator GetSectionCoroutine(string characterId, string sectionId, System.Action<string> callback)
        {
            string endpoint = "get-section";
            var jsonData = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "character_id", characterId },
                { "section_id", sectionId }
            });
            yield return SendPostRequestCoroutine(endpoint, jsonData, callback);
        }

        public IEnumerator ListSectionsCoroutine(string characterId, System.Action<string> callback)
        {
            string endpoint = "list-sections";
            var jsonData = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "character_id", characterId }
            });
            yield return SendPostRequestCoroutine(endpoint, jsonData, callback);
        }

        public IEnumerator CreateTriggerCoroutine(string characterId, string triggerName, System.Action<string> callback, string triggerMessage = null, string destinationSection = null)
        {
            string endpoint = "create-trigger";

            var jsonData = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "character_id", characterId },
                { "trigger_message", triggerMessage },
                { "destination_section", destinationSection }
            });

            yield return SendPostRequestCoroutine(endpoint, jsonData, callback);
        }

        public IEnumerator GetTriggerCoroutine(string characterId, string triggerId, System.Action<string> callback)
        {
            string endpoint = "get-trigger";
            var jsonData = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "character_id", characterId },
                { "trigger_id", triggerId }
            });
            yield return SendPostRequestCoroutine(endpoint, jsonData, callback);
        }

        public IEnumerator GetTriggerListCoroutine(string characterId, System.Action<string> callback)
        {
            string endpoint = "list-triggers";
            var jsonData = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                { "character_id", characterId }
            });
            yield return SendPostRequestCoroutine(endpoint, jsonData, callback);
        }

        private IEnumerator SendPostRequestCoroutine(string endpoint, string jsonData, System.Action<string> callback)
        {
            using (UnityWebRequest request = new UnityWebRequest(BASE_URL + endpoint, "POST"))
            {
                byte[] bodyRaw = new System.Text.UTF8Encoding().GetBytes(jsonData);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();

                // Set headers
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("CONVAI-API-KEY", ConvaiAPIKeySetup.GetAPIKey(out string apiKey) ? apiKey : "");

                // Send the request and wait for it to complete
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    string errorMsg = $"Request to {endpoint} failed: {request.error}";
                    ConvaiLogger.Exception(errorMsg, ConvaiLogger.LogCategory.Character);
                    callback?.Invoke(errorMsg);
                }
                else
                {
                    // Get the response
                    string responseText = request.downloadHandler.text;
                    callback?.Invoke(responseText);
                }
            }
        }
    }
}