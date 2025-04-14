using Convai.Scripts.Runtime.Core;
using Convai.Scripts.Runtime.Features;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.NPC
{
    public class ConvaiNPCComponentSettingsWindow : EditorWindow
    {
        private const float WINDOW_WIDTH = 300f;
        private const float WINDOW_HEIGHT = 180f;
        private const float LABEL_WIDTH = 200f;
        private const float BUTTON_HEIGHT = 40f;

        private ConvaiNPC _convaiNPC;

        [MenuItem("Convai/NPC Component Settings")]
        public static void ShowWindow()
        {
            GetWindow<ConvaiNPCComponentSettingsWindow>("Convai NPC Component Settings");
        }

        private void OnEnable()
        {
            minSize = maxSize = new Vector2(WINDOW_WIDTH, WINDOW_HEIGHT);
        }

        private void OnGUI()
        {
            if (_convaiNPC == null)
            {
                EditorGUILayout.HelpBox("No ConvaiNPC selected", MessageType.Warning);
                return;
            }

            DrawComponentToggles();
            GUILayout.Space(10);
            DrawApplyButton();
        }

        private void OnFocus() => RefreshComponentStates();

        public static void Open(ConvaiNPC convaiNPC)
        {
            var window = GetWindow<ConvaiNPCComponentSettingsWindow>();
            window._convaiNPC = convaiNPC;
            window.RefreshComponentStates();
            window.Show();
        }

        private void DrawComponentToggles()
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUIUtility.labelWidth = LABEL_WIDTH;

            _convaiNPC.enableActionsHandler = EditorGUILayout.Toggle(new GUIContent("NPC Actions", "Decides if Actions Handler is included"), _convaiNPC.enableActionsHandler);
            _convaiNPC.enableLipSync= EditorGUILayout.Toggle(new GUIContent("Lip Sync", "Decides if Lip Sync is enabled"), _convaiNPC.LipSync);
            _convaiNPC.enableHeadEyeTracking= EditorGUILayout.Toggle(new GUIContent("Head & Eye Tracking", "Decides if Head & Eye tracking is enabled"), _convaiNPC.enableHeadEyeTracking);
            _convaiNPC.enableEyeBlinking= EditorGUILayout.Toggle(new GUIContent("Eye Blinking", "Decides if Eye Blinking is enabled"), _convaiNPC.enableEyeBlinking);
            _convaiNPC.enableNarrativeDesignManager= EditorGUILayout.Toggle(new GUIContent("Narrative Design Manager", "Decides if Narrative Design Manager is enabled"),
                _convaiNPC.NarrativeDesignManager);
            _convaiNPC.enableNarrativeDesignKeyController =
                EditorGUILayout.Toggle(new GUIContent("Narrative Design Keys", "Adds handler for Narrative Design Keys for this character"),
                    _convaiNPC.NarrativeDesignKeyController);

            EditorGUILayout.EndVertical();
        }

        private void DrawApplyButton()
        {
            if (GUILayout.Button("Apply Changes", GUILayout.Height(BUTTON_HEIGHT)))
            {
                ApplyChanges();
                EditorUtility.SetDirty(_convaiNPC);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Close();
            }
        }

        private void RefreshComponentStates()
        {
            if (_convaiNPC == null) return;

            _convaiNPC.enableActionsHandler = _convaiNPC.GetComponent<ConvaiActionsHandler>() is not null;
            _convaiNPC.enableLipSync = _convaiNPC.GetComponent<ConvaiLipSync>() != null;
            _convaiNPC.enableHeadEyeTracking= _convaiNPC.GetComponent<ConvaiHeadTracking>() != null;
            _convaiNPC.enableEyeBlinking = _convaiNPC.GetComponent<ConvaiBlinkingHandler>() != null;
            _convaiNPC.enableNarrativeDesignManager = _convaiNPC.GetComponent<NarrativeDesignManager>() != null;
            _convaiNPC.enableNarrativeDesignKeyController = _convaiNPC.GetComponent<NarrativeDesignKeyController>() is not null;
            Repaint();
        }

        private void ApplyChanges()
        {
            if (!EditorUtility.DisplayDialog("Confirm Apply Changes", "Do you want to apply the following changes?", "Yes", "No"))
                return;

            ApplyComponent<ConvaiActionsHandler>(_convaiNPC.enableActionsHandler);
            ApplyComponent<ConvaiLipSync>(_convaiNPC.LipSync);
            ApplyComponent<ConvaiHeadTracking>(_convaiNPC.enableHeadEyeTracking);
            ApplyComponent<ConvaiBlinkingHandler>(_convaiNPC.enableEyeBlinking);
            ApplyComponent<NarrativeDesignManager>(_convaiNPC.enableNarrativeDesignManager);
            ApplyComponent<NarrativeDesignKeyController>(_convaiNPC.enableNarrativeDesignKeyController);

        }

        private void ApplyComponent<T>(bool includeComponent) where T : Component
        {
            var component = _convaiNPC.GetComponent<T>();

            if (includeComponent)
            {
                if (component == null)
                {
                    component = _convaiNPC.gameObject.AddComponentSafe<T>();
                }
            }
            else if (component != null)
            {
                DestroyImmediate(component);
            }
        }
    }
}
