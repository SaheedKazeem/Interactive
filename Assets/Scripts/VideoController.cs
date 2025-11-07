using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.SceneManagement;

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
    [SerializeField] private float skipFromEndSeconds = 4.25f; // Skip-to-end offset



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
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {

        // Check for the Escape key press to toggle video playback
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPlaying)
            {
                PauseVideo();
            }
            else
            {
                PlayVideo();
            }
        }
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
            SkipToEnd();
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
        return isPlaying;
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

    private void SkipToEnd()
    {
        if (videoPlayer != null)
        {
            // Ensure single subscription
            videoPlayer.loopPointReached -= EndReached;
            videoPlayer.loopPointReached += EndReached;
            videoPlayer.time = Mathf.Max(0f, (float)videoPlayer.length - skipFromEndSeconds);
            StartCoroutine(VideoPauseWaitAFewSecs());
        }
    }
    void EndReached(VideoPlayer vp)
{
    if (videoPlayer == null) return;
    PlayVideo();
    videoPlayer.time = Mathf.Max(0f, (float)videoPlayer.length - skipFromEndSeconds);
    PauseVideo();
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
    private IEnumerator VideoPauseWaitAFewSecs()
    {
        SetPlaybackSpeed(1);
        yield return new WaitForSeconds(1.25f); // Adjust the duration as needed
        if (videoPlayer != null && videoPlayer.isPrepared)
        {
            PauseVideo();
            
        }

    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= EndReached;
        }
    }

}
