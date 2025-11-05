using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using DG.Tweening;

namespace Interactive.Audio
{
    /// <summary>
    /// Simple music/cue director that loads audio clips from local files (Windows supported)
    /// and schedules them based on scene and video time. Lives across scenes.
    /// Place nothing in scenes: it auto-instantiates on first access.
    /// Configure via StreamingAssets/persistentDataPath music.json.
    /// </summary>
    public class MusicDirector : MonoBehaviour
    {
        public static MusicDirector Instance { get; private set; }

        private AudioSource srcA, srcB; // double-buffer for crossfades
        private bool usingA = true;
        private MusicProjectConfig config;
        private readonly List<ScheduledCue> scheduled = new List<ScheduledCue>();
        private VideoPlayer currentVideo;
        [Header("Ducking")] public bool enableDucking = true;
        [Range(0f,1f)] public float duckTo = 0.6f; // target volume for video audio while music plays
        public float duckFade = 0.5f;
        private int activeMusicCount = 0; // number of currently audible music sources
        private float originalVideoVolume = 1f;

        private class ScheduledCue
        {
            public MusicCue cue;
            public bool started;
            public bool stopping;
            public AudioSource source; // when started
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null)
            {
                var go = new GameObject("~MusicDirector");
                DontDestroyOnLoad(go);
                Instance = go.AddComponent<MusicDirector>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            EnsureSources();
            LoadConfig();
        }

        private void EnsureSources()
        {
            if (srcA == null)
            {
                srcA = gameObject.AddComponent<AudioSource>();
                srcA.loop = true; srcA.playOnAwake = false; srcA.volume = 0f;
            }
            if (srcB == null)
            {
                srcB = gameObject.AddComponent<AudioSource>();
                srcB.loop = true; srcB.playOnAwake = false; srcB.volume = 0f;
            }
        }

        private void LoadConfig()
        {
            try
            {
                string file = Path.Combine(Application.persistentDataPath, "music.json");
                if (!File.Exists(file)) file = Path.Combine(Application.streamingAssetsPath, "music.json");
                if (File.Exists(file))
                {
                    config = JsonUtility.FromJson<MusicProjectConfig>(File.ReadAllText(file));
                    Debug.Log($"MusicDirector loaded config: {file}");
                }
                else config = new MusicProjectConfig();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"MusicDirector failed to load config: {e.Message}");
                config = new MusicProjectConfig();
            }
        }

        public void ApplyForScene(string sceneName, VideoPlayer video)
        {
            currentVideo = video;
            scheduled.Clear();
            KillTweens(srcA); KillTweens(srcB);
            activeMusicCount = 0;

            var sc = config?.scenes?.Find(s => string.Equals(s.name, sceneName, StringComparison.OrdinalIgnoreCase));
            if (sc == null || sc.cues == null) return;

            foreach (var cue in sc.cues)
            {
                var sch = new ScheduledCue { cue = cue, started = false, stopping = false };
                scheduled.Add(sch);
                if (cue.startOnSceneLoad)
                {
                    _ = StartCoroutine(StartCueRoutine(sch));
                }
            }
        }

        private void Update()
        {
            if (currentVideo == null || !currentVideo.isPrepared) return;
            if (scheduled.Count == 0) return;
            double t = currentVideo.time;
            foreach (var s in scheduled)
            {
                if (!s.started && s.cue.startAtVideoTime >= 0f && t >= s.cue.startAtVideoTime)
                {
                    _ = StartCoroutine(StartCueRoutine(s));
                }
                if (s.started && !s.stopping && s.cue.stopAtVideoTime > 0f && t >= s.cue.stopAtVideoTime)
                {
                    s.stopping = true;
                    FadeOutAndStop(s.source, s.cue.fadeOut);
                }
            }
        }

