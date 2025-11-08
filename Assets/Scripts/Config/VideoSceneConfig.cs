using System;
using System.Collections.Generic;

namespace Interactive.Config
{
    [Serializable]
    public class VideoProjectConfig
    {
        public float defaultSeekSeconds = 5f;
        public List<SceneConfig> scenes = new List<SceneConfig>();

        // Global UI/audio polish
        public string decisionSfxFile; // optional: play when a decision becomes available
        public float decisionSfxVolume = 0.8f;
        public string decisionSfxSnippet; // optional: reuse a snippet defined in music.json
        public bool enableBars = true; // cinematic bars when choices appear
        public float barHeightPct = 0.08f; // top/bottom height percentage of screen
        public float barTween = 0.35f; // seconds
    }

    [Serializable]
    public class SceneConfig
    {
        public string name;
        public string windowsLocalPath;
        public List<TimedButtonConfig> buttons = new List<TimedButtonConfig>();
    }

    [Serializable]
    public class TimedButtonConfig
    {
        public string name;            // Button GameObject name in scene
        public float appearTime = 0f;  // Seconds
        public string targetScene;     // Scene to load on click
        public float hideAfterSeconds = -1f; // Optional; <=0 means stay visible

        // Optional UI/runtime-spawn settings (when not using existing scene buttons)
        public bool spawnDynamically = false; // If true, create a button from a prefab via VideoSceneConfigurator
        public string label;           // Optional text label to show on the spawned button
        public float anchorX = -1f;    // 0..1 normalized; -1 means auto layout
        public float anchorY = -1f;    // 0..1 normalized; -1 means auto layout
        public float offsetX = 0f;     // pixel offset from anchor
        public float offsetY = 0f;     // pixel offset from anchor
        public string hotkey;          // Optional keyboard shortcut (e.g., "Q", "E", "F")
        public float fadeIn = 0.35f;   // DOTween fade-in duration
        public float scaleFrom = 0.9f; // DOTween start scale when showing
        public bool pulse = false;     // If true, apply a subtle pulsing loop

        // Auto-select if not pressed after N seconds from appearance (<=0 disabled)
        public float autoSelectAfter = -1f;

        // Gamification metadata
        public int rewardPoints = 0;        // Base points granted when this choice is made
        public int timeBonusPoints = 0;     // Extra points for reacting before timeBonusWindow
        public float timeBonusWindow = 0f;  // Real-time seconds to earn the bonus (<=0 uses default)
        public string badgeId;              // Optional badge id unlocked when chosen
        public string badgeDescription;     // Friendly text for the badge
        public string branchTag;            // Optional string to group choices (hope, despair, etc.)
        public bool countsAsEnding = false; // Flag to treat the choice as an ending discovery
    }
}
