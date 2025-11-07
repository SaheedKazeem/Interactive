using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using Interactive.Config;

/// <summary>
/// Minimal overlay to show scene name, video time, and next configured decision times.
/// Toggle with F10.
/// </summary>
public class DebugHUD : MonoBehaviour
{
    private Canvas canvas;
    private Text text;
    private bool visible;
    private VideoPlayer vp;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        var go = new GameObject("~DebugHUD");
        DontDestroyOnLoad(go);
        go.AddComponent<DebugHUD>();
    }

    private void Awake()
    {
        Build();
        SceneManager.sceneLoaded += (_, __) => vp = Interactive.Util.SceneObjectFinder.FindFirst<VideoPlayer>(true);
        vp = Interactive.Util.SceneObjectFinder.FindFirst<VideoPlayer>(true);
        SetVisible(false);
    }

    private void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(transform, false);
        text = txtGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.alignment = TextAnchor.UpperLeft;
        text.color = new Color(1f, 1f, 1f, 0.9f);
        var rt = text.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(8, -8);
        rt.sizeDelta = new Vector2(800, 200);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10)) SetVisible(!visible);
        if (!visible) return;

        string scene = SceneManager.GetActiveScene().name;
        double t = (vp != null && vp.isPrepared) ? vp.time : 0;
        double len = (vp != null && vp.isPrepared) ? vp.length : 0;

        var cfg = VideoSceneConfigLoader.Load();
        var sc = cfg.scenes.FirstOrDefault(s => string.Equals(s.name, scene, System.StringComparison.OrdinalIgnoreCase));
        string choiceText = "";
        if (sc != null && sc.buttons != null && sc.buttons.Count > 0)
        {
            var upcoming = sc.buttons.OrderBy(b => b.appearTime).Select(b => $"{b.name} @ {b.appearTime:0.0}s -> {b.targetScene}");
            choiceText = string.Join("\n", upcoming);
        }

        text.text = $"Scene: {scene}\nVideo: {t:0.0}/{len:0.0}s\nChoices: \n{choiceText}";
    }

    private void SetVisible(bool on)
    {
        visible = on;
        canvas.enabled = on;
    }
}
