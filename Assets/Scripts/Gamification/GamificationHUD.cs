using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Interactive.Gamification
{
    /// <summary>
    /// Simple overlay that surfaces score, streak, badges, and bonus timers.
    /// Lives for the full session and auto-registers with the manager.
    /// </summary>
    public class GamificationHUD : MonoBehaviour
    {
        private Canvas canvas;
        private Text scoreText;
        private Text badgeText;
        private Text timerText;
        private GamificationManager manager;
        private GamificationManager.ScoreSnapshot snapshot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("~GamificationHUD");
            DontDestroyOnLoad(go);
            go.AddComponent<GamificationHUD>();
        }

        private void Awake()
        {
            Build();
            StartCoroutine(EnsureManager());
        }

        private IEnumerator EnsureManager()
        {
            while (GamificationManager.Instance == null)
            {
                yield return null;
            }

            manager = GamificationManager.Instance;
            manager.ScoreChanged += HandleScoreChanged;
            if (manager.CurrentSnapshot != null)
            {
                HandleScoreChanged(manager.CurrentSnapshot);
            }
        }

        private void OnDestroy()
        {
            if (manager != null)
            {
                manager.ScoreChanged -= HandleScoreChanged;
            }
        }

        private void Update()
        {
            if (timerText == null) return;

            if (GamificationManager.Instance != null &&
                GamificationManager.Instance.TryGetActiveDecision(out var runtime))
            {
                float remaining = runtime.timeBonusWindow - (Time.unscaledTime - runtime.shownRealtime);
                if (remaining > 0f)
                    timerText.text = $"Bonus window: {remaining:0.0}s";
                else
                    timerText.text = string.Empty;
            }
            else
            {
                timerText.text = string.Empty;
            }
        }

        private void HandleScoreChanged(GamificationManager.ScoreSnapshot snap)
        {
            snapshot = snap;
            RefreshText();
        }

        private void RefreshText()
        {
            if (snapshot == null || scoreText == null) return;

            var sb = new StringBuilder();
            sb.AppendLine($"Score: {snapshot.score}");
            sb.AppendLine($"Unique scenes: {snapshot.uniqueScenes}");
            sb.AppendLine($"Discovery streak: {snapshot.streak}");
            if (!string.IsNullOrEmpty(snapshot.currentScene))
                sb.AppendLine($"Current: {snapshot.currentScene}");
            if (!string.IsNullOrEmpty(snapshot.lastEvent))
                sb.AppendLine(snapshot.lastEvent);
            if (snapshot.recentScenes != null && snapshot.recentScenes.Length > 0)
            {
                sb.AppendLine("Trail:");
                sb.AppendLine(string.Join(" > ", snapshot.recentScenes));
            }
            scoreText.text = sb.ToString();

            if (badgeText != null)
            {
                if (snapshot.badges != null && snapshot.badges.Length > 0)
                {
                    badgeText.text = "Badges: " + string.Join(", ", snapshot.badges);
                }
                else
                {
                    badgeText.text = string.Empty;
                }
            }
        }

        private void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();

            var panel = new GameObject("Panel", typeof(Image));
            panel.transform.SetParent(transform, false);
            var img = panel.GetComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.6f);
            var panelRt = panel.GetComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(1f, 1f);
            panelRt.anchorMax = new Vector2(1f, 1f);
            panelRt.pivot = new Vector2(1f, 1f);
            panelRt.anchoredPosition = new Vector2(-24f, -24f);
            panelRt.sizeDelta = new Vector2(380f, 260f);

            scoreText = CreateText(panel.transform, "ScoreText", new Vector2(0f, 0f));
            scoreText.alignment = TextAnchor.UpperLeft;
            scoreText.horizontalOverflow = HorizontalWrapMode.Wrap;
            scoreText.verticalOverflow = VerticalWrapMode.Truncate;

            badgeText = CreateText(panel.transform, "BadgeText", new Vector2(0f, -160f));
            badgeText.alignment = TextAnchor.UpperLeft;
            badgeText.horizontalOverflow = HorizontalWrapMode.Wrap;

            timerText = CreateText(transform, "TimerText", Vector2.zero);
            var timerRt = timerText.GetComponent<RectTransform>();
            timerRt.anchorMin = new Vector2(0.5f, 1f);
            timerRt.anchorMax = new Vector2(0.5f, 1f);
            timerRt.pivot = new Vector2(0.5f, 1f);
            timerRt.anchoredPosition = new Vector2(0f, -30f);
            timerRt.sizeDelta = new Vector2(420f, 40f);
            timerText.alignment = TextAnchor.UpperCenter;
        }

        private Text CreateText(Transform parent, string name, Vector2 anchoredPos)
        {
            var go = new GameObject(name, typeof(Text));
            go.transform.SetParent(parent, false);
            var txt = go.GetComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 18;
            txt.color = new Color(1f, 1f, 1f, 0.9f);
            var rt = txt.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(360f, 140f);
            return txt;
        }
    }
}
