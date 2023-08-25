using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedSliderController : MonoBehaviour
{
    public VideoController videoController;
    public TextMeshProUGUI speedText;
    private Slider slider;

    private void Start()
    {   videoController = GameObject.Find("Video").GetComponent<VideoController>();
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
        videoController.SetPlaybackSpeed(value);
        UpdateSpeedText(value);
    }

    private void UpdateSpeedText(float value)
    {
        speedText.text = "Playback Speed: " + value.ToString("F1") + "x";
    }
}
