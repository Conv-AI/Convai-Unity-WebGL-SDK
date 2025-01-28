using Convai.Scripts.Runtime.Features;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace Convai.Scripts.Editor.NarrativeDesign
{
    [CustomEditor(typeof(NarrativeDesignTrigger))]
    public class NarrativeDesignTriggerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            NarrativeDesignTrigger narrativeDesignTrigger = (NarrativeDesignTrigger)target;

            if (GUILayout.Button("Update Triggers"))
            {
                if (narrativeDesignTrigger.convaiNPC != null)
                {
                    NarrativeDesignManager manager = narrativeDesignTrigger.convaiNPC.GetComponent<NarrativeDesignManager>();
                    if (manager != null)
                    {
                        EditorCoroutineUtility.StartCoroutine(UpdateTriggers(manager, narrativeDesignTrigger), this);
                    }
                }
            }

            GUILayout.Space(10);
            DrawDefaultInspector();
            if (narrativeDesignTrigger.availableTriggers is { Count: > 0 })
                narrativeDesignTrigger.selectedTriggerIndex =
                    EditorGUILayout.Popup("Trigger", narrativeDesignTrigger.selectedTriggerIndex, narrativeDesignTrigger.availableTriggers.ToArray());
        }

        private System.Collections.IEnumerator UpdateTriggers(NarrativeDesignManager manager, NarrativeDesignTrigger narrativeDesignTrigger)
        {
            yield return EditorCoroutineUtility.StartCoroutine(manager.UpdateTriggerListCoroutine(), this);
            narrativeDesignTrigger.UpdateAvailableTriggers();
            EditorUtility.SetDirty(narrativeDesignTrigger);
        }
    }
}