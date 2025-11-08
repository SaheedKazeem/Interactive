using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using DG.Tweening;
using Interactive.Util;

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
        [Header("Ducking")] public bool enableDucking = false;
        [Range(0f,1f)] public float duckTo = 0.6f; // target volume for video audio while music plays
        public float duckFade = 0.5f;
        private int activeMusicCount = 0; // number of currently audible music sources
        private float originalVideoVolume = 1f;
        private bool isDucked = false;
        private bool hasOriginalVideoVolume = false;

        // Playlist overlay (for pause/fast-forward lobby music)
        [Header("Playlist Overlay")] public bool enablePlaylistOverlay = true;
        public bool overlayDuringPause = true;
        public float fastForwardThreshold = 1.05f;
        public float overlayPauseVolume = 0.12f;
        public float overlayFastVolume = 0.35f;
        public float overlayFade = 0.5f;

        [Header("Scene Cue Mix")]
        [Range(0f,1f)] public float sceneCueVolumeMultiplier = 0.5f;
        [Range(0f,1f)] public float overlayVolumeMultiplier = 0.4f;

        [Header("Automatic Scene Binding")]
        public bool autoApplyOnSceneLoad = true;

        private List<MusicCue> playlist;
        private int playlistIndex = 0;
        private AudioSource playlistSrc;
        private bool overlayActive = false;
        private float overlayTarget = 0f;
        private Coroutine playlistLoopCo;
        private bool isAutoApplying;
        private const float defaultStopFade = 0.75f;

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
            if (autoApplyOnSceneLoad)
            {
                SceneManager.sceneLoaded += HandleSceneLoaded;
                StartCoroutine(CoAutoApply(SceneManager.GetActiveScene().name));
            }
        }

        private void OnDestroy()
        {
            if (Instance == this && autoApplyOnSceneLoad)
                SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!autoApplyOnSceneLoad) return;
            StartCoroutine(CoAutoApply(scene.name));
        }

        private System.Collections.IEnumerator CoAutoApply(string sceneName)
        {
            yield return null; // wait a frame so finders can locate objects
            isAutoApplying = true;
            VideoPlayer vp = null;
            try { vp = SceneObjectFinder.FindFirst<VideoPlayer>(true); } catch { }
            ApplyForScene(sceneName, vp);
            isAutoApplying = false;
        }

        private void EnsureSources()
        {
            if (srcA == null)
            {
                srcA = gameObject.AddComponent<AudioSource>();
                srcA.loop = true; srcA.playOnAwake = false; srcA.volume = 0f;
                srcA.ignoreListenerPause = true; srcA.pitch = 1f;
            }
            if (srcB == null)
            {
                srcB = gameObject.AddComponent<AudioSource>();
                srcB.loop = true; srcB.playOnAwake = false; srcB.volume = 0f;
                srcB.ignoreListenerPause = true; srcB.pitch = 1f;
            }
            if (playlistSrc == null)
            {
                playlistSrc = gameObject.AddComponent<AudioSource>();
                playlistSrc.loop = false; playlistSrc.playOnAwake = false; playlistSrc.volume = 0f;
                playlistSrc.ignoreListenerPause = true; playlistSrc.pitch = 1f;
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
            if (sc == null || sc.cues == null || sc.cues.Count == 0)
            {
                StopAllSceneSources(defaultStopFade);
                return;
            }

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
            // Evaluate overlay (pause/fast-forward lobby music)
            EvaluateOverlay();

            // If ducking is active but no music is audible anymore (e.g., non-loop clip ended), unduck
            if (enableDucking && isDucked && !IsAnySourceAudible())
                TryDuckVideo(false, duckFade);

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
            var clipReq = new ClipRequest();
            yield return StartCoroutine(LoadClipAsync(s.cue.file, clipReq));
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
            float targetVolume = Mathf.Clamp01(s.cue.volume * sceneCueVolumeMultiplier);
            tgt.DOFade(targetVolume, fadeInDur).SetUpdate(true);

            // If the other source is playing, fade it out
            var other = OtherSource(tgt);
            if (other.isPlaying)
            {
                FadeOutAndStop(other, Mathf.Max(0.05f, s.cue.fadeOut));
            }

            // Duck video audio while music is audible
            activeMusicCount++;
            if (!overlayActive)
                TryDuckVideo(true, fadeInDur);

            // If overlay is active (paused/fast-forward), keep scene music inaudible
            if (overlayActive)
            {
                tgt.DOKill();
                tgt.DOFade(0f, 0.1f).SetUpdate(true);
            }
        }

        private void FadeOutAndStop(AudioSource src, float duration)
        {
            if (src == null) return;
            src.DOKill();
            src.DOFade(0f, Mathf.Max(0.05f, duration)).SetUpdate(true).OnComplete(() =>
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

        private System.Collections.IEnumerator LoadClipAsync(string path, ClipRequest req)
        {
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
            // nothing to return; result assigned to holder
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

        private void EnsurePlaylistList()
        {
            if (playlist != null && playlist.Count > 0) return;
            if (config != null && config.playlist != null && config.playlist.Count > 0)
            {
                playlist = new List<MusicCue>(config.playlist);
            }
            else
            {
                var sc = config?.scenes?.Find(s => string.Equals(s.name, "_GLOBAL_", StringComparison.OrdinalIgnoreCase)
                                                 || string.Equals(s.name, "_PLAYLIST_", StringComparison.OrdinalIgnoreCase));
                if (sc != null && sc.cues != null && sc.cues.Count > 0)
                    playlist = new List<MusicCue>(sc.cues);
            }
        }

        private void EvaluateOverlay()
        {
            if (!enablePlaylistOverlay) return;
            EnsurePlaylistList();
            if (playlist == null || playlist.Count == 0) return;
            if (currentVideo == null) return;

            bool videoPlaying = false;
            float speed = 1f;
            try
            {
                videoPlaying = currentVideo.isPlaying;
                speed = currentVideo.playbackSpeed;
            }
            catch { }

            bool wantsPause = overlayDuringPause && (!videoPlaying || speed <= 0.01f);
            bool wantsFast = speed > fastForwardThreshold;
            bool shouldOverlay = wantsPause || wantsFast;
            float targetVol = wantsFast ? overlayFastVolume : overlayPauseVolume;

            if (shouldOverlay)
            {
                if (!overlayActive)
                {
                    StartOverlay(targetVol);
                }
                else if (Mathf.Abs(overlayTarget - targetVol) > 0.01f)
                {
                    overlayTarget = targetVol;
                    FadeSourceTo(playlistSrc, overlayTarget * overlayVolumeMultiplier, overlayFade);
                }
            }
            else if (overlayActive)
            {
                StopOverlay();
            }
        }

        private void StartOverlay(float targetVol)
        {
            overlayActive = true;
            overlayTarget = Mathf.Clamp01(targetVol);

            // Fade down scene sources without stopping, so they can resume after overlay
            ForEachActiveSceneSource((src, cue) => FadeSourceTo(src, 0f, overlayFade));

            // Ensure playlist loop coroutine is running
            if (playlistLoopCo == null) playlistLoopCo = StartCoroutine(PlaylistLoop());
            // Raise playlist volume
            FadeSourceTo(playlistSrc, overlayTarget * overlayVolumeMultiplier, overlayFade);

            // Ensure video is unducked while playlist overlay plays
            TryDuckVideo(false, overlayFade);
        }

        private void StopOverlay()
        {
            overlayActive = false;
            // Fade out playlist and pause
            if (playlistSrc != null)
            {
                playlistSrc.DOKill();
                playlistSrc.DOFade(0f, Mathf.Max(0.05f, overlayFade)).SetUpdate(true).OnComplete(() =>
                {
                    if (playlistSrc != null) playlistSrc.Pause();
                });
            }
            // Restore scene sources to their configured volumes
            ForEachActiveSceneSource((src, cue) => FadeSourceTo(src, Mathf.Clamp01(cue.volume * sceneCueVolumeMultiplier), overlayFade));

            // Reapply duck if scene music is audible
            if (enableDucking)
            {
                bool anySceneAudible = false;
                foreach (var s in scheduled)
                {
                    if (s.started && !s.stopping && s.source != null && s.source.volume > 0.01f)
                    {
                        anySceneAudible = true; break;
                    }
                }
                if (anySceneAudible) TryDuckVideo(true, overlayFade);
            }
        }

        private void ForEachActiveSceneSource(Action<AudioSource, MusicCue> op)
        {
            if (scheduled == null) return;
            foreach (var s in scheduled)
            {
                if (s.started && !s.stopping && s.source != null)
                {
                    op?.Invoke(s.source, s.cue);
                }
            }
        }

        private void FadeSourceTo(AudioSource src, float v, float dur)
        {
            if (src == null) return;
            src.DOKill();
            src.DOFade(Mathf.Clamp01(v), Mathf.Max(0.05f, dur)).SetUpdate(true);
        }

        private void StopAllSceneSources(float fade)
        {
            FadeOutAndStop(srcA, fade);
            FadeOutAndStop(srcB, fade);
            scheduled.Clear();
        }

        private System.Collections.IEnumerator PlaylistLoop()
        {
            EnsurePlaylistList();
            if (playlist == null || playlist.Count == 0) { playlistLoopCo = null; yield break; }
            while (true)
            {
                // Wait until overlay is active
                while (!overlayActive) { yield return null; }

                // If src playing, wait until it ends or overlay stops
                if (playlistSrc != null && playlistSrc.isPlaying)
                {
                    yield return null; continue;
                }

                // Load next track
                var cue = playlist[playlistIndex];
                var req = new ClipRequest();
                yield return StartCoroutine(LoadClipAsync(cue.file, req));
                var clip = req.Result;
                if (clip == null)
                {
                    // Skip to next
                    playlistIndex = (playlistIndex + 1) % playlist.Count;
                    continue;
                }

                if (playlistSrc == null) yield break;
                playlistSrc.clip = clip;
                playlistSrc.loop = false;
                if (!overlayActive)
                {
                    // If overlay turned off during load, pause and continue
                    playlistSrc.Pause();
                    continue;
                }
                // Keep current volume (it is animated by overlay start/adjust)
                if (!playlistSrc.isPlaying) playlistSrc.Play();

                // Wait for end or overlay deactivation
                double len = clip.length;
                double start = Time.unscaledTimeAsDouble;
                while (overlayActive && playlistSrc != null && playlistSrc.isPlaying)
                {
                    // If clip length is exceeded but still playing (streaming timing), break
                    if (Time.unscaledTimeAsDouble - start > len + 0.5f) break;
                    yield return null;
                }
                // Advance index
                playlistIndex = (playlistIndex + 1) % playlist.Count;
            }
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
                if (duck && !hasOriginalVideoVolume)
                {
                    originalVideoVolume = currentVideo.GetDirectAudioVolume(0);
                    if (originalVideoVolume <= 0f) originalVideoVolume = 1f; // fallback
                    hasOriginalVideoVolume = true;
                }
            }
            catch { /* GetDirectAudioVolume not supported on some platforms */ }

            // Smoothly set volume
            StartCoroutine(CoTweenVideoVolume(target, dur));
            isDucked = duck;
            if (!duck)
            {
                // Reset snapshot so next duck re-reads source volume
                hasOriginalVideoVolume = false;
            }
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
            bool any = IsAnySourceAudible();
            if (!any)
            {
                activeMusicCount = 0;
                TryDuckVideo(false, duckFade);
            }
        }

        private bool IsAnySourceAudible()
        {
            return (srcA != null && srcA.isPlaying && srcA.volume > 0.01f) || (srcB != null && srcB.isPlaying && srcB.volume > 0.01f);
        }
    }
}
