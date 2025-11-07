using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Interactive.Config;
using DG.Tweening;

/// <summary>
/// Applies per-scene configuration: video path for VideoSourceResolver and
/// timed button appearances that jump to target scenes.
/// Add this once per scene (any GameObject). It auto-discovers VideoPlayer and buttons by name.
/// </summary>
public class VideoSceneConfigurator : MonoBehaviour
{
    [Tooltip("Optional explicit VideoPlayer; if null, will FindObjectOfType.")]
    public VideoPlayer videoPlayer;

    [Tooltip("Optional explicit resolver; if null, will GetComponent on the VideoPlayer.")]
    public VideoSourceResolver resolver;

    private SceneConfig sceneConfig;
    private readonly Dictionary<string, TimedButtonState> buttonStates = new Dictionary<string, TimedButtonState>();
    private bool setupButtons;

    private class TimedButtonState
    {
        public TimedButtonConfig cfg;
        public GameObject go;
        public Button button;
        public bool shown;
        public float shownAt;
        public bool spawned; // true when created dynamically
        public bool autoScheduled;
    }

    private void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = Interactive.Util.SceneObjectFinder.FindFirst<VideoPlayer>(true);
        if (resolver == null && videoPlayer != null)
            resolver = videoPlayer.GetComponent<VideoSourceResolver>();

        var projectConfig = VideoSceneConfigLoader.Load();
        string sceneName = SceneManager.GetActiveScene().name;
        sceneConfig = projectConfig.scenes.Find(s => string.Equals(s.name, sceneName, System.StringComparison.OrdinalIgnoreCase));

        if (sceneConfig != null)
        {
            if (resolver != null)
            {
                resolver.windowsLocalPath = sceneConfig.windowsLocalPath;
                resolver.ApplyConfiguredSource();
            }
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            else if (videoPlayer != null && !string.IsNullOrWhiteSpace(sceneConfig.windowsLocalPath))
            {
                // Fallback: apply URL directly if resolver is not present
                string path = sceneConfig.windowsLocalPath.Trim();
                string url = VideoSourceResolver_ToVideoUrl(path);
                if (!string.IsNullOrEmpty(url))
                {
                    videoPlayer.source = VideoSource.Url;
                    videoPlayer.url = url;
                    if (!videoPlayer.isPrepared) videoPlayer.Prepare();
                }
            }
#endif
        }

