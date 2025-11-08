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
    private float defaultPlaybackSpeed;
    [SerializeField] private float speedMultiplier = 2.0f; // Multiplier for SpeedUp
    [SerializeField] private float seekSeconds = 5.0f;       // Q/E seek amount
    [SerializeField] private float skipFromEndSeconds = 4.25f; // Lead-in before decision when pausing
    [SerializeField] private float decisionSnapEpsilon = 0.2f; // Seconds past a decision before we skip to the next

    private readonly List<float> decisionTimes = new List<float>();
    private string cachedSceneName;
    private const double MinSeekTailSeconds = 0.05d;


    private void Start()
    {
        if (videoPlayer == null)
        {
            videoPlayer = Interactive.Util.SceneObjectFinder.FindFirst<VideoPlayer>(true);
        }

        if (videoPlayer != null)
        {
            defaultPlaybackSpeed = videoPlayer.playbackSpeed;
            // Ensure we don't double-subscribe
            videoPlayer.loopPointReached -= EndReached;
            videoPlayer.loopPointReached += EndReached;
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
        
        if (videoPlayer == null)
        {
            videoPlayer = Interactive.Util.SceneObjectFinder.FindFirst<VideoPlayer>(true);
        }
        else
        {
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
        if (videoPlayer != null)
        {
            videoPlayer.playbackSpeed = Mathf.Clamp(videoPlayer.playbackSpeed * speedMultiplier, 0.1f, 4f);
        }
    }

    public bool IsVideoPlaying()
    {
        return videoPlayer != null && videoPlayer.isPlaying;
    }

    // Method to get the current playback speed
    public float GetPlaybackSpeed()
    {
        return videoPlayer != null ? videoPlayer.playbackSpeed : 1f;
    }

    // Method to set the playback speed
    public void SetPlaybackSpeed(float speed)
    {
        if (videoPlayer != null)
        {
            videoPlayer.playbackSpeed = Mathf.Clamp(speed, 0.1f, 4f);
        }

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
            videoPlayer.SetDirectAudioVolume(0, Mathf.Clamp01(savedVolume));
        }
    }

    public void SetSeekSeconds(float seconds)
    {
        seekSeconds = Mathf.Max(0.1f, seconds);
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
        }
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

}
