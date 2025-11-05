using System;
using System.IO;
using UnityEngine;

namespace Interactive.Config
{
    public static class VideoSceneConfigLoader
    {
        private static VideoProjectConfig cached;
        private const string FileName = "videos.json";

        public static VideoProjectConfig Load()
        {
            if (cached != null) return cached;

            // Priority: persistentDataPath > StreamingAssets
            string persistent = Path.Combine(Application.persistentDataPath, FileName);
            if (File.Exists(persistent))
            {
                try
                {
                    var json = File.ReadAllText(persistent);
                    cached = JsonUtility.FromJson<VideoProjectConfig>(json);
                    Debug.Log($"Loaded video config from: {persistent}");
                    if (cached != null) return cached;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed parsing {persistent}: {e.Message}");
                }
            }

            string streaming = Path.Combine(Application.streamingAssetsPath, FileName);
            if (File.Exists(streaming))
            {
                try
                {
                    var json = File.ReadAllText(streaming);
                    cached = JsonUtility.FromJson<VideoProjectConfig>(json);
                    Debug.Log($"Loaded video config from: {streaming}");
                    if (cached != null) return cached;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed parsing {streaming}: {e.Message}");
                }
            }

            // Fallback to empty config
            cached = new VideoProjectConfig();
            return cached;
        }
    }
}
