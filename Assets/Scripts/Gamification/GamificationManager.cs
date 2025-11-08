using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Interactive.Config;

namespace Interactive.Gamification
{
    /// <summary>
    /// Lightweight scoring/achievement tracker that turns the still-photo film into a simple meta-game.
    /// Lives for the entire session and watches scene loads + configured decisions.
    /// </summary>
    public class GamificationManager : MonoBehaviour
    {
        public static GamificationManager Instance { get; private set; }

        [Serializable]
        public class ScoreSnapshot
        {
            public int score;
            public int uniqueScenes;
            public int streak;
            public string currentScene;
            public string lastEvent;
            public string[] recentScenes;
            public string[] badges;
            public float sessionSeconds;
        }

        public class DecisionRuntime
        {
            public string id;
            public string scene;
            public TimedButtonConfig config;
            public float shownRealtime;
            public float timeBonusWindow;
            public int baseReward;
            public int bonusReward;
            public string Label =>
                !string.IsNullOrEmpty(config?.label)
                    ? config.label
                    : (!string.IsNullOrEmpty(config?.name) ? config.name : "Choice");
        }

        public class DecisionResolution
        {
            public DecisionRuntime runtime;
            public int pointsAwarded;
            public bool bonusAwarded;
        }

        [Header("Scoring")]
        [SerializeField] private int sceneDiscoveryPoints = 125;
        [SerializeField] private int revisitPoints = 25;
        [SerializeField] private int defaultDecisionPoints = 60;
        [SerializeField] private int defaultFastBonus = 40;
        [SerializeField] private float defaultFastWindow = 4f;
        [SerializeField] private int badgeBonusPoints = 80;
        [SerializeField] private int endingBonusPoints = 220;

        private readonly Dictionary<string, DecisionRuntime> activeDecisions =
            new Dictionary<string, DecisionRuntime>(StringComparer.OrdinalIgnoreCase);

        private readonly HashSet<string> visitedScenes =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<string> trail = new List<string>();
        private readonly Dictionary<string, string> badgeLookup =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> recentBadges = new List<string>();

        private ScoreSnapshot snapshot = new ScoreSnapshot();
        private int score;
        private int discoveryStreak;
        private string currentScene;
        private string lastEvent;
        private DateTime sessionStart = DateTime.UtcNow;
        private bool skipNextSceneEvent;

