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
    private float speedMultiplier = 2.0f; // You can adjust this value to change the speed increase amount.



    private void Start()
    {
        videoPlayer = FindObjectOfType<VideoPlayer>();

        defaultPlaybackSpeed = videoPlayer.playbackSpeed;
        rewindButton = GameObject.Find("rewindButton");
        fastForwardButton = GameObject.Find("fastForwardButton");
        skipButton = GameObject.Find("skipButton");
        rewindButton.SetActive(false);
        fastForwardButton.SetActive(false);
        skipButton.SetActive(false);

        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        
        if (videoPlayer == null)
        {
            videoPlayer = FindObjectOfType<VideoPlayer>();
        }
        else PlayVideo();
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
            Screen.fullScreen = true;
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        }
        if (Input.GetKeyDown(KeyCode.Q))
        {
            videoPlayer.time -= 5.0f;
            ShowAndHideButton(rewindButton);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            videoPlayer.time += 5.0f;
            ShowAndHideButton(fastForwardButton);
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
        videoPlayer.Play();
        isPlaying = true;
    }

    public void PauseVideo()
    {
        videoPlayer.Pause();
        isPlaying = false;
    }

    public void SpeedUp()
    {
        videoPlayer.playbackSpeed *= speedMultiplier;
    }

    public bool IsVideoPlaying()
    {
        return isPlaying;
    }

    // Method to get the current playback speed
    public float GetPlaybackSpeed()
    {
        return videoPlayer.playbackSpeed;
    }

    // Method to set the playback speed
    public void SetPlaybackSpeed(float speed)
    {
        if (videoPlayer != null)
        {
            videoPlayer.playbackSpeed = speed;
        }

    }
    public void SetAudioVolume(float volume)
    {
        videoPlayer.SetDirectAudioVolume(0, volume);
    }
    public void SetAudioVolumeSaved()
    {
        float savedVolume = PlayerPrefs.GetFloat("VideoVolume", 1.0f);
        videoPlayer.SetDirectAudioVolume(0, savedVolume);
    }

    private void SkipToEnd()
    {
        videoPlayer.loopPointReached += EndReached;
        if (videoPlayer != null)
        {
            videoPlayer.time = videoPlayer.length - 4.25f;
            StartCoroutine(VideoPauseWaitAFewSecs());
        }
    }
    void EndReached(VideoPlayer vp)
{
    PlayVideo();
   videoPlayer.time = videoPlayer.length - 4.25f;
   PauseVideo();
}
    private void ShowAndHideButton(GameObject button)
    {
        StartCoroutine(ShowAndHideRoutine(button));
    }

    private IEnumerator ShowAndHideRoutine(GameObject button)
    {
        button.SetActive(true);
        yield return new WaitForSeconds(0.5f); // Adjust the duration as needed
        button.SetActive(false);
    }
    private IEnumerator VideoPauseWaitAFewSecs()
    {
        SetPlaybackSpeed(1);
        yield return new WaitForSeconds(1.25f); // Adjust the duration as needed
        if (videoPlayer.isPrepared)
        {
            PauseVideo();
            
        }

    }

}
