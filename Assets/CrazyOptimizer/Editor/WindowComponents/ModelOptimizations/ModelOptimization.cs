using CrazyGames.TreeLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CrazyGames.WindowComponents.ModelOptimizations
{
    public class ModelOptimization : EditorWindow
    {
        private static MultiColumnHeaderState _multiColumnHeaderState;
        private static ModelTree _modelTree;

        private static bool _isAnalyzing;
        private static bool _includeFilesFromPackages;

        // HashSet for O(1) extension lookups instead of O(n) List.Contains
        private static readonly HashSet<string> ModelFormats =
            new(StringComparer.OrdinalIgnoreCase) { ".fbx", ".dae", ".3ds", ".dxf", ".obj" };

        // Compiled once at class-load time; was being re-compiled on every analysis call
        private static readonly Regex ResourcesPathRegex =
            new(@"\w*(?<!Editor\/)Resources\/", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static void RenderGUI()
        {
            var rect = EditorGUILayout.BeginVertical(GUILayout.MinHeight(300));
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginVertical();
            GUILayout.Label("Press \"Analyze models\" button to load the table.");
            GUILayout.Label("Press it again when you need to refresh the data.");
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            _modelTree?.OnGUI(rect);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button(_isAnalyzing ? "Analyzing..." : "Analyze models", GUILayout.Width(200)))
            {
                AnalyzeModels();
            }

            var originalValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 160;
            _includeFilesFromPackages = EditorGUILayout.Toggle("Include files from Packages", _includeFilesFromPackages);
            EditorGUIUtility.labelWidth = originalValue;
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            GUILayout.Label(
                "This utility gives you an overview of the models used in your project. By optimizing various settings, you will be able to considerably decrease your final build size. You can click on a model to select it in the Project view. To find out more about how the tool finds the models, please check our GitHub repo.",
                EditorStyles.wordWrappedLabel);

            BuildExplanation("R/W enabled",
                "When a Mesh is read/write enabled, Unity uploads the Mesh data to GPU-addressable memory, but also keeps it in CPU-addressable memory. In most cases, you should disable this option to save runtime memory usage.");
            BuildExplanation("Polygons optimized",
                "Optimize the order of polygons in the mesh to make better use of the GPUs internal caches to improve rendering performance.");
            BuildExplanation("Vertices optimized",
                "Optimize the order of vertices in the mesh to make better use of the GPUs internal caches to improve rendering performance.");
            BuildExplanation("Mesh compression",
                "Compressing meshes will decrease the final build size, but more compression introduces more artifacts in vertex data.");
            BuildExplanation("Animation compression",
                "Compressing animations will decrease the final build size, but more compression introduces more artifacts in the animations.");
        }

        static void BuildExplanation(string label, string explanation)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(130));
            GUILayout.Label(explanation, EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // Collect model dependencies of a scene directly into the destination set
        static void CollectSceneModelDependencies(string scenePath, HashSet<string> result)
        {
            foreach (var dep in AssetDatabase.GetDependencies(scenePath, true))
            {
                if (IsModelAtPath(dep))
                    result.Add(dep);
            }
        }

        static HashSet<string> GetUsedModelsInBuildScenes()
        {
            var usedModelPaths = new HashSet<string>();
            foreach (var scenePath in OptimizerUtils.GetScenesInBuildPath())
                CollectSceneModelDependencies(scenePath, usedModelPaths);
            return usedModelPaths;
        }

        // Get models referenced by assets inside Resources folders (excluding Editor-only Resources).
        static HashSet<string> GetUsedModelsInResources()
        {
            var usedModelPaths = new HashSet<string>();

            // Keep only the assets inside a Resources folder that is not inside an Editor folder
            var resourceAssets = AssetDatabase
                .FindAssets("", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => ResourcesPathRegex.IsMatch(path));

            foreach (var assetPath in resourceAssets)
            {
                foreach (var dep in AssetDatabase.GetDependencies(assetPath, true))
                {
                    if (IsModelAtPath(dep))
                        usedModelPaths.Add(dep);
                }
            }

            return usedModelPaths;
        }

        static bool IsModelAtPath(string assetPath)
        {
            return AssetDatabase.GetMainAssetTypeAtPath(assetPath) == typeof(GameObject) &&
                   ModelFormats.Contains(Path.GetExtension(assetPath));
        }

        static void AnalyzeModels()
        {
            _isAnalyzing = true;
            OptimizerWindow.EditorWindowInstance?.Repaint();

            // Merge both sources without intermediate ToList + re-add to HashSet
            var usedModelPaths = GetUsedModelsInBuildScenes();
            usedModelPaths.UnionWith(GetUsedModelsInResources());

            var treeElements = new List<ModelTreeItem>(usedModelPaths.Count + 1);
            var idIncrement = 0;
            treeElements.Add(new ModelTreeItem("Root", -1, idIncrement, null, null));

            foreach (var modelPath in usedModelPaths)
            {
                if (modelPath.StartsWith("Packages/") && !_includeFilesFromPackages)
                    continue;

                try
                {
                    var modelImporter = (ModelImporter)AssetImporter.GetAtPath(modelPath);
                    treeElements.Add(new ModelTreeItem("Model", 0, ++idIncrement, modelPath, modelImporter));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to analyze model at path: {modelPath}\n{e.Message}");
                }
            }

            var treeModel = new TreeModel<ModelTreeItem>(treeElements);
            var treeViewState = new TreeViewState();

            // Preserve column state across re-analyses; only create once
            _multiColumnHeaderState ??= new MultiColumnHeaderState(new[]
            {
                // When adding a column, update SortIfNeeded and CellGUI too
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Model"),                width = 150, minWidth = 150, canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("R/W enabled"),         width = 80,  minWidth = 80,  canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Polygons optimized"),  width = 120, minWidth = 120, canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Vertices optimized"),  width = 120, minWidth = 120, canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Mesh compression"),    width = 120, minWidth = 120, canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Animation compression"), width = 140, minWidth = 140, canSort = true },
            });

            _modelTree = new ModelTree(treeViewState, new MultiColumnHeader(_multiColumnHeaderState), treeModel);
            _isAnalyzing = false;
            OptimizerWindow.EditorWindowInstance?.Repaint();
        }
    }
}