using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using Interactive.Util;

public class VideoVolumeSlider : MonoBehaviour
{
    public Slider volumeSlider;
    public VideoPlayer videoPlayer;
    public VideoController videoController;

    [Header("Keyboard Control")]
    [SerializeField] private KeyCode decreasePrimary = KeyCode.LeftBracket;
    [SerializeField] private KeyCode increasePrimary = KeyCode.RightBracket;
    [SerializeField] private KeyCode decreaseSecondary = KeyCode.Alpha9;
    [SerializeField] private KeyCode increaseSecondary = KeyCode.Alpha0;
    [SerializeField] private float keyboardStepPerSecond = 0.75f;
    [SerializeField] private float keyboardFastMultiplier = 2.5f;

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

    private void Update()
    {
        HandleKeyboardInput();
    }

    private void ChangeVideoVolume(float volume)
    {
        if (videoPlayer == null)
            videoPlayer = SceneObjectFinder.FindFirst<VideoPlayer>(true);
        if (videoPlayer != null)
            videoPlayer.SetDirectAudioVolume(0, volume);

        PlayerPrefs.SetFloat("VideoVolume", volume);
        PlayerPrefs.Save();

        EnsureVideoController();
        videoController?.RefreshAudioPolicy();
    }

    private void ApplySavedVolumeToCurrentScene()
    {
        videoPlayer = SceneObjectFinder.FindFirst<VideoPlayer>(true);
        EnsureVideoController();
        if (videoPlayer != null)
        {
            float savedVolume = PlayerPrefs.GetFloat("VideoVolume", 1.0f);
            videoPlayer.SetDirectAudioVolume(0, savedVolume);
        }
        videoController?.RefreshAudioPolicy();
    }

    private void HandleKeyboardInput()
    {
        if (volumeSlider == null) return;

        float direction = 0f;
        if (Input.GetKey(decreasePrimary) || Input.GetKey(decreaseSecondary))
            direction -= 1f;
        if (Input.GetKey(increasePrimary) || Input.GetKey(increaseSecondary))
            direction += 1f;
        if (Mathf.Approximately(direction, 0f)) return;

        float rate = keyboardStepPerSecond;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            rate *= keyboardFastMultiplier;

        float delta = direction * rate * Time.unscaledDeltaTime;
        float newValue = Mathf.Clamp01(volumeSlider.value + delta);
        volumeSlider.SetValueWithoutNotify(newValue);
        ChangeVideoVolume(newValue);
    }

    private void EnsureVideoController()
    {
        if (videoController == null)
        {
            videoController = SceneObjectFinder.FindFirst<VideoController>(true);
        }
    }
}