        // Apply music cues for this scene
        try
        {
            Interactive.Audio.MusicDirector.Instance?.ApplyForScene(sceneName, videoPlayer);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"VideoSceneConfigurator: couldn't apply music cues: {e.Message}");
        }
    }

    private void Start()
    {
        if (sceneConfig == null) return;
        SetupTimedButtons();
        if (videoPlayer != null)
        {
            // Ensure we can query time safely
            if (!videoPlayer.isPrepared)
                videoPlayer.Prepare();
        }

        // Apply default seek seconds to VideoController, if present
        var vc = Interactive.Util.SceneObjectFinder.FindFirst<VideoController>(true);
        if (vc != null)
        {
            var projectConfig = VideoSceneConfigLoader.Load();
            if (projectConfig != null && projectConfig.defaultSeekSeconds > 0f)
            {
                vc.SetSeekSeconds(projectConfig.defaultSeekSeconds);
            }
        }
    }

    [Header("Dynamic Choice UI (optional)")]
    [Tooltip("Container for dynamically spawned choice buttons (e.g., a Canvas panel)")]
    public RectTransform dynamicContainer;

    [Tooltip("Button prefab used when a config entry sets spawnDynamically=true. The prefab should contain a Button, Text/TMP for label, and a CanvasGroup for fade.")]
    public Button dynamicButtonPrefab;

    private bool barsShownThisScene;

    [Header("Control Options")]
    [Tooltip("If true, the configurator will hide/show existing scene buttons based on timings. If false, it keeps your buttons as-is and only triggers bars/SFX at the times.")]
    public bool manageExistingButtons = false;

    private void SetupTimedButtons()
    {
        if (sceneConfig.buttons == null || sceneConfig.buttons.Count == 0)
            return;

        foreach (var cfg in sceneConfig.buttons)
        {
            // Dynamic spawn path
            if (cfg.spawnDynamically && dynamicContainer != null && dynamicButtonPrefab != null)
            {
                var buttonObj = Instantiate(dynamicButtonPrefab, dynamicContainer);
                buttonObj.gameObject.name = string.IsNullOrEmpty(cfg.name) ? (cfg.label ?? "Choice") : cfg.name;
                // Text label (Text or TextMeshProUGUI)
                var txt = buttonObj.GetComponentInChildren<UnityEngine.UI.Text>();
                if (txt != null && !string.IsNullOrEmpty(cfg.label)) txt.text = cfg.label;
#if TMP_PRESENT
                var tmp = buttonObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null && !string.IsNullOrEmpty(cfg.label)) tmp.text = cfg.label;
#endif
                var rt = buttonObj.GetComponent<RectTransform>();
                if (cfg.anchorX >= 0 && cfg.anchorY >= 0)
                {
                    rt.anchorMin = rt.anchorMax = new Vector2(Mathf.Clamp01(cfg.anchorX), Mathf.Clamp01(cfg.anchorY));
                    rt.anchoredPosition = new Vector2(cfg.offsetX, cfg.offsetY);
                }

                var go = buttonObj.gameObject;
                var btn = buttonObj.GetComponent<Button>();
                PrepareButtonForShow(go, btn);
                WireButtonAction(btn, cfg.targetScene);

                buttonStates[go.name] = new TimedButtonState
                {
                    cfg = cfg,
                    go = go,
                    button = btn,
                    shown = false,
                    shownAt = 0f,
                    spawned = true
                };
            }
            else
            {
                // Pre-existing scene button path
                if (string.IsNullOrWhiteSpace(cfg.name)) continue;
                var go = GameObject.Find(cfg.name);
                if (go == null)
                {
                    Debug.LogWarning($"VideoSceneConfigurator: Button GameObject '{cfg.name}' not found in scene '{sceneConfig.name}'.");
                    continue;
                }
                var btn = go.GetComponent<Button>();
                if (btn == null)
                {
                    Debug.LogWarning($"VideoSceneConfigurator: GameObject '{cfg.name}' lacks a Button component.");
                    continue;
                }
                if (manageExistingButtons)
                    PrepareButtonForShow(go, btn);
                WireButtonAction(btn, cfg.targetScene);

                buttonStates[cfg.name] = new TimedButtonState
                {
                    cfg = cfg,
                    go = go,
                    button = btn,
                    shown = !manageExistingButtons && go.activeSelf, // respect current state if not managing
                    shownAt = 0f,
                    spawned = false
                };
            }
        }

        setupButtons = true;
    }

    private static void WireButtonAction(Button btn, string target)
    {
        // If target is empty, respect existing listeners set up in the scene.
        if (string.IsNullOrEmpty(target)) return;

        // Otherwise, we own the click and navigate to the configured scene.
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() =>
        {
            var bars = Interactive.Util.SceneObjectFinder.FindFirst<CinematicBars>(true);
            if (bars != null) bars.Hide();
            // Fade out then load for polish
            SceneFader.FadeAndLoad(target, 0.35f);
        });
    }

    private static void PrepareButtonForShow(GameObject go, Button btn)
    {
        go.SetActive(false);
        // Ensure a CanvasGroup for fade
        var cg = go.GetComponent<CanvasGroup>();
        if (cg == null) cg = go.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        // Reset any scale
        var rt = go.transform as RectTransform;
        if (rt != null) rt.localScale = Vector3.one;
    }

    private void Update()
    {
        if (!setupButtons || videoPlayer == null) return;
        if (!videoPlayer.isPrepared) return;

        double t = videoPlayer.time;
        foreach (var kv in buttonStates)
        {
            var state = kv.Value;
            // If we are not managing existing buttons, just use timing to trigger global polish (bars/SFX) once.
            if (!state.spawned && !manageExistingButtons)
            {
                if (!state.shown && t >= state.cfg.appearTime)
                {
                    TryTriggerBarsAndSfxOnce();
                    state.shown = true;
                    state.shownAt = (float)t;
                }
                continue;
            }
            if (!state.shown && t >= state.cfg.appearTime)
            {
                state.go.SetActive(true);
                // Cinematic bars and decision SFX on first decision in scene
                TryTriggerBarsAndSfxOnce();
                // DOTween show animation
                var cg = state.go.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.DOKill();
                    cg.alpha = 0f;
                    cg.DOFade(1f, Mathf.Max(0.05f, state.cfg.fadeIn));
                }
                var rt = state.go.transform as RectTransform;
                if (rt != null)
                {
                    float from = Mathf.Clamp(state.cfg.scaleFrom, 0.01f, 2f);
                    rt.localScale = Vector3.one * from;
                    rt.DOScale(1f, Mathf.Max(0.05f, state.cfg.fadeIn)).SetEase(Ease.OutBack);
                    if (state.cfg.pulse)
                    {
                        rt.DOScale(1.04f, 0.8f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
                    }
                }
                state.shown = true;
                state.shownAt = (float)t;
                if (state.cfg.autoSelectAfter > 0f && !state.autoScheduled)
                {
                    state.autoScheduled = true;
                    StartCoroutine(AutoSelectAfter(state, state.cfg.autoSelectAfter));
                }
            }
            else if (state.shown && state.cfg.hideAfterSeconds > 0f)
            {
                if ((float)t >= state.shownAt + state.cfg.hideAfterSeconds)
                {
                    // Fade out then deactivate
                    var cg = state.go.GetComponent<CanvasGroup>();
                    if (cg != null)
                    {
                        cg.DOKill();
                        cg.DOFade(0f, 0.25f).OnComplete(() => state.go.SetActive(false));
                    }
                    else
                    {
                        state.go.SetActive(false);
                    }
                }
            }
        }

        // Hotkeys
        foreach (var kv in buttonStates)
        {
            var state = kv.Value;
            if (!state.shown || string.IsNullOrEmpty(state.cfg.hotkey)) continue;
            if (TryGetKey(state.cfg.hotkey, out var key) && Input.GetKeyDown(key))
            {
                state.button.onClick?.Invoke();
                break;
            }
        }
    }

    private void TryTriggerBarsAndSfxOnce()
    {
        var projectConfig = VideoSceneConfigLoader.Load();
        if (projectConfig != null && projectConfig.enableBars && !barsShownThisScene)
        {
            barsShownThisScene = true;
            var bars = CinematicBars.Ensure();
            bars.Configure(projectConfig.barHeightPct, projectConfig.barTween);
            bars.Show();
        }
        if (projectConfig != null && !string.IsNullOrEmpty(projectConfig.decisionSfxFile))
        {
            Interactive.Audio.SfxPlayer.Ensure().PlayOneShot(projectConfig.decisionSfxFile, projectConfig.decisionSfxVolume);
        }
    }

    private System.Collections.IEnumerator AutoSelectAfter(TimedButtonState state, float afterSeconds)
    {
        float start = state.shownAt;
        while (true)
        {
            // Abort if button was hidden or destroyed
            if (state == null || state.go == null || !state.go.activeInHierarchy)
                yield break;
            // Use unscaled time so menu pauses don't affect it
            yield return new WaitForSecondsRealtime(afterSeconds);
            // If still visible, click it
            if (state.go != null && state.go.activeInHierarchy)
            {
                state.button?.onClick?.Invoke();
            }
            yield break;
        }
    }

    private static bool TryGetKey(string s, out KeyCode key)
    {
        key = KeyCode.None;
        if (string.IsNullOrEmpty(s)) return false;
        s = s.Trim();
        // Allow direct KeyCode names
        if (System.Enum.TryParse<KeyCode>(s, true, out var parsed))
        {
            key = parsed; return true;
        }
        // Single character -> alpha key
        if (s.Length == 1)
        {
            char c = char.ToUpperInvariant(s[0]);
            if (c >= 'A' && c <= 'Z') { key = KeyCode.A + (c - 'A'); return true; }
            if (c >= '0' && c <= '9') { key = KeyCode.Alpha0 + (c - '0'); return true; }
        }
        return false;
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    // Minimal duplication of VideoSourceResolver.ToVideoUrl for fallback behavior.
    private static string VideoSourceResolver_ToVideoUrl(string path)
    {
        if (string.IsNullOrEmpty(path)) return path;
        if (path.StartsWith("http://") || path.StartsWith("https://") || path.StartsWith("file:///"))
            return path;
        string normalized = path.Replace('\\', '/');
        if (normalized.Length > 1 && normalized[1] == ':')
        {
            return $"file:///{normalized}";
        }
        string abs = System.IO.Path.GetFullPath(path).Replace('\\', '/');
        return $"file:///{abs}";
    }
#endif
}
