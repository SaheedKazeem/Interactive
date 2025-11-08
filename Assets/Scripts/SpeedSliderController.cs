using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SpeedSliderController : MonoBehaviour
{
    public VideoController videoController;
    public TextMeshProUGUI speedText, speedwarningText;
    private Slider slider;
    private const float SliderSyncEpsilon = 0.01f;

    private void Start()
    {
        slider = GetComponent<Slider>();
        if (slider != null)
        {
            slider.onValueChanged.AddListener(OnSliderValueChanged);
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        BindVideoController();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (slider != null)
            slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindVideoController();
    }

    void Update()
    {
        if (videoController == null || slider == null) return;

        float current = videoController.GetPlaybackSpeed();
        if (Mathf.Abs(slider.value - current) > SliderSyncEpsilon)
        {
            slider.SetValueWithoutNotify(current);
        }
        UpdateSpeedText(current);
    }

    private void OnSliderValueChanged(float value)
    {
        if (videoController == null) return;

        videoController.SetPlaybackSpeed(value);

        // Check if the VideoController object has the "AHeavy" tag
        if (speedwarningText != null && videoController.gameObject.CompareTag("AHeavy"))
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
        if (speedText != null)
            speedText.text = "Playback Speed: " + value.ToString("F1") + "x";
    }

    private void BindVideoController()
    {
        videoController = Interactive.Util.SceneObjectFinder.FindFirst<VideoController>(true);
        if (videoController != null && slider != null)
        {
            float speed = videoController.GetPlaybackSpeed();
            slider.SetValueWithoutNotify(speed);
            UpdateSpeedText(speed);
        }
    }
}
