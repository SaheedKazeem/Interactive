using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System;

public class VideoVolumeSlider : MonoBehaviour
{
    public Slider volumeSlider;
    public VideoPlayer videoPlayer;

    private void Start()
    {
        // Load the volume setting from PlayerPrefs
        float savedVolume = PlayerPrefs.GetFloat("VideoVolume", 1.0f);
        volumeSlider.value = savedVolume;

        // Add a listener to the slider's value change event
        volumeSlider.onValueChanged.AddListener(ChangeVideoVolume);

        // Subscribe to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        // Unsubscribe from the sceneLoaded event to prevent memory leaks
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the VideoPlayer component
        videoPlayer = FindObjectOfType<VideoPlayer>();

        // Set the video player's volume based on the saved volume
        if (videoPlayer != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("VideoVolume", 1.0f);
            videoPlayer.SetDirectAudioVolume(0, savedVolume);
        }
    }

    private void ChangeVideoVolume(float volume)
    {
        // Set the video player's volume based on the slider value
        if (videoPlayer != null)
        {
            videoPlayer.SetDirectAudioVolume(0, volume);
        }

        // Update the volume setting in PlayerPrefs
        PlayerPrefs.SetFloat("VideoVolume", volume);
        PlayerPrefs.Save();
    }
}