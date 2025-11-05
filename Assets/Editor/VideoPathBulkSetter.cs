using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Interactive.Config;

public static class VideoPathBulkSetter
{
    [MenuItem("Tools/Interactive/Bulk set video paths from folder…")]
    public static void BulkSet()
    {
        string folder = EditorUtility.OpenFolderPanel("Select folder containing MP4s", "", "");
        if (string.IsNullOrEmpty(folder)) return;
        var files = Directory.GetFiles(folder, "*.mp4", SearchOption.AllDirectories);
        if (files.Length == 0)
        {
            Debug.LogWarning("No .mp4 files found.");
            return;
        }

        string path = Path.Combine(Application.streamingAssetsPath, "videos.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"videos.json not found at {path}. Generate it first (Tools/Interactive/Generate videos.json)." );
            return;
        }

        var cfg = JsonUtility.FromJson<VideoProjectConfig>(File.ReadAllText(path));
        foreach (var sc in cfg.scenes)
        {
            var match = files.FirstOrDefault(f => IsMatch(f, sc.name));
            if (match != null)
            {
                sc.windowsLocalPath = match.Replace('\\', '/');
            }
        }

        File.WriteAllText(path, JsonUtility.ToJson(cfg, true));
        AssetDatabase.Refresh();
        Debug.Log($"Updated windowsLocalPath for matching scenes using folder: {folder}");
    }

    private static bool IsMatch(string filePath, string sceneName)
    {
        string fn = Path.GetFileNameWithoutExtension(filePath);
        return Normalize(fn).Contains(Normalize(sceneName));
    }

    private static string Normalize(string s)
    {
        s = s.ToLowerInvariant();
        var arr = s.Where(char.IsLetterOrDigit).ToArray();
        return new string(arr);
    }
}

