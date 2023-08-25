using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManagerScript : MonoBehaviour
{
    public static GameManagerScript Instance { get; private set; }

    // Volume setting
    private float videoVolume = 1.0f; // Default volume

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public float GetVideoVolume()
    {
        return videoVolume;
    }

    public void SetVideoVolume(float volume)
    {
        videoVolume = volume;
        PlayerPrefs.SetFloat("VideoVolume", volume);
        PlayerPrefs.Save();
    }
}
