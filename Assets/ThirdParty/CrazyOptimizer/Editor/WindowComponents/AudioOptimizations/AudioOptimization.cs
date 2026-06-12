using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CrazyGames.TreeLib;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CrazyGames.WindowComponents.AudioOptimizations
{
    public class AudioOptimization : EditorWindow
    {
        private static MultiColumnHeaderState _multiColumnHeaderState;
        private static AudioTree _audioCompressionTree;

        private static bool _isAnalyzing;
        private static bool _includeFilesFromPackages;

        // Compiled once; reused across analyses
        private static readonly Regex ResourcesPathRegex =
            new(@"\w*(?<!Editor\/)Resources\/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void RenderGUI()
        {
            var rect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(300));
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Press \"Analyze audio\" button to load the table.");
            GUILayout.Label("Press it again when you need to refresh the data.");
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            _audioCompressionTree?.OnGUI(rect);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_isAnalyzing ? "Analyzing..." : "Analyze audio", GUILayout.Width(200)))
            {
                AnalyzeAudio();
            }

            var originalValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 160;
            _includeFilesFromPackages = EditorGUILayout.Toggle("Include files from Packages", _includeFilesFromPackages);
            EditorGUIUtility.labelWidth = originalValue;
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            GUILayout.Label(
                "This utility gives you an overview of the audio clips used in your project. By optimizing various settings, you will be able to considerably decrease your final build size and runtime memory usage. You can click on an audio clip to select it in the Project view. To find out more about how the tool finds the audio clips, please check our GitHub repo.",
                EditorStyles.wordWrappedLabel);

            BuildExplanation("Load type",
                "The default option, Decompress On Load, is good for audio clips that require precision when played, for example, audio effects or dialogues. For background audio clips Compressed In Memory is recommended, since it reduces the runtime memory, though audio playback is less precise and may introduce latency.");
            BuildExplanation("Quality",
                "Lowering the quality will reduce the build size. You can experiment with a lower audio quality for background audio.");
        }

        static void BuildExplanation(string label, string explanation)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(130));
            GUILayout.Label(explanation, EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // Find recursively the audio clips on which this scene depends.
        static void CollectSceneAudioDependencies(string scenePath, HashSet<string> result)
        {
            foreach (var assetDependency in AssetDatabase.GetDependencies(scenePath, true))
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(assetDependency) == typeof(AudioClip))
                    result.Add(assetDependency);
            }
        }

        static HashSet<string> GetUsedAudioInBuildScenes()
        {
            var usedAudioPaths = new HashSet<string>();
            foreach (var scenePath in OptimizerUtils.GetScenesInBuildPath())
                CollectSceneAudioDependencies(scenePath, usedAudioPaths);
            return usedAudioPaths;
        }

        // Get audio clips referenced by assets inside Resources folders (excluding Editor-only Resources).
        static HashSet<string> GetUsedAudioInResources()
        {
            var usedAudioPaths = new HashSet<string>();

            // Filter to assets inside a Resources folder that is not inside an Editor folder
            var resourceAssets = AssetDatabase
                .FindAssets("", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => ResourcesPathRegex.IsMatch(path));

            foreach (var assetPath in resourceAssets)
            {
                foreach (var dep in AssetDatabase.GetDependencies(assetPath, true))
                {
                    if (AssetDatabase.GetMainAssetTypeAtPath(dep) == typeof(AudioClip))
                        usedAudioPaths.Add(dep);
                }
            }

            return usedAudioPaths;
        }

        static void AnalyzeAudio()
        {
            _isAnalyzing = true;
            OptimizerWindow.EditorWindowInstance?.Repaint();

            // Merge results from both sources without intermediate ToList conversions
            var usedAudioPaths = GetUsedAudioInBuildScenes();
            usedAudioPaths.UnionWith(GetUsedAudioInResources());

            var treeElements = new List<AudioTreeItem>(usedAudioPaths.Count + 1);
            var idIncrement = 0;
            treeElements.Add(new AudioTreeItem("Root", -1, idIncrement, null, null));

            foreach (var audioPath in usedAudioPaths)
            {
                if (audioPath.StartsWith("Packages/") && !_includeFilesFromPackages)
                    continue;

                try
                {
                    var audioImporter = (AudioImporter)AssetImporter.GetAtPath(audioPath);
                    treeElements.Add(new AudioTreeItem("AudioClip", 0, ++idIncrement, audioPath, audioImporter));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to analyze audio clip at path: {audioPath}\n{e.Message}");
                }
            }

            var treeModel = new TreeModel<AudioTreeItem>(treeElements);
            var treeViewState = new TreeViewState();

            // Always rebuild the header state so stale column data doesn't persist across re-analyses
            _multiColumnHeaderState = new MultiColumnHeaderState(new[]
            {
                // When adding a new column, update SortIfNeeded and CellGUI accordingly
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Audio clip"), width = 150, minWidth = 150, canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Load type"),  width = 150, minWidth = 150, canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Quality"),    width = 60,  minWidth = 60,  canSort = true },
            });

            _audioCompressionTree = new AudioTree(treeViewState, new MultiColumnHeader(_multiColumnHeaderState), treeModel);
            _isAnalyzing = false;
            OptimizerWindow.EditorWindowInstance?.Repaint();
        }
    }
}