using UnityEditor;
using UnityEditor.PackageManager.Requests;
#if !READY_PLAYER_ME
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
#endif

namespace Convai.Scripts.Editor.CustomPackage
{
    [InitializeOnLoad]
    public class ReadyPlayerMeImporter
    {
        private static AddRequest _request;

        static ReadyPlayerMeImporter()
        {
#if !READY_PLAYER_ME
        Debug.Log("Ready Player Me is not installed, importing it");
        _request = Client.Add("https://github.com/readyplayerme/rpm-unity-sdk-core.git#f6ea3c4b0a8891b7c4c1d7b269cee545185549fb");
        EditorUtility.DisplayProgressBar("Importing Ready Player Me", "Importing.....", Random.Range(0,1f));
        EditorApplication.update += UnityEditorUpdateCallback;

#endif
        }

#if !READY_PLAYER_ME
        private static void UnityEditorUpdateCallback()
        {
            if (_request == null) return;
            if (!_request.IsCompleted) return;
            switch (_request.Status)
            {
                case StatusCode.Success:
                    Debug.Log($"Successfully installed: {_request.Result.name}");
                    break;
                case StatusCode.Failure:
                    Debug.Log($"Failure: {_request.Error.message}");
                    break;
            }
            EditorApplication.update -= UnityEditorUpdateCallback;
            EditorUtility.ClearProgressBar();
        }
#endif
    }
}