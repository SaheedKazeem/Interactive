using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Interactive.Audio
{
    /// <summary>
    /// Lightweight SFX loader/player for local files (mp3/ogg/wav). Add once to a scene or spawn on demand.
    /// Now also supports triggering short snippets defined in music.json so you can reuse soundtrack stems for UI hits.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private AudioSource oneShotSource;
        private AudioSource snippetSource;

        private void Awake()
        {
            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
            oneShotSource.loop = false;
            oneShotSource.spatialBlend = 0f;
            oneShotSource.ignoreListenerPause = true;

            snippetSource = gameObject.AddComponent<AudioSource>();
            snippetSource.playOnAwake = false;
            snippetSource.loop = false;
            snippetSource.spatialBlend = 0f;
            snippetSource.ignoreListenerPause = true;

            DontDestroyOnLoad(gameObject);
        }

        public void PlayOneShot(string path, float volume = 1f)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            StartCoroutine(PlayRoutine(path, volume));
        }

        private IEnumerator PlayRoutine(string path, float volume)
        {
            string url = ToUrl(path);
            var type = GuessAudioType(url);
            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogWarning($"SfxPlayer: failed to load '{url}': {req.error}");
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                oneShotSource.volume = Mathf.Clamp01(volume);
                oneShotSource.PlayOneShot(clip);
            }
        }

        public void PlaySnippet(string snippetName)
        {
            if (string.IsNullOrWhiteSpace(snippetName)) return;
            var snippet = ResolveSnippet(snippetName);
            if (snippet == null)
            {
                Debug.LogWarning($"SfxPlayer: snippet '{snippetName}' not found in music.json.");
                return;
            }
            StartCoroutine(PlaySnippetRoutine(snippet));
        }

        private IEnumerator PlaySnippetRoutine(MusicSnippet snippet)
        {
            if (snippet == null || string.IsNullOrWhiteSpace(snippet.file)) yield break;

            string url = ToUrl(snippet.file);
            var type = GuessAudioType(url);
            using (var req = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                {
                    Debug.LogWarning($"SfxPlayer: failed to load snippet '{snippet.name}' ({url}): {req.error}");
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(req);
                if (clip == null)
                {
                    Debug.LogWarning($"SfxPlayer: snippet '{snippet.name}' produced no clip.");
                    yield break;
                }

                float start = Mathf.Clamp(snippet.start, 0f, Mathf.Max(0f, clip.length - 0.01f));
                float maxDuration = Mathf.Max(0.05f, clip.length - start);
                float duration = snippet.duration > 0f ? Mathf.Min(snippet.duration, maxDuration) : maxDuration;

                snippetSource.Stop();
                snippetSource.clip = clip;
                snippetSource.time = start;
                snippetSource.volume = Mathf.Clamp01(snippet.volume);
                snippetSource.pitch = Mathf.Clamp(snippet.pitch, 0.25f, 3f);
                snippetSource.Play();

                yield return new WaitForSeconds(duration);

                snippetSource.Stop();
                snippetSource.clip = null;
            }
        }

        public static SfxPlayer Ensure()
        {
            var existing = Interactive.Util.SceneObjectFinder.FindFirst<SfxPlayer>(true);
            if (existing != null) return existing;
            var go = new GameObject("~SfxPlayer");
            return go.AddComponent<SfxPlayer>();
        }

        private static MusicSnippet ResolveSnippet(string snippetName)
        {
            var cfg = MusicConfigProvider.Load();
            if (cfg?.snippets == null || cfg.snippets.Count == 0) return null;
            foreach (var snippet in cfg.snippets)
            {
                if (snippet == null) continue;
                if (string.Equals(snippet.name, snippetName, System.StringComparison.OrdinalIgnoreCase))
                    return snippet;
            }
            return null;
        }

        private static string ToUrl(string path)
        {
            if (path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("file:///"))
                return path;
            string p = path.Replace('\\', '/');
            if (p.Length > 1 && p[1] == ':') return $"file:///{p}"; // Windows drive
            string abs = Path.GetFullPath(path).Replace('\\', '/');
            return $"file:///{abs}";
        }

        private static AudioType GuessAudioType(string url)
        {
            string lower = url.ToLowerInvariant();
            if (lower.EndsWith(".mp3")) return AudioType.MPEG;
            if (lower.EndsWith(".ogg")) return AudioType.OGGVORBIS;
            if (lower.EndsWith(".wav")) return AudioType.WAV;
            return AudioType.MPEG;
        }
    }
}
