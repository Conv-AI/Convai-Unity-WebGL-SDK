using Convai.Scripts.Runtime.Addons;
using Convai.Scripts.Runtime.Attributes;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Convai.Scripts.Runtime.Utils
{
    /// <summary>
    ///     ScriptableObject that stores Convai API Key.
    ///     Allows the API key to be easily changed from the Unity editor and reduces risk of embedding keys directly into
    ///     script.
    ///     This object can be created from Unity Editor by going in top menu to Convai -> API Key
    /// </summary>
    [CreateAssetMenu(fileName = "ConvaiAPIKey", menuName = "Convai/API Key")]
    public class ConvaiAPIKeySetup : ScriptableObject
    {
        private static bool _hasShownDialog;
        [ReadOnly] public string APIKey;

        public static bool GetAPIKey(out string apiKey)
        {
            ConvaiAPIKeySetup keySetup = Resources.Load<ConvaiAPIKeySetup>("ConvaiAPIKey");
            if (keySetup == null)
            {
#if UNITY_EDITOR
                if (!_hasShownDialog)
                {
                    // display editor utility to show a dialog box saying no API Key found
                    EditorUtility.DisplayDialog("Error", "Convai API Key not found. Please add your API Key by going to Convai -> Convai Setup", "OK");
                    _hasShownDialog = true;
                }
#endif
                if (NotificationSystemHandler.Instance != null) NotificationSystemHandler.Instance.NotificationRequest(NotificationType.APIKeyNotFound);
                apiKey = string.Empty;
                return false;
            }

            apiKey = keySetup.APIKey;
            return true;
        }
    }
}