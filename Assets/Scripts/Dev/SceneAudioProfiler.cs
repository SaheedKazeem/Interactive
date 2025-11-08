using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Interactive.Tools
{
    /// <summary>
    /// Runtime utility that samples the global audio mix (via AudioListener) and reports
    /// average RMS levels per scene so you can find videos that are mostly silent.
    /// Attach this to any scene (or a bootstrap object) and optionally make it DontDestroyOnLoad.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public class SceneAudioProfiler : MonoBehaviour
    {
        [Tooltip("Seconds between RMS samples.")]
        public float sampleInterval = 0.5f;

        [Tooltip("Number of spectrum samples to request from AudioListener. Higher = more accurate but more CPU.")]
        public int sampleSize = 1024;

        [Tooltip("Key that dumps the collected stats to the Console and optional CSV file.")]
        public KeyCode dumpKey = KeyCode.F9;

        [Tooltip("Automatically dump stats whenever the active scene changes.")]
        public bool dumpOnSceneChange = false;

        [Tooltip("Dump stats when the application quits (Play Mode exit).")]
        public bool dumpOnQuit = true;

        [Tooltip("Also write a CSV report to Application.persistentDataPath.")]
        public bool writeCsv = true;

        [Tooltip("CSV filename relative to Application.persistentDataPath.")]
        public string csvFileName = "scene_audio_log.csv";

        [Tooltip("Reset collected stats after each dump.")]
        public bool clearAfterDump = false;

        private readonly Dictionary<string, SceneStats> stats = new Dictionary<string, SceneStats>();
        private float timer;
        private float[] buffer;
        private Scene currentScene;

        private void OnEnable()
        {
            currentScene = SceneManager.GetActiveScene();
            SceneManager.activeSceneChanged += HandleSceneChanged;
            buffer = new float[Mathf.Max(64, sampleSize)];
            timer = sampleInterval;
        }

        private void OnDisable()
        {
            SceneManager.activeSceneChanged -= HandleSceneChanged;
        }

        private void Update()
        {
            if (buffer == null || buffer.Length != sampleSize)
                buffer = new float[Mathf.Max(64, sampleSize)];

            timer -= Time.unscaledDeltaTime;
            if (timer <= 0f)
            {
                timer = sampleInterval;
                Sample();
            }

            if (Input.GetKeyDown(dumpKey))
                DumpStats("Manual dump");
        }

        private void HandleSceneChanged(Scene oldScene, Scene newScene)
        {
            currentScene = newScene;
            if (dumpOnSceneChange)
                DumpStats($"Scene changed to {newScene.name}");
        }

        private void Sample()
        {
            if (!Application.isPlaying) return;
            if (!AudioListener.pause)
            {
                AudioListener.GetOutputData(buffer, 0);
                double sum = 0d;
                for (int i = 0; i < buffer.Length; i++)
                {
                    float s = buffer[i];
                    sum += s * s;
                }
                float rms = Mathf.Sqrt((float)(sum / buffer.Length));
                AddSample(currentScene.name, rms);
            }
        }

        private void AddSample(string sceneName, float rms)
        {
            if (!stats.TryGetValue(sceneName, out var s))
            {
                s = new SceneStats();
                stats[sceneName] = s;
            }
            s.count++;
            s.sum += rms;
            if (rms > s.peak) s.peak = rms;
        }

        private void DumpStats(string reason)
        {
            if (stats.Count == 0)
            {
                Debug.Log("SceneAudioProfiler: no samples collected yet.");
                return;
            }

            var ordered = stats
                .Select(kvp => new SceneReport
                {
                    scene = kvp.Key,
                    average = kvp.Value.Average,
                    peak = kvp.Value.peak,
                    samples = kvp.Value.count
                })
                .OrderBy(r => r.average)
                .ToList();

            Debug.Log($"SceneAudioProfiler dump ({reason}):\n" + string.Join("\n", ordered.Select(r => $"{r.scene}: avg={r.average:F4}, peak={r.peak:F4}, samples={r.samples}")));

            if (writeCsv)
            {
                try
                {
                    string path = Path.Combine(Application.persistentDataPath, csvFileName);
                    using (var sw = new StreamWriter(path, false))
                    {
                        sw.WriteLine("Scene,SampleCount,AverageRMS,PeakRMS");
                        foreach (var r in ordered)
                        {
                            sw.WriteLine($"{r.scene},{r.samples},{r.average:F6},{r.peak:F6}");
                        }
                    }
                    Debug.Log($"SceneAudioProfiler wrote CSV to {path}");
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"SceneAudioProfiler: failed to write CSV: {e.Message}");
                }
            }

            if (clearAfterDump)
                stats.Clear();
        }

        private void OnApplicationQuit()
        {
            if (dumpOnQuit)
                DumpStats("Application quit");
        }

        [Serializable]
        private class SceneStats
        {
            public double sum;
            public int count;
            public float peak;
            public float Average => count <= 0 ? 0f : (float)(sum / count);
        }

        private struct SceneReport
        {
            public string scene;
            public float average;
            public float peak;
            public int samples;
        }
    }
}
