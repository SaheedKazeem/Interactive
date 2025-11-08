using System;
using System.Collections;
using System.Collections.Generic;
using Interactive.Config;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject rewindButton;
    public GameObject fastForwardButton;
    public GameObject skipButton;
    public bool hasFBeenPressed;

    [SerializeField] private bool isPlaying = false;
    private float defaultPlaybackSpeed = 1f;
    [Header("Speed Settings")]
    [SerializeField] private float speedMultiplier = 2.0f; // Multiplier for SpeedUp
    [SerializeField] private float seekSeconds = 5.0f;       // Q/E seek amount
    [SerializeField] private float skipFromEndSeconds = 4.25f; // Lead-in before decision when pausing
    [SerializeField] private float decisionSnapEpsilon = 0.2f; // Seconds past a decision before we skip to the next
    [SerializeField] private float minPlaybackSpeed = 0.5f;
    [SerializeField] private float defaultMaxPlaybackSpeed = 3.5f;
    [SerializeField] private float shortClipSeconds = 60f;
    [SerializeField] private float mediumClipSeconds = 150f;
    [SerializeField] private float longClipSeconds = 300f;
    [SerializeField] private float extraLongClipSeconds = 480f;
    [SerializeField] private float shortClipMaxSpeed = 1.5f;
    [SerializeField] private float mediumClipMaxSpeed = 2.25f;
    [SerializeField] private float longClipMaxSpeed = 3f;
    [SerializeField] private float extraLongClipMaxSpeed = 3.5f;
    [SerializeField] private float marathonClipMaxSpeed = 4f;
    [SerializeField] private float muteSpeedThreshold = 1.05f;

    private readonly List<float> decisionTimes = new List<float>();
    private string cachedSceneName;
    private const double MinSeekTailSeconds = 0.05d;
    private const string PlaybackSpeedPrefKey = "PlaybackSpeed";

    private float requestedPlaybackSpeed = 1f;
    private bool audioMutedForSpeed;


    private void Start()
    {
        requestedPlaybackSpeed = PlayerPrefs.GetFloat(PlaybackSpeedPrefKey, 1f);

        if (videoPlayer == null)
        {
            videoPlayer = Interactive.Util.SceneObjectFinder.FindFirst<VideoPlayer>(true);
        }

        if (videoPlayer != null)
        {
            defaultPlaybackSpeed = Mathf.Max(0.1f, videoPlayer.playbackSpeed);
            if (!PlayerPrefs.HasKey(PlaybackSpeedPrefKey))
            {
                requestedPlaybackSpeed = defaultPlaybackSpeed;
            }
            // Ensure we don't double-subscribe
            videoPlayer.loopPointReached -= EndReached;
            videoPlayer.loopPointReached += EndReached;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            ApplyRequestedPlaybackSpeed();
        }

        // Try to resolve UI references if not wired in Inspector
        if (rewindButton == null) rewindButton = GameObject.Find("rewindButton");
        if (fastForwardButton == null) fastForwardButton = GameObject.Find("fastForwardButton");
        if (skipButton == null) skipButton = GameObject.Find("skipButton");

        if (rewindButton) rewindButton.SetActive(false);
        if (fastForwardButton) fastForwardButton.SetActive(false);
        if (skipButton) skipButton.SetActive(false);

        RefreshDecisionTimes(SceneManager.GetActiveScene().name);

        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        videoPlayer = Interactive.Util.SceneObjectFinder.FindFirst<VideoPlayer>(true);
        if (videoPlayer != null)
        {
            defaultPlaybackSpeed = Mathf.Max(0.1f, videoPlayer.playbackSpeed);
            videoPlayer.loopPointReached -= EndReached;
            videoPlayer.loopPointReached += EndReached;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            ApplyRequestedPlaybackSpeed();
            PlayVideo();
        }
        RefreshDecisionTimes(scene.name);
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F11))
        {
            // Toggle full screen
            Screen.fullScreen = !Screen.fullScreen;
            if (Screen.fullScreen)
                Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (videoPlayer != null)
            {
                videoPlayer.time = Mathf.Max(0f, (float)videoPlayer.time - seekSeconds);
                ShowAndHideButton(rewindButton);
            }
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (videoPlayer != null)
            {
                videoPlayer.time += seekSeconds;
                ShowAndHideButton(fastForwardButton);
            }
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            
            hasFBeenPressed = true;
            SkipToNextDecision();
            ShowAndHideButton(skipButton);
        }
        if (Input.GetKeyUp(KeyCode.F))
        {
            hasFBeenPressed = false;
        }
    }

    public void PlayVideo()
    {
        if (videoPlayer != null)
        {
            if (!videoPlayer.isPrepared)
            {
                videoPlayer.Prepare();
            }
            videoPlayer.Play();
            isPlaying = true;
        }
    }

    public void PauseVideo()
    {
        if (videoPlayer != null)
        {
            videoPlayer.Pause();
            isPlaying = false;
        }
    }

    public void SpeedUp()
    {
        SetPlaybackSpeed(GetPlaybackSpeed() * speedMultiplier);
    }

    public bool IsVideoPlaying()
    {
        return videoPlayer != null && videoPlayer.isPlaying;
    }

    // Method to get the current playback speed
    public float GetPlaybackSpeed()
    {
        if (videoPlayer != null)
            return videoPlayer.playbackSpeed;
        return requestedPlaybackSpeed;
    }

    // Method to set the playback speed
    public void SetPlaybackSpeed(float speed)
    {
        requestedPlaybackSpeed = speed;
        ApplyRequestedPlaybackSpeed();
    }
    public void SetAudioVolume(float volume)
    {
        if (videoPlayer != null)
        {
            videoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(volume));
        }
    }
    public void SetAudioVolumeSaved()
    {
        float savedVolume = PlayerPrefs.GetFloat("VideoVolume", 1.0f);
        if (videoPlayer != null)
        {
            float target = audioMutedForSpeed ? 0f : Mathf.Clamp01(savedVolume);
            videoPlayer.SetDirectAudioVolume(0, target);
        }
    }

    public void SetSeekSeconds(float seconds)
    {
        seekSeconds = Mathf.Max(0.1f, seconds);
    }

    public void GetPlaybackSpeedRange(out float min, out float max)
    {
        CalculateSpeedClamp(out min, out max);
    }

    public float GetAudioMuteThreshold() => muteSpeedThreshold;

    public void RefreshAudioPolicy()
    {
        ApplyAudioPolicy(GetPlaybackSpeed());
    }

    private void ApplyRequestedPlaybackSpeed()
    {
        CalculateSpeedClamp(out float min, out float max);
        float clamped = Mathf.Clamp(requestedPlaybackSpeed, min, max);
        requestedPlaybackSpeed = clamped;

        if (videoPlayer != null)
        {
            videoPlayer.playbackSpeed = clamped;
        }

        PlayerPrefs.SetFloat(PlaybackSpeedPrefKey, clamped);
        PlayerPrefs.Save();
        ApplyAudioPolicy(clamped);
    }

    private void CalculateSpeedClamp(out float min, out float max)
    {
        min = Mathf.Max(0.1f, minPlaybackSpeed);
        max = defaultMaxPlaybackSpeed;
        double clipLength = GetClipLengthSeconds();
        if (clipLength > 0d)
        {
            if (clipLength <= shortClipSeconds) max = shortClipMaxSpeed;
            else if (clipLength <= mediumClipSeconds) max = mediumClipMaxSpeed;
            else if (clipLength <= longClipSeconds) max = longClipMaxSpeed;
            else if (clipLength <= extraLongClipSeconds) max = extraLongClipMaxSpeed;
            else max = marathonClipMaxSpeed;
        }
        if (max < min + 0.05f) max = min + 0.05f;
    }

    private double GetClipLengthSeconds()
    {
        if (videoPlayer == null) return 0d;
        try { return videoPlayer.length; }
        catch { return 0d; }
    }

    private void ApplyAudioPolicy(float speed)
    {
        if (videoPlayer == null) return;

        if (speed >= muteSpeedThreshold)
        {
            videoPlayer.SetDirectAudioVolume(0, 0f);
            audioMutedForSpeed = true;
        }
        else
        {
            audioMutedForSpeed = false;
            float savedVolume = PlayerPrefs.GetFloat("VideoVolume", 1.0f);
            videoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(savedVolume));
        }
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        ApplyRequestedPlaybackSpeed();
    }

    private void SkipToNextDecision()
    {
        if (videoPlayer == null) return;
        if (!videoPlayer.canSetTime) return;
        if (!videoPlayer.isPrepared)
        {
            videoPlayer.Prepare();
            return;
        }

        // Ensure single subscription
        videoPlayer.loopPointReached -= EndReached;
        videoPlayer.loopPointReached += EndReached;

        double currentTime = videoPlayer.time;
        double? decisionTime = FindNextDecisionTime(currentTime);
        if (decisionTime.HasValue)
        {
            double target = decisionTime.Value - skipFromEndSeconds;
            SeekTo(target, keepPlaybackState: true);
        }
        else
        {
            // Fallback to legacy behaviour
            double fallback = videoPlayer.length > 0d
                ? Math.Max(0d, videoPlayer.length - skipFromEndSeconds)
                : 0d;
            SeekTo(fallback, keepPlaybackState: true);
        }
    }

    private void RefreshDecisionTimes(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;
        if (string.Equals(cachedSceneName, sceneName, StringComparison.OrdinalIgnoreCase))
            return;

        cachedSceneName = sceneName;
        decisionTimes.Clear();

        var projectConfig = VideoSceneConfigLoader.Load();
        if (projectConfig?.scenes == null) return;
        var sceneConfig = projectConfig.scenes.Find(s => string.Equals(s.name, sceneName, StringComparison.OrdinalIgnoreCase));
        if (sceneConfig?.buttons == null || sceneConfig.buttons.Count == 0) return;

        var uniqueTimes = new HashSet<float>();
        foreach (var button in sceneConfig.buttons)
        {
            if (button == null) continue;
            float t = Mathf.Max(0f, button.appearTime);
            if (uniqueTimes.Add(t))
            {
                decisionTimes.Add(t);
            }
        }

        decisionTimes.Sort();
    }

    private double? FindNextDecisionTime(double currentTime)
    {
        if (decisionTimes.Count == 0) return null;
        foreach (var t in decisionTimes)
        {
            if (currentTime <= t + decisionSnapEpsilon)
                return t;
        }
        return decisionTimes[decisionTimes.Count - 1];
    }

    private double? GetLastDecisionTime()
    {
        if (decisionTimes.Count == 0) return null;
        return decisionTimes[decisionTimes.Count - 1];
    }

    private void SeekTo(double targetTime, bool keepPlaybackState)
    {
        if (videoPlayer == null) return;
        if (!videoPlayer.canSetTime) return;

        if (!videoPlayer.isPrepared)
        {
            videoPlayer.Prepare();
            return;
        }

        double clipLength = videoPlayer.length;
        if (clipLength > 0d)
        {
            targetTime = Math.Min(targetTime, clipLength - MinSeekTailSeconds);
        }
        targetTime = Math.Max(0d, targetTime);

        bool wasPlaying = videoPlayer.isPlaying;
        videoPlayer.time = targetTime;

        if (!keepPlaybackState)
        {
            PauseVideo();
        }
        else if (!wasPlaying)
        {
            PauseVideo();
        }
    }

    void EndReached(VideoPlayer vp)
    {
        if (videoPlayer == null) return;
        double? decisionTime = GetLastDecisionTime();
        double target = decisionTime.HasValue
            ? decisionTime.Value - skipFromEndSeconds
            : (videoPlayer.length > 0d ? Math.Max(0d, videoPlayer.length - skipFromEndSeconds) : 0d);

        SeekTo(target, keepPlaybackState: false);
    }
    private void ShowAndHideButton(GameObject button)
    {
        StartCoroutine(ShowAndHideRoutine(button));
    }

    private IEnumerator ShowAndHideRoutine(GameObject button)
    {
        if (button)
            button.SetActive(true);
        yield return new WaitForSeconds(0.5f); // Adjust the duration as needed
        if (button)
            button.SetActive(false);
    }
    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= EndReached;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

}
