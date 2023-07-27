using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SpeedSliderController : MonoBehaviour
{
    public VideoController videoController;
    public TextMeshProUGUI speedText;
    private Slider slider;

    private void Start()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(OnSliderValueChanged);
        UpdateSpeedText(videoController.GetPlaybackSpeed());
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
