using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace Interactive.Audio
{
    /// <summary>
    /// Lightweight SFX loader/player for local files (mp3/ogg/wav). Add once to a scene or spawn on demand.
    /// </summary>
    public class SfxPlayer : MonoBehaviour
    {
        private AudioSource source;

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
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
                source.volume = Mathf.Clamp01(volume);
                source.PlayOneShot(clip);
            }
        }

        public static SfxPlayer Ensure()
        {
            var existing = FindObjectOfType<SfxPlayer>();
            if (existing != null) return existing;
            var go = new GameObject("~SfxPlayer");
            return go.AddComponent<SfxPlayer>();
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

