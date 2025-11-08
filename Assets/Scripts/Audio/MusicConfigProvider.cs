using System;
using System.IO;
using UnityEngine;

namespace Interactive.Audio
{
    /// <summary>
    /// Shared loader/cache for music.json so multiple systems (MusicDirector, SfxPlayer) stay in sync.
    /// </summary>
    public static class MusicConfigProvider
    {
        private static MusicProjectConfig cached;

        public static MusicProjectConfig Load()
        {
            if (cached != null) return cached;
            try
            {
                string path = Path.Combine(Application.persistentDataPath, "music.json");
                if (!File.Exists(path))
                    path = Path.Combine(Application.streamingAssetsPath, "music.json");
                if (File.Exists(path))
                {
                    cached = JsonUtility.FromJson<MusicProjectConfig>(File.ReadAllText(path));
                    if (cached != null) return cached;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"MusicConfigProvider: failed to load music.json: {e.Message}");
            }
            cached = new MusicProjectConfig();
            return cached;
        }

        public static void Invalidate()
        {
            cached = null;
        }
    }
}