        public event Action<ScoreSnapshot> ScoreChanged;
        public event Action<DecisionRuntime> DecisionShown;
        public event Action<DecisionResolution> DecisionResolved;
        public ScoreSnapshot CurrentSnapshot => snapshot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("~GamificationManager");
                DontDestroyOnLoad(go);
                go.AddComponent<GamificationManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            sessionStart = DateTime.UtcNow;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            RegisterSceneEntry(SceneManager.GetActiveScene().name);
            skipNextSceneEvent = true;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                SceneManager.sceneLoaded -= HandleSceneLoaded;
            }
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (skipNextSceneEvent)
            {
                skipNextSceneEvent = false;
                return;
            }
            RegisterSceneEntry(scene.name);
        }

        public void RegisterSceneEntry(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return;
            currentScene = sceneName;
            bool firstTime = visitedScenes.Add(sceneName);
            if (firstTime)
            {
                score += sceneDiscoveryPoints;
                discoveryStreak += 1;
                lastEvent = $"Discovered {sceneName}";
            }
            else
            {
                discoveryStreak = Mathf.Max(0, discoveryStreak - 1);
                score += revisitPoints;
                lastEvent = $"Revisited {sceneName}";
            }

            trail.Add(sceneName);
            if (trail.Count > 6)
                trail.RemoveAt(0);

            PushSnapshot();
        }

        public void NotifyDecisionShown(string sceneName, TimedButtonConfig cfg)
        {
            if (cfg == null) return;
            string id = BuildDecisionKey(sceneName, cfg);
            float window = cfg.timeBonusWindow > 0.01f ? cfg.timeBonusWindow : defaultFastWindow;
            int baseReward = cfg.rewardPoints != 0 ? cfg.rewardPoints : defaultDecisionPoints;
            int bonusReward = cfg.timeBonusPoints != 0 ? cfg.timeBonusPoints : defaultFastBonus;

            var runtime = new DecisionRuntime
            {
                id = id,
                scene = string.IsNullOrEmpty(sceneName) ? currentScene : sceneName,
                config = cfg,
                shownRealtime = Time.unscaledTime,
                timeBonusWindow = Mathf.Max(0f, window),
                baseReward = baseReward,
                bonusReward = Mathf.Max(0, bonusReward)
            };
            activeDecisions[id] = runtime;
            DecisionShown?.Invoke(runtime);
        }

        public void HandleChoiceSelected(string sceneName, TimedButtonConfig cfg)
        {
            if (cfg == null)
            {
                AddGenericChoice(sceneName);
                return;
            }

            string id = BuildDecisionKey(sceneName, cfg);
            if (!activeDecisions.TryGetValue(id, out var runtime))
            {
                runtime = new DecisionRuntime
                {
                    id = id,
                    scene = string.IsNullOrEmpty(sceneName) ? currentScene : sceneName,
                    config = cfg,
                    shownRealtime = Time.unscaledTime,
                    timeBonusWindow = defaultFastWindow,
                    baseReward = cfg.rewardPoints != 0 ? cfg.rewardPoints : defaultDecisionPoints,
                    bonusReward = cfg.timeBonusPoints != 0 ? cfg.timeBonusPoints : defaultFastBonus
                };
            }
            else
            {
                activeDecisions.Remove(id);
            }

            float elapsed = Time.unscaledTime - runtime.shownRealtime;
            bool withinWindow = runtime.timeBonusWindow > 0f && elapsed <= runtime.timeBonusWindow;
            int awarded = runtime.baseReward + (withinWindow ? runtime.bonusReward : 0);
            if (awarded <= 0) awarded = defaultDecisionPoints;

            score += awarded;
            lastEvent = $"Chose {runtime.Label} (+{awarded})";

            if (!string.IsNullOrEmpty(cfg.badgeId))
                UnlockBadge(cfg.badgeId, cfg.badgeDescription, badgeBonusPoints);

            if (cfg.countsAsEnding)
            {
                string endingId = $"ending::{cfg.badgeId ?? cfg.targetScene ?? cfg.name}";
                UnlockBadge(endingId, $"Ending: {runtime.Label}", endingBonusPoints);
            }

            PushSnapshot();

            DecisionResolved?.Invoke(new DecisionResolution
            {
                runtime = runtime,
                pointsAwarded = awarded,
                bonusAwarded = withinWindow
            });
        }

        public bool TryGetActiveDecision(out DecisionRuntime runtime)
        {
            runtime = null;
            float bestRemaining = float.PositiveInfinity;
            foreach (var kvp in activeDecisions)
            {
                var dec = kvp.Value;
                float remaining = dec.timeBonusWindow - (Time.unscaledTime - dec.shownRealtime);
                if (remaining > 0f && remaining < bestRemaining)
                {
                    bestRemaining = remaining;
                    runtime = dec;
                }
            }

            if (runtime == null && activeDecisions.Count > 0)
            {
                // Fall back to any decision so HUD can still show context.
                foreach (var kvp in activeDecisions)
                {
                    runtime = kvp.Value;
                    break;
                }
            }
            return runtime != null;
        }

        private void AddGenericChoice(string sceneName)
        {
            score += defaultDecisionPoints;
            lastEvent = $"Choice made in {sceneName ?? currentScene}";
            PushSnapshot();
        }

        private void UnlockBadge(string id, string description, int bonus)
        {
            if (string.IsNullOrEmpty(id)) return;
            if (badgeLookup.ContainsKey(id)) return;

            string desc = string.IsNullOrEmpty(description) ? id : description;
            badgeLookup[id] = desc;
            recentBadges.Add(desc);
            if (recentBadges.Count > 4)
                recentBadges.RemoveAt(0);

            score += bonus;
            lastEvent = $"Unlocked {desc} (+{bonus})";
        }

        private void PushSnapshot()
        {
            snapshot = new ScoreSnapshot
            {
                score = score,
                uniqueScenes = visitedScenes.Count,
                streak = discoveryStreak,
                currentScene = currentScene,
                lastEvent = lastEvent,
                recentScenes = trail.ToArray(),
                badges = recentBadges.ToArray(),
                sessionSeconds = (float)(DateTime.UtcNow - sessionStart).TotalSeconds
            };
            ScoreChanged?.Invoke(snapshot);
        }

        private static string BuildDecisionKey(string sceneName, TimedButtonConfig cfg)
        {
            string scene = string.IsNullOrEmpty(sceneName)
                ? SceneManager.GetActiveScene().name
                : sceneName;
            string choice = !string.IsNullOrEmpty(cfg?.name)
                ? cfg.name
                : (!string.IsNullOrEmpty(cfg?.label) ? cfg.label : "Choice");
            return $"{scene}|{choice}|{cfg?.appearTime:F2}";
        }
    }
}
