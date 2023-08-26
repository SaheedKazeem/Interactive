using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedSliderController : MonoBehaviour
{
    public VideoController videoController;
    public TextMeshProUGUI speedText, speedwarningText;
    private Slider slider;

    private void Start()
    {
        videoController = GameObject.Find("Video").GetComponent<VideoController>();
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        UpdateSpeedText(videoController.GetPlaybackSpeed());
    }

    void Update()
    {
        videoController = FindObjectOfType<VideoController>();
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
