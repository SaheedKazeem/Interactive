using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    [SerializeField] private bool isPlaying = false;
    private float defaultPlaybackSpeed;
    private float speedMultiplier = 2.0f; // You can adjust this value to change the speed increase amount.

    private void Start()
    {
        
        defaultPlaybackSpeed = videoPlayer.playbackSpeed;
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
        videoPlayer.playbackSpeed = speed;
    }
}
