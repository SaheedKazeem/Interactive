using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Interactive.Config;

public static class VideoConfigGenerator
{
    [MenuItem("Tools/Interactive/Generate videos.json (merge)")]
    public static void Generate()
    {
        string scenesRoot = Path.Combine(Application.dataPath, "Scenes");
        var sceneFiles = Directory.GetFiles(scenesRoot, "*.unity", SearchOption.AllDirectories);
        var sceneNames = sceneFiles.Select(p => Path.GetFileNameWithoutExtension(p)).Distinct().OrderBy(n => n).ToList();

        string streaming = Path.Combine(Application.streamingAssetsPath, "videos.json");
        VideoProjectConfig cfg = new VideoProjectConfig();
        if (File.Exists(streaming))
        {
            try { cfg = JsonUtility.FromJson<VideoProjectConfig>(File.ReadAllText(streaming)); }
            catch { cfg = new VideoProjectConfig(); }
        }

        if (cfg.scenes == null) cfg.scenes = new List<SceneConfig>();
        foreach (var name in sceneNames)
        {
            if (!cfg.scenes.Any(s => s != null && string.Equals(s.name, name, System.StringComparison.OrdinalIgnoreCase)))
            {
                cfg.scenes.Add(new SceneConfig { name = name, windowsLocalPath = "" });
            }
        }

        // Pretty-ish JSON via JsonUtility is limited; write minified
        string json = JsonUtility.ToJson(cfg, true);
        Directory.CreateDirectory(Application.streamingAssetsPath);
        File.WriteAllText(streaming, json);
        AssetDatabase.Refresh();
        Debug.Log($"Generated/merged videos.json with {cfg.scenes.Count} scenes at {streaming}");
    }
}

