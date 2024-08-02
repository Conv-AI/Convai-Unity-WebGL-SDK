#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Convai.Scripts.Editor.UI;
using Convai.Scripts.Runtime.Utils;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Convai.Scripts.Editor.Setup
{
    public class ConvaiSetup : EditorWindow
    {
        private const string API_URL = "https://api.convai.com/user/referral-source-status";
        private VisualElement _page1;
        private VisualElement _page2;
        private VisualElement _root;

        public void CreateGUI()
        {
            _root = rootVisualElement;
            _page1 = new ScrollView();
            _page2 = new ScrollView();

            CreatePage1();
            CreatePage2();

            _root.Add(_page1);
        }

        private void CreatePage1()
        {
            _root.Add(new Label(""));

            Image convaiLogoImage = new()
            {
                image = AssetDatabase.LoadAssetAtPath<Texture>(ConvaiImagesDirectory.CONVAI_LOGO_PATH),
                style =
                {
                    height = 100,
                    paddingBottom = 10,
                    paddingTop = 10,
                    paddingRight = 10,
                    paddingLeft = 10
                }
            };

            _root.Add(convaiLogoImage);

            Label convaiSetupLabel = new("Enter your API Key:")
            {
                style =
                {
                    fontSize = 16
                }
            };

            TextField apiKeyTextField = new("", -1, false, true, '*');

            Button beginButton = new(() => ClickEvent(apiKeyTextField.text))
            {
                text = "Begin!",
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    alignSelf = Align.Center,
                    paddingBottom = 10,
                    paddingLeft = 30,
                    paddingRight = 30,
                    paddingTop = 10
                }
            };

            Button docsLink = new(() => { Application.OpenURL("https://docs.convai.com/api-docs/plugins-and-integrations/unity-plugin/setting-up-unity-plugin"); })
            {
                text = "How do I find my API key?",
                style =
                {
                    alignSelf = Align.Center,
                    paddingBottom = 5,
                    paddingLeft = 50,
                    paddingRight = 50,
                    paddingTop = 5
                }
            };

            _page1.Add(convaiSetupLabel);
            _page1.Add(new Label("\n"));
            _page1.Add(apiKeyTextField);
            _page1.Add(new Label("\n"));
            _page1.Add(beginButton);
            _page1.Add(new Label("\n"));
            _page1.Add(docsLink);

            _page1.style.marginBottom = 20;
            _page1.style.marginLeft = 20;
            _page1.style.marginRight = 20;
            _page1.style.marginTop = 20;
        }

        private void CreatePage2()
        {
            Label attributionSourceLabel = new("[Step 2/2] Where did you discover Convai?")
            {
                style =
                {
                    fontSize = 14,
                    unityFontStyleAndWeight = FontStyle.Bold
                }
            };

            List<string> attributionSourceOptions = new()
            {
                "Search Engine (Google, Bing, etc.)",
                "Youtube",
                "Social Media (Facebook, Instagram, TikTok, etc.)",
                "Friend Referral",
                "Unity Asset Store",
                "Others"
            };

            TextField otherOptionTextField = new();

            ToolbarMenu toolbarMenu = new() { text = "Click here to select option..." };

            foreach (string choice in attributionSourceOptions)
                toolbarMenu.menu.AppendAction(choice,
                    action =>
                    {
                        _ = choice;
                        toolbarMenu.text = choice;
                    });

            toolbarMenu.style.paddingBottom = 10;
            toolbarMenu.style.paddingLeft = 30;
            toolbarMenu.style.paddingRight = 30;
            toolbarMenu.style.paddingTop = 10;

            Button continueButton = new(() => ContinueEvent(toolbarMenu.text, otherOptionTextField.text))
            {
                text = "Continue",
                style =
                {
                    fontSize = 16,
                    unityFontStyleAndWeight = FontStyle.Bold,
                    alignSelf = Align.Center,
                    paddingBottom = 5,
                    paddingLeft = 30,
                    paddingRight = 30,
                    paddingTop = 5
                }
            };

            _page2.Add(new Label("\n"));
            _page2.Add(attributionSourceLabel);
            _page2.Add(new Label("\n"));

            _page2.Add(toolbarMenu);
            _page2.Add(new Label("\nIf selected Others above, please specify from where: "));

            _page2.Add(otherOptionTextField);
            _page2.Add(new Label("\n"));
            _page2.Add(continueButton);

            _page2.style.marginBottom = 20;
            _page2.style.marginLeft = 20;
            _page2.style.marginRight = 20;
            _page2.style.marginTop = 20;
        }

        private async void ClickEvent(string apiKey)
        {
            (bool isSuccessful, bool shouldShowPage2) result = await BeginButtonTask(apiKey);

            if (result.isSuccessful)
            {
                if (result.shouldShowPage2)
                {
                    _root.Clear();
                    _root.Add(_page2);
                }
                else
                {
                    EditorUtility.DisplayDialog("Success", "API Key loaded successfully!", "OK");
                    Close();
                }
            }
            // If not successful, do nothing, keeping the window open
        }

        private async void ContinueEvent(string selectedOption, string otherOption)
        {
            List<string> attributionSourceOptions = new()
            {
                "Search Engine (Google, Bing, etc.)",
                "Youtube",
                "Social Media (Facebook, Instagram, TikTok, etc.)",
                "Friend Referral",
                "Unity Asset Store",
                "Others"
            };

            int currentChoiceIndex = attributionSourceOptions.IndexOf(selectedOption);

            if (currentChoiceIndex < 0)
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid referral source!", "OK");
                return;
            }

            UpdateSource updateSource = new(attributionSourceOptions[currentChoiceIndex]);

            if (attributionSourceOptions[currentChoiceIndex] == "Others") updateSource.ReferralSource = otherOption;

            ConvaiAPIKeySetup apiKeyObject = AssetDatabase.LoadAssetAtPath<ConvaiAPIKeySetup>("Assets/Resources/ConvaiAPIKey.Asset");

            await SendReferralRequest("https://api.convai.com/user/update-source", JsonConvert.SerializeObject(updateSource), apiKeyObject.APIKey);

            EditorUtility.DisplayDialog("Success", "Setup completed successfully!", "OK");
            Close();
        }

        [MenuItem("Convai/Convai Setup", false, 1)]
        public static void ShowConvaiSetupWindow()
        {
            GetWindow<ConvaiSetup>();
        }

        [MenuItem("Convai/Documentation")]
        public static void OpenDocumentation()
        {
            Application.OpenURL("https://docs.convai.com/plugins-and-integrations/unity-plugin");
        }

        private static async Task<string> CheckReferralStatus(string url, string apiKey)
        {
            // Create a new HttpWebRequest object
            WebRequest request = WebRequest.Create(url);
            request.Method = "post";

            // Set the request headers
            request.ContentType = "application/json";

            string bodyJsonString = "{}";

            // Convert the json string to bytes
            byte[] jsonBytes = Encoding.UTF8.GetBytes(bodyJsonString);

            request.Headers.Add("CONVAI-API-KEY", apiKey);

            // Write the data to the request stream
            await using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }

            // Get the response from the server
            try
            {
                using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                ReferralSourceStatus referralStatus = null;
                await using (Stream streamResponse = response.GetResponseStream())

                {
                    if (streamResponse != null)
                    {
                        using StreamReader reader = new(streamResponse);
                        string responseContent = await reader.ReadToEndAsync();

                        referralStatus = JsonConvert.DeserializeObject<ReferralSourceStatus>(responseContent);
                    }
                }

                if (referralStatus != null) return referralStatus.ReferralSourceStatusProperty;
            }
            catch (WebException e)
            {
                Debug.LogError(e.Message + "\nPlease check if API Key is correct.");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return null;
            }

            return null;
        }

        private static async Task SendReferralRequest(string url, string bodyJsonString, string apiKey)
        {
            // Create a new HttpWebRequest object
            WebRequest request = WebRequest.Create(url);
            request.Method = "post";

            // Set the request headers
            request.ContentType = "application/json";

            // Convert the json string to bytes
            byte[] jsonBytes = Encoding.UTF8.GetBytes(bodyJsonString);

            request.Headers.Add("CONVAI-API-KEY", apiKey);

            // Write the data to the request stream
            await using (Stream requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            }

            // Get the response from the server
            try
            {
                using HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();
                await using (Stream streamResponse = response.GetResponseStream())
                {
                    if (streamResponse != null)
                    {
                        using StreamReader reader = new(streamResponse);
                        await reader.ReadToEndAsync();
                    }
                }

                if ((int)response.StatusCode == 200)
                    Debug.Log("Referral sent successfully.");
            }
            catch (WebException e)
            {
                Debug.LogError(e.Message + "\nPlease check if API Key is correct.");
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private async Task<(bool isSuccessful, bool shouldShowPage2)> BeginButtonTask(string apiKey)
        {
            ConvaiAPIKeySetup aPIKeySetup = CreateInstance<ConvaiAPIKeySetup>();

            aPIKeySetup.APIKey = apiKey;

            if (string.IsNullOrEmpty(apiKey))
            {
                EditorUtility.DisplayDialog("Error", "Please enter a valid API Key.", "OK");
                return (false, false);
            }

            string referralStatus = await CheckReferralStatus(API_URL, apiKey);

            if (referralStatus == null)
            {
                EditorUtility.DisplayDialog("Error", "Something went wrong. Please check your API Key. Contact support@convai.com for more help.", "OK");
                return (false, false);
            }

            CreateOrUpdateAPIKeyAsset(aPIKeySetup);

            if (referralStatus.Trim().ToLower() == "undefined" || referralStatus.Trim().ToLower() == "")
            {
                EditorUtility.DisplayDialog("Success", "[Step 1/2] API Key loaded successfully!", "OK");
                return (true, true);
            }

            return (true, false);
        }

        private void CreateOrUpdateAPIKeyAsset(ConvaiAPIKeySetup aPIKeySetup)
        {
            string assetPath = "Assets/Resources/ConvaiAPIKey.asset";

            if (!File.Exists(assetPath))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                    AssetDatabase.CreateFolder("Assets", "Resources");

                AssetDatabase.CreateAsset(aPIKeySetup, assetPath);
            }
            else
            {
                AssetDatabase.DeleteAsset(assetPath);
                AssetDatabase.CreateAsset(aPIKeySetup, assetPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public class UpdateSource
        {
            [JsonProperty("referral_source")] public string ReferralSource;

            public UpdateSource(string referralSource)
            {
                ReferralSource = referralSource;
            }
        }

        public class ReferralSourceStatus
        {
            [JsonProperty("referral_source_status")]
            public string ReferralSourceStatusProperty;

            [JsonProperty("status")] public string Status;
        }
    }
}
#endif