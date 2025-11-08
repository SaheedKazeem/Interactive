using System.Collections.Generic;
using System.IO;
using System.Linq;
using Interactive.Audio;
using UnityEditor;
using UnityEngine;

namespace Interactive.EditorTools
{
    /// <summary>
    /// Inspector-style editor window for managing scene music cues inside music.json.
    /// Lets you pick a scene, edit its cue list, and save back to StreamingAssets.
    /// </summary>
    public class MusicSceneCueEditor : EditorWindow
    {
        private MusicProjectConfig config;
        private int selectedSceneIndex;
        private Vector2 cueScroll;
        private GUIStyle boxStyle;

        private string ConfigPath => Path.Combine(Application.streamingAssetsPath, "music.json");

        [MenuItem("Tools/Interactive/Music Scene Cue Editor")]
        public static void Open()
        {
            GetWindow<MusicSceneCueEditor>("Music Cues");
        }

        private void OnEnable()
        {
            LoadConfig();
        }

        private void OnGUI()
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox("music.json not found or failed to parse.", MessageType.Warning);
                if (GUILayout.Button("Reload")) LoadConfig();
                return;
            }

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    padding = new RectOffset(8, 8, 4, 4)
                };
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload", GUILayout.Width(80))) LoadConfig();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Save", GUILayout.Width(80))) SaveConfig();
            EditorGUILayout.EndHorizontal();

            if (config.scenes == null || config.scenes.Count == 0)
            {
                EditorGUILayout.HelpBox("No scenes defined in music.json.", MessageType.Info);
                return;
            }

            string[] sceneNames = config.scenes.Select(s => string.IsNullOrEmpty(s.name) ? "<unnamed>" : s.name).ToArray();
            selectedSceneIndex = Mathf.Clamp(selectedSceneIndex, 0, sceneNames.Length - 1);
            selectedSceneIndex = EditorGUILayout.Popup("Scene", selectedSceneIndex, sceneNames);
            var scene = config.scenes[selectedSceneIndex];

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Cues for '{scene.name}'", EditorStyles.boldLabel);

            cueScroll = EditorGUILayout.BeginScrollView(cueScroll);
            scene.cues ??= new List<MusicCue>();
            for (int i = 0; i < scene.cues.Count; i++)
            {
                var cue = scene.cues[i];
                EditorGUILayout.BeginVertical(boxStyle);
                EditorGUILayout.BeginHorizontal();
                cue.file = EditorGUILayout.TextField("File", cue.file);
                if (GUILayout.Button("...", GUILayout.Width(30)))
                {
                    string picked = EditorUtility.OpenFilePanel("Select audio file", string.IsNullOrEmpty(cue.file) ? Application.dataPath : Path.GetDirectoryName(cue.file), "mp3,ogg,wav");
                    if (!string.IsNullOrEmpty(picked)) cue.file = picked.Replace('\\', '/');
                }
                EditorGUILayout.EndHorizontal();

                cue.startOnSceneLoad = EditorGUILayout.Toggle("Start On Scene Load", cue.startOnSceneLoad);
                cue.startAtVideoTime = EditorGUILayout.FloatField(new GUIContent("Start At Video Time", "Seconds from VideoPlayer time to trigger; set -1 to disable."), cue.startAtVideoTime);
                cue.stopAtVideoTime = EditorGUILayout.FloatField(new GUIContent("Stop At Video Time", "Seconds from VideoPlayer time to fade out; set -1 to disable."), cue.stopAtVideoTime);
                cue.volume = EditorGUILayout.Slider("Volume", cue.volume, 0f, 1f);
                cue.fadeIn = EditorGUILayout.FloatField("Fade In", cue.fadeIn);
                cue.fadeOut = EditorGUILayout.FloatField("Fade Out", cue.fadeOut);
                cue.loop = EditorGUILayout.Toggle("Loop", cue.loop);

                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Duplicate", GUILayout.Width(90)))
                {
                    scene.cues.Insert(i + 1, CloneCue(cue));
                }
                if (GUILayout.Button("Remove", GUILayout.Width(90)))
                {
                    scene.cues.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space();
            }
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Add Cue"))
            {
                scene.cues.Add(new MusicCue
                {
                    file = string.Empty,
                    startOnSceneLoad = true,
                    startAtVideoTime = -1f,
                    stopAtVideoTime = -1f,
                    volume = 0.5f,
                    fadeIn = 0.5f,
                    fadeOut = 0.5f,
                    loop = true
                });
            }
        }

        private void LoadConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    config = new MusicProjectConfig { scenes = new List<SceneMusicConfig>() };
                    return;
                }
                string json = File.ReadAllText(ConfigPath);
                config = JsonUtility.FromJson<MusicProjectConfig>(json) ?? new MusicProjectConfig();
                selectedSceneIndex = Mathf.Clamp(selectedSceneIndex, 0, Mathf.Max(0, (config.scenes?.Count ?? 1) - 1));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"MusicSceneCueEditor: failed to load music.json: {e.Message}");
                config = null;
            }
        }

        private void SaveConfig()
        {
            if (config == null) return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(ConfigPath, json);
                AssetDatabase.Refresh();
                Debug.Log($"MusicSceneCueEditor: saved {config.scenes?.Count ?? 0} scenes to {ConfigPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"MusicSceneCueEditor: failed to save music.json: {e.Message}");
            }
        }

        private static MusicCue CloneCue(MusicCue cue)
        {
            return new MusicCue
            {
                file = cue.file,
                startOnSceneLoad = cue.startOnSceneLoad,
                startAtVideoTime = cue.startAtVideoTime,
                stopAtVideoTime = cue.stopAtVideoTime,
                volume = cue.volume,
                fadeIn = cue.fadeIn,
                fadeOut = cue.fadeOut,
                loop = cue.loop
            };
        }
    }
}
