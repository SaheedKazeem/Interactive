using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Interactive.Config;

/// <summary>
/// Scans all scenes for ButtonClick components and writes their timings
/// into StreamingAssets/videos.json as TimedButtonConfig entries.
/// - Preserves existing scene windowsLocalPath entries.
/// - Writes targetScene = "" so your existing ButtonClick onClick remains.
/// - Does NOT modify or save scenes.
/// </summary>
public static class ButtonsFromScenes
{
    [MenuItem("Tools/Interactive/Extract buttons to videos.json (from scenes)")]
    public static void Extract()
    {
        string scenesRoot = Path.Combine(Application.dataPath, "Scenes");
        if (!Directory.Exists(scenesRoot))
        {
            Debug.LogWarning($"Scenes folder not found at {scenesRoot}");
            return;
        }

        string cfgPath = Path.Combine(Application.streamingAssetsPath, "videos.json");
        VideoProjectConfig cfg = new VideoProjectConfig();
        if (File.Exists(cfgPath))
        {
            try { cfg = JsonUtility.FromJson<VideoProjectConfig>(File.ReadAllText(cfgPath)) ?? new VideoProjectConfig(); }
            catch { cfg = new VideoProjectConfig(); }
        }
        if (cfg.scenes == null) cfg.scenes = new List<SceneConfig>();

        var sceneFiles = Directory.GetFiles(scenesRoot, "*.unity", SearchOption.AllDirectories)
            .OrderBy(p => p).ToList();

        int totalButtons = 0;
        foreach (var path in sceneFiles)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            string sceneName = Path.GetFileNameWithoutExtension(path);

            // Find or create config entry
            var sc = cfg.scenes.FirstOrDefault(s => s != null && string.Equals(s.name, sceneName, StringComparison.OrdinalIgnoreCase));
            if (sc == null)
            {
                sc = new SceneConfig { name = sceneName, windowsLocalPath = "", buttons = new List<TimedButtonConfig>() };
                cfg.scenes.Add(sc);
            }

            // Extract ButtonClick components via reflection to read private serialized fields
            var components = GameObject.FindObjectsOfType<MonoBehaviour>(true)
                .Where(m => m != null && m.GetType().Name == "ButtonClick")
                .ToArray();

            var list = new List<TimedButtonConfig>();
            foreach (var comp in components)
            {
                try
                {
                    var t = comp.GetType();
                    var fiButton = t.GetField("RefToButton", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    var fiTime = t.GetField("buttonAppearTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
                    var go = fiButton?.GetValue(comp) as GameObject;
                    var time = fiTime != null ? Convert.ToSingle(fiTime.GetValue(comp)) : 0f;
                    if (go == null) continue;
                    list.Add(new TimedButtonConfig
                    {
                        name = go.name,
                        appearTime = Mathf.Max(0f, time),
                        targetScene = "", // keep existing ButtonClick onClick
                        hideAfterSeconds = -1f,
                        fadeIn = 0.35f,
                        scaleFrom = 0.95f,
                        pulse = false
                    });
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Extract buttons: error reading a ButtonClick in scene '{sceneName}': {e.Message}");
                }
            }

            // Sort by appear time for cleanliness
            list = list.OrderBy(b => b.appearTime).ToList();
            sc.buttons = list; // overwrite with extracted values
            totalButtons += list.Count;
        }

        // Save config
        Directory.CreateDirectory(Application.streamingAssetsPath);
        File.WriteAllText(cfgPath, JsonUtility.ToJson(cfg, true));
        AssetDatabase.Refresh();
        Debug.Log($"Extracted {totalButtons} button timing(s) into {cfgPath} from {sceneFiles.Count} scene(s).");
    }
}

