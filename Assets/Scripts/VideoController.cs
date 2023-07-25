using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public Button playButton;
    public Button speedUpButton;

    private bool isPlaying = false;
    private float defaultPlaybackSpeed;
    private float speedMultiplier = 2.0f; // You can adjust this value to change the speed increase amount.

    private void Start()
    {
        videoPlayer.playOnAwake = false;
        defaultPlaybackSpeed = videoPlayer.playbackSpeed;

        playButton.onClick.AddListener(TogglePlay);
        speedUpButton.onClick.AddListener(SpeedUp);
    }

    private void TogglePlay()
    {
        if (isPlaying)
        {
            videoPlayer.Pause();
        }
        else
        {
            videoPlayer.Play();
        }

        isPlaying = !isPlaying;
    }

    private void SpeedUp()
    {
        videoPlayer.playbackSpeed *= speedMultiplier;
    }
}
