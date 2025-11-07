using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.IO;
using System.Linq;

public static class InjectConfiguratorToScenes
{
    [MenuItem("Tools/Interactive/Inject VideoSceneConfigurator into all scenes")] 
    public static void Inject()
    {
        string[] scenePaths = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.AllDirectories);
        int added = 0;
        foreach (var path in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool has = Interactive.Util.SceneObjectFinder.FindFirst<VideoSceneConfigurator>(true) != null;
            if (!has)
            {
                var go = new GameObject("~SceneConfigurator");
                go.AddComponent<VideoSceneConfigurator>();
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                added++;
            }
        }
        Debug.Log($"Injected VideoSceneConfigurator into {added} scene(s).");
    }
}
