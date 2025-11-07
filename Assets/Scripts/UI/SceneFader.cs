using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }
    private Canvas canvas;
    private Image img;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (Instance != null) return;
        var go = new GameObject("~SceneFader");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<SceneFader>();
        Instance.Build();
    }

    private void Build()
    {
        canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();
        var go = new GameObject("Panel");
        go.transform.SetParent(transform, false);
        img = go.AddComponent<Image>();
        img.color = new Color(0,0,0,0);
        // Do not block UI clicks by default
        img.raycastTarget = false;
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    public static void FadeAndLoad(string scene, float duration = 0.35f)
    {
        if (Instance == null) Boot();
        Instance.img.DOKill();
        Instance.img.color = new Color(0,0,0,0);
        // Block clicks during fade/transition only
        Instance.img.raycastTarget = true;
        Instance.img.DOFade(1f, Mathf.Max(0.05f, duration)).SetUpdate(true).OnComplete(() =>
        {
            SceneManager.LoadScene(scene);
            Instance.img.DOFade(0f, Mathf.Max(0.05f, duration)).SetUpdate(true).OnComplete(() =>
            {
                // Re-enable clicks after fade completes
                Instance.img.raycastTarget = false;
            });
        });
    }
}
