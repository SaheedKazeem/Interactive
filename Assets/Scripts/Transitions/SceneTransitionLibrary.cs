using System;
using System.Collections.Generic;
using System.IO;
using DG.Tweening;
using UnityEngine;

namespace Interactive.Transitions
{
    [Serializable]
    public class SceneTransitionDatabase
    {
        public SceneTransitionDefaults defaults = new SceneTransitionDefaults();
        public List<SceneTransitionEntry> scenes = new List<SceneTransitionEntry>();
    }

    [Serializable]
    public class SceneTransitionDefaults
    {
        public float fadeOut = 0.35f;
        public float fadeIn = 0.35f;
        public string color = "#000000";
        public string easeOut = nameof(Ease.Linear);
        public string easeIn = nameof(Ease.Linear);
        public float holdAfterLoad = 0f;
    }

    [Serializable]
    public class SceneTransitionEntry
    {
        public string name;
        public string style;
        public float fadeOut = -1f;
        public float fadeIn = -1f;
        public string color;
        public string easeOut;
        public string easeIn;
        public float holdAfterLoad = -1f;
        public string sourceVideo;
        public float durationSeconds;
        public float avgFps;
        public float bitrate;
    }

    public class SceneTransitionOptions
    {
        public float fadeOut = 0.35f;
        public float fadeIn = 0.35f;
        public Color color = Color.black;
        public Ease fadeOutEase = Ease.Linear;
        public Ease fadeInEase = Ease.Linear;
        public float holdAfterLoad = 0f;
        public string style;

        public SceneTransitionOptions Clone() => (SceneTransitionOptions)MemberwiseClone();

        public static SceneTransitionOptions Default => new SceneTransitionOptions();

        public static SceneTransitionOptions FromDurations(float fadeOut, float fadeIn)
        {
            var opts = new SceneTransitionOptions();
            if (fadeOut > 0f) opts.fadeOut = fadeOut;
            opts.fadeIn = fadeIn > 0f ? fadeIn : opts.fadeOut;
            return opts;
        }

        public static SceneTransitionOptions FromDefaults(SceneTransitionDefaults defaults)
        {
            var opts = new SceneTransitionOptions();
            if (defaults == null) return opts;
            opts.Apply(defaults);
            return opts;
        }

        public void Apply(SceneTransitionDefaults data)
        {
            if (data == null) return;
            if (data.fadeOut > 0f) fadeOut = data.fadeOut;
            if (data.fadeIn > 0f) fadeIn = data.fadeIn;
            if (!string.IsNullOrWhiteSpace(data.color) && ColorUtility.TryParseHtmlString(data.color, out var c))
                color = c;
            if (!string.IsNullOrWhiteSpace(data.easeOut) && Enum.TryParse(data.easeOut, out Ease outEase))
                fadeOutEase = outEase;
            if (!string.IsNullOrWhiteSpace(data.easeIn) && Enum.TryParse(data.easeIn, out Ease inEase))
                fadeInEase = inEase;
            if (data.holdAfterLoad >= 0f) holdAfterLoad = data.holdAfterLoad;
        }

        public void Apply(SceneTransitionEntry entry)
        {
            if (entry == null) return;
            if (entry.fadeOut > 0f) fadeOut = entry.fadeOut;
            if (entry.fadeIn > 0f) fadeIn = entry.fadeIn;
            if (!string.IsNullOrWhiteSpace(entry.color) && ColorUtility.TryParseHtmlString(entry.color, out var c))
                color = c;
            if (!string.IsNullOrWhiteSpace(entry.easeOut) && Enum.TryParse(entry.easeOut, out Ease outEase))
                fadeOutEase = outEase;
            if (!string.IsNullOrWhiteSpace(entry.easeIn) && Enum.TryParse(entry.easeIn, out Ease inEase))
                fadeInEase = inEase;
            if (entry.holdAfterLoad >= 0f) holdAfterLoad = entry.holdAfterLoad;
            if (!string.IsNullOrEmpty(entry.style)) style = entry.style;
        }

        public void Apply(SceneTransitionOptions overrides)
        {
            if (overrides == null) return;
            if (overrides.fadeOut > 0f) fadeOut = overrides.fadeOut;
            if (overrides.fadeIn > 0f) fadeIn = overrides.fadeIn;
            color = overrides.color;
            fadeOutEase = overrides.fadeOutEase;
            fadeInEase = overrides.fadeInEase;
            if (overrides.holdAfterLoad >= 0f) holdAfterLoad = overrides.holdAfterLoad;
            if (!string.IsNullOrEmpty(overrides.style)) style = overrides.style;
        }
    }

    public static class SceneTransitionLibrary
    {
        private const string FileName = "sceneTransitions.json";
        private static SceneTransitionDatabase cached;

        public static SceneTransitionOptions Resolve(string sceneName, SceneTransitionOptions overrides = null)
        {
            var db = Load();
            var opts = SceneTransitionOptions.FromDefaults(db?.defaults);
            if (!string.IsNullOrEmpty(sceneName) && db?.scenes != null)
            {
                var entry = db.scenes.Find(s => !string.IsNullOrEmpty(s.name) && string.Equals(s.name, sceneName, StringComparison.OrdinalIgnoreCase));
                if (entry != null)
                {
                    opts.Apply(entry);
                }
            }

            if (overrides != null)
                opts.Apply(overrides);

            return opts;
        }

        public static void WarmReload()
        {
            cached = null;
        }

        private static SceneTransitionDatabase Load()
        {
            if (cached != null) return cached;

            string persistent = Path.Combine(Application.persistentDataPath, FileName);
            if (File.Exists(persistent))
            {
                cached = TryRead(persistent);
                if (cached != null) return cached;
            }

            string streaming = Path.Combine(Application.streamingAssetsPath, FileName);
            if (File.Exists(streaming))
            {
                cached = TryRead(streaming);
                if (cached != null) return cached;
            }

            cached = new SceneTransitionDatabase();
            return cached;
        }

        private static SceneTransitionDatabase TryRead(string path)
        {
            try
            {
                var json = File.ReadAllText(path);
                var db = JsonUtility.FromJson<SceneTransitionDatabase>(json);
                Debug.Log($"SceneTransitionLibrary loaded {path}");
                return db ?? new SceneTransitionDatabase();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"SceneTransitionLibrary failed to parse {path}: {e.Message}");
                return new SceneTransitionDatabase();
            }
        }
    }
}
