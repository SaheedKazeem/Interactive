using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-instantiated top/bottom black bars for cinematic effect. No setup needed.
/// </summary>
public class CinematicBars : MonoBehaviour
{
    public static CinematicBars Ensure()
    {
        var existing = FindObjectOfType<CinematicBars>();
        if (existing != null) return existing;
        var go = new GameObject("~CinematicBars");
        DontDestroyOnLoad(go);
        return go.AddComponent<CinematicBars>();
    }

    private RectTransform top, bottom;
    private Canvas canvas;
    private float heightPct = 0.08f;
    private float tween = 0.35f;
    private bool visible;

    private void Awake()
    {
        Build();
    }

    private void Build()
    {
        if (canvas != null) return;
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // on top
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        top = CreateBar("Top");
        bottom = CreateBar("Bottom");
        LayoutBars(0f); // hidden initially
    }

    private RectTransform CreateBar(string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var img = go.AddComponent<Image>();
        img.color = Color.black;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        return rt;
    }

    private void LayoutBars(float hPct)
    {
        float screenH = Screen.height;
        float barH = screenH * Mathf.Clamp01(hPct);
        if (top)
        {
            top.anchoredPosition = Vector2.zero;
            top.sizeDelta = new Vector2(0, barH);
        }
        if (bottom)
        {
            bottom.anchorMin = new Vector2(0, 0);
            bottom.anchorMax = new Vector2(1, 0);
            bottom.pivot = new Vector2(0.5f, 0f);
            bottom.anchoredPosition = Vector2.zero;
            bottom.sizeDelta = new Vector2(0, barH);
        }
    }

    public void Configure(float heightPercent, float tweenSeconds)
    {
        heightPct = heightPercent;
        tween = tweenSeconds;
        if (!visible) LayoutBars(0f); else LayoutBars(heightPct);
    }

    public void Show()
    {
        Build();
        visible = true;
        DOTween.Kill(top);
        DOTween.Kill(bottom);
        float target = heightPct;
        DOTween.To(() => 0f, v => LayoutBars(v), target, Mathf.Max(0.05f, tween)).SetUpdate(true);
    }

    public void Hide()
    {
        if (top == null || bottom == null) return;
        visible = false;
        DOTween.Kill(top);
        DOTween.Kill(bottom);
        DOTween.To(() => heightPct, v => LayoutBars(v), 0f, Mathf.Max(0.05f, tween)).SetUpdate(true);
    }
}

