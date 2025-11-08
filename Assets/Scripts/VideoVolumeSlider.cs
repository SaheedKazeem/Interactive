using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System;
using Interactive.Util;

public class VideoVolumeSlider : MonoBehaviour
{
    public Slider volumeSlider;
    public VideoPlayer videoPlayer;

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("VideoVolume", 1.0f);
        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(savedVolume);
            volumeSlider.onValueChanged.AddListener(ChangeVideoVolume);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        ApplySavedVolumeToCurrentScene();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(ChangeVideoVolume);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySavedVolumeToCurrentScene();
    }

    private void ChangeVideoVolume(float volume)
    {
        if (videoPlayer == null)
            videoPlayer = SceneObjectFinder.FindFirst<VideoPlayer>(true);
        if (videoPlayer != null)
            videoPlayer.SetDirectAudioVolume(0, volume);

        PlayerPrefs.SetFloat("VideoVolume", volume);
        PlayerPrefs.Save();
    }

    private void ApplySavedVolumeToCurrentScene()
    {
        videoPlayer = SceneObjectFinder.FindFirst<VideoPlayer>(true);
        if (videoPlayer != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("VideoVolume", 1.0f);
            videoPlayer.SetDirectAudioVolume(0, savedVolume);
        }
    }
}
