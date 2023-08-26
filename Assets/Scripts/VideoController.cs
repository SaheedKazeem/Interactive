using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject rewindButton;
    public GameObject fastForwardButton;
    public GameObject skipButton;

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

    }

    private void Update()
    {

        if (videoPlayer == null)
        {
            videoPlayer = FindObjectOfType<VideoPlayer>();
        }
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
            SkipToEnd();
            ShowAndHideButton(skipButton);
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
        if (videoPlayer != null)
        {
            videoPlayer.time = videoPlayer.length - 4.25f;
              StartCoroutine(VideoPauseWaitAFewSecs());
        }
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

        yield return new WaitForSeconds(1.25f); // Adjust the duration as needed
        if (videoPlayer.isPrepared)
        {
            videoPlayer.Pause();
        isPlaying = false;
        }
        
    }

}
