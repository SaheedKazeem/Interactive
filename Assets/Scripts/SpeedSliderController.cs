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

    [Header("Keyboard Control")]
    [SerializeField] private KeyCode decreasePrimary = KeyCode.LeftArrow;
    [SerializeField] private KeyCode increasePrimary = KeyCode.RightArrow;
    [SerializeField] private KeyCode decreaseSecondary = KeyCode.Comma;
    [SerializeField] private KeyCode increaseSecondary = KeyCode.Period;
    [SerializeField] private KeyCode decreaseTertiary = KeyCode.Minus;
    [SerializeField] private KeyCode increaseTertiary = KeyCode.Equals;
    [SerializeField] private float keyboardStepPerSecond = 1f;
    [SerializeField] private float keyboardFastMultiplier = 2f;

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
        if (slider == null) return;

        HandleKeyboardInput();

        if (videoController == null) return;

        SyncSliderRange();

        float current = videoController.GetPlaybackSpeed();
        if (Mathf.Abs(slider.value - current) > SliderSyncEpsilon)
        {
            slider.SetValueWithoutNotify(current);
        }
        UpdateSpeedText(current);
        UpdateWarning(current);
    }

    private void OnSliderValueChanged(float value)
    {
        if (videoController == null) return;

        videoController.SetPlaybackSpeed(value);

        float applied = videoController.GetPlaybackSpeed();
        if (slider != null)
        {
            slider.SetValueWithoutNotify(applied);
        }
        UpdateSpeedText(applied);
        UpdateWarning(applied);
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
            SyncSliderRange();
            float speed = videoController.GetPlaybackSpeed();
            slider.SetValueWithoutNotify(speed);
            UpdateSpeedText(speed);
            UpdateWarning(speed);
        }
    }

    private void HandleKeyboardInput()
    {
        float direction = 0f;
        if (Input.GetKey(decreasePrimary) || Input.GetKey(decreaseSecondary) || Input.GetKey(decreaseTertiary))
            direction -= 1f;
        if (Input.GetKey(increasePrimary) || Input.GetKey(increaseSecondary) || Input.GetKey(increaseTertiary))
            direction += 1f;
        if (Mathf.Approximately(direction, 0f) || slider == null) return;

        float rate = keyboardStepPerSecond;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            rate *= keyboardFastMultiplier;

        float delta = direction * rate * Time.unscaledDeltaTime;
        slider.value = Mathf.Clamp(slider.value + delta, slider.minValue, slider.maxValue);
    }

    private void SyncSliderRange()
    {
        if (videoController == null || slider == null) return;
        videoController.GetPlaybackSpeedRange(out float min, out float max);
        bool changed = false;
        if (!Mathf.Approximately(slider.minValue, min))
        {
            slider.minValue = min;
            changed = true;
        }
        if (!Mathf.Approximately(slider.maxValue, max))
        {
            slider.maxValue = max;
            changed = true;
        }
        if (changed)
        {
            float clamped = Mathf.Clamp(slider.value, min, max);
            slider.SetValueWithoutNotify(clamped);
        }
    }

    private void UpdateWarning(float speed)
    {
        if (speedwarningText == null || videoController == null) return;
        bool shouldShow = speed >= videoController.GetAudioMuteThreshold();
        if (speedwarningText.gameObject.activeSelf != shouldShow)
        {
            speedwarningText.gameObject.SetActive(shouldShow);
        }
    }
}