        private System.Collections.IEnumerator StartCueRoutine(ScheduledCue s)
        {
            s.started = true;
            var clipReq = LoadClipAsync(s.cue.file);
            yield return clipReq;
            var clip = clipReq.Result;
            if (clip == null)
            {
                Debug.LogWarning($"MusicDirector: failed to load clip '{s.cue.file}'");
                yield break;
            }

            var tgt = NextSource();
            tgt.clip = clip;
            tgt.loop = s.cue.loop;
            tgt.volume = 0f;
            tgt.Play();
            s.source = tgt;

            tgt.DOKill();
            float fadeInDur = Mathf.Max(0.05f, s.cue.fadeIn);
            tgt.DOFade(Mathf.Clamp01(s.cue.volume), fadeInDur);

            // If the other source is playing, fade it out
            var other = OtherSource(tgt);
            if (other.isPlaying)
            {
                FadeOutAndStop(other, Mathf.Max(0.05f, s.cue.fadeOut));
            }

            // Duck video audio while music is audible
            activeMusicCount++;
            TryDuckVideo(true, fadeInDur);
        }

        private void FadeOutAndStop(AudioSource src, float duration)
        {
            if (src == null) return;
            src.DOKill();
            src.DOFade(0f, Mathf.Max(0.05f, duration)).OnComplete(() =>
            {
                src.Stop();
                src.clip = null;
            });
            // When a source is fading to 0, schedule unduck when all stopped
            StartCoroutine(CoCheckUnduck(duration + 0.05f));
        }

        private AudioSource NextSource() => usingA ? (usingA = false, srcA).Item2 : (usingA = true, srcB).Item2;
        private AudioSource OtherSource(AudioSource one) => one == srcA ? srcB : srcA;

        private static void KillTweens(AudioSource s)
        {
            if (s == null) return;
            DOTween.Kill(s);
            s.DOKill();
        }

        private class ClipRequest
        {
            public AudioClip Result;
        }

        private System.Collections.IEnumerator LoadClipAsync(string path)
        {
            var req = new ClipRequest();
            if (string.IsNullOrWhiteSpace(path)) { yield break; }

            string url = ToUrl(path);
            var type = GuessAudioType(url);
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(url, type))
            {
                yield return uwr.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
                if (uwr.result != UnityWebRequest.Result.Success)
#else
                if (uwr.isNetworkError || uwr.isHttpError)
#endif
                {
                    Debug.LogWarning($"MusicDirector UWR error for '{url}': {uwr.error}");
                    yield break;
                }
                var clip = DownloadHandlerAudioClip.GetContent(uwr);
                req.Result = clip;
            }
            yield return req;
        }

        private static AudioType GuessAudioType(string url)
        {
            string lower = url.ToLowerInvariant();
            if (lower.EndsWith(".mp3")) return AudioType.MPEG;
            if (lower.EndsWith(".ogg")) return AudioType.OGGVORBIS;
            if (lower.EndsWith(".wav")) return AudioType.WAV;
            return AudioType.MPEG; // default
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

        private void TryDuckVideo(bool duck, float duration)
        {
            if (!enableDucking || currentVideo == null) return;
            float target = duck ? Mathf.Clamp01(duckTo) : originalVideoVolume;
            float dur = duration > 0 ? duration : duckFade;
            // Get current volume (track 0)
            try
            {
                // Snapshot original on first duck
                if (duck && Mathf.Approximately(originalVideoVolume, 1f))
                {
                    originalVideoVolume = currentVideo.GetDirectAudioVolume(0);
                    if (originalVideoVolume <= 0f) originalVideoVolume = 1f; // fallback
                }
            }
            catch { /* GetDirectAudioVolume not supported on some platforms */ }

            // Smoothly set volume
            StartCoroutine(CoTweenVideoVolume(target, dur));
        }

        private System.Collections.IEnumerator CoTweenVideoVolume(float target, float duration)
        {
            if (currentVideo == null) yield break;
            float start;
            try { start = currentVideo.GetDirectAudioVolume(0); }
            catch { start = 1f; }
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime; // don't tie to timescale
                float a = duration <= 0 ? 1f : Mathf.Clamp01(t / duration);
                float v = Mathf.Lerp(start, target, a);
                try { currentVideo.SetDirectAudioVolume(0, v); } catch { }
                yield return null;
            }
            try { currentVideo.SetDirectAudioVolume(0, target); } catch { }
        }

        private System.Collections.IEnumerator CoCheckUnduck(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            // Check if any source still audible
            bool any = (srcA != null && srcA.isPlaying && srcA.volume > 0.01f) || (srcB != null && srcB.isPlaying && srcB.volume > 0.01f);
            if (!any)
            {
                activeMusicCount = 0;
                TryDuckVideo(false, duckFade);
            }
        }
    }
}
