using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CrazyGames.TreeLib;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CrazyGames.WindowComponents.TextureOptimizations
{
    public class TextureOptimization : EditorWindow
    {
        private static MultiColumnHeaderState _multiColumnHeaderState;
        private static TextureTree _textureCompressionTree;

        private static bool _isAnalyzing;
        private static bool _includeFilesFromPackages;

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
            GUILayout.Label("Press \"Analyze textures\" button to load the table.");
            GUILayout.Label("Press it again when you need to refresh the data.");
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
            _textureCompressionTree?.OnGUI(rect);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(_isAnalyzing ? "Analyzing..." : "Analyze textures", GUILayout.Width(200)))
            {
                AnalyzeTextures();
            }

            var originalValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 160;
            _includeFilesFromPackages = EditorGUILayout.Toggle("Include files from Packages", _includeFilesFromPackages);
            EditorGUIUtility.labelWidth = originalValue;
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(5);

            GUILayout.Label(
                "This utility gives you an overview of the textures used in your project. By optimizing various settings, you will be able to considerably decrease your final build size. You can click on a texture to select it in the Project view. To find out more about how the tool finds the textures, please check our GitHub repo.",
                EditorStyles.wordWrappedLabel);

            BuildExplanation("Max size",
                "Decrease the max size as much as possible while the texture still looks good in game. You most likely don't need the default 2048 set by Unity.");
            BuildExplanation("Compression", "Lower quality will decrease the final build size.");
            BuildExplanation("Crunch compression",
                "All the textures with crunch compression enabled will be compressed together, decreasing the final build size.");
            BuildExplanation("Crunch comp. quality", "A higher compression quality means larger textures and longer compression times.");
        }

        static void BuildExplanation(string label, string explanation)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, EditorStyles.boldLabel, GUILayout.Width(130));
            GUILayout.Label(explanation, EditorStyles.wordWrappedLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        // Collect texture dependencies of a scene directly into the destination set
        static void CollectSceneTextureDependencies(string scenePath, HashSet<string> result)
        {
            foreach (var dep in AssetDatabase.GetDependencies(scenePath, true))
            {
                if (AssetDatabase.GetMainAssetTypeAtPath(dep) == typeof(Texture2D))
                    result.Add(dep);
            }
        }

        static HashSet<string> GetUsedTexturesInBuildScenes()
        {
            var usedTexturePaths = new HashSet<string>();
            foreach (var scenePath in OptimizerUtils.GetScenesInBuildPath())
                CollectSceneTextureDependencies(scenePath, usedTexturePaths);
            return usedTexturePaths;
        }

        // Get textures referenced by assets inside Resources folders (excluding Editor-only Resources).
        static HashSet<string> GetUsedTexturesInResources()
        {
            var usedTexturePaths = new HashSet<string>();

            var resourceAssets = AssetDatabase
                .FindAssets("", new[] { "Assets" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => ResourcesPathRegex.IsMatch(path));

            foreach (var assetPath in resourceAssets)
            {
                foreach (var dep in AssetDatabase.GetDependencies(assetPath, true))
                {
                    if (AssetDatabase.GetMainAssetTypeAtPath(dep) == typeof(Texture2D))
                        usedTexturePaths.Add(dep);
                }
            }

            return usedTexturePaths;
        }

        static void AnalyzeTextures()
        {
            _isAnalyzing = true;
            OptimizerWindow.EditorWindowInstance?.Repaint();

            // Merge both sources without intermediate ToList + re-add to HashSet
            var usedTexturePaths = GetUsedTexturesInBuildScenes();
            usedTexturePaths.UnionWith(GetUsedTexturesInResources());

            var treeElements = new List<TextureTreeItem>(usedTexturePaths.Count + 1);
            var idIncrement = 0;
            treeElements.Add(new TextureTreeItem("Root", -1, idIncrement, null, null));

            foreach (var texturePath in usedTexturePaths)
            {
                if (texturePath.StartsWith("Packages/") && !_includeFilesFromPackages)
                    continue;

                try
                {
                    var textureImporter = (TextureImporter)AssetImporter.GetAtPath(texturePath);
                    treeElements.Add(new TextureTreeItem("Texture2D", 0, ++idIncrement, texturePath, textureImporter));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to analyze texture at path: {texturePath}\n{e.Message}");
                }
            }

            var treeModel = new TreeModel<TextureTreeItem>(treeElements);
            var treeViewState = new TreeViewState();

            // Preserve column state across re-analyses; only create once
            _multiColumnHeaderState ??= new MultiColumnHeaderState(new[]
            {
                // When adding a column, update SortIfNeeded and CellGUI too
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Texture"),             width = 150, minWidth = 150, canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Type"),                width = 60,  minWidth = 60,  canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Max size"),            width = 60,  minWidth = 60,  canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Compression"),         width = 80,  minWidth = 80,  canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Crunch compression"),  width = 120, minWidth = 120, canSort = true },
                new MultiColumnHeaderState.Column { headerContent = new GUIContent("Crunch comp. quality"), width = 128, minWidth = 128, canSort = true },
            });

            _textureCompressionTree = new TextureTree(treeViewState, new MultiColumnHeader(_multiColumnHeaderState), treeModel);
            _isAnalyzing = false;
            OptimizerWindow.EditorWindowInstance?.Repaint();
        }
    }
}