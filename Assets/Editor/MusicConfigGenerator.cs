using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Interactive.Audio;
using Interactive.Config;

public static class MusicConfigGenerator
{
    [MenuItem("Tools/Interactive/Generate music.json (from folder)")]
    public static void Generate()
    {
        string folder = EditorUtility.OpenFolderPanel("Select music root folder", "", "");
        if (string.IsNullOrEmpty(folder)) return;

        var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".mp3") || p.EndsWith(".ogg") || p.EndsWith(".wav"))
            .ToList();

        // Load scenes from videos.json to propose mapping
        string videosPath = Path.Combine(Application.streamingAssetsPath, "videos.json");
        var proj = File.Exists(videosPath) ? JsonUtility.FromJson<VideoProjectConfig>(File.ReadAllText(videosPath)) : new VideoProjectConfig();
        var sceneNames = (proj.scenes ?? new List<SceneConfig>()).Select(s => s.name).ToList();

        var music = new MusicProjectConfig { scenes = new List<SceneMusicConfig>() };
        foreach (var s in sceneNames)
        {
            music.scenes.Add(new SceneMusicConfig { name = s, cues = new List<MusicCue>() });
        }

        // Naive mapping: match files whose filename contains scene name (case-insensitive)
        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            var match = music.scenes.FirstOrDefault(sc => name.ToLowerInvariant().Contains(sc.name.ToLowerInvariant()));
            if (match == null)
            {
                // store unmatched under a special scene
                match = music.scenes.FirstOrDefault(sc => sc.name == "_UNASSIGNED_");
                if (match == null)
                {
                    match = new SceneMusicConfig { name = "_UNASSIGNED_", cues = new List<MusicCue>() };
                    music.scenes.Add(match);
                }
            }
            match.cues.Add(new MusicCue
            {
                file = file.Replace('\\', '/'),
                startOnSceneLoad = true,
                volume = 0.7f,
                fadeIn = 0.5f,
                fadeOut = 0.5f,
                loop = true
            });
        }

        string outPath = Path.Combine(Application.streamingAssetsPath, "music.json");
        Directory.CreateDirectory(Application.streamingAssetsPath);
        File.WriteAllText(outPath, JsonUtility.ToJson(music, true));
        AssetDatabase.Refresh();
        Debug.Log($"Generated music.json with {files.Count} files at {outPath}");
    }
}

public static class MusicConfigFromAssets
{
    [MenuItem("Tools/Interactive/Generate music.json (from Assets/Music)")]
    public static void GenerateFromAssetsMusic()
    {
        string folder = Path.Combine(Application.dataPath, "Music");
        if (!Directory.Exists(folder))
        {
            Debug.LogWarning($"No Assets/Music folder found at {folder}");
            return;
        }
        // Reuse logic by simulating selection
        var files = Directory.GetFiles(folder, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".mp3") || p.EndsWith(".ogg") || p.EndsWith(".wav"))
            .ToList();

        string videosPath = Path.Combine(Application.streamingAssetsPath, "videos.json");
        var proj = File.Exists(videosPath) ? JsonUtility.FromJson<VideoProjectConfig>(File.ReadAllText(videosPath)) : new VideoProjectConfig();
        var sceneNames = (proj.scenes ?? new List<SceneConfig>()).Select(s => s.name).ToList();

        var music = new MusicProjectConfig { scenes = new List<SceneMusicConfig>() };
        foreach (var s in sceneNames)
        {
            music.scenes.Add(new SceneMusicConfig { name = s, cues = new List<MusicCue>() });
        }

        foreach (var file in files)
        {
            string name = Path.GetFileNameWithoutExtension(file);
            var match = music.scenes.FirstOrDefault(sc => name.ToLowerInvariant().Contains(sc.name.ToLowerInvariant()));
            if (match == null)
            {
                match = music.scenes.FirstOrDefault(sc => sc.name == "_UNASSIGNED_");
                if (match == null)
                {
                    match = new SceneMusicConfig { name = "_UNASSIGNED_", cues = new List<MusicCue>() };
                    music.scenes.Add(match);
                }
            }
            match.cues.Add(new MusicCue
            {
                file = file.Replace('\\', '/'),
                startOnSceneLoad = true,
                volume = 0.7f,
                fadeIn = 0.5f,
                fadeOut = 0.5f,
                loop = true
            });
        }

        string outPath = Path.Combine(Application.streamingAssetsPath, "music.json");
        Directory.CreateDirectory(Application.streamingAssetsPath);
        File.WriteAllText(outPath, JsonUtility.ToJson(music, true));
        AssetDatabase.Refresh();
        Debug.Log($"Generated music.json from Assets/Music at {outPath}");
    }
}
