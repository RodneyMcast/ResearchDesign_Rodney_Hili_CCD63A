using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RestartGame : MonoBehaviour
{
    private const string RestartActionId = "ui_restart_pressed";

    [Tooltip("If enabled, Reset Level loads the selected scene below. If disabled, it reloads the current active scene.")]
    [SerializeField] private bool useSelectedScene = false;

#if UNITY_EDITOR
    [Tooltip("Optional: drag a Scene asset here. If none is assigned, the current scene will restart.")]
    [SerializeField] private SceneAsset sceneToLoadAsset;
#endif

    [Tooltip("Runtime scene name used for loading. This is auto-filled from Scene To Load Asset in the editor.")]
    [SerializeField] private string sceneToLoad;

    public void ResetLevel()
    {
        string targetScene = ResolveTargetScene();

        GameManager.Instance?.RecordAction(RestartActionId);
        GameManager.Instance?.RecordAction($"telemetry_restart_target_scene:{targetScene}");

        SceneManager.LoadScene(targetScene);
    }

    private string ResolveTargetScene()
    {
        if (!useSelectedScene)
            return SceneManager.GetActiveScene().name;

#if UNITY_EDITOR
        if (sceneToLoadAsset != null)
        {
            string scenePath = AssetDatabase.GetAssetPath(sceneToLoadAsset);
            string sceneNameFromAsset = System.IO.Path.GetFileNameWithoutExtension(scenePath);
            if (!string.IsNullOrWhiteSpace(sceneNameFromAsset))
                return sceneNameFromAsset;
        }
#endif

        if (!string.IsNullOrWhiteSpace(sceneToLoad))
            return sceneToLoad;

        return SceneManager.GetActiveScene().name;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (sceneToLoadAsset == null)
        {
            sceneToLoad = string.Empty;
            return;
        }

        string scenePath = AssetDatabase.GetAssetPath(sceneToLoadAsset);
        sceneToLoad = System.IO.Path.GetFileNameWithoutExtension(scenePath);
    }
#endif
}
