using System;
using System.Collections.Generic;

namespace Interactive.Audio
{
    [Serializable]
    public class MusicProjectConfig
    {
        public List<SceneMusicConfig> scenes = new List<SceneMusicConfig>();
        // Optional global playlist that plays across all scenes
        public List<MusicCue> playlist = new List<MusicCue>();
    }

    [Serializable]
    public class SceneMusicConfig
    {
        public string name;
        public List<MusicCue> cues = new List<MusicCue>();
    }

    [Serializable]
    public class MusicCue
    {
        public string file;                 // Absolute path or file:/// URL (mp3/ogg/wav)
        public bool startOnSceneLoad = false;
        public float startAtVideoTime = -1f; // Start when VideoPlayer.time >= value (seconds)
        public float stopAtVideoTime = -1f;  // Optional auto-stop time (seconds)
        public float volume = 1f;
        public float fadeIn = 0.75f;
        public float fadeOut = 0.75f;
        public bool loop = true;
    }
}
