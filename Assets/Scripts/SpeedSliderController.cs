using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SpeedSliderController : MonoBehaviour
{
    public VideoController videoController;
    public TextMeshProUGUI speedText, speedwarningText;
    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnSliderValueChanged);

        SceneManager.sceneLoaded += OnSceneLoaded;
        // Try to bind immediately if present in current scene
        videoController = Interactive.Util.SceneObjectFinder.FindFirst<VideoController>(true);
        if (videoController != null)
        {
            UpdateSpeedText(videoController.GetPlaybackSpeed());
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        videoController = Interactive.Util.SceneObjectFinder.FindFirst<VideoController>(true);
        if (videoController != null)
        {
            UpdateSpeedText(videoController.GetPlaybackSpeed());
        }
    }

    void Update()
    {
        if (videoController != null)
        {
            UpdateSpeedText(videoController.GetPlaybackSpeed());
        }
    }

    private void OnSliderValueChanged(float value)
    {
        // Update the playback speed in the VideoController
        videoController.SetPlaybackSpeed(value);

        // Check if the VideoController object has the "AHeavy" tag
        if (videoController.gameObject.CompareTag("AHeavy"))
        {
            // Mute the audio for "AHeavy" objects with playback speed greater than 3.5
            if (value > 2)
            {
                videoController.SetAudioVolume(0);
                speedwarningText.gameObject.SetActive(true);
                
            }
            else
            {
                videoController.SetAudioVolumeSaved();
                speedwarningText.gameObject.SetActive(false);
            }
        }

        UpdateSpeedText(value);
    }

    private void UpdateSpeedText(float value)
    {
        speedText.text = "Playback Speed: " + value.ToString("F1") + "x";
    }
}
