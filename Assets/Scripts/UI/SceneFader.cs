using System;
using System.Collections;
using System.IO;
using DG.Tweening;
using Interactive.Transitions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance { get; private set; }
    private Canvas canvas;
    private Image img;
    private Coroutine fadeRoutine;
    private const float MinFadeDuration = 0.05f;

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

    public static void FadeAndLoad(string scene)
    {
        FadeAndLoad(scene, (SceneTransitionOptions)null);
    }

    public static void FadeAndLoad(string scene, SceneTransitionOptions overrides)
    {
        if (Instance == null) Boot();
        var options = SceneTransitionLibrary.Resolve(scene, overrides);
        Instance.StartFade(() => SceneManager.LoadSceneAsync(scene), options);
    }

    public static void FadeAndLoad(string scene, float fadeOut, float fadeIn = -1f)
    {
        var overrides = SceneTransitionOptions.FromDurations(fadeOut, fadeIn > 0f ? fadeIn : fadeOut);
        FadeAndLoad(scene, overrides);
    }

    public static void FadeAndLoad(int buildIndex)
    {
        FadeAndLoad(buildIndex, (SceneTransitionOptions)null);
    }

    public static void FadeAndLoad(int buildIndex, SceneTransitionOptions overrides)
    {
        if (Instance == null) Boot();
        var targetName = ResolveBuildIndexName(buildIndex);
        var options = SceneTransitionLibrary.Resolve(targetName, overrides);
        Instance.StartFade(() => SceneManager.LoadSceneAsync(buildIndex), options);
    }

    public static void FadeAndLoad(int buildIndex, float fadeOut, float fadeIn = -1f)
    {
        var overrides = SceneTransitionOptions.FromDurations(fadeOut, fadeIn > 0f ? fadeIn : fadeOut);
        FadeAndLoad(buildIndex, overrides);
    }

    private static string ResolveBuildIndexName(int buildIndex)
    {
        var path = SceneUtility.GetScenePathByBuildIndex(buildIndex);
        if (string.IsNullOrEmpty(path)) return null;
        return Path.GetFileNameWithoutExtension(path);
    }

    private void StartFade(Func<AsyncOperation> loader, SceneTransitionOptions options)
    {
        if (img == null) Build();
        options ??= SceneTransitionOptions.Default;
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        img.DOKill();
        fadeRoutine = StartCoroutine(CoFadeAndLoad(loader, options));
    }

    private IEnumerator CoFadeAndLoad(Func<AsyncOperation> loader, SceneTransitionOptions options)
    {
        img.raycastTarget = true;
        var color = options.color;
        color.a = 0f;
        img.color = color;
        yield return img.DOFade(1f, Mathf.Max(MinFadeDuration, options.fadeOut)).SetEase(options.fadeOutEase).SetUpdate(true).WaitForCompletion();

        var op = loader?.Invoke();
        if (op == null)
        {
            Debug.LogWarning("SceneFader received a null async operation; aborting fade.");
            yield return img.DOFade(0f, Mathf.Max(MinFadeDuration, options.fadeIn)).SetEase(options.fadeInEase).SetUpdate(true).WaitForCompletion();
            img.raycastTarget = false;
            fadeRoutine = null;
            yield break;
        }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f)
        {
            yield return null; // wait for assets to stream
        }

        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            yield return null;
        }

        if (options.holdAfterLoad > 0f)
            yield return new WaitForSecondsRealtime(options.holdAfterLoad);

        yield return img.DOFade(0f, Mathf.Max(MinFadeDuration, options.fadeIn)).SetEase(options.fadeInEase).SetUpdate(true).WaitForCompletion();
        img.raycastTarget = false;
        fadeRoutine = null;
    }
}
